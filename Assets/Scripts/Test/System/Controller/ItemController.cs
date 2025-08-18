using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ItemController : MonoBehaviourPun
{
    #region  참조
    private DataBase.ItemData itemData;
    private PhotonView photonView;
    #endregion

    #region 캐싱된 값들 (성능 최적화)
    private GameObject[] cachedItemPrefab;
    private int cachedMaxItemSlot;
    private bool dataBaseCached = false;
    #endregion

    #region 내부 상태 변수
    private int currentItemIndex = -1;
    private int currentItemSlotIndex = 0;
    #endregion

    #region 인스펙터 할당 변수
    [Header("아이템 슬롯 할당")]
    [SerializeField] private GameObject itemSlot1;
    [Header("아이템 쓰레기통 할당")]
    [SerializeField] private GameObject itemTemp;
    [Header("UI 참조")]
    [SerializeField] private HUDPanel hudPanel; // HUDPanel 직접 참조
    #endregion

    #region Unity 생명주기
    void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }   

    void Start()
    {   
        if (!photonView.IsMine) return;
        CacheDataBaseInfo();
        // HUDPanel 찾아서 캐싱
        FindAndCacheHUDPanel();
    }
    #endregion

    #region 초기화
    private void CacheDataBaseInfo()
    {
        try
        {
            if (!dataBaseCached)
            {
                itemData = DataBase.Instance.itemData;
                cachedItemPrefab = itemData.ItemPrefabData.ToArray();
                cachedMaxItemSlot = itemData.MaxItemSlot;
                dataBaseCached = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("아이템 데이터 캐시 실패: " + e.Message);
        }
    }
    #endregion

    #region 아이템 컨트롤

    public void AttachItem(GameObject itemPrefab)
    {
        if (!photonView.IsMine) return;
        RPC_AttachItem(itemPrefab.name);
        //if (!PhotonView.Get(this).IsMine) return;
        //PhotonView.Get(this).RPC("RPC_AttachItem", RpcTarget.All, itemPrefab.name);
    }

    //[PunRPC]
    public void RPC_AttachItem(string itemPrefabName)
    {
        if (itemSlot1 == null)
        {
            Debug.LogError("❌ ItemController - ItemSlot을 찾을 수 없습니다.");
            return;
        }
        GameObject prefab = null;
        foreach (var go in cachedItemPrefab)
        {
            if (go != null && go.name == itemPrefabName)
            {
                prefab = go;
                break;
            }
        }
        if (prefab == null)
        {
            Debug.LogError($"❌ ItemController - {itemPrefabName} 프리팹을 찾을 수 없습니다.");
            return;
        }
        try
        {
            GameObject itemInstance = Instantiate(prefab, itemSlot1.transform);
            itemInstance.transform.SetAsFirstSibling();
            currentItemSlotIndex++;
            UpdateItemOrderAndActivation();
            UpdateHUDPanelSafely();
            Debug.Log($"✅ ItemController - 아이템 부착 완료: {prefab.name} -> {itemInstance.name} (첫 번째 자식으로 배치)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ItemController - 아이템 부착 중 오류: {e.Message}");
        }
    }

    /// <summary>
    /// 아이템 순서 재정렬 및 활성화 상태 업데이트
    /// </summary>
    public void UpdateItemOrderAndActivation()
    {
        if (!photonView.IsMine) return;

        if (itemSlot1 == null) return;

        int childCount = itemSlot1.transform.childCount;
        if (childCount == 0) return;

        Debug.Log($"🔄 ItemController - 아이템 순서 재정렬 시작: {childCount}개 아이템");

        // 모든 자식을 비활성화
        for (int i = 0; i < childCount; i++)
        {
            Transform child = itemSlot1.transform.GetChild(i);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        // 마지막 자식(가장 아래)을 첫 번째 아이템으로 활성화 (Unity에서는 마지막 자식이 0번째 인덱스)
        Transform lastChild = itemSlot1.transform.GetChild(childCount - 1);
        if (lastChild != null)
        {
            lastChild.gameObject.SetActive(true);
        }
        
        // HUDPanel 안전하게 업데이트
        UpdateHUDPanelSafely();
    }

    /// <summary>
    /// HUDPanel을 찾아서 캐싱
    /// </summary>
    private void FindAndCacheHUDPanel()
    {
        if (hudPanel == null)
        {
            hudPanel = FindObjectOfType<HUDPanel>();
            if (hudPanel != null)
            {
                Debug.Log("✅ ItemController - HUDPanel 찾기 및 캐싱 완료");
            }
            else
            {
                Debug.LogWarning("⚠️ ItemController - HUDPanel을 찾을 수 없습니다.");
            }
        }
    }

    /// <summary>
    /// HUDPanel을 안전하게 업데이트 (이벤트 기반으로 변경)
    /// </summary>
    private void UpdateHUDPanelSafely()
    {
        // 이벤트를 통해 HUD에 알림 (직접 호출 대신)
        // HUD는 자체적으로 로컬 플레이어의 ItemController를 모니터링하므로
        // 별도의 업데이트 호출이 필요하지 않음
        Debug.Log("✅ ItemController - 아이템 변경 완료, HUD는 자동 업데이트됨");
    }

    /// <summary>
    /// 외부에서 HUD 패널 업데이트 요청 (상점에서 나갈 때 등)
    /// </summary>
    public void RequestHUDPanelUpdate()
    {
        // HUD는 자체적으로 업데이트되므로 별도 작업 불필요
        Debug.Log("✅ ItemController - HUD 업데이트 요청 (자동 처리됨)");
    }

    /// <summary>
    /// 사용된 아이템을 쓰레기통으로 이동
    /// </summary>
    /// <param name="usedItem">사용된 아이템</param>
    public void MoveUsedItemToTemp(GameObject usedItem)
    {
        if (!photonView.IsMine) return;
        
        if (itemTemp == null)
        {
            Debug.LogError("❌ ItemController - itemTemp가 할당되지 않았습니다.");
            return;
        }

        if (usedItem == null)
        {
            Debug.LogError("❌ ItemController - 사용된 아이템이 null입니다.");
            return;
        }

        try
        {
            // 사용된 아이템이 실제로 itemSlot1의 자식인지 확인
            if (usedItem.transform.parent != itemSlot1.transform)
            {
                Debug.LogWarning($"⚠️ ItemController - 사용된 아이템이 ItemSlot1의 자식이 아닙니다: {usedItem.name}");
                return;
            }

            // 사용된 아이템이 실제로 활성화되어 있는지 확인
            if (!usedItem.activeInHierarchy)
            {
                Debug.LogWarning($"⚠️ ItemController - 사용된 아이템이 비활성화되어 있습니다: {usedItem.name}");
                return;
            }

            // 아이템을 쓰레기통으로 이동
            usedItem.transform.SetParent(itemTemp.transform);
            currentItemSlotIndex--;
            
            // 아이템 순서 재정렬 및 활성화 상태 업데이트 (남은 아이템들만)
            if (itemSlot1.transform.childCount > 0)
            {
                UpdateItemOrderAndActivation();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ItemController - 아이템 이동 중 오류: {e.Message}");
        }
    }

    /// <summary>
    /// 첫 번째와 두 번째 아이템의 위치를 바꿉니다.
    /// </summary>
    public void SwapFirstAndSecondItems()
    {
        if (!photonView.IsMine) return;
        
        if (itemSlot1 == null || itemSlot1.transform.childCount < 2)
        {
            Debug.LogWarning("⚠️ ItemController - 아이템이 2개 미만이어서 위치를 바꿀 수 없습니다.");
            return;
        }

        try
        {
            // 첫 번째 아이템(활성화된 아이템)과 두 번째 아이템(비활성화된 아이템) 찾기
            Transform firstChild = itemSlot1.transform.GetChild(itemSlot1.transform.childCount - 1); // 활성화된 아이템
            Transform secondChild = itemSlot1.transform.GetChild(itemSlot1.transform.childCount - 2); // 비활성화된 아이템

            if (firstChild == null || secondChild == null)
            {
                return;
            }

            // 위치 변경
            firstChild.SetAsFirstSibling();

            // 아이템 순서 재정렬 및 활성화 상태 업데이트
            UpdateItemOrderAndActivation();

            Debug.Log("✅ ItemController - 아이템 위치 변경 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ItemController - 아이템 위치 변경 중 오류: {e.Message}");
        }
    }

    /// <summary>
    /// 첫 번째 활성화된 아이템 가져오기
    /// </summary>
    /// <returns>첫 번째 활성화된 아이템, 없으면 null</returns>
    public CharacterItem GetFirstActiveItem()
    {
        if (itemSlot1 == null || itemSlot1.transform.childCount == 0)
        {
            Debug.LogWarning($"⚠️ ItemController - GetFirstActiveItem: itemSlot1이 null이거나 자식이 없음. childCount: {(itemSlot1 != null ? itemSlot1.transform.childCount : 0)}");
            return null;
        }

        // 마지막 자식(가장 아래)을 첫 번째 아이템으로 처리
        Transform lastChild = itemSlot1.transform.GetChild(itemSlot1.transform.childCount - 1);
        if (lastChild == null || !lastChild.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"⚠️ ItemController - GetFirstActiveItem: 마지막 자식이 null이거나 비활성화. lastChild: {(lastChild != null ? lastChild.name : "null")}, active: {(lastChild != null ? lastChild.gameObject.activeInHierarchy : false)}");
            return null;
        }

        CharacterItem item = lastChild.GetComponent<CharacterItem>();
        if (item == null)
        {
            Debug.LogWarning($"⚠️ ItemController - GetFirstActiveItem: 마지막 자식에 CharacterItem 컴포넌트가 없음. lastChild: {lastChild.name}");
            return null;
        }

        Debug.Log($"✅ ItemController - GetFirstActiveItem: {item.SkillName} 반환 (자식 {itemSlot1.transform.childCount}개 중 마지막)");
        return item;
    }

    /// <summary>
    /// 활성화된 아이템 개수 가져오기
    /// </summary>
    /// <returns>활성화된 아이템 개수</returns>
    public int GetActiveItemCount()
    {
        if (itemSlot1 == null)
        {
            return 0;
        }

        int activeCount = 0;
        for (int i = 0; i < itemSlot1.transform.childCount; i++)
        {
            Transform child = itemSlot1.transform.GetChild(i);
            if (child != null && child.gameObject.activeInHierarchy)
            {
                activeCount++;
            }
        }

        return activeCount;
    }

    /// <summary>
    /// 특정 아이템이 첫 번째 아이템인지 확인
    /// </summary>
    /// <param name="characterItem">확인할 아이템</param>
    /// <returns>첫 번째 아이템 여부</returns>
    public bool IsFirstActiveItem(CharacterItem characterItem)
    {
        if (characterItem == null) 
        {
            Debug.LogWarning("⚠️ ItemController - IsFirstActiveItem: characterItem이 null");
            return false;
        }
        
        // 실제 활성화된 아이템을 찾기
        if (itemSlot1 == null) 
        {
            Debug.LogWarning("⚠️ ItemController - IsFirstActiveItem: itemSlot1이 null");
            return false;
        }
        
        Debug.Log($"🔍 ItemController - IsFirstActiveItem 검사: {characterItem.SkillName}");
        
        for (int i = 0; i < itemSlot1.transform.childCount; i++)
        {
            Transform child = itemSlot1.transform.GetChild(i);
            if (child != null && child.gameObject.activeInHierarchy)
            {
                CharacterItem activeItem = child.GetComponent<CharacterItem>();
                if (activeItem != null)
                {
                    Debug.Log($"  - 활성 아이템 {i}: {activeItem.SkillName}");
                    if (activeItem == characterItem)
                    {
                        Debug.Log($"✅ ItemController - IsFirstActiveItem: {characterItem.SkillName}이 첫 번째 활성 아이템임");
                        return true;
                    }
                }
            }
        }
        
        Debug.LogWarning($"⚠️ ItemController - IsFirstActiveItem: {characterItem.SkillName}이 첫 번째 활성 아이템이 아님");
        return false;
    }

    #endregion

    #region 아이템 정보 조회

    public int GetItemIndex()
    {
        return currentItemIndex;
    }

    public GameObject GetItemPrefab(int index)
    {
        return cachedItemPrefab[index];
    }

    public int GetItemSlotIndex()
    {
        return currentItemSlotIndex;
    }

    public int GetMaxItemSlot()
    {
        return cachedMaxItemSlot;
    }

    /// <summary>
    /// ItemSlot1 가져오기
    /// </summary>
    /// <returns>ItemSlot1 Transform</returns>
    public Transform GetItemSlot1()
    {
        return itemSlot1 != null ? itemSlot1.transform : null;
    }

    /// <summary>
    /// 특정 SkillName을 가진 아이템이 이미 보유하고 있는지 확인
    /// </summary>
    /// <param name="skillName">확인할 아이템의 SkillName</param>
    /// <returns>이미 보유하고 있으면 true, 없으면 false</returns>
    public bool HasItemBySkillName(string skillName)
    {
        if (itemSlot1 == null || string.IsNullOrEmpty(skillName))
        {
            return false;
        }

        // itemSlot1의 모든 자식을 확인하여 SkillName 비교
        for (int i = 0; i < itemSlot1.transform.childCount; i++)
        {
            Transform child = itemSlot1.transform.GetChild(i);
            if (child == null) continue;

            CharacterItem characterItem = child.GetComponent<CharacterItem>();
            if (characterItem != null && characterItem.SkillName == skillName)
            {
                Debug.Log($"✅ ItemController - 중복 아이템 발견: {skillName}");
                return true;
            }
        }

        return false;
    }

    #endregion
}
