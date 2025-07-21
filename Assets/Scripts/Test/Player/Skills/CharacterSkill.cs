using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 스킬 클래스
/// 재사용 대기시간이 있고 무제한으로 사용 가능한 스킬
/// </summary>
public class CharacterSkill : Skill
{
    #region Serialized Fields

    [Header("재사용 대기시간 설정")]
    [SerializeField] private float cooldownTime; // 재사용 대기시간 (초)
    [SerializeField] private bool showCooldownUI = true; // UI에 대기시간 표시 여부

    [Header("입력 설정")]
    [SerializeField] private bool useSkillInput = true; // 스킬 입력 사용 여부

    #endregion

    #region Private Fields

    private float lastUseTime = 0f; // 마지막 사용 시간
    private bool isOnCooldown = false; // 대기시간 중인지 여부

    #endregion

    #region Properties

    /// <summary>
    /// 재사용 대기시간
    /// </summary>
    public float CooldownTime => cooldownTime;

    /// <summary>
    /// 현재 남은 대기시간
    /// </summary>
    public float RemainingCooldown
    {
        get
        {
            if (!isOnCooldown) return 0f;
            float elapsed = Time.time - lastUseTime;
            return Mathf.Max(0f, cooldownTime - elapsed);
        }
    }

    /// <summary>
    /// 대기시간 진행률 (0~1)
    /// </summary>
    public float CooldownProgress
    {
        get
        {
            if (!isOnCooldown) return 1f;
            return 1f - (RemainingCooldown / cooldownTime);
        }
    }

    /// <summary>
    /// 스킬 사용 가능 여부 (대기시간 체크 포함)
    /// </summary>
    public override bool CanUse => !isOnCooldown && CheckCanUse();

    #endregion

    #region Unity Lifecycle

    protected override void Start()
    {
        base.Start();
        InitializeCooldown();
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
    /// 대기시간 관련 초기화
    /// </summary>
    private void InitializeCooldown()
    {
        lastUseTime = -cooldownTime; // 시작 시 즉시 사용 가능하도록 설정
        isOnCooldown = false;
    }

    /// <summary>
    /// 입력 이벤트 구독
    /// </summary>
    private void SubscribeToInputEvents()
    {
        if (useSkillInput)
        {
            InputManager.OnSkillPressed += OnSkillInputPressed;
            Debug.Log($"캐릭터 스킬 '{skillName}' 스킬 입력 이벤트 구독");
        }
    }

    /// <summary>
    /// 입력 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromInputEvents()
    {
        if (useSkillInput)
        {
            InputManager.OnSkillPressed -= OnSkillInputPressed;
            Debug.Log($"캐릭터 스킬 '{skillName}' 스킬 입력 이벤트 구독 해제");
        }
    }

    #endregion

    #region Input Event Handlers

    /// <summary>
    /// 스킬 입력 이벤트 처리
    /// </summary>
    private void OnSkillInputPressed()
    {
        if (useSkillInput && CanUse)
        {
            // 상점이 열려있으면 스킬 사용 차단
            ShopController shopController = FindObjectOfType<ShopController>();
            if (shopController != null && shopController.IsShopOpen())
            {
                Debug.Log("⚠️ CharacterSkill - 상점이 열려있어 스킬을 사용할 수 없습니다.");
                return;
            }
            UseSkill();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 스킬을 사용합니다.
    /// </summary>
    /// <returns>스킬 사용 성공 여부</returns>
    public override bool UseSkill()
    {
        if (!CanUse)
        {
            Debug.LogWarning($"캐릭터 스킬 '{skillName}' 사용 불가: 대기시간 중 또는 기타 조건 불충족");
            return false;
        }

        bool success = base.UseSkill();
        
        if (success)
        {
            StartCooldown();
        }

        return success;
    }

    /// <summary>
    /// 대기시간을 강제로 리셋합니다.
    /// </summary>
    public void ResetCooldown()
    {
        isOnCooldown = false;
        lastUseTime = -cooldownTime;
        Debug.Log($"캐릭터 스킬 '{skillName}' 대기시간 리셋");
    }

    /// <summary>
    /// 대기시간을 설정된 시간만큼 줄입니다.
    /// </summary>
    /// <param name="reductionTime">줄일 시간 (초)</param>
    public void ReduceCooldown(float reductionTime)
    {
        if (isOnCooldown)
        {
            lastUseTime += reductionTime;
            Debug.Log($"캐릭터 스킬 '{skillName}' 대기시간 {reductionTime}초 감소");
        }
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// 대기시간을 시작합니다.
    /// </summary>
    protected virtual void StartCooldown()
    {
        lastUseTime = Time.time;
        isOnCooldown = true;
        StartCoroutine(CooldownRoutine());
        
    }

    /// <summary>
    /// 대기시간을 처리하는 코루틴
    /// </summary>
    /// <returns>코루틴</returns>
    protected virtual IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(cooldownTime);
        
        isOnCooldown = false;
        OnCooldownFinished();
    }

    /// <summary>
    /// 스킬 사용 가능 여부를 확인합니다.
    /// </summary>
    /// <returns>사용 가능 여부</returns>
    protected override bool CheckCanUse()
    {
        // 기본적으로 항상 사용 가능 (대기시간은 CanUse에서 체크)
        return true;
    }

    #endregion

    #region Virtual Methods

    /// <summary>
    /// 대기시간 종료 시 호출됩니다.
    /// </summary>
    protected virtual void OnCooldownFinished()
    {
        
        // UI 업데이트가 필요한 경우 여기서 처리
        if (showCooldownUI)
        {
            // UI 업데이트 로직 (필요시 구현)
        }
    }

    /// <summary>
    /// 스킬 실행 완료 시 호출됩니다.
    /// </summary>
    protected override void OnSkillExecuted()
    {
        base.OnSkillExecuted();
    }

    /// <summary>
    /// 스킬 중단 시 호출됩니다.
    /// </summary>
    protected override void OnSkillCancelled()
    {
        base.OnSkillCancelled();
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 스킬 정보를 문자열로 반환합니다.
    /// </summary>
    /// <returns>스킬 정보 문자열</returns>
    public override string ToString()
    {
        string cooldownInfo = isOnCooldown 
            ? $"대기시간: {RemainingCooldown:F1}초 남음" 
            : "사용 가능";
            
        return $"캐릭터 스킬: {skillName} ({cooldownInfo})";
    }

    #endregion
}
