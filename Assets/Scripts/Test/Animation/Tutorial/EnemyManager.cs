using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static int killCount = 0; // 플레이어가 처치한 적의 수
    public int killGoal = 5; // 목표 처치 수

    // 적이 죽을 때마다 호출되도록 이 함수를 Enemy 스크립트에서 실행
    public static void AddKill()
    {
        killCount++;

        // 목표 킬 수 달성 시 튜토리얼 메시지 표시
        if (killCount == 5)
        {
            TutorialMessageManager.Instance.ShowMessage(5); // 5번 메시지
        }
    }
}