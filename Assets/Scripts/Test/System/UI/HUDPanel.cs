using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.Heat;
using System.Collections;
using Febucci.UI;

/// <summary>
/// 🎮 통합 HUD 패널
/// CrosshairUI, HealthUI, ScoreUI, SkillAndItemUI를 모두 포함하는 메인 HUD
/// </summary>
public class HUDPanel : MonoBehaviour
{
    #region 인스펙터 할당 변수

    [Header("크로스헤어 UI 컴포넌트들")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private RectTransform crosshairContainer;

    [Header("체력바 UI 컴포넌트들")]
    [SerializeField] private ProgressBar healthProgressBar; // HeatUI ProgressBar
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("점수 UI 컴포넌트들")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI multiplierText;
    [SerializeField] private TextMeshProUGUI gameTimeText;
    [SerializeField] private TextMeshProUGUI attachStatusText;
    [SerializeField] private TextMeshProUGUI scoreStatusText;
    [SerializeField] private Image statusIcon;

    [Header("스킬 UI 컴포넌트들")]
    [SerializeField] private Image skillIcon;
    [SerializeField] private Image skillCooldownOverlay;
    [SerializeField] private TextMeshProUGUI skillCooldownText;

    [Header("아이템 UI 컴포넌트들")]
    [SerializeField] private Image itemIcon1;
    [SerializeField] private Image itemIcon2;
    [SerializeField] private Sprite emptyItemIcon;

    [Header("아이템 모달 UI 컴포넌트들")]
    [SerializeField] private ModalWindowManager itemModalWindow; // HeatUI Modal

    [Header("코인 UI 컴포넌트들")]
    [SerializeField] private TextMeshProUGUI coinText;


    #endregion

    #region 내부 상태 변수들

    private float currentHealth = 100f;
    private float maxHealth = 100f;
    private bool isTargeting = false;
    private bool isItemUIOpen = false;
    private int currentSpawnedCharacterIndex = -1;
    private GameObject currentCharacterPrefab; // 현재 캐릭터 프리팹 정보 저장
    private CharacterSkill currentCharacterSkill; // 현재 캐릭터 스킬 정보 저장
    private Sprite currentSkillIcon; // 현재 스킬 아이콘 스프라이트 저장
    private Sprite currentItemIcon1; // 현재 아이템1 아이콘 스프라이트 저장
    private Sprite currentItemIcon2; // 현재 아이템2 아이콘 스프라이트 저장
    private TestTeddyBear currentTeddyBear; // 현재 테디베어 컴포넌트 저장
    private int currentCoin = 0; // 현재 코인 저장
    
    // TextAnimator 관련 변수들
    private float lastMultiplier = -1f; // 마지막 배율 값 
    private TextAnimator_TMP multiplierTextAnimator; // TextAnimator 컴포넌트 참조
    private string lastMultiplierText = ""; // 마지막 설정된 텍스트 (중복 설정 방지)
    private float lastMultiplierUpdateTime = 0f; // 마지막 업데이트 시간 (애니메이션 보호)

    // 추가 TextAnimator 컴포넌트들
    private TextAnimator_TMP scoreTextAnimator;
    private TextAnimator_TMP gameTimeTextAnimator;
    private TextAnimator_TMP coinTextAnimator;
    
    // 각 텍스트의 마지막 상태 추적
    private string lastScoreText = "";
    private string lastGameTimeText = "";
    private string lastCoinText = "";
    private float lastScoreUpdateTime = 0f;
    private float lastGameTimeUpdateTime = 0f;
    private float lastCoinUpdateTime = 0f;

    #endregion

    #region 데이터베이스 참조

    private DataBase.UIData uiData;
    private DataBase.PlayerData playerData;
    private DataBase.ItemData itemData;

    #endregion

    #region 캐싱된 값들 (성능 최적화)

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

    private GameObject cachedPlayerPrefabData;

    private string cachedCoinFormat;
    private Color cachedCoinFormatColor;

    private bool dataBaseCached = false;

    #endregion

    #region Unity 생명주기

    void Awake()
    {
        InitializeHUD();
    }

    void OnEnable()
    {
        CacheDataBaseInfo();
        
        // HUD 패널이 활성화될 때 현재 플레이어의 CoinController에서 코인 상태 가져오기
        UpdateCoinFromCurrentPlayer();
        UpdateItemUI(); // OnEnable 시점에 아이템 아이콘 업데이트
    }

    void OnDisable()
    {
        // 패널이 비활성화될 때 정리 작업
    }

    void Start()
    {
        SubscribeToEvents();
        SetInitialState();
        FindTeddyBear();
        Debug.Log("✅ HUDPanel - 초기화 완료, 이벤트 구독됨");
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    void Update()
    {
        // 스킬 쿨타임 업데이트 (스킬 데이터가 로드된 경우에만)
        if (currentCharacterSkill != null)
        {
            UpdateSkillIconState();
        }

        // 실시간 점수 상태 업데이트
        UpdateRealTimeScoreStatus();

        // 실시간 게임 시간 업데이트
        UpdateRealTimeUI();

        // 시간대별 배율 UI 실시간 업데이트 (값이 변경된 경우에만)
        if (GameManager.Instance != null)
        {
            float currentMultiplier = GameManager.Instance.GetScoreMultiplier();
            if (Mathf.Abs(currentMultiplier - lastMultiplier) > 0.01f) // 값이 변경된 경우에만 업데이트
            {
                UpdateMultiplier(currentMultiplier);
                lastMultiplier = currentMultiplier;
            }
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

        // TextAnimator 컴포넌트 초기화 (Best Practices 적용)
        InitializeTextAnimator();

        // 스킬 시스템 초기화는 캐릭터 스폰 완료 후에 처리

        // 초기값 설정
        SetHealth(100f, 100f);
        SetCrosshairTargeting(false);
        UpdateScore(0f);
        UpdateMultiplier(1f);
        UpdateGameTime(0f);
        UpdateAttachStatus(false, 0f);
        
        // 아이템 아이콘 초기화 (빈 아이콘으로 표시)
        ClearItemIcons();
        
        // 로컬 CoinController 찾기 및 코인 초기화
        UpdateCoin(0);
    }

    /// <summary>
    /// DataBase 정보 캐싱
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

                cachedCoinFormat = uiData.CoinText;
                cachedCoinFormatColor = uiData.CoinFormatColor;

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
    /// 초기 상태 설정
    /// </summary>
    void SetInitialState()
    {
        // 크로스헤어 표시
        ShowCrosshair(true);

        // 아이템 UI 닫힌 상태로 시작
        CloseItemUI();
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
            GameManager.OnSkillUsed += UpdateSkillUI;
            GameManager.OnSkillCooldownStarted += SetSkillCooldown;

            // 캐릭터 스폰 이벤트
            GameManager.OnCharacterSpawned += OnCharacterSpawned;
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
            GameManager.OnSkillUsed -= UpdateSkillUI;
            GameManager.OnSkillCooldownStarted -= SetSkillCooldown;
            GameManager.OnCharacterSpawned -= OnCharacterSpawned;
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
        if (scoreText == null) return;

        // 너무 빠른 연속 호출 방지 (TextAnimator 애니메이션 보호)
        float currentTime = Time.time;
        float timeSinceLastUpdate = currentTime - lastScoreUpdateTime;
        
        // 0.1초 이내의 연속 호출은 무시 (애니메이션 중단 방지)
        if (timeSinceLastUpdate < 0.1f && lastScoreUpdateTime > 0f)
        {
            return;
        }

        string formattedText = string.Format(cachedScoreFormat, score);

        // 텍스트 내용이 실제로 변경되었을 때만 업데이트 (TextAnimator 애니메이션 보호)
        if (formattedText != lastScoreText)
        {
            scoreText.color = cachedScoreFormatColor;

            // TextAnimator SetText 메서드 사용 (Best Practices 적용)
            if (scoreTextAnimator != null)
            {
                scoreTextAnimator.SetText(formattedText);
                
                // 마지막 설정된 텍스트와 시간 저장
                lastScoreText = formattedText;
                lastScoreUpdateTime = currentTime;
            }
            else
            {
                // Fallback: 일반 텍스트 설정
                scoreText.text = formattedText;
                lastScoreText = formattedText;
                lastScoreUpdateTime = currentTime;
            }
        }
    }

    /// <summary>
    /// 배율 업데이트 (시간대별 포맷 적용)
    /// </summary>
    public void UpdateMultiplier(float multiplier)
    {
        if (multiplierText == null) return;

        // 너무 빠른 연속 호출 방지 (TextAnimator 애니메이션 보호)
        float currentTime = Time.time;
        float timeSinceLastUpdate = currentTime - lastMultiplierUpdateTime;
        
        // 0.1초 이내의 연속 호출은 무시 (애니메이션 중단 방지)
        if (timeSinceLastUpdate < 0.1f && lastMultiplierUpdateTime > 0f)
        {
            return;
        }

        string formattedText = "";
        Color textColor = Color.white;

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
                    textColor = multiplier > 1f ? cachedMultiplierFormatColor : cachedGeneralMultiplierFormatColor;
                    formattedText = string.Format(cachedMultiplierFormat, multiplier);
                }
                else
                {
                    textColor = cachedGeneralMultiplierFormatColor;
                    // 점수배율 적용 전: GeneralMultiplierFormat 사용
                    formattedText = string.Format(cachedGeneralMultiplierFormat, multiplier);
                }
            }
            else
            {
                textColor = cachedGeneralMultiplierFormatColor;
                // GameManager가 없는 경우
                formattedText = string.Format(cachedGeneralMultiplierFormat, multiplier);
            }
        }
        catch (System.Exception e)
        {
            textColor = cachedGeneralMultiplierFormatColor;
            // 안전한 fallback
            formattedText = string.Format(cachedGeneralMultiplierFormat, multiplier);
        }

        // 텍스트 내용이 실제로 변경되었을 때만 업데이트 (TextAnimator 애니메이션 보호)
        if (formattedText != lastMultiplierText)
        {
            // 색상 설정
            multiplierText.color = textColor;

            // TextAnimator SetText 메서드 사용 (Best Practices 적용)
            if (multiplierTextAnimator != null)
            {
                // 공식 문서 권장: textAnimator.SetText() 사용
                multiplierTextAnimator.SetText(formattedText);
                
                // 마지막 설정된 텍스트와 시간 저장
                lastMultiplierText = formattedText;
                lastMultiplierUpdateTime = currentTime;
            }
            else
            {
                // Fallback: 일반 텍스트 설정 (TextAnimator 없을 경우에만)
                multiplierText.text = formattedText;
                lastMultiplierText = formattedText;
                lastMultiplierUpdateTime = currentTime;
                Debug.LogWarning("⚠️ HUDPanel - TextAnimator가 없어 일반 텍스트로 업데이트: " + formattedText);
            }
        }
    }

    /// <summary>
    /// 게임 시간 업데이트
    /// </summary>
    public void UpdateGameTime(float time)
    {
        if (gameTimeText == null) return;

        // 너무 빠른 연속 호출 방지 (TextAnimator 애니메이션 보호)
        float currentTime = Time.time;
        float timeSinceLastUpdate = currentTime - lastGameTimeUpdateTime;
        
        // 0.1초 이내의 연속 호출은 무시 (애니메이션 중단 방지)
        if (timeSinceLastUpdate < 0.1f && lastGameTimeUpdateTime > 0f)
        {
            return;
        }

        string formattedText = string.Format(cachedGameTimeFormat, time);

        // 텍스트 내용이 실제로 변경되었을 때만 업데이트 (TextAnimator 애니메이션 보호)
        if (formattedText != lastGameTimeText)
        {
            gameTimeText.color = cachedGameTimeFormatColor;

            // TextAnimator SetText 메서드 사용 (Best Practices 적용)
            if (gameTimeTextAnimator != null)
            {
                gameTimeTextAnimator.SetText(formattedText);
                
                // 마지막 설정된 텍스트와 시간 저장
                lastGameTimeText = formattedText;
                lastGameTimeUpdateTime = currentTime;
            }
            else
            {
                // Fallback: 일반 텍스트 설정
                gameTimeText.text = formattedText;
                lastGameTimeText = formattedText;
                lastGameTimeUpdateTime = currentTime;
            }
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
    /// 스킬 쿨다운 설정 (UI 업데이트용)
    /// </summary>
    public void SetSkillCooldown(int skillIndex, float cooldownTime)
    {
        // UI 업데이트에만 집중 - 실제 게임 조작은 하지 않음
        UpdateSkillUI();
    }

    /// <summary>
    /// 스킬 UI 업데이트 (기존 시스템 호환용)
    /// </summary>
    void UpdateSkillUI()
    {
        // 저장된 캐릭터 스킬 정보가 있으면 상태만 업데이트
        if (currentCharacterSkill != null)
        {
            UpdateSkillIconState();
            return;
        }

        // 저장된 정보가 없으면 새로 로드
        LoadCharacterPrefabData();
        LoadSkillIconFromCharacterSkill();
        ConnectSkillIconToHUD();
        UpdateSkillIconState();
    }

    /// <summary>
    /// 스킬 쿨타임 실시간 업데이트
    /// </summary>
    public void UpdateSkillCooldowns()
    {
        // 저장된 캐릭터 스킬 정보가 있으면 상태만 업데이트
        if (currentCharacterSkill != null)
        {
            UpdateSkillIconState();
            return;
        }

        // 스킬 데이터가 로드되지 않은 경우, 스폰된 캐릭터에서 다시 시도
        SpawnController spawnController = FindObjectOfType<SpawnController>();
        if (spawnController != null)
        {
            currentCharacterSkill = spawnController.GetCurrentSpawnedCharacterSkill();
            if (currentCharacterSkill != null)
            {
                // 스킬 아이콘도 함께 로드
                currentSkillIcon = currentCharacterSkill.SkillIcon;
                if (currentSkillIcon != null && skillIcon != null)
                {
                    skillIcon.sprite = currentSkillIcon;
                    skillIcon.color = currentCharacterSkill.SkillColor;
                }
                UpdateSkillIconState();
            }
        }
    }

    #endregion

    #region 아이템 UI

    /// <summary>
    /// 아이템 UI 업데이트
    /// </summary>
    public void UpdateItemUI()
    {
        UpdateItemIcons();
    }

    /// <summary>
    /// 아이템 아이콘 업데이트
    /// </summary>
    private void UpdateItemIcons()
    {
        // 현재 플레이어의 ItemController 찾기
        ItemController itemController = FindCurrentPlayerItemController();
        if (itemController == null)
        {
            Debug.LogWarning("⚠️ HUDPanel - 현재 플레이어의 ItemController를 찾을 수 없습니다.");
            ClearItemIcons();
            return;
        }

        try
        {
            // ItemSlot1의 모든 아이템 가져오기
            Transform itemSlot = itemController.GetItemSlot1();
            if (itemSlot == null)
            {
                Debug.LogWarning("⚠️ HUDPanel - ItemSlot을 찾을 수 없습니다.");
                ClearItemIcons();
                return;
            }

            int itemCount = itemSlot.childCount;
            if (itemCount == 0)
            {
                // 아이템이 없으면 아이콘 초기화
                ClearItemIcons();
                return;
            }

            // 첫 번째 아이템 (itemIcon1에 표시) - 활성화된 아이템
            if (itemCount >= 1)
            {
                Transform firstChild = itemSlot.GetChild(itemSlot.childCount - 1); // 마지막 자식이 활성화된 아이템
                if (firstChild != null)
                {
                    CharacterItem firstItem = firstChild.GetComponent<CharacterItem>();
                    if (firstItem != null)
                    {
                        bool isActive = firstChild.gameObject.activeInHierarchy;
                        UpdateItemIcon(itemIcon1, firstItem.SkillIcon, firstItem.SkillColor, isActive);
                    }
                    else
                    {
                        ClearItemIcon(itemIcon1);
                    }
                }
                else
                {
                    ClearItemIcon(itemIcon1);
                }
            }

            // 두 번째 아이템 (itemIcon2에 표시) - 비활성화된 아이템
            if (itemCount >= 2)
            {
                Transform secondChild = itemSlot.GetChild(itemSlot.childCount - 2); // 두 번째 마지막 자식
                if (secondChild != null)
                {
                    CharacterItem secondItem = secondChild.GetComponent<CharacterItem>();
                    if (secondItem != null)
                    {
                        bool isActive = secondChild.gameObject.activeInHierarchy;
                        UpdateItemIcon(itemIcon2, secondItem.SkillIcon, secondItem.SkillColor, isActive);
                    }
                    else
                    {
                        ClearItemIcon(itemIcon2);
                    }
                }
                else
                {
                    ClearItemIcon(itemIcon2);
                }
            }
            else
            {
                // 두 번째 아이템이 없으면 아이콘 초기화
                ClearItemIcon(itemIcon2);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ HUDPanel - 아이템 아이콘 업데이트 중 오류: {e.Message}");
            ClearItemIcons();
        }
    }

    /// <summary>
    /// 아이템 아이콘 업데이트
    /// </summary>
    /// <param name="iconImage">업데이트할 아이콘 이미지</param>
    /// <param name="skillIcon">스킬 아이콘 스프라이트</param>
    /// <param name="skillColor">스킬 색상</param>
    /// <param name="isActive">활성화 상태</param>
    private void UpdateItemIcon(Image iconImage, Sprite skillIcon, Color skillColor, bool isActive)
    {
        if (iconImage == null)
        {
            Debug.LogWarning("⚠️ HUDPanel - 아이콘 이미지가 null입니다.");
            return;
        }

        if (skillIcon == null)
        {
            Debug.LogWarning("⚠️ HUDPanel - 스킬 아이콘이 null입니다. 빈 아이콘으로 표시합니다.");
            // 스킬 아이콘이 없으면 빈 아이콘 표시
            iconImage.sprite = emptyItemIcon;
            iconImage.color = Color.white;
            iconImage.gameObject.SetActive(true);
            return;
        }

        // 아이콘 설정 (비활성화되어 있어도 아이콘은 표시)
        iconImage.sprite = skillIcon;
        iconImage.color = isActive ? skillColor : Color.gray; // 비활성화된 아이템은 회색
        iconImage.gameObject.SetActive(true); // 항상 활성화
    }

    /// <summary>
    /// 특정 아이템 아이콘 초기화
    /// </summary>
    /// <param name="iconImage">초기화할 아이콘 이미지</param>
    private void ClearItemIcon(Image iconImage)
    {
        if (iconImage != null)
        {
            iconImage.sprite = emptyItemIcon;
            iconImage.color = Color.white; // 빈 아이콘은 흰색으로 표시
            iconImage.gameObject.SetActive(true); // 빈 아이콘도 표시
        }
    }

    /// <summary>
    /// 모든 아이템 아이콘 초기화
    /// </summary>
    private void ClearItemIcons()
    {
        ClearItemIcon(itemIcon1);
        ClearItemIcon(itemIcon2);
    }

    #endregion


    #region 아이템 UI (모달창)

    /// <summary>
    /// 아이템 UI 열기
    /// </summary>
    public void OpenItemUI()
    {
        if (itemModalWindow == null) return;
        if (!gameObject.activeSelf) return;
        if (!itemModalWindow.isOn)
        {

            TestShoot.SetIsShooting(false);
        
        
            isItemUIOpen = true;
            itemModalWindow.OpenWindow();

            // 마우스 커서 보이게 하고 고정 해제
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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

            if(!currentTeddyBear.IsAttached())
            {
                TestShoot.SetIsShooting(true);
            }

            isItemUIOpen = false;
            itemModalWindow.CloseWindow();

            // 마우스 커서 숨기고 중앙에 고정
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (!currentTeddyBear.IsAttached())
            {
                TestShoot.SetIsShooting(true);
            }
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

    #region 아이템 아이콘 업데이트

  

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
    /// 실시간 배율 업데이트 (게임 시간은 이벤트로 처리)
    /// </summary>
    void UpdateRealTimeUI()
    {
        if (GameManager.Instance == null) return;

        // 배율만 실시간으로 업데이트 (게임 시간은 OnGameTimeUpdated 이벤트로 처리)
        float currentMultiplier = GameManager.Instance.GetScoreMultiplier();
        UpdateMultiplier(currentMultiplier);
    }

    #endregion

    #region 캐릭터 스폰 이벤트 처리

    /// <summary>
    /// 캐릭터 스폰 완료 시 호출되는 이벤트 핸들러
    /// </summary>
    void OnCharacterSpawned()
    {
        Debug.Log("🎯 HUDPanel - 캐릭터 스폰 완료, 스킬 시스템 초기화 시작");

        // 약간의 지연 후 스킬 시스템 초기화 (스폰 완료 보장)
        StartCoroutine(InitializeSkillSystemAfterSpawn());
    }

    /// <summary>
    /// 스폰 완료 후 스킬 시스템 초기화
    /// </summary>
    IEnumerator InitializeSkillSystemAfterSpawn()
    {
        // 스폰 완료를 확실히 보장하기 위한 짧은 지연
        yield return new WaitForSeconds(0.1f);

        // 스킬 데이터 업데이트
        UpdateSkillDataFromSpawnedCharacter();

        Debug.Log("✅ HUDPanel - 스킬 시스템 초기화 완료");
    }

    #endregion

    #region 스킬 데이터 관리

    /// <summary>
    /// 스폰된 캐릭터 프리팹 정보 받아오기 (1단계)
    /// </summary>
    public void LoadCharacterPrefabData()
    {
        // SpawnController에서 현재 스폰된 캐릭터 인덱스 가져오기
        SpawnController spawnController = FindObjectOfType<SpawnController>();
        if (spawnController == null)    
        {
            Debug.LogWarning("⚠️ HUDPanel - SpawnController를 찾을 수 없습니다.");
            return;
        }

        int currentSpawnedCharacterIndex = spawnController.NotifyHUDOfCharacterSpawn();
        if (currentSpawnedCharacterIndex < 0)
        {
            Debug.LogWarning("⚠️ HUDPanel - 스폰된 캐릭터 인덱스가 유효하지 않습니다: " + currentSpawnedCharacterIndex);
            return;
        }

        // DataBase에서 해당 인덱스의 프리팹 정보 가져오기
        if (!dataBaseCached || DataBase.Instance == null)
        {
            Debug.LogWarning("⚠️ HUDPanel - DataBase가 캐싱되지 않았습니다.");
            return;
        }

        try
        {
            currentCharacterPrefab = DataBase.Instance.playerData.PlayerPrefabData[currentSpawnedCharacterIndex];
            if (currentCharacterPrefab == null)
            {
                Debug.LogError($"❌ HUDPanel - 캐릭터 인덱스 {currentSpawnedCharacterIndex}의 프리팹이 null입니다.");
                return;
            }

            this.currentSpawnedCharacterIndex = currentSpawnedCharacterIndex;
            Debug.Log($"✅ HUDPanel - 캐릭터 프리팹 정보 로드 완료: 인덱스 {currentSpawnedCharacterIndex}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ HUDPanel - 프리팹 정보 로드 중 오류: {e.Message}");
        }
    }

    /// <summary>
    /// CharacterSkill 정보에서 스킬 아이콘 스프라이트 받아오기 (2단계)
    /// </summary>
    public void LoadSkillIconFromCharacterSkill()
    {
        if (currentCharacterPrefab == null)
        {
            Debug.LogWarning("⚠️ HUDPanel - 캐릭터 프리팹 정보가 없습니다. LoadCharacterPrefabData()를 먼저 호출하세요.");
            return;
        }

        try
        {
            // SpawnController에서 실제 스폰된 캐릭터 인스턴스의 CharacterSkill 가져오기
            SpawnController spawnController = FindObjectOfType<SpawnController>();
            if (spawnController == null)
            {
                Debug.LogWarning("⚠️ HUDPanel - SpawnController를 찾을 수 없습니다.");
                return;
            }

            currentCharacterSkill = spawnController.GetCurrentSpawnedCharacterSkill();
            if (currentCharacterSkill == null)
            {
                Debug.LogWarning($"⚠️ HUDPanel - 스폰된 캐릭터에 CharacterSkill이 없습니다.");
                return;
            }

            // 스킬 아이콘 스프라이트 가져오기
            currentSkillIcon = currentCharacterSkill.SkillIcon;
            if (currentSkillIcon == null)
            {
                Debug.LogWarning($"⚠️ HUDPanel - 스킬 '{currentCharacterSkill.SkillName}'의 아이콘이 null입니다.");
                return;
            }

            Debug.Log($"✅ HUDPanel - 스킬 아이콘 로드 완료: {currentCharacterSkill.SkillName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ HUDPanel - 스킬 아이콘 로드 중 오류: {e.Message}");
        }
    }

    /// <summary>
    /// 받아온 스킬 아이콘을 HUDPanel에 연결하기 (3단계)
    /// </summary>
    public void ConnectSkillIconToHUD()
    {
        if (currentSkillIcon == null)
        {
            Debug.LogWarning("⚠️ HUDPanel - 스킬 아이콘이 없습니다. LoadSkillIconFromCharacterSkill()를 먼저 호출하세요.");
            return;
        }

        if (skillIcon == null)
        {
            Debug.LogWarning("⚠️ HUDPanel - skillIcon UI 컴포넌트가 null입니다.");
            return;
        }

        try
        {
            // 스킬 아이콘을 HUD에 연결
            skillIcon.sprite = currentSkillIcon;
            skillIcon.color = currentCharacterSkill.SkillColor;

            Debug.Log($"✅ HUDPanel - 스킬 아이콘 HUD 연결 완료: {currentCharacterSkill.SkillName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ HUDPanel - 스킬 아이콘 HUD 연결 중 오류: {e.Message}");
        }
    }

    /// <summary>
    /// 스킬 아이콘 상태 업데이트 (4단계)
    /// </summary>
    public void UpdateSkillIconState()
    {
        if (currentCharacterSkill == null)
        {
            Debug.LogWarning("⚠️ HUDPanel - 캐릭터 스킬 정보가 없습니다.");
            return;
        }

        if (skillIcon == null)
        {
            Debug.LogWarning("⚠️ HUDPanel - skillIcon UI 컴포넌트가 null입니다.");
            return;
        }

        try
        {
            // 스킬 사용 가능 여부에 따른 아이콘 색상 업데이트
            bool isOnCooldown = currentCharacterSkill.RemainingCooldown > 0f;
            skillIcon.color = isOnCooldown ? Color.gray : Color.white;

            // 쿨다운 오버레이 업데이트
            if (skillCooldownOverlay != null)
            {
                if (isOnCooldown && currentCharacterSkill.CooldownTime > 0f)
                {
                    float fillAmount = currentCharacterSkill.RemainingCooldown / currentCharacterSkill.CooldownTime;
                    skillCooldownOverlay.fillAmount = fillAmount;
                    skillCooldownOverlay.gameObject.SetActive(true);
                }
                else
                {
                    skillCooldownOverlay.gameObject.SetActive(false);
                }
            }

            // 쿨다운 텍스트 업데이트
            if (skillCooldownText != null)
            {
                if (isOnCooldown)
                {
                    skillCooldownText.text = currentCharacterSkill.RemainingCooldown.ToString("F1");
                    skillCooldownText.gameObject.SetActive(true);
                }
                else
                {
                    skillCooldownText.gameObject.SetActive(false);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ HUDPanel - 스킬 아이콘 상태 업데이트 중 오류: {e.Message}");
        }
    }

    /// <summary>
    /// 전체 스킬 데이터 업데이트 (모든 단계를 순차적으로 실행)
    /// </summary>
    public void UpdateSkillDataFromSpawnedCharacter()
    {
        LoadCharacterPrefabData();        // 1단계: 프리팹 정보 받아오기
        LoadSkillIconFromCharacterSkill(); // 2단계: 스킬 아이콘 받아오기
        ConnectSkillIconToHUD();          // 3단계: HUD에 연결
        UpdateSkillIconState();           // 4단계: 상태 업데이트
    }

    /// <summary>
    /// 캐릭터 인덱스로부터 스킬 데이터를 가져와 HUD 업데이트 (기존 호환성 유지)
    /// </summary>
    public void UpdateSkillDataFromCharacterIndex(int characterIndex)
    {
        if (!dataBaseCached || DataBase.Instance == null)
        {
            Debug.LogWarning("⚠️ HUDPanel - DataBase가 캐싱되지 않았습니다.");
            return;
        }

        try
        {
            // playerData의 프리팹 데이터 배열에서 해당 인덱스의 프리팹 가져오기
            currentCharacterPrefab = DataBase.Instance.playerData.PlayerPrefabData[characterIndex];
            if (currentCharacterPrefab == null)
            {
                Debug.LogError($"❌ HUDPanel - 캐릭터 인덱스 {characterIndex}의 프리팹이 null입니다.");
                return;
            }

            this.currentSpawnedCharacterIndex = characterIndex;

            // 나머지 단계들 실행
            LoadSkillIconFromCharacterSkill();
            ConnectSkillIconToHUD();
            UpdateSkillIconState();

            Debug.Log($"✅ HUDPanel - 캐릭터 인덱스 {characterIndex}의 스킬 데이터 업데이트 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ HUDPanel - 스킬 데이터 업데이트 중 오류: {e.Message}");
        }
    }

    /// <summary>
    /// 캐릭터 스킬로부터 스킬 데이터를 가져와 HUD 업데이트
    /// </summary>
    public void UpdateSkillDataFromCharacterSkill(CharacterSkill characterSkill)
    {
        if (characterSkill == null)
        {
            Debug.LogWarning("⚠️ HUDPanel - CharacterSkill이 null입니다.");
            return;
        }

        try
        {
            currentCharacterSkill = characterSkill;
            currentSkillIcon = characterSkill.SkillIcon;

            ConnectSkillIconToHUD();

            Debug.Log($"✅ HUDPanel - 스킬 '{characterSkill.SkillName}' 데이터 업데이트 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ HUDPanel - 스킬 데이터 업데이트 중 오류: {e.Message}");
        }
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
    /// 스킬 상태 확인 (UI 표시용)
    /// </summary>
    public bool IsSkillReady(int skillIndex)
    {
        // UI 표시용으로만 사용 - 실제 게임 조작은 하지 않음
        // 현재는 항상 true 반환 (실제 스킬 상태는 CharacterSkill에서 관리)
        return true;
    }

    #endregion


    #region 컴포넌트 찾기
    void FindTeddyBear()
    {
        if (currentTeddyBear == null)
        {
            currentTeddyBear = FindObjectOfType<TestTeddyBear>();
            if (currentTeddyBear != null)
            {
                Debug.Log("테디베어를 찾았습니다!");
            }
        }
    }

    #endregion

    #region 코인 UI 관리
    /// <summary>
    /// 코인 UI 업데이트 (CoinController로부터 받은 값 사용)
    /// </summary>
    /// <param name="coinAmount">표시할 코인 수</param>
    public void UpdateCoin(int coinAmount)
    {
        if (coinText == null) return;

        // 너무 빠른 연속 호출 방지 (TextAnimator 애니메이션 보호)
        float currentTime = Time.time;
        float timeSinceLastUpdate = currentTime - lastCoinUpdateTime;
        
        // 0.1초 이내의 연속 호출은 무시 (애니메이션 중단 방지)
        if (timeSinceLastUpdate < 0.1f && lastCoinUpdateTime > 0f)
        {
            return;
        }

        currentCoin = coinAmount;
        string formattedText = string.Format(cachedCoinFormat, currentCoin);

        // 텍스트 내용이 실제로 변경되었을 때만 업데이트 (TextAnimator 애니메이션 보호)
        if (formattedText != lastCoinText)
        {
            coinText.color = cachedCoinFormatColor;

            // TextAnimator SetText 메서드 사용 (Best Practices 적용)
            if (coinTextAnimator != null)
            {
                coinTextAnimator.SetText(formattedText);
                
                // 마지막 설정된 텍스트와 시간 저장
                lastCoinText = formattedText;
                lastCoinUpdateTime = currentTime;
            }
            else
            {
                // Fallback: 일반 텍스트 설정
                coinText.text = formattedText;
                lastCoinText = formattedText;
                lastCoinUpdateTime = currentTime;
            }
        }
    }

    /// <summary>
    /// 현재 플레이어의 CoinController에서 코인 상태를 가져와 HUD에 업데이트
    /// </summary>
    private void UpdateCoinFromCurrentPlayer()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("⚠️ HUDPanel - GameManager가 없어 현재 플레이어의 CoinController를 찾을 수 없습니다.");
            return;
        }

        CoinController coinController = GameManager.Instance.GetCurrentPlayerCoinController();
        if (coinController != null)
        {
            UpdateCoin(coinController.GetCurrentCoin());
        }
        else
        {
            Debug.LogWarning("⚠️ HUDPanel - 현재 플레이어의 CoinController를 찾을 수 없습니다.");
            UpdateCoin(0); // 기본값 설정
        }
    }
    #endregion

    /// <summary>
    /// 현재 플레이어의 ItemController 찾기 (싱글 기반, Photon2 확장 고려)
    /// </summary>
    /// <returns>현재 플레이어의 ItemController</returns>
    private ItemController FindCurrentPlayerItemController()
    {
        // 캐릭터가 스폰되기 전에는 ItemController가 존재하지 않음
        if (GameManager.Instance == null)
        {
            Debug.Log("⚠️ HUDPanel - GameManager가 없어 ItemController를 찾을 수 없습니다.");
            return null;
        }

        // 플레이어 태그로 찾기 (싱글 환경에서는 안전)
        // 나중에 Photon2 환경에서는 PhotonNetwork.LocalPlayer 사용
        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
        if (currentPlayer == null)
        {
            Debug.Log("⚠️ HUDPanel - 플레이어가 아직 스폰되지 않았습니다.");
            return null;
        }

        // ItemController 찾기
        ItemController itemController = currentPlayer.GetComponent<ItemController>();
        if (itemController == null)
        {
            // 플레이어에 직접 ItemController가 없으면 자식에서 찾기
            itemController = currentPlayer.GetComponentInChildren<ItemController>();
        }

        if (itemController == null)
        {
            Debug.Log("⚠️ HUDPanel - 플레이어에 ItemController가 없습니다.");
            return null;
        }

        return itemController;
    }

    /// <summary>
    /// Photon2 환경에서 로컬 플레이어의 ItemController 찾기 (미래 확장용)
    /// </summary>
    /// <returns>로컬 플레이어의 ItemController</returns>
    private ItemController FindLocalPlayerItemControllerPhoton()
    {
        // Photon2 환경에서만 사용
        // 현재는 주석 처리, 나중에 Photon2 추가 시 활성화
        /*
        if (PhotonNetwork.LocalPlayer != null)
        {
            // PhotonView를 통해 로컬 플레이어 찾기
            PhotonView[] photonViews = FindObjectsOfType<PhotonView>();
            foreach (PhotonView pv in photonViews)
            {
                if (pv.IsMine)
                {
                    ItemController itemController = pv.GetComponent<ItemController>();
                    if (itemController != null)
                    {
                        return itemController;
                    }
                }
            }
        }
        */
        
        // 현재는 싱글 환경이므로 기본 방법 사용
        return FindCurrentPlayerItemController();
    }

    /// <summary>
    /// TextAnimator 컴포넌트 초기화 (Best Practices 적용)
    /// </summary>
    private void InitializeTextAnimator()
    {
        // multiplierText TextAnimator 초기화
        if (multiplierText != null)
        {
            multiplierTextAnimator = multiplierText.GetComponent<TextAnimator_TMP>();
            if (multiplierTextAnimator == null)
            {
                multiplierTextAnimator = multiplierText.gameObject.AddComponent<TextAnimator_TMP>();
                Debug.Log("✅ HUDPanel - TextAnimator_TMP 컴포넌트를 multiplierText에 추가했습니다.");
            }
            else
            {
                Debug.Log("✅ HUDPanel - multiplierText 기존 TextAnimator_TMP 컴포넌트를 찾았습니다.");
            }
        }

        // scoreText TextAnimator 초기화
        if (scoreText != null)
        {
            scoreTextAnimator = scoreText.GetComponent<TextAnimator_TMP>();
            if (scoreTextAnimator == null)
            {
                scoreTextAnimator = scoreText.gameObject.AddComponent<TextAnimator_TMP>();
                Debug.Log("✅ HUDPanel - TextAnimator_TMP 컴포넌트를 scoreText에 추가했습니다.");
            }
            else
            {
                Debug.Log("✅ HUDPanel - scoreText 기존 TextAnimator_TMP 컴포넌트를 찾았습니다.");
            }
        }

        // gameTimeText TextAnimator 초기화
        if (gameTimeText != null)
        {
            gameTimeTextAnimator = gameTimeText.GetComponent<TextAnimator_TMP>();
            if (gameTimeTextAnimator == null)
            {
                gameTimeTextAnimator = gameTimeText.gameObject.AddComponent<TextAnimator_TMP>();
                Debug.Log("✅ HUDPanel - TextAnimator_TMP 컴포넌트를 gameTimeText에 추가했습니다.");
            }
            else
            {
                Debug.Log("✅ HUDPanel - gameTimeText 기존 TextAnimator_TMP 컴포넌트를 찾았습니다.");
            }
        }

        // coinText TextAnimator 초기화
        if (coinText != null)
        {
            coinTextAnimator = coinText.GetComponent<TextAnimator_TMP>();
            if (coinTextAnimator == null)
            {
                coinTextAnimator = coinText.gameObject.AddComponent<TextAnimator_TMP>();
                Debug.Log("✅ HUDPanel - TextAnimator_TMP 컴포넌트를 coinText에 추가했습니다.");
            }
            else
            {
                Debug.Log("✅ HUDPanel - coinText 기존 TextAnimator_TMP 컴포넌트를 찾았습니다.");
            }
        }

        Debug.Log("🎨 HUDPanel - 모든 TextAnimator 초기화 완료. <shake>, <wave>, <bounce> 등 태그 사용 가능");
    }

    /// <summary>
    /// 모든 TextAnimator 메시 새로고침 (Best Practices 권장)
    /// TMPro.ForceMeshUpdate() 대신 사용
    /// </summary>
    public void RefreshAllTextMeshes()
    {
        RefreshMultiplierTextMesh();
        RefreshScoreTextMesh();
        RefreshGameTimeTextMesh();
        RefreshCoinTextMesh();
    }

    /// <summary>
    /// Multiplier TextAnimator 메시 새로고침
    /// </summary>
    public void RefreshMultiplierTextMesh()
    {
        if (multiplierTextAnimator != null)
        {
            multiplierTextAnimator.ScheduleMeshRefresh();
        }
    }

    /// <summary>
    /// Score TextAnimator 메시 새로고침
    /// </summary>
    public void RefreshScoreTextMesh()
    {
        if (scoreTextAnimator != null)
        {
            scoreTextAnimator.ScheduleMeshRefresh();
        }
    }

    /// <summary>
    /// GameTime TextAnimator 메시 새로고침
    /// </summary>
    public void RefreshGameTimeTextMesh()
    {
        if (gameTimeTextAnimator != null)
        {
            gameTimeTextAnimator.ScheduleMeshRefresh();
        }
    }

    /// <summary>
    /// Coin TextAnimator 메시 새로고침
    /// </summary>
    public void RefreshCoinTextMesh()
    {
        if (coinTextAnimator != null)
        {
            coinTextAnimator.ScheduleMeshRefresh();
        }
    }

    #region TextAnimator 테스트 메서드들 (에디터 전용)

    /// <summary>
    /// 모든 TextAnimator 테스트 메서드 실행
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestAllTextAnimations()
    {
        TestMultiplierAnimation();
        TestScoreAnimation();
        TestGameTimeAnimation();
        TestCoinAnimation();
    }

    /// <summary>
    /// Multiplier TextAnimator 테스트 메서드
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestMultiplierAnimation()
    {
        if (multiplierTextAnimator != null)
        {
            string testText = "<shake>×2.5</shake>";
            multiplierTextAnimator.SetText(testText);
            lastMultiplierText = testText;
            Debug.Log("🎭 HUDPanel - Multiplier Shake 애니메이션 테스트: " + testText);
        }
    }

    /// <summary>
    /// Score TextAnimator 테스트 메서드
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestScoreAnimation()
    {
        if (scoreTextAnimator != null)
        {
            string testText = "<bounce>SCORE: 1,500</bounce>";
            scoreTextAnimator.SetText(testText);
            lastScoreText = testText;
            Debug.Log("🎭 HUDPanel - Score Bounce 애니메이션 테스트: " + testText);
        }
    }

    /// <summary>
    /// GameTime TextAnimator 테스트 메서드
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestGameTimeAnimation()
    {
        if (gameTimeTextAnimator != null)
        {
            string testText = "<wave>TIME: 03:45</wave>";
            gameTimeTextAnimator.SetText(testText);
            lastGameTimeText = testText;
            Debug.Log("🎭 HUDPanel - GameTime Wave 애니메이션 테스트: " + testText);
        }
    }

    /// <summary>
    /// Coin TextAnimator 테스트 메서드
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestCoinAnimation()
    {
        if (coinTextAnimator != null)
        {
            string testText = "<shake><color=yellow>💰 999</color></shake>";
            coinTextAnimator.SetText(testText);
            lastCoinText = testText;
            Debug.Log("🎭 HUDPanel - Coin Shake 애니메이션 테스트: " + testText);
        }
    }

    #endregion
} 
