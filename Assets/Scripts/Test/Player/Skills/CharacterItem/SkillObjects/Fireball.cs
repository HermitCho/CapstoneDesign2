using Photon.Pun;
using UnityEngine;
using System.Collections;

public class Fireball : MonoBehaviourPun, IPunObservable
{
    [Header("파이어볼 설정")]
    [SerializeField] private float damage = 30f;
    [SerializeField] private float speed = 50f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float explosionRadius = 5f;

    private Rigidbody rb;
    private AudioSource aS;
    [SerializeField] private AudioClip burningSound;
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private GameObject explosionSoundPrefab;

    private int ownerActorNumber;
    private bool hasExploded = false;

    // 네트워크 보간용
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private bool firstSync = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        aS = GetComponent<AudioSource>();
    }

    private void Start()
    {
        rb.isKinematic = !photonView.IsMine;
        StartCoroutine(PlayBurningSound());
        StartCoroutine(DeactivateAfterTime());
    }

    [PunRPC]
    public void InitializeAndLaunch(int ownerId, Vector3 direction, float launchSpeed)
    {
        ownerActorNumber = ownerId;

        // 자신이 생성한 Fireball만 실제 물리 이동 처리
        if (photonView.IsMine)
        {
            rb.velocity = direction.normalized * launchSpeed;
        }
    }

    private void FixedUpdate()
    {
        // 자신이 생성하지 않은 Fireball은 보간만 수행
        if (!photonView.IsMine)
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.fixedDeltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.fixedDeltaTime * 10f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        // 자신이 생성한 Fireball만 폭발 처리
        if (photonView.IsMine)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                MoveController hitPlayer = collision.gameObject.GetComponent<MoveController>();
                if (hitPlayer != null && hitPlayer.photonView.OwnerActorNr != ownerActorNumber)
                {
                    ExplodeAt(transform.position);
                }
            }
            else
            {
                ExplodeAt(transform.position);
            }
        }
    }

    private void ExplodeAt(Vector3 explosionPosition)
    {
        if (hasExploded) return;
        hasExploded = true;

        // 모든 클라이언트에서 동일한 폭발 연출
        photonView.RPC("PlayExplosionSound", RpcTarget.All, explosionPosition);
        photonView.RPC("ExplodeAndApplyAreaDamageRPC", RpcTarget.All, explosionPosition);
    }

    [PunRPC]
    private void ExplodeAndApplyAreaDamageRPC(Vector3 explosionPosition)
    {
        // 폭발 이펙트
        if (explosionEffectPrefab != null)
            Instantiate(explosionEffectPrefab, explosionPosition, Quaternion.identity);

        // 데미지는 자신이 생성한 Fireball일 때만 적용
        if (photonView.IsMine)
        {
            Collider[] colliders = Physics.OverlapSphere(explosionPosition, explosionRadius);

            foreach (Collider hitCollider in colliders)
            {
                MoveController hitPlayer = hitCollider.GetComponent<MoveController>();
                if (hitPlayer != null && hitPlayer.photonView.OwnerActorNr != ownerActorNumber)
                {
                    Vector3 hitPoint = hitPlayer.transform.position;
                    Vector3 hitNormal = Vector3.zero;

                    Vector3 direction = (hitPlayer.transform.position - explosionPosition).normalized;
                    if (Physics.Raycast(explosionPosition, direction, out RaycastHit hit, explosionRadius))
                    {
                        if (hit.collider == hitCollider)
                        {
                            hitPoint = hit.point;
                            hitNormal = hit.normal;
                        }
                    }

                    hitPlayer.photonView.RPC("OnDamage", RpcTarget.All, damage, hitPoint, hitNormal, ownerActorNumber);
                }
            }
        }

        // 오브젝트 비활성화
        PhotonNetwork.Destroy(gameObject);
    }

    private IEnumerator DeactivateAfterTime()
    {
        yield return new WaitForSeconds(lifetime);

        if (!hasExploded && gameObject.activeInHierarchy)
        {
            ExplodeAt(transform.position);
        }
    }

    [PunRPC]
    IEnumerator PlayBurningSound()
    {
        aS.PlayOneShot(burningSound, 2.0f);
        yield return new WaitForSeconds(burningSound.length);
    }

    [PunRPC]
    void PlayExplosionSound(Vector3 explosionPosition)
    {
        if (explosionSoundPrefab != null)
        {
            Instantiate(explosionSoundPrefab, explosionPosition, Quaternion.identity);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();

            if (firstSync)
            {
                transform.position = networkPosition;
                transform.rotation = networkRotation;
                firstSync = false;
            }
        }
    }

    public float GetFireballSpeed() => speed;
}
