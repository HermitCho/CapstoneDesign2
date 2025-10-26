using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialShopAndItem : MonoBehaviour
{
    [Header("튜토리얼 UI 참조")]
    [SerializeField] private TutorialUI tutorialUI;
    [Space(10)]

    [Header("튜토리얼 완료 참조")]
    [SerializeField] private TutorialComplete tutorialComplete;
    [Space(10)]
    
    private bool isCounting = false;
    private bool hasPurchased = false;
    private bool hasUsedItem = false;

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
        isCounting = false;
    }

    private void BeginCounting()
    {
        hasPurchased = false;
        hasUsedItem = false;
        isCounting = true;
        
        ShopController.OnLocalItemPurchased += OnItemPurchased;
        SkillController.OnLocalItemUsed += OnItemUsed;
    }

    private void OnItemPurchased()
    {
        if (!isCounting) return;
        hasPurchased = true;
        CheckCompletion();
    }

    private void OnItemUsed()
    {
        if (!isCounting) return;
        hasUsedItem = true;
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (hasPurchased && hasUsedItem)
        {
            CompleteTutorial();
        }
    }

    private void CompleteTutorial()
    {
        isCounting = false;
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
