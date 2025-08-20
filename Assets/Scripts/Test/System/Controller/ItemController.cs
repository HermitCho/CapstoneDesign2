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
        AttachItemString(itemPrefab.name);
    }

    //[PunRPC]
    public void AttachItemString(string itemPrefabName)
    {
        if (itemSlot1 == null) return;

        GameObject prefab = null;
        foreach (var go in cachedItemPrefab)
        {
            if (go != null && go.name == itemPrefabName)
            {
                prefab = go;
                break;
            }
        }
        
        if (prefab == null) return;

        try
        {
            GameObject itemInstance = Instantiate(prefab, itemSlot1.transform);
            itemInstance.transform.SetAsFirstSibling();
            currentItemSlotIndex++;
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
    public void UpdateItemOrderAndActivation()
    {
        if (!photonView.IsMine) return;

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
    /// 사용된 아이템을 쓰레기통으로 이동
    /// </summary>
    /// <param name="usedItem">사용된 아이템</param>
    public void MoveUsedItemToTemp(GameObject usedItem)
    {
        if (!photonView.IsMine) return;
        
        if (itemTemp == null)
        {
            return;
        }

        if (usedItem == null)
        {
            return;
        }

        try
        {
            // 사용된 아이템이 실제로 itemSlot1의 자식인지 확인
            if (usedItem.transform.parent != itemSlot1.transform) return;

            // 사용된 아이템이 실제로 활성화되어 있는지 확인
            if (!usedItem.activeInHierarchy) return;

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
        
        if (itemSlot1 == null || itemSlot1.transform.childCount < 2) return;

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
    public Skill GetFirstActiveItem()
    {
        if (itemSlot1 == null || itemSlot1.transform.childCount == 0) return null;
        // 마지막 자식(가장 아래)을 첫 번째 아이템으로 처리
        Transform lastChild = itemSlot1.transform.GetChild(itemSlot1.transform.childCount - 1);

        if (lastChild == null || !lastChild.gameObject.activeInHierarchy) return null;
        Skill item = lastChild.GetComponent<Skill>();

        if (item == null) return null;
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
    public bool IsFirstActiveItem(Skill characterItem)
    {
        if (characterItem == null) return false;
        
        // 실제 활성화된 아이템을 찾기
        if (itemSlot1 == null) return false;
        
        for (int i = 0; i < itemSlot1.transform.childCount; i++)
        {
            Transform child = itemSlot1.transform.GetChild(i);
            if (child != null && child.gameObject.activeInHierarchy)
            {
                Skill activeItem = child.GetComponent<Skill>();
                if (activeItem != null)
                {
                    if (activeItem == characterItem)
                    {
                        return true;
                    }
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

    /// <summary>
    /// 특정 SkillName을 가진 아이템이 이미 보유하고 있는지 확인
    /// </summary>
    /// <param name="index">확인할 아이템의 인덱스</param>
    /// <returns>이미 보유하고 있으면 true, 없으면 false</returns>
    public bool HasItemByIndex(int index)
    {
        if (itemSlot1 == null || index < 0)
        {
            return false;
        }

        // itemSlot1의 모든 자식을 확인하여 SkillName 비교
        for (int i = 0; i < itemSlot1.transform.childCount; i++)
        {
            Transform child = itemSlot1.transform.GetChild(i);
            if (child == null) continue;

            Skill characterItem = child.GetComponent<Skill>();
            if (characterItem != null && characterItem.Index == index)
            {
                return true;
            }
        }

        return false;
    }

    #endregion
}
