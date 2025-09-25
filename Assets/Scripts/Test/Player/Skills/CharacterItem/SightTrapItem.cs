using Photon.Pun;
using UnityEngine;

public class SightTrapItem : Skill
{
    [Header("Sight Trap Settings")]
    [SerializeField] private GameObject trapPrefab;   // 설치할 트랩 프리팹
    [SerializeField] private float trapLifetime = 15f; // 설치 후 지속 시간
    private int useItemCount = 3;

    protected override void Awake()
    {
        base.Awake();

        if (usableCountComponent == null)
            usableCountComponent = gameObject.AddComponent<UsableCountComponent>();
        _usableCount = usableCountComponent;

        (usableCountComponent as UsableCountComponent).SetMaxUses(useItemCount);
    }

    public override void CastExecute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        Vector3 spawnPos = placementPreviewComponent.GetPlacementPosition();
        Quaternion spawnRot = placementPreviewComponent.GetPlacementRotation();

        GameObject trapObj = PhotonNetwork.Instantiate(
            "Prefabs/Skill/" + trapPrefab.name,
            spawnPos,
            spawnRot
        );

        if (trapObj.TryGetComponent<SightTrap>(out SightTrap trap))
        {
            trap.photonView.RPC(
                "InitializeTrap",
                RpcTarget.All,
                executor.photonView.OwnerActorNr,
                trapLifetime
            );
        }
    }

    public override GameObject GetPlacementPrefab() { return trapPrefab; }
}
