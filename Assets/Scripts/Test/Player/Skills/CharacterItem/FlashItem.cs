using System.Collections;
using UnityEngine;
using Photon.Pun;

public class FlashItem : Skill
{
    [Header("대시 파워 설정")]
    public float dashForce = 10f;

    [Header("착지 감지 설정")]
    [Tooltip("바닥 감지용 Raycast 거리")]
    public float groundCheckDistance = 0.1f;

    [Tooltip("대시 후 최대 대기 시간 (무한 루프 방지)")]
    public float maxDashTime = 2f;

    [Tooltip("대시 시작 후 이 시간 동안은 착지 검사를 하지 않음 (최소 비행 시간 보장)")]
    public float minDashDuration = 0.5f;

    [Header("레이어 및 안전 설정")]
    [SerializeField] private string dashLayerName = "PlayerDash";
    [Tooltip("땅으로 인식할 레이어 (PlayerDash 제외, Default/Ground 등)")]
    [SerializeField] private LayerMask groundLayerMask;

    // ✅ 레이어 복구를 위한 변수
    private int savedOriginLayer;

    // 상태 관리 변수
    private bool isDashing = false;
    private Rigidbody currentRb;
    private SkillController currentExecutor;

    protected override void Awake()
    {
        base.Awake();

        if (usableCountComponent == null)
            usableCountComponent = gameObject.AddComponent<UsableCountComponent>();
        _usableCount = usableCountComponent;

        (usableCountComponent as UsableCountComponent).SetMaxUses(1);

        // 마스크 미설정 시 기본값 세팅
        if (groundLayerMask == 0)
            groundLayerMask = LayerMask.GetMask("Default", "Terrain", "Ground");
    }

    // ✅ 안전장치: 스크립트가 꺼지거나 오브젝트가 죽으면 레이어 즉시 복구
    // (Skill이 MonoBehaviour를 상속받지 않는 구조라면 override 제거)
    private void OnDisable()
    {
        // base.OnDisable(); // 부모에 OnDisable이 없다면 주석 처리
        if (isDashing)
        {
            StopAllCoroutines();
            EndDashLogic();
        }
    }

    public override void Execute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        base.Execute(executor, pos, dir);

        if (executor.photonView.IsMine)
        {
            var rb = executor.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (isDashing) return; // 중복 실행 방지

                currentExecutor = executor;
                currentRb = rb;

                // 1. 대시 로직 시작 (레이어 변경, 물리 설정)
                StartDashLogic(executor.transform, rb);

                // 2. FlashItem 고유의 힘 적용 공식 유지
                // (Y축 방향을 고려한 힘 조절 로직)
                rb.velocity = Vector3.zero;
                rb.AddForce(dir * dashForce * (executor.transform.forward.y + 1) / 2, ForceMode.VelocityChange);

                StartCoroutine(DashStopRoutine(executor, rb));
                PlayFollowEffectAtRemote(executor);
            }
        }

        // SpawnEffectAtPosition(trailEffectPrefab, pos, Quaternion.identity, 1f);
    }

    private void StartDashLogic(Transform root, Rigidbody rb)
    {
        isDashing = true;

        // 루트 오브젝트의 레이어만 저장 및 변경
        savedOriginLayer = root.gameObject.layer;

        int dashLayer = LayerMask.NameToLayer(dashLayerName);
        if (dashLayer != -1)
        {
            root.gameObject.layer = dashLayer;
        }
        else
        {
            Debug.LogWarning($"Layer '{dashLayerName}'가 없습니다. Project Settings를 확인하세요.");
        }

        // 터널링 방지 및 중력 무시
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.useGravity = false;
    }

    private void EndDashLogic()
    {
        // 물리 설정 복구
        if (currentRb != null)
        {
            // 수직 속도는 유지하고 수평 속도만 0으로 (자연스러운 착지)
            Vector3 finalVel = currentRb.velocity;
            currentRb.velocity = new Vector3(0f, finalVel.y, 0f);

            currentRb.useGravity = true;
            currentRb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        // 레이어 복구
        if (currentExecutor != null)
        {
            currentExecutor.gameObject.layer = savedOriginLayer;

            // ✅ 스킬 종료 상태 알림 (기존 코드 유지)
            currentExecutor.EndSkillInProgress();
        }

        isDashing = false;
        currentRb = null;
        currentExecutor = null;

        Debug.Log("✅ FlashItem - 대시 종료 및 레이어 복구 완료");
    }

    IEnumerator DashStopRoutine(SkillController executor, Rigidbody rb)
    {
        if (!executor.photonView.IsMine) yield break;

        float startTime = Time.time;

        // 1. 최소 대시 시간만큼 대기 (이 동안은 중력 없이 날아감)
        yield return new WaitForSeconds(minDashDuration);

        // ✅ [핵심 수정] 최소 시간이 지났으면 중력을 즉시 복구!
        // 이렇게 해야 공중에서 2초 동안 둥둥 떠있지 않고 아래로 떨어집니다.
        if (rb != null) rb.useGravity = true;

        while (Time.time - startTime < maxDashTime)
        {
            // 바닥 감지 (자기 자신 제외)
            if (Physics.Raycast(executor.transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayerMask))
            {
                // 착지 시 속도가 안정화되면 멈춤
                if (rb.velocity.y <= 0.1f)
                {
                    break;
                }
            }
            yield return null;
        }

        // 루프가 끝나면(착지했거나 시간 초과) 종료 로직 실행
        EndDashLogic();
    }
}