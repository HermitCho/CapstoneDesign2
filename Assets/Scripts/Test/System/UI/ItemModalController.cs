using UnityEngine;
using Michsky.UI.Heat;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 아이템 모달창 관리 컨트롤러
/// 아이템 선택 모달창의 UI 업데이트 및 아이템 선택 기능을 담당
/// </summary>
public class ItemModalController : MonoBehaviour
{
    #region 인스펙터 할당 변수
    [Header("HUD 참조")]
    [SerializeField] private HUDPanel hudPanel;

    [Header("모달창 관리")]
    [SerializeField] private ModalWindowManager modalWindowManager;

    [Header("아이템 버튼 관리")]
    [SerializeField] private BoxButtonManager itemButton1; // 아이템 변경 버튼

    [Header("선택된 아이템 정보")]
    [SerializeField] private Image selectedItemIcon;
    [SerializeField] private TextMeshProUGUI selectedItemName;
    [SerializeField] private TextMeshProUGUI selectedItemDescription;
    [SerializeField] private Sprite emptyItemIcon;
    #endregion

    #region 내부 상태 변수
    private bool isInitialized = false; // 초기화 여부
    private ItemController cachedPlayerItemController; // 캐싱된 ItemController
    #endregion

    #region Unity 생명주기

    void Awake()
    {
        InitializeItemModalController();
    }

    void OnEnable()
    {
        if (isInitialized)
        {
            // ItemController 찾아서 캐싱
            FindAndCachePlayerItemController();
            UpdateModalItemUI();
        }
    }

    void Start()
    {
    }

    #endregion

    #region 초기화

    /// <summary>
    /// 아이템 모달 컨트롤러 초기화
    /// </summary>
    private void InitializeItemModalController()
    {
        try
        {
            // 컴포넌트 검증
            ValidateComponents();

            // 이벤트 구독
            SubscribeToEvents();

            isInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ItemModalController - 초기화 중 오류: {e.Message}");
        }
    }

    /// <summary>
    /// 필수 컴포넌트 검증
    /// </summary>
    private void ValidateComponents()
    {
        if (modalWindowManager == null)
        {
            Debug.LogError("❌ ItemModalController - ModalWindowManager가 할당되지 않았습니다.");
        }

        if (itemButton1 == null)
        {
            Debug.LogError("❌ ItemModalController - itemButton1이 할당되지 않았습니다.");
        }

        if (selectedItemIcon == null)
        {
            Debug.LogError("❌ ItemModalController - selectedItemIcon이 할당되지 않았습니다.");
        }

        if (selectedItemName == null)
        {
            Debug.LogError("❌ ItemModalController - selectedItemName이 할당되지 않았습니다.");
        }

        if (selectedItemDescription == null)
        {
            Debug.LogError("❌ ItemModalController - selectedItemDescription이 할당되지 않았습니다.");
        }

        // playerItemController는 인스펙터에서 제거되었으므로 검증 제거
    }

    /// <summary>
    /// 이벤트 구독
    /// </summary>
    private void SubscribeToEvents()
    {
        // ModalWindowManager 이벤트 구독
        if (modalWindowManager != null)
        {
            modalWindowManager.onOpen.AddListener(OnModalWindowOpened);
            modalWindowManager.onClose.AddListener(OnModalWindowClosed);
        }

        // BoxButtonManager 클릭 이벤트 구독
        if (itemButton1 != null)
        {
            itemButton1.onClick.AddListener(() => OnItemButtonClick());
        }
    }

    #endregion

    #region 모달창 이벤트 처리

    /// <summary>
    /// 모달창이 열릴 때 호출
    /// </summary>
    private void OnModalWindowOpened()
    {
        UpdateModalItemUI();
    }

    /// <summary>
    /// 모달창이 닫힐 때 호출
    /// </summary>
    private void OnModalWindowClosed()
    {
        // HUD 패널 업데이트
        UpdateHUDPanel();
    }

    #endregion

    #region 아이템 버튼 이벤트 처리

    /// <summary>
    /// 아이템 버튼 클릭 시 호출
    /// </summary>
    private void OnItemButtonClick()
    {
        // 캐싱된 ItemController가 없으면 다시 찾기
        if (cachedPlayerItemController == null)
        {
            FindAndCachePlayerItemController();
        }

        if (cachedPlayerItemController == null)
        {
            Debug.LogWarning("⚠️ ItemModalController - playerItemController가 null입니다.");
            return;
        }

        try
        {
            // ItemSlot1에서 아이템 개수 확인
            Transform itemSlot = cachedPlayerItemController.GetItemSlot1();
            if (itemSlot == null)
            {
                Debug.LogWarning("⚠️ ItemModalController - ItemSlot1을 찾을 수 없습니다.");
                return;
            }

            // 아이템이 2개 이상 있을 때만 위치 변경
            if (itemSlot.childCount > 1)
            {
                // 아이템 위치 변경
                SwapActiveAndInactiveItems();
                
                // 선택된 아이템 정보 업데이트
                UpdateSelectedItemInfo();
                
                // 버튼 정보 업데이트 (비활성화된 아이템 정보 표시)
                UpdateItemButtonInfo();
                
                // HUD 패널 즉시 업데이트
                UpdateHUDPanel();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ItemModalController - 아이템 클릭 처리 중 오류: {e.Message}");
        }
    }


    #endregion

    #region 아이템 관리

    /// <summary>
    /// 플레이어의 ItemController를 찾아서 캐싱
    /// </summary>
    private void FindAndCachePlayerItemController()
    {
        if (cachedPlayerItemController == null)
        {
            cachedPlayerItemController = FindCurrentPlayerItemController();
        }
    }

    /// <summary>
    /// 현재 플레이어의 ItemController 찾기 (싱글 기반, Photon2 확장 고려)
    /// </summary>
    /// <returns>현재 플레이어의 ItemController</returns>
    private ItemController FindCurrentPlayerItemController()
    {
        // 2. 플레이어 태그로 찾기 (싱글 환경에서는 안전)
        // 나중에 Photon2 환경에서는 PhotonNetwork.LocalPlayer 사용
        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
        if (currentPlayer != null)
        {
            ItemController itemController = currentPlayer.GetComponent<ItemController>();
            if (itemController == null)
            {
                itemController = currentPlayer.GetComponentInChildren<ItemController>();
            }
            if (itemController != null)
            {
                return itemController;
            }
        }

        // 3. FindObjectOfType으로 찾기 (마지막 수단)
        ItemController foundController = FindObjectOfType<ItemController>();
        if (foundController != null)
        {
            return foundController;
        }

        // 4. 캐릭터가 스폰되지 않았거나 찾을 수 없는 경우
        Debug.LogWarning("⚠️ ItemModalController - 플레이어의 ItemController를 찾을 수 없습니다.");
        return null;
    }

    /// <summary>
    /// 활성화된 아이템과 비활성화된 아이템 위치 변경
    /// </summary>
    private void SwapActiveAndInactiveItems()
    {
        if (cachedPlayerItemController == null) return;

        // ItemController의 전용 메서드 사용
        cachedPlayerItemController.SwapFirstAndSecondItems();
        
        Debug.Log("✅ ItemModalController - 아이템 위치 변경 요청 완료");
    }

    /// <summary>
    /// 선택된 아이템 정보 업데이트 (활성화된 아이템 정보 표시)
    /// </summary>
    private void UpdateSelectedItemInfo()
    {
        if (cachedPlayerItemController == null) 
        {
            Debug.LogWarning("⚠️ ItemModalController - playerItemController가 null입니다.");
            ClearSelectedItemInfo();
            return;
        }

        // ItemSlot1에서 활성화된 아이템(첫 번째 아이템) 찾기
        Transform itemSlot = cachedPlayerItemController.GetItemSlot1();
        if (itemSlot == null) 
        {
            Debug.LogWarning("⚠️ ItemModalController - ItemSlot1이 null입니다.");
            ClearSelectedItemInfo();
            return;
        }
        
        if (itemSlot.childCount == 0) 
        {
            Debug.LogWarning("⚠️ ItemModalController - ItemSlot1에 자식이 없습니다.");
            ClearSelectedItemInfo();
            return;
        }

        // 마지막 자식(활성화된 아이템) 찾기
        Transform activeItemTransform = itemSlot.GetChild(itemSlot.childCount - 1);
        if (activeItemTransform == null) 
        {
            Debug.LogWarning("⚠️ ItemModalController - 활성화된 아이템 Transform이 null입니다.");
            ClearSelectedItemInfo();
            return;
        }

        CharacterItem activeItem = activeItemTransform.GetComponent<CharacterItem>();
        if (activeItem == null) 
        {
            Debug.LogWarning("⚠️ ItemModalController - 활성화된 아이템에 CharacterItem 컴포넌트가 없습니다.");
            ClearSelectedItemInfo();
            return;
        }

        // 선택된 아이템 정보 업데이트
        if (selectedItemIcon != null)
        {
            // 아이템 아이콘이 있으면 해당 아이콘, 없으면 빈 아이콘 표시
            if (activeItem.SkillIcon != null)
            {
                selectedItemIcon.sprite = activeItem.SkillIcon;
                selectedItemIcon.color = activeItem.SkillColor;
                // 알파값을 1로 설정 (완전 불투명)
                Color iconColor = selectedItemIcon.color;
                iconColor.a = 1f;
                selectedItemIcon.color = iconColor;
            }
            else
            {
                selectedItemIcon.sprite = emptyItemIcon;
                selectedItemIcon.color = Color.white;
                // 알파값을 0으로 설정 (완전 투명)
                Color iconColor = selectedItemIcon.color;
                iconColor.a = 0f;
                selectedItemIcon.color = iconColor;
                Debug.LogWarning("⚠️ ItemModalController - 아이템 아이콘이 null이므로 빈 아이콘을 표시합니다.");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ ItemModalController - selectedItemIcon이 null입니다.");
        }

        if (selectedItemName != null)
        {
            selectedItemName.text = !string.IsNullOrEmpty(activeItem.SkillName) ? activeItem.SkillName : "이름 없는 아이템";
            Debug.Log($"✅ ItemModalController - selectedItemName 업데이트: {activeItem.SkillName}");
        }
        else
        {
            Debug.LogWarning("⚠️ ItemModalController - selectedItemName이 null입니다.");
        }

        if (selectedItemDescription != null)
        {
            selectedItemDescription.text = !string.IsNullOrEmpty(activeItem.SkillDescription) ? activeItem.SkillDescription : "설명이 없습니다.";
            Debug.Log($"✅ ItemModalController - selectedItemDescription 업데이트: {activeItem.SkillDescription}");
        }
        else
        {
            Debug.LogWarning("⚠️ ItemModalController - selectedItemDescription이 null입니다.");
        }
    }

    /// <summary>
    /// 아이템 버튼 정보 업데이트 (비활성화된 아이템 정보 표시)
    /// </summary>
    private void UpdateItemButtonInfo()
    {
        if (itemButton1 == null) 
        {
            Debug.LogWarning("⚠️ ItemModalController - itemButton1이 null입니다.");
            return;
        }
        
        if (cachedPlayerItemController == null) 
        {
            Debug.LogWarning("⚠️ ItemModalController - playerItemController가 null입니다.");
            return;
        }

        // ItemSlot1에서 비활성화된 아이템(두 번째 아이템) 찾기
        Transform itemSlot = cachedPlayerItemController.GetItemSlot1();
        if (itemSlot == null) 
        {
            Debug.LogWarning("⚠️ ItemModalController - ItemSlot1이 null입니다.");
            ClearItemButton(itemButton1);
            return;
        }
        
        if (itemSlot.childCount < 2) 
        {
            Debug.LogWarning("⚠️ ItemModalController - 아이템이 2개 미만입니다.");
            ClearItemButton(itemButton1);
            return;
        }

        // 두 번째 마지막 자식(비활성화된 아이템) 찾기
        Transform inactiveItemTransform = itemSlot.GetChild(itemSlot.childCount - 2);
        if (inactiveItemTransform == null) 
        {
            Debug.LogWarning("⚠️ ItemModalController - 비활성화된 아이템 Transform이 null입니다.");
            ClearItemButton(itemButton1);
            return;
        }

        CharacterItem inactiveItem = inactiveItemTransform.GetComponent<CharacterItem>();
        if (inactiveItem == null) 
        {
            Debug.LogWarning("⚠️ ItemModalController - 비활성화된 아이템에 CharacterItem 컴포넌트가 없습니다.");
            ClearItemButton(itemButton1);
            return;
        }

        // 버튼 정보 업데이트 (비활성화된 아이템 정보 표시)
        if (inactiveItem.SkillIcon != null)
        {
            itemButton1.SetBackground(inactiveItem.SkillIcon);
            // 알파값을 1로 설정 (완전 불투명)
            Color bgColor = itemButton1.backgroundObj.color;
            bgColor.a = 1f;
            itemButton1.backgroundObj.color = bgColor;
        }
        else
        {
            itemButton1.SetBackground(emptyItemIcon); // 아이콘이 없으면 빈 아이콘 표시
            // 알파값을 0으로 설정 (완전 투명)
            Color bgColor = itemButton1.backgroundObj.color;
            bgColor.a = 0f;
            itemButton1.backgroundObj.color = bgColor;
            itemButton1.SetText("아이템이 없습니다.");
            itemButton1.SetDescription("아이템을 구매하세요.");
        }
        
        itemButton1.SetText(!string.IsNullOrEmpty(inactiveItem.SkillName) ? inactiveItem.SkillName : "이름 없는 아이템");
        itemButton1.SetDescription(!string.IsNullOrEmpty(inactiveItem.SkillDescription) ? inactiveItem.SkillDescription : "설명이 없습니다.");
        itemButton1.UpdateUI();

        Debug.Log($"✅ ItemModalController - 버튼 정보 업데이트 완료: {inactiveItem.SkillName} (비활성화됨)");
    }

    /// <summary>
    /// HUD 패널 업데이트
    /// </summary>
    private void UpdateHUDPanel()
    {
        // 업데이트
        if (hudPanel != null && hudPanel.gameObject.activeInHierarchy)
        {
            hudPanel.UpdateItemUI();
            Debug.Log("✅ ItemModalController - HUD 패널 업데이트 완료");
        }
    }

    #endregion

    #region UI 업데이트

    /// <summary>
    /// 모달창 아이템 UI 업데이트
    /// </summary>
    private void UpdateModalItemUI()
    {
        Debug.Log("🔄 ItemModalController - 모달창 아이템 UI 업데이트 시작");
        
        // 캐싱된 ItemController가 없으면 다시 찾기
        if (cachedPlayerItemController == null)
        {
            FindAndCachePlayerItemController();
        }
        
        if (cachedPlayerItemController == null) 
        {
            Debug.LogWarning("⚠️ ItemModalController - playerItemController가 null입니다.");
            return;
        }

        // ItemSlot1의 실제 상태를 기반으로 UI 업데이트
        Transform itemSlot = cachedPlayerItemController.GetItemSlot1();
        if (itemSlot == null) 
        {
            Debug.LogWarning("⚠️ ItemModalController - ItemSlot1이 null입니다.");
            ClearSelectedItemInfo();
            ClearItemButton(itemButton1);
            return;
        }
        
        if (itemSlot.childCount == 0)
        {
            Debug.LogWarning("⚠️ ItemModalController - ItemSlot1에 자식이 없습니다.");
            // 아이템이 없으면 초기화
            ClearSelectedItemInfo();
            ClearItemButton(itemButton1);
            return;
        }

        Debug.Log($"📊 ItemModalController - ItemSlot1 자식 개수: {itemSlot.childCount}");

        // 선택된 아이템 정보 업데이트 (활성화된 아이템)
        UpdateSelectedItemInfo();
        
        // 버튼 정보 업데이트 (비활성화된 아이템)
        UpdateItemButtonInfo();
        
        Debug.Log("✅ ItemModalController - 모달창 아이템 UI 업데이트 완료");
    }

    /// <summary>
    /// 선택된 아이템 정보 초기화
    /// </summary>
    private void ClearSelectedItemInfo()
    {
        if (selectedItemIcon != null)
        {
            selectedItemIcon.sprite = emptyItemIcon; // 빈 아이콘 표시
            selectedItemIcon.color = Color.white; // 빈 아이콘은 흰색으로 표시
            // 알파값을 0으로 설정 (완전 투명)
            Color iconColor = selectedItemIcon.color;
            iconColor.a = 0f;
            selectedItemIcon.color = iconColor;
        }

        if (selectedItemName != null)
        {
            selectedItemName.text = "선택된 아이템 없음";
        }

        if (selectedItemDescription != null)
        {
            selectedItemDescription.text = "사용 가능한 아이템이 없습니다.";
        }
    }

    /// <summary>
    /// 아이템 버튼 초기화
    /// </summary>
    private void InitializeItemButton()
    {
        if (itemButton1 != null)
        {
            // 클릭 시 아이템 변경
            itemButton1.onClick.AddListener(() => OnItemButtonClick());
        }
    }

    /// <summary>
    /// 아이템 버튼 초기화
    /// </summary>
    /// <param name="button">초기화할 버튼</param>
    private void ClearItemButton(BoxButtonManager button)
    {
        if (button == null) return;
        
        button.SetBackground(emptyItemIcon); // 빈 아이콘 표시

        Color bgColor = itemButton1.backgroundObj.color;
        bgColor.a = 0f;
        itemButton1.backgroundObj.color = bgColor;

        button.SetText("  ");
        button.SetDescription("아이템 구매 필요");
        button.UpdateUI();
        
        Debug.Log("🔄 ItemModalController - 아이템 버튼을 빈 아이콘으로 초기화");
    }

    #endregion

    #region 공개 메서드

    /// <summary>
    /// 현재 사용 가능한 아이템 개수 반환
    /// </summary>
    /// <returns>사용 가능한 아이템 개수</returns>
    public int GetAvailableItemCount()
    {
        if (cachedPlayerItemController == null) return 0;
        return cachedPlayerItemController.GetItemSlot1().childCount;
    }

    /// <summary>
    /// 특정 인덱스의 아이템 반환
    /// </summary>
    /// <param name="index">아이템 인덱스</param>
    /// <returns>CharacterItem 또는 null</returns>
    public CharacterItem GetItemAtIndex(int index)
    {
        if (cachedPlayerItemController == null) return null;

        if (index >= 0 && index < cachedPlayerItemController.GetItemSlot1().childCount)
        {
            Transform itemTransform = cachedPlayerItemController.GetItemSlot1().GetChild(index);
            if (itemTransform != null)
            {
                return itemTransform.GetComponent<CharacterItem>();
            }
        }
        return null;
    }

    #endregion
}
