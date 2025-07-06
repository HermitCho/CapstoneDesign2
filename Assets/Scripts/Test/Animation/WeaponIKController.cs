using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponIKController : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("총기 프리팹")]
    public GameObject weaponPrefab; // 씬에서 직접 연결된 rifle 오브젝트

    [Header("본 정보")]
    public string gripPointName = "GripPoint";
    public string leftGripPointName = "LeftGripPoint";

    [Header("IK 타겟들")]
    public Transform rightHandIKTarget;
    public Transform leftHandIKTarget;

    private Transform gripPoint;
    private Transform leftGripPoint;

    void Start()
    {
        if (weaponPrefab == null)
        {
            Debug.LogWarning("WeaponPrefab이 설정되지 않았습니다.");
            return;
        }

        // Grip 포인트 찾기
        gripPoint = weaponPrefab.transform.Find(gripPointName);
        leftGripPoint = weaponPrefab.transform.Find(leftGripPointName);

        if (gripPoint == null || leftGripPoint == null)
        {
            Debug.LogWarning("GripPoint 또는 LeftGripPoint가 weaponPrefab 하위에 없습니다.");
            return;
        }

        // IK 타겟 위치를 Grip 포인트로 맞춤
        if (rightHandIKTarget != null)
        {
            rightHandIKTarget.position = gripPoint.position;
            rightHandIKTarget.rotation = gripPoint.rotation;
        }

        if (leftHandIKTarget != null)
        {
            leftHandIKTarget.position = leftGripPoint.position;
            leftHandIKTarget.rotation = leftGripPoint.rotation;
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        if (rightHandIKTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandIKTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandIKTarget.rotation);
        }

        if (leftHandIKTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandIKTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandIKTarget.rotation);
        }
    }
}