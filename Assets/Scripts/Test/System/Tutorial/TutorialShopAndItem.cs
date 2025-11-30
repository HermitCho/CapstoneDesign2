using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialShopAndItem : MonoBehaviour
{
    [Header("튜토리얼 UI 참조")]
    [SerializeField] private TutorialUI tutorialUI;
    

    [Header("튜토리얼 완료 참조")]
    [SerializeField] private TutorialComplete tutorialComplete;
    

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
        ShopController.OnLocalItemPurchased -= OnItemPurchased;
        SkillController.OnLocalItemUsed -= OnItemUsed;
        
    }

    private void BeginCounting()
    {
        TutorialStateManager.ItemTriggered = true;
        TutorialStateManager.ItemCompleted = false;
        TutorialStateManager.ItemPurchased = false;
        TutorialStateManager.ItemUsed = false;

        ShopController.OnLocalItemPurchased += OnItemPurchased;
        SkillController.OnLocalItemUsed += OnItemUsed;
    }

    private void OnItemPurchased()
    {
        if (!TutorialStateManager.ItemTriggered ||
            TutorialStateManager.ItemCompleted)
            return;

        TutorialStateManager.ItemPurchased = true;
        CheckCompletion();
    }

    private void OnItemUsed()
    {
        if (!TutorialStateManager.ItemTriggered ||
            TutorialStateManager.ItemCompleted)
            return;

        TutorialStateManager.ItemUsed = true;
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (TutorialStateManager.ItemPurchased &&
            TutorialStateManager.ItemUsed)
        {
            CompleteTutorial();
        }
    }

    private void CompleteTutorial()
    {
        TutorialStateManager.ItemCompleted = true;

        ShopController.OnLocalItemPurchased -= OnItemPurchased;
        SkillController.OnLocalItemUsed -= OnItemUsed;

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
