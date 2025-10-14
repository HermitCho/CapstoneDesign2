using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class HiddenSmokeItem : Skill
{
    [Header("은신 연막 이펙트 프리팹")]
    [SerializeField] private GameObject hiddenSmokePrefab;
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
        Debug.Log("[OneTimeDefenseItem] 캐릭터 현재 위치 " + executor.transform.position);
        Debug.Log("[OneTimeDefenseItem] spawnPosition " + spawnPosition);

        if (hiddenSmokePrefab != null)
        {
            GameObject smokePrefab = PhotonNetwork.Instantiate(
                "Prefabs/ItemObject/" + hiddenSmokePrefab.name,
                executor.transform.position + spawnPosition, // 캐릭터 위치 기준으로 생성
                Quaternion.identity
            );

            // GetComponent로 PhotonView 가져오기
            PhotonView pv = smokePrefab.GetComponent<PhotonView>();

            if (pv != null)
            {
                Debug.Log("[OneTimeDefenseItem] PhotonView ViewID: " + pv.ViewID);

                pv.RPC(
                    "InitializeHiddenSmoke",
                    RpcTarget.All
                );
            }
            else
            {
                Debug.LogError("[OneTimeDefenseItem] PhotonView를 찾을 수 없습니다!");
            }
        }

        // 효과음 / 추가 이펙트 재생
        PlayEffectAtRemote(executor, pos, dir);
        executor.EndSkillInProgress();
    }
}