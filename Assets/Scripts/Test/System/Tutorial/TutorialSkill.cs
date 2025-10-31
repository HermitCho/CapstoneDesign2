using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialSkill : MonoBehaviour
{
    [Header("튜토리얼 UI 참조")]
    [SerializeField] private TutorialUI tutorialUI;
    [Space(10)]

    [Header("튜토리얼 완료 참조")]
    [SerializeField] private TutorialComplete tutorialComplete;
    [Space(10)]

    [Header("스킬 목표 트리거")]
    [SerializeField] private Collider skillGoalTrigger; // ✅ 스킬 목표 구역

    private bool isActive = false;
    private bool isCompleted = false;

    void OnEnable()
    {
        if (tutorialUI != null)
            tutorialUI.OnTutorialClosed += ActivateTrigger;
    }

    void OnDisable()
    {
        if (tutorialUI != null)
            tutorialUI.OnTutorialClosed -= ActivateTrigger;
    }

    private void ActivateTrigger()
    {
        isActive = true;
        if (skillGoalTrigger != null)
            skillGoalTrigger.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || isCompleted) return;
        if (!other.CompareTag("Player")) return;

        // ✅ 플레이어가 SkillGoal 트리거에 닿으면 미션 완료
        CompleteTutorial();
    }

    private void CompleteTutorial()
    {
        isCompleted = true;
        isActive = false;

        if (tutorialUI != null)
            tutorialUI.ShowCompleteSticker();

        if (tutorialComplete != null)
            tutorialComplete.OpenDoor();

        Debug.Log("✅ 스킬 튜토리얼 완료 (SkillGoal 트리거 도달)");
    }
}