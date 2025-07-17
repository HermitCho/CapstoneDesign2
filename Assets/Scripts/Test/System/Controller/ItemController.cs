using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemController : MonoBehaviour
{
    #region 아이템 프리팹 참조
    private Transform[] itemPrefabs;

    [Header("최대 아이템 슬롯 수")]
    [SerializeField] private int maxItemSlot = 2;
    #endregion


    #region 데이터베이스 참조
    private DataBase.ItemData itemData;
    #endregion


    #region 캐싱된 값들 (성능 최적화)
    private Transform[] cachedItemPrefab;
    private bool dataBaseCached = false;
    #endregion


    #region 내부 상태 변수
    private int currentItemIndex = -1;
    private int currentItemSlotIndex = -1;
    private Transform itemSlot;
    #endregion


    #region Unity 생명주기
    void Awake()
    {
        CacheDataBaseInfo();
    }   

    void Start()
    {
        FindItemSlot();
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
                dataBaseCached = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("아이템 데이터 캐시 실패: " + e.Message);
        }
    }

    private void FindItemSlot()
    {
        itemSlot = GameObject.FindGameObjectWithTag("ItemSlot").transform;
        
    }
    #endregion


    #region 아이템 컨트롤

    private void AttachItem()
    {
        if(currentItemSlotIndex >= maxItemSlot -1) return;

        itemPrefabs[currentItemSlotIndex].transform.parent = itemSlot;
        currentItemSlotIndex++;
    }

    public void DetachItem()
    {
        if(currentItemSlotIndex <= 0) return;
        currentItemSlotIndex--;
        
    }


    private int GetItemIndex()
    {
        return currentItemIndex;
    }

    private Transform GetItemPrefab(int index)
    {
        return cachedItemPrefab[index];
    }

    #endregion




}
