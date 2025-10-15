using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class InputManager : MonoBehaviourPun
{
    [Header("Input System")]
    [SerializeField] private PlayerAction playerAction;

    private PhotonView photonView;

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
    private bool changeItemPressed;
    private bool detachPressed;
    private bool handleGunPressed;

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
    public static event Action OnChangeItemPressed;
    public static event Action OnDetachPressed;
    public static event Action OnHandleGunPressed;

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
    public static bool ChangeItemPressed { get; private set; }
    public static bool DetachPressed { get; private set; }
    public static bool HandleGunPressd { get; private set; }

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        if (!photonView.IsMine) return;
        // PlayerAction 초기화
        if (playerAction == null)
            playerAction = new PlayerAction();
    }

    void OnEnable()
    {
        if (!photonView.IsMine) return;
        // PlayerAction이 null인 경우 다시 초기화
        if (playerAction == null)
        {
            playerAction = new PlayerAction();
            Debug.Log("PlayerAction이 OnEnable에서 초기화되었습니다.");
        }

        // Input System 활성화 전에 저장된 바인딩 로드
        LoadKeyBindingOverrides();

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
            playerAction.Player.ChangeItem.performed += OnChangeItem;
            playerAction.Player.Detach.performed += OnDetach;
            playerAction.Player.HandleGun.performed += OnHandleGun;

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
        if (!photonView.IsMine) return;
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
            playerAction.Player.Reload.performed -= OnReload;
            playerAction.Player.ChangeItem.performed -= OnChangeItem;
            playerAction.Player.Detach.performed -= OnDetach;
            playerAction.Player.HandleGun.performed -= OnHandleGun;
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

    // 아이템 변경 입력 처리
    void OnChangeItem(InputAction.CallbackContext context)
    {
        changeItemPressed = context.performed;
        ChangeItemPressed = changeItemPressed;

        if (changeItemPressed)
        {
            OnChangeItemPressed?.Invoke();
        }
    }

    void OnDetach(InputAction.CallbackContext context)
    {
        detachPressed = context.performed;
        DetachPressed = detachPressed;

        if (detachPressed)
        {
            OnDetachPressed?.Invoke();
        }
    }
    void OnHandleGun(InputAction.CallbackContext context)
    {
        handleGunPressed = context.performed;
        HandleGunPressd = handleGunPressed;

        if (handleGunPressed)
        {
            OnHandleGunPressed?.Invoke();
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

    #region 키 바인딩 오버라이드 로드

    /// <summary>
    /// PlayerPrefs에서 저장된 키 바인딩 오버라이드를 로드하여 적용
    /// SettingPanel에서 저장한 바인딩을 모든 PlayerAction 인스턴스에 자동 적용
    /// </summary>
    private void LoadKeyBindingOverrides()
    {
        if (playerAction == null) return;

        // Move 액션 바인딩 (2D Vector Composite)
        LoadMoveBinding("Up", 1);
        LoadMoveBinding("Down", 2);
        LoadMoveBinding("Left", 3);
        LoadMoveBinding("Right", 4);

        // 일반 액션 바인딩
        LoadActionBinding("Jump", playerAction.Player.Jump);
        LoadActionBinding("Reload", playerAction.Player.Reload);
        LoadActionBinding("Skill", playerAction.Player.Skill);
        LoadActionBinding("Item", playerAction.Player.Item);
        LoadActionBinding("ChangeItem", playerAction.Player.ChangeItem);
        LoadActionBinding("Detach", playerAction.Player.Detach);

        Debug.Log("InputManager: 저장된 키 바인딩 오버라이드 로드 완료");
    }

    /// <summary>
    /// Move 액션의 특정 방향 바인딩 로드
    /// </summary>
    private void LoadMoveBinding(string direction, int bindingIndex)
    {
        string savedKey = PlayerPrefs.GetString($"KeyBinding_{direction}", "");
        if (string.IsNullOrEmpty(savedKey)) return;

        string keyPath = ConvertKeyNameToPath(savedKey);
        playerAction.Player.Move.ApplyBindingOverride(bindingIndex, keyPath);

        Debug.Log($"InputManager: Move 바인딩 로드 - {direction}: {keyPath}");
    }

    /// <summary>
    /// 일반 액션 바인딩 로드
    /// </summary>
    private void LoadActionBinding(string actionName, InputAction action)
    {
        string savedKey = PlayerPrefs.GetString($"KeyBinding_{actionName}", "");
        if (string.IsNullOrEmpty(savedKey)) return;

        string keyPath = ConvertKeyNameToPath(savedKey);
        action.ApplyBindingOverride(0, keyPath);

        Debug.Log($"InputManager: {actionName} 바인딩 로드 - {keyPath}");
    }

    /// <summary>
    /// 키 이름을 Input System 경로로 변환
    /// </summary>
    private string ConvertKeyNameToPath(string keyName)
    {
        switch (keyName)
        {
            case "Space": return "<Keyboard>/space";
            case "LeftShift": return "<Keyboard>/leftShift";
            case "RightShift": return "<Keyboard>/rightShift";
            case "LeftCtrl": return "<Keyboard>/leftCtrl";
            case "RightCtrl": return "<Keyboard>/rightCtrl";
            case "LeftAlt": return "<Keyboard>/leftAlt";
            case "RightAlt": return "<Keyboard>/rightAlt";
            case "Enter": return "<Keyboard>/enter";
            case "Escape": return "<Keyboard>/escape";
            case "Backspace": return "<Keyboard>/backspace";
            case "Tab": return "<Keyboard>/tab";
            default:
                return $"<Keyboard>/{keyName.ToLower()}";
        }
    }

    #endregion

    #region 입력 차단/복원 (키 바인딩 중 사용)

    /// <summary>
    /// 모든 입력 차단 (키 바인딩 중)
    /// </summary>
    public void DisableInput()
    {
        if (playerAction == null) return;

        playerAction.Disable();
        Debug.Log("InputManager: 모든 입력 차단됨 (키 바인딩 모드)");
    }

    /// <summary>
    /// 입력 복원
    /// </summary>
    public void EnableInput()
    {
        if (playerAction == null) return;

        playerAction.Enable();
        Debug.Log("InputManager: 입력 복원됨 (키 바인딩 완료)");
    }

    #endregion
}