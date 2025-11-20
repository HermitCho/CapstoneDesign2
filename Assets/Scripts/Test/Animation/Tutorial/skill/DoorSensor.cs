using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorSensor : MonoBehaviour
{
    [Header("튜토리얼 완료 스크립트 참조")]
    [SerializeField] private TutorialComplete tutorialComplete;

    [Header("튜토리얼 스킬(UI) 참조")]
    [SerializeField] private TutorialSkill tutorialSkill;
    [SerializeField] private TutorialMove tutorialMove;

    [Header("감지 반경")]
    [SerializeField] private float detectRadius = 2f;

    private bool hasDetected = false;

    void Update()
    {
        if (hasDetected) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                hasDetected = true;

                // ✅ 문 열기
                if (tutorialComplete != null)
                    tutorialComplete.OpenDoor();

                // ✅ 튜토리얼 완료 (UI 스티커 표시)
                if (tutorialSkill != null)
                    tutorialSkill.CompleteTutorial();

                if (tutorialMove != null)
                    tutorialMove.CompleteTutorial();

                Debug.Log("✅ Player 접근 감지 → 문 열림 & 튜토리얼 완료 처리");
                break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
