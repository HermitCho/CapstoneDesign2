using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Linq;
using Photon.Pun;


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


public class MoveController : MonoBehaviourPun, IPunObservable
{
    private DataBase.PlayerMoveData playerMoveData;
    private DataBase.PlayerData playerData;
    private Rigidbody playerRigidbody;
    private Vector2 rawMoveInput; // 원본 입력값 저장
    private PhotonView photonView;
    private Transform tr;

    private double lastReceiveTime;
    //private double currentReceiveTime;
    Vector3 targetPos;
    Quaternion targetRot;

    // 벽 통과 방지를 위한 변수 추가
    private LayerMask wallLayerMask = -1; // 벽으로 인식할 레이어
    private Vector3 lastValidPosition; // 마지막 유효한 위치 저장

    // 🎯 Wall Penetration Ray 개선을 위한 변수들
    private float rayHeightOffset = 1.0f; // 캐릭터 허리 높이 (발 기준 +1m)
    private float capsuleRadius = 0.5f; // 캐릭터 반지름
    private LayerMask groundLayerMask = 1; // 땅바닥 레이어 (기본: Default)

    // ✅ DataBase 캐싱된 값들 (성능 최적화)
    private float cachedSpeed;
    private float cachedRotationSpeed;
    private float cachedZoomRotationSpeed;
    private float cachedMouseInputTimeout;
    private float cachedJumpCooldown;
    private float cachedGroundCheckDistance;
    private float cachedJumpHeight;
    private float cachedJumpBufferTime;
    private float cachedAirAcceleration;
    private float cachedAirMaxSpeed;
    private float cachedLandingFriction;
    private float cachedMaxMoveDistance;
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

    // ✅ 기절/조작 제어 관련 변수들
    private float slowMultiplier = 1f;
    private bool isSlowed = false;
    private bool isStunned = false;
    private bool canMove = true;
    private bool canRotate = true;
    private bool canJump = true;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        tr = GetComponent<Transform>();
    }
    // InputManager 이벤트 구독
    void OnEnable()
    {
        if (!photonView.IsMine) return;
        // InputManager 이벤트 구독
        InputManager.OnMoveInput += OnMoveInput;
        InputManager.OnXMouseInput += OnMouseInput;
        InputManager.OnJumpPressed += OnJumpInput;

        MouseLock();
    }

    void OnDisable()
    {
        if (!photonView.IsMine) return;
        // InputManager 이벤트 구독 해제
        InputManager.OnMoveInput -= OnMoveInput;
        InputManager.OnXMouseInput -= OnMouseInput;
        InputManager.OnJumpPressed -= OnJumpInput;
    }



    void Start()
    {
        if (photonView.IsMine)
        {               // 메인 카메라 찾기
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
            playerRigidbody = GetComponent<Rigidbody>();

            // 마지막 유효한 위치 초기화
            lastValidPosition = tr.position;
        }
        // DataBase 정보 안전하게 캐싱 (Start에서 지연 실행)
        CacheDataBaseInfo();
    }

    void CacheDataBaseInfo()
    {
        try
        {
            // DataBase 인스턴스가 없으면 잠시 대기 후 재시도
            if (DataBase.Instance == null)
            {
                StartCoroutine(RetryCacheDataBaseInfo());
                return;
            }

            if (DataBase.Instance.playerMoveData != null && DataBase.Instance.playerData != null && DataBase.Instance.itemData != null)
            {
                playerMoveData = DataBase.Instance.playerMoveData;
                playerData = DataBase.Instance.playerData;
                // 자주 사용되는 값들을 개별 변수로 캐싱
                cachedSpeed = playerMoveData.Speed;
                cachedRotationSpeed = playerMoveData.RotationSpeed;
                cachedZoomRotationSpeed = playerMoveData.ZoomRotationSpeed;
                cachedMouseInputTimeout = playerMoveData.MouseInputTimeout;
                cachedJumpCooldown = playerMoveData.JumpCooldown;
                cachedGroundCheckDistance = playerMoveData.GroundCheckDistance;
                cachedJumpHeight = playerMoveData.JumpHeight;
                cachedJumpBufferTime = playerMoveData.JumpBufferTime;
                cachedAirAcceleration = playerMoveData.AirAcceleration;
                cachedAirMaxSpeed = playerMoveData.AirMaxSpeed;
                cachedLandingFriction = playerMoveData.LandingFriction;
                cachedMaxMoveDistance = playerMoveData.MaxMoveDistance;
                dataBaseCached = true;
            }
            else
            {
                dataBaseCached = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MoveController - DataBase 캐싱 중 오류: {e.Message}");
            dataBaseCached = false;
        }
    }

    /// <summary>
    /// DataBase 캐싱 재시도 코루틴
    /// </summary>
    IEnumerator RetryCacheDataBaseInfo()
    {
        int maxRetries = 10;
        int currentRetry = 0;

        while (currentRetry < maxRetries)
        {
            yield return new WaitForSeconds(0.1f); // 0.1초 대기

            if (DataBase.Instance != null)
            {
                CacheDataBaseInfo(); // 재귀 호출로 다시 시도
                yield break;
            }

            currentRetry++;
        }

        Debug.LogError("MoveController - DataBase 캐싱 최대 재시도 횟수 초과");
        dataBaseCached = false;
    }


    void Update()
    {
        //currentReceiveTime = Mathf.Clamp01((float)((PhotonNetwork.Time - lastReceiveTime) * PhotonNetwork.SerializationRate));
        //if (!photonView.IsMine) return;
        UpdateGroundedState();
        HandleMovement();
        HandleRotation();
        UpdateJumpBuffer();
        HandleLanding();

        // 벽 통과 방지 체크 (HandleMovement 이후에 실행)
        CheckWallPenetration();
    }


    // 지면 상태 업데이트
    void UpdateGroundedState()
    {
        if (!photonView.IsMine) return;
        wasGrounded = isGrounded;
        isGrounded = CheckGrounded();
    }

    /// <summary>
    /// 모든 클라이언트에게 속도 감소 효과를 적용하는 RPC.
    /// </summary>
    [PunRPC]
    public void ApplySlowEffect(float slowAmount)
    {
        // 이미 슬로우 상태가 아니라면
        if (!isSlowed)
        {
            // 속도 배수를 적용하고 상태 변경
            slowMultiplier = 1.0f - slowAmount;
            isSlowed = true;
            UpdateSpeed();
        }
    }

    /// <summary>
    /// 모든 클라이언트에게 속도 감소 효과를 제거하는 RPC.
    /// </summary>
    [PunRPC]
    public void RemoveSlowEffect()
    {
        // 슬로우 상태라면
        if (isSlowed)
        {
            // 속도 배수를 원래대로 되돌리고 상태 변경
            slowMultiplier = 1.0f;
            isSlowed = false;
            UpdateSpeed();
        }
    }

    [PunRPC]
    public void ApplySpeedBuff(float multiplier)
    {
        cachedSpeed *= (1f + multiplier);
    }

    [PunRPC]
    public void RemoveSpeedBuff(float multiplier)
    {
        cachedSpeed /= (1f + multiplier);
    }

    // 실제 이동 속도를 업데이트하는 함수
    private void UpdateSpeed()
    {
        cachedSpeed = playerMoveData.Speed * slowMultiplier;
    }

    //움직임 처리
    void HandleMovement()
    {
        //if (!photonView.IsMine) return;
        //if (rawMoveInput.magnitude < 0.1f) return;

        if (photonView.IsMine)
        {
            if (rawMoveInput.magnitude < 0.1f) return;
            Vector3 playerRelativeMovement = GetPlayerRelativeMovement(rawMoveInput);

            if (isGrounded)
            {
                // 지상 이동 (즉시 반응) (캐싱된 값 사용)
                Vector3 movement = playerRelativeMovement * cachedSpeed * Time.deltaTime;
                tr.Translate(movement, Space.World);
            }
            else
            {
                // 공중 이동- 에어 스트레이핑 적용
                HandleAirMovement(playerRelativeMovement);
            }

        }
        else
        {
            tr.position = Vector3.Lerp(tr.position, targetPos, Time.deltaTime * 10f);

        }
    }

    // 공중 이동
    void HandleAirMovement(Vector3 wishDirection)
    {
        if (!photonView.IsMine) return;
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
        if (!photonView.IsMine) return;
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
        if (!photonView.IsMine) return;
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
        if (!photonView.IsMine) return;
        // ✅ 움직임 제어 확인
        if (!canMove || isStunned)
        {
            rawMoveInput = Vector2.zero;
            return;
        }

        // 🔒 네트워크 권한 체크 - 자신의 플레이어만 조작 가능
        if (!PhotonView.Get(this).IsMine) return;

        rawMoveInput = moveInput;
    }

    // InputManager에서 마우스 입력 받기
    void OnMouseInput(Vector2 mouseInput)
    {
        if (!photonView.IsMine) return;
        // ✅ 마우스 조작 제어 확인
        if (!canRotate || isStunned)
        {
            rotationAmount = 0;
            return;
        }

        // 🔒 네트워크 권한 체크 - 자신의 플레이어만 조작 가능
        if (!PhotonView.Get(this).IsMine) return;

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
        if (photonView.IsMine)
        {
            if (!canRotate || isStunned)
            {
                rotationAmount = 0;
                return;
            }

            if (Time.time - lastMouseInputTime > cachedMouseInputTimeout) // 캐싱된 값 사용
            {
                rotationAmount = 0;
            }
            tr.Rotate(Vector3.up, rotationAmount);
        }
        else
        {
            tr.rotation = Quaternion.Slerp(tr.rotation, targetRot, Time.deltaTime * 10f);
        }


        // if (!photonView.IsMine) return;
        // // ✅ 회전 제어 확인
        // if (!canRotate || isStunned)
        // {
        //     rotationAmount = 0;
        //     return;
        // }

        // if (Time.time - lastMouseInputTime > cachedMouseInputTimeout) // 캐싱된 값 사용
        // {
        //     rotationAmount = 0;
        // }
        // transform.Rotate(Vector3.up, rotationAmount);
    }

    // InputManager에서 점프 입력 받기
    void OnJumpInput()
    {
        if (!photonView.IsMine) return;
        // ✅ 점프 제어 확인
        if (!canJump || isStunned)
        {
            return;
        }

        // 🔒 네트워크 권한 체크 - 자신의 플레이어만 조작 가능
        if (!PhotonView.Get(this).IsMine) return;

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
        if (!photonView.IsMine) return;
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

        return Physics.Raycast(tr.position, Vector3.down, out hit, cachedGroundCheckDistance);
    }

    /// <summary>
    /// 벽 통과 방지 체크 메서드 (개선된 버전)
    /// </summary>
    private void CheckWallPenetration()
    {
        if (!photonView.IsMine) return;
        if (GameManager.Instance.IsGameOver()) return;
        Vector3 currentPosition = tr.position;
        Vector3 moveVector = currentPosition - lastValidPosition;
        float moveDistance = moveVector.magnitude;

        // 이동 거리가 임계값을 초과하거나 빠른 이동이 감지되면 체크
        if (moveDistance > cachedMaxMoveDistance * Time.deltaTime || moveDistance > 0.1f)
        {
            // 🎯 수평 방향으로만 이동 벡터 계산 (Y축 제거)
            Vector3 horizontalMoveVector = new Vector3(moveVector.x, 0f, moveVector.z);
            float horizontalDistance = horizontalMoveVector.magnitude;

            // 수평 이동이 거의 없으면 체크하지 않음
            if (horizontalDistance < 0.01f)
            {
                lastValidPosition = currentPosition;
                return;
            }

            Vector3 horizontalDirection = horizontalMoveVector.normalized;

            // 🎯 여러 높이에서 수평 Ray 체크 (발목, 허리, 가슴)
            float[] checkHeights = { 0.2f, rayHeightOffset, rayHeightOffset * 1.5f };
            bool hitDetected = false;
            RaycastHit closestHit = new RaycastHit();
            float closestDistance = float.MaxValue;

            foreach (float height in checkHeights)
            {
                Vector3 rayStart = lastValidPosition + Vector3.up * height;
                RaycastHit hit;

                // 땅바닥과 벽을 모두 감지하도록 LayerMask 조정
                LayerMask effectiveWallAndGroundMask = wallLayerMask | groundLayerMask;

                if (Physics.Raycast(rayStart, horizontalDirection, out hit, horizontalDistance + capsuleRadius, effectiveWallAndGroundMask))
                {
                    // 플레이어 자신이거나 Trigger 콜라이더는 무시
                    if (!hit.collider.CompareTag("Player") && !hit.collider.isTrigger)
                    {
                        // 가장 가까운 충돌점 찾기
                        float hitDistance = Vector3.Distance(rayStart, hit.point);
                        if (hitDistance < closestDistance)
                        {
                            closestDistance = hitDistance;
                            closestHit = hit;
                            hitDetected = true;
                        }
                    }
                }
            }

            // 충돌이 감지되면 안전한 위치로 이동
            if (hitDetected)
            {
                // 🎯 안전한 위치 계산 (캐릭터 반지름 + 여유 공간 확보)
                Vector3 safePosition = closestHit.point - horizontalDirection * (capsuleRadius + 0.1f);
                safePosition.y = currentPosition.y; // Y 좌표는 현재 위치 유지

                tr.position = safePosition;

                // Rigidbody 속도 조정 (수평 방향만)
                if (playerRigidbody != null)
                {
                    Vector3 currentVelocity = playerRigidbody.velocity;
                    // 벽 법선의 수평 성분만 사용
                    Vector3 horizontalNormal = new Vector3(closestHit.normal.x, 0f, closestHit.normal.z).normalized;
                    Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
                    Vector3 velocityAlongNormal = Vector3.Project(horizontalVelocity, horizontalNormal);

                    // 수정된 수평 속도 적용 (Y 속도는 유지)
                    Vector3 correctedHorizontalVelocity = horizontalVelocity - velocityAlongNormal;
                    playerRigidbody.velocity = new Vector3(correctedHorizontalVelocity.x, currentVelocity.y, correctedHorizontalVelocity.z);
                }

                lastValidPosition = safePosition;
                return;
            }
        }

        // 유효한 이동이면 마지막 위치 업데이트
        lastValidPosition = currentPosition;
    }

    public void MouseLock()
    {
        if (!photonView.IsMine) return;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 벽 충돌 시 자연스럽게 떨어지도록 처리
    void OnCollisionEnter(Collision collision)
    {
        if (!photonView.IsMine) return;
        //if (GameManager.Instance.IsGameOver()) return;
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
        if (!photonView.IsMine) return;
        //if (GameManager.Instance.IsGameOver()) return;
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


    // 플레이어 기준 이동 방향 계산
    Vector3 GetPlayerRelativeMovement(Vector2 input)
    {
        // 플레이어 기준 방향 벡터 계산
        Vector3 playerForward = tr.forward;
        Vector3 playerRight = tr.right;

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

    public bool IsGrounded()
    {
        return isGrounded;
    }

    // ========================================
    // === 기절/조작 제어 관련 메서드들 ===
    // ========================================

    /// <summary>
    /// 기절 상태 설정 (모든 조작 차단)
    /// </summary>
    /// <param name="stunned">기절 상태 여부</param>
    public void SetStunned(bool stunned)
    {
        isStunned = stunned;

        if (stunned)
        {
            // 기절 시 모든 입력 차단 및 움직임 정지
            rawMoveInput = Vector2.zero;
            rotationAmount = 0;
            jumpBufferTimer = 0;
        }
    }

    /// <summary>
    /// 현재 기절 상태 확인
    /// </summary>
    /// <returns>기절 상태 여부</returns>
    public bool IsStunned()
    {
        return isStunned;
    }

    // --- 움직임 제어 메서드들 ---

    /// <summary>
    /// 움직임 조작 차단
    /// </summary>
    public void DisableMovement()
    {
        canMove = false;
        rawMoveInput = Vector2.zero;
    }

    /// <summary>
    /// 움직임 조작 허용
    /// </summary>
    public void EnableMovement()
    {
        canMove = true;
    }

    /// <summary>
    /// 움직임 조작 가능 여부 확인
    /// </summary>
    /// <returns>움직임 조작 가능 여부</returns>
    public bool CanMove()
    {
        return canMove && !isStunned;
    }

    // --- 마우스 조작 제어 메서드들 ---

    /// <summary>
    /// 마우스 조작 차단 (회전 차단)
    /// </summary>
    public void DisableMouseControl()
    {
        canRotate = false;
        rotationAmount = 0;
    }

    /// <summary>
    /// 마우스 조작 허용 (회전 허용)
    /// </summary>
    public void EnableMouseControl()
    {
        canRotate = true;
    }

    /// <summary>
    /// 마우스 조작 가능 여부 확인
    /// </summary>
    /// <returns>마우스 조작 가능 여부</returns>
    public bool CanRotate()
    {
        return canRotate && !isStunned;
    }

    // --- 점프 제어 메서드들 ---

    /// <summary>
    /// 점프 조작 차단
    /// </summary>
    public void DisableJump()
    {
        canJump = false;
        jumpBufferTimer = 0;
    }

    /// <summary>
    /// 점프 조작 허용
    /// </summary>
    public void EnableJump()
    {
        canJump = true;
    }

    /// <summary>
    /// 점프 조작 가능 여부 확인
    /// </summary>
    /// <returns>점프 조작 가능 여부</returns>
    public bool CanJump()
    {
        return canJump && !isStunned;
    }

    // --- 통합 제어 메서드들 ---

    /// <summary>
    /// 모든 조작 차단 (기절 제외)
    /// </summary>
    public void DisableMoveControls()
    {
        DisableMovement();
        DisableMouseControl();
        DisableJump();
    }

    /// <summary>
    /// 모든 조작 허용
    /// </summary>
    public void EnableMoveControls()
    {
        EnableMovement();
        EnableMouseControl();
        EnableJump();
    }

    /// <summary>
    /// 현재 조작 상태 로그 출력
    /// </summary>
    public void LogControlStatus()
    {
        Debug.Log($"=== 조작 상태 ===");
        Debug.Log($"기절 상태: {isStunned}");
        Debug.Log($"움직임: {canMove && !isStunned}");
        Debug.Log($"마우스 조작: {canRotate && !isStunned}");
        Debug.Log($"점프: {canJump && !isStunned}");
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(tr.position);
            stream.SendNext(tr.rotation);
        }
        else
        {
            targetPos = (Vector3)stream.ReceiveNext();
            targetRot = (Quaternion)stream.ReceiveNext();
            lastReceiveTime = info.SentServerTime;
        }
    }


}