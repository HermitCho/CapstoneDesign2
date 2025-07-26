using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대시 스킬 클래스
/// 캐릭터를 앞방향으로 빠르게 도약시키는 일회성 스킬
/// </summary>
public class DashItem : CharacterItem
{
    #region Serialized Fields

    [Header("대시 설정")]
    [SerializeField] private float dashDuration = 0.3f; // 대시 지속시간\
    [SerializeField] private float dashForce = 3f; // 대시 힘
    [SerializeField] private AnimationCurve dashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 대시 곡선

    [Header("대시 효과")]
    [SerializeField] private TrailRenderer dashTrail; // 대시 궤적
    [SerializeField] private ParticleSystem dashParticles; // 대시 파티클

    #endregion

    #region Private Fields

    private Rigidbody playerRigidbody;
    private Transform playerTransform;
    private Vector3 dashDirection;
    private bool isDashing = false;


    #endregion

    #region Unity Lifecycle

    protected override void Start()
    {
        base.Start();
        InitializeDashSkill();

        
        // 테스트용: 대시 스킬을 자동으로 구매 상태로 설정
        PurchaseItemSkill();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// 대시 스킬 초기화
    /// </summary>
    private void InitializeDashSkill()
    {
        // 컴포넌트 참조 가져오기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        playerRigidbody = playerObj.GetComponent<Rigidbody>();
        playerTransform = playerObj.transform;

        // 대시 궤적 초기화
        if (dashTrail != null)
        {
            dashTrail.enabled = false;
        }

        // 스킬 기본 정보 설정
        duration = dashDuration;
        castTime = 0f; // 즉시 발동
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// 스킬 사용 가능 여부를 확인합니다.
    /// </summary>
    /// <returns>사용 가능 여부</returns>
    protected override bool CheckCanUse()
    {
        // 대시 중이 아닐 때만 사용 가능
        return !isDashing && playerRigidbody != null;
    }

    /// <summary>
    /// 실제 대시 효과를 실행합니다.
    /// </summary>
    protected override void ExecuteSkill()
    {
        base.ExecuteSkill();
        StartCoroutine(DashRoutine());
    }

    /// <summary>
    /// 대시 실행 루틴
    /// </summary>
    /// <returns>코루틴</returns>
    private IEnumerator DashRoutine()
    {
        isDashing = true;
        dashDirection = playerTransform.forward;
        OnDashStart();


        // 대시 궤적/파티클
        if (dashTrail != null) dashTrail.enabled = true;
        if (dashParticles != null) dashParticles.Play();

        // 기존 속도 제거 후 힘 가하기
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            float elapsedTime = 0f;
            while (elapsedTime < dashDuration)
            {
                float progress = elapsedTime / dashDuration;
                float curveValue = dashCurve.Evaluate(progress);
                // 힘을 곡선에 따라 가중치로 적용
                playerRigidbody.AddForce(dashDirection * dashForce * curveValue, ForceMode.VelocityChange);
                elapsedTime += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            // 대시 종료 후 속도 0으로
            playerRigidbody.velocity = Vector3.zero;
        }

        // 대시 궤적/파티클 종료
        OnDashEnd();
        isDashing = false;
    }

    /// <summary>
    /// 대시 시작 시 호출
    /// </summary>
    private void OnDashStart()
    {
    }

    /// <summary>
    /// 대시 종료 시 호출
    /// </summary>
    private void OnDashEnd()
    {
        // 대시 궤적 비활성화
        if (dashTrail != null)
        {
            dashTrail.enabled = false;
        }
        
        // 대시 파티클 정지
        if (dashParticles != null)
        {
            dashParticles.Stop();
        }
    }

    /// <summary>
    /// 아이템 스킬 사용 시 호출
    /// </summary>
    protected override void OnItemSkillUsed()
    {
        base.OnItemSkillUsed();
    }

    #endregion

    #region Public Methods



    /// <summary>
    /// 대시 지속시간을 설정합니다.
    /// </summary>
    /// <param name="duration">대시 지속시간</param>
    public void SetDashDuration(float duration)
    {
        dashDuration = Mathf.Max(0.1f, duration);
    }

    #endregion

    #region Utility Methods


    #endregion
}
