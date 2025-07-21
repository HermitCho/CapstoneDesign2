using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스킬 시스템의 기본 클래스
/// 캐릭터 스킬과 아이템 스킬의 공통 요소들을 정의
/// </summary>
public abstract class Skill : MonoBehaviour
{
    #region Serialized Fields

    [Header("스킬 기본 정보")]
    [SerializeField] protected string skillName = "기본 스킬";
    [SerializeField] protected string skillDescription = "스킬 설명";
    [SerializeField] protected float duration = 0f; // 지속시간 (0이면 즉시 발동)
    [SerializeField] protected float castTime = 0f; // 시전 시간

    [Header("UI 요소")]
    [SerializeField] protected Sprite skillIcon; // 스킬 아이콘
    [SerializeField] protected Color skillColor = Color.white; // 스킬 색상

    [Header("시각 효과")]
    [SerializeField] protected ParticleSystem skillEffect; // 스킬 이펙트
    [SerializeField] protected AudioClip skillSound; // 스킬 사운드

    #endregion

    #region Properties

    /// <summary>
    /// 스킬 이름
    /// </summary>
    public string SkillName => skillName;

    /// <summary>
    /// 스킬 설명
    /// </summary>
    public string SkillDescription => skillDescription;

    /// <summary>
    /// 스킬 지속시간
    /// </summary>
    public float Duration => duration;

    /// <summary>
    /// 시전 시간
    /// </summary>
    public float CastTime => castTime;

    /// <summary>
    /// 스킬 아이콘
    /// </summary>
    public Sprite SkillIcon => skillIcon;

    /// <summary>
    /// 스킬 색상
    /// </summary>
    public Color SkillColor => skillColor;

    /// <summary>
    /// 스킬이 현재 활성화되어 있는지 여부
    /// </summary>
    public bool IsActive { get; protected set; }

    /// <summary>
    /// 스킬 사용 가능 여부
    /// </summary>
    public abstract bool CanUse { get; }

    #endregion

    #region Protected Fields

    protected AudioSource audioSource;
    protected bool isInitialized = false;

    #endregion

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        InitializeComponents();
    }

    protected virtual void Start()
    {
        InitializeSkill();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// 컴포넌트들을 초기화합니다.
    /// </summary>
    protected virtual void InitializeComponents()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    /// <summary>
    /// 스킬을 초기화합니다.
    /// </summary>
    protected virtual void InitializeSkill()
    {
        IsActive = false;
        isInitialized = true;
        OnSkillInitialized();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 스킬을 사용합니다.
    /// </summary>
    /// <returns>스킬 사용 성공 여부</returns>
    public virtual bool UseSkill()
    {
        if (!CanUse || !isInitialized)
        {
            Debug.LogWarning($"스킬 '{skillName}' 사용 불가: CanUse={CanUse}, Initialized={isInitialized}");
            return false;
        }
        
        // 시전 시간이 있으면 코루틴으로 처리
        if (castTime > 0f)
        {
            StartCoroutine(CastSkillRoutine());
        }
        else
        {
            ExecuteSkill();
        }

        return true;
    }

    /// <summary>
    /// 스킬을 강제로 중단합니다.
    /// </summary>
    public virtual void CancelSkill()
    {
        if (IsActive)
        {
            Debug.Log($"스킬 '{skillName}' 중단");
            StopAllCoroutines();
            OnSkillCancelled();
            IsActive = false;
        }
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// 시전 시간을 고려한 스킬 사용 루틴
    /// </summary>
    /// <returns>코루틴</returns>
    protected virtual IEnumerator CastSkillRoutine()
    {
        OnSkillCastStart();
        
        yield return new WaitForSeconds(castTime);
        
        ExecuteSkill();
    }

    /// <summary>
    /// 실제 스킬 효과를 실행합니다.
    /// </summary>
    protected virtual void ExecuteSkill()
    {
        IsActive = true;
        
        PlaySkillEffects();
        OnSkillExecuted();
        
        // 지속시간이 있으면 코루틴으로 처리
        if (duration > 0f)
        {
            StartCoroutine(SkillDurationRoutine());
        }
        else
        {
            OnSkillFinished();
            IsActive = false;
        }
    }

    /// <summary>
    /// 스킬 지속시간을 처리하는 루틴
    /// </summary>
    /// <returns>코루틴</returns>
    protected virtual IEnumerator SkillDurationRoutine()
    {
        yield return new WaitForSeconds(duration);
        
        OnSkillFinished();
        IsActive = false;
    }

    /// <summary>
    /// 스킬 효과를 재생합니다.
    /// </summary>
    protected virtual void PlaySkillEffects()
    {
        // 파티클 이펙트 재생
        if (skillEffect != null)
        {
            skillEffect.Play();
        }

        // 사운드 재생
        if (skillSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(skillSound);
        }
    }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// 스킬 사용 가능 여부를 확인합니다.
    /// 자식 클래스에서 구현해야 합니다.
    /// </summary>
    /// <returns>사용 가능 여부</returns>
    protected abstract bool CheckCanUse();

    #endregion

    #region Virtual Methods (자식 클래스에서 오버라이드 가능)

    /// <summary>
    /// 스킬 초기화 완료 시 호출됩니다.
    /// </summary>
    protected virtual void OnSkillInitialized()
    {
        // 자식 클래스에서 오버라이드하여 사용
    }

    /// <summary>
    /// 스킬 시전 시작 시 호출됩니다.
    /// </summary>
    protected virtual void OnSkillCastStart()
    {
        // 자식 클래스에서 오버라이드하여 사용
    }

    /// <summary>
    /// 스킬 실행 완료 시 호출됩니다.
    /// </summary>
    protected virtual void OnSkillExecuted()
    {
        // 자식 클래스에서 오버라이드하여 사용
    }

    /// <summary>
    /// 스킬 지속시간 종료 시 호출됩니다.
    /// </summary>
    protected virtual void OnSkillFinished()
    {
        // 자식 클래스에서 오버라이드하여 사용
    }

    /// <summary>
    /// 스킬 중단 시 호출됩니다.
    /// </summary>
    protected virtual void OnSkillCancelled()
    {
        // 자식 클래스에서 오버라이드하여 사용
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 스킬 정보를 문자열로 반환합니다.
    /// </summary>
    /// <returns>스킬 정보 문자열</returns>
    public override string ToString()
    {
        return $"스킬: {skillName} (지속시간: {duration}s, 시전시간: {castTime}s, 활성화: {IsActive})";
    }

    #endregion
}
