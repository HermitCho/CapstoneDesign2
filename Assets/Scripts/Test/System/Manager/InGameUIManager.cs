using System.Collections;
using UnityEngine;
using Michsky.UI.Heat;

/// <summary>
/// 🎮 패널 매니저 기반 UI 시스템
/// HeatUI PanelManager와 연동하여 HUD, Pause, GameStart 패널 관리
/// </summary>
public class InGameUIManager : MonoBehaviour
{
    [Header("🎮 HeatUI 패널 매니저")]
    [SerializeField] private PanelManager panelManager;
    
    [Header("📱 게임 패널들")]
    [SerializeField] private HUDPanel hudPanel;
    [SerializeField] private SelectCharPanel selectCharPanel;
    
    [Header("🎯 패널 이름 설정")]
    [SerializeField] private string hudPanelName = "HUD Panel";
    [SerializeField] private string selectCharPanelName = "SelectChar Panel";
    [SerializeField] private string pausePanelName = "Pause";
    [SerializeField] private string gameStartPanelName = "GameStart";
    
    [Header("🎯 스폰 컨트롤러")]
    [SerializeField] private SpawnController spawnController;
    
    [Header("⚙️ UI 관리 설정")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool autoStartWithSelectChar = true;
    [SerializeField] private bool autoStartWithHUD = false;
    
    [Header("📊 캐릭터 프리팹 데이터")]
    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private float characterSelectionTime = 30f;
    
    [Header("🎯 현재 상태")]
    [SerializeField] private string currentPanel = "";
    [SerializeField] private bool isInitialized = false;
    [SerializeField] private int selectedCharacterIndex = -1;
    [SerializeField] private bool isCharacterSelectionPending = false;
    
    #region Unity 생명주기
    
    void Awake()
    {
        InitializeUIManager();
    }
    
    void Start()
    {
        if (autoStartWithSelectChar)
        {
            ShowSelectCharPanel();
        }
        else if (autoStartWithHUD)
        {
            ShowHUDPanel();
        }
    }
    
    #endregion
    
    #region 초기화
    
    /// <summary>
    /// UI 매니저 초기화
    /// </summary>
    void InitializeUIManager()
    {
        if (panelManager == null)
        {
            panelManager = FindObjectOfType<PanelManager>();
        }
        
        if (hudPanel == null)
        {
            Debug.LogWarning("⚠️ HUDPanel이 할당되지 않았습니다.");
        }
        
        if (selectCharPanel == null)
        {
            Debug.LogWarning("⚠️ SelectCharPanel이 할당되지 않았습니다.");
        }
        
        if (spawnController == null)
        {
            spawnController = FindObjectOfType<SpawnController>();
        }
        
        isInitialized = true;
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
    /// 캐릭터 선택 패널 표시
    /// </summary>
    public void ShowSelectCharPanel()
    {
        if (panelManager != null)
        {
            panelManager.OpenPanel(selectCharPanelName);
            currentPanel = selectCharPanelName;
        }
        
        SetSelectionMouseCursor();
    }
    
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
    /// 이전 패널로 되돌리기
    /// </summary>
    public void GoToPreviousPanel()
    {
        if (panelManager != null)
        {
            panelManager.OpenPanel(gameStartPanelName);
            currentPanel = gameStartPanelName;
        }
        
        SetMenuMouseCursor();
    }
    
    #endregion
    
    #region 마우스 커서 관리
    
    void SetGameplayMouseCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    void SetSelectionMouseCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    void SetMenuMouseCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    #endregion
    
    #region 캐릭터 선택 처리
    
    public void OnCharacterSelectionConfirmed(int characterIndex)
    {
        selectedCharacterIndex = characterIndex;
        isCharacterSelectionPending = true;
        
        StartCoroutine(WaitForSelectionTimeAndSpawn());
    }
    
    public void OnCharacterSelectionCanceled()
    {
        selectedCharacterIndex = -1;
        isCharacterSelectionPending = false;
    }
    
    IEnumerator WaitForSelectionTimeAndSpawn()
    {
        if (selectCharPanel != null)
        {
            while (selectCharPanel.IsSelectionActive())
            {
                yield return null;
            }
        }
        
        yield return new WaitForSeconds(1f);
        
        SpawnSelectedCharacter();
        ShowHUDPanel();
    }
    
    void SpawnSelectedCharacter()
    {
        if (spawnController != null && characterPrefabs != null && selectedCharacterIndex >= 0 && selectedCharacterIndex < characterPrefabs.Length)
        {
            GameObject prefabToSpawn = characterPrefabs[selectedCharacterIndex];
            spawnController.SpawnCharacterPrefab(prefabToSpawn);
        }
        
        isCharacterSelectionPending = false;
    }
    
    #endregion
    
    #region 유틸리티 메서드
    
    public PanelManager GetPanelManager()
    {
        return panelManager;
    }
    
    public bool IsInitialized()
    {
        return isInitialized;
    }
    
    public bool IsSelectCharPanelActive()
    {
        return currentPanel == selectCharPanelName && selectCharPanel != null && selectCharPanel.gameObject.activeInHierarchy;
    }
    
    public SelectCharPanel GetSelectCharPanel()
    {
        return selectCharPanel;
    }
    
    public SpawnController GetSpawnController()
    {
        return spawnController;
    }
    
    /// <summary>
    /// 현재 선택된 캐릭터 인덱스 반환
    /// </summary>
    public int GetSelectedCharacterIndex()
    {
        return selectedCharacterIndex;
    }
    
    /// <summary>
    /// 사용 가능한 캐릭터 프리팹 배열 반환
    /// </summary>
    public GameObject[] GetCharacterPrefabs()
    {
        return characterPrefabs;
    }
    
    /// <summary>
    /// 캐릭터 선택 시간 반환
    /// </summary>
    public float GetCharacterSelectionTime()
    {
        return characterSelectionTime;
    }
    
    public bool IsCharacterSelectionPending()
    {
        return isCharacterSelectionPending;
    }
    
    #endregion
} 