using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using Photon.Pun;

public class ShopStand : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private TextMeshPro itemPriceText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshPro itemRenewTimeText;
    [SerializeField] private GameObject descriptionPanel;

    [Header("구매 프로그레스 UI")]
    [SerializeField] private GameObject purchaseProgressPanel;
    [SerializeField] private Image purchaseProgressFill;

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
    private bool isPurchased = false;

    // 구매 진행 상태
    private bool isPurchaseInProgress = false;
    private bool isPlayingPurchaseAnimation = false;
    private ShopController currentPurchaser = null;

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

        // 프로그레스 초기화
        ResetPurchaseProgress();
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
        item.transform.localRotation = Quaternion.Euler(90f, 0, 90f); // z축으로 90도 회전

        // 아이템에 회전 애니메이션 컴포넌트 추가
        ItemRotator rotator = item.GetComponent<ItemRotator>();
        if (rotator == null)
        {
            rotator = item.AddComponent<ItemRotator>();
        }

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
            // 구매 애니메이션 중이면 StopPurchaseAnimation에서 정리하도록 위임
            if (isPlayingPurchaseAnimation)
            {
                Debug.Log("ShopStand: 구매 애니메이션 중이므로 ClearCurrentItem 지연 처리");
                currentItem = null;
                currentItemComponent = null;
                currentItemSkill = null;
                return;
            }

            // 진행 중인 구매가 있다면 취소
            if (isPurchaseInProgress)
            {
                CancelPurchaseProgress();
            }

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

            // 타이머는 비활성화하지 않음 - Shop.cs에서 관리
            // isRenewTimerActive = false; // 제거
            // remainingRenewTime = 0f; // 제거
        }
    }

    /// <summary>
    /// 갱신 타이머 업데이트 (Shop.cs에서 SyncRenewTimer로 관리됨)
    /// </summary>
    void UpdateRenewTimer()
    {
        // Shop.cs에서 전달받은 타이머 정보를 기반으로 실시간 UI 업데이트
        // 마스터 클라이언트가 아닌 경우에도 UI 표시를 위해 로컬에서 시간 감소
        if (isRenewTimerActive && remainingRenewTime > 0f)
        {
            remainingRenewTime -= Time.deltaTime;

            // 0 이하로 내려가지 않도록 제한
            if (remainingRenewTime < 0f)
            {
                remainingRenewTime = 0f;
            }
        }

        // 디버깅용: 5초마다 현재 타이머 상태 로그
        if (Time.time % 5f < 0.1f && isRenewTimerActive)
        {
            //Debug.Log($"ShopStand: 갱신 타이머 상태 - 남은시간: {remainingRenewTime:F1}초, 활성: {isRenewTimerActive}");
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
            itemPriceText.text = $"x {currentItemSkill.Price}";
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

        // 타이머가 활성화되어 있으면 항상 표시 (아이템이 있어도 없어도)
        if (isRenewTimerActive && remainingRenewTime > 0f)
        {
            int remainingSeconds = Mathf.CeilToInt(remainingRenewTime);
            itemRenewTimeText.text = $"{remainingSeconds}초";
        }
        else if (currentItem != null)
        {
            // 아이템이 있지만 타이머가 비활성화된 경우 (초기 상태)
            int remainingSeconds = Mathf.CeilToInt(remainingRenewTime);
            itemRenewTimeText.text = $"{remainingSeconds}초";
        }
        else
        {
            // 아이템도 없고 타이머도 비활성화된 경우
            itemRenewTimeText.text = "0초";
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

        Debug.Log($"[ShopStand] TryPurchaseItem 호출됨 by {PhotonNetwork.LocalPlayer.NickName} - Time: {Time.time}");
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

    /// <summary>
    /// 갱신 타이머 설정 (Shop.cs에서 호출)
    /// </summary>
    /// <param name="remainingTime">남은 시간</param>
    /// <param name="isActive">타이머 활성 상태</param>
    public void SetRenewTimer(float remainingTime, bool isActive)
    {
        remainingRenewTime = remainingTime;
        isRenewTimerActive = isActive;
    }

    #region 구매 프로그레스 관리

    /// <summary>
    /// 구매 프로그레스 시작
    /// </summary>
    /// <param name="purchaser">구매자</param>
    /// <param name="holdTime">구매 완료까지 필요한 시간</param>
    public void StartPurchaseProgress(ShopController purchaser, float holdTime)
    {
        if (isPurchaseInProgress || isPlayingPurchaseAnimation) return;
        if (currentItem == null) return;

        isPurchaseInProgress = true;
        currentPurchaser = purchaser;
        purchaseHoldTime = holdTime;


        // 프로그레스 초기화
        ResetPurchaseProgress();

        Debug.Log($"ShopStand: 구매 프로그레스 시작 - {purchaser.name}, 시간: {holdTime}초");
    }

    /// <summary>
    /// 구매 프로그레스 업데이트
    /// </summary>
    /// <param name="progress">진행률 (0~1)</param>
    public void UpdatePurchaseProgress(float progress)
    {
        if (!isPurchaseInProgress) return;

        progress = Mathf.Clamp01(progress);

        if (purchaseProgressFill != null)
        {
            purchaseProgressFill.fillAmount = progress;
        }

        // 프로그레스가 완료되면 구매 완료 처리
        if (progress >= 1f)
        {
            CompletePurchase();
        }
    }

    /// <summary>
    /// 구매 프로그레스 취소
    /// </summary>
    public void CancelPurchaseProgress()
    {
        if (!isPurchaseInProgress) return;

        Debug.Log("ShopStand: 구매 프로그레스 취소");

        isPurchaseInProgress = false;
        currentPurchaser = null;

        ResetPurchaseProgress();
    }

    /// <summary>
    /// 구매 완료 처리
    /// </summary>
    private void CompletePurchase()
    {
        if (!isPurchaseInProgress || currentPurchaser == null) return;

        Debug.Log("ShopStand: 구매 완료!");

        isPurchaseInProgress = false;
        isPlayingPurchaseAnimation = true;

        // 구매 애니메이션 시작
        StartCoroutine(PlayPurchaseAnimation());

        // 실제 구매 처리
        isPurchased = TryPurchaseItem(currentPurchaser);
        if (!isPurchased)
        {
            // 구매 실패 시 애니메이션 중단
            StopPurchaseAnimation();
        }
    }

    /// <summary>
    /// 구매 애니메이션 재생
    /// </summary>
    private IEnumerator PlayPurchaseAnimation()
    {
        Debug.Log("ShopStand: 구매 애니메이션 시작");

        // 프로그레스 완료 표시 
        if (purchaseProgressFill != null)
        {
            Color originalColor = purchaseProgressFill.color;
            if (isPurchased)
            {
                purchaseProgressFill.color = Color.red;
            }


            // 흔들림 효과 (DOTween 사용)
            if (purchaseProgressPanel != null)
            {
                Vector3 originalPos = purchaseProgressPanel.transform.localPosition;

                // 간단한 흔들림 효과
                for (int i = 0; i < 8; i++)
                {
                    float offsetX = UnityEngine.Random.Range(-0.1f, 0.1f);
                    float offsetY = UnityEngine.Random.Range(-0.01f, 0.01f);
                    purchaseProgressPanel.transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
                    yield return new WaitForSeconds(0.05f);
                }

                // 원래 위치로 복원
                purchaseProgressPanel.transform.localPosition = originalPos;
            }

            // 색상 복원
            purchaseProgressFill.color = originalColor;
        }

        // 애니메이션 완료 후 정리
        yield return new WaitForSeconds(0.5f);
        StopPurchaseAnimation();
    }

    /// <summary>
    /// 구매 애니메이션 중단
    /// </summary>
    private void StopPurchaseAnimation()
    {
        Debug.Log("ShopStand: 구매 애니메이션 종료");

        isPlayingPurchaseAnimation = false;
        currentPurchaser = null;


        ResetPurchaseProgress();

        // 아이템이 제거된 후 설명 패널도 비활성화
        if (currentItem == null)
        {
            if (descriptionPanel != null)
            {
                descriptionPanel.SetActive(false);
            }

            // 모든 플레이어의 시선 추적 해제
            foreach (ShopController player in lookingPlayers)
            {
                if (player != null)
                {
                    player.OnShopStandStopLooking(this);
                }
            }
            lookingPlayers.Clear();
        }
    }

    /// <summary>
    /// 프로그레스 초기화
    /// </summary>
    private void ResetPurchaseProgress()
    {
        if (purchaseProgressFill != null)
        {
            purchaseProgressFill.fillAmount = 0f;
            purchaseProgressFill.color = Color.white; // 기본 색상으로 초기화
        }
    }

    /// <summary>
    /// 구매 진행 중인지 확인
    /// </summary>
    public bool IsPurchaseInProgress()
    {
        return isPurchaseInProgress;
    }

    /// <summary>
    /// 구매 애니메이션 재생 중인지 확인
    /// </summary>
    public bool IsPlayingPurchaseAnimation()
    {
        return isPlayingPurchaseAnimation;
    }

    #endregion
}

