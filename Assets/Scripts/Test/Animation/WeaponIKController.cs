using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponIKController : MonoBehaviour
{
    public Animator animator;

    [Header("IK 타겟")]
    public Transform rightHandTarget;
    public Transform leftHandTarget;

    [Header("힌트 타겟 (팔꿈치 방향)")]
    public Transform rightElbowHint;
    public Transform leftElbowHint;

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        // 오른손 IK
        if (rightHandTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);

            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
        }

        // 왼손 IK
        if (leftHandTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);

            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
        }

        // 오른팔 팔꿈치 Hint
        if (rightElbowHint != null)
        {
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 1);
            animator.SetIKHintPosition(AvatarIKHint.RightElbow, rightElbowHint.position);
        }

        // 왼팔 팔꿈치 Hint
        if (leftElbowHint != null)
        {
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 1);
            animator.SetIKHintPosition(AvatarIKHint.LeftElbow, leftElbowHint.position);
        }
    }

    // 왼손 IK를 설정하는 함수
    public void SetLeftHandIK(bool isEnabled)
    {
        // 왼손 IK 활성화/비활성화 로직
        if (isEnabled)
        {
            // 왼손 IK 활성화
            leftHandTarget.gameObject.SetActive(true);
        }
        else
        {
            // 왼손 IK 비활성화
            leftHandTarget.gameObject.SetActive(false);
        }
    }
}