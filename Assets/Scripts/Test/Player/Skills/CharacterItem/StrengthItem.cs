using System.Collections;
using UnityEngine;
using Photon.Pun;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System;

// Skill 클래스가 MonoBehaviorPun을 상속받는다고 가정합니다.
public class StrengthItem : Skill
{
    [Header("강화 지속 시간")]
    private float buffDuration = 7f;

    [Header("강화 효과 배율")]
    [SerializeField, Range(0.01f, 1f)] private float buffMultiplier = 0.1f; // +10%

    private int useItemCount = 1;

    [Header("강화 이펙트 프리팹")]
    [SerializeField] private GameObject effectPrefab;
    GameObject effectInstantiate;


    PhotonView excuterPV;

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

    public override void Execute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        if (!photonView.IsMine) return;

        var living = executor.GetComponent<LivingEntity>();
        var mover = executor.GetComponent<MoveController>();
        if (living == null || mover == null) return;

        // ✅ RPC를 한 번만 호출하여, 이펙트 오브젝트를 생성하고 모든 제어권을 넘깁니다.
        photonView.RPC("EffectStart", RpcTarget.All, executor.photonView.ViewID, buffMultiplier, duration, executor.transform.position);
        PlayFollowEffectAtRemote(executor);

        // PlayFollowEffectAtRemote(executor); 는 여기에 남겨둘 수 있습니다. (생략)
    }

    /// <summary>
    /// 모든 클라이언트에서 실행되며, 이펙트를 생성하고 버프 정보를 전달합니다.
    /// </summary>
    [PunRPC]
    private void EffectStart(int executorViewId, float multiplier, float duration, Vector3 position)
    {
        // 1. 이펙트 오브젝트 인스턴스화
        effectInstantiate = PhotonNetwork.Instantiate("Prefabs/ItemObject/" + effectPrefab.name,
        position,
        Quaternion.identity);
        PhotonView effectPV = effectInstantiate.GetComponent<PhotonView>();

        // 2. 이펙트 오브젝트에게 버프 시작 정보 및 지속 시간을 넘겨줍니다.
        // ⭐ 이펙트 오브젝트가 버프 적용/해제를 모두 제어하도록 합니다.
        effectPV.RPC("InitializeEffectAndBuff", RpcTarget.All, executorViewId, multiplier, duration);

        // 3. 외형적인 처리 및 즉시 파괴
        effectInstantiate.transform.parent = transform.root;
        ParticleSystem effect = effectInstantiate.GetComponent<ParticleSystem>();
        effect.Play();

        // 4. 아이템 자신을 즉시 파괴
        if (photonView.IsMine)
        {
            DestroySelfAfterUseAsync().Forget();
        }
    }

    // 아이템 파괴 로직 (이펙트 스크립트와 독립적)
    private async UniTask DestroySelfAfterUseAsync()
    {
        await UniTask.Yield();
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}