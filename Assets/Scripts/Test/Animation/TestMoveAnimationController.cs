using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 캐릭터의 이동/점프/조준/재장전 애니메이션을 제어하는 컨트롤러
/// </summary>
public class TestMoveAnimationController : MonoBehaviour
{   
    // 애니메이터 컴포넌트
    private Animator animator;
    private Rigidbody rb;

    // 입력값(WASD, 마우스 X)
    private Vector2 moveInput; 
    private Vector2 mouseInput;

    // 총기 상태 체크용
    private TestGun testGun;

    // 캐릭터 이동 정보를 가져오는 컴포넌트
    private MoveController moveController;

    // 애니메이션 속도 관련
    private float normalAnimSpeed = 1f;
    private float aimingAnimSpeed = 0.5f;
    private bool isAiming = false;

    // 점프 관련
    
    
    // 회전 관련 파라미터
    private float smoothedTurnValue = 0f;  // 회전 방향 스무딩
    private float smoothedMoveTurnValue = 0f; // 회전 방향 스무딩
    private float turnSensitivity = 0.2f;  // 민감도 조절
    private float MoveturnLerpSpeed = 5f; // Moveturn 부드러운 전환 속도
    private float turnLerpSpeed = 12f;     // turn 부드러운 전환 속도

    private void Awake()
    {
        animator = GetComponent<Animator>();
        moveController = GetComponent<MoveController>();
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        InputManager.OnMoveInput += OnMoveInput;
        InputManager.OnXMouseInput += OnMouseInput;
        InputManager.OnZoomPressed += OnZoomInput;
        InputManager.OnZoomCanceledPressed += OnZoomCanceledInput;
        InputManager.OnReloadPressed += OnReloadInput;
        // InputManager.OnJumpPressed += OnJumpInput;
    }

    private void OnDisable()
    {
        InputManager.OnMoveInput -= OnMoveInput;
        InputManager.OnXMouseInput -= OnMouseInput;
        InputManager.OnZoomPressed -= OnZoomInput;
        InputManager.OnZoomCanceledPressed -= OnZoomCanceledInput;
        InputManager.OnReloadPressed -= OnReloadInput;
        // InputManager.OnJumpPressed -= OnJumpInput;
    }

    private void Update()
    {
        HandleMovementAnimation();
        //HandleReloadAnimation();
        HandleTurnAnimation();
        HandleAnimatorSpeed();
        HandleJumpAnimation();
    }

    // 이동 입력 처리
    void OnMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    // 마우스 X 이동 입력 처리
    void OnMouseInput(Vector2 input)
    {
        mouseInput = input;
    }

    // 이동 애니메이션 처리
    void HandleMovementAnimation()
    {
        animator.SetFloat("MoveX", moveInput.x, 0.1f, Time.deltaTime);
        animator.SetFloat("MoveY", moveInput.y, 0.1f, Time.deltaTime);
    }

    // 캐릭터 회전값을 받아 애니메이션 전달
    void HandleTurnAnimation()
    {
        float currentRotationAmount = moveController.GetRotationAmount();

        float rawTurn = Mathf.Clamp(currentRotationAmount * turnSensitivity, -1f, 1f);

        if (Mathf.Abs(rawTurn) < 0.1f)
            rawTurn = 0f;

        bool isMoving = moveInput.magnitude > 0.1f;
        
        float targetTurnX = isMoving ? 0f : rawTurn;
        float targetMoveTurnX = isMoving ? rawTurn : 0f;
        
        smoothedTurnValue = Mathf.Lerp(smoothedTurnValue, targetTurnX, Time.deltaTime * turnLerpSpeed);
        if (Mathf.Abs(smoothedTurnValue) < 0.005f) smoothedTurnValue = 0f;

        smoothedMoveTurnValue = Mathf.Lerp(smoothedMoveTurnValue, targetMoveTurnX, Time.deltaTime * MoveturnLerpSpeed);
        if (Mathf.Abs(smoothedMoveTurnValue) < 0.005f) smoothedMoveTurnValue = 0f;

        animator.SetFloat("TurnX", smoothedTurnValue, 0f, Time.deltaTime);
        animator.SetFloat("MoveTurnX", smoothedMoveTurnValue, 0f, Time.deltaTime);
    }


    // 재장전시 트리거 실행
    void OnReloadInput()
    {
        animator.SetTrigger("Reload");
    }

    // // 재장전 상태 확인 후 애니메이션 파라미터 반영
    // void HandleReloadAnimation()
    // {
    //     if (testGun == null) return;

    //     bool isReloading = testGun.CurrentState == TestGun.GunState.Reloading;
    //     animator.SetBool("IsReloading", isReloading);
    // }

    // 조준 상태에 따른 애니메이션 속도 변경
    void HandleAnimatorSpeed()
    {
        animator.speed = isAiming ? aimingAnimSpeed : normalAnimSpeed;
    }

    void HandleJumpAnimation()
    {
        bool grounded = moveController.IsGrounded();

        Debug.Log($"isGrounded: {grounded}, velocityY: {rb.velocity.y}");
        // 2. 낙하 중일 때
        if (!moveController.IsGrounded() && rb.velocity.y > 0.1f)
        {
            animator.SetBool("JumpUp", true);
        }
        else
        {
            animator.SetBool("JumpUp", false);
        }

        // 2. 낙하 중일 때
        if (!moveController.IsGrounded() && rb.velocity.y < -0.1f)
        {
            animator.SetBool("JumpDown", true);
        }
        else
        {
            animator.SetBool("JumpDown", false);
        }
    }
    
    // void OnJumpInput()
    // {
    //     animator.SetTrigger("JumpUp");
    // }

    // 조준 시작 시 호출
    void OnZoomInput()
    {
        isAiming = true;
        animator.SetBool("IsAiming", true);
    }

    // 조준 해제 시 호출
    void OnZoomCanceledInput()
    {
        isAiming = false;
        animator.SetBool("IsAiming", false);
    }

}
