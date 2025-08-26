using Photon.Pun;
using UnityEngine;
using System.Collections;

/// <summary>
/// 파이어볼 스킬 동작을 담당하는 스크립트.
/// 오브젝트 풀링을 사용하여 성능을 최적화합니다.
/// </summary>
public class Fireball : Skill
{
    [Header("파이어볼 설정")]
    [SerializeField] private float damage = 30f;
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 5f; // 파이어볼 수명
    [SerializeField] private GameObject explosionEffectPrefab; // 폭발 이펙트 프리팹


    private PhotonView photonView;
    private Rigidbody rb;
    private Vector3 initialDirection;
    private int ownerActorNumber; // 발사한 플레이어의 ActorNumber

    // 초기화 메서드 (오브젝트 풀에서 가져올 때 호출)
    public void Initialize(int ownerId, Vector3 direction)
    {
        ownerActorNumber = ownerId;
        initialDirection = direction;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            photonView = GetComponent<PhotonView>();
        }

        // 파이어볼 발사
        rb.velocity = initialDirection.normalized * speed;

        // 일정 시간 후 파이어볼 비활성화
        StartCoroutine(DeactivateAfterTime());
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 마스터 클라이언트만 충돌 처리를 담당
        if (PhotonNetwork.IsMasterClient)
        {
            // 충돌한 오브젝트가 플레이어인지 확인
            MoveController hitPlayer = collision.gameObject.GetComponent<MoveController>();

            if (hitPlayer != null)
            {
                // (핵심) 발사자와 충돌 대상이 다른 플레이어인지 확인
                if (hitPlayer.photonView.OwnerActorNr != ownerActorNumber)
                {
                    // RPC 호출을 통해 데미지 적용
                    hitPlayer.photonView.RPC("ApplyDamageRPC", RpcTarget.AllBuffered, damage);
                }
            }

            // 폭발 이펙트 생성 및 풀로 반환
            photonView.RPC("ExplodeAndDeactivateRPC", RpcTarget.All);
        }
    }

    // 모든 클라이언트에서 실행되는 폭발 및 비활성화 RPC
    [PunRPC]
    private void ExplodeAndDeactivateRPC()
    {
        // 폭발 이펙트 생성
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 파이어볼 오브젝트 비활성화 (풀로 반환)
        gameObject.SetActive(false);
    }

    // 일정 시간 후 파이어볼 비활성화
    private IEnumerator DeactivateAfterTime()
    {
        yield return new WaitForSeconds(lifetime);

        // 마스터 클라이언트만 비활성화 RPC 호출
        if (PhotonNetwork.IsMasterClient && gameObject.activeInHierarchy)
        {
            photonView.RPC("ExplodeAndDeactivateRPC", RpcTarget.All);
        }
    }
}