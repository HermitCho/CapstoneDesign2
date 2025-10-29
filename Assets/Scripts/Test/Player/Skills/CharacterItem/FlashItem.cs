using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashItem : Skill
{
    [Header("대시 파워 설정")]
    public float dashForce = 10f;

    [Header("착지 감지 설정")]
    [Tooltip("바닥 감지용 Raycast 거리")]
    public float groundCheckDistance = 0.1f;
    [Tooltip("대시 후 최대 대기 시간 (무한 루프 방지)")]
    public float maxDashTime = 2f;

    protected override void Awake()
    {
        base.Awake();

        if (usableCountComponent == null)
            usableCountComponent = gameObject.AddComponent<UsableCountComponent>();
        _usableCount = usableCountComponent; // 반드시 인터페이스 캐싱

        (usableCountComponent as UsableCountComponent).SetMaxUses(1); // 1회용
    }
    
    public override void Execute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        base.Execute(executor, pos, dir);

        if (executor.photonView.IsMine)
        {
            var rb = executor.GetComponent<Rigidbody>();
            rb.AddForce(dir * dashForce * (executor.transform.forward.y + 1) / 2, ForceMode.VelocityChange);

            // 대시 후 착지 감지 코루틴 시작
            StartCoroutine(DashStopRoutine(executor));
            PlayFollowEffectAtRemote(executor);
        }

        // 순간적으로 바닥에 남는 흔적 같은 이펙트도 추가 가능
        // SpawnEffectAtPosition(trailEffectPrefab, pos, Quaternion.identity, 1f);
    }

    /// <summary>
    /// 대시 후 바닥 착지를 감지하여 속도를 0으로 만드는 코루틴
    /// </summary>
    IEnumerator DashStopRoutine(SkillController executor)
    {
        if (!executor.photonView.IsMine) yield break;

        var rb = executor.GetComponent<Rigidbody>();
        if (rb == null) yield break;

        float startTime = Time.time;

        // 대시 직후 잠깐 대기 (즉시 체크 방지)
        yield return new WaitForSeconds(2f);

        // 바닥에 착지할 때까지 또는 최대 시간까지 대기
        while (Time.time - startTime < maxDashTime)
        {
            // 바닥 감지용 Raycast
            RaycastHit hit;
            bool isGrounded = Physics.Raycast(
                executor.transform.position,
                Vector3.down,
                out hit,
                groundCheckDistance
            );

            // 바닥에 착지했고, 수직 속도가 떨어지는 중이면 정지
            if (isGrounded && rb.velocity.y <= 0.1f)
            {
                // 수평 속도만 0으로 설정 (Y축 속도는 유지하여 자연스러운 착지)
                Vector3 currentVelocity = rb.velocity;
                rb.velocity = new Vector3(0f, currentVelocity.y, 0f);

                Debug.Log("✅ Dash - 바닥 착지 감지, 수평 속도 정지");
                yield break;
            }

            // 매 프레임마다 체크
            yield return null;
        }

        // 최대 시간 초과 시 강제 정지
        Vector3 finalVelocity = rb.velocity;
        rb.velocity = new Vector3(0f, finalVelocity.y, 0f);
        Debug.Log("⚠️ Dash - 최대 시간 초과, 강제 정지");
        executor.EndSkillInProgress();
    }

    public override void CastExecute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        base.CastExecute(executor, pos, dir);
        if (executor.photonView.IsMine)
        {
            //스킬 시전 시간 중 실제 물리연산이 필요한 경우 위와 같이 사용
        }
    }
}
