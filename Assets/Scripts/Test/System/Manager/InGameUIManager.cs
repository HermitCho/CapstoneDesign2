using System.Collections;
using UnityEngine;
using Michsky.UI.Heat;

/// <summary>
/// 🎮 패널 매니저 기반 UI 시스템
/// HeatUI PanelManager와 연동하여 HUD, Pause, GameStart 패널 관리
/// </summary>
public class InGameUIManager : MonoBehaviour
{
    [Header("HeatUI 패널 매니저")]
    [SerializeField] private PanelManager panelManager;

    
    [Header("패널 이름 설정")]
    [SerializeField] private string hudPanelName = "HUD";
    [SerializeField] private string shopPanelName = "Shop";
    [SerializeField] private string pausePanelName = "Pause";
    [SerializeField] private string gameOverPanelName = "GameOver";
    
    [Header(" 현재 상태")]
    private string currentPanel = "";
    
    #region Unity 생명주기
    
    void Start()
    {
        ShowHUDPanel();
    }
    
    #endregion

    
    #region 패널 전환
    
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
        if (panelManager != null)
        {
            panelManager.OpenPanel(gameOverPanelName);
            currentPanel = gameOverPanelName;
            SetMenuMouseCursor();
            Debug.Log($"✅ InGameUIManager: 게임 오버 패널 표시 - 최종 점수: {finalScore}");
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

    
    #endregion
} 