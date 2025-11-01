using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialSkill : MonoBehaviour
{
    [Header("튜토리얼 UI 참조")]
    [SerializeField] private TutorialUI tutorialUI;

    [Header("튜토리얼 완료 참조")]
    [SerializeField] private TutorialComplete tutorialComplete;

    private bool isActive = false;
    private bool isCompleted = false;

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
        isActive = true;
        Debug.Log("✅ 튜토리얼 UI 닫힘 - 튜토리얼 활성화됨");
    }

    // 외부(예: DoorSensor나 Trigger 등)에서 튜토리얼 완료를 알릴 때 호출
    public void CompleteTutorial()
    {
        if (!isActive || isCompleted) return;
        isCompleted = true;

        // ✅ 완료 스티커 표시
        if (tutorialUI != null)
            tutorialUI.ShowCompleteSticker();

        // ✅ 문 열기 등 실제 완료 처리
        if (tutorialComplete != null)
            tutorialComplete.OpenDoor();

        Debug.Log("✅ 튜토리얼 완료 - 스티커 표시 및 문 열림");
    }
}