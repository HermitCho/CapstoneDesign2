using UnityEngine;

/// <summary>
/// muzzleTransform을 주어진 방향으로 회전시키는 스크립트 (애니메이션 이후 LateUpdate에서 적용)
/// </summary>
public class MuzzleDirectionController : MonoBehaviour
{
    [Header("총구 트랜스폼")]
    public Transform muzzleTransform;

    // 외부에서 설정하는 목표 방향
    private Vector3? targetDirection;

    /// <summary>
    /// 외부에서 발사 방향을 설정
    /// </summary>
    /// <param name="direction">정규화된 방향 벡터</param>
    public void SetDirection(Vector3 direction)
    {
        if (direction != Vector3.zero)
            targetDirection = direction;
    }

    void LateUpdate()
    {
        if (muzzleTransform == null || targetDirection == null) return;

        muzzleTransform.rotation = Quaternion.LookRotation(targetDirection.Value, Vector3.up);
    }
}