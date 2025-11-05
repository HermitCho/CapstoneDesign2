using Photon.Pun;
using UnityEngine;

public class FlashbangItem : Skill
{
    [Header("발사 설정")]
    [SerializeField] private GameObject flashbangPrefab;
    [SerializeField] private Transform launchPoint;
    [SerializeField] private float flashbangSpeed;
    Vector3 spawnPosition;

    [Header("프리뷰 설정")]
    [SerializeField] private ProjectilePreviewComponent previewComponent;
    protected override void Awake()
    {
        base.Awake();

        if (usableCountComponent == null)
        {
            usableCountComponent = gameObject.AddComponent<UsableCountComponent>();
            _usableCount = usableCountComponent;
        }

        (usableCountComponent as UsableCountComponent).SetMaxUses(1); // 1회용
    }

    public override void CastExecute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        Debug.Log("[FlashbangItem - CastExcute] - 사용");
        spawnPosition = launchPoint != null
           ? launchPoint.position
           : executor.transform.position + executor.transform.forward * 1.5f + executor.transform.up * 1.5f;

        GameObject FlashbangInstance = PhotonNetwork.Instantiate(
            "Prefabs/ItemObject/" + flashbangPrefab.name,
            spawnPosition,
            Quaternion.identity
        );
        Debug.Log("[FlashbangItem - CastExcute] - 생성");

        if (FlashbangInstance.TryGetComponent<Flashbang>(out Flashbang FlashbangScript))
        {
            FlashbangScript.photonView.RPC(
                "InitializeAndLaunch",
                RpcTarget.All,
                executor.photonView.OwnerActorNr,
                executor.GetComponent<TestShoot>().CalculateShotDirection(),
                FlashbangScript.GetFlashbangSpeed()
            );
            SetSpeed(FlashbangScript.GetFlashbangSpeed());
            // 실제 발사 시점에 애니메이션 재생 (E 입력 시에는 재생하지 않음)
            PlayExecuteAnimation(executor);
            executor.EndSkillInProgress();
            Debug.Log("[FlashbangItem - CastExcute] - FlashItem 역할 끝");
        }
    }

    public override void StartPreview(SkillController owner)
    {
        base.StartPreview(owner);

        Debug.Log("FlashbangItem 전용 StartPreview 로직 실행");
        PlayFollowCastEffectAtRemote(owner);
    }

    public override void UpdatePreview(SkillController owner, Vector3 origin, Vector3 direction, float initialSpeed = 10f)
    {
        // 부모 클래스의 UpdatePreview 메서드를 호출하며 Flashbang의 고유 속도 전달
        base.UpdatePreview(owner, origin, direction, GetProjectileSpeed());
        //Debug.Log("[FlashbangItem] 방향 " + direction);
    }

    public override void EndPreview(SkillController owner)
    {
        // 부모 클래스의 기본 프리뷰 종료 로직을 실행
        base.EndPreview(owner);
        // 필요하다면 여기에 FlashbangItem에 특화된 추가 로직을 넣을 수 있습니다.
        Debug.Log("FlashbangItem 전용 EndPreview 로직 실행");
    }

    private void SetSpeed(float newSpeed)
    {
        flashbangSpeed = newSpeed;
    }

    public override float GetProjectileSpeed()
    {
        return flashbangSpeed;
    }
}
