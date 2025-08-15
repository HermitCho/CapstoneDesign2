using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

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
        if (!PhotonView.Get(this).IsMine) return;
        PhotonView.Get(this).RPC("RPC_AddCoin", RpcTarget.All, amount);
    }

    [PunRPC]
    public void RPC_AddCoin(int amount)
    {
        currentCoin += amount;
        NotifyHUDCoinChanged();
    }

    /// <summary>
    /// 코인 차감
    /// </summary>
    /// <param name="amount">차감할 코인 수</param>
    public void SubtractCoin(int amount)
    {
        if (!PhotonView.Get(this).IsMine) return;
        PhotonView.Get(this).RPC("RPC_SubtractCoin", RpcTarget.All, amount);
    }

    [PunRPC]
    public void RPC_SubtractCoin(int amount)
    {
        currentCoin -= amount;
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
        if (!PhotonView.Get(this).IsMine) return;
        PhotonView.Get(this).RPC("RPC_ResetCoin", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_ResetCoin()
    {
        currentCoin = 0;
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
