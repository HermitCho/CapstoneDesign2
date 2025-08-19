using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CoinController : MonoBehaviourPun
{
    #region 변수

    [Header("코인 관리")]
    [SerializeField] private int currentCoin = 0;
    
    [Header("점수 관리")]
    private float currentScore = 0f;
    private float scoreMultiplier = 1f;

    private PhotonView photonView;


    private bool isTeddyBearAttached = false;

    #endregion

    #region Unity 생명주기

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    void Start()
    {
        if (!photonView.IsMine) return;
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
        currentScore = 0f;
        scoreMultiplier = 1f;
    }

    #endregion

    #region 코인 관리 메서드

    /// <summary>
    /// 코인 추가
    /// </summary>
    /// <param name="amount">추가할 코인 수</param>
    public void AddCoin(int amount)
    {
        if (!photonView.IsMine) return;

        currentCoin += amount;
        AudioManager.Inst.PlayOneShot("SFX_Game_GetCoin");
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
        if (!photonView.IsMine) return;
        
        // GameManager를 통해 테디베어 점수 증가
        if (GameManager.Instance != null)
        {
            // 테디베어가 부착되어 있는지 확인
            isTeddyBearAttached = GameManager.Instance.IsTeddyBearAttached();
            
            // 기본 점수 (코인 1개당 1점)
            float baseScore = coinAmount;
            
            // 테디베어가 부착되어 있다면 배율 적용
            if (isTeddyBearAttached)
            {
                scoreMultiplier = GameManager.Instance.GetScoreIncreaseRate();
                baseScore *= scoreMultiplier;
            }
            else
            {
                scoreMultiplier = 1f;
            }
            
            // 점수 추가
            AddScore(baseScore);
        }
    }
    
    /// <summary>
    /// 점수 추가
    /// </summary>
    /// <param name="scoreToAdd">추가할 점수</param>
    public void AddScore(float scoreToAdd)
    {
        if (!photonView.IsMine) return;
        
        currentScore += scoreToAdd;
    }
    
    /// <summary>
    /// 점수 차감
    /// </summary>
    /// <param name="scoreToSubtract">차감할 점수</param>
    public void SubtractScore(float scoreToSubtract)
    {
        if (!photonView.IsMine) return;
        
        float amount = Mathf.Abs(scoreToSubtract);
        
        // 현재 점수보다 많이 차감하려는 경우 방지
        if (amount > currentScore)
        {
            currentScore = 0f;
        }
        else
        {
            currentScore -= amount;
        }
    }
    
    /// <summary>
    /// 점수 초기화
    /// </summary>
    public void ResetScore()
    {
        if (!photonView.IsMine) return;
        
        currentScore = 0f;
    }
    
    /// <summary>
    /// 현재 점수 가져오기
    /// </summary>
    /// <returns>현재 점수</returns>
    public float GetCurrentScore()
    {
        return currentScore;
    }
    
    /// <summary>
    /// 현재 점수 배율 가져오기
    /// </summary>
    /// <returns>현재 점수 배율</returns>
    public float GetScoreMultiplier()
    {
        return scoreMultiplier;
    }

    /// <summary>
    /// 코인 차감
    /// </summary>
    /// <param name="amount">차감할 코인 수</param>
    public void SubtractCoin(int amount)
    {
        if (!photonView.IsMine) return;
        
        // 음수 값 방지
        if (amount < 0)
        {
            return;
        }
        
        // 현재 코인보다 많이 차감하려는 경우 방지
        if (amount > currentCoin)
        {
            currentCoin = 0;
        }
        else
        {
            currentCoin -= amount;
        }
        
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
        if (!photonView.IsMine) return;

        currentCoin = 0;

        // HUDPanel에 코인 변경 알림
        NotifyHUDCoinChanged();
    }

    #endregion

    #region UI 알림 메서드

    /// <summary>
    /// HUDPanel에 코인 변경 알림 (이벤트 기반으로 변경)
    /// </summary>
    private void NotifyHUDCoinChanged()
    {
        if (!photonView.IsMine) return;
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


    public bool GetIsTeddyBearAttached()
    {
        return isTeddyBearAttached;
    }

    #endregion
}
