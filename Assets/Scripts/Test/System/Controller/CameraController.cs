using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Photon.Pun;
using Cinemachine;

/// <summary>
/// TPS 게임용 카메라 컨트롤러 - 캐릭터 바로 뒤쪽에 가상카메라 배치
/// 캐릭터 프리팹에 붙여서 사용
/// DataBase.cs 파일에서 카메라 데이터 설정 가능
/// 해당 스크립트에 존재하는 변수 값 수정 X
/// </summary>

public class CameraController : MonoBehaviourPun
{
    // 회전 각도 관련 변수 (수직만)
    private float targetVerticalAngle = 0f;

    // TPS용 고정 카메라 거리
    private const float TPS_CAMERA_SIDE = 0.5f;
    private const float TPS_CAMERA_DISTANCE = 2f;
    private const float TPS_CAMERA_HEIGHT = 1f;

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

    // Cinemachine 관련
    private CinemachineVirtualCamera virtualCamera;
    private Cinemachine3rdPersonFollow cam3rdPerson;
    private Camera mainCamera;

    // 카메라 조작 제어
    private bool cameraControlEnabled = true;
    private bool isLocalPlayer = false;

    // 데이터베이스 참조
    private DataBase.CameraData cameraData;

    // ✅ DataBase 캐싱된 값들 (성능 최적화)
    private float cachedMouseSensitivityX; // ✨ 수평 감도 캐싱 변수 추가
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
    private string cachedPlayerTag;
    private bool dataBaseCached = false;

    void Awake()
    {
        if (photonView == null)
        {
            Debug.LogError("❌ CameraController - PhotonView가 필요합니다!");
            return;
        }

        isLocalPlayer = photonView.IsMine;

        if (!isLocalPlayer)
        {
            this.enabled = false;
            return;
        }

        Debug.Log("✅ TPS CameraController - 로컬 플레이어 카메라 초기화 시작");
    }

    void Start()
    {
        if (!isLocalPlayer) return;

        CacheDataBaseInfo();
        playerTransform = transform;
        isPlayerFound = true;
        CreateVirtualCamera();
        InitializeCamera();
        Debug.Log("✅ TPS CameraController - 로컬 플레이어 카메라 설정 완료");
    }

    void CreateVirtualCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindObjectOfType<Camera>();

        if (mainCamera == null)
        {
            Debug.LogError("❌ TPS CameraController - 메인 카메라를 찾을 수 없습니다!");
            return;
        }

        CinemachineBrain brain = mainCamera.GetComponent<CinemachineBrain>();
        if (brain == null)
            brain = mainCamera.gameObject.AddComponent<CinemachineBrain>();

        GameObject vcamObj = new GameObject($"TPS_VirtualCamera_{photonView.ViewID}");
        vcamObj.transform.position = playerTransform.position;

        virtualCamera = vcamObj.AddComponent<CinemachineVirtualCamera>();
        virtualCamera.Priority = 10;
        virtualCamera.Follow = playerTransform;
        virtualCamera.LookAt = playerTransform;


        cam3rdPerson = virtualCamera.AddCinemachineComponent<Cinemachine3rdPersonFollow>();
        if (cam3rdPerson != null)
        {
            cam3rdPerson.ShoulderOffset = new Vector3(-0.5f, 1, -2);
        }

        Debug.Log("✅ TPS VirtualCamera 생성 및 설정 완료");
    }
    void CacheDataBaseInfo()
    {
        try
        {
            if (DataBase.Instance != null && DataBase.Instance.cameraData != null)
            {
                cameraData = DataBase.Instance.cameraData;

                // ✨ 수평 감도(MouseSensitivityX)를 DataBase에서 가져오도록 추가합니다.
                //    (만약 DataBase.cs에 MouseSensitivityX가 없다면 추가해주셔야 합니다)
                cachedMouseSensitivityX = cameraData.MouseSensitivityY;
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
                cachedPlayerTag = cameraData.PlayerTag;
                dataBaseCached = true;
                Debug.Log("✅ TPS CameraController - DataBase 정보 캐싱 완료");
            }
            else
            {
                Debug.LogWarning("⚠️ TPS CameraController - DataBase 접근 실패, 기본값 사용");
                dataBaseCached = false;
                SetDefaultValues();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ TPS CameraController - DataBase 캐싱 중 오류: {e.Message}");
            dataBaseCached = false;
            SetDefaultValues();
        }
    }

    void SetDefaultValues()
    {
        cachedMouseSensitivityX = 100f; // ✨ 수평 감도 기본값 추가
        cachedMouseSensitivityY = 100f;
        cachedZoomMouseSensitivityY = 50f;
        cachedMinVerticalAngle = -20f;
        cachedMaxVerticalAngle = 45f;
        cachedRotationSmoothTime = 0.08f;
        cachedZoomValue = 2f;
        cachedZoomDuration = 0.2f;
        cachedUseWallCollisionAvoidance = true;
        cachedCameraFix = 0.2f;
        cachedWallAvoidanceSpeed = 6f;
        cachedPlayerTag = "Player";
    }

    IEnumerator RetryCacheDataBaseInfo()
    {
        int maxRetries = 10;
        int currentRetry = 0;
        while (currentRetry < maxRetries)
        {
            yield return new WaitForSeconds(0.1f);
            if (DataBase.Instance != null)
            {
                CacheDataBaseInfo();
                yield break;
            }
            currentRetry++;
        }
        Debug.LogError("❌ TPS CameraController - DataBase 캐싱 최대 재시도 횟수 초과, 기본값 사용");
        dataBaseCached = false;
        SetDefaultValues();
    }

    void InitializeCamera()
    {
        targetVerticalAngle = 0f;
        if (virtualCamera != null)
        {
            originalFOV = virtualCamera.m_Lens.FieldOfView;
            var lens = virtualCamera.m_Lens;
            lens.FieldOfView = 60f;
            virtualCamera.m_Lens = lens;
        }
        Debug.Log("🎮 TPS Camera 초기화 완료");
    }

    void OnEnable()
    {
        if (!isLocalPlayer) return;

        // ✨ Y축 입력 이벤트 대신 통합된 마우스 입력 이벤트를 구독하도록 변경합니다.
        //    (InputManager에서 OnMouseInput 이라는 이름으로 Vector2를 보내준다고 가정)
        InputManager.OnYMouseInput += HandleMouseInput;
        InputManager.OnZoomPressed += OnZoomPressed;
        InputManager.OnZoomCanceledPressed += OnZoomPressedCanceled;
    }

    void OnDisable()
    {
        if (!isLocalPlayer) return;

        InputManager.OnYMouseInput -= HandleMouseInput;
        InputManager.OnZoomPressed -= OnZoomPressed;
        InputManager.OnZoomCanceledPressed -= OnZoomPressedCanceled;
    }

    // ✨ 마우스 입력을 통합적으로 처리하는 함수
    void HandleMouseInput(Vector2 mouseInput)
    {
        if (!cameraControlEnabled || !isPlayerFound || !isLocalPlayer)
            return;

        // 줌 상태일 때와 아닐 때 Y 감도만 사용
        float sensitivityX = isZoomed ? cachedZoomMouseSensitivityY : cachedMouseSensitivityY;
        float sensitivityY = isZoomed ? cachedZoomMouseSensitivityY : cachedMouseSensitivityY;


        float mouseY = mouseInput.y * sensitivityY * Time.deltaTime;
        float mouseX = mouseInput.x * sensitivityX;

        // 수직 각도 누적
        targetVerticalAngle -= mouseY;
        targetVerticalAngle = Mathf.Clamp(targetVerticalAngle, cachedMinVerticalAngle, cachedMaxVerticalAngle);

        // // 카메라 회전 적용
        // if (virtualCamera != null)
        // {
        //     Transform camTransform = virtualCamera.transform;
        //     Vector3 currentAngles = camTransform.eulerAngles;

        //     camTransform.rotation = Quaternion.Euler(targetVerticalAngle, currentAngles.y, 0f);
        // }
    }

    void OnZoomPressed()
    {
        // 카메라 조작이 비활성화되어 있으면 무시
        if (!cameraControlEnabled || !isPlayerFound)
        {
            return;
        }

        // 카메라 확대(줌) 적용
        ApplyCameraZoom();
    }

    void OnZoomPressedCanceled()
    {
        // 카메라 조작이 비활성화되어 있으면 무시
        if (!cameraControlEnabled || !isPlayerFound)
        {
            return;
        }

        // 카메라 확대(줌) 되돌리기 적용
        ApplyCameraZoomCanceled();
    }

    void LateUpdate()
    {
        if (!isLocalPlayer || !isPlayerFound || virtualCamera == null) return;

        if (cachedUseWallCollisionAvoidance)
        {
            HandleTPSWallCollisionAvoidance();
        }
    }

    // ✨ 벽 충돌 처리 로직을 Transposer의 오프셋을 직접 조절하는 방식으로 유지합니다.
    void HandleTPSWallCollisionAvoidance()
    {
        if (playerTransform == null || cam3rdPerson == null) return;

        Vector3 targetOffset;
        Vector3 originalOffset = new Vector3(-TPS_CAMERA_SIDE, TPS_CAMERA_HEIGHT, -TPS_CAMERA_DISTANCE);

        // 카메라의 실제 위치가 아닌, 플레이어 위치에서 원래 오프셋만큼 떨어진 가상의 위치를 기준으로 레이를 쏩니다.
        Vector3 rayOrigin = playerTransform.position + Vector3.up * TPS_CAMERA_HEIGHT; // 캐릭터 어깨 높이에서 시작
        Vector3 rayDirection = -playerTransform.forward;
        float rayDistance = TPS_CAMERA_DISTANCE;

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, ~LayerMask.GetMask(cachedPlayerTag)))
        {
            float safeDistance = hit.distance - cachedCameraFix;
            safeDistance = Mathf.Max(safeDistance, 0.3f);
            targetOffset = new Vector3(-TPS_CAMERA_SIDE, TPS_CAMERA_HEIGHT, -safeDistance);
        }
        else
        {
            targetOffset = originalOffset;
        }

        // 이 부분이 핵심입니다. Cinemachine3rdPersonFollow의 ShoulderOffset을 Raycast 결과에 따라 업데이트합니다.
        cam3rdPerson.ShoulderOffset = Vector3.Lerp(cam3rdPerson.ShoulderOffset, targetOffset, Time.deltaTime * cachedWallAvoidanceSpeed);
    }

    void ApplyCameraZoom()
    {
        if (virtualCamera != null && !isZoomed)
        {
            isZoomed = true;


            if (originalFOV == 0f)
            {
                originalFOV = virtualCamera.m_Lens.FieldOfView;
            }

            if (!rotationSmoothTimeStored)
            {
                originalRotationSmoothTime = cachedRotationSmoothTime;
                rotationSmoothTimeStored = true;
            }

            zoomTween?.Kill();

            float targetFOV = originalFOV / cachedZoomValue;
            zoomTween = DOTween.To(() => virtualCamera.m_Lens.FieldOfView,
                       x =>
                       {
                           var lens = virtualCamera.m_Lens;
                           lens.FieldOfView = x;
                           virtualCamera.m_Lens = lens;
                       },
                       targetFOV, cachedZoomDuration)
                     .SetEase(Ease.OutQuad);

            // 줌 시 더 정밀한 제어
            cachedRotationSmoothTime *= 0.3f;
        }
    }

    void ApplyCameraZoomCanceled()
    {
        if (virtualCamera != null && isZoomed)
        {
            isZoomed = false;

            zoomTween?.Kill();

            zoomTween = DOTween.To(() => virtualCamera.m_Lens.FieldOfView,
                      x =>
                      {
                          var lens = virtualCamera.m_Lens;
                          lens.FieldOfView = x;
                          virtualCamera.m_Lens = lens;
                      },
                      originalFOV, cachedZoomDuration)
                      .SetEase(Ease.OutQuad);



            if (rotationSmoothTimeStored)
            {
                cachedRotationSmoothTime = originalRotationSmoothTime;
            }
        }
    }



    // ========================================
    // === 유틸리티 메서드들 ===
    // ========================================



    public bool IsDataBaseCached()
    {
        return dataBaseCached;
    }



    public void RefreshDataBaseCache()
    {
        CacheDataBaseInfo();
    }



    public void EnableCameraControl()
    {
        if (!isLocalPlayer) return;

        cameraControlEnabled = true;
        Debug.Log("✅ TPS CameraController: 카메라 조작 활성화");
    }



    public void DisableCameraControl()
    {
        if (!isLocalPlayer) return;

        cameraControlEnabled = false;
        Debug.Log("❌ TPS CameraController: 카메라 조작 비활성화");
    }



    public bool IsCameraControlEnabled()
    {
        return cameraControlEnabled;
    }



    public bool IsLocalPlayer()
    {
        return isLocalPlayer;
    }



    public float GetTargetVerticalAngle()
    {
        return targetVerticalAngle;
    }

    void OnDestroy()
    {
        zoomTween?.Kill();

        // VirtualCamera 정리 (로컬 플레이어인 경우만)
        if (isLocalPlayer && virtualCamera != null)

        {
            DestroyImmediate(virtualCamera.gameObject);
        }
    }
}