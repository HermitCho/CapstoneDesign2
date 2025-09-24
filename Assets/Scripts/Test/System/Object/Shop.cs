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
            Debug.LogError("Shop: shopStands가 설정되지 않았습니다.");
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
            StartItemGeneration();
        }
        else
        {
            // 다른 클라이언트는 마스터 클라이언트에게 초기 아이템 요청
            photonView.RPC("RequestInitialItems", RpcTarget.MasterClient);
        }
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
            Debug.Log($"Shop: 플레이어 {shopController.name} 연결 해제됨");
        }
    }
    
    /// <summary>
    /// 다른 클라이언트가 초기 아이템을 요청할 때 호출
    /// </summary>
    [PunRPC]
    void RequestInitialItems()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        Debug.Log("Shop: 다른 클라이언트가 초기 아이템 요청");
        
        // 현재 생성된 아이템들을 모든 클라이언트에게 동기화
        for (int i = 0; i < currentItems.Length; i++)
        {
            if (currentItems[i] != null)
            {
                PhotonView itemPV = currentItems[i].GetComponent<PhotonView>();
                if (itemPV != null)
                {
                    photonView.RPC("SyncItemToShopStand", RpcTarget.All, i, itemPV.ViewID);
                }
            }
        }
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
            // 마스터 클라이언트만 PhotonNetwork.Destroy 호출
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(currentItems[positionIndex]);
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
            // 네트워크 오브젝트로 생성
            Vector3 spawnPos = shopStand.GetItemSpawnPoint().position;
            Quaternion spawnRot = shopStand.GetItemSpawnPoint().rotation;
            spawnRot = Quaternion.Euler(0, 0, 90);
            GameObject spawnedItem = PhotonNetwork.Instantiate(selectedPrefab.name, spawnPos, spawnRot);
            currentItems[positionIndex] = spawnedItem;
            
            // 아이템에 회전 애니메이션 컴포넌트 추가
            ItemRotator rotator = spawnedItem.GetComponent<ItemRotator>();
            if (rotator == null)
            {
                rotator = spawnedItem.AddComponent<ItemRotator>();
            }
            
            // ShopStand에 아이템 배치 (모든 클라이언트에서 동기화)
            photonView.RPC("SyncItemToShopStand", RpcTarget.All, positionIndex, spawnedItem.GetComponent<PhotonView>().ViewID);
            
            // 아이템의 renewTime 설정 및 타이머 시작
            Item itemComponent = spawnedItem.GetComponent<Item>();
            if (itemComponent != null)
            {
                float renewTime = itemComponent.RenewTime;
                itemRenewTimes[positionIndex] = renewTime;
                isRenewTimerActive[positionIndex] = true;
                // renewCoroutines[positionIndex] = StartCoroutine(RenewItemAfterTime(positionIndex, renewTime)); // 더 이상 사용하지 않음
                
                // 모든 클라이언트에 타이머 정보 동기화
                photonView.RPC("SyncRenewTimer", RpcTarget.All, positionIndex, renewTime, true);
            }
            
            Debug.Log($"Shop: 위치 {positionIndex}에 아이템 {selectedPrefab.name} 생성됨");
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
        
        return availableItems[Random.Range(0, availableItems.Count)];
    }
    
    /// <summary>
    /// 지정된 시간 후 아이템 갱신 (더 이상 사용하지 않음 - UpdateRenewTimers로 대체)
    /// </summary>
    /// <param name="positionIndex">갱신할 위치 인덱스</param>
    /// <param name="renewTime">갱신 시간</param>
    IEnumerator RenewItemAfterTime(int positionIndex, float renewTime)
    {
        yield return new WaitForSeconds(renewTime);
        // 이 메서드는 더 이상 사용되지 않음 - UpdateRenewTimers에서 처리
    }
    
    /// <summary>
    /// ShopStand에 아이템 동기화 (모든 클라이언트에서 호출)
    /// </summary>
    /// <param name="positionIndex">위치 인덱스</param>
    /// <param name="itemViewID">아이템의 PhotonView ID</param>
    [PunRPC]
    void SyncItemToShopStand(int positionIndex, int itemViewID)
    {
        if (positionIndex < 0 || positionIndex >= shopStands.Length) return;
        
        PhotonView itemPV = PhotonView.Find(itemViewID);
        if (itemPV == null) return;
        
        GameObject item = itemPV.gameObject;
        ShopStand shopStand = shopStands[positionIndex];
        
        if (shopStand != null)
        {
            shopStand.SetItem(item);
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
        
        // 구매자에게 구매 처리 RPC 전송 (구매자의 ShopController에서 직접 처리)
        buyerPV.RPC("ProcessPurchase", buyerPV.Owner, 
            skillComponent.Price, 
            skillComponent.Index, 
            itemComponent.ItemObject.name,
            positionIndex);
        
        Debug.Log($"Shop: 플레이어 {buyer.name}가 아이템 {item.name} 구매 완료");
    }
    
    
    [PunRPC]
    void OnItemPurchased(int positionIndex)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (positionIndex < 0 || positionIndex >= currentItems.Length) return;
        
        ShopStand shopStand = shopStands[positionIndex];
        if (shopStand == null) return;
        
        Debug.Log($"Shop: 마스터 클라이언트에서 아이템 제거 처리 - PositionIndex: {positionIndex}");
        
        // ShopStand 정리
        shopStand.ClearCurrentItem();
        
        // 구매된 아이템 제거 (마스터 클라이언트만 PhotonNetwork.Destroy 호출)
        if (currentItems[positionIndex] != null)
        {
            PhotonView itemPV = currentItems[positionIndex].GetComponent<PhotonView>();
            if (itemPV != null && itemPV.IsMine)
            {
                Debug.Log($"Shop: 아이템 파괴 - ViewID: {itemPV.ViewID}");
                PhotonNetwork.Destroy(currentItems[positionIndex]);
            }
            currentItems[positionIndex] = null;
        }
        
        // 기존 갱신 코루틴 정지
        if (renewCoroutines[positionIndex] != null)
        {
            StopCoroutine(renewCoroutines[positionIndex]);
            renewCoroutines[positionIndex] = null;
        }
        
        // 기존 타이머 시간 유지 (아이템이 구매되어도 타이머는 계속 진행)
        if (isRenewTimerActive[positionIndex] && itemRenewTimes[positionIndex] > 0f)
        {
            Debug.Log($"Shop: 기존 타이머 시간 유지 - {itemRenewTimes[positionIndex]}초");
            // 타이머는 UpdateRenewTimers()에서 계속 업데이트됨
        }
        else
        {
            // 타이머가 비활성화되어 있으면 기본값 사용
            float defaultRenewTime = 10f;
            itemRenewTimes[positionIndex] = defaultRenewTime;
            isRenewTimerActive[positionIndex] = true;
            
            // 모든 클라이언트에 타이머 정보 동기화
            photonView.RPC("SyncRenewTimer", RpcTarget.All, positionIndex, defaultRenewTime, true);
            Debug.Log($"Shop: 기본 타이머 시간 사용 - {defaultRenewTime}초");
        }
        
        Debug.Log($"Shop: 위치 {positionIndex} 아이템 구매 처리 완료");
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
        
        Debug.Log($"Shop: 타이머 동기화 - Position: {positionIndex}, Time: {remainingTime}, Active: {isActive}");
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
                itemRenewTimes[i] -= Time.deltaTime;
                
                // 타이머가 0 이하가 되면 갱신
                if (itemRenewTimes[i] <= 0f)
                {
                    itemRenewTimes[i] = 0f;
                    isRenewTimerActive[i] = false;
                    GenerateItemAtPosition(i);
                }
                
                // 모든 클라이언트에 타이머 업데이트 전송
                photonView.RPC("SyncRenewTimer", RpcTarget.All, i, itemRenewTimes[i], isRenewTimerActive[i]);
            }
        }
    }
    
    void Update()
    {
        UpdateRenewTimers();
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
    
    #endregion
}
