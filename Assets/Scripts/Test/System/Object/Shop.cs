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
        
        // 마스터 클라이언트만 아이템 생성
        if (PhotonNetwork.IsMasterClient)
        {
            StartItemGeneration();
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
        Debug.Log($"Shop: 플레이어 {shopController.name} 연결됨");
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
            PhotonNetwork.Destroy(currentItems[positionIndex]);
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
            
            // ShopStand에 아이템 배치 (모든 클라이언트에서 동기화)
            photonView.RPC("SyncItemToShopStand", RpcTarget.All, positionIndex, spawnedItem.GetComponent<PhotonView>().ViewID);
            
            // 아이템의 renewTime 후에 새로운 아이템으로 교체
            Item itemComponent = spawnedItem.GetComponent<Item>();
            if (itemComponent != null)
            {
                float renewTime = itemComponent.RenewTime;
                renewCoroutines[positionIndex] = StartCoroutine(RenewItemAfterTime(positionIndex, renewTime));
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
    /// 지정된 시간 후 아이템 갱신
    /// </summary>
    /// <param name="positionIndex">갱신할 위치 인덱스</param>
    /// <param name="renewTime">갱신 시간</param>
    IEnumerator RenewItemAfterTime(int positionIndex, float renewTime)
    {
        yield return new WaitForSeconds(renewTime);
        GenerateItemAtPosition(positionIndex);
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
            Debug.Log($"Shop: 위치 {positionIndex}에 아이템 {item.name} 동기화 완료");
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
        Debug.Log($"Shop: PurchaseItem 호출됨 - item: {item != null}, buyer: {buyer != null}");
        
        if (item == null)
        {
            Debug.Log("Shop: item이 null");
            return;
        }
        
        if (buyer == null)
        {
            Debug.Log("Shop: buyer가 null");
            return;
        }
        
        // 마스터 클라이언트에게 구매 요청
        PhotonView buyerPV = buyer.GetComponent<PhotonView>();
        if (buyerPV != null)
        {
            PhotonView itemPV = item.GetComponent<PhotonView>();
            if (itemPV != null)
            {
                Debug.Log($"Shop: RPC RequestPurchaseItem 전송 - itemViewID: {itemPV.ViewID}, buyerViewID: {buyerPV.ViewID}");
                photonView.RPC("RequestPurchaseItem", RpcTarget.MasterClient, 
                    itemPV.ViewID, buyerPV.ViewID);
            }
            else
            {
                Debug.LogError("Shop: item에 PhotonView가 없음");
            }
        }
        else
        {
            Debug.LogError("Shop: buyer에 PhotonView가 없음");
        }
    }
    
    [PunRPC]
    void RequestPurchaseItem(int itemViewID, int buyerViewID)
    {
        Debug.Log($"Shop: RequestPurchaseItem RPC 수신 - itemViewID: {itemViewID}, buyerViewID: {buyerViewID}");
        
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Shop: 마스터 클라이언트가 아님");
            return;
        }
        
        PhotonView itemPV = PhotonView.Find(itemViewID);
        PhotonView buyerPV = PhotonView.Find(buyerViewID);
        
        if (itemPV == null)
        {
            Debug.LogError($"Shop: itemViewID {itemViewID}에 해당하는 PhotonView를 찾을 수 없음");
            return;
        }
        
        if (buyerPV == null)
        {
            Debug.LogError($"Shop: buyerViewID {buyerViewID}에 해당하는 PhotonView를 찾을 수 없음");
            return;
        }
        
        GameObject item = itemPV.gameObject;
        ShopController buyer = buyerPV.GetComponent<ShopController>();
        
        Debug.Log($"Shop: 아이템과 구매자 확인 - item: {item.name}, buyer: {buyer.name}");
        
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
        
        if (positionIndex == -1)
        {
            Debug.LogError($"Shop: 아이템 {item.name}이 현재 상점에 없음");
            return;
        }
        
        Debug.Log($"Shop: 아이템 위치 확인 - positionIndex: {positionIndex}");
        
        // 구매 처리
        Item itemComponent = item.GetComponent<Item>();
        Skill skillComponent = itemComponent.ItemObject.GetComponent<Skill>();
        
        if (itemComponent == null)
        {
            Debug.LogError($"Shop: 아이템 {item.name}에 Item 컴포넌트가 없음");
            return;
        }
        
        if (skillComponent == null)
        {
            Debug.LogError($"Shop: 아이템 {item.name}에 Skill 컴포넌트가 없음");
            return;
        }
        
        Debug.Log($"Shop: 컴포넌트 확인 완료 - Item: {itemComponent.name}, Skill: {skillComponent.SkillName}");
        
        // 플레이어의 코인 및 아이템 컨트롤러 확인
        CoinController coinController = buyer.GetComponent<CoinController>();
        ItemController itemController = buyer.GetComponent<ItemController>();
        
        if (coinController == null)
        {
            Debug.LogError($"Shop: 구매자 {buyer.name}에 CoinController가 없음");
            return;
        }
        
        if (itemController == null)
        {
            Debug.LogError($"Shop: 구매자 {buyer.name}에 ItemController가 없음");
            return;
        }
        
        Debug.Log($"Shop: 컨트롤러 확인 완료 - Coin: {coinController.GetCoin()}, Price: {skillComponent.Price}");
        
        // 구매 조건 확인
        if (coinController.GetCoin() < skillComponent.Price)
        {
            Debug.Log($"Shop: 코인 부족 - 현재: {coinController.GetCoin()}, 필요: {skillComponent.Price}");
            return;
        }
        
        if (itemController.HasItemByIndex(skillComponent.Index))
        {
            Debug.Log($"Shop: 이미 보유한 아이템 - Index: {skillComponent.Index}");
            return;
        }
        
        if (itemController.GetItemSlotIndex() >= itemController.GetMaxItemSlot())
        {
            Debug.Log($"Shop: 아이템 슬롯 부족 - 현재: {itemController.GetItemSlotIndex()}, 최대: {itemController.GetMaxItemSlot()}");
            return;
        }
        
        Debug.Log("Shop: 모든 구매 조건 통과 - 구매 처리 시작");
        
        // 구매 성공
        coinController.SubtractCoin(skillComponent.Price);
        Debug.Log($"Shop: 코인 차감 완료 - 남은 코인: {coinController.GetCoin()}");
        
        // itemObject만 플레이어에게 부착 (실제 Skill 컴포넌트가 있는 오브젝트)
        GameObject itemObject = itemComponent.ItemObject;
        if (itemObject != null)
        {
            Debug.Log($"Shop: itemObject 부착 시도 - {itemObject.name}");
            itemController.AttachItemObject(itemObject);
        }
        else
        {
            Debug.LogError($"Shop: {item.name}의 itemObject가 null입니다.");
        }
        
        // 아이템 제거 및 갱신 예약
        photonView.RPC("OnItemPurchased", RpcTarget.All, positionIndex);
        
        Debug.Log($"Shop: 플레이어 {buyer.name}가 아이템 {item.name} 구매 완료");
    }
    
    [PunRPC]
    void OnItemPurchased(int positionIndex)
    {
        if (positionIndex < 0 || positionIndex >= currentItems.Length) return;
        
        ShopStand shopStand = shopStands[positionIndex];
        if (shopStand == null) return;
        
        // 모든 클라이언트에서 ShopStand 정리
        shopStand.ClearCurrentItem();
        
        // 구매된 아이템 제거
        if (currentItems[positionIndex] != null)
        {
            if (PhotonNetwork.IsMasterClient)
            {
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
        
        // 마스터 클라이언트만 새 아이템 생성 예약
        if (PhotonNetwork.IsMasterClient)
        {
            // 구매된 아이템의 renewTime을 사용하여 새 아이템 생성 예약
            // 이미 아이템이 제거되었으므로 기본값 사용
            float renewTime = 10f; // 기본값 10초 (또는 DataBase에서 가져오기)
            
            renewCoroutines[positionIndex] = StartCoroutine(RenewItemAfterTime(positionIndex, renewTime));
        }
        
        Debug.Log($"Shop: 위치 {positionIndex} 아이템 구매 처리 완료 (모든 클라이언트)");
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
