using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dash : Skill
{
    [Header("대시 파워 설정")]
    public float dashForce = 10f;

    public override void Execute(MoveController executor, Vector3 pos, Vector3 dir)
    {
        base.Execute(executor, pos, dir);

        if (!executor.photonView.IsMine) return;
        
        var rb = executor.GetComponent<Rigidbody>();
        rb.AddForce(dir * dashForce, ForceMode.VelocityChange);
        
        // 순간적으로 바닥에 남는 흔적 같은 이펙트도 추가 가능
        // SpawnEffectAtPosition(trailEffectPrefab, pos, Quaternion.identity, 1f);
    }

    public override void CastExecute(MoveController executor, Vector3 pos, Vector3 dir)
    {
        base.CastExecute(executor, pos, dir);
        if (executor.photonView.IsMine)
        {
            //스킬 시전 시간 중 실제 물리연산이 필요한 경우 위와 같이 사용
        }
    }
}
