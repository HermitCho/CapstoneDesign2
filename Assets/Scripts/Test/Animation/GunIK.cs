using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;

public class GunIK : MonoBehaviour
{
    public FullBodyBipedIK ik;
    public Transform leftHandTarget;

    // 왼손 IK On/Off 함수
    public void SetLeftHandIK(bool enable)
    {
        float weight = enable ? 1f : 0f;
        ik.solver.leftHandEffector.target = leftHandTarget;
        ik.solver.leftHandEffector.positionWeight = weight;
        ik.solver.leftHandEffector.rotationWeight = weight;
    }

    void LateUpdate()
    {
        // 평소에는 IK를 켜두고 싶으면 아래처럼 항상 1로 유지
        // (아니면 외부에서 SetLeftHandIK로 제어)
        // ik.solver.leftHandEffector.positionWeight = 1f;
        // ik.solver.leftHandEffector.rotationWeight = 1f;
    }
}
