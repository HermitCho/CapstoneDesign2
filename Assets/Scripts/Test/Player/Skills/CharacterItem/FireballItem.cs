using Photon.Pun;
using UnityEngine;

public class FireballItem : Skill
{
    [Header("발사 설정")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform launchPoint;
    [SerializeField] private float fireballSpeed = 50f; // 파이어볼 속도 (Fireball.cs와 동일)

    [Header("프리뷰 설정")]
    [SerializeField] private ProjectilePreviewComponent previewComponent;

    public override void CastExecute(MoveController executor, Vector3 pos, Vector3 dir)
    {
        Vector3 spawnPosition = launchPoint != null
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

    private void Awake()
    {
        // ProjectilePreviewComponent 자동 찾기
        if (previewComponent == null)
        {
            previewComponent = GetComponent<ProjectilePreviewComponent>();
            Debug.Log($"Awake에서 ProjectilePreviewComponent 찾음: {previewComponent?.name}");
        }
    }

    /// <summary>
    /// 프리뷰 시작 (Skill.cs에서 호출)
    /// </summary>
    public void StartPreview(MoveController owner)
    {
        Debug.Log($"FireballItem StartPreview 호출됨 - previewComponent: {previewComponent?.name}");
        
        if (previewComponent != null)
        {
            previewComponent.StartPreview(owner);
        }
        else
        {
            Debug.LogWarning("PreviewComponent가 null입니다!");
        }
    }

    /// <summary>
    /// 프리뷰 업데이트 (Skill.cs에서 호출)
    /// </summary>
    public void UpdatePreview(Vector3 origin, Vector3 direction, float initialSpeed)
    {
        Debug.Log($"FireballItem UpdatePreview 호출됨 - previewComponent: {previewComponent?.name}");
        
        if (previewComponent != null)
        {
            // 파이어볼의 실제 속도 사용
            previewComponent.UpdatePreview(origin, direction, fireballSpeed);
        }
        else
        {
            Debug.LogWarning("PreviewComponent가 null입니다!");
        }
    }

    /// <summary>
    /// 프리뷰 종료 (Skill.cs에서 호출)
    /// </summary>
    public void EndPreview()
    {
        if (previewComponent != null)
        {
            previewComponent.EndPreview();
        }
    }
}
