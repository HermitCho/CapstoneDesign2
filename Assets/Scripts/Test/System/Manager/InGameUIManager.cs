using UnityEngine;
using Michsky.UI.Heat;

/// <summary>
/// 🎮 패널 매니저 기반 UI 시스템
/// HeatUI PanelManager와 연동하여 HUD, Pause, GameStart 패널 관리
/// </summary>
public class InGameUIManager : MonoBehaviour
{
    [Header("🎮 HeatUI 패널 매니저")]
    [SerializeField] private PanelManager panelManager; // HeatUI PanelManager
    
    [Header("📱 게임 패널들 - Inspector에서 할당")]
    [SerializeField] private HUDPanel hudPanel;
    // [SerializeField] private PausePanel pausePanel; // 구현 예정
    // [SerializeField] private GameStartPanel gameStartPanel; // 구현 예정
    
    [Header("🎯 패널 이름 설정 (HeatUI PanelManager와 일치해야 함)")]
    [SerializeField] private string hudPanelName = "HUD";
    [SerializeField] private string pausePanelName = "Pause";
    [SerializeField] private string gameStartPanelName = "GameStart";
    
    [Header("⚙️ UI 관리 설정")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool autoStartWithHUD = true; // 게임 시작 시 자동으로 HUD 패널 표시
    
    [Header("🎯 현재 상태")]
    [SerializeField] private string currentPanel = "";
    [SerializeField] private bool isInitialized = false;
    
    #region Unity 생명주기
    
    void Awake()
    {
        InitializeUIManager();
    }
    
    void Start()
    {
        if (autoStartWithHUD)
        {
            ShowHUDPanel();
        }
        
        if (debugMode)
            Debug.Log("🎮 InGameUIManager 시작 완료");
    }
    
    #endregion
    
    #region 초기화
    
    /// <summary>
    /// UI 매니저 초기화
    /// </summary>
    void InitializeUIManager()
    {
        // PanelManager 확인
        if (panelManager == null)
        {
            panelManager = FindObjectOfType<PanelManager>();
            if (panelManager == null)
            {
                Debug.LogError("❌ HeatUI PanelManager를 찾을 수 없습니다!");
                return;
            }
        }
        
        // HUD 패널 확인
        if (hudPanel == null)
        {
            Debug.LogWarning("⚠️ HUDPanel이 할당되지 않았습니다. Inspector에서 할당해주세요.");
        }
        
        isInitialized = true;
        
        if (debugMode)
            Debug.Log("✅ UI 매니저 초기화 완료");
    }
    
    #endregion
    
    #region 패널 전환 메서드들
    
    /// <summary>
    /// HUD 패널 표시
    /// </summary>
    public void ShowHUDPanel()
    {
        if (!isInitialized) return;
        
        if (panelManager != null)
        {
            panelManager.OpenPanel(hudPanelName);
            currentPanel = hudPanelName;
            
            if (debugMode)
                Debug.Log($"🎮 HUD 패널 표시: {hudPanelName}");
        }
    }
    
    /// <summary>
    /// 일시정지 패널 표시 (구현 예정)
    /// </summary>
    public void ShowPausePanel()
    {
        if (!isInitialized) return;
        
        if (panelManager != null)
        {
            panelManager.OpenPanel(pausePanelName);
            currentPanel = pausePanelName;
            
            if (debugMode)
                Debug.Log($"⏸️ 일시정지 패널 표시: {pausePanelName}");
        }
    }
    
    /// <summary>
    /// 게임 시작 패널 표시 (구현 예정)
    /// </summary>
    public void ShowGameStartPanel()
    {
        if (!isInitialized) return;
        
        if (panelManager != null)
        {
            panelManager.OpenPanel(gameStartPanelName);
            currentPanel = gameStartPanelName;
            
            if (debugMode)
                Debug.Log($"🎯 게임 시작 패널 표시: {gameStartPanelName}");
        }
    }
    
    /// <summary>
    /// 이전 패널로 되돌리기
    /// </summary>
    public void GoToPreviousPanel()
    {
        if (panelManager != null)
        {
            panelManager.PreviousPanel();
            
            if (debugMode)
                Debug.Log("⬅️ 이전 패널로 이동");
        }
    }
    
    /// <summary>
    /// 다음 패널로 이동
    /// </summary>
    public void GoToNextPanel()
    {
        if (panelManager != null)
        {
            panelManager.NextPanel();
            
            if (debugMode)
                Debug.Log("➡️ 다음 패널로 이동");
        }
    }
    
    #endregion
    
    #region 공개 유틸리티 메서드들
    
    /// <summary>
    /// 현재 활성화된 패널 이름 반환
    /// </summary>
    public string GetCurrentPanel()
    {
        return currentPanel;
    }
    
    /// <summary>
    /// HUD 패널이 활성화되어 있는지 확인
    /// </summary>
    public bool IsHUDActive()
    {
        return currentPanel == hudPanelName && hudPanel != null && hudPanel.gameObject.activeInHierarchy;
    }
    
    /// <summary>
    /// 특정 패널이 활성화되어 있는지 확인
    /// </summary>
    public bool IsPanelActive(string panelName)
    {
        return currentPanel == panelName;
    }
    
    /// <summary>
    /// HUD 패널 참조 반환 (다른 스크립트에서 직접 접근용)
    /// </summary>
    public HUDPanel GetHUDPanel()
    {
        return hudPanel;
    }
    
    /// <summary>
    /// PanelManager 참조 반환
    /// </summary>
    public PanelManager GetPanelManager()
    {
        return panelManager;
    }
    
    /// <summary>
    /// 초기화 여부 확인
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }
    
    /// <summary>
    /// 디버그 모드 토글
    /// </summary>
    public void ToggleDebugMode()
    {
        debugMode = !debugMode;
        Debug.Log($"🔧 UI 디버그 모드: {debugMode}");
    }
    
    #endregion
    
    #region 입력 처리 (외부 스크립트에서 호출용)
    
    /// <summary>
    /// ESC 키 처리 (일시정지 패널 토글)
    /// </summary>
    public void OnEscapePressed()
    {
        if (currentPanel == hudPanelName)
        {
            ShowPausePanel();
        }
        else if (currentPanel == pausePanelName)
        {
            ShowHUDPanel();
        }
    }
    
    /// <summary>
    /// Tab 키 처리 (아이템 UI 토글)
    /// </summary>
    public void OnTabPressed()
    {
        if (hudPanel != null && IsHUDActive())
        {
            hudPanel.ToggleItemUI();
        }
    }
    
    #endregion
} 