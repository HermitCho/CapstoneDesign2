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

    // 캐릭터 이동 정보를 가져오는 컴포넌트
    private MoveController moveController;
    private CameraController cameraController;

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
        skill = GetComponent<Skill>();
        itemSkill = GetComponent<Skill>();
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

        if (isReloading)
        {
            animator.SetLayerWeight(upperBodyLayerIndex, 1f);
            return;
        }

        bool isInMovement = animator.GetCurrentAnimatorStateInfo(0).IsName("Movement");
        bool isJumping = animator.GetBool("JumpUp");

        animator.SetLayerWeight(upperBodyLayerIndex, isInMovement || isJumping ? 1f : 0f);        
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
        if(GameManager.Instance.IsGameOver()) return;
        isReloading = true;
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.RightHand, gunIK.rightHandTarget, 0f, 0f);
        // aimIK.enabled = false;
        animator.SetTrigger("Reload");

    }

    // 재장전 시작
    void OnReloadStart()
    {
        
    }

    // 재장전 종료
    void OnReloadEnd()
    {
        Debug.Log("OnReloadEnd 호출됨");
        isReloading = false;
        gunIK.SetEffectorPositionWeight(FullBodyBipedEffector.RightHand, gunIK.rightHandTarget, 1f, 0.5f);
        // aimIK.enabled = true;
        //animator.SetLayerWeight(upperBodyLayerIndex, 0f);

    }

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
}