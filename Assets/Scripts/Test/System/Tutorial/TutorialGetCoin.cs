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
    [SerializeField] private int requiredCoinCount = 5;

    private bool isCounting = false;
    private int startCoin = 0;

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
        // 튜토리얼 패널을 닫은 시점부터 코인 카운트 시작
        isCounting = true;
        startCoin = GetLocalCoinSafe();
        CoinController.LocalCoinChanged += OnLocalCoinChanged;
    }

    private void OnLocalCoinChanged(int currentCoin)
    {
        if (!isCounting) return;

        int gained = currentCoin - startCoin;
        if (gained >= requiredCoinCount)
        {
            isCounting = false;
            CoinController.LocalCoinChanged -= OnLocalCoinChanged;

            // 완료 스티커 표시 (있을 때만)
            if (tutorialUI != null)
            {
                tutorialUI.ShowCompleteSticker();
            }

            // 다음 문 열기
            if (tutorialComplete != null)
            {
                tutorialComplete.OpenDoor();
            }
        }
    }

    private int GetLocalCoinSafe()
    {
        var coinController = FindObjectOfType<CoinController>();
        return coinController != null ? coinController.GetCurrentCoin() : 0;
    }
}
