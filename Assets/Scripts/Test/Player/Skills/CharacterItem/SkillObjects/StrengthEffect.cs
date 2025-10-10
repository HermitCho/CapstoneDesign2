using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System;

public class StrengthEffect : MonoBehaviourPun
{
    // StrengthItem의 RPC를 통해 이펙트가 초기화되고 버프가 시작됩니다.
    [PunRPC]
    public void InitializeEffectAndBuff(int executorViewId, float multiplier, float duration)
    {
        // ⭐ 1. 실행 주체(플레이어) 찾기
        PhotonView excuterPV = PhotonView.Find(executorViewId);
        if (excuterPV == null)
        {
            Debug.LogError($"[StrengthEffect] Executor ViewID {executorViewId}를 찾을 수 없습니다.");
            return;
        }

        var living = excuterPV.GetComponent<LivingEntity>();
        var mover = excuterPV.GetComponent<MoveController>();
        TestGun gun = excuterPV.GetComponentInChildren<TestGun>(true);

        if (living == null || mover == null) return;
        
        // ⭐ 2. 이펙트 오브젝트에서 버프 로직 및 파괴 로직 시작 (Async)
        ApplyStrengthBuffAndDestroyAsync(living, mover, gun, multiplier, duration).Forget();
    }

    /// <summary>
    /// 버프 적용, duration 대기, 버프 해제, 이펙트 파괴까지 모두 처리합니다.
    /// 이 로직은 아이템이 파괴된 후에도 이펙트 오브젝트 위에서 안정적으로 실행됩니다.
    /// </summary>
    private async UniTask ApplyStrengthBuffAndDestroyAsync(LivingEntity living, MoveController mover, TestGun gun, float multiplier, float duration) 
    {
        Debug.Log($"[StrengthEffect] 강화 효과 시작: {multiplier * 100f}% for {duration}s");

        // ======= 1. 버프 적용 로직 =======
        float originalHealth = living.StartingHealth;
        living.photonView.RPC("RPC_UpdateHealth", RpcTarget.All, originalHealth * (1f + multiplier), false, 0);
        mover.photonView.RPC("ApplySpeedBuff", RpcTarget.All, multiplier);

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

        Debug.Log($"[StrengthEffect] 강화 효과 종료 및 이펙트 파괴");

        // ======= 4. 이펙트 오브젝트 파괴 =======
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject); 
        }
    }
}