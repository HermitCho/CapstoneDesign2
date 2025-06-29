using UnityEngine;
using UnityEngine.InputSystem;
using Michsky.UI.Heat;

// 아이템 UI 관리 전용 스크립트
public class UIManager : MonoBehaviour
{
    #region 변수
    [Header("Heat UI Modal Window 컴포넌트 설정")]
    [Tooltip("아이템 모달 창 컴포넌트 - 자동할당")]
    [SerializeField] private ModalWindowManager modalWindowManager;

    [Header("Heat UI Progress Bar 컴포넌트 설정")]
    [Tooltip("플레이어 체력 표시 프로그레스바 컴포넌넌트 - 자동 할당")]
    [SerializeField] private ProgressBar playerHealthProgressBar;

    [Header("현재 체력 값")]
    [Tooltip("현재 체력 설정 - 스킬 및 아이템에 따라 변경할 수 있게끔 구현")]
    [Range(0, 500)]
    [SerializeField] private float curruentPlayerHealth = 100;

    [Header("최대 체력 값")]
    [Tooltip("최대 체력 설정 - 스킬 및 아이템에 따라 변경할 수 있게끔 구현")]
    [Range(0, 500)]
    [SerializeField] private float maxPlayerHealth = 100;


    private float healValueStatus = 0;



    //아이템 모달창 열림 상태 확인 변수
    private bool isItemUIOpen = false;






    #endregion

    #region 이벤트 구독
    void OnEnable()
    {
        // InputManager 이벤트 구독
        InputManager.OnItemUIPressed += OnItemUIPressed;
        InputManager.OnItemUICanceledPressed += OnItemUICanceled;
    }

    void OnDisable()
    {
        // InputManager 이벤트 구독 해제
        InputManager.OnItemUIPressed -= OnItemUIPressed;
        InputManager.OnItemUICanceledPressed -= OnItemUICanceled;
    }
    #endregion

    #region 컴포넌트 및 초기 상태 설정
    void Awake()
    {   
       
        // ModalWindowManager가 할당되지 않은 경우 자동으로 찾기
        if (modalWindowManager == null && playerHealthProgressBar == null)
        {
            // 모달창과 프로그레스바 찾기
            //추후에 모달창과 프로그레스바 찾는 방법 수정 필요
            modalWindowManager = FindObjectOfType<ModalWindowManager>();
            playerHealthProgressBar = FindObjectOfType<ProgressBar>();
            
            if (modalWindowManager == null)
            {
                // 씬의 모든 오브젝트에서 ModalWindowManager 찾기
                ModalWindowManager[] allManagers = FindObjectsOfType<ModalWindowManager>();
                if (allManagers.Length > 0)
                {
                    modalWindowManager = allManagers[0];
                    Debug.Log($"ModalWindowManager를 찾았습니다: {modalWindowManager.name}");
                }
            }
            else if (playerHealthProgressBar == null)
            {
                // 씬의 모든 오브젝트에서 ProgressBar 찾기
                ProgressBar[] allProgressBars = FindObjectsOfType<ProgressBar>();
                if (allProgressBars.Length > 0)
                {
                    playerHealthProgressBar = allProgressBars[0];
                    Debug.Log($"ProgressBar를 찾았습니다: {playerHealthProgressBar.name}");
                }
            }
        }
        // 초기 상태 설정
        isItemUIOpen = false;
    }
    #endregion




    #region 아이템 UI 관련 메서드 모음
    // Tab키를 눌렀을 때 (UI 열기) - InputManager 이벤트
    void OnItemUIPressed()
    {      
        // Heat UI Modal Window 열기
        OpenItemUI();
    }
    
    // Tab키를 놓았을 때 (UI 닫기) - InputManager 이벤트
    void OnItemUICanceled()
    {     
        // Heat UI Modal Window 닫기
        CloseItemUI();
    }
    
    // 아이템 UI 열기 (Heat UI Modal Window 사용)
    public void OpenItemUI()
    {
        if (modalWindowManager != null)
        {
            // Modal Window가 닫혀있을 때만 열기
            if (!modalWindowManager.isOn)
            {
                isItemUIOpen = true;
                
                // Heat UI Modal Window 열기 (매개변수 없음)
                modalWindowManager.OpenWindow();

                // 커서 잠금 해제
                CursorState(false);
            }
        }
        else
        {
            Debug.LogError("ModalWindowManager가 할당되지 않았습니다.");
        }
    }
    
    // 아이템 UI 닫기 (Heat UI Modal Window 사용)
    public void CloseItemUI()
    {
        if (modalWindowManager != null)
        {
            // Modal Window가 열려있을 때만 닫기
            if (modalWindowManager.isOn)
            {
                isItemUIOpen = false;
                
                // Heat UI Modal Window 닫기 (매개변수 없음)
                modalWindowManager.CloseWindow();

                // 커서 잠금
                CursorState(true);
            }
        }
    }
    
    // UI 상태 확인
    public bool IsItemUIOpen => isItemUIOpen;
    
    // 외부에서 UI 상태 변경 가능
    public void SetItemUIOpen(bool isOpen)
    {
        if (isOpen)
            OpenItemUI();
        else
            CloseItemUI();
    }
    
    // ModalWindowManager 수동 설정
    public void SetModalWindowManager(ModalWindowManager manager)
    {
        modalWindowManager = manager;
    }
    
    // 현재 Modal Window 상태 확인 (Heat UI의 isOn 프로퍼티 사용)
    public bool IsModalWindowOpen()
    {
        if (modalWindowManager != null)
        {
            return modalWindowManager.isOn;
        }
        return false;
    }
    #endregion


 

    #region 체력 프로그레스바 관련 메서드 모음
    private void ProgressBarSetting()
    {
        playerHealthProgressBar.currentValue = curruentPlayerHealth;
        playerHealthProgressBar.maxValue = maxPlayerHealth;
    }

    // 현재 체력 값 프로그레스바 업데이트
    private void CurrentPlayerhealthProgressBarUpdate()
    {
        if (playerHealthProgressBar != null)
        {
            //데미지나 힐에 따라 체력 업데이트 기능 구현 필요
            playerHealthProgressBar.currentValue = Mathf.Clamp(curruentPlayerHealth + healValueStatus, 0, maxPlayerHealth);
        }
        
    }

    // 최대 체력 값 프로그레스바 업데이트
    private void MaxPlayerhealthProgressBarUpdate()
    {
        //아이템 및 스킬에 따라 최대 체력 변경 기능 구현 필요
        playerHealthProgressBar.maxValue = Mathf.Clamp(maxPlayerHealth, 0, maxPlayerHealth);
    }


    // 데미지 값 불러오기
    private void DamageValueGet(float damage)
    {
        healValueStatus = damage * -1f;
    }

    // 힐 값 불러오기
    private void HealValueGet(float heal)
    {
        healValueStatus = heal;
    }


    #region 체력 프로그레스바 관련 외부 호출용 메서드 모음
    // 현재 체력 값 반환
    public float GetCurrentPlayerHealth()
    {
        return curruentPlayerHealth;
    }

    // 현재 체력 값 설정
    public void SetCurrentPlayerHealth(float health)
    {
        curruentPlayerHealth = health;
        CurrentPlayerhealthProgressBarUpdate();
    }

    // 최대 체력 값 반환
    public float GetMaxPlayerHealth()
    {
        return maxPlayerHealth;
    }

    // 최대 체력 값 설정
    public void SetMaxPlayerHealth(float health)
    {
        maxPlayerHealth = health;
        MaxPlayerhealthProgressBarUpdate();
    }
    #endregion
    #endregion



    #region 커서 잠금 및 해제
    private void CursorState(bool isLock)
    {
        if (isLock)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    #endregion

} 