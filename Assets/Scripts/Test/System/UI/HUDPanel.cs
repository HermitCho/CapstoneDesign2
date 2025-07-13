using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.Heat;

/// <summary>
/// 🎮 통합 HUD 패널
/// CrosshairUI, HealthUI, ScoreUI, SkillAndItemUI를 모두 포함하는 메인 HUD
/// </summary>
public class HUDPanel : MonoBehaviour
{
    [Header("🎯 크로스헤어 UI 컴포넌트들")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private RectTransform crosshairContainer;

    
    [Header("❤️ 체력바 UI 컴포넌트들")]
    [SerializeField] private ProgressBar healthProgressBar; // HeatUI ProgressBar
    [SerializeField] private TextMeshProUGUI healthText;

    
    [Header("📊 점수 UI 컴포넌트들")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI multiplierText;
    [SerializeField] private TextMeshProUGUI gameTimeText;
    [SerializeField] private TextMeshProUGUI attachStatusText;
    [SerializeField] private TextMeshProUGUI scoreStatusText;
    [SerializeField] private Image statusIcon;
    
    [Header("⚔️ 스킬 UI 컴포넌트들")]
    [SerializeField] private Button[] skillButtons;
    [SerializeField] private Image[] skillIcons;
    [SerializeField] private Image[] skillCooldownOverlays;
    [SerializeField] private TextMeshProUGUI[] skillCooldownTexts;
    [SerializeField] private int maxSkillSlots = 4;
    
    [Header("📦 아이템 UI 컴포넌트들")]
    [SerializeField] private ModalWindowManager itemModalWindow; // HeatUI Modal
    [SerializeField] private Button itemUIButton; // 아이템 UI 열기 버튼 (선택적)
    
    // 내부 상태 변수들
    private float currentHealth = 100f;
    private float maxHealth = 100f;
    private bool isTargeting = false;
    private bool isItemUIOpen = false;
    private float playTime = 360f;
    private float[] skillCooldowns;
    private float[] maxCooldowns;
    private bool[] skillAvailable;


    // 데이터베이스 참조
    private DataBase.UIData uiData;

    // ✅ DataBase 캐싱된 값들 (성능 최적화)
    private Color cachedCrosshairNormalColor;
    private Color cachedCrosshairTargetColor;
    private float cachedCrosshairSize;

    private Color cachedHealthNormalColor;
    private Color cachedHealthWarningColor;
    private Color cachedHealthDangerColor;
    private float cachedHealthWarningThreshold;
    private float cachedHealthDangerThreshold;

    private string cachedScoreFormat;
    private Color cachedScoreFormatColor;

    private string cachedGeneralMultiplierFormat;
    private Color cachedGeneralMultiplierFormatColor;

    private string cachedMultiplierFormat;
    private Color cachedMultiplierFormatColor;

    private string cachedGameTimeFormat;
    private Color cachedGameTimeFormatColor;

    private string cachedHealthFormat;
    private Color cachedHealthFormatColor;

    private bool dataBaseCached = false;
    #region Unity 생명주기
    void Awake()
    {
        InitializeHUD();
    }
    
    void OnEnable()
    {
       CacheDataBaseInfo();
    }
    
    void OnDisable()
    {
        // 패널이 비활성화될 때 정리 작업
    }
    
    void Start()
    {
        SubscribeToEvents();
        SetInitialState();


    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    void Update()
    {
        // 스킬 쿨타임 업데이트
        UpdateSkillCooldowns();
        
        // 실시간 점수 상태 업데이트
        UpdateRealTimeScoreStatus();
        
        // 실시간 게임 시간 업데이트
        UpdateRealTimeUI();
        
        // 시간대별 배율 UI 실시간 업데이트
        if (GameManager.Instance != null)
        {
            UpdateMultiplier(GameManager.Instance.GetScoreMultiplier());
        }
    }
    
    #endregion
    
    #region 초기화
    
    /// <summary>
    /// HUD 초기화
    /// </summary>
    void InitializeHUD()
    {
        CacheDataBaseInfo();

        // 스킬 시스템 초기화
        InitializeSkillSystem();
        
        
        // 초기값 설정
        SetHealth(100f, 100f);
        SetPlayTime();
        SetCrosshairTargeting(false);
        UpdateScore(0f);
        UpdateMultiplier(1f);
        UpdateGameTime(0f);
        UpdateAttachStatus(false, 0f);

    }
    
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

                cachedCrosshairNormalColor = uiData.CrosshairNormalColor;
                cachedCrosshairTargetColor = uiData.CrosshairTargetColor;
                cachedCrosshairSize = uiData.CrosshairSize;

                cachedHealthNormalColor = uiData.HealthyColor;
                cachedHealthWarningColor = uiData.WarningColor;
                cachedHealthDangerColor = uiData.DangerColor;
                cachedHealthWarningThreshold = uiData.WaringThreshold;
                cachedHealthDangerThreshold = uiData.DangerThreshold;       

                cachedScoreFormat = uiData.ScoreText;
                cachedScoreFormatColor = uiData.ScoreFormatColor;

                cachedGeneralMultiplierFormat = uiData.GeneralMultiplierText;
                cachedGeneralMultiplierFormatColor = uiData.GeneralMultiplierFormatColor;

                cachedMultiplierFormat = uiData.MultiplierText; 
                cachedMultiplierFormatColor = uiData.MultiplierFormatColor;

                cachedGameTimeFormat = uiData.GameTimeText;
                cachedGameTimeFormatColor = uiData.GameTimeFormatColor;

                cachedHealthFormat = uiData.HealthText;
                cachedHealthFormatColor = uiData.HealthFormatColor; 

                dataBaseCached = true;
                Debug.Log("✅ HUDPanel - DataBase 정보 캐싱 완료");
            }
            else
            {
                Debug.LogWarning("⚠️ HUDPanel - DataBase 접근 실패, 기본값 사용");
                dataBaseCached = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ HUDPanel - DataBase 캐싱 중 오류: {e.Message}");
            dataBaseCached = false;
        }
    }


    /// <summary>
    /// 스킬 시스템 초기화
    /// </summary>
    void InitializeSkillSystem()
    {
        skillCooldowns = new float[maxSkillSlots];
        maxCooldowns = new float[maxSkillSlots];
        skillAvailable = new bool[maxSkillSlots];
        
        for (int i = 0; i < maxSkillSlots; i++)
        {
            skillCooldowns[i] = 0f;
            maxCooldowns[i] = 10f;
            skillAvailable[i] = true;
        }
        
        UpdateAllSkillUI();
    }
    
    /// <summary>
    /// 초기 상태 설정
    /// </summary>
    void SetInitialState()
    {
        // 크로스헤어 표시
        ShowCrosshair(true);
        
        // 아이템 UI 닫힌 상태로 시작
        CloseItemUI();
        

    }
    void SetPlayTime()
    {
        playTime = GameManager.Instance.GetPlayTime();
    }
    
    #endregion
    
    #region 이벤트 구독/해제
    
    void SubscribeToEvents()
    {
        if (GameManager.Instance != null)
        {
            // 점수 관련 이벤트
            GameManager.OnScoreUpdated += UpdateScore;
            GameManager.OnScoreMultiplierUpdated += UpdateMultiplier;
            GameManager.OnGameTimeUpdated += UpdateGameTime;
            
            // 테디베어 관련 이벤트
            GameManager.OnTeddyBearAttachmentChanged += OnTeddyBearAttachmentChanged;
            GameManager.OnTeddyBearReattachTimeChanged += OnTeddyBearReattachTimeChanged;
            
            // 플레이어 체력 이벤트
            GameManager.OnPlayerHealthChanged += OnPlayerHealthChanged;
            
            // 크로스헤어 이벤트
            GameManager.OnCrosshairTargetingChanged += SetCrosshairTargeting;
            
            // 스킬 이벤트
            GameManager.OnSkillUsed += UseSkill;
            GameManager.OnSkillCooldownStarted += SetSkillCooldown;
        }
        
        // InputManager 이벤트
        InputManager.OnItemUIPressed += OpenItemUI;
        InputManager.OnItemUICanceledPressed += CloseItemUI;

    }
    
    void UnsubscribeFromEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.OnScoreUpdated -= UpdateScore;
            GameManager.OnScoreMultiplierUpdated -= UpdateMultiplier;
            GameManager.OnGameTimeUpdated -= UpdateGameTime;
            GameManager.OnTeddyBearAttachmentChanged -= OnTeddyBearAttachmentChanged;
            GameManager.OnTeddyBearReattachTimeChanged -= OnTeddyBearReattachTimeChanged;
            GameManager.OnPlayerHealthChanged -= OnPlayerHealthChanged;
            GameManager.OnCrosshairTargetingChanged -= SetCrosshairTargeting;
            GameManager.OnSkillUsed -= UseSkill;
            GameManager.OnSkillCooldownStarted -= SetSkillCooldown;
        }
        
        InputManager.OnItemUIPressed -= OpenItemUI;
        InputManager.OnItemUICanceledPressed -= CloseItemUI;
    }
    
    #endregion
    
    #region 크로스헤어 UI
    
    /// <summary>
    /// 크로스헤어 표시/숨김
    /// </summary>
    public void ShowCrosshair(bool show)
    {
        if (crosshairImage != null)
        {
            crosshairImage.gameObject.SetActive(show);
        }
    }
    
    /// <summary>
    /// 크로스헤어 타겟팅 상태 설정
    /// </summary>
    public void SetCrosshairTargeting(bool targeting)
    {
        isTargeting = targeting;
        
        if (crosshairImage != null)
        {
            crosshairImage.color = targeting ? cachedCrosshairTargetColor : cachedCrosshairNormalColor;
        }
    }
    
    /// <summary>
    /// 크로스헤어 크기 설정 (줌 애니메이션용)
    /// </summary>
    public void SetCrosshairSize(float size)
    {
        cachedCrosshairSize = Mathf.Clamp(size, 0.1f, 3f);
        
        if (crosshairContainer != null)
        {
            crosshairContainer.localScale = Vector3.one * cachedCrosshairSize;
        }
    }
    
    #endregion
    
    #region 체력바 UI
    
    /// <summary>
    /// 체력 설정
    /// </summary>
    public void SetHealth(float current, float max)
    {
        currentHealth = Mathf.Clamp(current, 0f, max);
        maxHealth = Mathf.Max(max, 1f);
        
        UpdateHealthDisplay();
    }
    
    /// <summary>
    /// 체력 표시 업데이트
    /// </summary>
    void UpdateHealthDisplay()
    {
        float healthRatio = currentHealth / maxHealth;
        
        // HeatUI ProgressBar 업데이트
        if (healthProgressBar != null)
        {
            healthProgressBar.currentValue = currentHealth;
            healthProgressBar.maxValue = maxHealth;
            healthProgressBar.UpdateUI(); // 반드시 호출!
        }
        
        // 체력 텍스트 업데이트
        if (healthText != null)
        {
            healthText.text = string.Format(cachedHealthFormat, currentHealth, maxHealth);
            
            // 체력 비율에 따른 색상 변경
            if (healthRatio <= cachedHealthDangerThreshold)
                healthText.color = cachedHealthDangerColor;
            else if (healthRatio <= cachedHealthWarningThreshold)
                healthText.color = cachedHealthWarningColor;
            else
                healthText.color = cachedHealthNormalColor;
        }
    }
    
    void OnPlayerHealthChanged(float current, float max)
    {
        SetHealth(current, max);
    }
    
    #endregion
    
    #region 점수, 시간, 배율 UI
    
    /// <summary>
    /// 점수 업데이트
    /// </summary>
    public void UpdateScore(float score)
    {
        if (scoreText != null)
        {
            scoreText.text = string.Format(cachedScoreFormat, score);
            scoreText.color = cachedScoreFormatColor;
        }
    }
    
    /// <summary>
    /// 배율 업데이트 (시간대별 포맷 적용)
    /// </summary>
    public void UpdateMultiplier(float multiplier)
    {
        if (multiplierText == null) return;
        
        // 시간대에 따른 포맷 선택 - GameManager 기반 안전한 접근
        try
        {
            // GameManager 존재 여부만 체크
            bool hasGameManager = GameManager.Instance != null;
            
            if (hasGameManager)
            {
                float gameTime = GameManager.Instance.GetGameTime();
                float scoreIncreaseTime = GameManager.Instance.GetScoreIncreaseTime();
                bool dataBaseCached = GameManager.Instance.IsDataBaseCached();
                
                if (gameTime >= scoreIncreaseTime)
                {
                    // 점수배율 적용 시점 이후: multiplierFormat 사용
                    multiplierText.color = multiplier > 1f ? cachedMultiplierFormatColor : cachedGeneralMultiplierFormatColor;
                    multiplierText.text = string.Format(cachedMultiplierFormat, multiplier);
                }
                else
                {
                    multiplierText.color = cachedGeneralMultiplierFormatColor;
                    // 점수배율 적용 전: GeneralMultiplierFormat 사용
                    multiplierText.text = string.Format(cachedGeneralMultiplierFormat, multiplier);
                   
                }
            }
            else
            {
                multiplierText.color = cachedGeneralMultiplierFormatColor;
                
                // GameManager가 없는 경우
                multiplierText.text = string.Format(cachedGeneralMultiplierFormat, multiplier);
                
            }
        }
        catch (System.Exception e)
        {
            multiplierText.color = cachedGeneralMultiplierFormatColor;
            // 안전한 fallback
            multiplierText.text = string.Format(cachedGeneralMultiplierFormat, multiplier);
            
        }
    }
    
    /// <summary>
    /// 게임 시간 업데이트
    /// </summary>
    public void UpdateGameTime(float time)
    {
        if (gameTimeText != null)
        {
            gameTimeText.text = string.Format(cachedGameTimeFormat, time);
            gameTimeText.color = cachedGameTimeFormatColor;
        }
    }
    
    /// <summary>
    /// 테디베어 부착 상태 업데이트
    /// </summary>
    public void UpdateAttachStatus(bool isAttached, float timeUntilReattach = 0f)
    {
        if (attachStatusText == null) return;
        
        if (isAttached)
        {
            attachStatusText.text = "테디베어 부착됨";
            attachStatusText.color = Color.green;
            if (statusIcon != null) statusIcon.color = Color.green;
        }
        else if (timeUntilReattach > 0f)
        {
            attachStatusText.text = $"재부착까지 {timeUntilReattach:F1}초";
            attachStatusText.color = Color.yellow;
            if (statusIcon != null) statusIcon.color = Color.yellow;
        }
        else
        {
            attachStatusText.text = "테디베어 미부착";
            attachStatusText.color = Color.red;
            if (statusIcon != null) statusIcon.color = Color.red;
        }
    }
    
    /// <summary>
    /// 점수 상태 업데이트
    /// </summary>
    public void UpdateScoreStatus(string status, float timeRemaining)
    {
        if (scoreStatusText == null) return;
        
        if (timeRemaining > 0f)
        {
            scoreStatusText.text = $"{status} - 증가까지 {timeRemaining:F1}초";
            scoreStatusText.color = Color.white;
        }
        else
        {
            scoreStatusText.text = $"{status} (활성화됨)";
            scoreStatusText.color = Color.yellow;
        }
    }
    
    void OnTeddyBearAttachmentChanged(bool isAttached)
    {
        UpdateAttachStatus(isAttached, 0f);
    }
    
    void OnTeddyBearReattachTimeChanged(float timeRemaining)
    {
        if (!GameManager.Instance.IsTeddyBearAttached())
        {
            UpdateAttachStatus(false, timeRemaining);
        }
    }
    
    #endregion
    
    #region 스킬 UI
    
    /// <summary>
    /// 스킬 사용
    /// </summary>
    public void UseSkill(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= maxSkillSlots) return;
        
        if (skillAvailable[skillIndex] && skillCooldowns[skillIndex] <= 0f)
        {
            skillCooldowns[skillIndex] = maxCooldowns[skillIndex];
            UpdateSkillUI(skillIndex);
        }
    }
    
    /// <summary>
    /// 스킬 쿨다운 설정
    /// </summary>
    public void SetSkillCooldown(int skillIndex, float cooldownTime)
    {
        if (skillIndex < 0 || skillIndex >= maxSkillSlots) return;
        
        maxCooldowns[skillIndex] = cooldownTime;
    }
    
    /// <summary>
    /// 특정 스킬 UI 업데이트
    /// </summary>
    void UpdateSkillUI(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= maxSkillSlots) return;
        
        bool isOnCooldown = skillCooldowns[skillIndex] > 0f;
        bool isAvailable = skillAvailable[skillIndex];
        
        // 버튼 상태 업데이트
        if (skillIndex < skillButtons.Length && skillButtons[skillIndex] != null)
        {
            skillButtons[skillIndex].interactable = isAvailable && !isOnCooldown;
        }
        
        // 아이콘 색상 업데이트
        if (skillIndex < skillIcons.Length && skillIcons[skillIndex] != null)
        {
            skillIcons[skillIndex].color = isOnCooldown ? Color.gray : Color.white;
        }
        
        // 쿨다운 오버레이 업데이트
        if (skillIndex < skillCooldownOverlays.Length && skillCooldownOverlays[skillIndex] != null)
        {
            if (isOnCooldown)
            {
                float fillAmount = skillCooldowns[skillIndex] / maxCooldowns[skillIndex];
                skillCooldownOverlays[skillIndex].fillAmount = fillAmount;
                skillCooldownOverlays[skillIndex].gameObject.SetActive(true);
            }
            else
            {
                skillCooldownOverlays[skillIndex].gameObject.SetActive(false);
            }
        }
        
        // 쿨다운 텍스트 업데이트
        if (skillIndex < skillCooldownTexts.Length && skillCooldownTexts[skillIndex] != null)
        {
            if (isOnCooldown)
            {
                skillCooldownTexts[skillIndex].text = skillCooldowns[skillIndex].ToString("F1");
                skillCooldownTexts[skillIndex].gameObject.SetActive(true);
            }
            else
            {
                skillCooldownTexts[skillIndex].gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 모든 스킬 UI 업데이트
    /// </summary>
    void UpdateAllSkillUI()
    {
        for (int i = 0; i < maxSkillSlots; i++)
        {
            UpdateSkillUI(i);
        }
    }
    
    /// <summary>
    /// 스킬 쿨타임 실시간 업데이트
    /// </summary>
    public void UpdateSkillCooldowns()
    {
        for (int i = 0; i < maxSkillSlots; i++)
        {
            if (skillCooldowns[i] > 0f)
            {
                skillCooldowns[i] -= Time.deltaTime;
                
                if (skillCooldowns[i] <= 0f)
                {
                    skillCooldowns[i] = 0f;
                }
                
                UpdateSkillUI(i);
            }
        }
    }
    
    #endregion
    
    #region 아이템 UI (모달창)
    
    /// <summary>
    /// 아이템 UI 열기
    /// </summary>
    public void OpenItemUI()
    {
        if (itemModalWindow == null) return;
        
        if (!itemModalWindow.isOn)
        {
            isItemUIOpen = true;
            itemModalWindow.OpenWindow();
            
            // 커서 해제
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // GameManager에 알림
            GameManager.Instance?.NotifyItemUIToggled(true);
        }
    }
    
    /// <summary>
    /// 아이템 UI 닫기
    /// </summary>
    public void CloseItemUI()
    {
        if (itemModalWindow == null) return;
        
        if (itemModalWindow.isOn)
        {
            isItemUIOpen = false;
            itemModalWindow.CloseWindow();
            
            // 커서 잠금
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // GameManager에 알림
            GameManager.Instance?.NotifyItemUIToggled(false);
        }
    }
    
    /// <summary>
    /// 아이템 UI 토글
    /// </summary>
    public void ToggleItemUI()
    {
        if (isItemUIOpen)
            CloseItemUI();
        else
            OpenItemUI();
    }
    
    #endregion
    
    #region 실시간 업데이트
    
    /// <summary>
    /// 실시간 점수 상태 업데이트
    /// </summary>
    void UpdateRealTimeScoreStatus()
    {
        if (GameManager.Instance == null) return;
        
        float gameTime = GameManager.Instance.GetGameTime();
        float scoreIncreaseTime = GameManager.Instance.GetScoreIncreaseTime();
        
        if (gameTime >= scoreIncreaseTime)
        {
            UpdateScoreStatus("증가한 점수", 0f);
        }
        else
        {
            float remaining = scoreIncreaseTime - gameTime;
            UpdateScoreStatus("기본 점수", remaining);
        }
        
        // 재부착 시간 실시간 업데이트
        if (!GameManager.Instance.IsTeddyBearAttached())
        {
            float timeUntil = GameManager.Instance.GetTimeUntilReattach();
            UpdateAttachStatus(false, timeUntil);
        }
    }
    
    /// <summary>
    /// 실시간 게임 시간 및 배율 업데이트
    /// </summary>
    void UpdateRealTimeUI()
    {
        if (GameManager.Instance == null) return;
        
        // 게임 시간 가져오기
        float gameTime = GameManager.Instance.GetGameTime();
        gameTime = playTime - gameTime;
        
        // 게임 시간 UI 업데이트
        if (gameTimeText != null)
        {
            gameTimeText.text = string.Format(cachedGameTimeFormat, gameTime);
            gameTimeText.color = cachedGameTimeFormatColor;
        }
        
        // GameManager에 게임 시간 업데이트 알림
        GameManager.Instance.NotifyGameTimeUpdated(gameTime);
        
        // 배율도 실시간으로 추가 업데이트 (더 빠른 반응을 위해)
        float currentMultiplier = GameManager.Instance.GetScoreMultiplier();
        UpdateMultiplier(currentMultiplier);
    }




    #endregion
    
    #region 공개 메서드들
    
    /// <summary>
    /// HUD 표시/숨김
    /// </summary>
    public void SetHUDVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
    
    /// <summary>
    /// 현재 체력 정보
    /// </summary>
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthRatio() => currentHealth / maxHealth;
    
    /// <summary>
    /// 아이템 UI 상태 확인
    /// </summary>
    public bool IsItemUIOpen() => isItemUIOpen;
    
    /// <summary>
    /// 스킬 상태 확인
    /// </summary>
    public bool IsSkillReady(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= maxSkillSlots) return false;
        return skillAvailable[skillIndex] && skillCooldowns[skillIndex] <= 0f;
    }

    
    #endregion
} 
