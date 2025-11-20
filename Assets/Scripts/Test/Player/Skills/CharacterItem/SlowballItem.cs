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
    private float slowballSpeed = 20f;
    Vector3 spawnPosition;
    protected override void Awake()
    {
        base.Awake();

        if (usableCountComponent == null)
            usableCountComponent = gameObject.AddComponent<UsableCountComponent>();
        _usableCount = usableCountComponent; // 반드시 인터페이스 캐싱

        (usableCountComponent as UsableCountComponent).SetMaxUses(1); // 1회용
    }
    /// <summary>
    /// 플레이어가 아이템을 사용했을 때 호출되는 메서드.
    /// </summary>
    /// <param name="player">아이템을 사용하는 플레이어의 SkillController.</param>
    public override void CastExecute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        spawnPosition = launchPoint != null
           ? launchPoint.position
           : executor.transform.position + executor.transform.forward * 1.5f + executor.transform.up * 1.5f;

        // 네트워크 상에 파이어볼 프리팹을 생성
        GameObject slowballInstance = PhotonNetwork.Instantiate(
            "Prefabs/ItemObject/" + slowballPrefab.name,
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
            // 실제 발사 시점에 애니메이션 재생 (E 입력 시에는 재생하지 않음)
            PlayExecuteAnimation(executor);
        }
        executor.EndSkillInProgress();
    }

    public override void StartPreview(SkillController owner)
    {
        base.StartPreview(owner);

        Debug.Log("FireballItem 전용 StartPreview 로직 실행");
        PlayFollowCastEffectAtRemote(owner);
    }

    public override void UpdatePreview(SkillController owner, Vector3 origin, Vector3 direction, float initialSpeed = 10f)
    {
        // 부모 클래스의 UpdatePreview 메서드를 호출하며 Fireball의 고유 속도 전달
        base.UpdatePreview(owner, origin, direction, GetProjectileSpeed());
        Debug.Log("[FireballItem] 방향 " + direction);
    }

    public override void EndPreview(SkillController owner)
    {
        // 부모 클래스의 기본 프리뷰 종료 로직을 실행
        base.EndPreview(owner);
        // 필요하다면 여기에 FireballItem에 특화된 추가 로직을 넣을 수 있습니다.
        Debug.Log("FireballItem 전용 EndPreview 로직 실행");
    }


    public override float GetProjectileSpeed()
    {
        return slowballSpeed;
    }
}