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

    private bool isCounting = false;
    private bool hasCrownAttached = false;
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
        Crown.OnLocalCrownAttached -= OnCrownAttached;
        CoinController.LocalCoinChanged -= OnCoinChanged;
        isCounting = false;
    }

    private void BeginCounting()
    {
        hasCrownAttached = false;
        isCounting = true;
        
        // 왕관 부착 이벤트 구독
        Crown.OnLocalCrownAttached += OnCrownAttached;
    }

    private void OnCrownAttached()
    {
        if (!isCounting) return;
        
        hasCrownAttached = true;
        startCoin = GetLocalCoinSafe();
        
        // 코인 변화 추적 시작
        CoinController.LocalCoinChanged += OnCoinChanged;
    }

    private void OnCoinChanged(int currentCoin)
    {
        if (!isCounting || !hasCrownAttached) return;

        int gained = currentCoin - startCoin;
        if (gained >= requiredCoinCount)
        {
            CompleteTutorial();
        }
    }

    private int GetLocalCoinSafe()
    {
        var coinController = FindObjectOfType<CoinController>();
        return coinController != null ? coinController.GetCurrentCoin() : 0;
    }

    private void CompleteTutorial()
    {
        isCounting = false;
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
