using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
/// <summary>
/// 카메라 연결 방법 - 플레이어 또는 기타 controller 오브젝트에 붙여서 사용
/// DataBase.cs 파일에서 카메라 데이터 설정 가능
///해당 스크립트에 존재하는 변수 값 수정 X
/// </summary>


// 순수 Unity Camera Transform 제어 방식
public class CameraController : MonoBehaviour
{
    // 회전 각도 관련 변수 (수직만)
    private float currentVerticalAngle = 0f;   // pitch (상하 각도)
    private float targetVerticalAngle = 0f;
    private float rotationVelocity = 0f;
    
    // 카메라 거리 관련 변수
    private float maxCameraDistance = 5f;      // 최대 카메라 거리
    private float currentCameraDistance;       // 현재 카메라 거리
    private float targetCameraDistance;        // 목표 카메라 거리

    // 마우스 입력 관련 변수
    private float mouseY;
    
    // 회전 부드러움 관련 변수
    private float originalRotationSmoothTime;
    private bool rotationSmoothTimeStored = false;
    
    // 플레이어 참조
    private Transform playerTransform;
    private bool isPlayerFound = false;

    // 줌 관련 변수
    private float originalFOV;
    [HideInInspector] public static bool isZoomed = false;
    private Tween zoomTween;
    

    // 카메라 제어용
    private Camera mainCamera;
    
    // 데이터베이스 참조
    private DataBase.CameraData cameraData;
    
    // ✅ DataBase 캐싱된 값들 (성능 최적화)
    private float cachedMouseSensitivityY;
    private float cachedZoomMouseSensitivityY;
    private float cachedMinVerticalAngle;
    private float cachedMaxVerticalAngle;
    private float cachedRotationSmoothTime;
    private float cachedZoomValue;
    private float cachedZoomDuration;
    private bool cachedUseWallCollisionAvoidance;
    private float cachedCameraFix;
    private float cachedWallAvoidanceSpeed;
    private float cachedMaxCameraDistance;
    private float cachedPivotHeightOffset;
    private string cachedPlayerTag;
    private float cachedFindPlayerInterval;
    private bool dataBaseCached = false;


    void Awake()
    {
        // DataBase 정보 안전하게 캐싱
        CacheDataBaseInfo();
    }

    void Start()
    {
        // 메인 카메라 참조 얻기
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        // 카메라 시스템 초기화
        InitializeCamera();
            
        // 플레이어 찾기 시작
        StartCoroutine(FindPlayerRoutine());
    }
    
    /// <summary>
    /// DataBase 정보 안전하게 캐싱 (GameManager와 동일한 방식)
    /// </summary>
    void CacheDataBaseInfo()
    {
        try
        {
            if (DataBase.Instance != null && DataBase.Instance.cameraData != null)
            {
                cameraData = DataBase.Instance.cameraData;
                
                // 자주 사용되는 값들을 개별 변수로 캐싱
                cachedMouseSensitivityY = cameraData.MouseSensitivityY;
                cachedZoomMouseSensitivityY = cameraData.ZoomMouseSensitivityY;
                cachedMinVerticalAngle = cameraData.MinVerticalAngle;
                cachedMaxVerticalAngle = cameraData.MaxVerticalAngle;
                cachedRotationSmoothTime = cameraData.RotationSmoothTime;
                cachedZoomValue = cameraData.ZoomValue;
                cachedZoomDuration = cameraData.ZoomDuration;
                cachedUseWallCollisionAvoidance = cameraData.UseWallCollisionAvoidance;
                cachedCameraFix = cameraData.CameraFix;
                cachedWallAvoidanceSpeed = cameraData.WallAvoidanceSpeed;
                cachedMaxCameraDistance = cameraData.MaxCameraDistance;
                cachedPivotHeightOffset = cameraData.PivotHeightOffset;
                cachedPlayerTag = cameraData.PlayerTag;
                cachedFindPlayerInterval = cameraData.FindPlayerInterval;
                
                dataBaseCached = true;
                Debug.Log("✅ CameraController - DataBase 정보 캐싱 완료");
            }
            else
            {
                Debug.LogWarning("⚠️ CameraController - DataBase 접근 실패, 기본값 사용");
                dataBaseCached = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ CameraController - DataBase 캐싱 중 오류: {e.Message}");
            dataBaseCached = false;
        }
    }
    
    /// <summary>
    /// 카메라 시스템 초기화
    /// </summary>
    void InitializeCamera()
    {
        // 초기 카메라 거리 설정 (캐싱된 값 사용)
        maxCameraDistance = cachedMaxCameraDistance;
        currentCameraDistance = maxCameraDistance;
        targetCameraDistance = maxCameraDistance;
        
        // 초기 각도 설정 (수직만)
        currentVerticalAngle = 0f;
        targetVerticalAngle = 0f;
    }
    
    // InputManager 이벤트 구독
    void OnEnable()
    {
        InputManager.OnYMouseInput += OnYMouseInput; // Y축만 처리 (상하 시점)
        InputManager.OnZoomPressed += OnZoomPressed;
        InputManager.OnZoomCanceledPressed += OnZoomPressedCanceled;
    }
    
    void OnDisable()
    {
        InputManager.OnYMouseInput -= OnYMouseInput;
        InputManager.OnZoomPressed -= OnZoomPressed;
        InputManager.OnZoomCanceledPressed -= OnZoomPressedCanceled;
    }
    
    // InputManager에서 Y축 마우스 입력 받기 (상하 시점만 처리)
    void OnYMouseInput(Vector2 mouseInput)
    {
        if (isPlayerFound)
        {
            if(isZoomed)
            {
                mouseY = mouseInput.y * cachedZoomMouseSensitivityY * Time.deltaTime;
            }
            else
            {
                mouseY = mouseInput.y * cachedMouseSensitivityY * Time.deltaTime;
            }

            //사용 X
            // 거리 기반 감도 계산
            // float currentSensitivity = GetDistanceBasedSensitivity();    
            // Y축 처리 (수직 회전만)
            // float mouseY = mouseInput.y * currentSensitivity * Time.deltaTime;

            targetVerticalAngle -= mouseY; // Y축은 반전
            targetVerticalAngle = Mathf.Clamp(targetVerticalAngle, cachedMinVerticalAngle, cachedMaxVerticalAngle);
            
        }
    }

    void OnZoomPressed()
    {
        if (isPlayerFound)
        {
            // 카메라 확대(줌) 적용
            ApplyCameraZoom();
        }
    }

    void OnZoomPressedCanceled()
    {
        if (isPlayerFound)
        {
            // 카메라 확대(줌) 되돌리기 적용
            ApplyCameraZoomCanceled();
        }
    }
    
    // 플레이어를 주기적으로 찾는 코루틴
    IEnumerator FindPlayerRoutine()
    {
        while (!isPlayerFound)
        {
            FindPlayer();
            
            if (isPlayerFound)
            {
                SetupCamera();
                break;
            }
            
            yield return new WaitForSeconds(cachedFindPlayerInterval);
        }
    }
    
    // 플레이어 찾기
    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag(cachedPlayerTag);
        if (player != null)
        {
            playerTransform = player.transform;
            isPlayerFound = true;
        }
    }
    
    // 카메라 설정 - 필요 시 구현
    void SetupCamera()
    {
        if (playerTransform != null)
        {
            
        }
    }
    
 
    
    void LateUpdate()
    {
        if (isPlayerFound)
        {       
            // 벽 충돌 방지 처리 (캐싱된 값 사용)
            if (cachedUseWallCollisionAvoidance)
            {
                HandleWallCollisionAvoidance();
            }
            
            // 부드러운 회전 적용 (수직 각도만) (캐싱된 값 사용)
            currentVerticalAngle = Mathf.SmoothDamp(currentVerticalAngle, targetVerticalAngle, ref rotationVelocity, cachedRotationSmoothTime);
            
            // 3인칭 카메라 위치 및 회전 적용
            ApplyThirdPersonCamera();
        }
    }

    /// <summary>
    /// 3인칭 카메라 위치 및 회전 적용
    /// </summary>
    void ApplyThirdPersonCamera()
    {
        if (playerTransform == null || mainCamera == null) return;
        
        // 플레이어 피벗 포인트 계산 (카메라가 바라볼 지점) (캐싱된 값 사용)
        Vector3 pivotPoint = playerTransform.position + Vector3.up * cachedPivotHeightOffset;
        
        // 플레이어 뒤쪽 방향 계산 (플레이어가 바라보는 방향의 반대)
        Vector3 playerBackward = -playerTransform.forward;
        
        // 수직 각도 적용
        float verticalOffset = currentCameraDistance * Mathf.Sin(currentVerticalAngle * Mathf.Deg2Rad);
        float horizontalDistance = currentCameraDistance * Mathf.Cos(currentVerticalAngle * Mathf.Deg2Rad);
        
        // 카메라 위치 계산 (플레이어 뒤쪽 + 수직 오프셋)
        Vector3 cameraPosition = pivotPoint + playerBackward * horizontalDistance + Vector3.up * verticalOffset;
        
        // 카메라 위치 적용
        mainCamera.transform.position = cameraPosition;
        
        // 카메라가 항상 플레이어를 바라보도록 회전
        mainCamera.transform.LookAt(pivotPoint);
        
    }
    
    /// <summary>
    /// 3인칭 카메라 위치 계산
    /// </summary>
    Vector3 CalculateThirdPersonCameraPosition(Vector3 pivotPoint, float distance)
    {
        // 플레이어 뒤쪽 방향 계산 (플레이어가 바라보는 방향의 반대)
        Vector3 playerBackward = -playerTransform.forward;
        
        // 수직 각도 적용
        float verticalOffset = distance * Mathf.Sin(currentVerticalAngle * Mathf.Deg2Rad);
        float horizontalDistance = distance * Mathf.Cos(currentVerticalAngle * Mathf.Deg2Rad);
        
        // 카메라 위치 계산 (플레이어 뒤쪽 + 수직 오프셋)
        Vector3 cameraPosition = pivotPoint + playerBackward * horizontalDistance + Vector3.up * verticalOffset;
        
        return cameraPosition;
    }
    


    void ApplyCameraZoom()
    {
        if (mainCamera != null && !isZoomed)
        {
            isZoomed = true;
            
            // 현재 FOV를 원본 FOV로 저장 (첫 번째 줌 시에만)
            if (originalFOV == 0f)
            {
                originalFOV = mainCamera.fieldOfView;
            }
            
            // 회전 부드러움 값 저장 (첫 번째 줌 시에만) (캐싱된 값 사용)
            if (!rotationSmoothTimeStored)
            {
                originalRotationSmoothTime = cachedRotationSmoothTime;
                rotationSmoothTimeStored = true;
            }
            
            // 기존 애니메이션 중지
            zoomTween?.Kill();
            
            // 줌 인: FOV를 zoomValue만큼 감소 (캐싱된 값 사용)
            float targetFOV = originalFOV / cachedZoomValue;
            zoomTween = DOTween.To(() => mainCamera.fieldOfView, 
                                  x => mainCamera.fieldOfView = x, 
                                  targetFOV, cachedZoomDuration)
                               .SetEase(Ease.OutQuad);

            // 회전 부드러움을 0으로 설정 (즉시 반응) (캐싱된 값 직접 수정)
            cachedRotationSmoothTime = 0f;
        }
    }

    void ApplyCameraZoomCanceled()
    {
        if (mainCamera != null && isZoomed)
        {
            isZoomed = false;
            
            // 기존 애니메이션 중지
            zoomTween?.Kill();
            
            // 줌 아웃: 원본 FOV로 복원 (캐싱된 값 사용)
            zoomTween = DOTween.To(() => mainCamera.fieldOfView, 
                                x => mainCamera.fieldOfView = x, 
                                originalFOV, cachedZoomDuration)
                                .SetEase(Ease.OutQuad);

            // 회전 부드러움을 원본 값으로 복원 (캐싱된 값 사용)
            if (rotationSmoothTimeStored)
            {
                cachedRotationSmoothTime = originalRotationSmoothTime;
            }
        }
    }

    


    // ========================================
    // === DataBase 캐싱 유틸리티 메서드들 ===
    // ========================================
    
    /// <summary>
    /// DataBase가 성공적으로 캐싱되었는지 확인
    /// </summary>
    public bool IsDataBaseCached()
    {
        return dataBaseCached;
    }
    
    /// <summary>
    /// DataBase 정보 강제 새로고침
    /// </summary>
    public void RefreshDataBaseCache()
    {
        CacheDataBaseInfo();
    }

    // ========================================
    // === 유틸리티 함수들 ===
    // ========================================

    // 외부에서 플레이어 설정 가능 (수동 설정용)
    public void SetPlayer(Transform player)
    {
        playerTransform = player;
        isPlayerFound = true;
        SetupCamera();
    }
    
    // 플레이어 재찾기 (플레이어가 파괴되었을 때)
    public void ResetPlayer()
    {
        isPlayerFound = false;
        playerTransform = null;
        StartCoroutine(FindPlayerRoutine());
    }
    
    // 거리 기반 감도 계산
    float GetDistanceBasedSensitivity()
    {
        // 기본 감도 선택
        float baseSensitivity = isZoomed ? cameraData.ZoomMouseSensitivityY : cameraData.MouseSensitivityY;
        
        // 현재 카메라 거리 사용
        float currentDistance = currentCameraDistance;
        
        // 기준 거리 설정 (최대 카메라 거리)
        float baseDistance = maxCameraDistance;
        
        if (baseDistance <= 0f)
        {
            return baseSensitivity;
        }
        
        // 거리 비율 계산 (1.0 = 기준 거리, 0.5 = 절반 거리, 2.0 = 2배 거리)
        float distanceRatio = currentDistance / baseDistance;
        
        // 거리 비율에 따른 감도 조정
        // 카메라가 가까우면 감도 감소, 멀면 감도 증가
        float adjustedSensitivity = baseSensitivity * distanceRatio;
        
        // 감도 제한 (너무 극단적이지 않게)
        adjustedSensitivity = Mathf.Clamp(adjustedSensitivity, baseSensitivity * 0.1f, baseSensitivity * 3f);
        
        return adjustedSensitivity;
    }

    // ========================================
    // === 벽 충돌 방지 시스템 ===
    // ========================================
    
    /// <summary>
    /// 벽 충돌 방지 처리 메인 함수 (3인칭 카메라용)
    /// </summary>
    void HandleWallCollisionAvoidance()
    {
        if (playerTransform == null) return;
        
        // 이상적인 카메라 위치 계산 (3인칭 방식) (캐싱된 값 사용)
        Vector3 pivotPoint = playerTransform.position + Vector3.up * cachedPivotHeightOffset;
        Vector3 idealCameraPosition = CalculateThirdPersonCameraPosition(pivotPoint, maxCameraDistance);
        
        // 벽 충돌 감지 및 안전 거리 계산
        float safeDistance = PerformWallCollisionCheck(pivotPoint, idealCameraPosition);
        
        // 타겟 거리 업데이트
        targetCameraDistance = safeDistance;
        
        // 현재 거리를 타겟 거리로 부드럽게 보간 (캐싱된 값 사용)
        currentCameraDistance = Mathf.Lerp(currentCameraDistance, targetCameraDistance, 
            Time.deltaTime * cachedWallAvoidanceSpeed);
    }
    
    /// <summary>
    /// 벽 충돌 체크 및 안전 거리 반환
    /// </summary>
    float PerformWallCollisionCheck(Vector3 pivotPoint, Vector3 idealCameraPosition)
    {
        Vector3 directionToCamera = (idealCameraPosition - pivotPoint).normalized;
        float maxDistance = Vector3.Distance(pivotPoint, idealCameraPosition);
        
        // 레이캐스트로 벽 충돌 감지
        RaycastHit hit;
        if (Physics.Raycast(pivotPoint, directionToCamera, out hit, maxDistance))
        {
            // 플레이어 자신은 무시 (캐싱된 값 사용)
            if (hit.collider.CompareTag(cachedPlayerTag))
            {
                return maxDistance;
            }
            
            // 안전 거리 계산 (충돌 지점에서 약간 떨어진 위치) (캐싱된 값 사용)
            float safeDistance = hit.distance - cachedCameraFix;
            safeDistance = Mathf.Max(safeDistance, 0.5f); // 최소 거리 보장
            
            return safeDistance;
        }
        else
        {       
            return maxDistance;
        }
    }
    
    /// <summary>
    /// 각도 정규화 (0-360도 범위로 유지)
    /// </summary>
    float NormalizeAngle(float angle)
    {
        while (angle < 0f)
            angle += 360f;
        while (angle >= 360f)
            angle -= 360f;
        return angle;
    }

}