using System;
using System.Collections;
using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class ShopController : MonoBehaviourPun
{
    public static System.Action OnLocalItemPurchased; // 로컬 플레이어가 아이템 구매 완료 시
    
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
    
    // 프리팹 캐싱 (렉 방지)
    private static Dictionary<string, GameObject> cachedItemPrefabs = null;
    private static bool isCacheInitialized = false;

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
        
        // 프리팹 캐시 초기화 (첫 구매 시 렉 방지)
        InitializeItemPrefabCache();

        Debug.Log("ShopController - 초기화 완료");
    }

    void OnDestroy()
    {
        // 입력 이벤트 구독 해제
        InputManager.OnShootPressed -= OnShootPressed;
        InputManager.OnShootCanceledPressed -= OnShootCanceled;
    }
    
    /// <summary>
    /// 아이템 프리팹 캐시 초기화 (최초 1회만 실행)
    /// </summary>
    private void InitializeItemPrefabCache()
    {
        if (isCacheInitialized) return;
        
        StartCoroutine(InitializeCacheAsync());
    }
    
    /// <summary>
    /// 비동기 캐시 초기화 (로딩 시 렉 분산)
    /// </summary>
    private System.Collections.IEnumerator InitializeCacheAsync()
    {
        if (isCacheInitialized) yield break;
        
        cachedItemPrefabs = new Dictionary<string, GameObject>();
        
        // Resources.LoadAll을 백그라운드에서 실행 (첫 프레임에만 영향)
        GameObject[] allPrefabs = Resources.LoadAll<GameObject>("Prefabs/Items");
        
        foreach (GameObject prefab in allPrefabs)
        {
            if (prefab != null && !cachedItemPrefabs.ContainsKey(prefab.name))
            {
                cachedItemPrefabs[prefab.name] = prefab;
            }
        }
        
        isCacheInitialized = true;
        yield return null;
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
                // 진행 중인 구매가 있다면 취소
                if (isPurchaseHolding)
                {
                    currentLookingShopStand.CancelPurchaseProgress();
                }

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
                // 진행 중인 구매가 있다면 취소
                if (isPurchaseHolding)
                {
                    currentLookingShopStand.CancelPurchaseProgress();
                    //EnableGameInput(); // 게임 입력 다시 활성화
                }

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
        // 1. 직접 ShopStand 컴포넌트가 있는지 확인'
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
            float progress = purchaseHoldTimer / purchaseHoldTime;

            // ShopStand에 프로그레스 업데이트
            currentLookingShopStand.UpdatePurchaseProgress(progress);

            Debug.Log($"ShopController: 구매 진행 중 - {purchaseHoldTimer:F2}초 / {purchaseHoldTime}초 ({progress * 100:F1}%)");

            // 프로그레스가 1.0에 도달하면 ShopStand에서 자동으로 구매 완료 처리됨
        }
    }

    /// <summary>
    /// 현재 보고 있는 아이템 구매 시도 (레거시 메서드 - 프로그레스 시스템으로 대체됨)
    /// </summary>
    void TryPurchaseCurrentItem()
    {
        // 이 메서드는 더 이상 직접 호출되지 않음
        // ShopStand.CompletePurchase()에서 TryPurchaseItem()을 직접 호출함
        Debug.LogWarning("ShopController: TryPurchaseCurrentItem은 레거시 메서드입니다. 프로그레스 시스템을 사용하세요.");
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

        // 이미 구매 진행 중이거나 애니메이션 재생 중이면 무시
        if (currentLookingShopStand.IsPurchaseInProgress() ||
            currentLookingShopStand.IsPlayingPurchaseAnimation())
        {
            Debug.Log("ShopController: 이미 구매 진행 중이므로 무시");
            return;
        }

        Debug.Log("ShopController: 구매 홀드 시작");

        // ShopStand에 구매 프로그레스 시작 알림
        currentLookingShopStand.StartPurchaseProgress(this, purchaseHoldTime);

        isPurchaseHolding = true;
        purchaseHoldTimer = 0f;

        // 게임 입력 비활성화 (구매 중에는 다른 조작 차단)

    }

    /// <summary>
    /// 발사 버튼 해제 (구매 취소)
    /// </summary>
    void OnShootCanceled()
    {
        if (!isShopOpen) return;

        Debug.Log("ShopController: 구매 홀드 취소");

        // ShopStand에 구매 프로그레스 취소 알림
        if (currentLookingShopStand != null && isPurchaseHolding)
        {
            currentLookingShopStand.CancelPurchaseProgress();
        }

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
    /// 이 함수는 구매자 클라이언트에서만 실행되어야 합니다.
    /// </summary>
    public bool ProcessPurchaseLocal(int price, int itemIndex, string itemObjectName)
    {
        Debug.Log($"ShopController: ProcessPurchaseLocal 시작 - Price: {price}, ItemIndex: {itemIndex}");
        
        // ✅ 필수 컴포넌트 확인
        if (playerCoinController == null || playerItemController == null)
        {
            Debug.LogError("ShopController: 필수 컴포넌트 없음!");
            return false;
        }
        
        // ✅ 코인 확인 (이미 ShopStand에서 확인했지만 한번 더 안전하게)
        int currentCoin = playerCoinController.GetCurrentCoin();
        if (currentCoin < price)
        {
            Debug.LogWarning($"ShopController: 코인 부족! 현재: {currentCoin}, 필요: {price}");
            return false;
        }
        
        // ✅ 슬롯 확인
        if (playerItemController.GetItemSlotIndex() >= playerItemController.GetMaxItemSlot())
        {
            Debug.LogWarning("ShopController: 슬롯 가득 찼음!");
            return false;
        }
        

        
        // ✅ 코인 차감
        playerCoinController.SubtractCoin(price);
        Debug.Log($"ShopController: 코인 차감 완료 - {price}코인 (남은 코인: {playerCoinController.GetCurrentCoin()})");

        // ⚡️ 아이템 생성 및 부착 로직 변경: PhotonNetwork.Instantiate 사용
        GameObject itemPrefabToInstantiate = FindItemPrefabInResources(itemObjectName);

        if (itemPrefabToInstantiate != null)
        {
            // 1. PhotonNetwork.Instantiate를 사용하여 아이템을 네트워크 상에 생성합니다.
            //    (주의: 이 프리팹은 반드시 Resources 폴더 내에 있어야 하며 PhotonView를 포함해야 합니다.)

            // Resources.LoadAll을 사용하셨으므로, Resources 폴더 내 경로를 사용합니다.
            // itemPrefabToInstantiate.name (예: StrengthItem)
            string resourcePath = FindResourcePath("Prefabs/Items/"+itemPrefabToInstantiate.name);

            if (string.IsNullOrEmpty(resourcePath))
            {
                Debug.LogError($"ShopController: Resources 폴더에서 경로를 찾을 수 없음 - {itemObjectName}");
                return false;
            }

            GameObject networkItemObject = PhotonNetwork.Instantiate(
                resourcePath,
                Vector3.zero,
                Quaternion.identity,
                0,
                new object[] { photonView.ViewID } // 초기화 데이터: 소유자 플레이어의 ViewID (선택 사항)
            );

            if (networkItemObject != null)
            {
                PhotonView itemPv = networkItemObject.GetComponent<PhotonView>();
                if (itemPv != null)
                {
                    // 2. 아이템의 소유권을 구매한 플레이어에게 이전합니다.
                    itemPv.TransferOwnership(PhotonNetwork.LocalPlayer);

                    // 3. RPC를 사용하여 모든 클라이언트에게 아이템을 해당 플레이어에게 부착하도록 지시합니다.
                    //    (ItemController에 추가한 RPC_AttachNetworkItem 함수 사용)
                    playerItemController.photonView.RPC("RPC_AttachNetworkItem", RpcTarget.All,
                        itemPv.ViewID, photonView.ViewID);

                    Debug.Log($"ShopController: 아이템 생성 및 RPC 요청 완료 - {itemObjectName} (ViewID: {itemPv.ViewID})");
                    
                    // 로컬 구매 완료 이벤트 발행
                    OnLocalItemPurchased?.Invoke();
                    
                    return true;
                }
            }

            // 생성 실패
            Debug.LogError($"ShopController: PhotonNetwork.Instantiate 실패 - {itemObjectName}");
            return false;
        }
        else
        {
            Debug.LogError($"ShopController: 아이템 프리팹을 찾을 수 없음 - {itemObjectName}");
            return false;
        }
    }

    /// <summary>
    /// 아이템 오브젝트 이름으로 캐시된 프리팹을 찾아 반환합니다.
    /// (렉 방지: 캐시 사용)
    /// </summary>
    GameObject FindItemPrefabInResources(string itemObjectName)
    {
        // 캐시가 초기화되지 않았으면 즉시 초기화 (안전장치)
        if (!isCacheInitialized || cachedItemPrefabs == null)
        {
            cachedItemPrefabs = new Dictionary<string, GameObject>();
            GameObject[] allPrefabs = Resources.LoadAll<GameObject>("Prefabs/Items");
            
            foreach (GameObject prefab in allPrefabs)
            {
                if (prefab != null && !cachedItemPrefabs.ContainsKey(prefab.name))
                {
                    cachedItemPrefabs[prefab.name] = prefab;
                }
            }
            isCacheInitialized = true;
        }
        
        // 캐시에서 빠르게 찾기 (O(1) 성능)
        if (cachedItemPrefabs.ContainsKey(itemObjectName))
        {
            return cachedItemPrefabs[itemObjectName];
        }
        
        // 캐시에 없으면 null 반환
        return null;
    }

    /// <summary>
    /// 프리팹 이름으로 Resources 폴더 내의 상대 경로를 찾아 반환합니다.
    /// (PhotonNetwork.Instantiate는 경로 문자열이 필요합니다.)
    /// </summary>
    string FindResourcePath(string prefabName)
    {
        // 실제로 Assets/Resources/Skills/StrengthItem.prefab 일 경우 "Skills/StrengthItem"을 반환해야 합니다.
        // 여기서는 간단하게 프리팹 이름과 동일한 경로에 있다고 가정합니다. 
        // 실제 프로젝트 구조에 따라 이 부분을 정확하게 수정해야 합니다.

        // 예시: "Prefabs/Items/" + prefabName 
        // 예시: "Skills/Items/" + prefabName 

        // 현재는 이름만 가지고 있으므로, 이름과 동일한 경로에 있다고 가정합니다.
        // 만약 `StrengthItem`을 찾고 싶다면, `Resources.Load("StrengthItem")`이 가능하도록 해야 합니다.
        return prefabName;
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
