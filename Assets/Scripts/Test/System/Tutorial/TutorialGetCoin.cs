using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialGetCoin : MonoBehaviour
{
    [Header("튜토리얼 UI 참조")]
    [SerializeField] private TutorialUI tutorialUI;
    [Space(10)]

    [Header("튜토리얼 완료 참조")]
    [SerializeField] private TutorialComplete tutorialComplete;
    [Space(10)]

    [Header("획득해야 하는 코인 수")]
    [SerializeField] private int requiredCoinCount = 8;

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
        CoinController.LocalCoinChanged -= OnLocalCoinChanged;
    }

    private void BeginCounting()
    {
        TutorialStateManager.CoinTriggered = true;
        TutorialStateManager.CoinCompleted = false;
        TutorialStateManager.CoinGained = 0;

        CoinController.LocalCoinChanged += OnLocalCoinChanged;
    }

    private void OnLocalCoinChanged(int currentCoin)
    {
        if (!TutorialStateManager.CoinTriggered ||
            TutorialStateManager.CoinCompleted)
            return;

        // 첫 갱신이라면 기준점 기록
        if (TutorialStateManager.CoinGained == 0)
            TutorialStateManager.CoinGained = currentCoin;

        int gained = currentCoin - TutorialStateManager.CoinGained;

        Debug.Log($"💰 코인 획득량 계산됨: {gained}/{requiredCoinCount}");

        if (gained >= 8)
        {
            CompleteTutorial();
        }
    }

    private void CompleteTutorial()
    {
        TutorialStateManager.CoinCompleted = true;
        CoinController.LocalCoinChanged -= OnLocalCoinChanged;

        Debug.Log("✅ 코인 튜토리얼 완료!");

        if (tutorialUI != null)
            tutorialUI.ShowCompleteSticker();

        if (tutorialComplete != null)
            tutorialComplete.OpenDoor();
    }
}
