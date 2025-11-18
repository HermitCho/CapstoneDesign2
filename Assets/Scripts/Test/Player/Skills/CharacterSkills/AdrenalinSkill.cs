using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System;
using System.Threading; // CancellationTokenSource를 위해 추가

public class AdrenalinSkill : Skill
{
    [Header("강화 효과 배율")]
    [SerializeField, Range(0.01f, 1f)] private float buffMultiplier = 0.2f; // +10%
    [SerializeField] private Transform effectTransform;
    private LivingEntity living;
    private TestGun gun;

    // ✅ 활성화된 스킬의 취소 토큰 관리를 위한 필드
    private CancellationTokenSource cancellationTokenSource;
    // ✅ 사망으로 인해 스킬이 취소되었는지 여부
    private bool cancelledByDeath;

    protected override void Awake()
    {
        base.Awake();
        living = GetComponent<LivingEntity>();
        gun = GetComponentInChildren<TestGun>(true);

        // 무한 사용 → UsableCountComponent 제거
        if (usableCountComponent != null)
            Destroy(usableCountComponent as Component);
    }

    protected void OnDestroy()
    {
        // 오브젝트가 파괴되면 진행 중인 비동기 작업 취소
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
    }

    public override void Execute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        if (!photonView.IsMine) return;
        if (living.IsDead) return;

        if (living == null) return;
        // ✅ 기존 실행 중인 버프가 있다면 취소 (중복 사용 방지 또는 이전 버프 해제)
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = new CancellationTokenSource();

        // 새 버프 시작 시, 사망 취소 플래그 리셋
        cancelledByDeath = false;

        living.OnDeath += OnLivingEntityDeath;

        // ✅ RPC를 한 번만 호출하여, 이펙트 오브젝트를 생성하고 모든 제어권을 넘깁니다.
        ApplyStrengthBuffAndDestroyAsync(
            living,
            gun,
            buffMultiplier,
            duration,
            cancellationTokenSource.Token // ✅ 취소 토큰 전달
        ).Forget();

        PlayFollowEffectOnHeartAtRemote(executor, effectTransform);
    }

    // ✅ LivingEntity 사망 시 호출되는 메서드
    private void OnLivingEntityDeath()
    {
        // LivingEntity가 사망하면 비동기 작업을 취소합니다.
        if (living != null)
        {
            living.OnDeath -= OnLivingEntityDeath; // 구독 해제
        }

        // 사망으로 인해 스킬이 종료되었음을 표시
        cancelledByDeath = true;
        cancellationTokenSource?.Cancel();
    }

    private async UniTask ApplyStrengthBuffAndDestroyAsync(
        LivingEntity living,
        TestGun gun,
        float multiplier,
        float duration,
        CancellationToken token
    )
    {
        Debug.Log($"[StrengthEffect] 강화 효과 시작: {multiplier * 100f}% for {duration}s");

        float originalHealth = living.StartingHealth;
        float originalDamage = 0f;
        float originalReload = 0f;

        // ======= 1. 버프 적용 로직 =======
        try
        {
            living.photonView.RPC("RPC_UpdateHealth", RpcTarget.All, originalHealth * (1f + multiplier), false, 0);

            if (gun != null)
            {
                var data = gun.GetGunData();
                originalDamage = data.damage;
                originalReload = data.reloadTime;

                data.damage *= (1f + multiplier);
                data.reloadTime *= (1f - multiplier);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(duration), ignoreTimeScale: false, cancellationToken: token);
            Debug.Log($"[StrengthEffect] 강화 효과 종료 대기 완료");

            // 정상 종료이고, 버프가 "사망" 때문에 끝난 것이 아니며 아직 살아있을 때만 체력을 원상 복구
            if (living != null && !cancelledByDeath && !living.IsDead)
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
        catch (OperationCanceledException)
        {
            // LivingEntity.IsDead로 인해 취소된 경우
            Debug.LogWarning($"[StrengthEffect] 강화 효과가 취소되었습니다 (LivingEntity 사망 또는 스킬 중복 사용)");

            // ✅ 취소 시점에 공격/재장전 버프는 항상 원상 복구
            if (gun != null)
            {
                var data = gun.GetGunData();
                data.damage = originalDamage;
                data.reloadTime = originalReload;
            }

            // ✅ "사망이 아닌 이유"로 취소된 경우에만 체력을 되돌립니다.
            //   (예: 스킬 중복 사용으로 인한 취소 등)
            if (living != null && !cancelledByDeath && !living.IsDead)
            {
                float healthToRestore = (living.CurrentHealth < originalHealth) ? living.CurrentHealth : originalHealth;
                living.photonView.RPC("RPC_UpdateHealth", RpcTarget.All, healthToRestore, false, 0);
            }
        }
        finally
        {
            if (living != null)
            {
                living.OnDeath -= OnLivingEntityDeath;
            }
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            // 다음 사용을 위해 플래그 초기화
            cancelledByDeath = false;
        }
    }
}