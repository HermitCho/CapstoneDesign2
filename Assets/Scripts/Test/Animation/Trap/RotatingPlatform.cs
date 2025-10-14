using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingPlatform : MonoBehaviour
{
    [Header("회전 설정")]
    public Vector3 rotationAxis = Vector3.up; // 회전축 (보통 Y)
    public float rotationSpeed = 50f;         // 회전 속도 (초당 도 단위)

    private void Update()
    {
        // 매 프레임마다 회전
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime, Space.Self);
    }

    // 플레이어가 플랫폼 위에 올라왔을 때
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 플레이어를 플랫폼의 자식으로 설정
            collision.transform.SetParent(transform);
        }
    }

    // 플랫폼에서 벗어났을 때
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 부모 관계 해제
            collision.transform.SetParent(null);
        }
    }
}