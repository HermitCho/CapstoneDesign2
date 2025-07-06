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

    // 점프 및 낙하 상태
    private bool isJumping = false;  
    private bool isFalling = false;  
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        moveController = GetComponent<MoveController>();
    }

    private void OnEnable()
    {
        InputManager.OnMoveInput += OnMoveInput;
        InputManager.OnXMouseInput += OnMouseInput;
        InputManager.OnZoomPressed += OnZoomInput;
        InputManager.OnZoomCanceledPressed += OnZoomCanceledInput;
        InputManager.OnReloadPressed += OnReloadInput;
    }

    private void OnDisable()
    {
        InputManager.OnMoveInput -= OnMoveInput;
        InputManager.OnXMouseInput -= OnMouseInput;
        InputManager.OnZoomPressed -= OnZoomInput;
        InputManager.OnZoomCanceledPressed -= OnZoomCanceledInput;
        InputManager.OnReloadPressed -= OnReloadInput;
    }

    private void Update()
    {
        HandleMovementAnimation();
        HandleReloadAnimation();
        HandleJumpAnimation();
        HandleTurnAnimation();
        HandleAnimatorSpeed();
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
        
        float rotationAmount = moveController.GetRotationAmount();

        animator.SetFloat("TurnX", rotationAmount, 0.1f, Time.deltaTime);
    }

    // 점프 및 낙하 애니메이션 처리
    void HandleJumpAnimation()
    {
        bool isGrounded = moveController.IsGrounded();

        if (!isGrounded)
        {
            animator.SetBool("IsFalling", true);
        }
        else
        {
            animator.SetBool("IsFalling", false);
        }

        if (moveController.IsJumping())
        {
            animator.SetBool("IsJumping", true);
        }
        else
        {
            animator.SetBool("IsJumping", false);
        }
    }

    // 재장전시 트리거 실행
    void OnReloadInput()
    {
        animator.SetTrigger("Reload");
    }

    // 재장전 상태 확인 후 애니메이션 파라미터 반영
    void HandleReloadAnimation()
    {
        if (testGun == null) return;

        bool isReloading = testGun.CurrentState == TestGun.GunState.Reloading;
        animator.SetBool("IsReloading", isReloading);
    }

    // 조준 상태에 따른 애니메이션 속도 변경
    void HandleAnimatorSpeed()
    {
        animator.speed = isAiming ? aimingAnimSpeed : normalAnimSpeed;
    }

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
