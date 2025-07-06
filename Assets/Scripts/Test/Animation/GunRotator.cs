using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunRotator : MonoBehaviour
{
    [Header("카메라 & 조준 UI")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private RectTransform aimPointUI;

    [Header("총 회전 대상 (자기 자신 가능)")]
    [SerializeField] private Transform gunRoot;

    [Header("총구 기준 (회전 방향 계산용)")]
    [SerializeField] private Transform muzzle;

    [Header("설정")]
    [SerializeField] private float aimDistance = 100f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private LayerMask aimLayerMask = ~0; // 기본: 전부 감지


    private void Update()
    {
        if (muzzle != null)
        {
            Debug.DrawRay(muzzle.position, muzzle.forward * 5f, Color.red); // 총구
        }

        if (gunRoot != null)
        {
            Debug.DrawRay(gunRoot.position, gunRoot.forward * 5f, Color.blue); // 회전 대상
        }
    }
    private void LateUpdate()
    {
        if (mainCamera == null || aimPointUI == null || gunRoot == null || muzzle == null)
            return;

        // 화면 중앙 기준 레이 계산
        Vector3 screenPoint = aimPointUI.position;
        Ray ray = mainCamera.ScreenPointToRay(screenPoint);

        Vector3 targetPoint;

        // 레이캐스트로 조준 지점 확인
        if (Physics.Raycast(ray, out RaycastHit hit, aimDistance, aimLayerMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * aimDistance;
        }

        // 조준 방향 계산
        Vector3 aimDirection = (targetPoint - muzzle.position).normalized;

        // 현재 회전 → 목표 방향으로 부드럽게 회전
        Quaternion lookRotation = Quaternion.LookRotation(aimDirection);
        gunRoot.rotation = Quaternion.Slerp(gunRoot.rotation, lookRotation, Time.deltaTime * rotationSpeed);
    }
}