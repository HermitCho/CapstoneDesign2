using Photon.Pun;
using UnityEngine;

public class FireballItem : Skill
{
    [Header("발사 설정")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform launchPoint;
    [SerializeField] private float fireballSpeed = 50f; // 파이어볼 속도 (Fireball.cs와 동일)
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
        spawnPosition = launchPoint != null
           ? launchPoint.position
           : executor.transform.position + executor.transform.forward * 1.5f + executor.transform.up * 1.5f;

        GameObject fireballInstance = PhotonNetwork.Instantiate(
            "Prefabs/Skill/" + fireballPrefab.name,
            spawnPosition,
            Quaternion.identity
        );

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

    public override void StartPreview(SkillController owner)
    {
        // 부모 클래스의 기본 프리뷰 로직을 실행
        base.StartPreview(owner);
        // 필요하다면 여기에 FireballItem에 특화된 추가 로직을 넣을 수 있습니다.
        Debug.Log("FireballItem 전용 StartPreview 로직 실행");
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
        return fireballSpeed;
    }
}
