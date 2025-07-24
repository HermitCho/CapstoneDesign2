using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 생명체의 기본 기능을 담당하는 클래스
/// 체력, 방어막, 데미지 처리, 사망 처리 등을 관리
/// </summary>
public class LivingEntity : MonoBehaviour, IDamageable
{
    [Header("Character Data")]
    [SerializeField] private CharacterData characterData;
    // Health & Shield Properties
    public float StartingHealth { get; private set; }
    public float StartingShield { get; private set; }
    public float CurrentHealth { get; private set; }
    public float CurrentShield { get; private set; }
    public bool IsDead { get; private set; }
    
    [Header("스턴 제어")]
    private MoveController moveController;
    // Events
    public event Action OnDeath;
    public event Action OnRevive;

    #region Unity Lifecycle

    /// <summary>
    /// 오브젝트가 활성화될 때 호출되는 메서드
    /// 초기 상태를 설정합니다.
    /// </summary>
    protected virtual void OnEnable()
    {
        InitializeEntity();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// 생명체의 초기 상태를 설정합니다.
    /// </summary>
    private void InitializeEntity()
    {
        IsDead = false;

        // 체력 초기화
        StartingHealth = characterData.startingHealth;
        CurrentHealth = StartingHealth;

        // 방어막 초기화
        StartingShield = characterData.startingShield;
        CurrentShield = StartingShield;

        moveController = GetComponent<MoveController>();
    }

    #endregion

    #region Damage System

    /// <summary>
    /// 데미지를 받았을 때 호출되는 메서드
    /// </summary>
    /// <param name="damage">받을 데미지량</param>
    /// <param name="hitPoint">피격 위치</param>
    /// <param name="hitNormal">피격 방향</param>
    public virtual void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (IsDead) return;

        LogDamageInfo(damage);
        ProcessDamage(damage);
        CheckDeathCondition();
    }

    /// <summary>
    /// 데미지 정보를 로그로 출력합니다.
    /// </summary>
    private void LogDamageInfo(float damage)
    {
        // Debug.Log($"Damage received: {damage}");
        // Debug.Log($"Current Shield: {CurrentShield}");
        // Debug.Log($"Current Health: {CurrentHealth}");
    }

    /// <summary>
    /// 데미지를 처리합니다. 방어막을 먼저 소모하고, 남은 데미지는 체력에 적용합니다.
    /// </summary>
    /// <param name="damage">처리할 데미지량</param>
    private void ProcessDamage(float damage)
    {
        // // 방어막이 데미지를 완전히 흡수할 수 있는 경우
        // if (CurrentShield >= damage)
        // {
        //     CurrentShield -= damage;
        //     GameManager.Instance.ChangePlayerHealth(-damage);
        //     return;
        // }

        // 방어막이 부족한 경우
        float remainingDamage = damage;// - CurrentShield;
                                       //CurrentShield = 0f;

        // 남은 데미지를 체력에 적용
        CurrentHealth = Mathf.Max(0f, CurrentHealth - remainingDamage);
        GameManager.Instance.ChangePlayerHealth(-remainingDamage);
    }

    /// <summary>
    /// 사망 조건을 확인하고 필요시 사망 처리를 실행합니다.
    /// </summary>
    private void CheckDeathCondition()
    {
        if (CurrentHealth <= 0f && !IsDead)
        {
            Die();
        }
    }

    #endregion

    #region Health Management

    /// <summary>
    /// 체력을 회복합니다.
    /// </summary>
    /// <param name="healAmount">회복할 체력량</param>
    public virtual void RestoreHealth(float healAmount)
    {
        if (IsDead || healAmount <= 0f) return;

        float prevHealth = CurrentHealth;
        CurrentHealth = Mathf.Min(StartingHealth, CurrentHealth + healAmount);
        float actualHealed = CurrentHealth - prevHealth;
        GameManager.Instance.ChangePlayerHealth(actualHealed);
    }

    /// <summary>
    /// 방어막을 회복합니다.
    /// </summary>
    /// <param name="shieldAmount">회복할 방어막량</param>
    public virtual void RestoreShield(float shieldAmount)
    {
        if (IsDead || shieldAmount <= 0f) return;

        CurrentShield = Mathf.Min(StartingShield, CurrentShield + shieldAmount);
    }

    #endregion

    #region Death System

    /// <summary>
    /// 사망 처리를 실행합니다.
    /// </summary>
    /// <returns>사망 처리 성공 여부</returns>
    public virtual bool Die()
    {
        if (IsDead) return false;

        IsDead = true;

        // 사망 이벤트 호출
        OnDeath?.Invoke();

        // 10초 후 부활
        moveController.SetStunned(true);
        StartCoroutine(ReviveAfterDelay(10f));

        return true;
    }

    /// <summary>
    /// 일정 시간 후 부활 처리 코루틴
    /// </summary>
    private IEnumerator ReviveAfterDelay(float delay)
    {
        float remaining = delay;
        while (remaining > 0f)
        {
            Debug.Log($"[부활 대기] 남은 시간: {Mathf.CeilToInt(remaining)}초");
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }
        Revive();
    }

    /// <summary>
    /// 부활 처리: 체력 회복 및 IsDead 해제
    /// </summary>
    public virtual void Revive()
    {
        IsDead = false;
        RestoreHealth(StartingHealth);
        moveController.SetStunned(false);
        OnRevive?.Invoke();
        // 필요시 부활 이벤트 등 추가 가능
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 현재 체력 비율을 반환합니다 (0.0 ~ 1.0).
    /// </summary>
    /// <returns>체력 비율</returns>
    public float GetHealthRatio()
    {
        return StartingHealth > 0f ? CurrentHealth / StartingHealth : 0f;
    }

    /// <summary>
    /// 현재 방어막 비율을 반환합니다 (0.0 ~ 1.0).
    /// </summary>
    /// <returns>방어막 비율</returns>
    public float GetShieldRatio()
    {
        return StartingShield > 0f ? CurrentShield / StartingShield : 0f;
    }

    /// <summary>
    /// 생명체가 살아있는지 확인합니다.
    /// </summary>
    /// <returns>살아있으면 true, 죽어있으면 false</returns>
    public bool IsAlive()
    {
        return !IsDead && CurrentHealth > 0f;
    }

    #endregion
}
