using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RootMotion.FinalIK;

/// <summary>
/// 싱글플레이 튜토리얼용 캐릭터 애니메이션 컨트롤러
/// (이동/점프/사격/재장전/스킬/아이템 애니메이션 전용)
/// </summary>
public class TutorialAnimation : MonoBehaviour
{
    // --- 컴포넌트 ---
    private Animator animator;
    private Rigidbody rb;
    private MoveController moveController;
    private CameraController cameraController;
    private ItemController itemController;
    private FootstepSoundPlayer footstepSoundPlayer;
    private AimIK aimIK;
    private Skill skill;
    private Skill itemSkill;
    private TestGun gun;

    // --- 애니메이션/상태 ---
    private int upperBodyLayerIndex;
    private Vector2 moveInput;
    private Vector2 mouseInput;
    private bool isReloading = false;
    private bool isJumping = false;
    private bool isShooting = false;
    private float upperBodyWeightVelocity;
    private float reloadCooldown = 0f;
    private bool firstReload = true;

    // --- 스킬 관련 ---
    private Coroutine speedSkillCoroutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        moveController = GetComponent<MoveController>();
        cameraController = GetComponent<CameraController>();
        itemController = GetComponent<ItemController>();
        footstepSoundPlayer = GetComponent<FootstepSoundPlayer>();
        aimIK = GetComponent<AimIK>();
        skill = GetComponent<Skill>();
        gun = GetComponentInChildren<TestGun>();

        upperBodyLayerIndex = animator.GetLayerIndex("UpperBody");
        animator.SetLayerWeight(upperBodyLayerIndex, 1f);
        animator.SetFloat("SpeedMultiplier", 1.2f);
    }

    private void OnEnable()
    {
        InputManager.OnMoveInput += OnMoveInput;
        InputManager.OnXMouseInput += OnMouseInput;
        InputManager.OnReloadPressed += OnReloadInput;
        InputManager.OnSkillPressed += OnSkillInput;
        InputManager.OnItemPressed += OnItemInput;
        InputManager.OnShootPressed += OnShootInput;
        InputManager.OnShootCanceledPressed += OnShootCanceledInput;
    }

    private void OnDisable()
    {
        InputManager.OnMoveInput -= OnMoveInput;
        InputManager.OnXMouseInput -= OnMouseInput;
        InputManager.OnReloadPressed -= OnReloadInput;
        InputManager.OnSkillPressed -= OnSkillInput;
        InputManager.OnItemPressed -= OnItemInput;
        InputManager.OnShootPressed -= OnShootInput;
        InputManager.OnShootCanceledPressed -= OnShootCanceledInput;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver()) return;

        HandleMovementAnimation();
        HandleJumpAnimation();
        HandleUpperBodyLayer();

        if (reloadCooldown > 0f)
            reloadCooldown -= Time.deltaTime;
    }

    // --- 애니메이션 처리 ---
    private void HandleUpperBodyLayer()
    {
        float targetWeight = 0f;
        bool isInMovement = animator.GetCurrentAnimatorStateInfo(0).IsName("Movement");
        bool isInJumpDown = animator.GetCurrentAnimatorStateInfo(0).IsName("JumpDown");

        if (isReloading || (isJumping && isShooting) || isInMovement)
            targetWeight = 1f;
        if (isInJumpDown && isShooting)
            targetWeight = 1f;

        float smoothedWeight = Mathf.SmoothDamp(
            animator.GetLayerWeight(upperBodyLayerIndex),
            targetWeight,
            ref upperBodyWeightVelocity,
            0.15f
        );

        animator.SetLayerWeight(upperBodyLayerIndex, smoothedWeight);
    }

    private void HandleMovementAnimation()
    {
        bool isMoving = moveInput.magnitude > 0.1f;
        animator.SetFloat("MoveX", moveInput.x, 0.1f, Time.deltaTime);
        animator.SetFloat("MoveY", moveInput.y, 0.1f, Time.deltaTime);
        footstepSoundPlayer?.SetIsMoving(isMoving);
    }

    private void HandleJumpAnimation()
    {
        if (moveController == null) return;

        if (!moveController.IsGrounded())
        {
            isJumping = true;
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
            isJumping = false;
            animator.SetBool("JumpUp", false);
            animator.SetBool("JumpDown", false);
        }
    }

    // --- 입력 처리 ---
    private void OnMoveInput(Vector2 input) => moveInput = input;
    private void OnMouseInput(Vector2 input) => mouseInput = input;
    private void OnShootInput() => isShooting = true;
    private void OnShootCanceledInput() => isShooting = false;

    private void OnReloadInput()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver()) return;
        if (isReloading || gun == null) return;
        if (gun.CurrentMagAmmo >= gun.GetGunData().maxAmmo) return;
        if (reloadCooldown > 0f) return;

        // 재장전 시작
        animator.SetLayerWeight(upperBodyLayerIndex, 1f);
        animator.SetBool("Reload", true);
        isReloading = true;

        // 첫 장전만 쿨타임 0초
        reloadCooldown = firstReload ? 0f : 3.3f;
        firstReload = false;
    }

    // --- 애니메이션 이벤트에서 호출 ---
    private void OnReloadStart() => isReloading = true;
    private void OnReloadEnd()
    {
        if (!isReloading) return;
        gun?.Reload();
        isReloading = false;
        animator.SetBool("Reload", false);
    }

    private void OnSkillInput()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver()) return;
        if (skill != null && skill.CanUse)
        {
            animator.SetTrigger(skill.SkillAnimationTriggerName);
            if (speedSkillCoroutine != null) StopCoroutine(speedSkillCoroutine);
            speedSkillCoroutine = StartCoroutine(SpeedSkillRoutine());
        }
    }

    private void OnItemInput()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver()) return;
        itemSkill = itemController.GetFirstActiveItem();
        if (itemSkill != null && itemSkill.CanUse)
        {
            animator.SetTrigger(itemSkill.SkillAnimationTriggerName);
        }
    }

    private IEnumerator SpeedSkillRoutine()
    {
        animator.SetFloat("SpeedMultiplier", 1.5f);
        yield return new WaitForSeconds(3f);
        animator.SetFloat("SpeedMultiplier", 1.2f);
    }

    public void PlayVictoryPose()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
        {
            StartCoroutine(PlayVictoryPoseAfterDelay());
        }
    }

    private IEnumerator PlayVictoryPoseAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        animator.SetTrigger("Victory");
        animator.SetLayerWeight(upperBodyLayerIndex, 0f);
    }
}
