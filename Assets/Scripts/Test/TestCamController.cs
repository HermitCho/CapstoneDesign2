using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using DG.Tweening;

public class TestCamController : MonoBehaviour
{
    [Header("Cinemachine Virtual Camera 설정")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    
    [Header("플레이어 설정")]
    [Tooltip("플레이어 태그")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("플레이어 찾는 간격 (초) 설정")]
    [Tooltip("Tag에 맞게 플레이어를 찾는 간격")]
    [Range(0.1f, 2f)]
    [SerializeField] private float findPlayerInterval = 0.5f;
    
    [Header("카메라 Y축 감도 설정")]
    [Tooltip("마우스 Y축 감도 조절")]
    [Range(0.1f, 3f)]
    [SerializeField] private float mouseSensitivityY = 1f;

    [Header("카메라 줌 Y축 감도 설정")]
    [Tooltip("마우스 줌 Y축 감도 조절")]
    [Range(0.1f, 3f)]
    [SerializeField] private float zoomMouseSensitivityY = 0.3f;
    
    [Header("카메라 Y축 아래 방향 제한 값 설정")]
    [Tooltip("수직 회전 최소 각도")]
    [Range(-2f, 0f)]
    [SerializeField] private float minVerticalAngle = 0.5f;

    [Header("카메라 Y축 위 방향 제한 값 설정")]
    [Tooltip("수직 회전 최대 각도")]
    [Range(0f, 10f)]
    [SerializeField] private float maxVerticalAngle = 5f;

    [Header("카메라 확대 배율 수치 설정")]
    [Tooltip("확대(줌) 배율")]
    [Range(0.1f, 5f)]
    [SerializeField] private float zoomValue = 2f;

    [Header("줌 애니메이션 시간 설정")]
    [Tooltip("줌 인/아웃 애니메이션 시간")]
    [Range(0.1f, 2f)]
    [SerializeField] private float zoomDuration = 0.3f;

    [Header("카메라 회전 부드러움 설정")]
    [Tooltip("카메라 회전 부드러움 시간 (낮을수록 더 민감)")]
    [Range(0f, 2f)]
    [SerializeField] private float rotationSmoothTime = 0f;

    
    // 현재 Y축 회전 각도
    private float currentVerticalAngle = 0f;
    private float targetVerticalAngle = 0f;
    private float rotationVelocity = 0f;
    
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
    private Tween horizontalDampingTween;
    private Tween verticalDampingTween;
    
    // Damping 관련 변수
    private float originalHorizontalDamping;
    private float originalVerticalDamping;
    private bool dampingValuesStored = false;
    
    // CinemachineTransposer 컴포넌트 참조
    private CinemachineTransposer transposer;
    // CinemachineComposer 컴포넌트 참조
    private CinemachineComposer composer;

    void Start()
    {
        // Virtual Camera 컴포넌트 찾기
        if (virtualCamera == null)
            virtualCamera = GetComponent<CinemachineVirtualCamera>();
            
        // CinemachineTransposer 컴포넌트 가져오기 (위치 제어용)
        transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        // CinemachineComposer 컴포넌트 가져오기 (Aim 제어용)
        composer = virtualCamera.GetCinemachineComponent<CinemachineComposer>();
            
        // 플레이어 찾기 시작
        StartCoroutine(FindPlayerRoutine());

        dampingValuesStored = false;
    }
    
    // InputManager 이벤트 구독
    void OnEnable()
    {
        InputManager.OnYMouseInput += OnYMouseInput;
        InputManager.OnZoomPressed += OnZoomPressed;
        InputManager.OnZoomCanceledPressed += OnZoomPressedCanceled;
    }
    
    void OnDisable()
    {
        InputManager.OnYMouseInput -= OnYMouseInput;
        InputManager.OnZoomPressed -= OnZoomPressed;
        InputManager.OnZoomCanceledPressed -= OnZoomPressedCanceled;
    }
    
    // InputManager에서 마우스 입력 받기
    void OnYMouseInput(Vector2 yMouseInput)
    {
        if (isPlayerFound)
        {
            float mouseY = yMouseInput.y * mouseSensitivityY * Time.deltaTime;
            if (isZoomed)
            {
                 mouseY = yMouseInput.y * zoomMouseSensitivityY * Time.deltaTime;
            }
            // 마우스 Y축 (수직 회전만)
            
            targetVerticalAngle -= mouseY; // Y축은 반전
            
            // 수직 각도 제한
            targetVerticalAngle = Mathf.Clamp(targetVerticalAngle, minVerticalAngle, maxVerticalAngle);
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
            
            yield return new WaitForSeconds(findPlayerInterval);
        }
    }
    
    // 플레이어 찾기
    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
            isPlayerFound = true;
        }
    }
    
    // 카메라 설정 (시네머신 인스펙터에서 이미 설정된 경우 간단하게)
    void SetupCamera()
    {
        if (virtualCamera != null && playerTransform != null)
        {
            // Follow와 Look At 설정만
            virtualCamera.Follow = playerTransform;
            virtualCamera.LookAt = playerTransform;
        }
    }
    
    void LateUpdate()
    {
        if (isPlayerFound)
        {
            // 부드러운 회전 적용
            currentVerticalAngle = Mathf.SmoothDamp(currentVerticalAngle, targetVerticalAngle, ref rotationVelocity, rotationSmoothTime);
            
            // 카메라 Y축 회전 적용
            ApplyCameraYRotation();
        }
    }

    void ApplyCameraYRotation()
    {
        if (virtualCamera != null && transposer != null)
        {
            transposer.m_FollowOffset = new Vector3(transposer.m_FollowOffset.x, currentVerticalAngle, transposer.m_FollowOffset.z);
        }
    }

    void ApplyCameraZoom()
    {
        if (virtualCamera != null && !isZoomed && composer != null)
        {
            isZoomed = true;
            // 현재 FOV를 원본 FOV로 저장 (첫 번째 줌 시에만)
            if (originalFOV == 0f)
            {
                originalFOV = virtualCamera.m_Lens.FieldOfView;
            }
            
            // 회전 부드러움 값 저장 (첫 번째 줌 시에만)
            if (!rotationSmoothTimeStored)
            {
                originalRotationSmoothTime = rotationSmoothTime;
                rotationSmoothTimeStored = true;
            }
            
            // Damping 값 저장 (첫 번째 줌 시에만)
            if (!dampingValuesStored)
            {
                originalHorizontalDamping = composer.m_HorizontalDamping;
                originalVerticalDamping = composer.m_VerticalDamping;
                dampingValuesStored = true;
            }
            
            // 기존 애니메이션들 중지
            zoomTween?.Kill();
           horizontalDampingTween?.Kill();
           verticalDampingTween?.Kill();
            
            // 줌 인: FOV를 zoomValue만큼 감소
            float targetFOV = originalFOV / zoomValue;
            zoomTween = DOTween.To(() => virtualCamera.m_Lens.FieldOfView, 
                                  x => virtualCamera.m_Lens.FieldOfView = x, 
                                  targetFOV, zoomDuration)
                               .SetEase(Ease.OutQuad);
            
            
            
            // Damping 값을 0으로 부드럽게 설정 (줌 시에는 더 민감하게)
            horizontalDampingTween = DOTween.To(() => composer.m_HorizontalDamping,
                                            x => composer.m_HorizontalDamping = x,
                                            0f, zoomDuration) 
                                            .SetEase(Ease.OutQuad);
            
            verticalDampingTween = DOTween.To(() => composer.m_VerticalDamping,
                                            x => composer.m_VerticalDamping = x,
                                            0.1f, zoomDuration) // 완전히 0이 아닌 0.1f로 설정
                                            .SetEase(Ease.OutQuad);

            // 회전 부드러움을 0으로 설정 (즉시 반응)
            rotationSmoothTime = 0f;
        }
    }

    void ApplyCameraZoomCanceled()
    {
        

        if (virtualCamera != null && isZoomed && composer != null)
        {
            isZoomed = false;
            // 기존 애니메이션들 중지
            zoomTween?.Kill();
            horizontalDampingTween?.Kill();
            verticalDampingTween?.Kill();
            
            // 줌 아웃: 원본 FOV로 복원
            zoomTween = DOTween.To(() => virtualCamera.m_Lens.FieldOfView, 
                                x => virtualCamera.m_Lens.FieldOfView = x, 
                                originalFOV, zoomDuration)
                                .SetEase(Ease.OutQuad);
                       
            // Damping 값을 원본으로 부드럽게 복원
            if (dampingValuesStored)
            {
                horizontalDampingTween = DOTween.To(() => composer.m_HorizontalDamping,
                                                x => composer.m_HorizontalDamping = x,
                                                originalHorizontalDamping, zoomDuration)
                                                .SetEase(Ease.OutQuad);
                
                 verticalDampingTween = DOTween.To(() => composer.m_VerticalDamping,
                                                x => composer.m_VerticalDamping = x,
                                                originalVerticalDamping, zoomDuration)
                                                .SetEase(Ease.OutQuad);
            }

            // 회전 부드러움을 원본 값으로 복원
            if (rotationSmoothTimeStored)
            {
                rotationSmoothTime = originalRotationSmoothTime;
            }
        }
    }
    
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
}
