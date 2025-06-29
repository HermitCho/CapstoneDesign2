using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIKController : MonoBehaviour
{
    Animator animator;

    [Header("회전 값")]
    public float lookAngle = 0f;
    public float turnAngle = 0f;

    [Header("Aim 상태")]
    public bool isAiming = false;

    [Header("Aim IK 타겟")]
    public Transform rightHandTarget;
    public Transform leftHandTarget;

    [Header("왼손 IK 설정")]
    public bool enableLeftHandIK = true;

    [Header("Bone 회전 강도")]
    public float spineWeight = 0.3f;
    public float chestWeight = 0.5f;
    public float neckWeight = 0.7f;
    public float headWeight = 1.0f;

    [Header("회전 제한")]
    public float maxLookUp = 60f;
    public float maxLookDown = -60f;
    public float maxTurn = 90f;


    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    

    void OnAnimatorIK(int layerIndex)
    {
    
        // Bone 회전
        ApplyRotationToBone(HumanBodyBones.Spine, lookAngle * spineWeight, turnAngle * spineWeight);
        ApplyRotationToBone(HumanBodyBones.Chest, lookAngle * chestWeight, turnAngle * chestWeight);
        ApplyRotationToBone(HumanBodyBones.Neck, lookAngle * neckWeight, turnAngle * neckWeight);
        ApplyRotationToBone(HumanBodyBones.Head, lookAngle * headWeight, turnAngle * headWeight);

        // Aim IK 처리
        if (isAiming)
        {
            // 오른손 IK
            if (rightHandTarget != null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
                animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
                animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
            }

            // 왼손 IK
            if (leftHandTarget != null && enableLeftHandIK)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
            }
        }
        else
        {
            // IK 초기화
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
        }
    }

    // Bone 회전 함수
    void ApplyRotationToBone(HumanBodyBones bone, float pitch, float yaw)
    {
        Transform boneTransform = animator.GetBoneTransform(bone);
        if (boneTransform == null) return;

        Quaternion originalRotation = boneTransform.localRotation;
        Quaternion lookRotation = Quaternion.Euler(pitch, yaw, 0);
        boneTransform.localRotation = originalRotation * lookRotation;
    }
}