using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 일회용 아이템(스킬) 클래스
/// 1회만 사용할 수 있는 스킬 아이템을 제어
/// </summary>
public class CharacterItem : Skill
{
    #region Serialized Fields

    [Header("아이템 사용 후 삭제 유예 시간 설정")]
    [SerializeField] private float destroyDelay = 20f;

    [Header("아이템 구매 여부 설정")]
    [SerializeField] private bool isPurchased = false; // 아이템(스킬) 구매 여부

    [Header("입력 설정")]
    [SerializeField] private bool useItemInput = true; // 아이템 입력 사용 여부

    [Header("애니메이션 클립 트리거 설정")]
    [SerializeField] private string animationTrigger;

    [Header("아이템 가격 설정")]
    [SerializeField] private int price = 3;
    
    #endregion

    #region Private Fields

    private int useCount = 0; // 현재 사용 횟수
    private const int maxUseCount = 1; // 최대 사용 횟수(1회)
    private ItemController itemController; // ItemController 참조

    #endregion

    #region Properties

    /// <summary>
    /// 아이템(스킬) 구매 여부
    /// </summary>
    public bool IsPurchased => isPurchased;

    /// <summary>
    /// 현재 사용 횟수
    /// </summary>
    public int UseCount => useCount;

    /// <summary>
    /// 최대 사용 횟수
    /// </summary>
    public int MaxUseCount => maxUseCount;

    /// <summary>
    /// 애니메이션 클립 트리거 설정
    /// </summary>
    public string AnimationTrigger => animationTrigger;

    /// <summary>
    /// 스킬 사용 가능 여부 (구매 & 1회 미만 사용)
    /// </summary>
    public override bool CanUse => isPurchased && useCount < maxUseCount && CheckCanUse();

    #endregion

    #region Unity 생명주기

    protected override void Start()
    {
        base.Start();
        InitializeItemSkill();
        FindItemController();
        SubscribeToInputEvents();
    }

    protected virtual void OnEnable()
    {
        SubscribeToInputEvents();
    }

    protected virtual void OnDisable()
    {
        // 비활성화된 아이템은 입력 이벤트 구독 해제
        UnsubscribeFromInputEvents();
    }

    protected virtual void OnDestroy()
    {
        UnsubscribeFromInputEvents();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// 아이템 스킬 관련 초기화
    /// </summary>
    private void InitializeItemSkill()
    {
        useCount = 0;
    }

    /// <summary>
    /// ItemController 찾기 (부모에서 찾기)
    /// </summary>
    private void FindItemController()
    {
        // 부모 오브젝트에서 ItemController 찾기
        Transform parent = transform.parent;
        while (parent != null)
        {
            itemController = parent.GetComponent<ItemController>();
            if (itemController != null)
            {
                break;
            }
            parent = parent.parent;
        }

        if (itemController == null)
        {
            Debug.LogWarning($"⚠️ CharacterItem - ItemController를 찾을 수 없습니다: {gameObject.name}");
        }
    }

    /// <summary>
    /// 입력 이벤트 구독
    /// </summary>
    private void SubscribeToInputEvents()
    {
        // MoveController에서 중앙 집중식으로 아이템 사용을 관리하므로 이벤트 구독 제거
        // if (useItemInput)
        // {
        //     InputManager.OnItemPressed += OnItemInputPressed;
        // }
    }

    /// <summary>
    /// 입력 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromInputEvents()
    {
        // MoveController에서 중앙 집중식으로 아이템 사용을 관리하므로 이벤트 구독 해제 제거
        // if (useItemInput)
        // {
        //     InputManager.OnItemPressed -= OnItemInputPressed;
        // }
    }

    #endregion

    #region Input Event Handlers

    /// <summary>
    /// 스킬 입력 이벤트 처리
    /// </summary>
    private void OnItemInputPressed()
    {
        if (useItemInput && CanUse)
        {
            // 아이템이 실제로 활성화되어 있는지 확인
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            // 상점이 열려있으면 아이템 사용 차단
            ShopController shopController = FindObjectOfType<ShopController>();
            if (shopController != null && shopController.IsShopOpen())
            {
                Debug.Log("⚠️ CharacterItem - 상점이 열려있어 아이템을 사용할 수 없습니다.");
                return;
            }
            
            // ItemController에서 첫 번째 아이템인지 확인
            if (itemController != null)
            {
                if (!itemController.IsFirstActiveItem(this))
                {
                    Debug.Log($"⚠️ CharacterItem - '{skillName}'은 첫 번째 아이템이 아니므로 사용되지 않습니다.");
                    return;
                }
            }
            else
            {
                return;
            }
            UseSkill();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 아이템(스킬)을 사용합니다.
    /// </summary>
    /// <returns>스킬 사용 성공 여부</returns>
    public override bool UseSkill()
    {
        if (!CanUse)
        {
            Debug.LogWarning($"아이템 스킬 '{skillName}' 사용 불가: 구매되지 않았거나 이미 사용됨");
            return false;
        }

        bool success = base.UseSkill();
        if (success)
        {
            useCount++;
            OnItemSkillUsed();
        }
        return success;
    }

    /// <summary>
    /// 아이템(스킬) 구매 처리
    /// </summary>
    public void PurchaseItemSkill()
    {
        isPurchased = true;
        useCount = 0;
    }

    /// <summary>
    /// 아이템(스킬) 사용 상태 리셋
    /// </summary>
    public void ResetItemSkill()
    {
        useCount = 0;
        isPurchased = false;
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// 스킬 사용 가능 여부를 확인합니다.
    /// </summary>
    /// <returns>사용 가능 여부</returns>
    protected override bool CheckCanUse()
    {
        // 추가 조건이 있으면 여기에 구현
        return true;
    }

    /// <summary>
    /// 아이템 스킬 사용 시 호출
    /// </summary>
    protected virtual void OnItemSkillUsed()
    {     
        // 1회 사용 후 ItemController를 통해 쓰레기통으로 이동
        if (useCount >= maxUseCount)
        {
            
            // ItemController를 통해 쓰레기통으로 이동
            if (itemController != null)
            {
                itemController.MoveUsedItemToTemp(gameObject);
                
                // HUD 패널 업데이트 요청
                itemController.RequestHUDPanelUpdate();
            }
            else
            {
                Debug.LogError($"❌ CharacterItem - ItemController가 null입니다: {skillName}");
            }
            
            // 20초 후 파괴 (기존 딜레이 유지)
            Destroy(gameObject, destroyDelay);
        }
    }

    /// <summary>
    /// 스킬 실행 완료 시 호출됩니다.
    /// </summary>
    protected override void OnSkillExecuted()
    {
        base.OnSkillExecuted();
        ResetItemSkill();
    }

    /// <summary>
    /// 스킬 중단 시 호출됩니다.
    /// </summary>
    protected override void OnSkillCancelled()
    {
        base.OnSkillCancelled();
    }

    /// <summary>
    /// 아이템 가격 가져오기
    /// </summary>
    /// <returns>아이템 가격</returns>
    public int GetPrice()
    {
        return price;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 스킬 정보를 문자열로 반환합니다.
    /// </summary>
    /// <returns>스킬 정보 문자열</returns>
    public override string ToString()
    {
        string useInfo = isPurchased
            ? (useCount < maxUseCount ? $"사용 가능 ({maxUseCount - useCount}회 남음)" : "더 이상 사용 불가")
            : "구매 필요";
        return $"아이템 스킬: {skillName} ({useInfo})";
    }

    #endregion
}