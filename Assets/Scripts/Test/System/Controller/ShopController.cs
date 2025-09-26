using System;
using System.Collections;
using UnityEngine;
using Photon.Pun;

public class ShopController : MonoBehaviourPun
{
    #region 변수

    [Header("상점 설정")]
    [SerializeField] private bool isShopOpen = false;
    [SerializeField] private float raycastDistance = 10f;
    [SerializeField] private LayerMask itemLayerMask = -1;
    
    [Header("구매 설정")]
    [SerializeField] private float purchaseHoldTime = 1f;

    private Shop shopObject;
    private PhotonView photonView;
    
    // 시선 추적 관련
    private Camera playerCamera;
    private ShopStand currentLookingShopStand;
    private float purchaseHoldTimer = 0f;
    private bool isPurchaseHolding = false;

    #endregion

    #region 내부 상태 변수
    private CoinController playerCoinController;
    private ItemController playerItemController;
    private MoveController moveController;
    private CameraController cameraController;
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
    }
    
    void Update()
    {
        if (!photonView.IsMine) return;
        
        if (isShopOpen)
        {
            HandleItemLooking();
            HandlePurchaseInput();
        }
    }

    #endregion

    #region 초기화 메서드

    /// <summary>
    /// 상점 컨트롤러 초기화
    /// </summary>
    void InitializeShopController()
    {
        // 필요한 컴포넌트들 찾기
        playerCoinController = GetComponent<CoinController>();
        playerItemController = GetComponent<ItemController>();
        moveController = GetComponent<MoveController>();
        cameraController = GetComponent<CameraController>();
        
        // 플레이어 카메라 찾기
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Shop 오브젝트 찾기
        if (shopObject == null)
        {
            shopObject = FindObjectOfType<Shop>();
        }
        
        // 입력 이벤트 구독
        InputManager.OnShootPressed += OnShootPressed;
        InputManager.OnShootCanceledPressed += OnShootCanceled;

        Debug.Log("ShopController - 초기화 완료");
    }
    
    void OnDestroy()
    {
        // 입력 이벤트 구독 해제
        InputManager.OnShootPressed -= OnShootPressed;
        InputManager.OnShootCanceledPressed -= OnShootCanceled;
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
    void OpenShop()
    {
        if (!photonView.IsMine) return;

        isShopOpen = true;

        // Shop 오브젝트와 연결
        if (shopObject != null)
        {
            shopObject.ConnectShopController(this);
        }

        // 게임 입력 차단
        DisableGameInput();
        
        // 오디오 재생
        if (AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayOneShot("SFX_UI_OpenShop");
        }
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
            
            // 현재 보고 있던 상점 스탠드 정리
            if (currentLookingShopStand != null)
            {
                currentLookingShopStand.OnPlayerStopLooking(this);
                currentLookingShopStand = null;
            }
            
            // 구매 상태 초기화
            isPurchaseHolding = false;
            purchaseHoldTimer = 0f;

            // Shop 오브젝트와의 연결 해제
            if (shopObject != null)
            {
                shopObject.DisconnectShopController(this);
            }

            // 게임 입력 복원
            EnableGameInput();
            
            // 오디오 재생
            if (AudioManager.Inst != null)
            {
                AudioManager.Inst.PlayOneShot("SFX_UI_CloseShop");
            }

            Debug.Log("ShopController: 상점 퇴장");
        }
    }

    #endregion

    #region 시선 추적 및 구매 시스템

    /// <summary>
    /// 상점 스탠드 시선 추적 처리
    /// </summary>
    void HandleItemLooking()
    {
        if (playerCamera == null) return;

        ShopStand hitShopStand = CalculateShopStandDirection();

        // 이전에 보던 상점 스탠드와 다른 경우
        if (currentLookingShopStand != hitShopStand)
        {
            // 이전 상점 스탠드 정리
            if (currentLookingShopStand != null)
            {
                currentLookingShopStand.OnPlayerStopLooking(this);
            }

            // 새 상점 스탠드 설정
            currentLookingShopStand = hitShopStand;
            
            if (currentLookingShopStand != null)
            {
                currentLookingShopStand.OnPlayerStartLooking(this);
            }
            
            // 구매 상태 초기화
            isPurchaseHolding = false;
            purchaseHoldTimer = 0f;
        }
    }

    /// <summary>
    /// TestShoot의 CalculateShotDirection 방식으로 ShopStand 찾기
    /// </summary>
    /// <returns>히트된 ShopStand, 없으면 null</returns>
    private ShopStand CalculateShopStandDirection()
    {
        // 화면 중앙에서 카메라 레이캐스트
        Ray cameraRay = playerCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));
        RaycastHit hit;
        int layerMask = ~LayerMask.GetMask("PlayerPosition", "Shop");

        Vector3 targetPosition;

        // 카메라에서 레이캐스트로 목표 지점 찾기
        if (Physics.Raycast(cameraRay, out hit, raycastDistance, layerMask))
        {
            targetPosition = hit.point;
            
            // 히트된 오브젝트에서 ShopStand 찾기
            return ProcessHitObject(hit);
        }
        else
        {
            // 레이캐스트가 히트하지 않으면 카메라 방향으로 최대 거리 지점을 목표로 설정
            targetPosition = cameraRay.origin + cameraRay.direction * raycastDistance;
        }

        // 디버깅용 레이캐스트 시각화
        Debug.DrawRay(cameraRay.origin, cameraRay.direction * raycastDistance, Color.red, 0.1f);
        
        return null;
    }

    /// <summary>
    /// 히트된 오브젝트에서 ShopStand 찾기
    /// </summary>
    /// <param name="hit">히트 정보</param>
    /// <returns>찾은 ShopStand, 없으면 null</returns>
    private ShopStand ProcessHitObject(RaycastHit hit)
    {
        // 1. 직접 ShopStand 컴포넌트가 있는지 확인
        ShopStand shopStand = hit.collider.GetComponent<ShopStand>();
        if (shopStand != null && shopStand.GetCurrentItem() != null)
        {
            return shopStand;
        }

        // 2. 부모에서 ShopStand 찾기
        shopStand = hit.collider.GetComponentInParent<ShopStand>();
        if (shopStand != null && shopStand.GetCurrentItem() != null)
        {
            return shopStand;
        }

        // 3. Item 컴포넌트가 있는지 확인하고 해당 Item이 ShopStand에 속하는지 확인
        Item hitItem = hit.collider.GetComponent<Item>();
        if (hitItem != null)
        {
            // Item의 부모에서 ShopStand 찾기
            shopStand = hit.collider.GetComponentInParent<ShopStand>();
            if (shopStand != null && shopStand.GetCurrentItem() == hit.collider.gameObject)
            {
                return shopStand;
            }
        }

        // 4. itemObject에서 Skill 컴포넌트가 있는지 확인 (Item의 자식 오브젝트)
        if (hitItem != null && hitItem.ItemObject != null)
        {
            // itemObject의 부모에서 ShopStand 찾기
            shopStand = hitItem.ItemObject.GetComponentInParent<ShopStand>();
            if (shopStand != null && shopStand.GetCurrentItem() == hitItem.gameObject)
            {
                return shopStand;
            }
        }

        return null;
    }

    /// <summary>
    /// 구매 입력 처리
    /// </summary>
    void HandlePurchaseInput()
    {
        if (currentLookingShopStand == null) return;

        if (isPurchaseHolding)
        {
            purchaseHoldTimer += Time.deltaTime;
            Debug.Log($"ShopController: 구매 진행 중 - {purchaseHoldTimer:F2}초 / {purchaseHoldTime}초");
            
            // 구매 시간이 충족되면 구매 시도
            if (purchaseHoldTimer >= purchaseHoldTime)
            {
                Debug.Log("ShopController: 구매 시간 충족 - 구매 시도");
                TryPurchaseCurrentItem();
                isPurchaseHolding = false;
                purchaseHoldTimer = 0f;
            }
        }
    }

    /// <summary>
    /// 현재 보고 있는 아이템 구매 시도
    /// </summary>
    void TryPurchaseCurrentItem()
    {
        if (currentLookingShopStand == null) 
        {
            Debug.Log("ShopController: TryPurchaseCurrentItem 실패 - currentLookingShopStand가 null");
            return;
        }

        Debug.Log($"ShopController: {currentLookingShopStand.name}에서 아이템 구매 시도");
        bool purchaseSuccess = currentLookingShopStand.TryPurchaseItem(this);
        
        if (purchaseSuccess)
        {
            Debug.Log("ShopController: 구매 요청 성공");
            // 구매 성공 시 현재 상점 스탠드 참조 해제 (아이템이 파괴될 예정)
            currentLookingShopStand = null;
        }
        else
        {
            Debug.Log("ShopController: 구매 요청 실패");
        }
    }

    /// <summary>
    /// 발사 버튼 눌림 (구매 시작)
    /// </summary>
    void OnShootPressed()
    {
    
        
        if (!isShopOpen)
        {
            Debug.Log("ShopController: 상점이 열려있지 않음");
            return;
        }
        
        if (currentLookingShopStand == null)
        {
            Debug.Log("ShopController: 보고 있는 상점 스탠드가 없음");
            return;
        }

        Debug.Log("ShopController: 구매 홀드 시작");
        isPurchaseHolding = true;
        purchaseHoldTimer = 0f;
    }

    /// <summary>
    /// 발사 버튼 해제 (구매 취소)
    /// </summary>
    void OnShootCanceled()
    {
        if (!isShopOpen) return;

        isPurchaseHolding = false;
        purchaseHoldTimer = 0f;
    }

    #endregion

    #region 게임 입력 제어

    /// <summary>
    /// 게임 입력 차단 (상점 열림 시)
    /// </summary>
    void DisableGameInput()
    {
        // 총 발사 비활성화
        TestShoot.SetIsShooting(false);
        
        // 플레이어 이동 제한 (필요 시)
        if (moveController != null)
        {
            // moveController.DisableMovement(); // 이동을 막고 싶다면 구현
        }
        
        // 아이템/스킬 사용 차단
        // ItemController나 SkillController에서 상점 상태 확인하도록 구현 가능
        
        Debug.Log("ShopController: 게임 입력 차단됨");
    }

    /// <summary>
    /// 게임 입력 복원 (상점 닫힘 시)
    /// </summary>
    void EnableGameInput()
    {
        // 총 발사 활성화
        TestShoot.SetIsShooting(true);
        
        // 플레이어 이동 복원
        if (moveController != null)
        {
            // moveController.EnableMovement(); // 이동 제한을 했다면 복원
        }
        
        Debug.Log("ShopController: 게임 입력 복원됨");
    }

    #endregion

    #region 구매 처리

    /// <summary>
    /// 구매 처리 RPC (Shop.cs에서 호출)
    /// </summary>
    /// <param name="price">아이템 가격</param>
    /// <param name="itemIndex">아이템 인덱스</param>
    /// <param name="itemObjectName">아이템 오브젝트 이름</param>
    /// <param name="positionIndex">상점 위치 인덱스</param>
    [PunRPC]
    void ProcessPurchase(int price, int itemIndex, string itemObjectName, int positionIndex)
    {
        Debug.Log($"ShopController: RPC 구매 처리 시작 - Price: {price}, Index: {itemIndex}, ItemObject: {itemObjectName}");
        
        bool purchaseSuccess = ProcessPurchaseLocal(price, itemIndex, itemObjectName);
        
        if (purchaseSuccess)
        {
            // 구매 성공 시 Shop에게 아이템 제거 요청 (현재 남은 시간과 함께)
            Shop shop = FindObjectOfType<Shop>();
            if (shop != null)
            {
                shop.GetComponent<PhotonView>().RPC("OnItemPurchased", RpcTarget.MasterClient, positionIndex);
                Debug.Log("ShopController: 구매 성공 - Shop에게 아이템 제거 요청");
            }
        }
        else
        {
            Debug.Log("ShopController: 구매 실패 - 조건 불만족");
        }
    }

    /// <summary>
    /// 로컬 구매 처리 (ShopController.cs에서 호출)
    /// </summary>
    /// <param name="price">아이템 가격</param>
    /// <param name="itemIndex">아이템 인덱스</param>
    /// <param name="itemObjectName">아이템 오브젝트 이름</param>
    /// <returns>구매 성공 여부</returns>
    public bool ProcessPurchaseLocal(int price, int itemIndex, string itemObjectName)
    {
        // 컴포넌트 확인
        if (playerCoinController == null || playerItemController == null)
        {
            Debug.LogError("ShopController: 필요한 컴포넌트가 없음");
            return false;
        }
        
        // 구매 조건 재확인 (로컬에서)
        if (playerCoinController.GetCoin() < price)
        {
            Debug.Log($"ShopController: 코인 부족 - 필요: {price}, 보유: {playerCoinController.GetCoin()}");
            return false;
        }
        
        if (playerItemController.HasItemByIndex(itemIndex))
        {
            Debug.Log($"ShopController: 이미 보유한 아이템 - Index: {itemIndex}");
            return false;
        }
        
        if (playerItemController.GetItemSlotIndex() >= playerItemController.GetMaxItemSlot())
        {
            Debug.Log($"ShopController: 아이템 슬롯 부족 - 현재: {playerItemController.GetItemSlotIndex()}, 최대: {playerItemController.GetMaxItemSlot()}");
            return false;
        }
        
        // 구매 처리
        playerCoinController.SubtractCoin(price);
        Debug.Log($"ShopController: 코인 차감 완료 - {price}코인");
        
        // 아이템 오브젝트 찾기 및 부착
        GameObject itemObject = FindItemObjectByName(itemObjectName);
        if (itemObject != null)
        {
            playerItemController.AttachItemObject(itemObject);
            Debug.Log($"ShopController: 아이템 부착 완료 - {itemObjectName}");
            return true;
        }
        else
        {
            Debug.LogError($"ShopController: 아이템 오브젝트를 찾을 수 없음 - {itemObjectName}");
            return false;
        }
    }
    
    /// <summary>
    /// 아이템 오브젝트 이름으로 프리팹 찾기
    /// </summary>
    /// <param name="itemObjectName">찾을 아이템 오브젝트 이름</param>
    /// <returns>아이템 오브젝트 프리팹</returns>
    GameObject FindItemObjectByName(string itemObjectName)
    {
        // Shop 오브젝트에서 itemPrefabs 가져오기
        Shop shop = FindObjectOfType<Shop>();
        if (shop == null)
        {
            Debug.LogError("ShopController: Shop 오브젝트를 찾을 수 없음");
            return null;
        }
        
        // Shop의 itemPrefabs를 직접 접근할 수 없으므로 다른 방법 사용
        // Resources 폴더에서 찾거나 다른 방식으로 구현해야 함
        
        // 임시로 모든 프리팹을 검색하는 방식 사용
        GameObject[] allPrefabs = Resources.LoadAll<GameObject>("");
        foreach (GameObject prefab in allPrefabs)
        {
            if (prefab == null) continue;
            
            Item itemComponent = prefab.GetComponent<Item>();
            if (itemComponent != null && itemComponent.ItemObject != null)
            {
                if (itemComponent.ItemObject.name == itemObjectName)
                {
                    return itemComponent.ItemObject;
                }
            }
        }
        
        return null;
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
    /// 현재 보고 있는 상점 스탠드 가져오기
    /// </summary>
    /// <returns>현재 보고 있는 상점 스탠드</returns>
    public ShopStand GetCurrentLookingShopStand()
    {
        return currentLookingShopStand;
    }

    /// <summary>
    /// 상점 스탠드 시선 추적 해제 (ShopStand에서 호출)
    /// </summary>
    /// <param name="shopStand">시선을 해제할 상점 스탠드</param>
    public void OnShopStandStopLooking(ShopStand shopStand)
    {
        if (currentLookingShopStand == shopStand)
        {
            currentLookingShopStand = null;
            isPurchaseHolding = false;
            purchaseHoldTimer = 0f;
        }
    }
    
    /// <summary>
    /// 구매 진행 상태 가져오기
    /// </summary>
    /// <returns>구매 진행률 (0~1)</returns>
    public float GetPurchaseProgress()
    {
        if (!isPurchaseHolding) return 0f;
        return Mathf.Clamp01(purchaseHoldTimer / purchaseHoldTime);
    }

    #endregion
}
