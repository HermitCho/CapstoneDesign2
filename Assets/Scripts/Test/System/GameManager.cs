using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : Singleton<GameManager>
{
    [Header("테디베어 점수 관리")]
    [SerializeField] private float totalTeddyBearScore = 0f;
    private TestTeddyBear currentTeddyBear;
    
    [Header("플레이어 상태 관리 - 자동 할당")]
    [SerializeField] private float playerHealth = 100f;
    [SerializeField] private float maxPlayerHealth = 100f;
    [SerializeField] private LivingEntity player;
    
    // ✅ UI 시스템을 위한 이벤트들
    // 점수 관련 이벤트
    public static event Action<float> OnScoreUpdated;
    public static event Action<float> OnScoreMultiplierUpdated;
    public static event Action<float> OnGameTimeUpdated;
    
    // 테디베어 관련 이벤트
    public static event Action<bool> OnTeddyBearAttachmentChanged;
    public static event Action<float> OnTeddyBearReattachTimeChanged;
    
    // 플레이어 상태 이벤트
    public static event Action<float, float> OnPlayerHealthChanged; // current, max
    
    // 아이템 UI 이벤트
    public static event Action<bool> OnItemUIToggled;
    
    // 크로스헤어 이벤트
    public static event Action<bool> OnCrosshairTargetingChanged;
    
    // 스킬 이벤트 (구현 예정)
    public static event Action<int> OnSkillUsed;
    public static event Action<int, float> OnSkillCooldownStarted;
    
    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<LivingEntity>();
        playerHealth = player.StartingHealth;
        maxPlayerHealth = player.CurrentHealth;
    }

    // Start is called before the first frame update
    void Start()
    {
        // 테디베어 찾기
        FindTeddyBear();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // 테디베어 찾기
    void FindTeddyBear()
    {
        if (currentTeddyBear == null)
        {
            currentTeddyBear = FindObjectOfType<TestTeddyBear>();
            if (currentTeddyBear != null)
            {
                Debug.Log("테디베어를 찾았습니다!");
            }
        }
    }
    
    // 테디베어 점수 업데이트 (TestTeddyBear에서 호출)
    public void UpdateTeddyBearScore(float newScore)
    {
        totalTeddyBearScore = newScore;
        
        // HeatUI에 점수 업데이트 이벤트 발생
        OnScoreUpdated?.Invoke(totalTeddyBearScore);
        
        // 점수 배율도 함께 업데이트
        if (currentTeddyBear != null)
        {
            OnScoreMultiplierUpdated?.Invoke(currentTeddyBear.GetCurrentScoreMultiplier());
        }
    }
    
    // 현재 테디베어 점수 가져오기
    public float GetTeddyBearScore()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.GetCurrentScore();
        }
        return totalTeddyBearScore;
    }
    
    // 현재 점수 배율 가져오기
    public float GetScoreMultiplier()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.GetCurrentScoreMultiplier();
        }
        return 1f;
    }
    
    // 게임 시간 가져오기
    public float GetGameTime()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.GetGameTime();
        }
        return Time.time;
    }
    
    // 테디베어가 부착되어 있는지 확인
    public bool IsTeddyBearAttached()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.IsAttached();
        }
        return false;
    }
    
    // 테디베어 재부착까지 남은 시간 가져오기
    public float GetTimeUntilReattach()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.GetTimeUntilReattach();
        }
        return 0f;
    }
    
    // 테디베어 재부착 가능 여부 확인
    public bool CanTeddyBearReattach()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.CanReattach();
        }
        return true;
    }
    
    // 점수 초기화 (개발자용)
    public void ResetAllScores()
    {
        totalTeddyBearScore = 0f;
        if (currentTeddyBear != null)
        {
            currentTeddyBear.ResetScore();
        }
        OnScoreUpdated?.Invoke(0f);
        OnScoreMultiplierUpdated?.Invoke(1f);
    }
    
    // ====================================
    // ✅ 플레이어 체력 관리 메서드들
    // ====================================
    
    /// <summary>
    /// 플레이어 체력 설정
    /// </summary>
    public void SetPlayerHealth(float current, float max)
    {
        playerHealth = Mathf.Clamp(current, 0f, max);
        maxPlayerHealth = Mathf.Max(max, 1f);
        
        OnPlayerHealthChanged?.Invoke(playerHealth, maxPlayerHealth);
        Debug.Log($"플레이어 체력 설정: {playerHealth}/{maxPlayerHealth}");
    }
    
    /// <summary>
    /// 플레이어 현재 체력만 변경
    /// </summary>
    public void SetPlayerCurrentHealth(float health)
    {
        SetPlayerHealth(health, maxPlayerHealth);
    }
    
    /// <summary>
    /// 플레이어 체력 변경 (데미지/힐)
    /// </summary>
    public void ChangePlayerHealth(float amount)
    {
        SetPlayerHealth(playerHealth + amount, maxPlayerHealth);
    }
    
    /// <summary>
    /// 플레이어 체력 정보 가져오기
    /// </summary>
    public float GetPlayerHealth() => playerHealth;
    public float GetMaxPlayerHealth() => maxPlayerHealth;
    public float GetPlayerHealthRatio() => playerHealth / maxPlayerHealth;
    
    // ====================================
    // ✅ 테디베어 상태 이벤트 발생 메서드들
    // ====================================
    
    /// <summary>
    /// 테디베어 부착 상태 변경 알림
    /// </summary>
    public void NotifyTeddyBearAttachmentChanged(bool isAttached)
    {
        OnTeddyBearAttachmentChanged?.Invoke(isAttached);
        Debug.Log($"테디베어 부착 상태 변경: {isAttached}");
    }
    
    /// <summary>
    /// 테디베어 재부착 시간 변경 알림
    /// </summary>
    public void NotifyTeddyBearReattachTime(float timeRemaining)
    {
        OnTeddyBearReattachTimeChanged?.Invoke(timeRemaining);
    }
    
    /// <summary>
    /// 게임 시간 업데이트 알림
    /// </summary>
    public void NotifyGameTimeUpdated(float gameTime)
    {
        OnGameTimeUpdated?.Invoke(gameTime);
    }
    
    // ====================================
    // ✅ UI 상태 관리 메서드들
    // ====================================
    
    /// <summary>
    /// 아이템 UI 토글 알림
    /// </summary>
    public void NotifyItemUIToggled(bool isOpen)
    {
        OnItemUIToggled?.Invoke(isOpen);
        Debug.Log($"아이템 UI 토글: {isOpen}");
    }
    
    /// <summary>
    /// 크로스헤어 타겟팅 상태 변경 알림
    /// </summary>
    public void NotifyCrosshairTargeting(bool isTargeting)
    {
        OnCrosshairTargetingChanged?.Invoke(isTargeting);
        Debug.Log($"크로스헤어 타겟팅 상태: {isTargeting}");
    }
    
    // ====================================
    // ✅ 스킬 시스템 메서드들 (구현 예정)
    // ====================================
    
    /// <summary>
    /// 스킬 사용 알림
    /// </summary>
    public void NotifySkillUsed(int skillIndex)
    {
        OnSkillUsed?.Invoke(skillIndex);
        Debug.Log($"스킬 {skillIndex} 사용됨");
    }
    
    /// <summary>
    /// 스킬 쿨다운 시작 알림
    /// </summary>
    public void NotifySkillCooldownStarted(int skillIndex, float cooldownTime)
    {
        OnSkillCooldownStarted?.Invoke(skillIndex, cooldownTime);
        Debug.Log($"스킬 {skillIndex} 쿨다운 시작: {cooldownTime}초");
    }
}
