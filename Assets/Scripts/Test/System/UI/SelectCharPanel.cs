using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.Heat;

/// <summary>
/// 🎮 캐릭터 선택 패널
/// HeatUI를 이용한 캐릭터 선택 시스템
/// </summary>
public class SelectCharPanel : MonoBehaviour
{

    
    [Header("⏰ 시간 표시 컴포넌트들")]
    [SerializeField] private TextMeshProUGUI TimeText;

    
    [Header("📋 캐릭터 정보 표시")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI characterDescriptionText;
    [SerializeField] private Image characterPreviewImage;
    [SerializeField] private ProgressBar characterStatsHealth;
    [SerializeField] private ProgressBar characterStatsSpeed;
    [SerializeField] private ProgressBar characterStatsAttack;
    

    
    [Header("📊 캐릭터 프리팹 데이터")]
    [SerializeField] private GameObject[] characterPrefabs;
    
    [Header("🎮 매니저 연결")]
    [SerializeField] private InGameUIManager uiManager;
    
    [Header("📊 캐릭터별 스탯 설정")]
    [SerializeField] private float[] characterHealthStats = {80f, 60f, 100f, 70f};
    [SerializeField] private float[] characterSpeedStats = {70f, 90f, 50f, 80f};
    [SerializeField] private float[] characterAttackStats = {60f, 70f, 90f, 85f};
    
    // 내부 상태 변수들
    private float remainingTime;
    private bool isSelectionActive = true;
    private bool isInitialized = false;
    private int previousSelectedIndex = -1;
    private bool[] characterUnlocked;
    

    // 데이터베이스 참조
    private DataBase.UIData uiData;

    // ✅ DataBase 캐싱된 값들 (성능 최적화)
    private int cachedMaxCharacterSlots;
    private int cachedCurrentSelectedIndex;
    private float cachedSelectionTime;
    private string cachedSelectionTimeText;
    private float cachedSelectionWarningTime;
    private float cachedSelectionDangerTime;
    private Color cachedSelectionTimeNormalFormatColor;
    private Color cachedSelectionTimeWarningFormatColor;
    private Color cachedSelectionTimeDangerFormatColor;
    private bool dataBaseCached = false;


    #region Unity 생명주기
    
    void Awake()
    {
        InitializePanel();
    }

    void OnEnable()
    {
        CacheDataBaseInfo();
    }
    
    void Start()
    {
        SetInitialState();
    }
    
    void Update()
    {
        // 선택 시간 업데이트
        UpdateSelectionTime();
        
        // 실시간 UI 업데이트
        UpdateRealTimeUI();
    }
    
    #endregion
    
    #region 초기화

    /// <summary>
    /// DataBase 정보 안전하게 캐싱 (GameManager와 동일한 방식)
    /// </summary>
    void CacheDataBaseInfo()
    {
        try
        {   
            if (DataBase.Instance == null)
            {
               Debug.LogWarning("DataBase 인스턴스가 없습니다.");
              return;
            }

            if (DataBase.Instance.uiData != null)
            {
                uiData = DataBase.Instance.uiData;

                cachedMaxCharacterSlots = uiData.MaxCharacterSlots;
                cachedCurrentSelectedIndex = uiData.CurrentSelectedIndex;
                cachedSelectionTime = uiData.SelectionTime;
                cachedSelectionTimeText = uiData.SelectionTimeText;
                cachedSelectionWarningTime = uiData.SelectionWarningTime;
                cachedSelectionDangerTime = uiData.SelectionDangerTime;
                cachedSelectionTimeNormalFormatColor = uiData.SelectionTimeNormalFormatColor;
                cachedSelectionTimeWarningFormatColor = uiData.SelectionTimeWarningFormatColor;
                cachedSelectionTimeDangerFormatColor = uiData.SelectionTimeDangerFormatColor;

                dataBaseCached = true;
                Debug.Log("✅ SelectCharPanel - DataBase 정보 캐싱 완료");
            }
            else
            {
                Debug.LogWarning("⚠️ SelectCharPanel - DataBase 접근 실패, 기본값 사용");
                dataBaseCached = false;
            }   
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ SelectCharPanel - DataBase 캐싱 중 오류: {e.Message}");
            dataBaseCached = false;
        }
    }
    
    /// <summary>
    /// 패널 초기화
    /// </summary>
    void InitializePanel()
    {
        CacheDataBaseInfo();
        // UI 매니저 찾기
        FindUIManager();
        
        // UI 매니저에서 캐릭터 데이터 가져오기
        LoadCharacterDataFromUIManager();
        
        // 시간 초기화
        LoadSelectionTimeFromUIManager();
        
        // 캐릭터 언락 상태 초기화
        InitializeCharacterUnlockStatus();
        
        // 초기 선택 설정
        SetCharacterSelection(cachedCurrentSelectedIndex);
    }
    

    
    /// <summary>
    /// 캐릭터 언락 상태 초기화
    /// </summary>
    void InitializeCharacterUnlockStatus()
    {
        characterUnlocked = new bool[cachedMaxCharacterSlots];
        for (int i = 0; i < cachedMaxCharacterSlots; i++)
        {
            characterUnlocked[i] = true;
        }
    }
    
    /// <summary>
    /// UI 매니저 찾기
    /// </summary>
    void FindUIManager()
    {
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<InGameUIManager>();
        }
    }
    
    /// <summary>
    /// UI 매니저에서 캐릭터 프리팹 로드
    /// </summary>
    void LoadCharacterDataFromUIManager()
    {
        if (uiManager != null)
        {
            GameObject[] managerPrefabs = uiManager.GetCharacterPrefabs();
            if (managerPrefabs != null && managerPrefabs.Length > 0)
            {
                characterPrefabs = managerPrefabs;
                cachedMaxCharacterSlots = characterPrefabs.Length;
            }
        }
    }
    
    /// <summary>
    /// UI 매니저에서 선택 시간 로드
    /// </summary>
    void LoadSelectionTimeFromUIManager()
    {
        if (uiManager != null)
        {
            float managerSelectionTime = uiManager.GetCharacterSelectionTime();
            if (managerSelectionTime > 0)
            {
                cachedSelectionTime = managerSelectionTime;
            }
        }
        remainingTime = cachedSelectionTime;
    }
    
    /// <summary>
    /// 초기 상태 설정
    /// </summary>
    void SetInitialState()
    {
        // 첫 번째 캐릭터 선택
        SetCharacterSelection(0);
        
        // 시간 표시 시작
        UpdateTimeDisplay();
        
        // 패널 활성화
        SetPanelVisible(true);
        isInitialized = true;
    }
    
    #endregion
    
    #region 캐릭터 선택 UI
    
    /// <summary>
    /// 캐릭터 버튼 클릭 처리
    /// </summary>
    public void OnCharacterButtonClick(int characterIndex)
    {
        if (!isSelectionActive || !IsCharacterUnlocked(characterIndex)) return;
        
        SetCharacterSelection(characterIndex);
        
        // InGameUIManager에 선택된 캐릭터 인덱스 전달
        if (uiManager != null)
        {
            uiManager.OnCharacterSelectionConfirmed(characterIndex);
        }
    }
    
    /// <summary>
    /// 캐릭터 선택 설정
    /// </summary>
    public void SetCharacterSelection(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= cachedMaxCharacterSlots) return;
        
        // 새 선택 적용
        cachedCurrentSelectedIndex = characterIndex;
        previousSelectedIndex = characterIndex;
        
        // 캐릭터 정보 표시 업데이트
        UpdateCharacterInfo(characterIndex);
    }
    

    
    /// <summary>
    /// 캐릭터 정보 업데이트
    /// </summary>
    void UpdateCharacterInfo(int characterIndex)
    {
        if (characterPrefabs == null || characterIndex >= characterPrefabs.Length) return;
        
        GameObject prefab = characterPrefabs[characterIndex];
        if (prefab == null) return;
        
        if (characterNameText != null)
            characterNameText.text = $"캐릭터 {characterIndex + 1}";
        
        if (characterDescriptionText != null)
            characterDescriptionText.text = $"캐릭터 {characterIndex + 1}의 설명입니다.";
        
        if (characterPreviewImage != null)
            characterPreviewImage.sprite = null;
        
        UpdateCharacterStatsForIndex(characterIndex);
    }
    

    

    

    
    /// <summary>
    /// 특정 캐릭터 인덱스의 스탯 표시
    /// </summary>
    void UpdateCharacterStatsForIndex(int characterIndex)
    {
        if (characterStatsHealth != null)
        {
            float healthValue = GetStatValue(characterHealthStats, characterIndex, 50f);
            characterStatsHealth.currentValue = healthValue;
            characterStatsHealth.maxValue = 100f;
            characterStatsHealth.UpdateUI();
        }
        
        if (characterStatsSpeed != null)
        {
            float speedValue = GetStatValue(characterSpeedStats, characterIndex, 50f);
            characterStatsSpeed.currentValue = speedValue;
            characterStatsSpeed.maxValue = 100f;
            characterStatsSpeed.UpdateUI();
        }
        
        if (characterStatsAttack != null)
        {
            float attackValue = GetStatValue(characterAttackStats, characterIndex, 50f);
            characterStatsAttack.currentValue = attackValue;
            characterStatsAttack.maxValue = 100f;
            characterStatsAttack.UpdateUI();
        }
    }
    
    /// <summary>
    /// 배열에서 안전하게 스탯 값 가져오기
    /// </summary>
    float GetStatValue(float[] statArray, int index, float defaultValue)
    {
        if (statArray != null && index >= 0 && index < statArray.Length)
            return statArray[index];
        return defaultValue;
    }
    
    #endregion
    
    #region 시간 관리
    
    void UpdateSelectionTime()
    {
        if (!isSelectionActive) return;
        
        remainingTime -= Time.deltaTime;
        
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            OnTimeExpired();
        }
    }
    
    void UpdateTimeDisplay()
    {
        if (TimeText == null) return;
        
        TimeText.text = string.Format(cachedSelectionTimeText, remainingTime);
        
        if (remainingTime <= cachedSelectionDangerTime)
            TimeText.color = cachedSelectionTimeDangerFormatColor;
        else if (remainingTime <= cachedSelectionWarningTime)
            TimeText.color = cachedSelectionTimeWarningFormatColor;
        else
            TimeText.color = cachedSelectionTimeNormalFormatColor;
    }
    
    void OnTimeExpired()
    {
        isSelectionActive = false;
        
        if (uiManager != null)
        {
            uiManager.OnCharacterSelectionConfirmed(cachedCurrentSelectedIndex);
        }
    }
    
    #endregion
    
    #region 버튼 이벤트 처리
    
    public void OnCancelButtonClick()
    {
        CancelCharacterSelection();
    }
    
    public void OnRandomSelectButtonClick()
    {
        if (!isSelectionActive) return;
        
        List<int> unlockedIndices = new List<int>();
        for (int i = 0; i < cachedMaxCharacterSlots; i++)
        {
            if (IsCharacterUnlocked(i))
                unlockedIndices.Add(i);
        }
        
        if (unlockedIndices.Count > 0)
        {
            int randomIndex = unlockedIndices[Random.Range(0, unlockedIndices.Count)];
            SetCharacterSelection(randomIndex);
            
            if (uiManager != null)
            {
                uiManager.OnCharacterSelectionConfirmed(randomIndex);
            }
        }
    }
    
    #endregion
    
    #region 실시간 업데이트
    
    /// <summary>
    /// 실시간 UI 업데이트
    /// </summary>
    void UpdateRealTimeUI()
    {
        // 시간 표시 업데이트
        UpdateTimeDisplay();
    }
    
    #endregion
    
    #region 공개 메서드들
    
    /// <summary>
    /// 캐릭터 선택 (Inspector 이벤트용 - 매개변수 전달)
    /// </summary>
    /// <param name="characterIndex">선택할 캐릭터 인덱스 (0~3)</param>
    public void SelectCharacter(int characterIndex)
    {
        OnCharacterButtonClick(characterIndex);
    }
    
    public void CancelCharacterSelection()
    {
        isSelectionActive = false;
        SetMouseCursor(false);
        
        if (uiManager != null)
        {
            uiManager.OnCharacterSelectionCanceled();
        }
    }
    
    /// <summary>
    /// 캐릭터 언락 상태 확인
    /// </summary>
    public bool IsCharacterUnlocked(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= characterUnlocked.Length) return false;
        return characterUnlocked[characterIndex];
    }
    
    /// <summary>
    /// 캐릭터 언락/잠금 설정
    /// </summary>
    public void SetCharacterUnlocked(int characterIndex, bool unlocked)
    {
        if (characterIndex < 0 || characterIndex >= characterUnlocked.Length) return;
        characterUnlocked[characterIndex] = unlocked;
    }
    
    /// <summary>
    /// 패널 표시/숨김
    /// </summary>
    public void SetPanelVisible(bool visible)
    {
        gameObject.SetActive(visible);
        
        if (visible)
        {
            remainingTime = cachedSelectionTime;
            isSelectionActive = true;
            SetMouseCursor(true);
        }
        else
        {
            isSelectionActive = false;
            SetMouseCursor(false);
        }
    }
    
    void SetMouseCursor(bool visible)
    {
        if (visible)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    
    public int GetSelectedCharacterIndex() => cachedCurrentSelectedIndex;
    
    /// <summary>
    /// 남은 선택 시간
    /// </summary>
    public float GetRemainingTime() => remainingTime;
    
    /// <summary>
    /// 선택 활성 상태
    /// </summary>
    public bool IsSelectionActive() => isSelectionActive;
    
    #endregion
}

