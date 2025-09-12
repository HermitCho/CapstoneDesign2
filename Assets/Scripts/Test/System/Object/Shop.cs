using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// 🏪 상점 오브젝트 - 씬의 UI 버튼과 플레이어의 ShopController를 연결하는 중계자
/// 씬에 고정되어 있어서 UI 버튼에 직접 연결 가능
/// </summary>
public class Shop : MonoBehaviour
{

    
    // 현재 상점을 이용중인 플레이어의 ShopController
    private ShopController currentPlayerShopController;
    
    #region Unity 생명주기
    
    void Update()
    {
        // 현재 연결된 ShopController가 유효한지 체크
        ValidateCurrentShopController();
    }
    
    #endregion
    
    #region ShopController 연결 관리
    
    /// <summary>
    /// 플레이어의 ShopController와 연결
    /// </summary>
    /// <param name="shopController">연결할 ShopController</param>
    public void ConnectShopController(ShopController shopController)
    {
        if (shopController == null) return;
        
        // 로컬 플레이어인지 확인
        PhotonView pv = shopController.GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine) return;
        
        // 기존 연결 해제
        DisconnectShopController();
        
        // 새로운 연결
        currentPlayerShopController = shopController;
    }
    
    /// <summary>
    /// 현재 ShopController와의 연결 해제
    /// </summary>
    public void DisconnectShopController()
    {
        if (currentPlayerShopController != null)
        {       
            currentPlayerShopController = null;
        }
    }
    
    /// <summary>
    /// 현재 연결된 ShopController가 유효한지 검증
    /// </summary>
    void ValidateCurrentShopController()
    {
        if (currentPlayerShopController != null)
        {
            // ShopController가 파괴되었거나 상점이 닫혔다면 연결 해제
            if (currentPlayerShopController == null || !currentPlayerShopController.IsShopOpen())
            {
                DisconnectShopController();
            }
        }
    }
    
    #endregion
    
    #region 아이템 구매 처리 (UI 버튼에서 호출)
    
    /// <summary>
    /// 아이템 구매 요청 - UI 버튼의 onClick 이벤트에서 호출
    /// </summary>
    /// <param name="itemIndex">구매할 아이템 인덱스</param>
    public void PurchaseItem(int itemIndex)
    {
        // ShopPanel에서 직접 구매 처리하도록 변경
        ShopPanel shopPanel = FindObjectOfType<ShopPanel>();
        if (shopPanel != null)
        {
            shopPanel.OnPurchaseItem(itemIndex);
        }
    }
    
    #endregion
    
    #region 상점 상태 확인
    
    /// <summary>
    /// 현재 연결된 ShopController 가져오기
    /// </summary>
    /// <returns>현재 ShopController</returns>
    public ShopController GetCurrentShopController()
    {
        return currentPlayerShopController;
    }
    
    /// <summary>
    /// ShopController가 연결되어 있는지 확인
    /// </summary>
    /// <returns>연결 여부</returns>
    public bool IsConnected()
    {
        return currentPlayerShopController != null;
    }
    
    #endregion
}
