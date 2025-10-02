using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using RootMotion.FinalIK;
using Photon.Pun;

/// <summary>
/// 캐릭터의 이동/점프/조준/재장전/스킬/아이템 애니메이션을 제어하는 컨트롤러
/// </summary>
public class TestMoveAnimationController : MonoBehaviourPun, IPunObservable
{
    // --- 컴포넌트 ---
    private Animator animator;
    private Rigidbody rb;
    private PhotonView photonView;
    private MoveController moveController;
    private CameraController cameraController;
    private ItemController itemController;
    private FootstepSoundPlayer footstepSoundPlayer;
    private AimIK aimIK;
    private LivingEntity livingEntity;
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
    private string skillAnimationTriggerName = "None";
    private string itemSkillAnimationTriggerName = "None";

    // --- 무기/테디베어 ---
    [SerializeField] private Crown teddyBear;
    [SerializeField] private GameObject gunObject;
    private bool previousAttachState = false;

    // --- 포톤 동기화용 변수 ---
    private Vector2 remoteMove;
    private bool remoteIsJumpingUp;
    private bool remoteIsJumpingDown;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        photonView = GetComponent<PhotonView>();
        moveController = GetComponent<MoveController>();
        cameraController = GetComponent<CameraController>();
        itemController = GetComponent<ItemController>();
        footstepSoundPlayer = GetComponent<FootstepSoundPlayer>();
        aimIK = GetComponent<AimIK>();
        livingEntity = GetComponent<LivingEntity>();
        skill = GetComponent<Skill>();
        gun = gunObject?.GetComponent<TestGun>();

        upperBodyLayerIndex = animator.GetLayerIndex("UpperBody");
        animator.SetLayerWeight(upperBodyLayerIndex, 1f);
        animator.SetFloat("SpeedMultiplier", 1.2f);

        if (skill != null) skillAnimationTriggerName = skill.SkillAnimationTriggerName;
    }

    private void OnEnable()
    {
        if (!photonView.IsMine) return;

        InputManager.OnMoveInput += OnMoveInput;
        InputManager.OnXMouseInput += OnMouseInput;
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
        if (GameManager.Instance.IsGameOver()) return;

        if (photonView.IsMine)
        {
            HandleMovementAnimation();
            HandleJumpAnimation();
            HandleTeddyBearWeaponState();
            HandleHealthBasedAnimation();
            HandleUpperBodyLayer();

            if (reloadCooldown > 0f) reloadCooldown -= Time.deltaTime;
        }
        else
        {
            HandleRemoteAnimation();
        }
    }

    private void HandleRemoteAnimation()
    {
        // 부드럽게 이동 애니메이션 보간
        animator.SetFloat("MoveX", Mathf.Lerp(animator.GetFloat("MoveX"), remoteMove.x, Time.deltaTime * 10f));
        animator.SetFloat("MoveY", Mathf.Lerp(animator.GetFloat("MoveY"), remoteMove.y, Time.deltaTime * 10f));

        // 점프 상태 동기화
        animator.SetBool("JumpUp", remoteIsJumpingUp);
        animator.SetBool("JumpDown", remoteIsJumpingDown);
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

        // 부드럽게 보간
        float smoothedWeight = Mathf.SmoothDamp(
            animator.GetLayerWeight(upperBodyLayerIndex), // 현재값
            targetWeight,                                // 목표값
            ref upperBodyWeightVelocity,                 // 속도 참조
            0.15f                                        // 스무딩 시간
        );

        animator.SetLayerWeight(upperBodyLayerIndex, smoothedWeight);
    }

    private void HandleHealthBasedAnimation()
    {
        if (livingEntity == null || moveController == null) return;
        bool isStunned = moveController.IsStunned();
        // 필요 시 애니메이션 파라미터 추가 가능
    }

    private void HandleMovementAnimation()
    {
        bool isMoving = moveInput.magnitude > 0.1f;
        animator.SetFloat("MoveX", moveInput.x, 0.1f, Time.deltaTime);
        animator.SetFloat("MoveY", moveInput.y, 0.1f, Time.deltaTime);
        footstepSoundPlayer.SetIsMoving(isMoving);
    }

    private void HandleJumpAnimation()
    {
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

    private void HandleTeddyBearWeaponState()
    {
        if (teddyBear == null || gunObject == null) return;

        bool isAttached = teddyBear.IsAttached();
        if (previousAttachState != isAttached)
        {
            gunObject.SetActive(!isAttached);
            previousAttachState = isAttached;
        }
    }

    // --- 상태 이벤트 ---
    private void OnStunned()
    {
        if (GameManager.Instance.IsGameOver()) return;


        photonView.RPC("RpcPlayDeathAnimation", RpcTarget.All);
        moveController?.SetStunned(true);
    }

    private void OnRevive()
    {
        if (GameManager.Instance.IsGameOver()) return;

        photonView.RPC("RpcPlayReviveAnimation", RpcTarget.All);
        moveController?.SetStunned(false);
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
        // 총알이 꽉 찼으면 재장전 불가
        if (gun.CurrentMagAmmo >= gun.GetGunData().maxAmmo) return;
        if (reloadCooldown > 0f) return; // 쿨타임 중이면 무시

        photonView.RPC("RpcPlayReloadAnimation", RpcTarget.All);


        // // TestGun 재장전 호출
        // gun.Reload();

        // 첫 장전이면 쿨타임 0, 이후부터는 3.3초
        if (firstReload)
        {
            reloadCooldown = 0f;
            firstReload = false;
        }
        else
        {
            reloadCooldown = 3.3f; // 애니메이션 길이와 동일하게
        }
    }

    // 애니메이션 이벤트에서 호출
    private void OnReloadStart() => isReloading = true;
    private void OnReloadEnd()
    {
        if (!isReloading) return;

        // Reload 끝났으니 상태 초기화
        isReloading = false;
        animator.SetBool("Reload", false);
    }

    private void OnSkillInput()
    {
        if (GameManager.Instance.IsGameOver()) return;

        if (skill != null && skill.CanUse)
        {
            photonView.RPC("RpcPlaySkillAnimation", RpcTarget.All, skill.SkillAnimationTriggerName);

            if (speedSkillCoroutine != null) StopCoroutine(speedSkillCoroutine);
            speedSkillCoroutine = StartCoroutine(SpeedSkillRoutine());
        }
    }

    private void OnItemInput()
    {
        if (GameManager.Instance.IsGameOver()) return;

        itemSkill = itemController.GetFirstActiveItem();
        string triggerName = itemSkill.SkillAnimationTriggerName;
        if (itemSkill != null && itemSkill.CanUse)
        {
            photonView.RPC("RpcPlaySkillAnimation", RpcTarget.All, triggerName);
        }
    }

    // --- 공통 처리 ---
    private void PlaySkillAnimation(string triggerName)
    {
        animator.SetTrigger(triggerName);
        animator.SetLayerWeight(upperBodyLayerIndex, 0f);
    }

    private IEnumerator SpeedSkillRoutine()
    {
        animator.SetFloat("SpeedMultiplier", 1.5f);
        yield return new WaitForSeconds(3f);
        animator.SetFloat("SpeedMultiplier", 1.2f);
    }

    public void OnSkillEnd() { }

    public void PlayVictoryPose()
    {
        if (GameManager.Instance.IsGameOver())
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


    // --- RPC 처리 ---

    [PunRPC]
    private void RpcPlayReloadAnimation()
    {
        isReloading = true;
        animator.SetLayerWeight(upperBodyLayerIndex, 1f);
        animator.SetBool("Reload", true);
    }

    [PunRPC]
    private void RpcPlaySkillAnimation(string triggerName)
    {
        if(string.IsNullOrEmpty(triggerName) || triggerName == "None") return;

        animator.SetTrigger(triggerName);
        animator.SetLayerWeight(upperBodyLayerIndex, 0f);
    }

    [PunRPC]
    private void RpcPlayDeathAnimation()
    {
        animator.SetTrigger("Death");
    }

    [PunRPC]
    private void RpcPlayReviveAnimation()
    {
        animator.SetTrigger("Revive");
    }

    [PunRPC]
    private void RpcPlayVictoryPose()
    {
        if(GameManager.Instance.IsGameOver())
        {
            StartCoroutine(PlayVictoryPoseAfterDelay());
        }
    }

    // --- 포톤 동기화 ---


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            stream.SendNext(moveInput);
            stream.SendNext(animator.GetBool("JumpUp"));
            stream.SendNext(animator.GetBool("JumpDown"));
        }
        else
        {
            remoteMove = (Vector2)stream.ReceiveNext();
            remoteIsJumpingUp = (bool)stream.ReceiveNext();
            remoteIsJumpingDown = (bool)stream.ReceiveNext();
        }
    }
}
