using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class RollTrap : MonoBehaviourPun
{
    [Header("감지 설정")]
    public float detectionRadius = 6f;
    public LayerMask playerLayer;

    [Header("회전 설정")]
    public float rollForce = 10f;
    public Vector3 rollDirection = Vector3.right; // 굴러갈 방향
    public float maxSpeed = 8f;

    [Header("데미지 설정")]
    public float damage = 30f;

    private Rigidbody rb;
    private bool isRolling = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // 처음엔 고정된 상태
    }

    private void Update()
    {
        if (isRolling) return;

        // 일정 거리 내 플레이어 감지
        Collider[] players = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
        if (players.Length > 0)
        {
            StartRolling();
        }
    }

    private void StartRolling()
    {
        isRolling = true;
        rb.isKinematic = false;
        rb.AddForce(rollDirection.normalized * rollForce, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        if (isRolling && rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        LivingEntity entity = collision.collider.GetComponent<LivingEntity>();
        if (entity != null && PhotonNetwork.IsMasterClient)
        {
            Vector3 hitPoint = collision.contacts[0].point;
            Vector3 hitNormal = collision.contacts[0].normal;
            int attackerViewId = -1; // 트랩이므로 공격자 없음

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}