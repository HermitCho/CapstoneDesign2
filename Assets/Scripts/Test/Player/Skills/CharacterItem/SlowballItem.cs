using Photon.Pun;
using UnityEngine;

/// <summary>
/// 슬로우볼을 생성하여 발사하는 아이템 스크립트.
/// 발사 후 바닥에 캐릭터가 느려지는 장판 일시 설치
/// 이 스크립트는 플레이어 또는 아이템 오브젝트에 부착될 수 있습니다.
/// </summary>
public class SlowballItem : Skill
{
    [Header("발사 설정")]
    [SerializeField] private GameObject slowballPrefab;
    [SerializeField] private Transform launchPoint;

    /// <summary>
    /// 플레이어가 아이템을 사용했을 때 호출되는 메서드.
    /// </summary>
    /// <param name="player">아이템을 사용하는 플레이어의 SkillController.</param>
    public override void Execute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        // 발사 위치가 지정되지 않았으면 플레이어의 위치를 사용
        Vector3 spawnPosition = launchPoint != null ? launchPoint.position : executor.transform.position + executor.transform.forward * 1.5f + executor.transform.up * 1.5f;

        // 네트워크 상에 파이어볼 프리팹을 생성
        GameObject slowballInstance = PhotonNetwork.Instantiate(
            "Prefabs/Skill/" + slowballPrefab.name,
            spawnPosition,
            Quaternion.identity
        );

        // 생성된 파이어볼에 초기화 RPC 호출
        if (slowballInstance.TryGetComponent<Slowball>(out Slowball slowballScript))
        {
            slowballScript.photonView.RPC(
                "InitializeAndLaunch",
                RpcTarget.All,
                executor.photonView.OwnerActorNr,
                executor.GetComponent<TestShoot>().CalculateShotDirection(),
                slowballScript.GetSlowballSpeed()
            );
        }
    }
}