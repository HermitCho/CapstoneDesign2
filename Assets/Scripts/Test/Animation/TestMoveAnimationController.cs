using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using RootMotion.FinalIK;

/// <summary>
/// 캐릭터의 이동/점프/조준/재장전 애니메이션을 제어하는 컨트롤러
/// </summary>
public class TestMoveAnimationController : MonoBehaviour
{   
    // 애니메이터 컴포넌트
    private Animator animator;
    private Rigidbody rb;
    private int upperBodyLayerIndex;

    // 입력값(WASD, 마우스 X)
    private Vector2 moveInput; 
    private Vector2 mouseInput;

    // 총기 상태 체크용
    private TestGun testGun;

    // 캐릭터 이동 정보를 가져오는 컴포넌트
    private MoveController moveController;
    private CameraController cameraController;

    // 애니메이션 속도 관련
    private float normalAnimSpeed = 1f;
    private float aimingAnimSpeed = 0.5f;
    private bool isAiming = false;

    // 점프 관련
    
    
    // 좌우회전 관련
    private float smoothedTurnValue = 0f;  // 회전 방향 스무딩
    private float smoothedMoveTurnValue = 0f; // 회전 방향 스무딩
    private float turnSensitivity = 0.2f;  // 민감도 조절
    private float MoveturnLerpSpeed = 5f; // Moveturn 부드러운 전환 속도
    private float turnLerpSpeed = 10f;     // turn 부드러운 전환 속도

    // // 상하회전 관련
    // [SerializeField] MultiAimConstraint headLookConstraint;
    // [SerializeField] Transform aimTarget;
    // [SerializeField] private float maxLookUpAngle = 40f;
    // [SerializeField] private float maxLookDownAngle = -20f;

    // // Rig설정
    // [SerializeField] private Rig armAimRig; // Rig3

    private GunIK gunIK;
    private LivingEntity livingEntity;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        moveController = GetComponent<MoveController>();
        cameraController = GetComponent<CameraController>();
        rb = GetComponent<Rigidbody>();
        upperBodyLayerIndex = animator.GetLayerIndex("UpperBody");
        gunIK = GetComponent<GunIK>();
        livingEntity = GetComponent<LivingEntity>();
    }

    private void OnEnable()
    {
        InputManager.OnMoveInput += OnMoveInput;
        InputManager.OnXMouseInput += OnMouseInput;
        InputManager.OnZoomPressed += OnZoomInput;
        InputManager.OnZoomCanceledPressed += OnZoomCanceledInput;
        InputManager.OnReloadPressed += OnReloadInput;
        InputManager.OnSkillPressed += OnDashInput;
        InputManager.OnItemPressed += OnItemInput;
        if (livingEntity != null)
        {
            livingEntity.OnDeath += OnStunned;
            livingEntity.OnRevive += OnRevive;
        }
    }

    private void OnDisable()
    {
        InputManager.OnMoveInput -= OnMoveInput;
        InputManager.OnXMouseInput -= OnMouseInput;
        InputManager.OnZoomPressed -= OnZoomInput;
        InputManager.OnZoomCanceledPressed -= OnZoomCanceledInput;
        InputManager.OnReloadPressed -= OnReloadInput;
        InputManager.OnSkillPressed -= OnDashInput;
        InputManager.OnItemPressed -= OnItemInput;
        if (livingEntity != null)
        {
            livingEntity.OnDeath -= OnStunned;
            livingEntity.OnRevive -= OnRevive;
        }
    }
    private void Update()
    {
        HandleMovementAnimation();
        HandleTurnAnimation();
        HandleAnimatorSpeed();
        HandleJumpAnimation();
        // HandleLookAnimation();

    }

    // private void LateUpdate()
    // {
    //     if (aimTarget != null && cameraController != null)
    //     {
    //         float pitch = cameraController.GetTargetVerticalAngle();

    //         // 각도 제한
    //         pitch = Mathf.Clamp(pitch, maxLookDownAngle, maxLookUpAngle);

    //         // 현재 로컬 회전의 나머지 축은 유지
    //         Vector3 currentEuler = aimTarget.localEulerAngles;
    //         aimTarget.localEulerAngles = new Vector3(pitch, currentEuler.y, currentEuler.z);
    //     }
    // }

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

    private void OnStunned()
    {
        animator.SetTrigger("Death");
    }

    private void OnRevive()
    {
        animator.SetTrigger("Revive");
    }

    // 캐릭터 회전값을 받아 애니메이션 전달
    void HandleTurnAnimation()
    {   
        if (moveController == null) return;

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

    // // 카메라 회전값을 받아 애니메이션 전달
    // void HandleLookAnimation()
    // {
    //     if (cameraController == null) return;

    //     float verticalAngle = cameraController.GetTargetVerticalAngle(); // -60 ~ 60 기준 가정
    //     float normalizedLookY = Mathf.InverseLerp(-60f, 60f, verticalAngle) * 2f - 1f; // -1 ~ 1로 정규화

    //     animator.SetFloat("LookY", normalizedLookY);
    // }

    // 재장전시 트리거 실행
    void OnReloadInput()
    {
        animator.SetLayerWeight(upperBodyLayerIndex, 1f);
        gunIK.SetLeftHandIK(false);
        animator.SetTrigger("Reload");

    }

    // 재장전 시작
    void OnReloadStart()
    {
        
    }

    // 재장전 종료
    void OnReloadEnd()
    {
        gunIK.SetLeftHandIK(true);
        animator.SetLayerWeight(upperBodyLayerIndex, 0f);

    }

    // 조준 상태에 따른 애니메이션 속도 변경
    void HandleAnimatorSpeed()
    {
        animator.speed = isAiming ? aimingAnimSpeed : normalAnimSpeed;
    }

    void HandleJumpAnimation()
    {
        bool grounded = moveController.IsGrounded();

        Debug.Log($"isGrounded: {grounded}, velocityY: {rb.velocity.y}");

        // 점프 모션 
        if (!moveController.IsGrounded() && rb.velocity.y > 0.1f)
        {
            animator.SetBool("JumpUp", true);
        }
        else
        {
            animator.SetBool("JumpUp", false);
        }

        // 낙하 모션
        if (!moveController.IsGrounded() && rb.velocity.y < -0.1f)
        {
            animator.SetBool("JumpDown", true);
        }
        else
        {
            animator.SetBool("JumpDown", false);
        }
    }
    
    // 조준 시작 시 호출
    void OnZoomInput()
    {
        isAiming = true;
        animator.SetLayerWeight(upperBodyLayerIndex, 1f);
        animator.SetBool("IsAiming", true);
        // armAimRig.weight = 1f;
    }

    // 조준 해제 시 호출
    void OnZoomCanceledInput()
    {
        isAiming = false;
        animator.SetLayerWeight(upperBodyLayerIndex, 0f);
        animator.SetBool("IsAiming", false);
        // armAimRig.weight = 0f;
    }

    // 대쉬 스킬
    void OnDashInput()
    {
        animator.SetTrigger("Dash");
        animator.SetLayerWeight(upperBodyLayerIndex, 0f);
    }

    // 아이템 스킬
    void OnItemInput()
    {
        animator.SetTrigger("Item");
        animator.SetTrigger("PowerUP");
        animator.SetTrigger("Debuff 1");
        animator.SetTrigger("Throw");
        animator.SetLayerWeight(upperBodyLayerIndex, 0f);
    }
}
