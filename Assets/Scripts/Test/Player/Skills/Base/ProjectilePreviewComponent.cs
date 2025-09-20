// ProjectilePreviewComponent.cs
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ProjectilePreviewComponent : MonoBehaviour, IProjectilePreview
{
    [Header("궤적 설정")]
    [SerializeField] int segments = 30;
    [SerializeField] float timeStep = 0.05f;
    [SerializeField] float maxDistance = 100f; // 최대 궤적 거리
    
    [Header("마커 설정")]
    [SerializeField] GameObject targetMarker; // 3D 구체 마커 참조
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
        if (targetMarker != null) targetMarker.SetActive(false);
    }

    public void StartPreview(MoveController owner)
    {
        Debug.Log($"ProjectilePreviewComponent StartPreview 호출됨 - owner: {owner?.name}");
        
        hitDetected = false; // 프리뷰 시작 시 초기화
        
        if (owner != null && owner.photonView != null && owner.photonView.IsMine)
        {
            activeForLocal = true;
            lr.enabled = true;
            if (targetMarker != null) 
            {
                targetMarker.SetActive(true);
                Debug.Log($"Target Marker 활성화됨: {targetMarker.name}");
            }
            else
            {
                Debug.LogWarning("Target Marker가 null입니다!");
            }
        }
        else
        {
            activeForLocal = false;
            lr.enabled = false;
            if (targetMarker != null) targetMarker.SetActive(false);
        }
    }

    public void UpdatePreview(Vector3 origin, Vector3 direction, float initialSpeed)
    {
        if (!activeForLocal) 
        {
            Debug.Log("UpdatePreview: activeForLocal이 false입니다");
            return;
        }
        if (lr == null) 
        {
            Debug.Log("UpdatePreview: LineRenderer가 null입니다");
            return;
        }

        Debug.Log($"UpdatePreview 호출됨 - origin: {origin}, direction: {direction}, speed: {initialSpeed}");

        Vector3 v = direction.normalized * initialSpeed;
        Vector3 g = Physics.gravity;
        Vector3 previousPosition = origin;
        lastValidPosition = origin;
        hitDetected = false; // 이번 프레임의 충돌 감지 시작

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
                
                if (targetMarker != null)
                {
                    targetMarker.transform.position = hit.point;
                    targetMarker.transform.localScale = Vector3.one * 5f; // 임시로 크게 만들기
                    targetMarker.SetActive(true);
                    Debug.Log($"충돌 감지 - 마커 위치: {hit.point}, 마커 활성화: {targetMarker.activeInHierarchy}, 마커 Scale: {targetMarker.transform.localScale}");
                }
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

        // 마커 처리 - hitDetected 상태와 관계없이 항상 마커 표시
        if (targetMarker != null)
        {
            targetMarker.transform.position = lastValidPosition;
            targetMarker.transform.localScale = Vector3.one * 5f; // 임시로 크게 만들기
            targetMarker.SetActive(true);
            
            if (hitDetected)
            {
                Debug.Log($"충돌 감지 완료 - 마커 위치: {lastValidPosition}, 마커 활성화: {targetMarker.activeInHierarchy}, 마커 Scale: {targetMarker.transform.localScale}");
            }
            else
            {
                Debug.Log($"마커 위치 업데이트 (충돌 없음): {lastValidPosition}, 마커 활성화: {targetMarker.activeInHierarchy}, 마커 Scale: {targetMarker.transform.localScale}");
            }
        }
        else
        {
            Debug.LogWarning("Target Marker가 null입니다!");
        }
    }

    public void EndPreview()
    {
        activeForLocal = false;
        if (lr != null) lr.enabled = false;
        if (targetMarker != null) targetMarker.SetActive(false);
    }
}