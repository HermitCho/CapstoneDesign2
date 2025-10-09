using System.Collections;
using UnityEngine;
using Photon.Pun;

// Skill 클래스가 MonoBehaviorPun을 상속받는다고 가정합니다.
public class StrengthItem : Skill
{
    [Header("강화 지속 시간")]
    private float buffDuration = 7f;

    [Header("강화 효과 배율")]
    [SerializeField, Range(0.01f, 1f)] private float buffMultiplier = 0.1f; // +10%

    private int useItemCount = 1;

    // 아이템을 사용한 플레이어의 ViewID를 저장합니다.
    // 이는 RPC를 받기 전에 아이템이 이미 네트워크상에 존재하므로, 
    // 아이템 자체의 PhotonView를 사용하여 플레이어를 찾을 수 있습니다.
    private int executorViewId;

    protected override void Awake()
    {
        base.Awake();

        // OneTimeDefenseItem 구조 참고
        if (usableCountComponent == null)
            usableCountComponent = gameObject.AddComponent<UsableCountComponent>();

        _usableCount = usableCountComponent;
        usableCountComponent.SetMaxUses(useItemCount);
        duration = buffDuration;
    }

    /// <summary>
    /// 아이템 사용 로직. 로컬에서 실행되며, 네트워크 동기화는 아이템 자신의 RPC로 처리합니다.
    /// </summary>
    public override void Execute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        // ⭐ ItemController에서 이 아이템을 부착했을 때 소유권을 넘겨주었는지 확인합니다.
        // ItemController.RPC_AttachNetworkItem에서 itemPv.TransferOwnership(PhotonNetwork.LocalPlayer)이 실행되어야 합니다.
        if (!photonView.IsMine) return;

        var living = executor.GetComponent<LivingEntity>();
        var mover = executor.GetComponent<MoveController>();
        if (living == null || mover == null) return;

        // ✅ 즉발형 스킬 — 이펙트 및 사운드 재생 (Skill 기본 클래스의 구현에 따라 처리)
        PlayEffectAtRemote(executor, pos, dir);

        // ✅ 강화 효과 시작 (아이템 자신의 PhotonView를 사용하여 RPC 호출)
        // ItemController에서 RPC 호출 로직을 옮겨왔으므로, 이제 아이템 자체가 동기화의 주체가 됩니다.
        photonView.RPC("ApplyStrengthBuff", RpcTarget.All,
            executor.photonView.ViewID, buffMultiplier, duration);
    }

    /// <summary>
    /// 모든 클라이언트에서 실행되며, 강화 효과를 적용합니다.
    /// </summary>
    [PunRPC]
    private void ApplyStrengthBuff(int executorViewId, float multiplier, float duration)
    {
        // 1. 실행 주체(플레이어) 찾기
        PhotonView pv = PhotonView.Find(executorViewId);
        if (pv == null)
        {
            Debug.LogError($"[StrengthItem] Executor ViewID {executorViewId}를 찾을 수 없습니다.");
            return;
        }

        var living = pv.GetComponent<LivingEntity>();
        var mover = pv.GetComponent<MoveController>();

        if (living == null || mover == null) return;

        // ⭐ 2. 중복 실행 방지 및 이전 버프 코루틴 중지 (선택 사항)
        // 기존에 플레이어에게 적용 중인 버프가 있다면 먼저 중지할 수 있습니다.
        // pv.StopCoroutine("ApplyBuffCoroutine"); // 코루틴 이름을 사용하여 중지할 경우

        // ✅ 3. 총기 찾기
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

        // ✅ 4. 코루틴 시작 (강화 효과 적용 및 해제)
        // Coroutine을 아이템 오브젝트(이 스크립트가 붙은)에서 시작하지 않고, 
        // 효과를 받는 플레이어(pv)에서 시작하여 안정성을 높입니다.
        pv.StartCoroutine(ApplyBuffCoroutine(living, mover, gun, multiplier, duration));

        // 5. 아이템 사용 횟수 감소 (ItemController가 아닌 Skill 클래스에서 처리할 수도 있습니다.)
        // 여기서는 버프 적용이 완료되면 아이템을 파괴하는 로직을 추가합니다.

        // ⭐ 6. 사용 완료 후 파괴 요청
        // 아이템의 오너(구매자)만 파괴를 요청할 수 있습니다.
        if (photonView.IsMine)
        {
            // 다음 프레임에 파괴될 수 있도록 코루틴으로 처리합니다.
            StartCoroutine(DestroySelfAfterUse(pv.gameObject));
        }
    }

    /// <summary>
    /// 버프 적용이 완료된 후 아이템 오브젝트를 네트워크에서 파괴합니다.
    /// </summary>
    private IEnumerator DestroySelfAfterUse(GameObject playerObject)
    {
        // 아이템 컨트롤러에게 사용 완료를 알리는 로직이 필요할 수 있습니다.
        // 예: playerObject.GetComponent<ItemController>().MoveUsedItemToTemp(gameObject); 

        // 1 프레임 대기 후 파괴하여 다른 로직이 먼저 실행되도록 합니다.
        yield return null;

        // ⭐ 오너만 네트워크 파괴를 호출합니다.
        if (photonView.IsMine)
        {
            // 이 아이템의 PhotonView를 사용하여 네트워크에서 파괴합니다.
            PhotonNetwork.Destroy(gameObject);
        }
    }

    // 이 코루틴은 **효과를 받는 플레이어 오브젝트(pv)**에서 실행됩니다.
    private IEnumerator ApplyBuffCoroutine(LivingEntity living, MoveController mover, TestGun gun, float multiplier, float duration)
    {
        Debug.Log($"[StrengthItem] 강화 효과 시작: {multiplier * 100f}% for {duration}s");

        // 원래 값 백업
        float originalHealth = living.StartingHealth;
        // LivingEntity의 StartHealth는 모든 클라에서 같아야 합니다.

        // ✅ 최대 체력 증가 (living의 photonView로 RPC 호출)
        living.photonView.RPC("RPC_UpdateHealth", RpcTarget.All, originalHealth * (1f + multiplier), false, 0);

        // ✅ 이동 속도 증가 (mover의 photonView로 RPC 호출)
        mover.photonView.RPC("ApplySpeedBuff", RpcTarget.All, multiplier);

        // ✅ 총기 데미지/재장전 속도 감소 (데미지 증가 효과를 포함하기 위해)
        float originalDamage = 0f;
        float originalReload = 0f;
        if (gun != null)
        {
            // 이 로직은 로컬에서만 실행되어야 하는지, 아니면 GunData도 네트워크 동기화가 필요한지 확인 필요.
            // (일반적으로 총기 스탯 변경은 로컬에서 진행하고, 총 발사 시 데미지 계산만 네트워크로 처리합니다.)
            var data = gun.GetGunData();
            originalDamage = data.damage;
            originalReload = data.reloadTime;

            // ⭐ 로컬에서만 스탯 변경
            data.damage *= (1f + multiplier);
            data.reloadTime *= (1f - multiplier);
        }

        yield return new WaitForSeconds(duration);

        // ⭐ 강화 효과 해제 (모든 클라이언트에게 동기화)
        if (living != null)
        {
            living.photonView.RPC("RPC_UpdateHealth", RpcTarget.All, originalHealth, false, 0);
        }

        if (mover != null)
        {
            mover.photonView.RPC("RemoveSpeedBuff", RpcTarget.All, multiplier);
        }

        // ⭐ 로컬에서만 스탯 복구
        if (gun != null)
        {
            var data = gun.GetGunData();
            data.damage = originalDamage;
            data.reloadTime = originalReload;
        }

        Debug.Log($"[StrengthItem] 강화 효과 종료");
    }
}