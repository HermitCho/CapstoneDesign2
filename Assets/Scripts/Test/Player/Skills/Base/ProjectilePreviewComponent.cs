// ProjectilePreviewComponent.cs
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ProjectilePreviewComponent : MonoBehaviour, IProjectilePreview
{
    [Header("궤적 설정")]
    [SerializeField] int segments = 30;
    [SerializeField] float timeStep = 0.05f;
    [SerializeField] float maxDistance = 100f; // 최대 궤적 거리

    [Header("마커 설정")]
    [SerializeField] GameObject targetMarkerPrefab; // 3D 구체 마커 참조
    private GameObject targetMarkerInstance;
    [SerializeField] bool showMarkerOnNoHit = true; // 충돌이 없을 때도 마커 표시 여부

    LineRenderer lr;
    private bool activeForLocal = false;
    private Vector3 lastValidPosition;
    private bool hitDetected = false; // 클래스 레벨로 이동

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = segments;
        lr.enabled = false;
        if (targetMarkerPrefab != null)
        {
            targetMarkerInstance = Instantiate(targetMarkerPrefab, transform);
            targetMarkerInstance.SetActive(false);
        }
    }

    public void StartPreview(MoveController owner)
    {
        hitDetected = false;

        if (owner != null && owner.photonView != null && owner.photonView.IsMine)
        {
            activeForLocal = true;
            lr.enabled = true;
            targetMarkerInstance.SetActive(true);
            Debug.Log("ProjectilePreviewComponent 초기화 성공!!!!" + owner != null && owner.photonView != null && owner.photonView.IsMine);
        }
        else
        {
            activeForLocal = false;
            lr.enabled = false;
            targetMarkerInstance.SetActive(false);
            Debug.Log("ProjectilePreviewComponent 초기화 실패ㅠㅜㅜ " + owner != null && owner.photonView != null && owner.photonView.IsMine);
        }
    }

    public void UpdatePreview(Vector3 origin, Vector3 direction, float initialSpeed)
    {
        if (!activeForLocal)
        {
            return;
        }
        if (lr == null)
        {
            return;
        }

        Vector3 v = direction.normalized * initialSpeed;
        Vector3 g = Physics.gravity;
        Vector3 previousPosition = origin;
        lastValidPosition = origin;
        hitDetected = false; // 이번 프레임의 충돌 감지 시작
        Debug.Log("dfdffdf"+ initialSpeed);

        for (int i = 0; i < segments; i++)
        {
            float t = i * timeStep;
            Vector3 currentPosition = origin + v * t + 0.5f * g * t * t;

            // 이전 위치와 현재 위치 사이의 충돌을 확인
            RaycastHit hit;
            Vector3 rayDirection = (currentPosition - previousPosition).normalized;
            float distance = Vector3.Distance(previousPosition, currentPosition);

            if (distance > 0.01f && Physics.Raycast(previousPosition, rayDirection, out hit, distance))
            {
                // 충돌 지점 찾음
                lr.positionCount = i + 1;
                lr.SetPosition(i, hit.point);
                lastValidPosition = hit.point;

                targetMarkerInstance.transform.position = hit.point;
                targetMarkerInstance.SetActive(true);
                //Debug.Log($"충돌 감지 - 마커 위치: {hit.point}, 마커 활성화: {targetMarker.activeInHierarchy}, 마커 Scale: {targetMarker.transform.localScale}");
                hitDetected = true;
                break; // 궤적 그리기 중단
            }

            // 충돌이 없으면 궤적 계속 그리기
            lr.positionCount = i + 1;
            lr.SetPosition(i, currentPosition);
            lastValidPosition = currentPosition;
            previousPosition = currentPosition;

            // 최대 거리 제한
            if (Vector3.Distance(origin, currentPosition) > maxDistance)
            {
                break;
            }
        }
        targetMarkerInstance.transform.position = lastValidPosition;
        targetMarkerInstance.SetActive(true);

        if (hitDetected)
        {
            Debug.Log("targetMarker.transform.root.gameObject.name + " + targetMarkerInstance.transform.root.gameObject.name);
            Debug.Log($"마커 상태: activeSelf={targetMarkerInstance.activeSelf}, activeInHierarchy={targetMarkerInstance.activeInHierarchy}");
            Debug.Log($"충돌 감지 완료 - 마커 위치: {lastValidPosition}, 마커 활성화: {targetMarkerInstance.activeInHierarchy}, 마커 Scale: {targetMarkerInstance.transform.localScale}");
        }
        else
        {
            Debug.Log($"마커 위치 업데이트 (충돌 없음): {lastValidPosition}, 마커 활성화: {targetMarkerInstance.activeInHierarchy}, 마커 Scale: {targetMarkerInstance.transform.localScale}");
        }
    }

    public void EndPreview()
    {
        activeForLocal = false;
        if (lr != null) lr.enabled = false;
        if (targetMarkerInstance != null) targetMarkerInstance.SetActive(false);
    }
}