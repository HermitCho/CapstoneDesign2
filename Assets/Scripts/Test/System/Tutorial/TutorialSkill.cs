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

    [Header("스킬 사용 횟수")]
    [SerializeField] private int requiredSkillUses = 3;
    
    private bool isCounting = false;
    private int usedCount = 0;

    void OnEnable()
    {
        if (tutorialUI != null)
        {
            tutorialUI.OnTutorialClosed += BeginCounting;
        }
    }

    void OnDisable()
    {
        if (tutorialUI != null)
        {
            tutorialUI.OnTutorialClosed -= BeginCounting;
        }
        SkillController.OnLocalSkillUsed -= OnLocalSkillUsed;
        isCounting = false;
    }

    private void BeginCounting()
    {
        usedCount = 0;
        isCounting = true;
        SkillController.OnLocalSkillUsed += OnLocalSkillUsed;
    }

    private void OnLocalSkillUsed()
    {
        if (!isCounting) return;
        usedCount++;
        if (usedCount >= requiredSkillUses)
        {
            CompleteTutorial();
        }
    }

    private void CompleteTutorial()
    {
        isCounting = false;
        SkillController.OnLocalSkillUsed -= OnLocalSkillUsed;

        if (tutorialUI != null)
        {
            tutorialUI.ShowCompleteSticker();
        }
        if (tutorialComplete != null)
        {
            tutorialComplete.OpenDoor();
        }
    }
}

