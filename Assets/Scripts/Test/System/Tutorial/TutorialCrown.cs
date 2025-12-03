using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialCrown : MonoBehaviour
{
    [Header("튜토리얼 UI 참조")]
    [SerializeField] private TutorialUI tutorialUI;
    [Space(10)]

    [Header("튜토리얼 완료 참조")]
    [SerializeField] private TutorialComplete tutorialComplete;
    [Space(10)]

    [Header("왕관 착용 후 요구 코인 수")]
    [SerializeField] private int requiredCoinCount = 2;


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
        Crown.OnLocalCrownAttached -= OnCrownAttached;
        CoinController.LocalCoinChanged -= OnCoinChanged;
    }

    private void BeginCounting()
    {
        TutorialStateManager.CrownTriggered = true;
        TutorialStateManager.CrownCompleted = false;

        TutorialStateManager.CrownAttached = false;
        TutorialStateManager.CrownStartCoin = 0;

        // 왕관 착용 이벤트 구독
        Crown.OnLocalCrownAttached += OnCrownAttached;
    }

    private void OnCrownAttached()
    {
        if (!TutorialStateManager.CrownTriggered ||
            TutorialStateManager.CrownCompleted)
            return;

        TutorialStateManager.CrownAttached = true;

        // 기준 코인 저장
        TutorialStateManager.CrownStartCoin = GetLocalCoinSafe();

        // 코인 체크 시작
        CoinController.LocalCoinChanged += OnCoinChanged;
    }

    private void OnCoinChanged(int currentCoin)
    {
        if (!TutorialStateManager.CrownTriggered ||
            TutorialStateManager.CrownCompleted ||
            !TutorialStateManager.CrownAttached)
            return;

        int gained = currentCoin - TutorialStateManager.CrownStartCoin;

        Debug.Log($"👑 코인 획득량: {gained}/{requiredCoinCount}");

        if (gained >= 10)
            CompleteTutorial();
    }

    private int GetLocalCoinSafe()
    {
        var coinController = FindObjectOfType<CoinController>();
        return coinController != null ? coinController.GetCurrentCoin() : 0;
    }

    private void CompleteTutorial()
    {
        TutorialStateManager.CrownCompleted = true;

        Crown.OnLocalCrownAttached -= OnCrownAttached;
        CoinController.LocalCoinChanged -= OnCoinChanged;

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
