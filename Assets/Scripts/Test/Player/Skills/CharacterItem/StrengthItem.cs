using System.Collections;
using UnityEngine;
using Photon.Pun;

public class StrengthItem : Skill
{
    [Header("강화 지속 시간")]
    private float buffDuration = 7f;

    [Header("강화 효과 배율")]
    [SerializeField, Range(0.01f, 1f)] private float buffMultiplier = 0.1f; // +10%

    private int useItemCount = 1;

    protected override void Awake()
    {
        base.Awake();

        // ✅ OneTimeDefenseItem 구조 참고
        if (usableCountComponent == null)
            usableCountComponent = gameObject.AddComponent<UsableCountComponent>();

        _usableCount = usableCountComponent;
        usableCountComponent.SetMaxUses(useItemCount);
        duration = buffDuration;
    }

    public override void Execute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        var living = executor.GetComponent<LivingEntity>();
        var mover = executor.GetComponent<MoveController>();
        if (living == null || mover == null) return;

        // ✅ 즉발형 스킬 — 이펙트 및 사운드 재생
        PlayEffectAtRemote(executor, pos, dir);

        // ✅ 강화 효과 시작 (RPC로 모든 클라 동기화)
        executor.photonView.RPC("ApplyStrengthBuffForExecutor", RpcTarget.All,
                    executor.photonView.ViewID, buffMultiplier, duration);

    }

    [PunRPC]
    private void ApplyStrengthBuffForExecutor(int executorViewId, float multiplier, float duration)
    {
        PhotonView pv = PhotonView.Find(executorViewId);
        if (pv == null) return;

        var living = pv.GetComponent<LivingEntity>();
        var mover = pv.GetComponent<MoveController>();

        if (living == null || mover == null) return;

        // ✅ 총기 찾기 (Tag "Gun" 우선)
        TestGun gun = null;
        var gunObj = pv.GetComponentsInChildren<Transform>();
        foreach (var t in gunObj)
        {
            if (t.CompareTag("Gun"))
            {
                gun = t.GetComponent<TestGun>();
                break;
            }
        }

        // ✅ 코루틴 시작 (강화 효과 적용 및 해제)
        pv.StartCoroutine(ApplyBuffCoroutine(living, mover, gun, multiplier, duration));
    }

    private IEnumerator ApplyBuffCoroutine(LivingEntity living, MoveController mover, TestGun gun, float multiplier, float duration)
    {
        Debug.Log($"[StrengthItem] 강화 효과 시작: {multiplier * 100f}% for {duration}s");

        // 원래 값 백업
        float originalHealth = living.StartingHealth;
        float originalCurrentHealth = living.CurrentHealth;
        float newHealth = originalHealth * (1f + multiplier);

        // ✅ 최대 체력 증가
        living.photonView.RPC("RPC_UpdateHealth", RpcTarget.All, newHealth, false, 0);

        // ✅ 이동 속도 증가
        mover.photonView.RPC("ApplySpeedBuff", RpcTarget.All, multiplier);

        // ✅ 총기 데미지/재장전 속도 감소 (데미지 증가 효과를 포함하기 위해)
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

        yield return new WaitForSeconds(duration);

        if (living != null)
        {
            living.photonView.RPC("RPC_UpdateHealth", RpcTarget.All, originalHealth, false, 0);
        }

        if (mover != null)
        {
            mover.photonView.RPC("RemoveSpeedBuff", RpcTarget.All, multiplier);
        }

        if (gun != null)
        {
            var data = gun.GetGunData();
            data.damage = originalDamage;
            data.reloadTime = originalReload;
        }

        Debug.Log($"[StrengthItem] 강화 효과 종료");
    }
}

