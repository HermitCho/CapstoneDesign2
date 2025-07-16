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

    [Header("아이템 구매 여부 설정")]
    [SerializeField] private bool isPurchased = false; // 아이템(스킬) 구매 여부

    [Header("입력 설정")]
    [SerializeField] private bool useItemInput = true; // 아이템 입력 사용 여부
    #endregion

    #region Private Fields

    private int useCount = 0; // 현재 사용 횟수
    private const int maxUseCount = 1; // 최대 사용 횟수(1회)

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
    /// 스킬 사용 가능 여부 (구매 & 1회 미만 사용)
    /// </summary>
    public override bool CanUse => isPurchased && useCount < maxUseCount && CheckCanUse();

    #endregion

    #region Unity Lifecycle

    protected override void Start()
    {
        base.Start();
        InitializeItemSkill();
        SubscribeToInputEvents();
    }
    protected virtual void OnDisable()
    {
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
    /// 입력 이벤트 구독
    /// </summary>
    private void SubscribeToInputEvents()
    {
        if (useItemInput)
        {
            InputManager.OnItemPressed += OnItemInputPressed;
            Debug.Log($"아이템 스킬 '{skillName}' 아이템 입력 이벤트 구독");
        }
    }

    /// <summary>
    /// 입력 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromInputEvents()
    {
        if (useItemInput)
        {
            InputManager.OnItemPressed -= OnItemInputPressed;
            Debug.Log($"아이템 스킬 '{skillName}' 아이템 입력 이벤트 구독 해제");
        }
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
            Debug.Log($"스킬 입력으로 캐릭터 스킬 '{skillName}' 실행");
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
            Debug.Log($"아이템 스킬 '{skillName}' 사용됨 (총 {useCount}/{maxUseCount}회)");
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
        Debug.Log($"아이템 스킬 '{skillName}' 구매 완료");
    }

    /// <summary>
    /// 아이템(스킬) 사용 상태 리셋
    /// </summary>
    public void ResetItemSkill()
    {
        useCount = 0;
        isPurchased = false;
        Debug.Log($"아이템 스킬 '{skillName}' 상태 리셋");
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
        // 1회 사용 후 추가 처리 필요시 구현
        if (useCount >= maxUseCount)
        {
            Debug.Log($"아이템 스킬 '{skillName}'은(는) 더 이상 사용할 수 없습니다.");
        }
    }

    /// <summary>
    /// 스킬 실행 완료 시 호출됩니다.
    /// </summary>
    protected override void OnSkillExecuted()
    {
        base.OnSkillExecuted();
        Debug.Log($"아이템 스킬 '{skillName}' 실행 완료");
        ResetItemSkill();
    }

    /// <summary>
    /// 스킬 중단 시 호출됩니다.
    /// </summary>
    protected override void OnSkillCancelled()
    {
        base.OnSkillCancelled();
        Debug.Log($"아이템 스킬 '{skillName}' 중단됨");
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