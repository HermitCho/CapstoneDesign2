using UnityEngine;
using Photon.Pun;

public class ProximityExplosiveObstacle : MonoBehaviourPun
{
    [Header("Detection Settings")]
    public float detectionRadius = 5f;
    public LayerMask playerLayer;

    [Header("Explosion Settings")]
    public float explosionRadius = 6f;
    public float explosionForce = 800f;
    public float damage = 50f;
    public GameObject explosionEffect;

    private bool hasExploded = false;

    private void Update()
    {
        if (hasExploded) return;

        // 플레이어 감지
        Collider[] players = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
        if (players.Length > 0)
        {
            Explode();
        }
    }

    private void Explode()
    {
        hasExploded = true;

        // 폭발 이펙트 생성
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // 폭발력 및 데미지 적용
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider nearby in colliders)
        {
            Rigidbody rb = nearby.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 1f, ForceMode.Impulse);

            // LivingEntity에 데미지 주기
            LivingEntity entity = nearby.GetComponent<LivingEntity>();
            if (entity != null && PhotonNetwork.IsMasterClient)
            {
                Vector3 hitPoint = nearby.ClosestPoint(transform.position);
                Vector3 hitNormal = (nearby.transform.position - transform.position).normalized;
                int attackerViewId = -1; // 폭탄은 플레이어가 아니므로 ViewID 없음

                // RPC로 데미지 적용 (마스터 클라이언트에서만 실행)
                entity.photonView.RPC(
                    "OnDamage",
                    RpcTarget.All,
                    damage,
                    hitPoint,
                    hitNormal,
                    attackerViewId
                );
            }
        }

        // 폭탄 제거
        Destroy(gameObject, 0.1f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}