using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TutorialMove : MonoBehaviour
{
    [Header("튜토리얼 UI 참조")]
    [SerializeField] private TutorialUI tutorialUI;

    [Header("튜토리얼 완료 참조")]
    [SerializeField] private TutorialComplete tutorialComplete;

    [Header("트랩 팬 참조")]
    [SerializeField] private Trap_Fan trap_Fan;

    void OnEnable()
    {
        if (tutorialUI != null)
            tutorialUI.OnTutorialClosed += ActivateTutorial;
    }

    void OnDisable()
    {
        if (tutorialUI != null)
            tutorialUI.OnTutorialClosed -= ActivateTutorial;
    }

    private void ActivateTutorial()
    {
        TutorialStateManager.MoveTriggered = true;
        Debug.Log("✅ 이동 튜토리얼 UI 닫힘 - 튜토리얼 활성화됨");

    }

    // 외부(DoorSensor 등)에서 호출
    public void CompleteTutorial()
    {
        // 이미 활성화 안되었거나 완료됐으면 무시
        if (!TutorialStateManager.MoveTriggered || TutorialStateManager.MoveCompleted)
            return;

        // 완료 기록
        TutorialStateManager.MoveCompleted = true;

        // ✅ 완료 스티커 표시
        if (tutorialUI != null)
            tutorialUI.ShowCompleteSticker();

        // ✅ 문 열기 등 완료 처리
        if (tutorialComplete != null)
            tutorialComplete.OpenDoor();
            
        Debug.Log("✅ 이동 튜토리얼 완료 - 스티커 표시 및 문 열림");
    }
}