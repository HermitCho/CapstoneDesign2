using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// 대시 스킬 클래스
/// 캐릭터를 앞방향으로 빠르게 도약시키는 일회성 스킬
/// </summary>
public class DashItem : CharacterItem
{
    #region Serialized Fields

    [Header("대시 설정")]
    [SerializeField] private float dashDuration = 0.3f; // 대시 지속시간
    [SerializeField] private float dashForce = 130f; // 대시 힘
    [SerializeField] private AnimationCurve dashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 대시 곡선

    [Header("대시 효과")]
    [SerializeField] private TrailRenderer dashTrail; // 대시 궤적

    #endregion

    #region Private Fields

    private Rigidbody playerRigidbody; // 실제로 이동할 대상(Root Player)의 리지드바디
    private Transform playerTransform; // 실제로 이동할 대상(Root Player)의 트랜스폼
    private MoveController playerMoveController; // 이동 제어
    private PhotonView ownerPhotonView; // 부모의 PV

    private Vector3 dashDirection;
    private bool isDashing = false;

    private PhotonView myPhotonView = null; // 이 스크립트가 부착된 게임 오브젝트의 PhotonView
    private bool isOwner = false;

    #endregion

    #region Unity Lifecycle

    protected override void Start()
    {
        base.Start();
        
        // 이 스크립트의 PhotonView를 가져옵니다.
        myPhotonView = GetComponentInParent<PhotonView>();
        
        // 초기화 시에 오너 여부 확인
        isOwner = myPhotonView.IsMine;
        
        // 대시 스킬 초기화는 모든 클라이언트에서 실행해야 합니다.
        InitializeDashSkill();

        // 오너 클라이언트에서만 초기화 디버그 메시지 출력
        if (isOwner)
        {
            Debug.Log($"[DashItem] Init: pv={(ownerPhotonView != null)}, tr={(playerTransform != null)}, rb={(playerRigidbody != null)}");

            // 테스트용: 대시 스킬을 자동으로 구매 상태로 설정
            PurchaseItemSkill();
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// 대시 스킬 초기화
    /// </summary>
    private void InitializeDashSkill()
    {
        // 부모 객체(플레이어)의 PhotonView, Transform, Rigidbody, MoveController를 찾습니다.
        ownerPhotonView = myPhotonView;
        if (ownerPhotonView != null)
        {
            playerTransform = ownerPhotonView.transform;
            playerRigidbody = ownerPhotonView.GetComponent<Rigidbody>();
            playerMoveController = ownerPhotonView.GetComponent<MoveController>();

            if (isOwner)
            {
                Debug.Log($"[DashItem] 컴포넌트 설정 완료 - Transform: {playerTransform != null}, Rigidbody: {playerRigidbody != null}, MoveController: {playerMoveController != null}");
            }
        }
        else
        {
            if (isOwner)
            {
                Debug.LogError("[DashItem] PhotonView를 찾을 수 없습니다! 대시 기능을 사용할 수 없습니다.");
            }
        }
        
        // 대시 궤적 초기화
        if (dashTrail != null)
        {
            dashTrail.enabled = false;
        }

        // 스킬 기본 정보 설정
        duration = dashDuration;
        castTime = 0f; // 즉시 발동
        if (isOwner)
        {
            Debug.Log($"[DashItem] 초기화 완료 - duration: {duration}, castTime: {castTime}");
        }
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// 스킬 사용 가능 여부를 확인합니다.
    /// </summary>
    /// <returns>사용 가능 여부</returns>
    protected override bool CheckCanUse()
    {
        // 대시 중이 아니고, 이 스크립트가 부착된 플레이어의 소유자일 때만 사용 가능
        return !isDashing && isOwner && playerRigidbody != null && playerTransform != null;
    }

    /// <summary>
    /// 실제 대시 효과를 실행합니다.
    /// </summary>
    protected override void ExecuteSkill()
    {
        // 이 스크립트가 부착된 플레이어의 소유자일 때만 RPC를 호출합니다.
        if (isOwner)
        {
            Debug.Log("[DashItem] RPC 호출 (오너)");
            // 이펙트와 이동을 동시에 RPC로 호출
            photonView.RPC("RPC_ApplyDash", RpcTarget.All);
            photonView.RPC("RPC_PlayDashItemEffects", RpcTarget.All);
        }
    }

    [PunRPC]
    protected void RPC_ApplyDash()
    {
        StartCoroutine(DashRoutine());
    }

    /// <summary>
    /// 대시 실행 루틴
    /// </summary>
    /// <returns>코루틴</returns>
    private IEnumerator DashRoutine()
    {
        Debug.Log("[DashItem] DashRoutine 시작");
        isDashing = true;
        dashDirection = (playerTransform != null ? playerTransform.forward : transform.forward).normalized;

        // 대시 궤적/파티클은 모든 클라이언트에서 동일하게 실행
        if (dashTrail != null) dashTrail.enabled = true;
        if (skillEffect != null) skillEffect.Play();

        // 오너에서만 이동 입력 잠시 차단
        if (isOwner && playerMoveController != null)
        {
            playerMoveController.DisableMovement();
        }

        // 오너에서만 리지드바디로 이동을 수행
        if (isOwner && playerRigidbody != null)
        {
            // 원래 상태 백업
            Vector3 originalVelocity = playerRigidbody.velocity;
            float originalDrag = playerRigidbody.drag;
            float originalAngularDrag = playerRigidbody.angularDrag;
            bool originalUseGravity = playerRigidbody.useGravity;

            // 대시 동안 저항 최소화
            playerRigidbody.drag = 0f;
            playerRigidbody.angularDrag = 0f;
            playerRigidbody.useGravity = false;

            float elapsedTime = 0f;
            while (elapsedTime < dashDuration)
            {
                float progress = dashDuration > 0f ? elapsedTime / dashDuration : 1f;
                float curveValue = dashCurve != null ? dashCurve.Evaluate(progress) : 1f;
                Vector3 targetVelocity = dashDirection * (dashForce * curveValue);
                playerRigidbody.velocity = targetVelocity;

                Debug.Log($"[DashItem] 대시 진행: {progress:F2}, 속도: {targetVelocity}");

                elapsedTime += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            // 원래 상태 복구
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.drag = originalDrag;
            playerRigidbody.angularDrag = originalAngularDrag;
            playerRigidbody.useGravity = originalUseGravity;

            Debug.Log("[DashItem] 대시 완료 - 원래 상태 복구");
        }
        else
        {
            // 오너가 아닌 클라이언트는 이동 수행 없음 (이펙트만 재생)
            Debug.Log("[DashItem] 타 클라이언트 - 이펙트만 재생");
            yield return new WaitForSeconds(dashDuration);
        }

        // 이동 입력 복구
        if (isOwner && playerMoveController != null)
        {
            playerMoveController.EnableMovement();
            Debug.Log("[DashItem] 이동 입력 활성화");
        }

        // 대시 궤적/파티클 종료
        OnDashEnd();
        isDashing = false;

        Debug.Log("[DashItem] DashRoutine 완료");
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
        if (skillEffect != null)
        {
            skillEffect.Stop();
        }
    }

    [PunRPC]
    private void RPC_PlayDashItemEffects()
    {
        if (skillEffect != null)
        {
            skillEffect.Play();
        }

        if (skillSound != null)
        {
            AudioManager.Inst.PlayClipAtPoint(skillSound, transform.position, 1f, 1f, null, transform);
        }
    }

    // 부모의 PlaySkillEffects 막아버리고 직접 RPC 실행
    protected override void PlaySkillEffects()
    {
        // 아무 것도 하지 않음 → 위에서 RPC로 실행하므로 중복 방지
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
}