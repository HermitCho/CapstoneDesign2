using Photon.Pun;
using UnityEngine;

public class TrapItem : Skill
{
    [Header("Trap Settings")]
    [SerializeField] private GameObject trapPrefab;   // 설치할 함정 프리팹
    [SerializeField] private float trapLifetime = 15f; // 설치 후 지속 시간
    private int useItemCount = 3;

    protected override void Awake()
    {
        base.Awake();

        if (usableCountComponent == null)
            usableCountComponent = gameObject.AddComponent<UsableCountComponent>();
        _usableCount = usableCountComponent;

        (usableCountComponent as UsableCountComponent).SetMaxUses(useItemCount); // 1회용
    }
    public override void CastExecute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        // 설치 위치 (예: 발 밑 조금 앞쪽)
        Vector3 spawnPos = placementPreviewComponent.GetPlacementPosition();
        Quaternion spawnRot = placementPreviewComponent.GetPlacementRotation();

        // 네트워크 상에 설치
        GameObject trapObj = PhotonNetwork.Instantiate(
            "Prefabs/ItemObject/" + trapPrefab.name, // 반드시 Resources/Prefabs/Skill 경로에 있어야 함
            spawnPos,
            spawnRot
        );

        if (trapObj.TryGetComponent<Trap>(out Trap trap))
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

    // 프리뷰 지원 (설치 전 위치 표시)
    public override void StartPreview(SkillController owner)
    {

        base.StartPreview(owner);
    }

    public override void UpdatePreview(SkillController owner, Vector3 origin, Vector3 direction, float initialSpeed = 10f)
    {
        base.UpdatePreview(owner, origin, direction, initialSpeed);
    }

    public override void EndPreview(SkillController owner)
    {
        base.EndPreview(owner);
    }
}
