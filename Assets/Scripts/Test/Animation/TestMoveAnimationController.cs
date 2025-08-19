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

    // 테디베어 총기 부착 관련
    [SerializeField] private TestTeddyBear teddyBear;
    [SerializeField] private GameObject gunObject;
    private bool previousAttachState = false;

    // 발소리 관련
    private FootstepSoundPlayer footstepSoundPlayer;


    private GunIK gunIK;
    private AimIK aimIK;
    private LivingEntity livingEntity;

    // 대쉬 스킬
    private Skill dashSkill;
    // 아이템 스킬
    private Skill itemSkill;

    private Coroutine speedSkillCoroutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        moveController = GetComponent<MoveController>();
        cameraController = GetComponent<CameraController>();
        rb = GetComponent<Rigidbody>();
        upperBodyLayerIndex = animator.GetLayerIndex("UpperBody");
        gunIK = GetComponent<GunIK>();
        livingEntity = GetComponent<LivingEntity>();
        footstepSoundPlayer = GetComponent<FootstepSoundPlayer>();
        dashSkill = GetComponent<Skill>();
        itemSkill = GetComponent<Skill>();
        aimIK = GetComponent<AimIK>();
        animator.SetFloat("SpeedMultiplier", 1.2f);
    }

    private void OnEnable()
    {
        InputManager.OnMoveInput += OnMoveInput;
        InputManager.OnXMouseInput += OnMouseInput;
        InputManager.OnZoomPressed += OnZoomInput;
        InputManager.OnZoomCanceledPressed += OnZoomCanceledInput;
        InputManager.OnReloadPressed += OnReloadInput;
        InputManager.OnSkillPressed += OnSkillInput;
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
        InputManager.OnSkillPressed -= OnSkillInput;
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
        // HandleTurnAnimation();
        // HandleAnimatorSpeed();
        HandleJumpAnimation();
        // HandleLookAnimation();
        HandleTeddyBearWeaponState();

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
        bool isMoving = moveInput.magnitude > 0.1f;

        animator.SetFloat("MoveX", moveInput.x, 0.1f, Time.deltaTime);
        animator.SetFloat("MoveY", moveInput.y, 0.1f, Time.deltaTime);

        footstepSoundPlayer.SetIsMoving(isMoving);
    }

    private void OnStunned()
    {
        animator.SetTrigger("Death");
    }

    private void OnRevive()
    {
        animator.SetTrigger("Revive");
    }

    // // 캐릭터 회전값을 받아 애니메이션 전달
    // void HandleTurnAnimation()
    // {   
    //     if (moveController == null) return;

    //     float currentRotationAmount = moveController.GetRotationAmount();
    //     float rawTurn = Mathf.Clamp(currentRotationAmount * turnSensitivity, -1f, 1f);

    //     if (Mathf.Abs(rawTurn) < 0.1f)
    //         rawTurn = 0f;

    //     bool isMoving = moveInput.magnitude > 0.1f;
        
    //     float targetTurnX = isMoving ? 0f : rawTurn;
    //     float targetMoveTurnX = isMoving ? rawTurn : 0f;
        
    //     smoothedTurnValue = Mathf.Lerp(smoothedTurnValue, targetTurnX, Time.deltaTime * turnLerpSpeed);
    //     if (Mathf.Abs(smoothedTurnValue) < 0.005f) smoothedTurnValue = 0f;

    //     smoothedMoveTurnValue = Mathf.Lerp(smoothedMoveTurnValue, targetMoveTurnX, Time.deltaTime * MoveturnLerpSpeed);
    //     if (Mathf.Abs(smoothedMoveTurnValue) < 0.005f) smoothedMoveTurnValue = 0f;

    //     animator.SetFloat("TurnX", smoothedTurnValue, 0f, Time.deltaTime);
    //     animator.SetFloat("MoveTurnX", smoothedMoveTurnValue, 0f, Time.deltaTime);
    // }

    // 재장전시 트리거 실행
    void OnReloadInput()
    {
        animator.SetLayerWeight(upperBodyLayerIndex, 1f);
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.LeftHand, gunIK.leftHandTarget, 0f, 0f);
        aimIK.enabled = false;
        animator.SetTrigger("Reload");

    }

    // 재장전 시작
    void OnReloadStart()
    {
        
    }

    // 재장전 종료
    void OnReloadEnd()
    {
        Debug.Log("Reload 애니메이션 종료됨");
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.LeftHand, gunIK.leftHandTarget, 1f, 1f);
        aimIK.enabled = true;
        animator.SetLayerWeight(upperBodyLayerIndex, 0f);

    }

    // // 조준 상태에 따른 애니메이션 속도 변경
    // void HandleAnimatorSpeed()
    // {
    //     animator.speed = isAiming ? aimingAnimSpeed : normalAnimSpeed;
    // }

    void HandleJumpAnimation()
    {
        bool grounded = moveController.IsGrounded();

        if (!moveController.IsGrounded())
        {
            if (rb.velocity.y > 0.05f)
            {
                animator.SetBool("JumpUp", true);
                animator.SetBool("JumpDown", false);
            }
            else if (rb.velocity.y < -0.05f)
            {
                animator.SetBool("JumpUp", false);
                animator.SetBool("JumpDown", true);
            }
        }
        else
        {
            animator.SetBool("JumpUp", false);
            animator.SetBool("JumpDown", false);
        }
    }
    
    // 조준 시작 시 호출
    void OnZoomInput()
    {
        isAiming = true;
        animator.SetLayerWeight(upperBodyLayerIndex, 1f);
        animator.SetBool("IsAiming", true);
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.Body, gunIK.bodyTarget, 0.04f);
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.RightFoot, gunIK.rightLegTarget, 0.3f);
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.LeftFoot, gunIK.leftLegTarget, 0.3f);

        animator.SetFloat("SpeedMultiplier", 0.6f); // 조준 시 이동 느리게
    }

    // 조준 해제 시 호출
    void OnZoomCanceledInput()
    {
        isAiming = false;
        animator.SetLayerWeight(upperBodyLayerIndex, 0f);
        animator.SetBool("IsAiming", false);
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.Body, gunIK.bodyTarget, 0.01f);
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.RightFoot, gunIK.rightLegTarget, 0.2f);
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.LeftFoot, gunIK.leftLegTarget, 0.2f);

        animator.SetFloat("SpeedMultiplier", 1.2f); // 조준 해제 시 원래 속도
    }

    // 스피드 스킬
    void OnSkillInput()
    {
        
        if (dashSkill != null && dashSkill.CanUse)
        {   
            //dashSkill.Activate(moveController);
            animator.SetTrigger("Speed");
            animator.SetLayerWeight(upperBodyLayerIndex, 0f);
            gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.LeftHand, gunIK.leftHandTarget, 0f, 0f);

            if (speedSkillCoroutine != null)
                StopCoroutine(speedSkillCoroutine);
            speedSkillCoroutine = StartCoroutine(SpeedSkillRoutine());
        }
    }

    private IEnumerator SpeedSkillRoutine()
    {
        animator.SetFloat("SpeedMultiplier", 1.5f); // 이동만 1.5배
        yield return new WaitForSeconds(3f);
        animator.SetFloat("SpeedMultiplier", 1.2f); // 원래대로
    }

    // 아이템 스킬
    void OnItemInput()
    {
        if (itemSkill != null && itemSkill.CanUse)
        {
            //itemSkill.Activate(moveController);
            animator.SetTrigger("Item");
            animator.SetTrigger("PowerUP");
            animator.SetTrigger("Debuff 1");
            animator.SetTrigger("Throw");
            animator.SetLayerWeight(upperBodyLayerIndex, 0f);
            gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.LeftHand, gunIK.leftHandTarget, 0f, 0f);
        }
    }

    // 테디베어 총기 부착
    private void HandleTeddyBearWeaponState()
    {
        if (teddyBear == null || gunObject == null) return;

        bool isAttached = teddyBear.IsAttached();

        if (previousAttachState != isAttached)
        {
            gunObject.SetActive(!isAttached); // 곰인형 들고 있으면 false
            previousAttachState = isAttached;

            Debug.Log($"총기 {(isAttached ? "숨김" : "표시")} 상태로 변경됨");
        }
    }

    public void OnSkillEnd()
    {
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.LeftHand, gunIK.leftHandTarget, 1f, 1f);
    }
}