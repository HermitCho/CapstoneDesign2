using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SmokeBombSkill : Skill
{
    [Header("연막탄 설정")]
    [SerializeField] private GameObject smokePrefab;   // 연막탄 오브젝트
    [SerializeField] private float throwForce;     // 던지는 힘
    Vector3 spawnPosition;

    [Header("프리뷰 설정")]
    [SerializeField] private ProjectilePreviewComponent previewComponent;
    protected override void Awake()
    {
        base.Awake();
        // 무한 사용 → UsableCountComponent 제거
        if (usableCountComponent != null)
            Destroy(usableCountComponent as Component);
    }
    public override void CastExecute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        spawnPosition = executor.transform.position + executor.transform.forward * 1.5f + executor.transform.up * 1.5f;

        // 수류탄 생성
        GameObject smokeBombInstance = PhotonNetwork.Instantiate(
            "Prefabs/SkillObject/" + smokePrefab.name,
            spawnPosition,
            Quaternion.identity);

        if (smokeBombInstance.TryGetComponent<SmokeBomb>(out SmokeBomb smokeBombScript))
        {
            smokeBombScript.photonView.RPC(
                "InitializeAndLaunch",
                RpcTarget.All,
                executor.photonView.ViewID,
                executor.GetComponent<TestShoot>().CalculateShotDirection(),
                smokeBombScript.GetBombSpeed()
            );
            SetSpeed(smokeBombScript.GetBombSpeed());
        }
    }

    public override void StartPreview(SkillController owner)
    {
        // 부모 클래스의 기본 프리뷰 로직을 실행
        if (throwForce <= 0f && smokePrefab.TryGetComponent<SmokeBomb>(out var bomb))
        {
            throwForce = bomb.GetBombSpeed();
        }
        base.StartPreview(owner);
    }

    public override void UpdatePreview(SkillController owner, Vector3 origin, Vector3 direction, float initialSpeed = 10f)
    {
        Debug.Log($"[SmokeBombSkill] UpdatePreview - Speed: {initialSpeed}, ThrowForce: {throwForce}, Dir: {direction}");
        base.UpdatePreview(owner, origin, direction, GetProjectileSpeed());
        //Debug.Log("[SmokeBombSkill] 방향 " + direction);
    }

    public override void EndPreview(SkillController owner)
    {
        // 부모 클래스의 기본 프리뷰 종료 로직을 실행
        base.EndPreview(owner);
        // 필요하다면 여기에 FireballItem에 특화된 추가 로직을 넣을 수 있습니다.
        //Debug.Log("SmokeBombSkill 전용 EndPreview 로직 실행");
    }

    private void SetSpeed(float newSpeed)
    {
        throwForce = newSpeed;
    }

    public override float GetProjectileSpeed()
    {
        return throwForce;
    }
}