using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;

public class GunIK : MonoBehaviour
{
    public FullBodyBipedIK ik;
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    public Transform bodyTarget;
    public Transform rightLegTarget;
    public Transform leftLegTarget;
    public Transform headTarget;

    // 통합 Effector 제어 메서드
    public void SetEffectorPositionWeight(FullBodyBipedEffector effector, Transform target, float positionWeight, float rotationWeight = -1f)
    {
        var e = ik.solver.GetEffector(effector);
        e.target = target;
        e.positionWeight = positionWeight;
        if (rotationWeight >= 0f) e.rotationWeight = rotationWeight;
    }

    void LateUpdate()
    {
        // 평소에는 IK를 켜두고 싶으면 아래처럼 항상 1로 유지
        // (아니면 외부에서 SetLeftHandIK로 제어)
        ik.solver.rightHandEffector.positionWeight = 1f;
        // ik.solver.leftHandEffector.rotationWeight = 1f;
    }
}
