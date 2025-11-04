using System.Collections;
using UnityEngine;
using Michsky.UI.Heat;
using Photon.Pun;

/// <summary>
/// 🎮 패널 매니저 기반 UI 시스템
/// HeatUI PanelManager와 연동하여 HUD, Pause, GameStart 패널 관리
/// </summary>
public class InGameUIManager : MonoBehaviour
{
    [Header("HeatUI 패널 매니저")]
    [SerializeField] private PanelManager panelManager;

    [Header("GameOver 모달 창 설정")]
    [SerializeField] private ModalWindowManager gameOverModalWindowManager;
    
    [Header("패널 이름 설정")]
    [SerializeField] private string readyPanelName = "Ready";
    [SerializeField] private string hudPanelName = "HUD";
    [SerializeField] private string shopPanelName = "Shop";
    [SerializeField] private string pausePanelName = "Pause";
    [SerializeField] private string gameOverPanelName = "GameOver";


    
    [Header(" 현재 상태")]
    private string currentPanel = "";
    private bool isGameOverPanelActive = false; // GameOverPanel 활성화 상태 추적
    
    // BGMController 캐싱 (성능 최적화)
    private InGameBGMController cachedBGMController;
    
    #region Unity 생명주기
    
    void Start()
    {
        // BGMController 캐싱 (씬 시작 시 한 번만)
        CacheBGMController();
        
        // 게임 단계에 따라 적절한 패널 표시
        CheckGamePhaseAndShowPanel();
    }
    
    /// <summary>
    /// BGMController 캐싱 (성능 최적화 + 안전성)
    /// </summary>
    private void CacheBGMController()
    {
        if (cachedBGMController == null)
        {
            cachedBGMController = FindObjectOfType<InGameBGMController>();
            
            if (cachedBGMController == null)
            {
                Debug.LogWarning("InGameUIManager: InGameBGMController를 찾을 수 없습니다. BGM이 재생되지 않을 수 있습니다.");
            }
        }
    }
    
    void Update()
    {
        // Room Properties 변경 감지하여 UI 전환
        CheckGamePhaseAndShowPanel();
    }
    
    /// <summary>
    /// 게임 단계 확인 후 적절한 패널 표시 (최적화된 실무 스타일)
    /// </summary>
    private void CheckGamePhaseAndShowPanel()
    {
        // GameOverPanel이 활성화된 경우 더 이상 자동 전환하지 않음
        if (isGameOverPanelActive)
        {
            return;
        }
        
        string targetPanel = "Ready"; // 기본값
        
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gamePhase", out object phase))
        {
            string gamePhase = phase.ToString();
            
            if (gamePhase == "GAMEOVER")
            {
                // GAMEOVER 상태에서는 자동 전환 중지 (ShowGameOverPanel에서 수동 처리)
                return;
            }
            else if (gamePhase == "PLAYING")
            {
                targetPanel = "HUD";
            }
            else
            {
                targetPanel = "Ready";
            }
        }
        
        // 중복 호출 방지
        if (currentPanel != targetPanel)
        {
            currentPanel = targetPanel;
            
            if (targetPanel == "HUD")
            {
                ShowHUDPanel();
            }
            else
            {
                ShowReadyPanel();
            }
        }
    }
    
    // OnGameActuallyStarted 메서드 제거 - Room Properties 기반으로 변경
    
    #endregion

    
    #region 패널 전환
    
    /// <summary>
    /// Ready 패널 표시
    /// </summary>
    public void ShowReadyPanel()
    {
        if (panelManager != null)
        {
            panelManager.OpenPanel(readyPanelName);
            currentPanel = readyPanelName;
        }
        
        SetMenuMouseCursor();
    }
    
    /// <summary>
    /// HUD 패널 표시
    /// </summary>
    public void ShowHUDPanel()
    {
        if (panelManager != null)
        {
            panelManager.OpenPanel(hudPanelName);
            currentPanel = hudPanelName;
        }
        
        SetGameplayMouseCursor();
    }
    
    /// <summary>
    /// 일시정지 패널 표시
    /// </summary>
    public void ShowPausePanel()
    {
        if (panelManager != null)
        {
            panelManager.OpenPanel(pausePanelName);
            currentPanel = pausePanelName;
        }
        
        SetMenuMouseCursor();
    }
    
    /// <summary>
    /// 상점 패널 표시
    /// </summary>
    public void ShowShopPanel()
    {
        if (panelManager != null)
        {
            panelManager.OpenPanel(shopPanelName);
            currentPanel = shopPanelName;
        }
        
        SetMenuMouseCursor();
    }
    /// <summary>
    /// 게임 오버 패널 표시 (점수 포함)
    /// </summary>
    public void ShowGameOverPanel(float finalScore)
    {
        if (panelManager != null && gameOverModalWindowManager != null)
        {
            // GameOverPanel 활성화 상태 설정 (자동 전환 방지)
            isGameOverPanelActive = true;
            
            SetMenuMouseCursor();
            
            // ✅ 인게임 BGM 중지 (캐싱된 참조 사용 - 성능 최적화)
            if (cachedBGMController == null)
            {
                // 캐싱 안되어 있으면 찾기 시도
                CacheBGMController();
            }
            
            if (cachedBGMController != null)
            {
                cachedBGMController.StopInGameBGM();
            }
            else
            {
                // BGMController가 없어도 AudioManager로 직접 중지 (백업)
                if (AudioManager.Inst != null)
                {
                    AudioManager.Inst.StopBGMLoop();
                }
            }
            
            // ✅ STOP 모달 창 열 때 START 사운드 재생
            if (AudioManager.Inst != null)
            {
                AudioManager.Inst.PlayOneShot("SFX_UI_Ready_Start");
                AudioManager.Inst.PlayOneShot("SFX_UI_GameOver_FinishVoice");
            }
            
            gameOverModalWindowManager.OpenWindow();

            StartCoroutine(ShowGameOverPanelCoroutine(3f));
        }
    }

    IEnumerator ShowGameOverPanelCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameOverModalWindowManager.CloseWindow();
        panelManager.OpenPanel(gameOverPanelName);
        currentPanel = gameOverPanelName;
        
        Debug.Log("InGameUIManager: GameOverPanel 활성화 완료 - 자동 전환 차단됨");
        
        // GameOverPanel 활성화 후 SetWinnerPlayer 호출
        yield return new WaitForSeconds(0.1f); // 패널 완전 활성화 대기
        
        GameOverController gameOverController = FindObjectOfType<GameOverController>();
        if (gameOverController != null)
        {
            gameOverController.SetWinnerPlayer();
            Debug.Log("InGameUIManager: GameOverController.SetWinnerPlayer() 호출 완료");
        }
        else
        {
            Debug.LogError("InGameUIManager: GameOverController를 찾을 수 없습니다!");
        }
    }
    
    #endregion
    
    #region 마우스 커서 관리
    
    public void SetGameplayMouseCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    public void SetMenuMouseCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    #endregion
    
    
    #region 유틸리티 메서드
    
    public PanelManager GetPanelManager()
    {
        return panelManager;
    }
    
    /// <summary>
    /// 게임 상태 리셋 (새 게임 시작 시 호출)
    /// </summary>
    public void ResetGameState()
    {
        isGameOverPanelActive = false;
        currentPanel = "";
        cachedBGMController = null; // 캐시 초기화 (새 씬에서 다시 찾기)
        Debug.Log("InGameUIManager: 게임 상태 리셋 - 자동 전환 재활성화");
    }

    
    #endregion
} 