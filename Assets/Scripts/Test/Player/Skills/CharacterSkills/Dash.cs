using System.Collections;
using UnityEngine;

public class Dash : Skill
{
    [Header("대시 파워 설정")]
    public float dashForce = 10f;

    [Header("착지 감지 설정")]
    public float groundCheckDistance = 0.1f;
    public float maxDashTime = 2f;

    protected override void Awake()
    {
        base.Awake();
        // 무한 사용 → UsableCountComponent 제거
        if (usableCountComponent != null)
            Destroy(usableCountComponent as Component);
        skillSound = GetComponent<AudioSource>().clip;
    }

    // 실제 스킬 실행
    public override void Execute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        base.Execute(executor, pos, dir);
        PlayFollowEffectAtRemote(executor);
        
        // ✅ 내 캐릭터일 때만 물리 연산 실행
        if (executor.photonView.IsMine)
        {
            var rb = executor.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(dir * dashForce, ForceMode.VelocityChange);
                StartCoroutine(DashStopRoutine(executor));
            }
        }
    }

    private IEnumerator DashStopRoutine(SkillController executor)
    {
        if (!executor.photonView.IsMine) yield break;

        var rb = executor.GetComponent<Rigidbody>();
        if (rb == null) yield break;

        float startTime = Time.time;
        yield return new WaitForSeconds(0.5f); // 너무 빨리 체크 방지

        while (Time.time - startTime < maxDashTime)
        {
            if (Physics.Raycast(executor.transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance))
            {
                if (rb.velocity.y <= 0.1f)
                {
                    Vector3 currentVelocity = rb.velocity;
                    rb.velocity = new Vector3(0f, currentVelocity.y, 0f);
                    Debug.Log("✅ Dash - 바닥 착지 감지, 수평 속도 정지");
                    yield break;
                }
            }
            yield return null;
        }

        // 시간 초과 시 강제 정지
        Vector3 finalVelocity = rb.velocity;
        rb.velocity = new Vector3(0f, finalVelocity.y, 0f);
        Debug.Log("⚠️ Dash - 최대 시간 초과, 강제 정지");
    }
}