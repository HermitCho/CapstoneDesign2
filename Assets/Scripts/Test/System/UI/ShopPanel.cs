using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.Heat;
using Photon.Pun;

public class ShopPanel : MonoBehaviour
{
    [Header("ShopPanel 버튼 컴포넌트들")]
    [SerializeField] private ButtonManager[] ShopButtons;

    [Header("ShopPanel UI 컴포넌트들")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Image itemIcon1;
    [SerializeField] private Image itemIcon2;
    [SerializeField] private Image itemIcon3;
    [SerializeField] private Image itemIcon4;
    [SerializeField] private Image itemIcon5;

    [Header("ShopPanel 아이템 텍스트")]
    [SerializeField] private TextMeshProUGUI itemText1;
    [SerializeField] private TextMeshProUGUI itemText2;
    [SerializeField] private TextMeshProUGUI itemText3;
    [SerializeField] private TextMeshProUGUI itemText4;
    [SerializeField] private TextMeshProUGUI itemText5;

    [Header("ShopPanel 상점 시간 텍스트")]
    [SerializeField] private TextMeshProUGUI shopTimeText;

    [Header("Item Description 모달 컴포넌트")]
    [SerializeField] private ModalWindowManager itemDescriptionModal;

    [Header("ShopPanel 구매된 아이템 표시")]
    [SerializeField] private Sprite purchasedItemImages;
    [SerializeField] private string purchasedItemTexts;

    // 현재 상점 아이템 데이터
    private GameObject[] currentShopItems;
    private bool[] itemPurchasedStatus;
    
    // 로컬 플레이어 캐싱
    private GameObject localPlayer;
    private CoinController localCoinController;
    private ItemController localItemController;
    
    // 버튼 아이템 컴포넌트 캐싱 (모달창 표시용)
    private Skill[] buttonItemComponents = new Skill[5];


    void Start()
    {
        InitializeShopPanel();
    }

    void OnEnable()
    {
        SubscribeToEvents();
        
        // 로컬 플레이어 다시 찾기 (씬 전환 등으로 인한 변경 대응)
        FindLocalPlayer();
        
        // 상점창이 열릴 때마다 최신 데이터로 업데이트
        if (GameManager.Instance != null)
        {
            currentShopItems = GameManager.Instance.GetCurrentShopItems();
            itemPurchasedStatus = GameManager.Instance.GetShopItemPurchasedStatus();
            UpdateShopUI();
        }
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeShopPanel()
    {
        itemPurchasedStatus = new bool[5];
        
        // 로컬 플레이어 찾기 및 캐싱
        FindLocalPlayer();
        
        // GameManager에서 현재 상점 데이터 가져오기
        if (GameManager.Instance != null)
        {
            currentShopItems = GameManager.Instance.GetCurrentShopItems();
            itemPurchasedStatus = GameManager.Instance.GetShopItemPurchasedStatus();
            UpdateShopUI();
        }
    }
    
    /// <summary>
    /// 로컬 플레이어 찾기 및 캐싱 (멀티플레이어 환경 고려)
    /// </summary>
    private void FindLocalPlayer()
    {
        // 이미 찾았고 유효하다면 건너뜀
        if (localPlayer != null && localCoinController != null && localItemController != null)
            return;
            
        // 모든 Player 태그를 가진 오브젝트 찾기
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject player in allPlayers)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                // 로컬 플레이어 발견
                localPlayer = player;
                localCoinController = player.GetComponent<CoinController>();
                localItemController = player.GetComponent<ItemController>();
                
                Debug.Log($"✅ ShopPanel: 로컬 플레이어 찾기 성공 - {player.name}");
                return;
            }
        }
        
        Debug.LogWarning("⚠️ ShopPanel: 로컬 플레이어를 찾을 수 없습니다");
    }

    private float coinUpdateTimer = 0f;
    private const float COIN_UPDATE_INTERVAL = 0.1f; // 0.1초마다 코인 업데이트
    
    void Update()
    {
        // 코인 업데이트 빈도 제한 (성능 최적화)
        coinUpdateTimer += Time.deltaTime;
        if (coinUpdateTimer >= COIN_UPDATE_INTERVAL)
        {
            UpdatePlayerCoinDisplay();
            coinUpdateTimer = 0f;
        }
    }

    private void SubscribeToEvents()
    {
        GameManager.OnShopTimeUpdated += UpdateShopTime;
        GameManager.OnShopItemsUpdated += UpdateShopItems;
        GameManager.OnShopItemPurchased += OnItemPurchased;
    }

    private void UnsubscribeFromEvents()
    {
        GameManager.OnShopTimeUpdated -= UpdateShopTime;
        GameManager.OnShopItemsUpdated -= UpdateShopItems;
        GameManager.OnShopItemPurchased -= OnItemPurchased;
    }

    private void UpdatePlayerCoinDisplay()
    {
        // 캐싱된 로컬 플레이어가 없거나 파괴되었다면 다시 찾기
        if (localPlayer == null || localCoinController == null)
        {
            FindLocalPlayer();
        }
        
        // 캐싱된 CoinController 사용
        if (localCoinController != null)
        {
            int currentCoins = localCoinController.GetCoin();
            if (currentCoins >= 0) // -1이 아닌 경우만 업데이트 (유효한 값)
            {
                UpdateCoinText(currentCoins);
            }
        }
    }

    private void UpdateShopTime(float remainingTime)
    {
        if (shopTimeText != null)
        {
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            shopTimeText.text = $"갱신까지 남은 시간... {seconds:00}";
        }
    }

    private void UpdateShopItems(GameObject[] newShopItems)
    {
        currentShopItems = newShopItems;
        if (GameManager.Instance != null)
        {
            itemPurchasedStatus = GameManager.Instance.GetShopItemPurchasedStatus();
        }
        else
        {
            itemPurchasedStatus = new bool[5];
        }
        UpdateShopUI();
    }

    private void OnItemPurchased(int itemIndex)
    {
        if (itemIndex >= 0 && itemIndex < itemPurchasedStatus.Length)
        {
            itemPurchasedStatus[itemIndex] = true;
            UpdateButtonPurchasedState(itemIndex);
        }
    }

    private void UpdateShopUI()
    {
        if (currentShopItems == null) return;

        // 개별 아이템 UI 업데이트
        UpdateItemUI(0, itemIcon1, itemText1);
        UpdateItemUI(1, itemIcon2, itemText2);
        UpdateItemUI(2, itemIcon3, itemText3);
        UpdateItemUI(3, itemIcon4, itemText4);
        UpdateItemUI(4, itemIcon5, itemText5);
        
        // 아이템 컴포넌트 캐싱 (모달창 표시용)
        CacheItemComponents();
        
        // 버튼 상태 업데이트
        if (ShopButtons != null)
        {
            int maxCount = Mathf.Min(currentShopItems.Length, ShopButtons.Length);
            for (int i = 0; i < maxCount; i++)
            {
                if (ShopButtons[i] != null && currentShopItems[i] != null)
                {
                    Skill itemComponent = currentShopItems[i].GetComponent<Skill>();
                    if (itemComponent != null)
                    {
                        AssignButtonData(ShopButtons[i], itemComponent, i);
                        ShopButtons[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        ShopButtons[i].gameObject.SetActive(false);
                    }
                }
                else if (ShopButtons[i] != null)
                {
                    ShopButtons[i].gameObject.SetActive(false);
                }
                
                UpdateButtonPurchasedState(i);
            }
            
            // 남은 버튼들 비활성화
            for (int i = maxCount; i < ShopButtons.Length; i++)
            {
                if (ShopButtons[i] != null)
                {
                    ShopButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }
    
    /// <summary>
    /// 아이템 컴포넌트 캐싱 (모달창 표시 최적화)
    /// </summary>
    private void CacheItemComponents()
    {
        for (int i = 0; i < buttonItemComponents.Length; i++)
        {
            if (i < currentShopItems.Length && currentShopItems[i] != null)
            {
                buttonItemComponents[i] = currentShopItems[i].GetComponent<Skill>();
            }
            else
            {
                buttonItemComponents[i] = null;
            }
        }
    }
    
    private void UpdateItemUI(int index, Image iconImage, TextMeshProUGUI textComponent)
    {
        if (iconImage == null || textComponent == null) return;
        
        if (index < currentShopItems.Length && currentShopItems[index] != null)
        {
            Skill itemComponent = currentShopItems[index].GetComponent<Skill>();
            if (itemComponent != null)
            {
                // 구매된 아이템인지 확인
                if (index < itemPurchasedStatus.Length && itemPurchasedStatus[index])
                {
                    // 구매된 상태 표시
                    iconImage.sprite = purchasedItemImages;
                    if (purchasedItemTexts != null)
                    {
                        textComponent.text = purchasedItemTexts;
                    }
                    else
                    {
                        textComponent.text = "SOLD OUT";
                    }
                }
                else
                {
                    // 일반 아이템 표시
                    iconImage.sprite = itemComponent.SkillIcon;
                    textComponent.text = itemComponent.Price.ToString();
                }
                
                iconImage.gameObject.SetActive(true);
                textComponent.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
                textComponent.gameObject.SetActive(false);
            }
        }
        else
        {
            iconImage.gameObject.SetActive(false);
            textComponent.gameObject.SetActive(false);
        }
    }

    public void OnHoverItemButton(int index)
    {
        if (itemDescriptionModal == null) return;
        if (index < 0 || index >= buttonItemComponents.Length) return; // buttonItemComponents를 사용하여 범위 확인
        
        // 구매된 아이템은 호버 정보 표시하지 않음
        if (index < itemPurchasedStatus.Length && itemPurchasedStatus[index])
        {
            itemDescriptionModal.CloseWindow();
            return;
        }
        
        Skill itemComponent = buttonItemComponents[index]; // 캐싱된 아이템 컴포넌트 사용
        if (itemComponent != null)
        {
            // ModalWindowManager의 올바른 필드 설정
            itemDescriptionModal.icon = itemComponent.SkillIcon;
            itemDescriptionModal.titleText = itemComponent.SkillName; // 아이템 이름을 제목으로
            itemDescriptionModal.descriptionText = itemComponent.SkillDescription;
            
            // UI 업데이트 후 모달 열기
            itemDescriptionModal.UpdateUI();
            itemDescriptionModal.OpenWindow();
        }
        else
        {
            itemDescriptionModal.CloseWindow();
        }
    }

    public void OnLeaveItemButton()
    {
        if (itemDescriptionModal != null)
        {
            itemDescriptionModal.CloseWindow();
            itemDescriptionModal.UpdateUI();
        }
    }

    public void OnPurchaseItem(int itemIndex)
    {
        if (itemPurchasedStatus[itemIndex])
            return;

        // 캐싱된 로컬 플레이어가 없다면 다시 찾기
        if (localPlayer == null || localCoinController == null || localItemController == null)
        {
            FindLocalPlayer();
        }

        if (localPlayer == null || localCoinController == null || localItemController == null) 
        {
            Debug.LogWarning("⚠️ ShopPanel: 로컬 플레이어 정보를 찾을 수 없어 구매를 진행할 수 없습니다");
            return;
        }

        // GameManager에서 구매 처리 및 동기화를 모두 담당
        GameManager.Instance.PurchaseShopItem(itemIndex, localCoinController, localItemController);
    }

    private void UpdateButtonPurchasedState(int itemIndex)
    {
        if (ShopButtons == null || itemIndex >= ShopButtons.Length || itemIndex >= itemPurchasedStatus.Length) 
            return;

        ButtonManager button = ShopButtons[itemIndex];
        if (button == null) return;

        bool isPurchased = itemPurchasedStatus[itemIndex];
        button.isInteractable = !isPurchased;
        button.UpdateUI();
        
        // 개별 아이템 UI도 업데이트
        switch (itemIndex)
        {
            case 0: UpdateItemUI(0, itemIcon1, itemText1); break;
            case 1: UpdateItemUI(1, itemIcon2, itemText2); break;
            case 2: UpdateItemUI(2, itemIcon3, itemText3); break;
            case 3: UpdateItemUI(3, itemIcon4, itemText4); break;
            case 4: UpdateItemUI(4, itemIcon5, itemText5); break;
        }
    }

    private void AssignButtonData(ButtonManager button, Skill itemComponent, int itemIndex)
    {
        if (button == null || itemComponent == null) return;

        Sprite skillIcon = itemComponent.SkillIcon;
        string skillPrice = itemComponent.Price.ToString();

        button.SetIcon(skillIcon);
        button.SetText(skillPrice);
        button.UpdateUI();
    }

    public void UpdateCoinText(int coinAmount)
    {
        if (coinText != null)
        {
            coinText.text = coinAmount.ToString();
        }
    }

    public void ForceUpdateShopItems(GameObject[] newShopItems)
    {
        currentShopItems = newShopItems;
        if (GameManager.Instance != null)
        {
            itemPurchasedStatus = GameManager.Instance.GetShopItemPurchasedStatus();
        }
        else
        {
            itemPurchasedStatus = new bool[5];
        }
        
        // 아이템 컴포넌트 캐싱 업데이트
        CacheItemComponents();
        
        // 상점창이 활성화되어 있을 때만 UI 업데이트
        if (gameObject.activeInHierarchy)
        {
            UpdateShopUI();
        }
    }

    public void ForceUpdatePurchaseState(int itemIndex)
    {
        if (itemIndex >= 0 && itemIndex < itemPurchasedStatus.Length)
        {
            if (GameManager.Instance != null)
            {
                bool[] latestStatus = GameManager.Instance.GetShopItemPurchasedStatus();
                if (latestStatus != null && itemIndex < latestStatus.Length)
                {
                    itemPurchasedStatus[itemIndex] = latestStatus[itemIndex];
                }
            }
            
            // 상점창이 활성화되어 있을 때만 UI 업데이트
            if (gameObject.activeInHierarchy)
            {
                UpdateButtonPurchasedState(itemIndex);
            }
        }
    }
}
