using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System;

public class AdrenalinSkill : Skill
{
    [Header("강화 효과 배율")]
    [SerializeField, Range(0.01f, 1f)] private float buffMultiplier = 0.2f; // +10%
    [SerializeField] private Transform effectTransform;
    private LivingEntity living;
    private TestGun gun;


    protected override void Awake()
    {
        base.Awake();
        living = GetComponent<LivingEntity>();
        gun = GetComponentInChildren<TestGun>(true);

        // 무한 사용 → UsableCountComponent 제거
        if (usableCountComponent != null)
            Destroy(usableCountComponent as Component);
    }

    public override void Execute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        if (!photonView.IsMine) return;

        if (living == null) return;
        // ✅ RPC를 한 번만 호출하여, 이펙트 오브젝트를 생성하고 모든 제어권을 넘깁니다.
        ApplyStrengthBuffAndDestroyAsync(living, gun, buffMultiplier, duration).Forget();
        PlayFollowEffectOnHeartAtRemote(executor, effectTransform);
    }


    /// <summary>
    /// 버프 적용, duration 대기, 버프 해제, 이펙트 파괴까지 모두 처리합니다.
    /// 이 로직은 아이템이 파괴된 후에도 이펙트 오브젝트 위에서 안정적으로 실행됩니다.
    /// </summary>
    private async UniTask ApplyStrengthBuffAndDestroyAsync(LivingEntity living, TestGun gun, float multiplier, float duration)
    {
        Debug.Log($"[StrengthEffect] 강화 효과 시작: {multiplier * 100f}% for {duration}s");

        // ======= 1. 버프 적용 로직 =======
        float originalHealth = living.StartingHealth;
        living.photonView.RPC("RPC_UpdateHealth", RpcTarget.All, originalHealth * (1f + multiplier), false, 0);

        float originalDamage = 0f;
        float originalReload = 0f;
        if (gun != null)
        {
            var data = gun.GetGunData();
            originalDamage = data.damage;
            originalReload = data.reloadTime;

            data.damage *= (1f + multiplier);
            data.reloadTime *= (1f - multiplier);
        }

        // ======= 2. 지속 시간 대기 =======
        await UniTask.Delay(TimeSpan.FromSeconds(duration), ignoreTimeScale: false);
        Debug.Log($"[StrengthEffect] 강화 효과 종료 대기 완료");

        // ======= 3. 버프 해제 로직 =======
        if (living != null)
        {
            living.photonView.RPC("RPC_UpdateHealth", RpcTarget.All, originalHealth, false, 0);
        }

        if (gun != null)
        {
            var data = gun.GetGunData();
            data.damage = originalDamage;
            data.reloadTime = originalReload;
        }

        Debug.Log($"[StrengthEffect] 강화 효과 종료 및 이펙트 파괴");
    }
}
