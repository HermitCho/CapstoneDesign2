using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

/// <summary>
/// 맵 기반 상점 오브젝트 - 아이템 생성 및 구매 처리를 담당
/// </summary>
public class Shop : MonoBehaviourPun
{
    [Header("상점 스탠드들")]
    [SerializeField] private ShopStand[] shopStands;
    [Header("상점 아이템 프리팹")]
    [SerializeField] private GameObject[] itemPrefabs;

    // 현재 생성된 아이템들
    private GameObject[] currentItems;
    private Coroutine[] renewCoroutines;

    // 아이템 갱신 시간 관리
    private float[] itemRenewTimes; // 각 슬롯의 남은 갱신 시간
    private bool[] isRenewTimerActive; // 각 슬롯의 타이머 활성 상태

    // 현재 상점을 이용중인 플레이어들
    private HashSet<ShopController> connectedPlayers = new HashSet<ShopController>();

    #region Unity 생명주기

    void Start()
    {
        InitializeShop();

        Debug.Log("[Shop - Start] 상점 초기화 작업");
        // 방 속성 변경 이벤트 구독 (비마스터 클라이언트용)
        if (!PhotonNetwork.IsMasterClient)
        {
            // 초기 아이템 상태 동기화 요청
            StartCoroutine(RequestInitialSyncWithDelay());
        }
    }

    /// <summary>
    /// 지연된 초기 동기화 요청 (네트워크 안정화 대기)
    /// </summary>
    private IEnumerator RequestInitialSyncWithDelay()
    {
        // 마스터 클라이언트가 아닌 경우에만 동기화 요청
        if (!PhotonNetwork.IsMasterClient && PhotonNetwork.IsConnected)
        {
            Debug.Log("[Shop - RequestInitialSyncWithDelay] 상점 초기화 후 동기화 작업");
            yield return new WaitForSeconds(1f); // 1초 대기 후 첫 요청

            // 최대 5번까지 재시도
            int maxRetries = 5;
            int currentRetry = 0;

            while (currentRetry < maxRetries)
            {
                //Debug.Log($"Shop: 초기 아이템 동기화 요청 - 시도 {currentRetry + 1}/{maxRetries}");
                photonView.RPC("RequestInitialItems", RpcTarget.MasterClient);

                // 2초 대기 후 아이템이 생성되었는지 확인
                yield return new WaitForSeconds(2f);

                bool hasItems = false;
                for (int i = 0; i < currentItems.Length; i++)
                {
                    if (currentItems[i] != null)
                    {
                        Debug.Log("[Shop - RequestInitialSyncWithDelay] currentItems 확인 " + currentItems[i]);
                        hasItems = true;
                        break;
                    }
                }

                if (hasItems)
                {
                    Debug.Log("Shop: 초기 아이템 동기화 성공!");
                    break;
                }

                currentRetry++;
                if (currentRetry < maxRetries)
                {
                    yield return new WaitForSeconds(1f); // 재시도 전 대기
                }
            }

            if (currentRetry >= maxRetries)
            {
                Debug.LogWarning("Shop: 초기 아이템 동기화 최대 재시도 횟수 초과");
            }
        }
    }

    #endregion

    #region 초기화

    /// <summary>
    /// 상점 초기화
    /// </summary>
    void InitializeShop()
    {
        if (shopStands == null || shopStands.Length == 0)
        {
            //Debug.LogError("Shop: shopStands가 설정되지 않았습니다.");
            return;
        }

        currentItems = new GameObject[shopStands.Length];
        renewCoroutines = new Coroutine[shopStands.Length];
        itemRenewTimes = new float[shopStands.Length];
        isRenewTimerActive = new bool[shopStands.Length];

        // 타이머 초기화
        for (int i = 0; i < itemRenewTimes.Length; i++)
        {
            itemRenewTimes[i] = 0f;
            isRenewTimerActive[i] = false;
        }

        // 마스터 클라이언트만 아이템 생성
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[Shop - InitializeShop] StartItemGeneration 아이템 생성 시작!");
            StartItemGeneration();
        }
        // 비마스터 클라이언트의 초기 동기화는 Start()에서 지연 처리
    }

    /// <summary>
    /// 아이템 생성 시작
    /// </summary>
    void StartItemGeneration()
    {
        for (int i = 0; i < shopStands.Length; i++)
        {
            GenerateItemAtPosition(i);
        }
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

        connectedPlayers.Add(shopController);
    }

    /// <summary>
    /// ShopController와의 연결 해제
    /// </summary>
    public void DisconnectShopController(ShopController shopController)
    {
        if (shopController != null)
        {
            connectedPlayers.Remove(shopController);
            //Debug.Log($"Shop: 플레이어 {shopController.name} 연결 해제됨");
        }
    }

    /// <summary>
    /// 다른 클라이언트가 초기 아이템을 요청할 때 호출
    /// </summary>
    [PunRPC]
    void RequestInitialItems()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        //Debug.Log("Shop: 다른 클라이언트가 초기 아이템 요청");

        // 아이템이 없으면 생성
        bool hasAnyItem = false;
        for (int i = 0; i < currentItems.Length; i++)
        {
            if (currentItems[i] != null)
            {
                hasAnyItem = true;
                break;
            }
        }

        if (!hasAnyItem)
        {
            //Debug.Log("Shop: 마스터 클라이언트에 아이템이 없어서 생성 시작");
            StartItemGeneration();

            // 아이템 생성 완료를 기다림
            StartCoroutine(WaitAndSyncItems());
        }
        else
        {
            // 즉시 동기화
            SyncExistingItemsToClients();
        }
    }

    /// <summary>
    /// 아이템 생성 완료 후 동기화
    /// </summary>
    private IEnumerator WaitAndSyncItems()
    {
        yield return new WaitForSeconds(1f); // 아이템 생성 대기
        SyncExistingItemsToClients();
    }

    /// <summary>
    /// 기존 아이템들을 클라이언트에 동기화
    /// </summary>
    private void SyncExistingItemsToClients()
    {
        int syncedItems = 0;

        // 현재 생성된 아이템들을 요청한 클라이언트에게 동기화
        for (int i = 0; i < currentItems.Length; i++)
        {
            Debug.Log("[Shop - SyncExistingItemsToClients] 클라이언트 동기화 " + currentItems[i]);
            if (currentItems[i] != null)
            {
                PhotonView itemPV = currentItems[i].GetComponent<PhotonView>();
                if (itemPV != null && itemPV.ViewID > 0)
                {
                    if (itemPV.gameObject == null) continue;
                    // 요청한 클라이언트에게만 전송
                    photonView.RPC("SyncItemToShopStand", RpcTarget.Others, i, itemPV.ViewID);
                    syncedItems++;

                    // 타이머 정보도 함께 동기화
                    if (i < itemRenewTimes.Length)
                    {
                        photonView.RPC("SyncRenewTimer", RpcTarget.Others, i, itemRenewTimes[i], isRenewTimerActive[i]);
                    }

                    //Debug.Log($"Shop: 아이템 {i} 동기화 - ViewID: {itemPV.ViewID}");
                }
            }
        }

        //Debug.Log($"Shop: 초기 아이템 동기화 완료 - {syncedItems}/{currentItems.Length}개 아이템 전송");
    }

    #endregion

    #region 아이템 생성 및 관리

    /// <summary>
    /// 특정 위치에 아이템 생성 (마스터 클라이언트만 호출)
    /// </summary>
    /// <param name="positionIndex">생성할 위치 인덱스</param>
    void GenerateItemAtPosition(int positionIndex)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (positionIndex < 0 || positionIndex >= shopStands.Length) return;

        ShopStand shopStand = shopStands[positionIndex];
        if (shopStand == null) return;

        // 기존 아이템이 있다면 제거
        if (currentItems[positionIndex] != null)
        {
            shopStand.ClearCurrentItem();

            // 안전한 오브젝트 파괴 처리
            GameObject itemToDestroy = currentItems[positionIndex];
            PhotonView itemPV = itemToDestroy.GetComponent<PhotonView>();

            // PhotonView가 있고 마스터 클라이언트이며 오브젝트가 아직 유효한 경우에만 파괴
            if (itemPV != null && PhotonNetwork.IsMasterClient && itemPV.gameObject != null && !itemPV.isRuntimeInstantiated == false)
            {
                try
                {
                    PhotonNetwork.Destroy(itemToDestroy);
                    //Debug.Log($"Shop: 아이템 파괴 성공 - ViewID: {itemPV.ViewID}");
                }
                catch (System.Exception e)
                {
                    //Debug.LogWarning($"Shop: 아이템 파괴 실패 - {e.Message}");
                    // 로컬에서만 제거
                    if (itemToDestroy != null)
                        Destroy(itemToDestroy);
                }
            }
            else if (itemPV == null)
            {
                // PhotonView가 없는 로컬 오브젝트는 일반 Destroy
                Destroy(itemToDestroy);
            }

            currentItems[positionIndex] = null;
        }

        // 기존 코루틴 정지
        if (renewCoroutines[positionIndex] != null)
        {
            StopCoroutine(renewCoroutines[positionIndex]);
        }

        // 확률 기반으로 아이템 선택
        GameObject selectedPrefab = SelectItemByProbability();
        if (selectedPrefab != null)
        {
            // 네트워크 오브젝트로 안전하게 생성
            Vector3 spawnPos = shopStand.GetItemSpawnPoint().position;
            Quaternion spawnRot = shopStand.GetItemSpawnPoint().rotation;
            spawnRot = Quaternion.Euler(90, 0, 90);

            GameObject spawnedItem = null;

            try
            {
                spawnedItem = PhotonNetwork.Instantiate($"Prefabs/ShopItems/{selectedPrefab.name}", spawnPos, spawnRot);
                if (spawnedItem != null)
                {
                    currentItems[positionIndex] = spawnedItem;
                    //Debug.Log($"Shop: 아이템 생성 성공 - Position: {positionIndex}, Item: {spawnedItem.name}");
                }
                else
                {
                    //Debug.LogError($"Shop: 아이템 생성 실패 - Position: {positionIndex}, Prefab: {selectedPrefab.name}");
                    return;
                }
            }
            catch (System.Exception e)
            {
                //Debug.LogError($"Shop: 아이템 생성 예외 발생 - {e.Message}");
                return;
            }

            // 생성된 아이템이 null이 아닌 경우에만 후처리 진행
            if (spawnedItem == null)
            {
                //Debug.LogError($"Shop: 아이템 후처리 실패 - spawnedItem이 null입니다");
                return;
            }

            // 아이템에 회전 애니메이션 컴포넌트 추가
            ItemRotator rotator = spawnedItem.GetComponent<ItemRotator>();
            if (rotator == null)
            {
                rotator = spawnedItem.AddComponent<ItemRotator>();
            }

            // PhotonView 확인 후 동기화
            PhotonView itemPV = spawnedItem.GetComponent<PhotonView>();
            if (itemPV != null)
            {
                // ShopStand에 아이템 배치 (모든 클라이언트에서 동기화)
                photonView.RPC("SyncItemToShopStand", RpcTarget.All, positionIndex, itemPV.ViewID);

                // 아이템의 renewTime 설정 및 타이머 시작
                Item itemComponent = spawnedItem.GetComponent<Item>();
                if (itemComponent != null)
                {
                    float renewTime = itemComponent.RenewTime;
                    itemRenewTimes[positionIndex] = renewTime;
                    isRenewTimerActive[positionIndex] = true;

                    // 모든 클라이언트에 타이머 정보 동기화
                    photonView.RPC("SyncRenewTimer", RpcTarget.All, positionIndex, renewTime, true);

                    //Debug.Log($"Shop: 아이템 설정 완료 - Position: {positionIndex}, RenewTime: {renewTime}초");
                }
            }
            else
            {
                //Debug.LogError($"Shop: 생성된 아이템에 PhotonView가 없습니다 - Position: {positionIndex}");
            }


        }
    }

    /// <summary>
    /// 확률 기반으로 아이템 선택
    /// </summary>
    /// <returns>선택된 아이템 프리팹</returns>
    GameObject SelectItemByProbability()
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0) return null;

        List<GameObject> availableItems = new List<GameObject>();

        foreach (GameObject prefab in itemPrefabs)
        {
            Item itemComponent = prefab.GetComponent<Item>();
            if (itemComponent != null)
            {
                float probability = itemComponent.Probability;
                float randomValue = Random.Range(0f, 1f);

                if (randomValue <= probability)
                {
                    availableItems.Add(prefab);
                }
            }
        }

        // 확률에 맞는 아이템이 없으면 기본 아이템 반환
        if (availableItems.Count == 0)
        {
            return itemPrefabs[Random.Range(0, itemPrefabs.Length)];
        }

        // Debug.Log("[Shop - SelectItemByProbability()] 최종 선택 아이템 목록: " +
        //           string.Join(", ", availableItems.Select(item => item.name)));
        return availableItems[Random.Range(0, availableItems.Count)];
    }

    // RenewItemAfterTime 메서드 제거 - UpdateRenewTimers에서 직접 처리

    /// <summary>
    /// ShopStand에 아이템 동기화 (모든 클라이언트에서 호출)
    /// </summary>
    /// <param name="positionIndex">위치 인덱스</param>
    /// <param name="itemViewID">아이템의 PhotonView ID</param>
    [PunRPC]
    void SyncItemToShopStand(int positionIndex, int itemViewID)
    {
        if (positionIndex < 0 || positionIndex >= shopStands.Length) return;

        // 약간의 지연을 두고 아이템 찾기 (네트워크 오브젝트 생성 완료 대기)
        StartCoroutine(FindAndSyncItemWithDelay(positionIndex, itemViewID));
    }

    /// <summary>
    /// 지연된 아이템 동기화 (네트워크 오브젝트 생성 완료 대기)
    /// </summary>
    private IEnumerator FindAndSyncItemWithDelay(int positionIndex, int itemViewID)
    {
        PhotonView itemPV = null;
        int attempts = 0;
        int maxAttempts = 15; // 시도 횟수 증가

        // 최대 1.5초 동안 아이템 찾기 시도
        while (itemPV == null && attempts < maxAttempts)
        {
            itemPV = PhotonView.Find(itemViewID);
            if (itemPV == null)
            {
                // 다른 방법으로도 시도
                itemPV = PhotonNetwork.GetPhotonView(itemViewID);
            }

            if (itemPV == null)
            {
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }
        }

        if (itemPV != null && itemPV.gameObject != null && positionIndex < shopStands.Length)
        {
            GameObject item = itemPV.gameObject;
            ShopStand shopStand = shopStands[positionIndex];

            if (shopStand != null)
            {
                shopStand.SetItem(item);
                // 비마스터 클라이언트에서도 currentItems 배열 업데이트
                if (positionIndex < currentItems.Length)
                {
                    //Debug.Log("[Shop - FindAndSyncItemWithDelay()] 현재 아이템 리스트가 있는지 확인 + " + currentItems);
                    //Debug.Log("[Shop - FindAndSyncItemWithDelay()] 현재 아이템 + " + currentItems[positionIndex].name);
                    currentItems[positionIndex] = item;
                }
                ////Debug.Log($"Shop: 아이템 동기화 완료 - Position: {positionIndex}, Item: {item.name}");
            }
        }
        else
        {
            //Debug.LogWarning($"Shop: 아이템 동기화 실패 - ViewID: {itemViewID}, Position: {positionIndex}");

            // 마스터 클라이언트에게 해당 위치 아이템 재생성 요청
            if (!PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RequestItemRegeneration", RpcTarget.MasterClient, positionIndex);
            }
        }
    }

    /// <summary>
    /// 아이템 재생성 요청 (마스터 클라이언트용)
    /// </summary>
    [PunRPC]
    void RequestItemRegeneration(int positionIndex)
    {
        if (PhotonNetwork.IsMasterClient && positionIndex >= 0 && positionIndex < currentItems.Length)
        {
            //Debug.Log($"Shop: 아이템 재생성 요청 받음 - Position: {positionIndex}");

            // 해당 위치에 아이템이 없으면 새로 생성
            if (currentItems[positionIndex] == null)
            {
                //Debug.Log("[Shop - RequestItemRegeneration()] 현재 아이템 없음 + " + currentItems[positionIndex]);
                GenerateItemAtPosition(positionIndex);
            }
            else
            {
                // 아이템이 있으면 다시 동기화
                PhotonView itemPV = currentItems[positionIndex].GetComponent<PhotonView>();
                if (itemPV != null && itemPV.ViewID > 0)
                {
                    //Debug.Log("[Shop - RequestItemRegeneration()] 현재 아이템 있음 SyncItemToShopStand 호출");
                    photonView.RPC("SyncItemToShopStand", RpcTarget.Others, positionIndex, itemPV.ViewID);
                }
            }
        }
    }

    #endregion

    #region 아이템 구매 처리

    /// <summary>
    /// 아이템 구매 요청 (ShopController에서 호출)
    /// </summary>
    /// <param name="item">구매할 아이템</param>
    /// <param name="buyer">구매자</param>
    public void PurchaseItem(GameObject item, ShopController buyer)
    {
        if (item == null || buyer == null) return;

        PhotonView buyerPV = buyer.GetComponent<PhotonView>();
        PhotonView itemPV = item.GetComponent<PhotonView>();

        if (buyerPV != null && itemPV != null)
        {
            // 마스터 클라이언트에게 구매 요청 (모든 클라이언트에서 가능)
            photonView.RPC("RequestPurchaseItem", RpcTarget.MasterClient,
                itemPV.ViewID, buyerPV.ViewID);
        }
    }

    [PunRPC]
    void RequestPurchaseItem(int itemViewID, int buyerViewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        //Debug.Log($"[Shop - RequestPurchaseItem] 구매해줄 아이템 : {itemViewID}, 구매해줄 이용자 : {buyerViewID}");

        PhotonView itemPV = PhotonView.Find(itemViewID);
        PhotonView buyerPV = PhotonView.Find(buyerViewID);


        if (itemPV == null || buyerPV == null) return;

        GameObject item = itemPV.gameObject;
        ShopController buyer = buyerPV.GetComponent<ShopController>();

        if (item == null || buyer == null) return;

        // 아이템이 현재 상점에 있는지 확인
        int positionIndex = -1;
        for (int i = 0; i < currentItems.Length; i++)
        {
            if (currentItems[i] == item)
            {
                positionIndex = i;
                break;
            }
        }

        if (positionIndex == -1) return;

        // 구매 처리 - 구매자 클라이언트에서 직접 처리하도록 RPC 전송
        Item itemComponent = item.GetComponent<Item>();
        Skill skillComponent = itemComponent.ItemObject.GetComponent<Skill>();

        if (itemComponent == null || skillComponent == null) return;

        // 현재 남은 갱신 시간 가져오기 (구매 시점 기준)
        float currentRemainingTime = itemRenewTimes[positionIndex];

        // 구매자에게 구매 처리 RPC 전송 (구매자의 ShopController에서 직접 처리)
        buyerPV.RPC("ProcessPurchase", buyerPV.Owner,
            skillComponent.Price,
            skillComponent.Index,
            itemComponent.ItemObject.name,
            positionIndex);

        //Debug.Log($"Shop: 플레이어 {buyer.name}가 아이템 {item.name} 구매 완료");
    }


    [PunRPC]
    void OnItemPurchased(int positionIndex)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (positionIndex < 0 || positionIndex >= currentItems.Length) return;

        ShopStand shopStand = shopStands[positionIndex];
        if (shopStand == null) return;

        //Debug.Log($"Shop: 마스터 클라이언트에서 아이템 제거 처리 - PositionIndex: {positionIndex}");

        // 구매 시점의 남은 갱신 시간 저장 (아이템 제거 전에)
        float remainingRenewTime = itemRenewTimes[positionIndex];
        //Debug.Log($"Shop: 구매 시점 남은 갱신 시간 - {remainingRenewTime:F1}초");

        // ShopStand 정리
        shopStand.ClearCurrentItem();

        // 구매된 아이템 제거 (안전한 파괴 처리)
        if (currentItems[positionIndex] != null)
        {
            StartCoroutine(SafeDestroyPurchasedItem(positionIndex, remainingRenewTime));
        }

        //Debug.Log($"Shop: 위치 {positionIndex} 아이템 구매 처리 완료 - SafeDestroyPurchasedItem에서 갱신 처리");
    }

    /// <summary>
    /// 갱신 타이머 동기화 (모든 클라이언트에서 호출)
    /// </summary>
    /// <param name="positionIndex">위치 인덱스</param>
    /// <param name="remainingTime">남은 시간</param>
    /// <param name="isActive">타이머 활성 상태</param>
    [PunRPC]
    void SyncRenewTimer(int positionIndex, float remainingTime, bool isActive)
    {
        if (positionIndex < 0 || positionIndex >= itemRenewTimes.Length) return;

        itemRenewTimes[positionIndex] = remainingTime;
        isRenewTimerActive[positionIndex] = isActive;

        // ShopStand에 타이머 정보 전달
        if (positionIndex < shopStands.Length && shopStands[positionIndex] != null)
        {
            shopStands[positionIndex].SetRenewTimer(remainingTime, isActive);
        }

    }

    /// <summary>
    /// 타이머 업데이트 (마스터 클라이언트에서 호출)
    /// </summary>
    void UpdateRenewTimers()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        for (int i = 0; i < itemRenewTimes.Length; i++)
        {
            if (isRenewTimerActive[i] && itemRenewTimes[i] > 0f)
            {
                float previousTime = itemRenewTimes[i];
                itemRenewTimes[i] -= Time.deltaTime;

                // 10초마다 한 번씩 디버그 로그 (너무 많은 로그 방지)
                if (Mathf.FloorToInt(previousTime) != Mathf.FloorToInt(itemRenewTimes[i]) &&
                    Mathf.FloorToInt(itemRenewTimes[i]) % 10 == 0)
                {
                    //Debug.Log($"Shop: 위치 {i} 갱신까지 {Mathf.CeilToInt(itemRenewTimes[i])}초 남음");
                }

                // 타이머가 0 이하가 되면 갱신
                if (itemRenewTimes[i] <= 0f)
                {
                    itemRenewTimes[i] = 0f;
                    isRenewTimerActive[i] = false;

                    //Debug.Log($"Shop: 타이머 완료로 아이템 갱신 시작 - Position {i}");
                    GenerateItemAtPosition(i);

                    // 갱신 완료 후 타이머 동기화
                    photonView.RPC("SyncRenewTimer", RpcTarget.Others, i, 0f, false);
                }
            }
        }
    }

    private float lastTimerSyncTime = 0f;
    private const float TIMER_SYNC_INTERVAL = 2f; // 2초마다 타이머 동기화 (UI 업데이트용)

    void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            UpdateRenewTimers();

            // 주기적으로 타이머 동기화 (너무 자주 보내지 않도록)
            if (Time.time - lastTimerSyncTime > TIMER_SYNC_INTERVAL)
            {
                SyncAllTimersToClients();
                lastTimerSyncTime = Time.time;
            }
        }
    }

    /// <summary>
    /// 모든 타이머를 클라이언트에 동기화
    /// </summary>
    private void SyncAllTimersToClients()
    {
        for (int i = 0; i < itemRenewTimes.Length; i++)
        {
            if (isRenewTimerActive[i])
            {
                photonView.RPC("SyncRenewTimer", RpcTarget.Others, i, itemRenewTimes[i], isRenewTimerActive[i]);
            }
        }
    }

    #endregion

    #region 상점 상태 확인

    /// <summary>
    /// 연결된 플레이어 수 확인
    /// </summary>
    /// <returns>연결된 플레이어 수</returns>
    public int GetConnectedPlayerCount()
    {
        return connectedPlayers.Count;
    }

    /// <summary>
    /// 특정 위치의 아이템 가져오기
    /// </summary>
    /// <param name="positionIndex">위치 인덱스</param>
    /// <returns>해당 위치의 아이템</returns>
    public GameObject GetItemAtPosition(int positionIndex)
    {
        if (positionIndex < 0 || positionIndex >= currentItems.Length) return null;
        return currentItems[positionIndex];
    }

    /// <summary>
    /// 구매된 아이템을 안전하게 파괴
    /// </summary>
    /// <param name="positionIndex">위치 인덱스</param>
    /// <param name="remainingRenewTime">구매 시점의 남은 갱신 시간</param>
    private System.Collections.IEnumerator SafeDestroyPurchasedItem(int positionIndex, float remainingRenewTime)
    {
        if (positionIndex < 0 || positionIndex >= currentItems.Length) yield break;

        GameObject itemToDestroy = currentItems[positionIndex];
        if (itemToDestroy == null) yield break;

        PhotonView itemPV = itemToDestroy.GetComponent<PhotonView>();

        if (itemPV != null)
        {
            // PhotonView가 있는 네트워크 오브젝트
            if (itemPV.gameObject != null && !itemPV.gameObject.Equals(null))
            {
                try
                {
                    // ViewID 유효성 검사
                    if (itemPV.ViewID > 0)
                    {
                        // PhotonNetwork에서 해당 ViewID를 가진 오브젝트 확인
                        PhotonView foundPV = PhotonNetwork.GetPhotonView(itemPV.ViewID);
                        if (foundPV != null && foundPV == itemPV)
                        {
                            if (PhotonNetwork.IsMasterClient)
                            {
                                PhotonNetwork.Destroy(itemToDestroy);
                                Debug.Log($"Shop: 네트워크 아이템 파괴 성공 - ViewID: {itemPV.ViewID}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Shop: PhotonView 불일치, 로컬에서만 제거 - {itemToDestroy.name}");
                            Destroy(itemToDestroy);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Shop: 유효하지 않은 ViewID, 로컬에서만 제거 - {itemToDestroy.name}");
                        Destroy(itemToDestroy);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Shop: 네트워크 아이템 파괴 실패 - {e.Message}, 로컬에서 제거");
                    if (itemToDestroy != null && !itemToDestroy.Equals(null))
                    {
                        Destroy(itemToDestroy);
                    }
                }
            }
        }
        else
        {
            // PhotonView가 없는 로컬 오브젝트
            if (itemToDestroy != null && !itemToDestroy.Equals(null))
            {
                Destroy(itemToDestroy);
                //Debug.Log($"Shop: 로컬 아이템 파괴 완료 - {itemToDestroy.name}");
            }
        }

        // 배열에서 제거
        currentItems[positionIndex] = null;

        // 잠시 대기 후 새 아이템 생성 스케줄링
        yield return new WaitForSeconds(0.1f);

        // 타이머 초기화 및 새 아이템 생성 스케줄링
        isRenewTimerActive[positionIndex] = false;
        itemRenewTimes[positionIndex] = 0f;

        // 구매 시점의 남은 갱신 시간 사용 (매개변수로 전달받은 값)
        float renewDelay = remainingRenewTime > 0f ? remainingRenewTime : GetDefaultRenewTime();

        itemRenewTimes[positionIndex] = renewDelay;
        isRenewTimerActive[positionIndex] = true;

        //Debug.Log($"Shop: 위치 {positionIndex} 갱신 타이머 시작 - 구매 시점 남은시간 {renewDelay:F1}초 사용");

        // 타이머 동기화 RPC 전송
        if (photonView != null)
        {
            photonView.RPC("SyncRenewTimer", RpcTarget.All, positionIndex, renewDelay, true);
        }

        // 연결된 모든 ShopController에게 갱신 알림
        if (positionIndex < shopStands.Length && shopStands[positionIndex] != null)
        {
            ShopStand shopStand = shopStands[positionIndex];
            foreach (var shopController in connectedPlayers)
            {
                if (shopController != null)
                {
                    shopController.OnShopStandStopLooking(shopStand);
                }
            }
        }
    }

    /// <summary>
    /// 기본 아이템 갱신 시간 반환
    /// </summary>
    private float GetDefaultRenewTime()
    {
        // DataBase에서 갱신 시간을 가져오거나 기본값 사용
        try
        {
            if (DataBase.Instance != null && DataBase.Instance.gameData != null)
            {
                // gameData에 상점 관련 속성이 있다면 사용, 없으면 PlayTime의 일정 비율 사용
                // 예: 게임 시간의 1/36 (360초 게임이면 10초 갱신)
                float playTime = DataBase.Instance.gameData.PlayTime;
                return Mathf.Max(5f, playTime / 36f); // 최소 5초, 최대 게임시간/36
            }
        }
        catch (System.Exception e)
        {
            //Debug.LogWarning($"Shop: DataBase에서 갱신 시간을 가져올 수 없음 - {e.Message}");
        }

        // 기본값: 10초
        return 10f;
    }

    #endregion
}
