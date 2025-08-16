using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.Heat;
using System.Collections;
using Febucci.UI;

public class HUDPanel : MonoBehaviour
{
    [Header("크로스헤어 UI 컴포넌트들")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private RectTransform crosshairContainer;

    [Header("체력바 UI 컴포넌트들")]
    [SerializeField] private ProgressBar healthProgressBar;
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
    [SerializeField] private ModalWindowManager itemModalWindow;

    [Header("코인 UI 컴포넌트들")]
    [SerializeField] private TextMeshProUGUI coinText;

    private float currentHealth = 100f;
    private float maxHealth = 100f;
    private bool isTargeting = false;
    private bool isItemUIOpen = false;
    private int currentSpawnedCharacterIndex = -1;
    private GameObject currentCharacterPrefab;
    private CharacterSkill currentCharacterSkill;
    private Sprite currentSkillIcon;
    private Sprite currentItemIcon1;
    private Sprite currentItemIcon2;
    private TestTeddyBear currentTeddyBear;
    private int currentCoin = 0;
    
    private float lastMultiplier = -1f;
    private TextAnimator_TMP multiplierTextAnimator;
    private string lastMultiplierText = "";
    private float lastMultiplierUpdateTime = 0f;

    private TextAnimator_TMP scoreTextAnimator;
    private TextAnimator_TMP gameTimeTextAnimator;
    private TextAnimator_TMP coinTextAnimator;
    
    private string lastScoreText = "";
    private string lastGameTimeText = "";
    private string lastCoinText = "";
    private float lastScoreUpdateTime = 0f;
    private float lastGameTimeUpdateTime = 0f;
    private float lastCoinUpdateTime = 0f;

    private DataBase.UIData uiData;
    private DataBase.PlayerData playerData;
    private DataBase.ItemData itemData;

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

    void Awake()
    {
        InitializeHUD();
    }

    void OnEnable()
    {
        CacheDataBaseInfo();
        UpdateCoinFromCurrentPlayer();
        UpdateItemUI();
    }

    void Start()
    {
        SubscribeToEvents();
        SetInitialState();
        FindTeddyBear();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    void Update()
    {
        if (currentCharacterSkill != null)
        {
            UpdateSkillIconState();
        }

        UpdateRealTimeScoreStatus();
        UpdateRealTimeUI();

        if (GameManager.Instance != null)
        {
            float currentMultiplier = GameManager.Instance.GetScoreMultiplier();
            if (Mathf.Abs(currentMultiplier - lastMultiplier) > 0.01f)
            {
                UpdateMultiplier(currentMultiplier);
                lastMultiplier = currentMultiplier;
            }
        }
    }

    void InitializeHUD()
    {
        CacheDataBaseInfo();
        InitializeTextAnimator();
        ClearItemIcons();
        UpdateCoin(0);
    }

    void CacheDataBaseInfo()
    {
        try
        {
            if (DataBase.Instance == null) return;

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
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ HUDPanel - DataBase 캐싱 중 오류: {e.Message}");
            dataBaseCached = false;
        }
    }

    void SetInitialState()
    {
        ShowCrosshair(true);
        CloseItemUI();
    }

    void SubscribeToEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.OnScoreUpdated += UpdateScore;
            GameManager.OnScoreMultiplierUpdated += UpdateMultiplier;
            GameManager.OnGameTimeUpdated += UpdateGameTime;
            GameManager.OnTeddyBearAttachmentChanged += OnTeddyBearAttachmentChanged;
            GameManager.OnTeddyBearReattachTimeChanged += OnTeddyBearReattachTimeChanged;
            GameManager.OnPlayerHealthChanged += OnPlayerHealthChanged;
            GameManager.OnCrosshairTargetingChanged += SetCrosshairTargeting;
            GameManager.OnSkillUsed += UpdateSkillUI;
            GameManager.OnSkillCooldownStarted += SetSkillCooldown;
            GameManager.OnCharacterSpawned += OnCharacterSpawned;
        }

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

    public void ShowCrosshair(bool show)
    {
        if (crosshairImage != null)
        {
            crosshairImage.gameObject.SetActive(show);
        }
    }

    public void SetCrosshairTargeting(bool targeting)
    {
        isTargeting = targeting;

        if (crosshairImage != null)
        {
            crosshairImage.color = targeting ? cachedCrosshairTargetColor : cachedCrosshairNormalColor;
        }
    }

    public void SetCrosshairSize(float size)
    {
        cachedCrosshairSize = Mathf.Clamp(size, 0.1f, 3f);

        if (crosshairContainer != null)
        {
            crosshairContainer.localScale = Vector3.one * cachedCrosshairSize;
        }
    }

    public void SetHealth(float current, float max)
    {
        currentHealth = Mathf.Clamp(current, 0f, max);
        maxHealth = Mathf.Max(max, 1f);

        UpdateHealthDisplay();
    }

    void UpdateHealthDisplay()
    {
        float healthRatio = currentHealth / maxHealth;

        if (healthProgressBar != null)
        {
            healthProgressBar.currentValue = currentHealth;
            healthProgressBar.maxValue = maxHealth;
            healthProgressBar.UpdateUI();
        }

        if (healthText != null)
        {
            healthText.text = string.Format(cachedHealthFormat, currentHealth, maxHealth);

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

    public void UpdateScore(float score)
    {
        if (scoreText == null) return;

        float currentTime = Time.time;
        float timeSinceLastUpdate = currentTime - lastScoreUpdateTime;
        
        if (timeSinceLastUpdate < 0.1f && lastScoreUpdateTime > 0f) return;

        string formattedText = string.Format(cachedScoreFormat, score);

        if (formattedText != lastScoreText)
        {
            scoreText.color = cachedScoreFormatColor;

            if (scoreTextAnimator != null)
            {
                scoreTextAnimator.SetText(formattedText);
                lastScoreText = formattedText;
                lastScoreUpdateTime = currentTime;
            }
            else
            {
                scoreText.text = formattedText;
                lastScoreText = formattedText;
                lastScoreUpdateTime = currentTime;
            }
        }
    }

    public void UpdateMultiplier(float multiplier)
    {
        if (multiplierText == null) return;

        float currentTime = Time.time;
        float timeSinceLastUpdate = currentTime - lastMultiplierUpdateTime;
        
        if (timeSinceLastUpdate < 0.1f && lastMultiplierUpdateTime > 0f) return;

        string formattedText = "";
        Color textColor = Color.white;

        try
        {
            bool hasGameManager = GameManager.Instance != null;

            if (hasGameManager)
            {
                bool isTeddyBearAttached = GameManager.Instance.IsTeddyBearAttached();

                if (isTeddyBearAttached)
                {
                    textColor = cachedMultiplierFormatColor;
                    formattedText = string.Format(cachedMultiplierFormat, multiplier);
                }
                else
                {
                    textColor = cachedGeneralMultiplierFormatColor;
                    formattedText = string.Format(cachedGeneralMultiplierFormat, multiplier);
                }
            }
            else
            {
                textColor = cachedGeneralMultiplierFormatColor;
                formattedText = string.Format(cachedGeneralMultiplierFormat, multiplier);
            }
        }
        catch (System.Exception e)
        {
            textColor = cachedGeneralMultiplierFormatColor;
            formattedText = string.Format(cachedGeneralMultiplierFormat, multiplier);
        }

        if (formattedText != lastMultiplierText)
        {
            multiplierText.color = textColor;

            if (multiplierTextAnimator != null)
            {
                multiplierTextAnimator.SetText(formattedText);
                lastMultiplierText = formattedText;
                lastMultiplierUpdateTime = currentTime;
            }
            else
            {
                multiplierText.text = formattedText;
                lastMultiplierText = formattedText;
                lastMultiplierUpdateTime = currentTime;
            }
        }
    }

    public void UpdateGameTime(float time)
    {
        if (gameTimeText == null) return;

        float currentTime = Time.time;
        float timeSinceLastUpdate = currentTime - lastGameTimeUpdateTime;
        
        if (timeSinceLastUpdate < 0.1f && lastGameTimeUpdateTime > 0f) return;

        string formattedText = string.Format(cachedGameTimeFormat, time);

        if (formattedText != lastGameTimeText)
        {
            gameTimeText.color = cachedGameTimeFormatColor;

            if (gameTimeTextAnimator != null)
            {
                gameTimeTextAnimator.SetText(formattedText);
                lastGameTimeText = formattedText;
                lastGameTimeUpdateTime = currentTime;
            }
            else
            {
                gameTimeText.text = formattedText;
                lastGameTimeText = formattedText;
                lastGameTimeUpdateTime = currentTime;
            }
        }
    }

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

    public void SetSkillCooldown(int skillIndex, float cooldownTime)
    {
        UpdateSkillUI();
    }

    void UpdateSkillUI()
    {
        if (currentCharacterSkill != null)
        {
            UpdateSkillIconState();
            return;
        }

        LoadCharacterPrefabData();
        LoadSkillIconFromCharacterSkill();
        ConnectSkillIconToHUD();
        UpdateSkillIconState();
    }

    public void UpdateSkillCooldowns()
    {
        if (currentCharacterSkill != null)
        {
            UpdateSkillIconState();
            return;
        }

        SpawnController spawnController = FindObjectOfType<SpawnController>();
        if (spawnController != null)
        {
            currentCharacterSkill = spawnController.GetCurrentSpawnedCharacterSkill();
            if (currentCharacterSkill != null)
            {
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

    public void UpdateItemUI()
    {
        UpdateItemIcons();
    }

    private void UpdateItemIcons()
    {
        ItemController itemController = FindCurrentPlayerItemController();
        if (itemController == null)
        {
            ClearItemIcons();
            return;
        }

        try
        {
            Transform itemSlot = itemController.GetItemSlot1();
            if (itemSlot == null)
            {
                ClearItemIcons();
                return;
            }

            int itemCount = itemSlot.childCount;
            if (itemCount == 0)
            {
                ClearItemIcons();
                return;
            }

            if (itemCount >= 1)
            {
                Transform firstChild = itemSlot.GetChild(itemSlot.childCount - 1);
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

            if (itemCount >= 2)
            {
                Transform secondChild = itemSlot.GetChild(itemSlot.childCount - 2);
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
                ClearItemIcon(itemIcon2);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ HUDPanel - 아이템 아이콘 업데이트 중 오류: {e.Message}");
            ClearItemIcons();
        }
    }

    private void UpdateItemIcon(Image iconImage, Sprite skillIcon, Color skillColor, bool isActive)
    {
        if (iconImage == null) return;

        if (skillIcon == null)
        {
            iconImage.sprite = emptyItemIcon;
            iconImage.color = Color.white;
            iconImage.gameObject.SetActive(true);
            return;
        }

        iconImage.sprite = skillIcon;
        iconImage.color = isActive ? skillColor : Color.gray;
        iconImage.gameObject.SetActive(true);
    }

    private void ClearItemIcon(Image iconImage)
    {
        if (iconImage != null)
        {
            iconImage.sprite = emptyItemIcon;
            iconImage.color = Color.white;
            iconImage.gameObject.SetActive(true);
        }
    }

    private void ClearItemIcons()
    {
        ClearItemIcon(itemIcon1);
        ClearItemIcon(itemIcon2);
    }

    public void OpenItemUI()
    {
        if (itemModalWindow == null) return;
        if (!gameObject.activeSelf) return;
        if (!itemModalWindow.isOn)
        {
            TestShoot.SetIsShooting(false);
        
            isItemUIOpen = true;
            itemModalWindow.OpenWindow();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

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

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (!currentTeddyBear.IsAttached())
            {
                TestShoot.SetIsShooting(true);
            }
        }
    }

    public void ToggleItemUI()
    {
        if (isItemUIOpen)
            CloseItemUI();
        else
            OpenItemUI();
    }

    void UpdateRealTimeScoreStatus()
    {
        if (GameManager.Instance == null) return;

        bool isTeddyBearAttached = GameManager.Instance.IsTeddyBearAttached();

        if (isTeddyBearAttached)
        {
            UpdateScoreStatus("증가한 점수", 0f);
        }
        else
        {
            UpdateScoreStatus("기본 점수", 0f);
        }

        if (!isTeddyBearAttached)
        {
            float timeUntil = GameManager.Instance.GetTimeUntilReattach();
            UpdateAttachStatus(false, timeUntil);
        }
    }

    void UpdateRealTimeUI()
    {
        if (GameManager.Instance == null) return;

        float currentMultiplier = GameManager.Instance.GetScoreMultiplier();
        UpdateMultiplier(currentMultiplier);
    }

    void OnCharacterSpawned()
    {
        StartCoroutine(InitializeSkillSystemAfterSpawn());
    }

    IEnumerator InitializeSkillSystemAfterSpawn()
    {
        yield return new WaitForSeconds(0.1f);
        UpdateSkillDataFromSpawnedCharacter();
    }

    public void LoadCharacterPrefabData()
    {
        SpawnController spawnController = FindObjectOfType<SpawnController>();
        if (spawnController == null) return;

        int currentSpawnedCharacterIndex = spawnController.NotifyHUDOfCharacterSpawn();
        if (currentSpawnedCharacterIndex < 0) return;

        if (!dataBaseCached || DataBase.Instance == null) return;

        try
        {
            currentCharacterPrefab = DataBase.Instance.playerData.PlayerPrefabData[currentSpawnedCharacterIndex];
            if (currentCharacterPrefab == null) return;

            this.currentSpawnedCharacterIndex = currentSpawnedCharacterIndex;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ HUDPanel - 프리팹 정보 로드 중 오류: {e.Message}");
        }
    }

    public void LoadSkillIconFromCharacterSkill()
    {
        if (currentCharacterPrefab == null) return;

        try
        {
            SpawnController spawnController = FindObjectOfType<SpawnController>();
            if (spawnController == null) return;

            currentCharacterSkill = spawnController.GetCurrentSpawnedCharacterSkill();
            if (currentCharacterSkill == null) return;

            currentSkillIcon = currentCharacterSkill.SkillIcon;
            if (currentSkillIcon == null) return;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ HUDPanel - 스킬 아이콘 로드 중 오류: {e.Message}");
        }
    }

    public void ConnectSkillIconToHUD()
    {
        if (currentSkillIcon == null || skillIcon == null) return;

        try
        {
            skillIcon.sprite = currentSkillIcon;
            skillIcon.color = currentCharacterSkill.SkillColor;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ HUDPanel - 스킬 아이콘 HUD 연결 중 오류: {e.Message}");
        }
    }

    public void UpdateSkillIconState()
    {
        if (currentCharacterSkill == null || skillIcon == null) return;

        try
        {
            bool isOnCooldown = currentCharacterSkill.RemainingCooldown > 0f;
            skillIcon.color = isOnCooldown ? Color.gray : Color.white;

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

    public void UpdateSkillDataFromSpawnedCharacter()
    {
        LoadCharacterPrefabData();
        LoadSkillIconFromCharacterSkill();
        ConnectSkillIconToHUD();
        UpdateSkillIconState();
    }

    public void UpdateSkillDataFromCharacterIndex(int characterIndex)
    {
        if (!dataBaseCached || DataBase.Instance == null) return;

        try
        {
            currentCharacterPrefab = DataBase.Instance.playerData.PlayerPrefabData[characterIndex];
            if (currentCharacterPrefab == null) return;

            this.currentSpawnedCharacterIndex = characterIndex;

            LoadSkillIconFromCharacterSkill();
            ConnectSkillIconToHUD();
            UpdateSkillIconState();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ HUDPanel - 스킬 데이터 업데이트 중 오류: {e.Message}");
        }
    }

    public void UpdateSkillDataFromCharacterSkill(CharacterSkill characterSkill)
    {
        if (characterSkill == null) return;

        try
        {
            currentCharacterSkill = characterSkill;
            currentSkillIcon = characterSkill.SkillIcon;
            ConnectSkillIconToHUD();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ HUDPanel - 스킬 데이터 업데이트 중 오류: {e.Message}");
        }
    }

    public void SetHUDVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthRatio() => currentHealth / maxHealth;
    public bool IsItemUIOpen() => isItemUIOpen;

    public bool IsSkillReady(int skillIndex)
    {
        return true;
    }

    void FindTeddyBear()
    {
        if (currentTeddyBear == null)
        {
            currentTeddyBear = FindObjectOfType<TestTeddyBear>();
        }
    }

    public void UpdateCoin(int coinAmount)
    {
        if (coinText == null) return;

        float currentTime = Time.time;
        float timeSinceLastUpdate = currentTime - lastCoinUpdateTime;
        
        if (timeSinceLastUpdate < 0.1f && lastCoinUpdateTime > 0f) return;

        currentCoin = coinAmount;
        string formattedText = string.Format(cachedCoinFormat, currentCoin);

        if (formattedText != lastCoinText)
        {
            coinText.color = cachedCoinFormatColor;

            if (coinTextAnimator != null)
            {
                coinTextAnimator.SetText(formattedText);
                lastCoinText = formattedText;
                lastCoinUpdateTime = currentTime;
            }
            else
            {
                coinText.text = formattedText;
                lastCoinText = formattedText;
                lastCoinUpdateTime = currentTime;
            }
        }
    }

    private void UpdateCoinFromCurrentPlayer()
    {
        if (GameManager.Instance == null) return;

        CoinController coinController = GameManager.Instance.GetCurrentPlayerCoinController();
        if (coinController != null)
        {
            UpdateCoin(coinController.GetCurrentCoin());
        }
        else
        {
            UpdateCoin(0);
        }
    }

    private ItemController FindCurrentPlayerItemController()
    {
        if (GameManager.Instance == null) return null;

        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
        if (currentPlayer == null) return null;

        ItemController itemController = currentPlayer.GetComponent<ItemController>();
        if (itemController == null)
        {
            itemController = currentPlayer.GetComponentInChildren<ItemController>();
        }

        return itemController;
    }

    private void InitializeTextAnimator()
    {
        if (multiplierText != null)
        {
            multiplierTextAnimator = multiplierText.GetComponent<TextAnimator_TMP>();
            if (multiplierTextAnimator == null)
            {
                multiplierTextAnimator = multiplierText.gameObject.AddComponent<TextAnimator_TMP>();
            }
        }

        if (scoreText != null)
        {
            scoreTextAnimator = scoreText.GetComponent<TextAnimator_TMP>();
            if (scoreTextAnimator == null)
            {
                scoreTextAnimator = scoreText.gameObject.AddComponent<TextAnimator_TMP>();
            }
        }

        if (gameTimeText != null)
        {
            gameTimeTextAnimator = gameTimeText.GetComponent<TextAnimator_TMP>();
            if (gameTimeTextAnimator == null)
            {
                gameTimeTextAnimator = gameTimeText.gameObject.AddComponent<TextAnimator_TMP>();
            }
        }

        if (coinText != null)
        {
            coinTextAnimator = coinText.GetComponent<TextAnimator_TMP>();
            if (coinTextAnimator == null)
            {
                coinTextAnimator = coinText.gameObject.AddComponent<TextAnimator_TMP>();
            }
        }
    }

    public void RefreshAllTextMeshes()
    {
        RefreshMultiplierTextMesh();
        RefreshScoreTextMesh();
        RefreshGameTimeTextMesh();
        RefreshCoinTextMesh();
    }

    public void RefreshMultiplierTextMesh()
    {
        if (multiplierTextAnimator != null)
        {
            multiplierTextAnimator.ScheduleMeshRefresh();
        }
    }

    public void RefreshScoreTextMesh()
    {
        if (scoreTextAnimator != null)
        {
            scoreTextAnimator.ScheduleMeshRefresh();
        }
    }

    public void RefreshGameTimeTextMesh()
    {
        if (gameTimeTextAnimator != null)
        {
            gameTimeTextAnimator.ScheduleMeshRefresh();
        }
    }

    public void RefreshCoinTextMesh()
    {
        if (coinTextAnimator != null)
        {
            coinTextAnimator.ScheduleMeshRefresh();
        }
    }
} 
