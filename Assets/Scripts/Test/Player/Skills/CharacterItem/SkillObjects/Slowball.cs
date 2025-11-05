using Photon.Pun;
using UnityEngine;
using System.Collections;

public class Slowball : MonoBehaviourPun
{
    [Header("슬로우볼 설정")]
    [SerializeField] private float speed = 30f;
    [SerializeField] private GameObject slowFieldPrefab;

    private Rigidbody rb;
    private AudioSource aS;
    [SerializeField] private AudioClip throwingSound;

    private int ownerActorNumber;
    private bool hasExploded = false;

    private Vector3 networkPosition;
    private Quaternion networkRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        aS = GetComponent<AudioSource>();
        photonView.RPC("PlayThrowingSound", RpcTarget.All);
    }

    private void Start()
    {
        // 자신이 생성한 Slowball만 물리 계산
        rb.isKinematic = !photonView.IsMine;
    }

    [PunRPC]
    public void InitializeAndLaunch(int ownerId, Vector3 direction, float launchSpeed)
    {
        ownerActorNumber = ownerId;

        if (photonView.IsMine)
        {
            rb.velocity = direction.normalized * launchSpeed;
        }
    }

    private void FixedUpdate()
    {
        // 내 Slowball이 아니면 위치 보간만 수행
        if (!photonView.IsMine)
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.fixedDeltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.fixedDeltaTime * 10f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        hasExploded = true;

        // 자신이 생성한 Slowball만 폭발 처리
        if (photonView.IsMine)
        {
            Vector3 impactPosition = transform.position;

            RaycastHit hit;
            if (Physics.Raycast(impactPosition, Vector3.down, out hit, 100f))
            {
                photonView.RPC("SpawnSlowFieldRPC", RpcTarget.All, hit.point);
            }
            else
            {
                photonView.RPC("SpawnSlowFieldRPC", RpcTarget.All, impactPosition);
            }
        }
    }

    [PunRPC]
    private void SpawnSlowFieldRPC(Vector3 position)
    {
        if (slowFieldPrefab != null)
        {
            string prefabPath = "Prefabs/ItemObject/" + slowFieldPrefab.name;
            PhotonNetwork.Instantiate(prefabPath, position + new Vector3(0, 0.02f, 0), Quaternion.identity);
        }
        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    private void PlayThrowingSound()
    {
        if (aS != null && throwingSound != null)
        {
            aS.PlayOneShot(throwingSound);
        }
    }

    public float GetSlowballSpeed()
    {
        return speed;
    }

    // 다른 클라이언트로부터 받은 위치 동기화용 함수 (PhotonTransformView 대체)
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
        }
    }
}
