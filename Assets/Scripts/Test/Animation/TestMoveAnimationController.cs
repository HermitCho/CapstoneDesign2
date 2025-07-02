using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestMoveAnimationController : MonoBehaviour
{
    private Animator animator;
    private Vector2 moveInput;
    private Vector2 mouseInput;
    private float turnValue;

    [Header("IK Controller")]
    [SerializeField] private WeaponIKController iKController; // IK Controller 참조

    [SerializeField] private float turnSmoothing = 5f;
    [SerializeField] private float turnSensitivity = 1f;

    [Header("MoveController 참조")]
    [SerializeField] private MoveController moveController;
    [SerializeField] private Rigidbody rb; // Rigidbody 참조

    private bool isReloading = false;  // 장전 상태 확인용

    private void Awake()
    {
        animator = GetComponent<Animator>();
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
        HandleTurnAnimation();
        HandleJumpAnimation();
    }

    void OnMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    void OnMouseInput(Vector2 input)
    {
        mouseInput = input;
    }

    void HandleMovementAnimation()
    {
        animator.SetFloat("MoveX", moveInput.x, 0.1f, Time.deltaTime);
        animator.SetFloat("MoveY", moveInput.y, 0.1f, Time.deltaTime);
    }

    void HandleTurnAnimation()
    {
        
    }

    void HandleJumpAnimation()
    {
        if (moveController == null || rb == null) return;

        bool isGrounded = GetPrivateField<bool>(moveController, "isGrounded");
        float verticalVelocity = rb.velocity.y;

        // Set the grounded status and vertical velocity for animation
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VerticalVelocity", verticalVelocity);

        // Trigger Jump animation when grounded and vertical velocity is not zero
        if (isGrounded && verticalVelocity <= 0.1f)  // when grounded and vertical velocity is low (or zero)
        {
            animator.SetTrigger("Jump");
            Debug.Log("[Animation] Jump Triggered");
        }

        Debug.Log($"[Anim] isGrounded: {isGrounded}, verticalVelocity: {verticalVelocity}, CurrentState: {animator.GetCurrentAnimatorStateInfo(0).shortNameHash}");
    }

    void OnZoomInput()
    {
        animator.SetBool("IsAiming", true);
    }

    void OnZoomCanceledInput()
    {
        animator.SetBool("IsAiming", false);
    }

    void OnReloadInput()
    {
        animator.SetTrigger("Reload");

        // 장전 중 IK 해제
        if (iKController != null)
        {
            iKController.SetLeftHandIK(false);  // 왼손 IK 해제
        }
        
        // 4초 후에 왼손 IK 설정
        Invoke("EnableLeftHandIK", 4f);  // 4초 후에 LeftHand IK 다시 활성화
    }
    
    void EnableLeftHandIK()
    {
        if (iKController != null)
        {
            iKController.SetLeftHandIK(true);  // 왼손 IK 다시 설정
        }
    }

    private T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field == null) return default;
        return (T)field.GetValue(obj);
    }
}
