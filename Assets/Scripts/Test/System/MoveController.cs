using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// 플레이어 이동 컨트롤러
/// DataBase.cs 파일에서 플레이어 이동 데이터 설정 가능
///해당 스크립트에 존재하는 변수 값 수정 X
/// 플레이어 오브젝트에 붙여서 사용
/// 단 CameraController.cs가 검사하는 PlayerTag는 플레이어 오브젝트에 붙이는 것이 아닌 플레이어 자식으로 선언된 오브젝트에 붙여야 함
/// 자식 오브젝트는 trigger가 설정된 Collider 컴포넌트가 있어야 함. 
///해당 자식 오브젝트는 플레이어 기준 왼쪽에 부착되어 있어 카메라는 해당 오브젝트 참조 -> 조준점과 이동 방향의 일치
/// 예시 - 플레이어 오브젝트에 붙인 자식 오브젝트에 PlayerTag 붙이고 그 오브젝트에 붙여서 사용   
/// </summary>


public class MoveController : MonoBehaviour
{
    private DataBase.PlayerMoveData playerMoveData;
    private Rigidbody playerRigidbody;
    private Vector2 rawMoveInput; // 원본 입력값 저장

    // ✅ DataBase 캐싱된 값들 (성능 최적화)
    private float cachedSpeed;
    private float cachedRotationSpeed;
    private float cachedZoomRotationSpeed;
    private float cachedMouseInputTimeout;
    private float cachedJumpCooldown;
    private float cachedJumpHeight;
    private float cachedJumpBufferTime;
    private float cachedAirAcceleration;
    private float cachedAirMaxSpeed;
    private float cachedLandingFriction;
    private bool dataBaseCached = false;

    private float rotationAmount;
    
    // 마우스 입력 타이머 (마우스 입력이 없으면 정지)
    private float lastMouseInputTime;
    
    // 점프 관련 변수 
    private float lastJumpTime = 0f;
    private float jumpBufferTimer = 0f;
    private bool isGrounded = false;
    private bool wasGrounded = false;
    private bool isAgainstWall = false; // 벽에 붙어있는 상태 추적
    private Vector3 wallNormal = Vector3.zero; // 벽의 법선 벡터 저장
    
    // 카메라 참조
    private Camera mainCamera;

    // InputManager 이벤트 구독
    void OnEnable()
    {
        // InputManager 이벤트 구독
        InputManager.OnMoveInput += OnMoveInput;
        InputManager.OnXMouseInput += OnMouseInput;
        InputManager.OnJumpPressed += OnJumpInput;
        InputManager.OnSkillPressed += OnSkillInput;
        InputManager.OnItemPressed += OnItemInput;

        MouseLock();
    }

    void OnDisable()
    {
        // InputManager 이벤트 구독 해제
        InputManager.OnMoveInput -= OnMoveInput;
        InputManager.OnXMouseInput -= OnMouseInput;
        InputManager.OnJumpPressed -= OnJumpInput;
        InputManager.OnSkillPressed -= OnSkillInput;
        InputManager.OnItemPressed -= OnItemInput;
    }

    void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        
        // DataBase 정보 안전하게 캐싱
        CacheDataBaseInfo();
        
        // 메인 카메라 찾기
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
    }
    
    /// <summary>
    /// DataBase 정보 안전하게 캐싱 (GameManager와 동일한 방식)
    /// </summary>
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
                cachedZoomRotationSpeed = playerMoveData.ZoomRotationSpeed;
                cachedMouseInputTimeout = playerMoveData.MouseInputTimeout;
                cachedJumpCooldown = playerMoveData.JumpCooldown;
                cachedJumpHeight = playerMoveData.JumpHeight;
                cachedJumpBufferTime = playerMoveData.JumpBufferTime;
                cachedAirAcceleration = playerMoveData.AirAcceleration;
                cachedAirMaxSpeed = playerMoveData.AirMaxSpeed;
                cachedLandingFriction = playerMoveData.LandingFriction;
                
                dataBaseCached = true;
                Debug.Log("✅ MoveController - DataBase 정보 캐싱 완료");
            }
            else
            {
                Debug.LogWarning("⚠️ MoveController - DataBase 접근 실패, 기본값 사용");
                dataBaseCached = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ MoveController - DataBase 캐싱 중 오류: {e.Message}");
            dataBaseCached = false;
        }
    }

    void Update()
    {
        UpdateGroundedState();
        HandleMovement();
        HandleRotation();
        UpdateJumpBuffer();
        HandleLanding();
    }

    // 지면 상태 업데이트
    void UpdateGroundedState()
    {
        wasGrounded = isGrounded;
        isGrounded = CheckGrounded();
    }

    //움직임 처리
    void HandleMovement()
    {
        if (rawMoveInput.magnitude < 0.1f) return;

        Vector3 playerRelativeMovement = GetPlayerRelativeMovement(rawMoveInput);

        if (isGrounded)
        {
            // 지상 이동 (즉시 반응) (캐싱된 값 사용)
            Vector3 movement = playerRelativeMovement * cachedSpeed * Time.deltaTime;
            transform.Translate(movement, Space.World);
        }
        else
        {
            // 공중 이동- 에어 스트레이핑 적용
            HandleAirMovement(playerRelativeMovement);
        }
    }

    // 공중 이동
    void HandleAirMovement(Vector3 wishDirection)
    {
        Vector3 currentVelocity = playerRigidbody.velocity;
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        
        // 벽에 붙어있는 상태에서 벽 쪽으로 이동하려고 하면 차단
        if (isAgainstWall)
        {
            // 벽 법선과 이동 방향의 내적 계산 (음수면 벽 쪽으로 이동하려는 것)
            float dotProduct = Vector3.Dot(wishDirection, wallNormal);
            if (dotProduct < 0)
            {
                // 벽 쪽으로 이동하려고 하면 그 방향 성분을 제거
                wishDirection = wishDirection - Vector3.Project(wishDirection, wallNormal);
                wishDirection = wishDirection.normalized;
                
                // 벽 방향 성분이 제거되어 이동 방향이 거의 없으면 리턴
                if (wishDirection.magnitude < 0.1f)
                    return;
            }
        }
        
        // 현재 속도와 원하는 방향의 내적
        float currentSpeed = Vector3.Dot(horizontalVelocity, wishDirection);
        
        // 가속할 수 있는 속도 계산 (캐싱된 값 사용)
        float addSpeed = cachedAirMaxSpeed - currentSpeed;
        if (addSpeed <= 0) return;
        
        // 가속도 적용 (캐싱된 값 사용)
        float accelerationSpeed = cachedAirAcceleration * Time.deltaTime;
        if (accelerationSpeed > addSpeed)
            accelerationSpeed = addSpeed;
        
        // 힘 적용
        Vector3 force = wishDirection * accelerationSpeed;
        playerRigidbody.AddForce(force, ForceMode.VelocityChange);
    }

    // 점프 버퍼 업데이트
    void UpdateJumpBuffer()
    {
        if (jumpBufferTimer > 0)
        {
            jumpBufferTimer -= Time.deltaTime;
            
            // 착지했고 점프 버퍼가 활성화되어 있으면 점프 실행
            if (isGrounded && jumpBufferTimer > 0)
            {
                PerformJump();
                jumpBufferTimer = 0f;
            }
        }
    }

    // 착지 처리
    void HandleLanding()
    {
        // 공중에서 땅으로 착지한 순간
        if (!wasGrounded && isGrounded)
        {
            Vector3 currentVelocity = playerRigidbody.velocity;
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            Vector3 reducedVelocity = horizontalVelocity * cachedLandingFriction; // 캐싱된 값 사용
            
            playerRigidbody.velocity = new Vector3(reducedVelocity.x, currentVelocity.y, reducedVelocity.z);
            
            // 착지 시 벽 상태 해제
            isAgainstWall = false;
            wallNormal = Vector3.zero;
        }
    }

    // InputManager에서 이동 입력 받기
    void OnMoveInput(Vector2 moveInput)
    {
        rawMoveInput = moveInput;
    }

    // InputManager에서 마우스 입력 받기
    void OnMouseInput(Vector2 mouseInput)
    {
        float mouseX = mouseInput.x;
       
        
        rotationAmount = mouseX * Time.deltaTime * cachedRotationSpeed; // 캐싱된 값 사용

        if (CameraController.isZoomed)
        {
            rotationAmount = mouseX * Time.deltaTime * cachedZoomRotationSpeed; // 캐싱된 값 사용
        }

        lastMouseInputTime = Time.time;
    }

    // 회전 처리
    void HandleRotation()
    {   
        if (Time.time - lastMouseInputTime > cachedMouseInputTimeout) // 캐싱된 값 사용
        {
            rotationAmount = 0;
        }
        transform.Rotate(Vector3.up, rotationAmount);
    }

    // InputManager에서 점프 입력 받기
    void OnJumpInput()
    {      
        if (isGrounded)
        {
            // 즉시 점프
            PerformJump();
        }
        else
        {
            // 점프 버퍼 활성화 (착지 직전 점프 입력 허용) (캐싱된 값 사용)
            jumpBufferTimer = cachedJumpBufferTime;
        }
    }

    // 점프 실행
    void PerformJump()
    {
        if (Time.time - lastJumpTime < cachedJumpCooldown) return; // 캐싱된 값 사용

        // 수직 속도만 리셋 (수평 속도는 유지)
        Vector3 currentVelocity = playerRigidbody.velocity;
        
        // 점프 높이를 직접 계산 (캐싱된 값 사용)
        float jumpVelocity = Mathf.Sqrt(2f * cachedJumpHeight * Mathf.Abs(Physics.gravity.y));
            
        // 수평 관성 유지하면서 수직 속도만 변경
        playerRigidbody.velocity = new Vector3(currentVelocity.x, jumpVelocity, currentVelocity.z);
        
        lastJumpTime = Time.time;
        isGrounded = false; // 점프 시 즉시 공중 상태로 변경
    }

    // 지면 체크
    private bool CheckGrounded()
    {
        RaycastHit hit;
        float groundCheckDistance = 1.1f;
        
        return Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance);
    }

    public void MouseLock()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 벽 충돌 시 자연스럽게 떨어지도록 처리
    void OnCollisionEnter(Collision collision)
    {
        // 공중에 있을 때만 처리
        if (!isGrounded)
        {
            // 벽이나 장애물과 충돌했을 때
            if (!collision.gameObject.CompareTag("Player"))
            {
                Vector3 currentVelocity = playerRigidbody.velocity;
                
                // 벽 상태 설정
                isAgainstWall = true;
                wallNormal = collision.contacts[0].normal;
                
                // 위쪽으로 올라가는 속도가 있을 때만 처리 (떨어지는 중이면 그대로 둠)
                if (currentVelocity.y > 0)
                {
                    // 수직 속도만 0으로 만들어서 중력이 자연스럽게 작용하도록 함
                    playerRigidbody.velocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
                    
                    // 플레이어의 이동 방향을 기준으로 밀어내기 방향 계산
                    Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
                    
                    if (horizontalVelocity.magnitude > 0.1f)
                    {
                        // 플레이어가 이동하던 방향의 반대로 밀어내기
                        Vector3 pushDirection = -horizontalVelocity.normalized;
                        Vector3 pushForce = pushDirection * 1.5f;
                        playerRigidbody.AddForce(pushForce, ForceMode.VelocityChange);
                    }
                    else
                    {
                        // 속도가 거의 없으면 벽 법선 방향으로 밀어내기
                        Vector3 pushDirection = new Vector3(wallNormal.x, 0, wallNormal.z).normalized;
                        Vector3 pushForce = pushDirection * 1.5f;
                        playerRigidbody.AddForce(pushForce, ForceMode.VelocityChange);
                    }
                }
            }
        }
    }
    
    // 벽에 계속 붙어있는 상태 방지
    void OnCollisionStay(Collision collision)
    {
        // 공중에 있고 수직 속도가 거의 0일 때 (벽에 붙어있는 상태)
        if (!isGrounded)
        {
            if (!collision.gameObject.CompareTag("Player"))
            {
                Vector3 currentVelocity = playerRigidbody.velocity;
                
                // 벽 상태 유지
                isAgainstWall = true;
                wallNormal = collision.contacts[0].normal;
                
                // 수직 속도가 거의 없고 공중에 있으면 벽에 붙어있는 상태
                if (Mathf.Abs(currentVelocity.y) < 0.5f)
                {
                    // 벽에서 약간 밀어내기 (벽의 법선 방향으로)
                    Vector3 pushDirection = new Vector3(wallNormal.x, 0, wallNormal.z).normalized;
                    Vector3 pushForce = pushDirection * 1.5f; // 지속적인 약한 밀어내기
                    playerRigidbody.AddForce(pushForce, ForceMode.Force);
                }
            }
        }
    }
    
    // 벽에서 떨어졌을 때
    void OnCollisionExit(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
        {
            // 벽 상태 해제
            isAgainstWall = false;
            wallNormal = Vector3.zero;
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

    // InputManager에서 스킬 입력 받기
    void OnSkillInput()
    {
        Debug.Log("E키가 눌렸습니다 - 스킬 사용");
    }

    // InputManager에서 아이템 입력 받기
    void OnItemInput()
    {
        Debug.Log("Q키가 눌렸습니다 - 아이템 사용");
    }

    // 플레이어 기준 이동 방향 계산
    Vector3 GetPlayerRelativeMovement(Vector2 input)
    {
        // 플레이어 기준 방향 벡터 계산
        Vector3 playerForward = transform.forward;
        Vector3 playerRight = transform.right;
        
        // Y축 제거 (수평 이동만)
        playerForward.y = 0;
        playerRight.y = 0;
        
        playerForward = playerForward.normalized;
        playerRight = playerRight.normalized;
        
        // 입력에 따른 이동 방향 계산
        Vector3 moveDirection = playerForward * input.y + playerRight * input.x;
        
        return moveDirection.normalized;
    }

    // 카메라 기준 이동 방향 계산 (참고용 - 현재 사용 안 함)
    Vector3 GetCameraRelativeMovement(Vector2 input)
    {
        if (mainCamera == null)
        {
            return new Vector3(input.x, 0, input.y).normalized;
        }
       
        return GetCameraForwardRightMovement(input);
    }

    // Camera-Relative 방식 (참고용 - 현재 사용 안 함)
    Vector3 GetCameraForwardRightMovement(Vector2 input)
    {
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        
        cameraForward.y = 0;
        cameraRight.y = 0;
        
        cameraForward = cameraForward.normalized;
        cameraRight = cameraRight.normalized;
        
        Vector3 moveDirection = cameraForward * input.y + cameraRight * input.x;
        
        return moveDirection.normalized;
    }


    /// <summary>
    /// 외부 메서드
    /// </summary>
    public float GetRotationAmount()
    {
        return rotationAmount;
    }
}   
