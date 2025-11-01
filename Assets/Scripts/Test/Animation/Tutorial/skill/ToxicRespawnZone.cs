using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToxicRespawnZone : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint; // 돌아갈 위치
    [SerializeField] private float delay = 0.2f;     // 살짝 늦게 이동 (자연스럽게)

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(RespawnAfterDelay(other.transform));
        }
    }

    private System.Collections.IEnumerator RespawnAfterDelay(Transform player)
    {
        yield return new WaitForSeconds(delay);

        // Rigidbody나 CharacterController를 잠시 꺼두면 안정적
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 실제 이동
        player.position = respawnPoint.position;
        player.rotation = respawnPoint.rotation;
    }
}