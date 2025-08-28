using Photon.Pun;
using UnityEngine;
using System.Collections;

/// <summary>
/// 파이어볼의 물리적 동작, 충돌 처리 및 폭발을 담당하는 스크립트.
/// </summary>
public class Fireball : MonoBehaviourPun
{
    [Header("파이어볼 설정")]
    [SerializeField] private float damage = 30f;
    [SerializeField] private float speed = 50f;
    [SerializeField] private float lifetime = 5f; // 파이어볼 수명
    [SerializeField] private GameObject explosionEffectPrefab; // 폭발 이펙트 프리팹
    [SerializeField] private float explosionRadius = 5f; // 폭발 반경

    private Rigidbody rb;
    private int ownerActorNumber; // 발사한 플레이어의 ActorNumber
    private bool hasExploded = false; // 중복 폭발 방지 플래그

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// 파이어볼을 초기화하고 발사합니다.
    /// </summary>
    [PunRPC]
    public void InitializeAndLaunch(int ownerId, Vector3 direction, float launchSpeed) // launchSpeed 파라미터 추가
    {
        ownerActorNumber = ownerId;
        rb.velocity = direction.normalized * launchSpeed;

        StartCoroutine(DeactivateAfterTime());
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        // 마스터 클라이언트만 충돌 및 데미지 처리를 담당
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("파이어볼이 닿음 " + collision.transform.name);
            hasExploded = true;
            Vector3 explosionPosition = transform.position;

            // 모든 클라이언트에서 폭발 효과 및 범위 데미지 처리
            photonView.RPC("ExplodeAndApplyAreaDamageRPC", RpcTarget.All, explosionPosition);
        }
    }

    [PunRPC]
    private void ExplodeAndApplyAreaDamageRPC(Vector3 explosionPosition)
    {
        // 폭발 이펙트 생성
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, explosionPosition, Quaternion.identity);
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
                        "OnDamage",
                        RpcTarget.All,
                        damage,
                        hitPoint,
                        hitNormal,
                        ownerActorNumber
                    );
                }
            }
        }

        // 파이어볼 오브젝트 비활성화 (풀링 또는 제거)
        gameObject.SetActive(false);
    }


    private IEnumerator DeactivateAfterTime()
    {
        yield return new WaitForSeconds(lifetime);

        // 마스터 클라이언트만 비활성화 RPC 호출
        if (PhotonNetwork.IsMasterClient && gameObject.activeInHierarchy && !hasExploded)
        {
            hasExploded = true;
            Vector3 explosionPosition = transform.position;
            photonView.RPC("ExplodeAndApplyAreaDamageRPC", RpcTarget.All, explosionPosition);
        }
    }

    public float GetFireballSpeed()
    {
        return speed;
    }
}