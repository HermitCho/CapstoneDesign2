using System;
using System.Collections;
using UnityEngine;
using Michsky.UI.Heat;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class ShopController : MonoBehaviourPun
{
    #region 변수

    [Header("상점 설정")]
    [SerializeField] private string shopPanelName = "Shop";
    [SerializeField] private bool isShopOpen = false;

    [Header("UI 컴포넌트")]
    private InGameUIManager inGameUIManager;
    private Shop shopObject;

    private PhotonView photonView;

    #endregion

    #region 내부 상태 변수
    private Collider shopCollider;
    private CoinController playerCoinController;
    private ItemController playerItemController;
    #endregion

    #region 데이터베이스 참조
    private DataBase.ItemData itemData;
    #endregion

    #region 캐싱된 값들 (성능 최적화)
    private GameObject[] cachedItemData;
    private bool dataBaseCached = false;
    #endregion


    #region Unity 생명주기

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }
    void Start()
    {
        if (!photonView.IsMine) return;

        InitializeShopController();
        CacheDataBaseInfo();
    }

    #endregion

    #region 초기화 메서드

    /// <summary>
    /// 상점 컨트롤러 초기화
    /// </summary>
    void InitializeShopController()
    {
        shopCollider = GetComponent<Collider>();
        if (inGameUIManager == null)
        {
            inGameUIManager = FindObjectOfType<InGameUIManager>();
        }
        
        // Shop 오브젝트 찾기
        if (shopObject == null)
        {
            shopObject = FindObjectOfType<Shop>();
        }
        
        Debug.Log("✅ ShopController - 초기화 완료");
    }

    /// <summary>
    /// DataBase 정보 캐싱
    /// </summary>
    void CacheDataBaseInfo()
    {
        try
        {
            if (!dataBaseCached && DataBase.Instance != null)
            {
                itemData = DataBase.Instance.itemData;
                cachedItemData = itemData.ItemPrefabData.ToArray();
                dataBaseCached = true;
                Debug.Log("✅ ShopController - DataBase 정보 캐싱 완료");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ShopController: DataBase 캐싱 중 오류: {e.Message}");
            dataBaseCached = false;
        }
    }

    #endregion

    #region 트리거 이벤트

    /// <summary>
    /// 플레이어가 상점에 진입
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;

        if (other.CompareTag("Shop"))
        {
            OpenShop();
        }
    }

    /// <summary>
    /// 플레이어가 상점에서 나감
    /// </summary>
    void OnTriggerExit(Collider other)
    {
        if (!photonView.IsMine) return;

        if (other.CompareTag("Shop"))
        {
            CloseShop();
        }
    }

    #endregion

    #region 상점 열기/닫기

    /// <summary>
    /// 상점 열기
    /// </summary>
    /// <param name="Shop">상점 오브젝트</param>
    void OpenShop()
    {
        if (!photonView.IsMine) return;

        isShopOpen = true;
        
        // 플레이어 컴포넌트 찾기
        playerItemController = GetComponent<ItemController>();
        playerCoinController = GetComponent<CoinController>();
        
        // Shop 오브젝트와 연결
        if (shopObject != null)
        {
            shopObject.ConnectShopController(this);
        }
        
        // 상점 패널 표시
        if (inGameUIManager != null)
        {
            inGameUIManager.ShowShopPanel();
            AudioManager.Inst.PlayOneShot("SFX_UI_OpenShop");
        }
        
        // 게임 입력 차단
        DisableGameInput();
    }

    /// <summary>
    /// 상점 닫기
    /// </summary>
    void CloseShop()
    {
        if (!photonView.IsMine) return;

        if (isShopOpen)
        {
            isShopOpen = false;
            EnableGameInput();
            
            // Shop 오브젝트와의 연결 해제
            if (shopObject != null)
            {
                shopObject.DisconnectShopController();
            }

            playerCoinController = null;
            playerItemController = null;
                
            if (inGameUIManager != null)
            {
                inGameUIManager.ShowHUDPanel();
                AudioManager.Inst.PlayOneShot("SFX_UI_CloseShop");
            }
        }
    }

    #endregion

    #region 아이템 구매 시스템

    /// <summary>
    /// 아이템 구매 처리 (HeatUI ShopButtonManager의 onClick 이벤트에서 호출)
    /// </summary>
    /// <param name="itemIndex">구매할 아이템 인덱스</param>
    public void PurchaseItem(int itemIndex)
    {
        if (!photonView.IsMine) return;

        if (!isShopOpen || playerCoinController == null || playerItemController == null)
        {
            return;
        }

        if (!dataBaseCached || itemIndex < 0 || itemIndex >= cachedItemData.Length)
        {
            return;
        }

        // ✅ 중복 아이템 체크 (SkillName으로 비교)
        CharacterItem itemComponent = cachedItemData[itemIndex].GetComponent<CharacterItem>();
        if (itemComponent != null && !string.IsNullOrEmpty(itemComponent.SkillName))
        {
            string skillName = itemComponent.SkillName;
            if (playerItemController.HasItemBySkillName(skillName))
            {
                Debug.LogWarning($"⚠️ ShopController - 이미 보유하고 있는 아이템입니다: {skillName}");
                return;
            }
        }

        // 아이템 슬롯 확인
        if (playerItemController.GetItemSlotIndex() >= playerItemController.GetMaxItemSlot())
        {
            Debug.LogWarning("⚠️ ShopController - 아이템 슬롯이 가득 찼습니다.");
            return;
        }

        // 아이템 가격 확인 (itemComponent는 이미 위에서 가져왔으므로 재사용)
        if (itemComponent == null)
        {
            Debug.LogError($"❌ ShopController - 아이템 인덱스 {itemIndex}에 CharacterItem 컴포넌트가 없습니다.");
            return;
        }

        int itemPrice = itemComponent.GetPrice();
        int playerCoins = playerCoinController.GetCoin();

        // 코인 확인
        if (playerCoins < itemPrice)
        {
            Debug.LogWarning($"❌ ShopController - 코인이 부족합니다. 필요: {itemPrice}, 보유: {playerCoins}");
            return;
        }

        // 구매 처리
        try
        {
            // 코인 차감
            playerCoinController.SubtractCoin(itemPrice);
            
            // 아이템 스킬 적용
            itemComponent.PurchaseItemSkill();
            
            // 아이템 슬롯에 추가 (프리팹 인스턴스화)
            if (cachedItemData[itemIndex] != null)
            {
                playerItemController.AttachItem(cachedItemData[itemIndex]);
            }
            else
            {
                Debug.LogError($"❌ ShopController - 아이템 인덱스 {itemIndex}의 프리팹이 null입니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ShopController - 아이템 구매 중 오류: {e.Message}");
        }
    }

    #endregion

    #region 게임 입력 제어

    /// <summary>
    /// 게임 입력 차단 (상점 열림 시)
    /// </summary>
    void DisableGameInput()
    {
        TestShoot.SetIsShooting(false);
    }

    /// <summary>
    /// 게임 입력 복원 (상점 닫힘 시)
    /// </summary>
    void EnableGameInput()
    {
        TestShoot.SetIsShooting(true);
    }

    #endregion

    #region 공개 메서드

    /// <summary>
    /// 상점이 열려있는지 확인
    /// </summary>
    /// <returns>상점 열림 여부</returns>
    public bool IsShopOpen()
    {
        return isShopOpen;
    }

    /// <summary>
    /// 플레이어의 현재 코인 수 가져오기
    /// </summary>
    /// <returns>현재 코인 수</returns>
    public int GetPlayerCoins()
    {       
        return playerCoinController != null ? playerCoinController.GetCoin() : 0;
    }

    /// <summary>
    /// 아이템 가격 가져오기
    /// </summary>
    /// <param name="itemIndex">아이템 인덱스</param>
    /// <returns>아이템 가격</returns>
    public int GetItemPrice(int itemIndex)
    {
        if (!dataBaseCached || itemIndex < 0 || itemIndex >= cachedItemData.Length)
        {
            return 0;
        }

        CharacterItem itemComponent = cachedItemData[itemIndex].GetComponent<CharacterItem>();
        return itemComponent != null ? itemComponent.GetPrice() : 0;
    }

    #endregion
}
