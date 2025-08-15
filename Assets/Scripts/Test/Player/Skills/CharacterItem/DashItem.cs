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
	[SerializeField] private float dashDuration = 0.3f; // 대시 지속시간\
	[SerializeField] private float dashForce = 130f; // 대시 힘
	[SerializeField] private AnimationCurve dashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 대시 곡선

	[Header("대시 효과")]
	[SerializeField] private TrailRenderer dashTrail; // 대시 궤적
	[SerializeField] private ParticleSystem dashParticles; // 대시 파티클

	#endregion

	#region Private Fields

	private Rigidbody playerRigidbody; // 실제로 이동할 대상(Root Player)의 리지드바디
	private Transform playerTransform; // 실제로 이동할 대상(Root Player)의 트랜스폼
	private MoveController playerMoveController; // 이동 제어
	private PhotonView ownerPhotonView; // 부모의 PV

	private Vector3 dashDirection;
	private bool isDashing = false;

	#endregion

	#region Unity Lifecycle

	protected override void Start()
	{
		base.Start();
		InitializeDashSkill();
		Debug.Log($"[DashItem] Init: pv={(ownerPhotonView!=null)}, tr={(playerTransform!=null)}, rb={(playerRigidbody!=null)}");

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
		Debug.Log("[DashItem] InitializeDashSkill 시작");
		
		// 먼저 Player 태그로 플레이어 오브젝트 찾기
		GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
		if (playerObj != null)
		{
			// 플레이어 오브젝트에서 PhotonView 찾기
			ownerPhotonView = playerObj.GetComponent<PhotonView>();
			if (ownerPhotonView != null)
			{
				playerTransform = ownerPhotonView.transform;
				playerRigidbody = ownerPhotonView.GetComponent<Rigidbody>();
				playerMoveController = ownerPhotonView.GetComponent<MoveController>();
				
				Debug.Log($"[DashItem] Player 태그로 찾은 PhotonView: {ownerPhotonView.ViewID}");
				Debug.Log($"[DashItem] 컴포넌트 설정 완료 - Transform: {playerTransform != null}, Rigidbody: {playerRigidbody != null}, MoveController: {playerMoveController != null}");
			}
			else
			{
				Debug.LogError("[DashItem] Player 오브젝트에 PhotonView가 없습니다!");
			}
		}
		else
		{
			Debug.LogError("[DashItem] Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
		}

		// 대시 궤적 초기화
		if (dashTrail != null)
		{
			dashTrail.enabled = false;
		}

		// 스킬 기본 정보 설정
		duration = dashDuration;
		castTime = 0f; // 즉시 발동
		
		Debug.Log($"[DashItem] 초기화 완료 - duration: {duration}, castTime: {castTime}");
	}

	#endregion

	#region Protected Methods

	/// <summary>
	/// 스킬 사용 가능 여부를 확인합니다.
	/// </summary>
	/// <returns>사용 가능 여부</returns>
	protected override bool CheckCanUse()
	{
		// 대시 중이 아니고, 이동 대상 컴포넌트들이 존재할 때만 사용 가능
		return !isDashing && playerRigidbody != null && playerTransform != null;
	}

	/// <summary>
	/// 실제 대시 효과를 실행합니다.
	/// </summary>
	protected override void ExecuteSkill()
	{
		Debug.Log($"[DashItem] ExecuteSkill 호출: {skillName}");
		base.ExecuteSkill();

		// Player 태그로 플레이어 찾기
		GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
		PhotonView pv = null;
		bool isOwner = false;
		
		if (playerObj != null)
		{
			pv = playerObj.GetComponent<PhotonView>();
			if (pv != null)
			{
				isOwner = pv.IsMine;
				Debug.Log($"[DashItem] Player 태그로 찾은 PhotonView: {pv.ViewID}, IsMine: {isOwner}");
			}
			else
			{
				Debug.LogError("[DashItem] Player 오브젝트에 PhotonView가 없습니다!");
			}
		}
		else
		{
			Debug.LogError("[DashItem] Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
		}
		
		if (isOwner)
		{
			Debug.Log("[DashItem] 코루틴 시작 (오너)");
			StartCoroutine(DashRoutine());
		}
		else
		{
			Debug.Log("[DashItem] 오너가 아니므로 이동 실행 안함");
		}
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

		// 대시 궤적/파티클
		if (dashTrail != null) dashTrail.enabled = true;
		if (dashParticles != null) dashParticles.Play();

		// Player 태그로 플레이어 찾기
		GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
		PhotonView pv = null;
		bool isOwner = false;
		
		if (playerObj != null)
		{
			pv = playerObj.GetComponent<PhotonView>();
			if (pv != null)
			{
				isOwner = pv.IsMine;
				Debug.Log($"[DashItem] DashRoutine에서 PhotonView: {pv.ViewID}, IsMine: {isOwner}");
			}
		}

		// 오너에서만 이동 입력 잠시 차단
		if (isOwner && playerMoveController != null)
		{
			playerMoveController.DisableMovement();
			Debug.Log("[DashItem] 이동 입력 비활성화");
		}

		if (isOwner && playerRigidbody != null)
		{
			Debug.Log($"[DashItem] 대시 시작 - 방향: {dashDirection}, 힘: {dashForce}");
			
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
				// 속도를 직접 설정하여 즉각적인 이동 보장
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
			// 오너가 아닌 클라는 이동 수행 없음 (이펙트만 재생)
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
