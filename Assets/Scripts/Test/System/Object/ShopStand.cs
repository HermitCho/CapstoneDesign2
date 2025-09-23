using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShopStand : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private TextMeshPro itemPriceText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshPro itemRenewTimeText;
    [SerializeField] private GameObject descriptionPanel;

    [Header("상점 관련")]
    [SerializeField] private Transform itemSpawnPoint;
    
    // 현재 이 상점에 배치된 아이템
    private GameObject currentItem;
    private Item currentItemComponent;
    private Skill currentItemSkill;
    
    // 현재 아이템을 보고 있는 플레이어들
    private HashSet<ShopController> lookingPlayers = new HashSet<ShopController>();
    
    // 갱신 시간 관련
    private float remainingRenewTime = 0f;
    private float purchaseHoldTime = 0f;
    private bool isRenewTimerActive = false;

    void Start()
    {
        InitializeShopStand();
    }

    void Update()
    {
        UpdateRenewTimer();
        UpdateRenewTimeDisplay();
    }

    void InitializeShopStand()
    {
        // 아이템 스폰 포인트가 설정되지 않았다면 자신의 Transform 사용
        if (itemSpawnPoint == null)
        {
            itemSpawnPoint = transform;
        }
        
        // 설명 패널 초기 비활성화
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(false);
        }
        
        Debug.Log($"ShopStand: {gameObject.name} 초기화 완료");
    }

    /// <summary>
    /// 아이템을 이 상점에 배치
    /// </summary>
    /// <param name="item">배치할 아이템</param>
    public void SetItem(GameObject item)
    {
        // 기존 아이템 제거
        ClearCurrentItem();
        
        if (item == null) return;
        
        currentItem = item;
        currentItemComponent = item.GetComponent<Item>();
        
        // Item 컴포넌트에서 itemObject를 통해 Skill 컴포넌트 찾기
        if (currentItemComponent != null)
        {
            GameObject itemObject = currentItemComponent.ItemObject;
            if (itemObject != null)
            {
                currentItemSkill = itemObject.GetComponent<Skill>();
            }
        }
        
        if (currentItemSkill == null)
        {
            Debug.LogError($"ShopStand: {item.name}의 itemObject에 Skill 컴포넌트가 없습니다.");
            return; // Skill 컴포넌트가 없으면 아이템 배치 중단
        }
        
        // 갱신 타이머 시작
        if (currentItemComponent != null)
        {
            remainingRenewTime = currentItemComponent.RenewTime;
            isRenewTimerActive = true;
        }
        
        // 아이템을 스폰 포인트에 배치
        item.transform.SetParent(itemSpawnPoint);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.Euler(0, 0, 90f); // z축으로 90도 회전
        
        // UI 업데이트
        UpdateItemUI();
    }

    /// <summary>
    /// 현재 아이템 제거
    /// </summary>
    public void ClearCurrentItem()
    {
        if (currentItem != null)
        {
            // 모든 플레이어의 시선 추적 해제
            foreach (ShopController player in lookingPlayers)
            {
                if (player != null)
                {
                    player.OnShopStandStopLooking(this);
                }
            }
            lookingPlayers.Clear();
            
            // 설명 패널 비활성화
            if (descriptionPanel != null)
            {
                descriptionPanel.SetActive(false);
            }
            
            currentItem = null;
            currentItemComponent = null;
            currentItemSkill = null;
            
            // 타이머 비활성화
            isRenewTimerActive = false;
            remainingRenewTime = 0f;
        }
    }

    /// <summary>
    /// 갱신 타이머 업데이트
    /// </summary>
    void UpdateRenewTimer()
    {
        if (isRenewTimerActive && remainingRenewTime > 0f)
        {
            remainingRenewTime -= Time.deltaTime;
            
            if (remainingRenewTime <= 0f)
            {
                remainingRenewTime = 0f;
                isRenewTimerActive = false;
                // 타이머 종료 - 실제 갱신은 Shop.cs에서 처리
            }
        }
    }

    /// <summary>
    /// 아이템 UI 업데이트
    /// </summary>
    void UpdateItemUI()
    {
        if (currentItemSkill == null) return;

        // 가격 텍스트 업데이트
        if (itemPriceText != null)
        {
            itemPriceText.text = $"{currentItemSkill.Price}";
        }

        // 설명 텍스트 업데이트
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = currentItemSkill.SkillDescription;
        }
    }

    /// <summary>
    /// 갱신 시간 표시 업데이트
    /// </summary>
    void UpdateRenewTimeDisplay()
    {
        if (itemRenewTimeText == null) return;
        
        if (currentItem != null && isRenewTimerActive)
        {
            int remainingSeconds = Mathf.CeilToInt(remainingRenewTime);
            itemRenewTimeText.text = $"{remainingSeconds}초";
            purchaseHoldTime = remainingSeconds;
        }
        else
        {
            int remainingSeconds = Mathf.CeilToInt(purchaseHoldTime);
            itemRenewTimeText.text = $"{remainingSeconds}초";
        }
    }

    /// <summary>
    /// 플레이어가 아이템을 보기 시작할 때 호출
    /// </summary>
    /// <param name="player">아이템을 보는 플레이어</param>
    public void OnPlayerStartLooking(ShopController player)
    {
        if (player == null || currentItem == null) return;

        lookingPlayers.Add(player);
        
        // 설명 패널 활성화
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 플레이어가 아이템 보기를 중단할 때 호출
    /// </summary>
    /// <param name="player">아이템 보기를 중단한 플레이어</param>
    public void OnPlayerStopLooking(ShopController player)
    {
        if (player == null) return;

        lookingPlayers.Remove(player);
        
        // 아무도 보고 있지 않으면 설명 패널 비활성화
        if (lookingPlayers.Count == 0 && descriptionPanel != null)
        {
            descriptionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 아이템 구매 시도
    /// </summary>
    /// <param name="buyer">구매자</param>
    /// <returns>구매 성공 여부</returns>
    public bool TryPurchaseItem(ShopController buyer)
    {
        if (buyer == null || currentItem == null || currentItemSkill == null) return false;

        // 상점 오브젝트 찾기
        Shop shop = FindObjectOfType<Shop>();
        if (shop == null) return false;

        // 상점을 통해 구매 처리 (모든 클라이언트에서 마스터 클라이언트에게 요청)
        shop.PurchaseItem(currentItem, buyer);
        return true;
    }

    /// <summary>
    /// 현재 아이템을 보고 있는 플레이어 수
    /// </summary>
    /// <returns>보고 있는 플레이어 수</returns>
    public int GetLookingPlayerCount()
    {
        return lookingPlayers.Count;
    }

    /// <summary>
    /// 특정 플레이어가 이 아이템을 보고 있는지 확인
    /// </summary>
    /// <param name="player">확인할 플레이어</param>
    /// <returns>보고 있는지 여부</returns>
    public bool IsPlayerLooking(ShopController player)
    {
        return lookingPlayers.Contains(player);
    }

    /// <summary>
    /// 현재 아이템 가져오기
    /// </summary>
    /// <returns>현재 아이템</returns>
    public GameObject GetCurrentItem()
    {
        return currentItem;
    }

    /// <summary>
    /// 아이템 스폰 포인트 가져오기
    /// </summary>
    /// <returns>아이템 스폰 포인트</returns>
    public Transform GetItemSpawnPoint()
    {
        return itemSpawnPoint;
    }
}
