using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class Fireball : Skill
{
    [Header("파이어볼 설정")]
    [SerializeField] GameObject firballPrefab;
    Rigidbody firballRigidbody;
    public float fbDamage = 30f;
    public float fbRange = 50f;
    public float fbForce = 9f;

    public override void Execute(MoveController executor, Vector3 pos, Vector3 dir)
    {
        base.Execute(executor, pos, dir);

        if (!executor.photonView.IsMine) return;
        
        GameObject fireball = PhotonNetwork.Instantiate(firballPrefab.ToString(), pos, firballPrefab.transform.rotation);
        firballRigidbody = fireball.GetComponent<Rigidbody>();
        firballRigidbody.AddForce(dir * fbForce, ForceMode.Impulse);


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
