using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetDestroy : MonoBehaviour
{
    [Header("파괴 이펙트")]
    [Tooltip("폭발 파티클 프리팹을 넣어주세요.")]
    public GameObject destroyEffectPrefab;
    public float effectDuration = 2f;

    private void OnTriggerEnter(Collider other)
    {
        // Fireball 태그를 가진 오브젝트와 충돌 시 파괴
        if (other.CompareTag("Fireball"))
        {
            
            // 폭발 이펙트 생성
            if (destroyEffectPrefab != null)
            {
                GameObject fx = Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
                Destroy(fx, effectDuration);
            }

            // 표적 파괴
            Destroy(gameObject);
        }
    }
}