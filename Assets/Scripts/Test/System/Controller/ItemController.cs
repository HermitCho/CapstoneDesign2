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
    #endregion

    #region Unity 생명주기
    void Awake()
    {
        CacheDataBaseInfo();
    }   

    void Start()
    {
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
            
            // 아이템 슬롯 인덱스 증가
            currentItemSlotIndex++;
            
            // 아이템 순서 재정렬 및 활성화 상태 업데이트
            UpdateItemOrderAndActivation();
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ItemController - 아이템 부착 중 오류: {e.Message}");
        }
    }

    /// <summary>
    /// 아이템 순서 재정렬 및 활성화 상태 업데이트
    /// </summary>
    private void UpdateItemOrderAndActivation()
    {
        if (itemSlot1 == null) return;

        int childCount = itemSlot1.transform.childCount;
        if (childCount == 0) return;

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
    }

    /// <summary>
    /// 첫 번째 아이템 사용 처리
    /// </summary>
    public void UseFirstItem()
    {
        if (itemSlot1 == null || itemSlot1.transform.childCount == 0)
        {
            Debug.LogWarning("⚠️ ItemController - 사용 가능한 아이템이 없습니다.");
            return;
        }

        // 마지막 자식(가장 아래)을 첫 번째 아이템으로 처리
        Transform lastChild = itemSlot1.transform.GetChild(itemSlot1.transform.childCount - 1);
        if (lastChild == null || !lastChild.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("⚠️ ItemController - 첫 번째 아이템이 활성화되지 않았습니다.");
            return;
        }

        CharacterItem characterItem = lastChild.GetComponent<CharacterItem>();
        if (characterItem == null)
        {
            Debug.LogError("❌ ItemController - 첫 번째 아이템에 CharacterItem 컴포넌트가 없습니다.");
            return;
        }

        Debug.Log($"✅ ItemController - 첫 번째 아이템 사용: {characterItem.SkillName}");
        characterItem.UseSkill();
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
            // 아이템을 쓰레기통으로 이동
            usedItem.transform.SetParent(itemTemp.transform);
            currentItemSlotIndex--;
        
            Debug.Log($"ItemSlot1 자식 수: {itemSlot1.transform.childCount}");
            
            // 아이템 순서 재정렬 및 활성화 상태 업데이트
            UpdateItemOrderAndActivation();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ItemController - 아이템 이동 중 오류: {e.Message}");
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
        
        CharacterItem firstItem = GetFirstActiveItem();
        return firstItem == characterItem;
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

    #endregion
}
