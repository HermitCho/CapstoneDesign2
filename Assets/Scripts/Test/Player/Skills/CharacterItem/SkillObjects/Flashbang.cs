using Photon.Pun;
using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// 섬광탄의 물리적 동작, 충돌 처리 및 폭발을 담당하는 스크립트.
/// </summary>
public class Flashbang : MonoBehaviourPun
{
    [Header("섬광탄 설정")]
    [SerializeField] private float damage = 30f;
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 2f; // 섬광탄 수명
    [SerializeField] private float explosionRadius = 5f; // 폭발 반경
    [SerializeField] private float stunTime = 2f;

    private Rigidbody rb;
    private AudioSource aS;
    [SerializeField] private ParticleSystem explosionEffect; // 폭발 이펙트 프리팹
    private int ownerActorNumber; // 발사한 플레이어의 ActorNumber
    private bool hasExploded = false; // 중복 폭발 방지 플래그

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        aS = GetComponent<AudioSource>();
    }

    /// <summary>
    /// 섬광탄을 초기화하고 발사합니다.
    /// </summary>
    [PunRPC]
    public void InitializeAndLaunch(int ownerId, Vector3 direction, float launchSpeed) // launchSpeed 파라미터 추가
    {
        Debug.Log("[Flashbang - InitializeAndLaunch] - 생성 및 초기화");
        ownerActorNumber = ownerId;
        rb.velocity = direction.normalized * launchSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        // 마스터 클라이언트만 충돌 및 데미지 처리를 담당
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("섬광탄이 닿음 " + collision.transform.name);
            hasExploded = true;
            Vector3 explosionPosition = transform.position;

            photonView.RPC("PlayExplosionSound", RpcTarget.All, explosionPosition);
            // 모든 클라이언트에서 폭발 효과 및 범위 데미지 처리
            photonView.RPC("ExplodeAndApplyAreaStunRPC", RpcTarget.All, explosionPosition);
        }
    }

    [PunRPC]
    private void ExplodeAndApplyAreaStunRPC(Vector3 explosionPosition)
    {
        // 폭발 이펙트 생성
        if (explosionEffect != null)
        {
            explosionEffect.Play();
        }

        // 마스터 클라이언트만 범위 내 플레이어에게 데미지 적용
        if (PhotonNetwork.IsMasterClient)
        {
            Collider[] colliders = Physics.OverlapSphere(explosionPosition, explosionRadius);

            foreach (Collider hitCollider in colliders)
            {
                MoveController hitPlayer = hitCollider.GetComponent<MoveController>();
                if (hitPlayer != null && hitPlayer.photonView.OwnerActorNr != ownerActorNumber)
                {
                    Vector3 hitPoint = hitPlayer.transform.position; // 기본값(실패 시 fallback)
                    Vector3 hitNormal = Vector3.zero;

                    // 폭발 중심 → 플레이어 중심 방향으로 레이캐스트 시도
                    Vector3 direction = (hitPlayer.transform.position - explosionPosition).normalized;
                    RaycastHit hit;

                    // 플레이어의 콜라이더에 레이가 닿으면 정확한 hitPoint와 hitNormal 계산
                    if (Physics.Raycast(explosionPosition, direction, out hit, explosionRadius))
                    {
                        // 플레이어 콜라이더에 닿은 경우에만 갱신
                        if (hit.collider == hitCollider)
                        {
                            hitPoint = hit.point;
                            hitNormal = hit.normal;
                        }
                    }

                    hitPlayer.photonView.RPC(
                        "RPCSetStunned",
                        RpcTarget.All,
                        stunTime
                    );
                }
            }
        }

        DestoryTimer().Forget();
    }


    [SerializeField] private AudioClip explosionSound;

    [PunRPC]
    void PlayExplosionSound(Vector3 explosionPosition)
    {
        aS.PlayOneShot(explosionSound, 1f);
    }

    public float GetFlashbangSpeed()
    {
        return speed;
    }

    private async UniTask DestoryTimer()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(lifetime), ignoreTimeScale: false);

        gameObject.SetActive(false);
        Debug.Log("[Flashbang - ExplodeAndApplyAreaStunRPC] : 섬광탄 정상 작동 후 사라짐");
    }
}