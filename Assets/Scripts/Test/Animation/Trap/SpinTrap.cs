using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpinTrap : MonoBehaviour
{
    [Header("회전 설정")]
    public Vector3 rotationAxis = Vector3.up;   // 회전 축
    public float rotationSpeed = 100f;          // 초당 회전 속도

    [Header("밀치는 힘 설정")]
    public float pushForce = 10f;               // 플레이어를 밀어내는 힘
    public float upwardForce = 2f;              // 살짝 위로 튀기기

    private void Update()
    {
        // 매 프레임마다 회전
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime, Space.Self);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Player 태그를 가진 오브젝트와 충돌했을 때
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 플레이어의 방향 계산 (장애물로부터 멀어지게)
                Vector3 pushDir = (collision.transform.position - transform.position).normalized;
                Vector3 force = (pushDir + Vector3.up * upwardForce).normalized * pushForce;

                rb.AddForce(force, ForceMode.Impulse);
            }
        }
    }
}