using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemController : MonoBehaviour
{
    #region 데이터베이스 참조
    private DataBase.ItemData itemData;
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
        CacheDataBaseInfo();
    }   

    void Start()
    {
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
        if(currentItemSlotIndex >= cachedMaxItemSlot) 
        {
            Debug.LogWarning("⚠️ ItemController - 아이템 슬롯이 가득 찼습니다.");
            return;
        }

        if (itemSlot1 == null)
        {
            Debug.LogError("❌ ItemController - ItemSlot을 찾을 수 없습니다.");
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogError("❌ ItemController - 아이템 프리팹이 null입니다.");
            return;
        }

        try
        {
            // 프리팹을 인스턴스화하여 새로운 게임오브젝트 생성
            GameObject itemInstance = Instantiate(itemPrefab, itemSlot1.transform);
            
            // 새로 구매한 아이템을 첫 번째 자식(가장 위)으로 배치
            itemInstance.transform.SetAsFirstSibling();
            
            // 아이템 슬롯 인덱스 증가
            currentItemSlotIndex++;
            
            // 아이템 순서 재정렬 및 활성화 상태 업데이트 (HUDPanel 업데이트 포함)
            UpdateItemOrderAndActivation();
            
            // HUD 패널 즉시 업데이트
            UpdateHUDPanelSafely();
            
            Debug.Log($"✅ ItemController - 아이템 부착 완료: {itemPrefab.name} -> {itemInstance.name} (첫 번째 자식으로 배치)");
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
    /// HUDPanel을 안전하게 업데이트
    /// </summary>
    private void UpdateHUDPanelSafely()
    {
        // 캐싱된 HUDPanel이 없으면 다시 찾기
        if (hudPanel == null)
        {
            FindAndCacheHUDPanel();
        }

        // HUDPanel이 할당되어 있고 활성화되어 있는 경우에만 업데이트
        if (hudPanel != null && hudPanel.gameObject.activeInHierarchy)
        {
            hudPanel.UpdateItemUI();
            Debug.Log("✅ ItemController - HUDPanel 아이템 UI 업데이트 완료");
        }
        else
        {
            // HUDPanel이 비활성화되어 있으면 나중에 OnEnable에서 업데이트됨
            Debug.Log("⚠️ ItemController - HUDPanel이 비활성화되어 있어 업데이트를 건너뜁니다.");
        }
    }

    /// <summary>
    /// 외부에서 HUD 패널 업데이트 요청 (상점에서 나갈 때 등)
    /// </summary>
    public void RequestHUDPanelUpdate()
    {
        // 캐싱된 HUDPanel이 없으면 다시 찾기
        if (hudPanel == null)
        {
            FindAndCacheHUDPanel();
        }
        
        UpdateHUDPanelSafely();
    }

    /// <summary>
    /// 사용된 아이템을 쓰레기통으로 이동
    /// </summary>
    /// <param name="usedItem">사용된 아이템</param>
    public void MoveUsedItemToTemp(GameObject usedItem)
    {
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
            return null;
        }

        // 마지막 자식(가장 아래)을 첫 번째 아이템으로 처리
        Transform lastChild = itemSlot1.transform.GetChild(itemSlot1.transform.childCount - 1);
        if (lastChild == null || !lastChild.gameObject.activeInHierarchy)
        {
            return null;
        }

        return lastChild.GetComponent<CharacterItem>();
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
        if (characterItem == null) return false;
        
        // 실제 활성화된 아이템을 찾기
        if (itemSlot1 == null) return false;
        
        for (int i = 0; i < itemSlot1.transform.childCount; i++)
        {
            Transform child = itemSlot1.transform.GetChild(i);
            if (child != null && child.gameObject.activeInHierarchy)
            {
                CharacterItem activeItem = child.GetComponent<CharacterItem>();
                if (activeItem == characterItem)
                {
                    return true;
                }
            }
        }
        
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

    #endregion
}
