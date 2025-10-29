using Photon.Pun;
using UnityEngine;

public class OneTimeDefenseItem : Skill
{
    [Header("방어막 이펙트 프리팹")]
    [SerializeField] private GameObject shieldPrefab;

    private int useItemCount = 1;
    private Vector3 spawnPosition;

    protected override void Awake()
    {
        base.Awake();

        if (usableCountComponent == null)
            usableCountComponent = gameObject.AddComponent<UsableCountComponent>();
        _usableCount = usableCountComponent;

        (usableCountComponent as UsableCountComponent).SetMaxUses(useItemCount);
        spawnPosition = new Vector3(0, 1, 0);
    }

    public override void Execute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        var living = executor.GetComponent<LivingEntity>();
        if (living == null) return;

        Debug.Log("[OneTimeDefenseItem] 캐릭터 현재 위치 " + executor.transform.position);
        Debug.Log("[OneTimeDefenseItem] spawnPosition " + spawnPosition);

        // 이미 쉴드가 붙어있다면 중복 생성 방지
        var existingShield = executor.GetComponentInChildren<OneTimeDefense>();
        if (existingShield != null)
        {
            PhotonNetwork.Destroy(existingShield.gameObject);
        }

        // 쉴드 프리팹 생성
        if (shieldPrefab != null)
        {

            GameObject shieldObj = PhotonNetwork.Instantiate(
                "Prefabs/ItemObject/" + shieldPrefab.name,
                spawnPosition,
                Quaternion.identity
            );

            Debug.Log("[OneTimeDefenseItem] 캐릭터 현재 위치 " + executor.transform.position);
            Debug.Log("[OneTimeDefenseItem] spawnPosition " + spawnPosition);

            shieldObj.GetComponent<PhotonView>().RPC(
            "RPC_SetParent",
            RpcTarget.All,
            executor.photonView.ViewID
            );

            // 쉴드 관리 컴포넌트 초기화
            var defense = shieldObj.GetComponent<OneTimeDefense>();
            if (defense == null)
                defense = shieldObj.AddComponent<OneTimeDefense>();

            defense.photonView.RPC(
                "InitializeShield",
                RpcTarget.All,
                living.photonView.ViewID
            );
        }

        // 효과음 / 추가 이펙트 재생
        PlayEffectAtRemote(executor, pos, dir);
        executor.EndSkillInProgress();
    }
}
