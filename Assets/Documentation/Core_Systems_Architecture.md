# 🎮 코어 시스템 구조 및 동작 가이드

> **작성일**: 2024년  
> **버전**: v1.0  
> **프로젝트**: CapstoneDesign2 - 코어 게임 시스템

---

## 📋 목차
1. [전체 코어 시스템 구조](#-전체-코어-시스템-구조)
2. [시스템 초기화 순서](#-시스템-초기화-순서)
3. [DataBase 시스템](#-database-시스템)
4. [InputManager 시스템](#-inputmanager-시스템)
5. [플레이어 이동 시스템](#-플레이어-이동-시스템)
6. [카메라 제어 시스템](#-카메라-제어-시스템)
7. [실시간 동작 과정](#-실시간-동작-과정)
8. [이벤트 기반 통신](#-이벤트-기반-통신)
9. [물리 시스템 연동](#-물리-시스템-연동)
10. [성능 최적화](#-성능-최적화)

---

## 🏗️ 전체 코어 시스템 구조

```
🎯 DataBase (싱글톤 - 설정 데이터)
    └── 모든 시스템이 참조
        ├── CameraData (카메라 설정)
        ├── PlayerMoveData (플레이어 이동 설정)
        └── TeddyBearData (테디베어 설정)
            ↓
📱 InputManager (입력 처리 중앙 허브)
    ├── Unity Input System 래핑
    ├── 정적 이벤트 발송
    └── 실시간 입력값 제공
        ↓
🎮 MoveController (플레이어 동작)
    ├── InputManager 이벤트 구독
    ├── DataBase.PlayerMoveData 참조
    ├── Rigidbody 기반 물리 이동
    └── 지상/공중 이동 분리 처리
        ↓
📷 CameraController (카메라 제어)
    ├── InputManager 이벤트 구독 (Y축 마우스만)
    ├── DataBase.CameraData 참조
    ├── 플레이어 추적 (3인칭)
    └── 벽 충돌 방지 시스템
```

### 시스템별 역할

| 시스템 | 역할 | 위치 | 의존성 |
|--------|------|------|--------|
| **DataBase** | 모든 설정값 중앙 관리 | `Singleton<DataBase>` | 없음 (최상위) |
| **InputManager** | Unity Input System 래핑, 이벤트 발송 | `MonoBehaviour` | PlayerAction.inputactions |
| **MoveController** | 플레이어 이동, 점프, 물리 처리 | `MonoBehaviour` | DataBase, InputManager |
| **CameraController** | 3인칭 카메라 제어, 플레이어 추적 | `MonoBehaviour` | DataBase, InputManager |

---

## 🚀 시스템 초기화 순서

### 1. **DataBase 초기화 (최우선)**
```
Singleton<DataBase>.Instance 생성
    ├── DontDestroyOnLoad 설정
    ├── 중복 인스턴스 제거
    └── 설정 데이터 준비
        ├── CameraData (감도, FOV, 거리 등)
        ├── PlayerMoveData (속도, 점프, 공중제어 등)
        └── TeddyBearData (부착, 점수, 발광 등)
```

### 2. **InputManager 초기화 (Awake → OnEnable)**
```
Awake():
├── PlayerAction 인스턴스 생성
└── Input System 준비

OnEnable():
├── PlayerAction.Enable() 실행
├── 모든 입력 이벤트 등록
│   ├── Player.Move, Jump, Skill, Item
│   ├── Player.Rotation, YRotation
│   ├── Player.Zoom, Shoot, Reload
│   └── UI.ItemUI
└── 예외 처리로 안전한 등록
```

### 3. **MoveController 초기화 (Awake → OnEnable)**
```
Awake():
├── Rigidbody 컴포넌트 획득
├── DataBase.PlayerMoveData 참조
└── 메인 카메라 탐색

OnEnable():
├── InputManager 이벤트 구독
│   ├── OnMoveInput (WASD)
│   ├── OnXMouseInput (좌우 회전)
│   ├── OnJumpPressed (점프)
│   ├── OnSkillPressed (스킬)
│   └── OnItemPressed (아이템)
├── 마우스 잠금 (MouseLock)
└── 입력 처리 준비
```

### 4. **CameraController 초기화 (Awake → Start)**
```
Awake():
└── DataBase.CameraData 참조

Start():
├── 메인 카메라 탐색
├── 카메라 시스템 초기화
│   ├── maxCameraDistance 설정
│   ├── 초기 각도 설정 (0도)
│   └── FOV 원본값 저장
├── FindPlayerRoutine() 코루틴 시작
└── 입력 이벤트 구독 (OnEnable)

OnEnable():
├── InputManager.OnYMouseInput 구독 (상하 시점만)
├── InputManager.OnZoomPressed 구독
└── InputManager.OnZoomCanceledPressed 구독
```

### 5. **플레이어 탐색 및 연결**
```
CameraController.FindPlayerRoutine():
├── 주기적으로 PlayerTag 탐색 (0.5초 간격)
├── 플레이어 발견 시 Transform 저장
├── isPlayerFound = true 설정
└── SetupCamera() 호출 (필요시 추가 설정)
```

---

## 📊 DataBase 시스템

### **싱글톤 구조**
```csharp
public class DataBase : Singleton<DataBase>
{
    public CameraData cameraData;
    public PlayerMoveData playerMoveData;
    public TeddyBearData teddyBearData;
}
```

### **CameraData 설정**
```csharp
[System.Serializable]
public class CameraData
{
    // 플레이어 탐색
    private string playerTag = "PlayerPosition";
    private float findPlayerInterval = 0.5f;
    
    // 마우스 감도
    private float mouseSensitivityY = 1f;
    private float zoomMouseSensitivityY = 0.3f;
    
    // 회전 제한
    private float minVerticalAngle = 0.5f;
    private float maxVerticalAngle = 5f;
    private float rotationSmoothTime = 0f;
    
    // 줌 설정
    private float zoomValue = 2f;
    private float zoomDuration = 0.3f;
    
    // 벽 충돌 방지
    private bool useWallCollisionAvoidance = true;
    private float cameraFix = 1f;
    private float wallAvoidanceSpeed = 5f;
    
    // 카메라 오프셋
    private float maxCameraDistance = 5f;
    private float pivotHeightOffset = 1.5f;
}
```

### **PlayerMoveData 설정**
```csharp
[System.Serializable]
public class PlayerMoveData
{
    // 이동 관련
    private float speed = 5f;
    private float rotationSpeed = 6f;
    private float zoomRotationSpeed = 2f;
    private float mouseInputTimeout = 0f;
    
    // 점프 관련
    private float jumpCooldown = 3f;
    private float jumpHeight = 3f;
    private float jumpBufferTime = 0.1f;
    
    // 공중 이동
    private float airControlForce = 10f;
    private float airAcceleration = 25f;
    private float airMaxSpeed = 8f;
    private float maxAirSpeedMultiplier = 1.5f;
    
    // 물리 상호작용
    private float landingFriction = 0.3f;
    private float wallCollisionFallForce = 8f;
    private float minHorizontalSpeedRatio = 0.7f;
    private float maxHorizontalSpeedReduction = 0.2f;
}
```

### **데이터 접근 패턴**
```csharp
// 초기화 시 한 번 참조 저장 (권장)
void Awake()
{
    playerMoveData = DataBase.Instance.playerMoveData;
}

// 실시간 접근 (성능상 비권장)
void Update()
{
    float speed = DataBase.Instance.playerMoveData.Speed;
}
```

---

## 📱 InputManager 시스템

### **Unity Input System 래핑**
```csharp
public class InputManager : MonoBehaviour
{
    [SerializeField] private PlayerAction playerAction;
    
    // 현재 입력값들 (정적 속성으로 외부 접근)
    public static Vector2 MoveInput { get; private set; }
    public static Vector2 XMouseInput { get; private set; }
    public static Vector2 YMouseInput { get; private set; }
    public static bool JumpPressed { get; private set; }
    public static bool ZoomPressed { get; private set; }
    // ... 기타 입력들
    
    // 이벤트들 (구독 방식으로 외부 알림)
    public static event Action<Vector2> OnMoveInput;
    public static event Action<Vector2> OnXMouseInput;
    public static event Action<Vector2> OnYMouseInput;
    public static event Action OnJumpPressed;
    public static event Action OnZoomPressed;
    // ... 기타 이벤트들
}
```

### **입력 처리 흐름**
```
사용자 입력 (키보드/마우스)
    ↓
Unity Input System (PlayerAction.inputactions)
    ↓
InputManager 콜백 메서드들
    ├── OnMove() → moveInput 저장 → OnMoveInput 이벤트 발송
    ├── OnMouseX() → xMouseInput 저장 → OnXMouseInput 이벤트 발송
    ├── OnMouseY() → yMouseInput 저장 → OnYMouseInput 이벤트 발송
    ├── OnJump() → jumpPressed 저장 → OnJumpPressed 이벤트 발송
    └── OnZoom() → zoomPressed 저장 → OnZoomPressed 이벤트 발송
        ↓
구독자들 (MoveController, CameraController 등)
    ├── 이벤트 수신 → 즉시 처리
    └── 정적 속성 읽기 → 매 프레임 처리
```

### **이벤트 등록/해제 패턴**
```csharp
void OnEnable()
{
    playerAction.Enable();
    
    try
    {
        // Player Actions
        playerAction.Player.Move.performed += OnMove;
        playerAction.Player.Move.canceled += OnMove;
        playerAction.Player.Rotation.performed += OnMouseX;
        playerAction.Player.YRotation.performed += OnMouseY;
        playerAction.Player.Jump.performed += OnJump;
        
        // UI Actions  
        playerAction.UI.ItemUI.performed += OnItemUI;
        playerAction.UI.ItemUI.canceled += OnItemUICanceled;
    }
    catch (System.Exception e)
    {
        Debug.LogError($"입력 이벤트 등록 실패: {e.Message}");
    }
}

void OnDisable()
{
    if (playerAction == null) return;
    
    playerAction.Disable();
    // 모든 이벤트 해제...
}
```

### **입력 타입별 처리**

#### **연속 입력 (Move, Mouse)**
```csharp
void OnMove(InputAction.CallbackContext context)
{
    moveInput = context.ReadValue<Vector2>();
    MoveInput = moveInput;
    OnMoveInput?.Invoke(moveInput);
}

// 사용: 매 프레임 처리
void Update()
{
    if (InputManager.MoveInput.magnitude > 0.1f)
    {
        // 이동 처리
    }
}
```

#### **단발 입력 (Jump, Skill)**
```csharp
void OnJump(InputAction.CallbackContext context)
{
    jumpPressed = context.performed;
    JumpPressed = jumpPressed;
    
    if (jumpPressed)
    {
        OnJumpPressed?.Invoke();
    }
}

// 사용: 이벤트 기반 처리
void OnEnable()
{
    InputManager.OnJumpPressed += OnJumpInput;
}
```

#### **토글 입력 (Zoom)**
```csharp
void OnZoom(InputAction.CallbackContext context)
{
    zoomPressed = context.performed;
    ZoomPressed = zoomPressed;
    
    if (zoomPressed)
        OnZoomPressed?.Invoke();
}

void OnZoomCanceled(InputAction.CallbackContext context)
{
    zoomPressed = false;
    ZoomPressed = zoomPressed;
    OnZoomCanceledPressed?.Invoke();
}
```

---

## 🎮 플레이어 이동 시스템

### **MoveController 구조**
```csharp
public class MoveController : MonoBehaviour
{
    // 데이터 참조
    private DataBase.PlayerMoveData playerMoveData;
    private Rigidbody playerRigidbody;
    
    // 입력 관련
    private Vector2 rawMoveInput;
    private float rotationAmount;
    private float lastMouseInputTime;
    
    // 점프 관련
    private float lastJumpTime = 0f;
    private float jumpBufferTimer = 0f;
    private bool isGrounded = false;
    private bool wasGrounded = false;
    
    // 벽 충돌 관련
    private bool isAgainstWall = false;
    private Vector3 wallNormal = Vector3.zero;
}
```

### **실시간 이동 처리 (Update)**
```csharp
void Update()
{
    UpdateGroundedState();    // 1. 지면 상태 확인
    HandleMovement();         // 2. 이동 처리
    HandleRotation();         // 3. 회전 처리
    UpdateJumpBuffer();       // 4. 점프 버퍼 업데이트
    HandleLanding();          // 5. 착지 처리
}
```

#### **1. 지면 상태 확인**
```csharp
void UpdateGroundedState()
{
    wasGrounded = isGrounded;
    isGrounded = CheckGrounded(); // Raycast로 지면 체크
}

private bool CheckGrounded()
{
    RaycastHit hit;
    float groundCheckDistance = 1.1f;
    return Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance);
}
```

#### **2. 이동 처리 (지상/공중 분리)**
```csharp
void HandleMovement()
{
    if (rawMoveInput.magnitude < 0.1f) return;
    
    Vector3 playerRelativeMovement = GetPlayerRelativeMovement(rawMoveInput);
    
    if (isGrounded)
    {
        // 지상 이동: 즉시 반응
        Vector3 movement = playerRelativeMovement * playerMoveData.Speed * Time.deltaTime;
        transform.Translate(movement, Space.World);
    }
    else
    {
        // 공중 이동: 에어 스트레이핑
        HandleAirMovement(playerRelativeMovement);
    }
}
```

#### **3. 공중 이동 (에어 스트레이핑)**
```csharp
void HandleAirMovement(Vector3 wishDirection)
{
    Vector3 currentVelocity = playerRigidbody.velocity;
    Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
    
    // 벽에 붙어있으면 벽 방향 이동 차단
    if (isAgainstWall)
    {
        float dotProduct = Vector3.Dot(wishDirection, wallNormal);
        if (dotProduct < 0)
        {
            wishDirection = wishDirection - Vector3.Project(wishDirection, wallNormal);
            wishDirection = wishDirection.normalized;
            if (wishDirection.magnitude < 0.1f) return;
        }
    }
    
    // 현재 속도와 원하는 방향의 내적
    float currentSpeed = Vector3.Dot(horizontalVelocity, wishDirection);
    
    // 가속할 수 있는 속도 계산
    float addSpeed = playerMoveData.AirMaxSpeed - currentSpeed;
    if (addSpeed <= 0) return;
    
    // 가속도 적용
    float accelerationSpeed = playerMoveData.AirAcceleration * Time.deltaTime;
    if (accelerationSpeed > addSpeed)
        accelerationSpeed = addSpeed;
    
    // 힘 적용
    Vector3 force = wishDirection * accelerationSpeed;
    playerRigidbody.AddForce(force, ForceMode.VelocityChange);
}
```

#### **4. 점프 시스템**
```csharp
void OnJumpInput()
{
    if (isGrounded)
    {
        PerformJump(); // 즉시 점프
    }
    else
    {
        // 점프 버퍼 활성화 (착지 직전 점프 입력 허용)
        jumpBufferTimer = playerMoveData.JumpBufferTime;
    }
}

void PerformJump()
{
    if (Time.time - lastJumpTime < playerMoveData.JumpCooldown) return;
    
    Vector3 currentVelocity = playerRigidbody.velocity;
    
    // 점프 높이를 직접 계산
    float jumpVelocity = Mathf.Sqrt(2f * playerMoveData.JumpHeight * Mathf.Abs(Physics.gravity.y));
    
    // 수평 관성 유지하면서 수직 속도만 변경
    playerRigidbody.velocity = new Vector3(currentVelocity.x, jumpVelocity, currentVelocity.z);
    
    lastJumpTime = Time.time;
    isGrounded = false;
}
```

#### **5. 벽 충돌 처리**
```csharp
void OnCollisionEnter(Collision collision)
{
    if (!isGrounded && !collision.gameObject.CompareTag("Player"))
    {
        Vector3 currentVelocity = playerRigidbody.velocity;
        
        // 벽 상태 설정
        isAgainstWall = true;
        wallNormal = collision.contacts[0].normal;
        
        // 위쪽으로 올라가는 속도가 있을 때만 처리
        if (currentVelocity.y > 0)
        {
            // 수직 속도만 0으로 만들어서 중력이 자연스럽게 작용
            playerRigidbody.velocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            
            // 벽에서 밀어내기
            Vector3 pushDirection = -horizontalVelocity.normalized;
            Vector3 pushForce = pushDirection * 1.5f;
            playerRigidbody.AddForce(pushForce, ForceMode.VelocityChange);
        }
    }
}
```

---

## 📷 카메라 제어 시스템

### **CameraController 구조**
```csharp
public class CameraController : MonoBehaviour
{
    // 회전 관련
    private float currentVerticalAngle = 0f;
    private float targetVerticalAngle = 0f;
    private float rotationVelocity = 0f;
    
    // 거리 관련
    private float maxCameraDistance = 5f;
    private float currentCameraDistance;
    private float targetCameraDistance;
    
    // 플레이어 추적
    private Transform playerTransform;
    private bool isPlayerFound = false;
    
    // 줌 관련
    private float originalFOV;
    public static bool isZoomed = false;
    private Tween zoomTween;
    
    // 컴포넌트
    private Camera mainCamera;
    private DataBase.CameraData cameraData;
}
```

### **실시간 카메라 업데이트 (LateUpdate)**
```csharp
void LateUpdate()
{
    if (isPlayerFound)
    {
        // 1. 벽 충돌 방지 처리
        if (cameraData.UseWallCollisionAvoidance)
        {
            HandleWallCollisionAvoidance();
        }
        
        // 2. 부드러운 회전 적용 (수직만)
        currentVerticalAngle = Mathf.SmoothDamp(
            currentVerticalAngle, 
            targetVerticalAngle, 
            ref rotationVelocity, 
            cameraData.RotationSmoothTime
        );
        
        // 3. 3인칭 카메라 위치 및 회전 적용
        ApplyThirdPersonCamera();
    }
}
```

### **3인칭 카메라 위치 계산**
```csharp
void ApplyThirdPersonCamera()
{
    if (playerTransform == null || mainCamera == null) return;
    
    // 1. 플레이어 피벗 포인트 계산 (카메라가 바라볼 지점)
    Vector3 pivotPoint = playerTransform.position + Vector3.up * cameraData.PivotHeightOffset;
    
    // 2. 플레이어 뒤쪽 방향 계산
    Vector3 playerBackward = -playerTransform.forward;
    
    // 3. 수직 각도 적용
    float verticalOffset = currentCameraDistance * Mathf.Sin(currentVerticalAngle * Mathf.Deg2Rad);
    float horizontalDistance = currentCameraDistance * Mathf.Cos(currentVerticalAngle * Mathf.Deg2Rad);
    
    // 4. 카메라 위치 계산
    Vector3 cameraPosition = pivotPoint + playerBackward * horizontalDistance + Vector3.up * verticalOffset;
    
    // 5. 카메라 위치 및 회전 적용
    mainCamera.transform.position = cameraPosition;
    mainCamera.transform.LookAt(pivotPoint);
}
```

### **벽 충돌 방지 시스템**
```csharp
void HandleWallCollisionAvoidance()
{
    if (playerTransform == null) return;
    
    // 1. 이상적인 카메라 위치 계산
    Vector3 pivotPoint = playerTransform.position + Vector3.up * cameraData.PivotHeightOffset;
    Vector3 idealCameraPosition = CalculateThirdPersonCameraPosition(pivotPoint, maxCameraDistance);
    
    // 2. 벽 충돌 감지 및 안전 거리 계산
    float safeDistance = PerformWallCollisionCheck(pivotPoint, idealCameraPosition);
    
    // 3. 타겟 거리 업데이트
    targetCameraDistance = safeDistance;
    
    // 4. 현재 거리를 타겟 거리로 부드럽게 보간
    currentCameraDistance = Mathf.Lerp(
        currentCameraDistance, 
        targetCameraDistance, 
        Time.deltaTime * cameraData.WallAvoidanceSpeed
    );
}

float PerformWallCollisionCheck(Vector3 pivotPoint, Vector3 idealCameraPosition)
{
    Vector3 directionToCamera = (idealCameraPosition - pivotPoint).normalized;
    float maxDistance = Vector3.Distance(pivotPoint, idealCameraPosition);
    
    // 레이캐스트로 벽 충돌 감지
    RaycastHit hit;
    if (Physics.Raycast(pivotPoint, directionToCamera, out hit, maxDistance))
    {
        // 플레이어 자신은 무시
        if (hit.collider.CompareTag(cameraData.PlayerTag))
        {
            return maxDistance;
        }
        
        // 안전 거리 계산 (충돌 지점에서 약간 떨어진 위치)
        float safeDistance = hit.distance - cameraData.CameraFix;
        safeDistance = Mathf.Max(safeDistance, 0.5f); // 최소 거리 보장
        
        return safeDistance;
    }
    else
    {
        return maxDistance;
    }
}
```

### **줌 시스템 (DOTween 기반)**
```csharp
void ApplyCameraZoom()
{
    if (mainCamera != null && !isZoomed)
    {
        isZoomed = true;
        
        // 원본 FOV 저장 (첫 번째 줌 시에만)
        if (originalFOV == 0f)
        {
            originalFOV = mainCamera.fieldOfView;
        }
        
        // 기존 애니메이션 중지
        zoomTween?.Kill();
        
        // 줌 인: FOV를 zoomValue만큼 감소
        float targetFOV = originalFOV / cameraData.ZoomValue;
        zoomTween = DOTween.To(
            () => mainCamera.fieldOfView, 
            x => mainCamera.fieldOfView = x, 
            targetFOV, 
            cameraData.ZoomDuration
        ).SetEase(Ease.OutQuad);
        
        // 회전 부드러움을 0으로 설정 (즉시 반응)
        cameraData.RotationSmoothTime = 0f;
    }
}
```

---

## 🔄 실시간 동작 과정

### **매 프레임 처리 순서**

#### **1. Input Phase (가장 먼저)**
```
사용자 입력
    ↓
Unity Input System
    ↓
InputManager 콜백들
    ├── OnMove() → rawMoveInput 업데이트
    ├── OnMouseX() → rotationAmount 업데이트  
    ├── OnMouseY() → targetVerticalAngle 업데이트
    └── OnJump() → jumpPressed 업데이트 + 이벤트 발송
```

#### **2. Update Phase (물리 업데이트 전)**
```
MoveController.Update():
├── UpdateGroundedState() → Raycast로 지면 체크
├── HandleMovement() → 이동 처리 (지상/공중 분리)
├── HandleRotation() → 플레이어 Y축 회전
├── UpdateJumpBuffer() → 점프 버퍼 시간 감소
└── HandleLanding() → 착지 시 마찰 적용
```

#### **3. Physics Phase (Unity 내부)**
```
Unity Physics System:
├── Rigidbody.AddForce() 적용
├── 중력 적용
├── 충돌 감지 및 해결
└── 속도/위치 업데이트
```

#### **4. LateUpdate Phase (카메라 처리)**
```
CameraController.LateUpdate():
├── HandleWallCollisionAvoidance() → 레이캐스트로 벽 체크
├── currentVerticalAngle 부드럽게 보간
└── ApplyThirdPersonCamera() → 카메라 위치/회전 적용
```

### **입력 처리 타이밍**

#### **즉시 처리 (이벤트 기반)**
```
점프 입력 → OnJumpPressed 이벤트 → PerformJump() 즉시 실행
줌 입력 → OnZoomPressed 이벤트 → ApplyCameraZoom() 즉시 실행
스킬 입력 → OnSkillPressed 이벤트 → 스킬 로직 즉시 실행
```

#### **연속 처리 (매 프레임)**
```
이동 입력 → rawMoveInput 저장 → Update()에서 매 프레임 처리
마우스 입력 → rotationAmount 저장 → Update()에서 매 프레임 처리
```

---

## ⚡ 이벤트 기반 통신

### **InputManager → 다른 시스템들**
```csharp
// InputManager에서 발송
public static event Action<Vector2> OnMoveInput;
public static event Action<Vector2> OnXMouseInput;  
public static event Action<Vector2> OnYMouseInput;
public static event Action OnJumpPressed;

// MoveController에서 구독
void OnEnable()
{
    InputManager.OnMoveInput += OnMoveInput;
    InputManager.OnXMouseInput += OnMouseInput;
    InputManager.OnJumpPressed += OnJumpInput;
}

// CameraController에서 구독
void OnEnable()
{
    InputManager.OnYMouseInput += OnYMouseInput; // Y축만
    InputManager.OnZoomPressed += OnZoomPressed;
}
```

### **시스템 간 독립성**
```
InputManager (중앙 허브)
    ├── MoveController (이동, 점프, 회전)
    ├── CameraController (카메라 상하, 줌)
    ├── WeaponController (발사, 재장전) - 구현 예정
    └── UIController (아이템 UI, ESC) - 이미 구현됨
    
각 시스템은 서로 직접 참조하지 않고
InputManager를 통해서만 소통
```

### **정적 속성 vs 이벤트**

#### **연속 입력: 정적 속성 사용**
```csharp
// InputManager
public static Vector2 MoveInput { get; private set; }

// MoveController
void Update()
{
    // 매 프레임 체크
    Vector2 currentInput = InputManager.MoveInput;
    if (currentInput.magnitude > 0.1f)
    {
        // 이동 처리
    }
}
```

#### **단발 입력: 이벤트 사용**
```csharp
// InputManager  
public static event Action OnJumpPressed;

// MoveController
void OnJumpInput()
{
    // 점프 입력 시 즉시 한 번만 실행
    PerformJump();
}
```

---

## 🔧 물리 시스템 연동

### **Rigidbody 설정**
```csharp
// MoveController
void Awake()
{
    playerRigidbody = GetComponent<Rigidbody>();
    
    // 권장 설정:
    // Mass: 1
    // Drag: 0
    // Angular Drag: 0.05
    // Use Gravity: true
    // Is Kinematic: false
}
```

### **물리 기반 이동**

#### **지상 이동: Transform 기반**
```csharp
if (isGrounded)
{
    // 즉시 반응을 위해 Transform.Translate 사용
    Vector3 movement = playerRelativeMovement * playerMoveData.Speed * Time.deltaTime;
    transform.Translate(movement, Space.World);
}
```

#### **공중 이동: Rigidbody 기반**
```csharp
else
{
    // 물리 법칙을 따르도록 AddForce 사용
    Vector3 force = wishDirection * accelerationSpeed;
    playerRigidbody.AddForce(force, ForceMode.VelocityChange);
}
```

#### **점프: 수직 속도 직접 설정**
```csharp
void PerformJump()
{
    Vector3 currentVelocity = playerRigidbody.velocity;
    
    // 물리 공식으로 점프 속도 계산
    float jumpVelocity = Mathf.Sqrt(2f * playerMoveData.JumpHeight * Mathf.Abs(Physics.gravity.y));
    
    // 수평 관성 유지, 수직 속도만 변경
    playerRigidbody.velocity = new Vector3(currentVelocity.x, jumpVelocity, currentVelocity.z);
}
```

### **충돌 처리**

#### **지면 감지: Raycast**
```csharp
private bool CheckGrounded()
{
    RaycastHit hit;
    float groundCheckDistance = 1.1f;
    return Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance);
}
```

#### **벽 충돌: OnCollision 이벤트**
```csharp
void OnCollisionEnter(Collision collision)
{
    if (!isGrounded && !collision.gameObject.CompareTag("Player"))
    {
        // 벽 법선 저장
        wallNormal = collision.contacts[0].normal;
        isAgainstWall = true;
        
        // 물리적 반응 처리
        HandleWallBounce(collision);
    }
}
```

---

## 🚀 성능 최적화

### **1. 참조 캐싱**
```csharp
// ❌ 비효율적: 매번 컴포넌트 검색
void Update()
{
    GetComponent<Rigidbody>().AddForce(force);
}

// ✅ 효율적: 한 번만 캐싱
void Awake()
{
    playerRigidbody = GetComponent<Rigidbody>();
}

void Update()
{
    playerRigidbody.AddForce(force);
}
```

### **2. DataBase 접근 최적화 (고급 캐싱 시스템)**

#### **기본 캐싱 방식**
```csharp
// ❌ 비효율적: 매번 싱글톤 접근
void Update()
{
    float speed = DataBase.Instance.playerMoveData.Speed;
}

// ✅ 효율적: 초기화 시 캐싱
void Awake()
{
    playerMoveData = DataBase.Instance.playerMoveData;
}

void Update()
{
    float speed = playerMoveData.Speed;
}
```

#### **고급 안전 캐싱 시스템 (GameManager/CameraController/MoveController 적용)**
```csharp
public class MoveController : MonoBehaviour
{
    // 캐싱된 값들 (성능 최적화)
    private float cachedSpeed;
    private float cachedRotationSpeed;
    private float cachedJumpHeight;
    private bool dataBaseCached = false;
    
    void Awake()
    {
        CacheDataBaseInfo(); // 안전한 캐싱
    }
    
    void CacheDataBaseInfo()
    {
        try
        {
            if (DataBase.Instance != null && DataBase.Instance.playerMoveData != null)
            {
                playerMoveData = DataBase.Instance.playerMoveData;
                
                // 자주 사용되는 값들을 개별 변수로 캐싱
                cachedSpeed = playerMoveData.Speed;
                cachedRotationSpeed = playerMoveData.RotationSpeed;
                cachedJumpHeight = playerMoveData.JumpHeight;
                
                dataBaseCached = true;
                Debug.Log("✅ MoveController - DataBase 정보 캐싱 완료");
            }
            else
            {
                Debug.LogWarning("⚠️ MoveController - DataBase 접근 실패");
                dataBaseCached = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ MoveController - DataBase 캐싱 오류: {e.Message}");
            dataBaseCached = false;
        }
    }
    
    void HandleMovement()
    {
        // 캐싱된 값 사용 (DataBase에 직접 접근하지 않음)
        Vector3 movement = direction * cachedSpeed * Time.deltaTime;
    }
    
    // 유틸리티 메서드들
    public bool IsDataBaseCached() => dataBaseCached;
    public void RefreshDataBaseCache() => CacheDataBaseInfo();
}
```

#### **캐싱 시스템의 장점**
```
✅ 성능 향상: 매 프레임 싱글톤 접근 → 캐싱된 변수 접근
✅ 안정성: try-catch로 런타임 오류 방지
✅ 디버깅 용이: 캐싱 상태 확인 가능
✅ 메모리 효율: 자주 사용하는 값만 별도 캐싱
✅ 확장성: RefreshCache()로 런타임 업데이트 가능
```

#### **시스템별 캐싱 적용 현황**
```
📁 GameManager: TeddyBear 관련 값들 캐싱
   ├── cachedScoreIncreaseTime
   ├── cachedScoreIncreaseRate  
   └── cachedDetachReattachTime

📁 CameraController: 카메라 관련 값들 캐싱
   ├── cachedMouseSensitivityY
   ├── cachedZoomValue
   ├── cachedWallAvoidanceSpeed
   └── cachedPivotHeightOffset

📁 MoveController: 플레이어 이동 관련 값들 캐싱
   ├── cachedSpeed
   ├── cachedJumpHeight
   ├── cachedAirAcceleration
   └── cachedRotationSpeed
```

### **3. 조건부 처리**
```csharp
void Update()
{
    // 입력이 있을 때만 처리
    if (rawMoveInput.magnitude > 0.1f)
    {
        HandleMovement();
    }
    
    // 마우스 입력 타임아웃 체크
    if (Time.time - lastMouseInputTime <= playerMoveData.MouseInputTimeout)
    {
        HandleRotation();
    }
}
```

### **4. 물리 최적화**
```csharp
// 연속 충돌 감지는 필요한 경우에만
rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete; // 기본값

// 빠르게 움직이는 객체만 Continuous 사용
rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
```

### **5. 카메라 최적화**
```csharp
// 플레이어가 발견된 후에만 LateUpdate 처리
void LateUpdate()
{
    if (!isPlayerFound) return;
    
    // 벽 충돌 검사를 설정에 따라 선택적 실행
    if (cameraData.UseWallCollisionAvoidance)
    {
        HandleWallCollisionAvoidance();
    }
}
```

### **6. 코루틴 활용**
```csharp
// 플레이어 탐색을 별도 코루틴으로 분리
IEnumerator FindPlayerRoutine()
{
    while (!isPlayerFound)
    {
        FindPlayer();
        if (isPlayerFound) break;
        
        // 0.5초마다 탐색 (매 프레임 탐색 방지)
        yield return new WaitForSeconds(cameraData.FindPlayerInterval);
    }
}
```

---

## 📋 문제 해결 가이드

### **자주 발생하는 문제들**

#### 1. **입력이 반응하지 않는 경우**
```
원인: InputManager의 PlayerAction이 null이거나 비활성화
해결: OnEnable에서 playerAction.Enable() 확인
확인: Console에서 "PlayerAction이 활성화되었습니다" 로그 체크
```

#### 2. **플레이어가 벽을 통과하는 경우**
```
원인: Rigidbody의 Collision Detection 모드 문제
해결: CollisionDetectionMode를 Continuous로 변경
확인: Rigidbody 컴포넌트의 Collision Detection 설정
```

#### 3. **카메라가 플레이어를 찾지 못하는 경우**
```
원인: PlayerTag가 잘못 설정되었거나 태그가 없음
해결: DataBase.cameraData.PlayerTag 확인
확인: "PlayerPosition" 태그가 플레이어 자식 오브젝트에 있는지 확인
```

#### 4. **점프가 작동하지 않는 경우**
```
원인: 지면 감지 Raycast가 제대로 작동하지 않음
해결: CheckGrounded()의 groundCheckDistance 조정
확인: Scene 뷰에서 Raycast 디버그 라인 표시
```

#### 5. **마우스 감도가 이상한 경우**
```
원인: Time.deltaTime이 중복 적용되거나 누락
해결: InputManager와 CameraController에서 deltaTime 사용 확인
확인: DataBase에서 감도 값 조정
```

---

## 🔧 확장 가능성

### **추가 구현 예정 기능들**

#### 1. **WeaponController**
```csharp
public class WeaponController : MonoBehaviour
{
    void OnEnable()
    {
        InputManager.OnShootPressed += OnShootPressed;
        InputManager.OnReloadPressed += OnReloadPressed;
    }
    
    // 무기 발사, 재장전, 조준 등
}
```

#### 2. **SkillController**
```csharp
public class SkillController : MonoBehaviour
{
    void OnEnable()
    {
        InputManager.OnSkillPressed += OnSkillPressed;
    }
    
    // 스킬 사용, 쿨다운 관리 등
}
```

#### 3. **고급 이동 시스템**
```csharp
// 벽 달리기
private bool isWallRunning = false;
private Vector3 wallRunDirection;

// 슬라이딩
private bool isSliding = false;
private float slideTimer = 0f;

// 대시
private bool canDash = true;
private float dashCooldown = 2f;
```

#### 4. **카메라 시스템 확장**
```csharp
// 카메라 셰이크
public void ShakeCamera(float intensity, float duration);

// 동적 FOV (속도에 따른 FOV 변화)
void UpdateDynamicFOV();

// 카메라 전환 (1인칭 ↔ 3인칭)
void SwitchCameraMode();
```

---

**문서 마지막 업데이트**: 2024년  
**작성자**: AI Assistant  
**문의사항**: 코어 시스템 확장 또는 최적화 시 이 문서 참조 