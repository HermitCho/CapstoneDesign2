using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CoinController : MonoBehaviourPun
{
    public static System.Action<int> LocalCoinChanged; // 로컬 코인 수 변경 알림
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

        // 로컬 코인 변경 이벤트 알림
        LocalCoinChanged?.Invoke(currentCoin);
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
            // ✅ 중요: 왕관이 로컬 플레이어 자신에게 부착되어 있는지 확인
            isTeddyBearAttached = IsLocalPlayerHoldingCrown();

            // 기본 점수 (코인 1개당 1점)
            float baseScore = coinAmount/2;

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
    /// 로컬 플레이어가 왕관을 소유하고 있는지 확인
    /// </summary>
    /// <returns>로컬 플레이어가 왕관을 소유 중이면 true, 아니면 false</returns>
    private bool IsLocalPlayerHoldingCrown()
    {
        if (!photonView.IsMine) return false;

        // 씬에서 왕관 찾기
        Crown crown = FindObjectOfType<Crown>();
        if (crown == null) return false;

        // 왕관이 현재 플레이어의 Transform에 부착되어 있는지 확인
        return crown.IsAttachedToPlayer(transform);
    }

    /// <summary>
    /// 점수 추가
    /// </summary>
    /// <param name="scoreToAdd">추가할 점수</param>
    public void AddScore(float scoreToAdd)
    {
        if (!photonView.IsMine) return;

        currentScore += scoreToAdd;

        // 네트워크로 점수 동기화
        SyncScoreToNetwork();
    }

    #region 킬 점수 부여 메서드

    /// <summary>
    /// 킬 점수를 부여하는 RPC. 마스터 클라이언트에서 호출되어 공격자 소유자에게 전달됩니다.
    /// </summary>
    /// <param name="score">부여할 점수</param>
    [PunRPC]
    public void RPC_GrantKillScore(float score)
    {
        if (!photonView.IsMine) return;

        AddScore(score);
    }

    #endregion

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

        // 네트워크로 점수 동기화
        SyncScoreToNetwork();
    }

    /// <summary>
    /// 점수 초기화
    /// </summary>
    public void ResetScore()
    {
        if (!photonView.IsMine) return;

        currentScore = 0f;

        // 네트워크로 점수 동기화
        SyncScoreToNetwork();
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

        // 로컬 코인 변경 이벤트 알림
        LocalCoinChanged?.Invoke(currentCoin);
    }

    /// <summary>
    /// 현재 코인 수 가져오기
    /// </summary>
    /// <returns>현재 코인 수 (다른 플레이어인 경우 -1 반환)</returns>
    public int GetCoin()
    {
        // PhotonView가 없는 경우 (싱글플레이) 또는 로컬 플레이어인 경우
        if (photonView == null || photonView.IsMine)
        {
            return currentCoin;
        }

        return -1; // 다른 플레이어의 코인은 접근 불가
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

        // 로컬 코인 변경 이벤트 알림
        LocalCoinChanged?.Invoke(currentCoin);
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

    /// <summary>
    /// 점수를 네트워크로 동기화
    /// </summary>
    private void SyncScoreToNetwork()
    {
        if (!photonView.IsMine || !PhotonNetwork.IsConnected) return;

        try
        {
            // Photon Custom Properties에 점수 저장
            var props = new ExitGames.Client.Photon.Hashtable();
            string scoreKey = $"score_{PhotonNetwork.LocalPlayer.ActorNumber}";
            props[scoreKey] = currentScore;

            // 닉네임도 함께 동기화 (처음 한 번만)
            if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("nickname"))
            {
                string nickname = PlayerPrefs.GetString("NickName", $"Player{PhotonNetwork.LocalPlayer.ActorNumber}");
                props["nickname"] = nickname;
            }

            PhotonNetwork.LocalPlayer.SetCustomProperties(props);



            // 추가 검증: 설정된 값 확인
            StartCoroutine(VerifyNetworkSync(scoreKey, currentScore));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CoinController: 점수 네트워크 동기화 실패 - {e.Message}");
        }
    }

    /// <summary>
    /// 네트워크 동기화 검증
    /// </summary>
    private System.Collections.IEnumerator VerifyNetworkSync(string scoreKey, float expectedScore)
    {
        yield return new WaitForSeconds(0.2f); // 동기화 대기 시간 증가

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(scoreKey, out object syncedScore))
        {
            float syncedScoreFloat = float.Parse(syncedScore.ToString());
            if (Mathf.Abs(syncedScoreFloat - expectedScore) > 0.01f)
            {
                Debug.LogWarning($"CoinController: 점수 동기화 불일치 - 예상: {expectedScore}, 실제: {syncedScoreFloat}");

                // 강제로 다시 설정
                var props = new ExitGames.Client.Photon.Hashtable();
                props[scoreKey] = expectedScore;
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            }

        }
        else
        {
            Debug.LogWarning($"CoinController: 점수 동기화 실패 - {scoreKey} 키를 찾을 수 없음");

            // 강제로 설정
            var props = new ExitGames.Client.Photon.Hashtable();
            props[scoreKey] = expectedScore;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    #endregion
}
