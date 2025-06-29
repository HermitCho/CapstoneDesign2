using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestMoveAnimationController : MonoBehaviour
{

    public PlayerIKController iKController;
    private Animator animator;

    // 현재값과 목표값
    private float currentMoveX = 0f;
    private float currentMoveY = 0f;
    private float targetMoveX = 0f;
    private float targetMoveY = 0f;

    // Lerp 속도 (부드럽게 적용할 속도)
    [Header("Lerp 속도 설정")]
    public float moveLerpSpeed = 10f; // 이동 애니메이션 부드럽게 변경할 속도

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (iKController == null)
        {
            iKController = GetComponent<PlayerIKController>(); 
        }
    }
    void Update()
    {
        // 매 프레임 목표값으로 부드럽게 수렴
        currentMoveX = Mathf.Lerp(currentMoveX, targetMoveX, moveLerpSpeed * Time.deltaTime);
        currentMoveY = Mathf.Lerp(currentMoveY, targetMoveY, moveLerpSpeed * Time.deltaTime);

        animator.SetFloat("MoveX", currentMoveX);
        animator.SetFloat("MoveY", currentMoveY);

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            animator.SetTrigger("Reload");
        }
    }

    void OnEnable()
    {
        // InputManager 이벤트 구독
        InputManager.OnMoveInput += OnMoveInput;
        InputManager.OnXMouseInput += OnMouseInput;
        InputManager.OnJumpPressed += OnJumpInput;
        InputManager.OnZoomPressed += OnZoomInput;
        InputManager.OnZoomCanceledPressed += OnZoomCanceledInput;
    }

    void OnDisable()
    {
        // InputManager 이벤트 구독 해제
        InputManager.OnMoveInput -= OnMoveInput;
        InputManager.OnXMouseInput -= OnMouseInput;
        InputManager.OnJumpPressed -= OnJumpInput;
        InputManager.OnZoomPressed -= OnZoomInput;
        InputManager.OnZoomCanceledPressed -= OnZoomCanceledInput;
    }


    void OnMoveInput(Vector2 moveInput)
    {
        // 입력될 때 목표값만 갱신
        targetMoveX = moveInput.x;
        targetMoveY = moveInput.y;
    }

    void OnMouseInput(Vector2 mouseInput)
    {
        // 캐릭터 회전 애니메이션 재생
        iKController.turnAngle += mouseInput.x;
        iKController.turnAngle = Mathf.Clamp(iKController.turnAngle, -iKController.maxTurn, iKController.maxTurn);
        animator.SetFloat("Turn", mouseInput.x);

        // 위아래 회전 애니메이션 재생
        iKController.lookAngle -= mouseInput.y;
        iKController.lookAngle = Mathf.Clamp(iKController.lookAngle, -iKController.maxLookDown, iKController.maxLookUp);
        animator.SetFloat("LookAngle", iKController.lookAngle);
    }
    
    void OnJumpInput()
    {      
        //캐릭터 점프 애니메이션 재생
        animator.SetTrigger("Jump");
    }

    void OnZoomInput()
    {
        animator.SetBool("IsAiming", true);
    }

    void OnZoomCanceledInput()
    {
        animator.SetBool("IsAiming", false);
    }
}