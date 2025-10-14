using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public void Die()
    {
        // 죽는 연출이나 파티클, 점수 처리 등 넣고
        EnemyManager.AddKill(); // ✅ 처치 카운트 증가
        Destroy(gameObject);
    }
}