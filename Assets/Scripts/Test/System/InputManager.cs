using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [Header("Input System")]
    [SerializeField] private PlayerAction playerAction;
    
    // 입력 값들
    private Vector2 moveInput;
    private Vector2 xMouseInput;
    private Vector2 yMouseInput;
    private bool jumpPressed;
    private bool skillPressed;
    private bool itemPressed;
    private bool itemUIPressed;
    private bool zoomPressed; 
    private bool shootPressed;
    private bool reloadPressed;
    
    // 이벤트들 (다른 스크립트들이 구독)
    public static event Action<Vector2> OnMoveInput;
    public static event Action<Vector2> OnXMouseInput;
    public static event Action<Vector2> OnYMouseInput;
    public static event Action OnJumpPressed;
    public static event Action OnSkillPressed;
    public static event Action OnItemPressed;
    public static event Action OnItemUIPressed;
    public static event Action OnItemUICanceledPressed;
    public static event Action OnZoomPressed;
    public static event Action OnZoomCanceledPressed;
    public static event Action OnShootPressed;
    public static event Action OnShootCanceledPressed;
    public static event Action OnReloadPressed;
    
    // 현재 입력 값들 (다른 스크립트들이 읽기용)
    public static Vector2 MoveInput { get; private set; }
    public static Vector2 XMouseInput { get; private set; }
    public static Vector2 YMouseInput { get; private set; }
    public static bool JumpPressed { get; private set; }
    public static bool SkillPressed { get; private set; }
    public static bool ItemPressed { get; private set; }
    public static bool ItemUIPressed { get; private set; }
    public static bool ZoomPressed { get; private set; }
    public static bool ShootPressed { get; private set; }
    public static bool ReloadPressed { get; private set; }
    
    void Awake()
    {
        // PlayerAction 초기화
        if (playerAction == null)
            playerAction = new PlayerAction();
    }
    
    void OnEnable()
    {
        
        // PlayerAction이 null인 경우 다시 초기화
        if (playerAction == null)
        {
            playerAction = new PlayerAction();
            Debug.Log("PlayerAction이 OnEnable에서 초기화되었습니다.");
        }
            
        // Input System 활성화
        playerAction.Enable();
        Debug.Log("PlayerAction이 활성화되었습니다.");
        
        // 이벤트 등록 (try-catch로 안전하게 처리)
        try
        {
            playerAction.Player.Move.performed += OnMove;
            playerAction.Player.Move.canceled += OnMove;
            playerAction.Player.Rotation.performed += OnMouseX;
            playerAction.Player.YRotation.performed += OnMouseY;
            playerAction.Player.Jump.performed += OnJump;
            playerAction.Player.Skill.performed += OnSkill;
            playerAction.Player.Item.performed += OnItem;
            playerAction.Player.Zoom.performed += OnZoom;
            playerAction.Player.Zoom.canceled += OnZoomCanceled;
            playerAction.Player.Shoot.performed += OnShoot;
            playerAction.Player.Shoot.canceled += OnShootCanceled;
            playerAction.Player.Reload.performed += OnReload;

            Debug.Log("Player actions이 등록되었습니다.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Player actions 등록 실패: {e.Message}");
        }
        
        try
        {
            playerAction.UI.ItemUI.performed += OnItemUI;
            playerAction.UI.ItemUI.canceled += OnItemUICanceled;
            Debug.Log("UI actions이 등록되었습니다.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UI actions 등록 실패: {e.Message}");
        }
    }
    
    void OnDisable()
    {
        // PlayerAction이 null인 경우 처리하지 않음
        if (playerAction == null)
            return;
            
        // Input System 비활성화
        playerAction.Disable();
        
        // 이벤트 해제 (try-catch로 안전하게 처리)
        try
        {
            playerAction.Player.Move.performed -= OnMove;
            playerAction.Player.Move.canceled -= OnMove;
            playerAction.Player.Rotation.performed -= OnMouseX;
            playerAction.Player.YRotation.performed -= OnMouseY;
            playerAction.Player.Jump.performed -= OnJump;
            playerAction.Player.Skill.performed -= OnSkill;
            playerAction.Player.Item.performed -= OnItem;
            playerAction.Player.Zoom.performed -= OnZoom;
            playerAction.Player.Zoom.canceled -= OnZoomCanceled;
            playerAction.Player.Shoot.performed -= OnShoot;
            playerAction.Player.Shoot.canceled -= OnShootCanceled;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Player actions 해제 실패: {e.Message}");
        }
        
        try
        {
            playerAction.UI.ItemUI.performed -= OnItemUI;
            playerAction.UI.ItemUI.canceled -= OnItemUICanceled;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UI actions 해제 실패: {e.Message}");
        }
    }

    
    // 이동 입력 처리
    void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        MoveInput = moveInput;
        
        // 이벤트 발송
        OnMoveInput?.Invoke(moveInput);
    }
    
    // 마우스 X축 입력 처리
    void OnMouseX(InputAction.CallbackContext context)
    {
        float mouseX = context.ReadValue<float>();
        xMouseInput.x = mouseX;
        XMouseInput = xMouseInput;
        
        // 이벤트 발송
        OnXMouseInput?.Invoke(xMouseInput);
    }

    // 마우스 Y축 입력 처리
    void OnMouseY(InputAction.CallbackContext context)
    {
        float mouseY = context.ReadValue<float>();
        yMouseInput.y = mouseY;
        YMouseInput = yMouseInput;

        // 이벤트 발송
        OnYMouseInput?.Invoke(yMouseInput);
    }

    void OnZoom(InputAction.CallbackContext context)
    {
        zoomPressed = context.performed;
        ZoomPressed = zoomPressed;

        if (zoomPressed)
        {
            OnZoomPressed?.Invoke();
        }
    }

    void OnZoomCanceled(InputAction.CallbackContext context)
    {
        zoomPressed = false;
        ZoomPressed = zoomPressed;

        OnZoomCanceledPressed?.Invoke();
        
        
    }
    
    
    // 점프 입력 처리
    void OnJump(InputAction.CallbackContext context)
    {
        jumpPressed = context.performed;
        JumpPressed = jumpPressed;
        
        if (jumpPressed)
        {
            OnJumpPressed?.Invoke();
        }
    }
    
    // 스킬 입력 처리
    void OnSkill(InputAction.CallbackContext context)
    {
        skillPressed = context.performed;
        SkillPressed = skillPressed;
        
        if (skillPressed)
        {
            OnSkillPressed?.Invoke();
        }
    }
    
    // 아이템 입력 처리
    void OnItem(InputAction.CallbackContext context)
    {
        itemPressed = context.performed;
        ItemPressed = itemPressed;
        
        if (itemPressed)
        {
            OnItemPressed?.Invoke();
        }
    }
    
    // 아이템 UI 입력 처리
    void OnItemUI(InputAction.CallbackContext context)
    {
        itemUIPressed = context.performed;
        ItemUIPressed = itemUIPressed;
        
        if (itemUIPressed)
        {
            OnItemUIPressed?.Invoke();
        }
    }
    
    // 아이템 UI 취소 처리
    void OnItemUICanceled(InputAction.CallbackContext context)
    {
        itemUIPressed = false;
        ItemUIPressed = itemUIPressed;
        
        OnItemUICanceledPressed?.Invoke();
    }
    
    // 총 발사 입력 처리
    void OnShoot(InputAction.CallbackContext context)
    {
        shootPressed = context.performed;
        ShootPressed = shootPressed;

        if (shootPressed)
        {
            OnShootPressed?.Invoke();
        }
    }

    //총 발사 입력 취소 처리
    void OnShootCanceled(InputAction.CallbackContext context)
    {
        shootPressed = false;
        ShootPressed = shootPressed;

        OnShootCanceledPressed?.Invoke();
    }

    //총 재장전 입력 처리
    void OnReload(InputAction.CallbackContext context)
    {
        reloadPressed = context.performed;
        ReloadPressed = reloadPressed;

        if (reloadPressed)
        {
            OnReloadPressed?.Invoke();
        }
    }
} 