using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using RootMotion.FinalIK;
using Photon.Pun;

/// <summary>
/// 캐릭터의 이동/점프/조준/재장전 애니메이션을 제어하는 컨트롤러
/// </summary>
public class TestMoveAnimationController : MonoBehaviourPun
{   
    // 애니메이터 컴포넌트
    private Animator animator;
    private Rigidbody rb;
    private int upperBodyLayerIndex;
    private PhotonView photonView;

    // 입력값(WASD, 마우스 X)
    private Vector2 moveInput; 
    private Vector2 mouseInput;
    
    // 재장전 관련
    private bool isReloading = false;
    
    // 점프 상태 추적
    private bool isJumping = false;
    
    // 총을 쏘는 상태 추적
    private bool isShooting = false;

    // 캐릭터 이동 정보를 가져오는 컴포넌트
    private MoveController moveController;
    private CameraController cameraController;
    private ItemController itemController;

    // 테디베어 총기 부착 관련
    [SerializeField] private Crown teddyBear;
    [SerializeField] private GameObject gunObject;
    private bool previousAttachState = false;

    // 발소리 관련
    private FootstepSoundPlayer footstepSoundPlayer;

    // 체력 관련
    private GunIK gunIK;
    private AimIK aimIK;
    private LivingEntity livingEntity;

    // 대쉬 스킬
    private Skill skill;
    // 아이템 스킬
    private Skill itemSkill;
    private Coroutine speedSkillCoroutine;
    private string skillAnimationTriggerName = "None";
    private string itemSkillAnimationTriggerName = "None";

    

    private void Awake()
    {
        animator = GetComponent<Animator>();
        moveController = GetComponent<MoveController>();
        cameraController = GetComponent<CameraController>();
        itemController = GetComponent<ItemController>();
        rb = GetComponent<Rigidbody>();
        upperBodyLayerIndex = animator.GetLayerIndex("UpperBody");
        gunIK = GetComponent<GunIK>();
        livingEntity = GetComponent<LivingEntity>();
        footstepSoundPlayer = GetComponent<FootstepSoundPlayer>();
        skill = GetComponent<Skill>();
        aimIK = GetComponent<AimIK>();
        photonView = GetComponent<PhotonView>();
        animator.SetFloat("SpeedMultiplier", 1.2f);
        skillAnimationTriggerName = skill.SkillAnimationTriggerName;
    }

    private void OnEnable()
    {
        if (!photonView.IsMine) return;
        InputManager.OnMoveInput += OnMoveInput;
        InputManager.OnXMouseInput += OnMouseInput;
        InputManager.OnZoomPressed += OnZoomInput;
        InputManager.OnZoomCanceledPressed += OnZoomCanceledInput;
        InputManager.OnReloadPressed += OnReloadInput;
        InputManager.OnSkillPressed += OnSkillInput;
        InputManager.OnItemPressed += OnItemInput;
        InputManager.OnShootPressed += OnShootInput;
        InputManager.OnShootCanceledPressed += OnShootCanceledInput;

        if (livingEntity != null)
        {
            livingEntity.OnDeath += OnStunned;
            livingEntity.OnRevive += OnRevive;
        }
    }

    private void OnDisable()
    {
        if (!photonView.IsMine) return;
        InputManager.OnMoveInput -= OnMoveInput;
        InputManager.OnXMouseInput -= OnMouseInput;
        InputManager.OnZoomPressed -= OnZoomInput;
        InputManager.OnZoomCanceledPressed -= OnZoomCanceledInput;
        InputManager.OnReloadPressed -= OnReloadInput;
        InputManager.OnSkillPressed -= OnSkillInput;
        InputManager.OnItemPressed -= OnItemInput;
        InputManager.OnShootPressed -= OnShootInput;
        InputManager.OnShootCanceledPressed -= OnShootCanceledInput;

        if (livingEntity != null)
        {
            livingEntity.OnDeath -= OnStunned;
            livingEntity.OnRevive -= OnRevive;
        }
    }
    
    private void Update()
    {
        if (!photonView.IsMine) return;
        if(GameManager.Instance.IsGameOver()) return;
        HandleMovementAnimation();
        HandleJumpAnimation();
        HandleTeddyBearWeaponState();
        HandleHealthBasedAnimation();
        HandleUpperBodyLayer();
    }

    private void HandleUpperBodyLayer()
    {
        bool isInMovement = animator.GetCurrentAnimatorStateInfo(0).IsName("Movement");
        float weight = 0f;
        
        // 장전 중이거나 (점프 중이면서 총을 쏘는 중)이거나 Movement 상태일 때 상체 레이어 활성화
        if (isReloading || (isJumping && isShooting) || isInMovement)
        {
            weight = 1f;
        }
        
        animator.SetLayerWeight(upperBodyLayerIndex, weight);
    }

    // 체력 기반 애니메이션 처리
    private void HandleHealthBasedAnimation()
    {
        if (livingEntity == null) return;

        // MoveController의 스턴 상태 확인하여 stun 애니메이션 제어
        if (moveController != null)
        {
            bool isStunned = moveController.IsStunned();
        }
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
        if(GameManager.Instance.IsGameOver()) return;
        animator.SetTrigger("Death");
    }

    private void OnRevive()
    {
        if(GameManager.Instance.IsGameOver()) return;
        animator.SetTrigger("Revive");
        // 부활 시 스턴 상태 해제
        if (moveController != null)
        {
            moveController.SetStunned(false);
        }
    }

    // 재장전시 트리거 실행
    void OnReloadInput()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver()) return;
        if (isReloading) return;

        // 점프 중에 재장전을 하면 즉시 재장전 상태로 설정
        if (isJumping)
        {
            isReloading = true;
            animator.SetLayerWeight(upperBodyLayerIndex, 1f);
        }

        animator.SetTrigger("Reload");
    }

    // 재장전 시작
    void OnReloadStart()
    {
        Debug.Log("OnReloadStart 호출됨");
        isReloading = true;
    }

    // 재장전 종료
    void OnReloadEnd()
    {
        Debug.Log("OnReloadEnd 호출됨");
        isReloading = false;
    }

    void HandleJumpAnimation()
    {
        bool grounded = moveController.IsGrounded();

        if (!moveController.IsGrounded())
        {
            isJumping = true; // 점프 중 상태로 설정
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
            isJumping = false; // 착지 시 점프 상태 해제
            animator.SetBool("JumpUp", false);
            animator.SetBool("JumpDown", false);
        }
    }

    private void OnShootInput()
    {
        isShooting = true;
    }

    private void OnShootCanceledInput()
    {
        isShooting = false;
    }
    
    // 조준 시작 시 호출
    void OnZoomInput()
    {
        if(GameManager.Instance.IsGameOver()) return;
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.Body, gunIK.bodyTarget, 0.04f);
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.RightFoot, gunIK.rightLegTarget, 0.3f);
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.LeftFoot, gunIK.leftLegTarget, 0.3f);

        animator.SetFloat("SpeedMultiplier", 0.6f); // 조준 시 이동 느리게
    }

    // 조준 해제 시 호출
    void OnZoomCanceledInput()
    {
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.Body, gunIK.bodyTarget, 0.01f);
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.RightFoot, gunIK.rightLegTarget, 0.2f);
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.LeftFoot, gunIK.leftLegTarget, 0.2f);

        animator.SetFloat("SpeedMultiplier", 1.2f); // 조준 해제 시 원래 속도
    }

    // 스피드 스킬
    void OnSkillInput()
    {
        if(GameManager.Instance.IsGameOver()) return;
        if (skill != null && skill.CanUse)
        {   
            animator.SetTrigger(skillAnimationTriggerName);
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
        if(GameManager.Instance.IsGameOver()) return;
        itemSkill = itemController.GetFirstActiveItem();
        itemSkillAnimationTriggerName = itemSkill.SkillAnimationTriggerName;



        if (itemSkill != null && itemSkill.CanUse)
        {
            animator.SetTrigger(itemSkill.SkillAnimationTriggerName);
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

    public void PlayVictoryPose()
    {
        if(GameManager.Instance.IsGameOver())
        {
            StartCoroutine(PlayVictoryPoseAfterDelay());
        }
    }

    private IEnumerator PlayVictoryPoseAfterDelay()
    {
        yield return new WaitForSeconds(5f);
        animator.SetTrigger("Victory");
        animator.SetLayerWeight(upperBodyLayerIndex, 0f);
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.LeftHand, gunIK.leftHandTarget, 0f, 0f);
    }
}