using Photon.Pun;
using UnityEngine;

/// <summary>
/// 파이어볼을 생성하여 발사하는 아이템 스크립트.
/// 이 스크립트는 플레이어 또는 아이템 오브젝트에 부착될 수 있습니다.
/// </summary>
public class FireballItem : Skill
{
    [Header("발사 설정")]
    [SerializeField] private GameObject fireballPrefab; // Fireball 스크립트가 부착된 파이어볼 프리팹
    [SerializeField] private Transform launchPoint; // 파이어볼이 생성될 위치

    /// <summary>
    /// 플레이어가 아이템을 사용했을 때 호출되는 메서드.
    /// </summary>
    /// <param name="player">아이템을 사용하는 플레이어의 MoveController.</param>
    public override void Execute(MoveController executor, Vector3 pos, Vector3 dir)
    {
        // 발사 위치가 지정되지 않았으면 플레이어의 위치를 사용
        Vector3 spawnPosition = launchPoint != null ? launchPoint.position : executor.transform.position + executor.transform.forward * 1.5f + executor.transform.up * 1.5f;

        // 네트워크 상에 파이어볼 프리팹을 생성
        GameObject fireballInstance = PhotonNetwork.Instantiate(
            "Prefabs/Skill/" + fireballPrefab.name,
            spawnPosition,
            Quaternion.identity
        );

        // 생성된 파이어볼에 초기화 RPC 호출
        if (fireballInstance.TryGetComponent<Fireball>(out Fireball fireballScript))
        {
            fireballScript.photonView.RPC(
                "InitializeAndLaunch",
                RpcTarget.All,
                executor.photonView.OwnerActorNr,
                executor.GetComponent<TestShoot>().CalculateShotDirection(),
                fireballScript.GetFireballSpeed()
            );
        }
    }
}