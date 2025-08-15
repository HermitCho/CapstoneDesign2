using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinController : MonoBehaviour
{
    #region 변수

    [Header("코인 관리")]
    [SerializeField] private int currentCoin = 0;
    
    #endregion

    #region Unity 생명주기

    void Start()
    {
        InitializeCoin();
    }

    #endregion

    #region 초기화

    /// <summary>
    /// 코인 초기화
    /// </summary>
    private void InitializeCoin()
    {
        currentCoin = 0;
        Debug.Log("✅ CoinController - 코인 초기화 완료");
    }

    #endregion

    #region 코인 관리 메서드

    /// <summary>
    /// 코인 추가
    /// </summary>
    /// <param name="amount">추가할 코인 수</param>
    public void AddCoin(int amount)
    {
        currentCoin += amount;
        
        // 테디베어 점수도 함께 증가
        AddTeddyBearScore(amount);
        
        // HUDPanel에 코인 변경 알림
        NotifyHUDCoinChanged();
    }

    /// <summary>
    /// 코인 수량에 따른 테디베어 점수 추가
    /// </summary>
    /// <param name="coinAmount">획득한 코인 수량</param>
    private void AddTeddyBearScore(int coinAmount)
    {
        // GameManager를 통해 테디베어 점수 증가
        if (GameManager.Instance != null)
        {
            // 테디베어가 부착되어 있는지 확인
            bool isTeddyBearAttached = GameManager.Instance.IsTeddyBearAttached();
            
            // 기본 점수 (코인 1개당 1점)
            float baseScore = coinAmount;
            
            // 테디베어가 부착되어 있다면 배율 적용
            if (isTeddyBearAttached)
            {
                float multiplier = GameManager.Instance.GetScoreMultiplier();
                baseScore *= multiplier;
                Debug.Log($"✅ CoinController - 테디베어 부착 상태에서 코인 {coinAmount}개 획득! 점수: {baseScore} (배율: {multiplier})");
            }
            else
            {
                Debug.Log($"✅ CoinController - 테디베어 미부착 상태에서 코인 {coinAmount}개 획득! 점수: {baseScore}");
            }
            
            // GameManager를 통해 테디베어 점수 업데이트
            GameManager.Instance.AddTeddyBearScore(baseScore);
        }
    }

    /// <summary>
    /// 코인 차감
    /// </summary>
    /// <param name="amount">차감할 코인 수</param>
    public void SubtractCoin(int amount)
    {
        currentCoin -= amount;
        // HUDPanel에 코인 변경 알림
        NotifyHUDCoinChanged();
    }

    /// <summary>
    /// 현재 코인 수 가져오기
    /// </summary>
    /// <returns>현재 코인 수</returns>
    public int GetCoin()
    {
        return currentCoin;
    }

    /// <summary>
    /// 코인 초기화
    /// </summary>
    public void ResetCoin()
    {
        currentCoin = 0;

        // HUDPanel에 코인 변경 알림
        NotifyHUDCoinChanged();
    }

    #endregion

    #region UI 알림 메서드

    /// <summary>
    /// HUDPanel에 코인 변경 알림
    /// </summary>
    private void NotifyHUDCoinChanged()
    {
        // HUD 패널이 비활성화되어 있어도 코인 업데이트를 위해 강제로 찾기
        HUDPanel hudPanel = FindObjectOfType<HUDPanel>();
        if (hudPanel != null)
        {
            hudPanel.UpdateCoin(currentCoin);
        }
    }

    #endregion

    #region 공개 메서드

    /// <summary>
    /// 현재 코인 수 가져오기 (HUD 패널용)
    /// </summary>
    /// <returns>현재 코인 수</returns>
    public int GetCurrentCoin()
    {
        return currentCoin;
    }

    #endregion
}
