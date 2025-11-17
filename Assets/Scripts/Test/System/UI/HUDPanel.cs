using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.Heat;
using Photon.Pun;
using Febucci.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using System.Xml.Schema;

/// <summary>
/// 로컬 플레이어의 기본 정보만을 표시하는 간단한 HUD
/// 다른 플레이어와 완전히 독립적으로 동작
/// </summary>
public class HUDPanel : MonoBehaviourPunCallbacks
{
    [Header("체력 UI")]
    [SerializeField] private ProgressBar healthProgressBar;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private ProgressBar healthProgressBGBar;
    
    [Header("점수 UI")]
    [SerializeField] private Image scoreIcon;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI scoreMultiplierText;
    
    [Header("코인 UI")]
    [SerializeField] private Image coinIcon;
    [SerializeField] private TextMeshProUGUI coinText;
    
    [Header("시간 UI")]
    [SerializeField] private TextMeshProUGUI gameTimeText;
    
    [Header("스킬 UI")]
    [SerializeField] private Image skillIcon;
    [SerializeField] private Image skillCooldownOverlay;
    [SerializeField] private TextMeshProUGUI skillCooldownText;
    
    [Header("아이템 UI")]
    [SerializeField] private Image itemIcon1;
    [SerializeField] private Image itemIcon2;
    [SerializeField] private Sprite emptyItemIcon;

    [Header("킬로그 UI")]
    [SerializeField] private GameObject killLogParent;
    [SerializeField] private GameObject killLogPrefab;

    [Header("점수판 UI")]
    [SerializeField] private GameObject scoreBoardParent;
    [SerializeField] private GameObject player1ScoreBoard;
    [SerializeField] private TextMeshProUGUI player1ScoreText;
    [SerializeField] private GameObject player2ScoreBoard;
    [SerializeField] private TextMeshProUGUI player2ScoreText;
    [SerializeField] private GameObject player3ScoreBoard;
    [SerializeField] private TextMeshProUGUI player3ScoreText;
    [SerializeField] private GameObject player4ScoreBoard;
    [SerializeField] private TextMeshProUGUI player4ScoreText;

    [Header("조준점 UI")]
    [SerializeField] private Animator zoomAnimator;
    [SerializeField] private Image zoomImage;

    [Header("장탄수 UI")]
    [SerializeField] private TextMeshProUGUI currentAmmoCountText;
    [SerializeField] private TextMeshProUGUI maxAmmoCountText;
    [SerializeField] private ProgressBar ammoBar;
    [SerializeField] private Image reloadIcon;
    [SerializeField] private Image ammoIcon;
    
    // 로컬 플레이어 참조
    private GameObject localPlayer;
    private LivingEntity localLivingEntity;
    private CoinController localCoinController;
    private Skill localCharacterSkill;
    private ItemController localItemController;
    private CameraController localCameraController;
    private TestGun localGun;
    
    // UI 상태
    private float currentHealth = 100f;
    private float maxHealth = 100f;
    private int currentCoin = 0;
    private float currentScore = 0f;
    
    // 체력 UI 애니메이션 관련
    private float previousHealth = 100f;
    private float targetHealth = 100f;
    private float displayedHealth = 100f;
    private bool isHealthAnimating = false;
    private bool isDamageAnimation = false;
    private bool isHealAnimation = false;
    private Tween healthBarTween;
    private Tween healthBGBarTween;
    private Tween healthBlinkTween;
    private Color originalBGColor = Color.white;
    private Color damageColor = Color.red;
    private Color healColor = new Color(0.5f, 0.8f, 1f, 1f); // 연한 파란색
    
    // 체력 바 페이드 관련
    private float lastHealthChangeTime = 0f;
    private bool isHealthBarFaded = false;
    private Tween healthBarFadeTween;
    private Tween healthBGBarFadeTween;
    private float healthFadeDelay = 3f; // 3초
    private Color originalHealthBarColor = Color.white;
    
    // 점수 아이콘 애니메이션 관련
    private Tween scoreIconShakeTween;
    
    // 코인 아이콘 애니메이션 관련
    private Tween coinIconRotateTween;
    private Tween coinIconScaleTween;
    
    // 시간 관련 (GameManager에서 받아옴)
    private float gameTime = 0f;
    private float lastTimeUpdate = 0f;
    
    // 스킬 관련
    private float lastSkillUpdate = 0f;
    
    // 아이템 관련
    private float lastItemUpdate = 0f;
    
    // 점수판 관련
    private List<PlayerScoreData> playerScoreDataList = new List<PlayerScoreData>();
    private List<GameObject> scoreBoardObjects = new List<GameObject>();
    private List<TextMeshProUGUI> scoreBoardTexts = new List<TextMeshProUGUI>();
    private float lastScoreBoardUpdate = 0f;
    private float scoreBoardUpdateInterval = 1f; // 1초마다 업데이트
    private bool isAnimating = false;

    //조준점 관련
    private bool isZoomed = false;
    
    // 장탄수 관련
    private int currentAmmo = 0;
    private int maxAmmo = 0;
    private int previousAmmo = 0;
    private float lastAmmoChangeTime = 0f;
    private bool isAmmoUIFaded = false;
    private bool isReloading = false;
    private TestGun.GunState previousGunState = TestGun.GunState.Ready;
    
    // 장탄수 애니메이션 관련
    private Tween ammoBarTween;
    private Tween ammoBarBlinkTween;
    private Tween ammoIconFadeTween;
    private Tween ammoBarImageFadeTween;
    private Tween currentAmmoTextFadeTween;
    private Tween maxAmmoTextFadeTween;
    private Tween currentAmmoTextBlinkTween;
    private Tween reloadIconFadeTween;
    private Tween reloadIconRotateTween;
    private Tween reloadIconBlinkTween;
    private Tween zoomImageFadeTween; // ✅ zoomImage 페이드 애니메이션
    private Color originalAmmoBarColor = Color.white;
    private Color originalAmmoTextColor = Color.white;
    private Color lowAmmoColor = Color.red;
    private float lowAmmoThreshold = 0.2f; // 20%
    private float ammoUIFadeDelay = 3f; // 3초
    private float originalZoomImageAlpha = 1f; // ✅ zoomImage 원래 투명도
    
    // 성능 최적화 관련
    private List<PlayerScoreData> previousPlayerDataList = new List<PlayerScoreData>();
    private bool hasScoreChanged = false;
    private float lastAmmoUpdate = 0f;
    
    // ✅ 게임 종료 카운트다운 사운드 관련
    private bool isCountdownSoundPlaying = false;
    private int lastCountdownSecond = -1;
    
    /// <summary>
    /// 플레이어 점수 Properties 초기화 (두 번째 게임 문제 해결)
    /// </summary>
    private void ClearPlayerScoreProperties()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null) return;
        
        // 로컬 플레이어의 점수 관련 Properties 초기화
        var props = new ExitGames.Client.Photon.Hashtable();
        props[$"score_{PhotonNetwork.LocalPlayer.ActorNumber}"] = null;
        props["nickname"] = null; // 닉네임도 초기화하여 재설정되도록
        
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        
        Debug.Log($"HUDPanel: 플레이어 {PhotonNetwork.LocalPlayer.ActorNumber} 점수 Properties 초기화");
    }
    
    void Start()
    {
        // 게임 재시작 시 점수 Properties 초기화 (핵심 수정)
        ClearPlayerScoreProperties();
        
        // 로컬 플레이어 찾기 시작
        StartCoroutine(FindLocalPlayerRoutine());
        
        // GameManager 이벤트 구독 (시간 정보만)
        if (GameManager.Instance != null)
        {
            GameManager.OnGameTimeUpdated += UpdateGameTime;
            GameManager.OnScoreUpdated += OnScoreChanged;
        }
        
        // LivingEntity 사망 이벤트 구독
        LivingEntity.OnPlayerDied += HandlePlayerDeath;
        Debug.Log("HUD: LivingEntity.OnPlayerDied 이벤트 구독 완료");
        
        // 점수판 초기화
        InitializeScoreBoard();

    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.OnGameTimeUpdated -= UpdateGameTime;
            GameManager.OnScoreUpdated -= OnScoreChanged;
        }
        
        // LivingEntity 사망 이벤트 구독 해제
        LivingEntity.OnPlayerDied -= HandlePlayerDeath;
        Debug.Log("HUD: LivingEntity.OnPlayerDied 이벤트 구독 해제 완료");
        
        // 체력 애니메이션 정리
        CleanupHealthAnimations();
        CleanupFadeAnimations();
        
        // 장탄수 애니메이션 정리
        CleanupAmmoAnimations();
        
        // 아이콘 애니메이션 정리
        CleanupIconAnimations();
    }
    
    /// <summary>
    /// Photon 플레이어 프로퍼티 변경 시 호출 (PunCallbacks)
    /// </summary>
    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // 점수 관련 프로퍼티가 변경되었는지 확인
        foreach (var prop in changedProps)
        {
            if (prop.Key.ToString().StartsWith("score_"))
            {
                ForceUpdateScoreBoard();
                break;
            }
        }
    }
    
    void Update()
    {
        // 로컬 플레이어가 없으면 찾기 시도
        if (localPlayer == null)
        {
            return;
        }
        
        // 실시간 업데이트 (0.1초마다)
        float currentTime = Time.time;
        
        // 체력 업데이트
        if (currentTime - lastTimeUpdate > 0.1f)
        {
            UpdateHealth();
            UpdateCoin(); // 코인도 자동 업데이트
            UpdateScore(); // 점수도 자동 업데이트
            lastTimeUpdate = currentTime;
        }
        
        // 스킬 상태 업데이트
        if (currentTime - lastSkillUpdate > 0.1f)
        {
            UpdateSkillUI();
            lastSkillUpdate = currentTime;
        }
        
        // 아이템 UI 업데이트
        if (currentTime - lastItemUpdate > 0.1f)
        {
            UpdateItemUI();
            lastItemUpdate = currentTime;
        }
        
        // 장탄수 UI 업데이트
        if (currentTime - lastAmmoUpdate > 0.1f)
        {
            UpdateAmmoUI();
            lastAmmoUpdate = currentTime;
        }
        
        // 점수판 업데이트 (1초마다)
        if (currentTime - lastScoreBoardUpdate > scoreBoardUpdateInterval)
        {
            UpdateScoreBoard();
            lastScoreBoardUpdate = currentTime;
        }

        ZoomAnimationControl();
        
        // 체력 바 페이드 체크
        CheckHealthBarFade();
        
        // 장탄수 UI 페이드 체크
        CheckAmmoUIFade();
        
        // ✅ 게임 종료 10초 전 카운트다운 사운드 체크
        CheckGameEndingCountdownSound();
    }
    
    /// <summary>
    /// 로컬 플레이어를 찾는 코루틴
    /// </summary>
    IEnumerator FindLocalPlayerRoutine()
    {
        float searchTime = 0f;
        float maxSearchTime = 10f; // 최대 10초 검색
        
        while (localPlayer == null && searchTime < maxSearchTime)
        {
            FindLocalPlayer();
            
            if (localPlayer == null)
            {
                yield return new WaitForSeconds(0.5f);
                searchTime += 0.5f;
            }
        }
        
        if (localPlayer != null)
        {
            Debug.Log($"HUD: 로컬 플레이어 연결 완료 - {localPlayer.name}");
            InitializeHUD();
        }
        else
        {
            Debug.LogError("HUD: 로컬 플레이어를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 로컬 플레이어 찾기
    /// </summary>
    void FindLocalPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                localPlayer = player;
                localLivingEntity = player.GetComponent<LivingEntity>();
                localCoinController = player.GetComponent<CoinController>();
                localCharacterSkill = player.GetComponent<Skill>();
                localItemController = player.GetComponent<ItemController>();
                localCameraController = player.GetComponent<CameraController>();
                localGun = player.GetComponentInChildren<TestGun>();
                break;
            }
        }
    }
    
    /// <summary>
    /// HUD 초기화
    /// </summary>
    void InitializeHUD()
    {
        if (localLivingEntity != null)
        {
            currentHealth = localLivingEntity.CurrentHealth;
            maxHealth = localLivingEntity.StartingHealth;
            previousHealth = currentHealth;
            targetHealth = currentHealth;
            displayedHealth = currentHealth;
            
            // 배경 바의 원래 색상 저장
            if (healthProgressBGBar != null && healthProgressBGBar.barImage != null)
            {
                originalBGColor = healthProgressBGBar.barImage.color;
            }
            
            // 체력 바의 원래 색상 저장
            if (healthProgressBar != null && healthProgressBar.barImage != null)
            {
                originalHealthBarColor = healthProgressBar.barImage.color;
            }
            
            // 체력 변화 시간 초기화
            lastHealthChangeTime = Time.time;
            
            UpdateHealthDisplay();
        }
        
        if (localCoinController != null)
        {
            currentCoin = localCoinController.GetCurrentCoin();
            currentScore = localCoinController.GetCurrentScore(); // ✅ 점수 초기화
            UpdateCoinDisplay();
        }
        else
        {
            // ✅ CoinController가 없으면 0으로 초기화
            currentCoin = 0;
            currentScore = 0f;
        }
        
        if (localCharacterSkill != null)
        {
            UpdateSkillDisplay();
        }
        
        // 초기 점수 표시 (강제 업데이트)
        UpdateScoreDisplay();

        // 초기 아이템 UI 표시
        UpdateItemUI();
        
        // 초기 장탄수 UI 설정
        if (localGun != null)
        {
            InitializeAmmoUI();
        }
       
    }
    
    /// <summary>
    /// 체력 업데이트
    /// </summary>
    void UpdateHealth()
    {
        if (localLivingEntity == null) return;
        
        float newHealth = localLivingEntity.CurrentHealth;
        float newMaxHealth = localLivingEntity.StartingHealth;
        
        // 최대 체력 변경 처리
        if (Mathf.Abs(newMaxHealth - maxHealth) > 0.1f)
        {
            maxHealth = newMaxHealth;
        }
        
        // 체력 변경 감지 및 애니메이션 처리
        if (Mathf.Abs(newHealth - currentHealth) > 0.1f)
        {
            previousHealth = currentHealth;
            currentHealth = newHealth;
            targetHealth = newHealth;
            
            // 체력 변화 시간 기록
            lastHealthChangeTime = Time.time;
            
            // 페이드된 상태라면 원래 투명도로 복원
            if (isHealthBarFaded)
            {
                RestoreHealthBarVisibility();
            }
            
            // 체력 변화에 따른 애니메이션 시작
            StartHealthAnimation();
        }
    }
    
    /// <summary>
    /// 체력 UI 업데이트 (즉시 업데이트용)
    /// </summary>
    void UpdateHealthDisplay()
    {
        if (healthProgressBar != null)
        {
            healthProgressBar.currentValue = displayedHealth;
            healthProgressBar.maxValue = maxHealth;
            healthProgressBar.UpdateUI();
        }
        
        if (healthProgressBGBar != null)
        {
            healthProgressBGBar.maxValue = maxHealth;
            healthProgressBGBar.UpdateUI();
        }
        
        if (healthText != null)
        {
            healthText.text = $"{currentHealth:F0}";
        }
    }
    
    /// <summary>
    /// 체력 애니메이션 시작
    /// </summary>
    void StartHealthAnimation()
    {
        // 기존 애니메이션 정리
        CleanupHealthAnimations();
        
        float healthDifference = targetHealth - previousHealth;
        
        if (healthDifference < 0) // 데미지
        {
            StartDamageAnimation(Mathf.Abs(healthDifference));
        }
        else if (healthDifference > 0) // 회복
        {
            StartHealAnimation(healthDifference);
        }
    }
    
    /// <summary>
    /// 데미지 애니메이션 시작
    /// </summary>
    void StartDamageAnimation(float damageAmount)
    {
        if (isHealAnimation)
        {
            // 회복 중 데미지 처리
            HandleDamageInterruptHeal(damageAmount);
            return;
        }
        
        isDamageAnimation = true;
        isHealthAnimating = true;
        
        // 배경 바 색상을 빨간색으로 변경
        if (healthProgressBGBar != null && healthProgressBGBar.barImage != null)
        {
            healthProgressBGBar.barImage.color = damageColor;
            healthProgressBGBar.currentValue = previousHealth;
            healthProgressBGBar.UpdateUI();
        }
        
        // 빨간색 깜박임 시작 (강한 깜박임)
        StartHealthBlink(damageColor, 0.3f);
        
        // 체력 바 부드럽게 감소
        healthBarTween = DOTween.To(() => displayedHealth, x => {
            displayedHealth = x;
            if (healthProgressBar != null)
            {
                healthProgressBar.currentValue = displayedHealth;
                healthProgressBar.UpdateUI();
            }
        }, targetHealth, 0.8f)
        .SetEase(Ease.OutCubic)
        .OnComplete(() => {
            // 체력 바 감소 완료 후 배경 바도 감소
            StartBackgroundBarDecrease();
        });
    }
    
    /// <summary>
    /// 회복 애니메이션 시작
    /// </summary>
    void StartHealAnimation(float healAmount)
    {
        if (isDamageAnimation)
        {
            // 데미지 중 회복 처리
            HandleHealInterruptDamage(healAmount);
            return;
        }
        
        isHealAnimation = true;
        isHealthAnimating = true;
        
        // 배경 바 색상을 연한 파란색으로 변경하고 회복 지점까지 증가
        if (healthProgressBGBar != null && healthProgressBGBar.barImage != null)
        {
            healthProgressBGBar.barImage.color = healColor;
            
            // 배경 바를 회복 지점까지 즉시 증가
            healthProgressBGBar.currentValue = targetHealth;
            healthProgressBGBar.UpdateUI();
        }
        
        // 파란색 부드러운 깜박임 시작
        StartHealthBlink(healColor, 0.5f);
        
        // 0.3초 후 체력 바 증가 시작
        DOVirtual.DelayedCall(0.3f, () => {
            healthBarTween = DOTween.To(() => displayedHealth, x => {
                displayedHealth = x;
                if (healthProgressBar != null)
                {
                    healthProgressBar.currentValue = displayedHealth;
                    healthProgressBar.UpdateUI();
                }
            }, targetHealth, 0.6f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => {
                // 회복 완료
                CompleteHealAnimation();
            });
        });
    }
    
    /// <summary>
    /// 배경 바 감소 애니메이션
    /// </summary>
    void StartBackgroundBarDecrease()
    {
        if (healthProgressBGBar != null)
        {
            healthBGBarTween = DOTween.To(() => healthProgressBGBar.currentValue, x => {
                healthProgressBGBar.currentValue = x;
                healthProgressBGBar.UpdateUI();
            }, targetHealth, 0.6f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => {
                // 데미지 애니메이션 완료
                CompleteDamageAnimation();
            });
        }
        else
        {
            CompleteDamageAnimation();
        }
    }
    
    /// <summary>
    /// 체력 깜박임 효과
    /// </summary>
    void StartHealthBlink(Color blinkColor, float blinkSpeed)
    {
        if (healthProgressBGBar != null && healthProgressBGBar.barImage != null)
        {
            var image = healthProgressBGBar.barImage;
            
            healthBlinkTween = DOTween.Sequence()
                .Append(image.DOFade(0f, blinkSpeed))
                .Append(image.DOFade(150f / 255f, blinkSpeed))
                .SetLoops(-1, LoopType.Yoyo);
        }
    }
    
    /// <summary>
    /// 회복 중 데미지 인터럽트 처리
    /// </summary>
    void HandleDamageInterruptHeal(float damageAmount)
    {
        float currentBGValue = healthProgressBGBar != null ? healthProgressBGBar.currentValue : displayedHealth;
        float healAmount = currentBGValue - displayedHealth;
        
        if (damageAmount < healAmount)
        {
            // 데미지가 회복량보다 작음 - 배경 바만 줄이기
            float newBGValue = currentBGValue - damageAmount;
            targetHealth = currentHealth; // 실제 체력으로 업데이트
            
            if (healthProgressBGBar != null)
            {
                healthBGBarTween?.Kill();
                healthBGBarTween = DOTween.To(() => healthProgressBGBar.currentValue, x => {
                    healthProgressBGBar.currentValue = x;
                    healthProgressBGBar.UpdateUI();
                }, newBGValue, 0.4f)
                .SetEase(Ease.OutCubic);
            }
        }
        else
        {
            // 데미지가 회복량보다 큼 - 데미지 애니메이션으로 전환
            CleanupHealthAnimations();
            isHealAnimation = false;
            StartDamageAnimation(damageAmount - healAmount);
        }
    }
    
    /// <summary>
    /// 데미지 중 회복 인터럽트 처리
    /// </summary>
    void HandleHealInterruptDamage(float healAmount)
    {
        float currentBGValue = healthProgressBGBar != null ? healthProgressBGBar.currentValue : displayedHealth;
        float damageAmount = displayedHealth - currentBGValue;
        
        if (healAmount < damageAmount)
        {
            // 회복량이 데미지보다 작음 - 배경 바만 증가
            float newBGValue = currentBGValue + healAmount;
            targetHealth = currentHealth; // 실제 체력으로 업데이트
            
            if (healthProgressBGBar != null)
            {
                healthBGBarTween?.Kill();
                healthBGBarTween = DOTween.To(() => healthProgressBGBar.currentValue, x => {
                    healthProgressBGBar.currentValue = x;
                    healthProgressBGBar.UpdateUI();
                }, newBGValue, 0.4f)
                .SetEase(Ease.OutCubic);
            }
        }
        else
        {
            // 회복량이 데미지보다 큼 - 회복 애니메이션으로 전환
            CleanupHealthAnimations();
            isDamageAnimation = false;
            StartHealAnimation(healAmount - damageAmount);
        }
    }
    
    /// <summary>
    /// 데미지 애니메이션 완료
    /// </summary>
    void CompleteDamageAnimation()
    {
        isDamageAnimation = false;
        isHealthAnimating = false;
        
        // 깜박임 정지 및 원래 색상으로 복원
        healthBlinkTween?.Kill();
        if (healthProgressBGBar != null && healthProgressBGBar.barImage != null)
        {
            healthProgressBGBar.barImage.color = originalBGColor;
            healthProgressBGBar.barImage.DOFade(originalBGColor.a, 0.3f);
        }
        
        // 체력 변화 시간 업데이트 (페이드 타이머 리셋)
        lastHealthChangeTime = Time.time;
    }
    
    /// <summary>
    /// 회복 애니메이션 완료
    /// </summary>
    void CompleteHealAnimation()
    {
        isHealAnimation = false;
        isHealthAnimating = false;
        
        // 깜박임 정지 및 원래 색상으로 복원
        healthBlinkTween?.Kill();
        if (healthProgressBGBar != null && healthProgressBGBar.barImage != null)
        {
            healthProgressBGBar.barImage.color = originalBGColor;
            healthProgressBGBar.barImage.DOFade(originalBGColor.a, 0.3f);
        }
        
        // 체력 변화 시간 업데이트 (페이드 타이머 리셋)
        lastHealthChangeTime = Time.time;
    }
    
    /// <summary>
    /// 체력 애니메이션 정리
    /// </summary>
    void CleanupHealthAnimations()
    {
        healthBarTween?.Kill();
        healthBGBarTween?.Kill();
        healthBlinkTween?.Kill();
        
        healthBarTween = null;
        healthBGBarTween = null;
        healthBlinkTween = null;
    }
    
    /// <summary>
    /// 체력 바 페이드 체크
    /// </summary>
    void CheckHealthBarFade()
    {
        // 애니메이션 중이거나 이미 페이드된 상태면 체크하지 않음
        if (isHealthAnimating || isHealthBarFaded) return;
        
        // 3초간 체력 변화가 없었는지 확인
        if (Time.time - lastHealthChangeTime >= healthFadeDelay)
        {
            FadeHealthBars();
        }
    }
    
    /// <summary>
    /// 체력 바들을 페이드 아웃
    /// </summary>
    void FadeHealthBars()
    {
        if (isHealthBarFaded) return;
        
        isHealthBarFaded = true;
        
        // 체력 바를 투명도 100 (약 39%)으로 페이드
        if (healthProgressBar != null && healthProgressBar.barImage != null)
        {
            healthBarFadeTween = healthProgressBar.barImage.DOFade(100f / 255f, 0.5f)
                .SetEase(Ease.OutCubic);
        }
        
        // 배경 바를 투명도 0으로 페이드
        if (healthProgressBGBar != null && healthProgressBGBar.barImage != null)
        {
            healthBGBarFadeTween = healthProgressBGBar.barImage.DOFade(0f, 0.5f)
                .SetEase(Ease.OutCubic);
        }
    }
    
    /// <summary>
    /// 체력 바 투명도를 원래대로 복원
    /// </summary>
    void RestoreHealthBarVisibility()
    {
        if (!isHealthBarFaded) return;
        
        isHealthBarFaded = false;
        
        // 페이드 애니메이션 정리
        healthBarFadeTween?.Kill();
        healthBGBarFadeTween?.Kill();
        
        // 체력 바를 원래 투명도로 복원
        if (healthProgressBar != null && healthProgressBar.barImage != null)
        {
            healthBarFadeTween = healthProgressBar.barImage.DOFade(originalHealthBarColor.a, 0.3f)
                .SetEase(Ease.OutCubic);
        }
        
        // 배경 바를 원래 투명도로 복원
        if (healthProgressBGBar != null && healthProgressBGBar.barImage != null)
        {
            healthBGBarFadeTween = healthProgressBGBar.barImage.DOFade(originalBGColor.a, 0.3f)
                .SetEase(Ease.OutCubic);
        }
    }
    
    /// <summary>
    /// 페이드 애니메이션 정리
    /// </summary>
    void CleanupFadeAnimations()
    {
        healthBarFadeTween?.Kill();
        healthBGBarFadeTween?.Kill();
        
        healthBarFadeTween = null;
        healthBGBarFadeTween = null;
    }

    
    /// <summary>
    /// 코인 자동 업데이트 (로컬 플레이어에서 직접 가져옴)
    /// </summary>
    void UpdateCoin()
    {
        if (localCoinController == null) return;
        
        int newCoin = localCoinController.GetCurrentCoin();
        if (newCoin != currentCoin)
        {
            currentCoin = newCoin;
            UpdateCoinDisplay();
            
            // 코인 아이콘 회전 애니메이션 실행
            PlayCoinIconRotateAnimation();
        }
    }
    
    /// <summary>
    /// 코인 업데이트 (외부 호출용)
    /// </summary>
    public void UpdateCoin(int coinAmount)
    {
        if (Mathf.Abs(coinAmount - currentCoin) > 0.1f)
        {
            currentCoin = coinAmount;
            UpdateCoinDisplay();
        }
    }
    
    /// <summary>
    /// 코인 UI 업데이트
    /// </summary>
    void UpdateCoinDisplay()
    {
        if (coinText != null)
        {
            coinText.text = $"{currentCoin}";
        }
    }
    
    /// <summary>
    /// 점수 자동 업데이트 (로컬 플레이어에서 직접 가져옴)
    /// </summary>
    void UpdateScore()
    {
        if (localCoinController == null) return;
        
        float newScore = localCoinController.GetCurrentScore();
        if (Mathf.Abs(newScore - currentScore) > 0.1f)
        {
            float previousScore = currentScore;
            currentScore = newScore;
            UpdateScoreDisplay();
            
            // 점수 아이콘 진동 애니메이션 실행
            PlayScoreIconShakeAnimation();
            
            // 점수가 변경되었을 때 네트워크 동기화
            if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
            {
                SyncPlayerScoreToNetwork(PhotonNetwork.LocalPlayer.ActorNumber, newScore);
            }
        }
    }
    
    /// <summary>
    /// 점수 UI 업데이트
    /// </summary>
    void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{currentScore:F0}";
        }
        else
        {
            scoreText.text = "0";
        }
    }


    void UpdateScoreMultiplier()
    {
        if (localCoinController == null) return;

        if (localCoinController.GetIsTeddyBearAttached())
        {
            UpdateScoreMultiplierDisplay();
        }
        else
        {
            scoreMultiplierText.text = "";
        }
    }

    void UpdateScoreMultiplierDisplay()
    {
        if (scoreMultiplierText != null)
        {
            scoreMultiplierText.text = $"<wave>점수 2배!</wave>";
        }
    }
    
    /// <summary>
    /// 게임 시간 업데이트 (GameManager에서 받아옴)
    /// </summary>
    void UpdateGameTime(float time)
    {
        gameTime = time;
        UpdateTimeDisplay();
    }
    
    /// <summary>
    /// 시간 UI 업데이트
    /// </summary>
    void UpdateTimeDisplay()
    {
        if (gameTimeText != null)
        {
            int minutes = Mathf.FloorToInt(gameTime / 60f);
            int seconds = Mathf.FloorToInt(gameTime % 60f);
            gameTimeText.text = $"{minutes:00}:{seconds:00}";
        }
    }
    
    /// <summary>
    /// 스킬 UI 업데이트
    /// </summary>
    void UpdateSkillUI()
    {
        if (localCharacterSkill == null) return;
        
        // 스킬 아이콘 설정
        if (skillIcon != null && localCharacterSkill.SkillIcon != null)
        {
            skillIcon.sprite = localCharacterSkill.SkillIcon;
            skillIcon.color = localCharacterSkill.SkillColor;
        }
        
        UpdateSkillState();
    }
    
    /// <summary>
    /// 스킬 상태 업데이트
    /// </summary>
    void UpdateSkillState()
    {
        if (localCharacterSkill == null || skillIcon == null) return;
        
        bool isOnCooldown = localCharacterSkill.RemainingCooldown > 0f;
        skillIcon.color = isOnCooldown ? Color.gray : Color.white;
        
        // 쿨다운 오버레이
        if (skillCooldownOverlay != null)
        {
            if (isOnCooldown && localCharacterSkill.Cooldown > 0f)
            {
                float fillAmount = localCharacterSkill.RemainingCooldown / localCharacterSkill.Cooldown;
                skillCooldownOverlay.fillAmount = fillAmount;
                skillCooldownOverlay.gameObject.SetActive(true);
            }
            else
            {
                skillCooldownOverlay.gameObject.SetActive(false);
            }
        }
        
        // 쿨다운 텍스트
        if (skillCooldownText != null)
        {
            if (isOnCooldown)
            {
                skillCooldownText.text = localCharacterSkill.RemainingCooldown.ToString("F1");
                skillCooldownText.gameObject.SetActive(true);
            }
            else
            {
                skillCooldownText.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 스킬 표시 초기화
    /// </summary>
    void UpdateSkillDisplay()
    {
        if (localCharacterSkill == null) return;
        
        if (skillIcon != null && localCharacterSkill.SkillIcon != null)
        {
            skillIcon.sprite = localCharacterSkill.SkillIcon;
            skillIcon.color = localCharacterSkill.SkillColor;
        }
        
        UpdateSkillState();
    }
    

    
    /// <summary>
    /// 외부에서 호출 가능한 메서드들
    /// </summary>
    public void RefreshHUD()
    {
        if (localPlayer != null)
        {
            InitializeHUD();
        }
    }
    
    public void SetHUDVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
    
    // Getter 메서드들
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public int GetCurrentCoin() => currentCoin;
    public float GetCurrentScore() => currentScore;
    
    /// <summary>
    /// 현재 점수판 데이터 리스트 반환 (GameOverController용)
    /// </summary>
    public List<PlayerScoreData> GetPlayerScoreDataList()
    {
        // 최신 점수판 데이터 수집
        CollectAllPlayersData();
        SortPlayersByScore();
        
        return playerScoreDataList;
    }
    
    /// <summary>
    /// 외부 호환성을 위한 메서드들
    /// </summary>
    
    // 아이템 UI 업데이트 
    public void UpdateItemUI()
    {
        if (localItemController == null) return;
        
        try
        {
            Transform itemSlot = localItemController.GetItemSlot1();
            if (itemSlot == null)
            {
                ClearItemIcons();
                return;
            }
            
            int itemCount = itemSlot.childCount;
            
            // 첫 번째 아이템
            if (itemCount >= 1)
            {
                Transform firstChild = itemSlot.GetChild(itemCount - 1);
                if (firstChild != null)
                {
                    Skill firstItem = firstChild.GetComponent<Skill>();
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
            }
            else
            {
                ClearItemIcon(itemIcon1);
            }
            
            // 두 번째 아이템
            if (itemCount >= 2)
            {
                Transform secondChild = itemSlot.GetChild(itemCount - 2);
                if (secondChild != null)
                {
                    Skill secondItem = secondChild.GetComponent<Skill>();
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
            }
            else
            {
                ClearItemIcon(itemIcon2);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HUD: 아이템 UI 업데이트 중 오류 - {e.Message}");
            ClearItemIcons();
        }
    }
    
    // 스킬 데이터 업데이트 (외부 호출용)
    public void UpdateSkillDataFromSpawnedCharacter()
    {
        if (localCharacterSkill != null)
        {
            UpdateSkillDisplay();
        }
    }
    
    // 아이템 아이콘 업데이트 헬퍼 메서드
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
    
    // 아이템 아이콘 클리어 헬퍼 메서드
    private void ClearItemIcon(Image iconImage)
    {
        if (iconImage != null)
        {
            iconImage.sprite = emptyItemIcon;
            iconImage.color = Color.white;
            iconImage.gameObject.SetActive(true);
        }
    }
    
    // 모든 아이템 아이콘 클리어
    private void ClearItemIcons()
    {
        ClearItemIcon(itemIcon1);
        ClearItemIcon(itemIcon2);
    }




    private void HandlePlayerDeath(LivingEntity victim)
    {
        if (victim == null) return;

        LivingEntity attacker = victim.GetAttacker();
        if (attacker != null)
        {
            // 모든 클라이언트에서 킬로그 생성
        GameObject killLog = Instantiate(killLogPrefab, killLogParent.transform);
            Debug.Log($"HUD: 킬로그 생성 - {killLog.name}");
            
     
        QuestItem questItem = killLog.GetComponent<QuestItem>();

            Photon.Realtime.Player attackerPlayer = attacker.photonView.Owner;
            Photon.Realtime.Player victimPlayer = victim.photonView.Owner;

            string attackerNickname = GetPlayerNickname(attackerPlayer);
            string victimNickname = GetPlayerNickname(victimPlayer);
            
            // 킬로그 텍스트 설정
            questItem.questText = $"{attackerNickname}       {victimNickname}";
            questItem.UpdateUI();

            // Animate quest
            questItem.AnimateQuest();


        }
    }
    
    private IEnumerator DestroyKillLogAfterDelay(GameObject killLog, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (killLog != null)
        {
            Debug.Log($"HUD: 킬로그 제거 - {killLog.name}");
            Destroy(killLog);
        }
    }




    #region 점수판 관련 메서드
    
    /// <summary>
    /// 점수판 초기화
    /// </summary>
    private void InitializeScoreBoard()
    {
        // 점수판 UI 요소들을 리스트에 추가
        scoreBoardObjects.Clear();
        scoreBoardTexts.Clear();
        
        if (player1ScoreBoard != null)
        {
            scoreBoardObjects.Add(player1ScoreBoard);
            scoreBoardTexts.Add(player1ScoreText);
        }
        if (player2ScoreBoard != null)
        {
            scoreBoardObjects.Add(player2ScoreBoard);
            scoreBoardTexts.Add(player2ScoreText);
        }
        if (player3ScoreBoard != null)
        {
            scoreBoardObjects.Add(player3ScoreBoard);
            scoreBoardTexts.Add(player3ScoreText);
        }
        if (player4ScoreBoard != null)
        {
            scoreBoardObjects.Add(player4ScoreBoard);
            scoreBoardTexts.Add(player4ScoreText);
        }
        
        // 초기에는 모든 점수판을 비활성화
        foreach (var scoreBoard in scoreBoardObjects)
        {
            if (scoreBoard != null)
                scoreBoard.SetActive(false);
        }
        
    }
    
    /// <summary>
    /// 점수판 업데이트 (메인 메서드)
    /// </summary>
    public void UpdateScoreBoard()
    {
        if (isAnimating) return; // 애니메이션 중이면 업데이트 건너뛰기
        
        // 현재 방의 모든 플레이어 데이터 수집
        CollectAllPlayersData();
        
        // 변경 사항 확인 (성능 최적화)
        if (!HasPlayerDataChanged())
        {
            return; // 변경사항이 없으면 업데이트 건너뛰기
        }
        
        // 점수 기준으로 정렬
        SortPlayersByScore();
        
        // UI 업데이트
        UpdateScoreBoardUI();
        
        // 순위 변경이 있다면 애니메이션 실행
        CheckAndAnimateRankingChanges();
        
        // 현재 데이터를 이전 데이터로 저장
        SaveCurrentDataAsPrevious();
    }
    
    /// <summary>
    /// 플레이어 데이터가 변경되었는지 확인 (성능 최적화)
    /// </summary>
    private bool HasPlayerDataChanged()
    {
        // 플레이어 수가 다르면 변경됨
        if (playerScoreDataList.Count != previousPlayerDataList.Count)
        {
            return true;
        }
        
        // 각 플레이어의 점수나 닉네임이 변경되었는지 확인
        for (int i = 0; i < playerScoreDataList.Count; i++)
        {
            if (i >= previousPlayerDataList.Count)
            {
                return true;
            }
            
            var current = playerScoreDataList[i];
            var previous = previousPlayerDataList[i];
            
            // 플레이어 ID, 점수, 닉네임 중 하나라도 다르면 변경됨
            if (current.playerId != previous.playerId ||
                Mathf.Abs(current.score - previous.score) > 0.1f ||
                current.nickname != previous.nickname)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 현재 데이터를 이전 데이터로 저장
    /// </summary>
    private void SaveCurrentDataAsPrevious()
    {
        previousPlayerDataList.Clear();
        foreach (var playerData in playerScoreDataList)
        {
            previousPlayerDataList.Add(new PlayerScoreData(
                playerData.playerId,
                playerData.nickname,
                playerData.score,
                playerData.isLocalPlayer,
                playerData.playerPhotonView
            ));
        }
    }
    
    /// <summary>
    /// 모든 플레이어의 데이터를 수집
    /// </summary>
    private void CollectAllPlayersData()
    {
        playerScoreDataList.Clear();
        
        // Photon 네트워크의 모든 플레이어 가져오기
        var allPlayers = PhotonNetwork.PlayerList;
        
        
        foreach (var player in allPlayers)
        {
            // 플레이어의 GameObject 찾기
            GameObject playerObject = FindPlayerObjectByPhotonPlayer(player);
            if (playerObject == null) 
            {
                continue;
            }
            
            // 점수 가져오기
            float playerScore = GetPlayerScore(playerObject);
            
            // 닉네임 가져오기
            string nickname = GetPlayerNickname(player);
            
            // PhotonView 가져오기
            PhotonView pv = playerObject.GetComponent<PhotonView>();
            
            // 로컬 플레이어인지 확인
            bool isLocal = pv != null && pv.IsMine;
            
            // 플레이어 데이터 생성
            PlayerScoreData playerData = new PlayerScoreData(
                player.ActorNumber,
                nickname,
                playerScore,
                isLocal,
                pv
            );
            
            playerScoreDataList.Add(playerData); 
        }
    }
    
    /// <summary>
    /// PhotonPlayer로부터 해당하는 GameObject 찾기
    /// </summary>
    private GameObject FindPlayerObjectByPhotonPlayer(Photon.Realtime.Player player)
    {
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject playerObj in playerObjects)
        {
            PhotonView pv = playerObj.GetComponent<PhotonView>();
            if (pv != null && pv.Owner.ActorNumber == player.ActorNumber)
            {
                return playerObj;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 플레이어의 점수 가져오기 (네트워크 동기화 우선)
    /// </summary>
    private float GetPlayerScore(GameObject playerObject)
    {
        PhotonView pv = playerObject.GetComponent<PhotonView>();
        if (pv != null && pv.Owner != null)
        {
            // 로컬 플레이어인 경우 CoinController에서 직접 가져오기
            if (pv.IsMine)
            {
                CoinController coinController = playerObject.GetComponent<CoinController>();
                if (coinController != null)
                {
                    float localScore = coinController.GetCurrentScore();
                    return localScore;
                }
            }
            else
            {
                // 원격 플레이어인 경우 네트워크에서 가져오기
                float networkScore = GetPlayerScoreFromNetwork(pv.Owner);
                return networkScore;
            }
        }
        
        return 0f;
    }
    
    /// <summary>
    /// 플레이어의 닉네임 가져오기
    /// </summary>
    private string GetPlayerNickname(Photon.Realtime.Player player)
    {
        if (player == null) return "Unknown";
        
        // 1. PhotonPlayer의 커스텀 프로퍼티에서 닉네임 가져오기 (최우선)
        if (player.CustomProperties != null && player.CustomProperties.TryGetValue("nickname", out object nicknameObj))
        {
            string nickname = nicknameObj?.ToString();
            if (!string.IsNullOrEmpty(nickname))
            {
                return nickname;
            }
        }
        
        // 2. Photon NickName 속성 확인
        if (!string.IsNullOrEmpty(player.NickName))
        {
            return player.NickName;
        }
        
        // 3. 로컬 플레이어인 경우 PlayerPrefs/CurrentUser에서 가져오기
        if (player.IsLocal)
        {
            string localNickname = "";
            
            // CurrentUser 확인
            if (CurrentUser.Instance != null && CurrentUser.Instance.IsLoggedIn())
            {
                localNickname = CurrentUser.Instance.GetNickname();
            }
            
            // PlayerPrefs 확인
            if (string.IsNullOrEmpty(localNickname))
            {
                localNickname = PlayerPrefs.GetString("NickName", "");
            }
            
            if (!string.IsNullOrEmpty(localNickname))
            {
                return localNickname;
            }
        }
        
        // 4. 기본값으로 Player + ActorNumber 사용
        return $"Player{player.ActorNumber}";
    }
    
    /// <summary>
    /// 플레이어들을 점수 기준으로 정렬
    /// </summary>
    private void SortPlayersByScore()
    {
        playerScoreDataList = playerScoreDataList
            .OrderByDescending(p => p.score)
            .ThenBy(p => p.playerId) // 점수가 같으면 ID 순으로
            .ToList();
    }
    
    /// <summary>
    /// 점수판 UI 업데이트
    /// </summary>
    private void UpdateScoreBoardUI()
    {
        // 모든 점수판을 먼저 비활성화
        foreach (var scoreBoard in scoreBoardObjects)
        {
            if (scoreBoard != null)
                scoreBoard.SetActive(false);
        }
        
        // 플레이어 데이터에 따라 점수판 업데이트
        for (int i = 0; i < playerScoreDataList.Count && i < scoreBoardObjects.Count; i++)
        {
            PlayerScoreData playerData = playerScoreDataList[i];
            
            // 해당 순위의 점수판 활성화
            scoreBoardObjects[i].SetActive(true);
            
            // 순위와 함께 표시
            string displayText = $"{playerData.nickname}   {playerData.score:F0}";
            
            // 로컬 플레이어인 경우 하이라이트
            if (playerData.isLocalPlayer)
            {
                displayText = $"<color=yellow>{displayText}</color>";
            }
            
            scoreBoardTexts[i].text = displayText;
            
        }
    }
    
    /// <summary>
    /// 순위 변경 확인 및 애니메이션 실행
    /// </summary>
    private void CheckAndAnimateRankingChanges()
    {
        // 이전 순위와 현재 순위를 비교하여 변경이 있는지 확인
        bool needsReordering = HasRankingChanged();
        
        if (needsReordering)
        {
            StartCoroutine(AnimateScoreBoardReordering());
        }
    }
    
    /// <summary>
    /// 순위가 변경되었는지 확인
    /// </summary>
    private bool HasRankingChanged()
    {
        if (previousPlayerDataList.Count != playerScoreDataList.Count)
        {
            return true;
        }
        
        // 순위 비교 (같은 순서인지 확인)
        for (int i = 0; i < playerScoreDataList.Count && i < previousPlayerDataList.Count; i++)
        {
            if (playerScoreDataList[i].playerId != previousPlayerDataList[i].playerId)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 점수판 순서 변경 애니메이션
    /// </summary>
    private IEnumerator AnimateScoreBoardReordering()
    {
        isAnimating = true;
        
        // 이전 순위와 현재 순위를 비교하여 어떤 플레이어가 어디로 이동해야 하는지 파악
        Dictionary<int, int> playerIdToOldRank = new Dictionary<int, int>();
        Dictionary<int, int> playerIdToNewRank = new Dictionary<int, int>();
        
        // 이전 순위 기록
        for (int i = 0; i < previousPlayerDataList.Count; i++)
        {
            playerIdToOldRank[previousPlayerDataList[i].playerId] = i;
        }
        
        // 현재 순위 기록
        for (int i = 0; i < playerScoreDataList.Count; i++)
        {
            playerIdToNewRank[playerScoreDataList[i].playerId] = i;
        }
        
        // 각 UI 요소의 현재 위치 저장
        Dictionary<int, Vector3> oldPositions = new Dictionary<int, Vector3>();
        for (int i = 0; i < scoreBoardObjects.Count; i++)
        {
            if (scoreBoardObjects[i] != null && scoreBoardObjects[i].activeSelf)
            {
                oldPositions[i] = scoreBoardObjects[i].transform.localPosition;
            }
        }
        
        // 새로운 순위에 맞게 UI 요소 재배치 (즉시)
        for (int i = 0; i < playerScoreDataList.Count && i < scoreBoardObjects.Count; i++)
        {
            PlayerScoreData playerData = playerScoreDataList[i];
            
            // 해당 순위의 점수판 활성화
            scoreBoardObjects[i].SetActive(true);
            
            // 순위와 함께 표시
            string displayText = $"{playerData.nickname}   {playerData.score:F0}";
            
            // 로컬 플레이어인 경우 하이라이트
            if (playerData.isLocalPlayer)
            {
                displayText = $"<color=yellow>{displayText}</color>";
            }
            
            scoreBoardTexts[i].text = displayText;
            
            // 새로운 Sibling Index 설정
            scoreBoardObjects[i].transform.SetSiblingIndex(i);
        }
        
        // 사용하지 않는 점수판 비활성화
        for (int i = playerScoreDataList.Count; i < scoreBoardObjects.Count; i++)
        {
            if (scoreBoardObjects[i] != null)
            {
                scoreBoardObjects[i].SetActive(false);
            }
        }
        
        // Layout 강제 업데이트
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scoreBoardParent.GetComponent<RectTransform>());
        
        // 새로운 목표 위치 저장
        Dictionary<int, Vector3> newPositions = new Dictionary<int, Vector3>();
        for (int i = 0; i < scoreBoardObjects.Count; i++)
        {
            if (scoreBoardObjects[i] != null && scoreBoardObjects[i].activeSelf)
            {
                newPositions[i] = scoreBoardObjects[i].transform.localPosition;
            }
        }
        
        // 각 UI 요소를 이전 위치로 되돌림 (애니메이션 시작 위치)
        for (int i = 0; i < playerScoreDataList.Count && i < scoreBoardObjects.Count; i++)
        {
            PlayerScoreData currentPlayer = playerScoreDataList[i];
            
            // 이 플레이어의 이전 순위 찾기
            if (playerIdToOldRank.TryGetValue(currentPlayer.playerId, out int oldRank))
            {
                // 이전 순위의 위치로 되돌림
                if (oldPositions.TryGetValue(oldRank, out Vector3 oldPos))
                {
                    scoreBoardObjects[i].transform.localPosition = oldPos;
                }
            }
        }
        
        // 잠시 대기 (위치 변경 반영)
        yield return new WaitForEndOfFrame();
        
        // 부드러운 이동 애니메이션 시작
        List<Tween> tweens = new List<Tween>();
        
        for (int i = 0; i < playerScoreDataList.Count && i < scoreBoardObjects.Count; i++)
        {
            if (scoreBoardObjects[i] != null && scoreBoardObjects[i].activeSelf && newPositions.ContainsKey(i))
            {
                // 현재 위치에서 목표 위치로 이동
                var tween = scoreBoardObjects[i].transform
                    .DOLocalMove(newPositions[i], 0.6f)
                    .SetEase(Ease.OutBack);
                
                tweens.Add(tween);
                
                // 약간의 스케일 애니메이션 추가 (펄스 효과)
                var scaleTween = DOTween.Sequence()
                    .Append(scoreBoardObjects[i].transform.DOScale(1.1f, 0.15f).SetEase(Ease.OutQuad))
                    .Append(scoreBoardObjects[i].transform.DOScale(1f, 0.15f).SetEase(Ease.InQuad));
                
                tweens.Add(scaleTween);
            }
        }
        
        // 모든 애니메이션이 완료될 때까지 대기
        yield return new WaitForSeconds(0.6f);
        
        // 애니메이션 정리
        foreach (var tween in tweens)
        {
            if (tween != null && tween.IsActive())
                tween.Kill();
        }
        
        // 최종 Layout 업데이트
        LayoutRebuilder.ForceRebuildLayoutImmediate(scoreBoardParent.GetComponent<RectTransform>());
        
        isAnimating = false;
    }
    
    /// <summary>
    /// 점수판 강제 업데이트 (외부 호출용)
    /// </summary>
    public void ForceUpdateScoreBoard()
    {
        lastScoreBoardUpdate = 0f; // 즉시 업데이트되도록 설정
    }
    
    /// <summary>
    /// 점수 변경 시 호출되는 이벤트 핸들러
    /// </summary>
    private void OnScoreChanged(float newScore)
    {
        // 로컬 플레이어의 점수가 변경되었을 때 네트워크로 동기화
        if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
        {
            SyncPlayerScoreToNetwork(PhotonNetwork.LocalPlayer.ActorNumber, newScore);
        }
        
        // 점수가 변경되면 즉시 점수판 업데이트
        ForceUpdateScoreBoard();
    }
    
    /// <summary>
    /// 플레이어 점수를 네트워크로 동기화
    /// </summary>
    private void SyncPlayerScoreToNetwork(int playerId, float score)
    {
        // Photon Custom Properties에 점수 저장
        var props = new ExitGames.Client.Photon.Hashtable();
        props[$"score_{playerId}"] = score;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        
        // RPC 제거 - Custom Properties로만 동기화
    }
    
    // RPC 메서드 제거됨 - Custom Properties만 사용
    
    /// <summary>
    /// Photon Custom Properties에서 플레이어 점수 가져오기
    /// </summary>
    private float GetPlayerScoreFromNetwork(Photon.Realtime.Player player)
    {
        if (player == null) return 0f;
        
        // Custom Properties에서 점수 확인
        string scoreKey = $"score_{player.ActorNumber}";
        if (player.CustomProperties.TryGetValue(scoreKey, out object scoreObj))
        {
            if (scoreObj != null && float.TryParse(scoreObj.ToString(), out float networkScore))
            {
                Debug.Log($"HUDPanel: 네트워크에서 점수 가져오기 - Player {player.ActorNumber}: {networkScore}점");
                return networkScore;
            }
        }
        
        Debug.LogWarning($"HUDPanel: Player {player.ActorNumber}의 점수를 네트워크에서 찾을 수 없음");
        return 0f;
    }

    // IPunObservable 인터페이스 제거됨 - Custom Properties만 사용

    #endregion


    #region 조준점 관련
    
    private void ZoomAnimationControl()
    {
        if(localCameraController.IsZoom())
        {
            zoomAnimator.SetBool("Zoom",true);
        }
        else
        {
            zoomAnimator.SetBool("Zoom",false);
        }
    }

    #endregion
    
    #region 게임 종료 카운트다운 사운드
    
    /// <summary>
    /// 게임 종료 10초 전 카운트다운 사운드 체크
    /// </summary>
    private void CheckGameEndingCountdownSound()
    {
        if (GameManager.Instance == null) return;
        
        // 남은 시간 가져오기
        float remainingTime = GameManager.Instance.GetRemainingTime();
        
        // 10초 이하일 때만 사운드 재생
        if (remainingTime <= 10f && remainingTime > 1f)
        {
            int currentSecond = Mathf.CeilToInt(remainingTime);
            
            // 새로운 초가 시작될 때만 사운드 재생 (중복 방지)
            if (currentSecond != lastCountdownSecond)
            {
                lastCountdownSecond = currentSecond;
                
                // 카운트다운 사운드 재생
                if (AudioManager.Inst != null)
                {
                    AudioManager.Inst.PlayOneShot("SFX_UI_GameOver_Count");
                }
                
                isCountdownSoundPlaying = true;
            }
        }
        else if (remainingTime <= 0f && isCountdownSoundPlaying)
        {
            // 게임 종료 시 카운트다운 사운드 플래그 리셋
            isCountdownSoundPlaying = false;
            lastCountdownSecond = -1;
        }
    }
    
    #endregion
    
    #region 장탄수 UI 시스템
    
    /// <summary>
    /// 장탄수 UI 초기화
    /// </summary>
    private void InitializeAmmoUI()
    {
        if (localGun == null) return;
        
        // GunData에서 최대 탄약 가져오기
        GunData gunData = localGun.GetGunData();
        if (gunData != null)
        {
            maxAmmo = gunData.maxAmmo;
            currentAmmo = localGun.CurrentMagAmmo;
            previousAmmo = currentAmmo;
            
            // ProgressBar 초기화
            if (ammoBar != null)
            {
                ammoBar.maxValue = maxAmmo;
                ammoBar.currentValue = currentAmmo;
                
                // 원래 색상 저장
                if (ammoBar.barImage != null)
                {
                    originalAmmoBarColor = ammoBar.barImage.color;
                }
                
                ammoBar.UpdateUI();
            }
            
            // 텍스트 초기화
            UpdateAmmoText();
            
            // 텍스트 원래 색상 저장
            if (currentAmmoCountText != null)
            {
                originalAmmoTextColor = currentAmmoCountText.color;
            }
            
            // 장탄수 변화 시간 초기화
            lastAmmoChangeTime = Time.time;
            
            // 리로드 아이콘 초기화 (투명하게)
            if (reloadIcon != null)
            {
                Color iconColor = reloadIcon.color;
                iconColor.a = 0f;
                reloadIcon.color = iconColor;
            }
            
            // ✅ zoomImage 원래 투명도 저장
            if (zoomImage != null)
            {
                originalZoomImageAlpha = zoomImage.color.a;
            }
            
            Debug.Log($"HUD: 장탄수 UI 초기화 완료 - Current: {currentAmmo}, Max: {maxAmmo}");
        }
    }
    
    /// <summary>
    /// 장탄수 UI 업데이트 (매 프레임)
    /// </summary>
    private void UpdateAmmoUI()
    {
        if (localGun == null) return;
        
        int newAmmo = localGun.CurrentMagAmmo;
        TestGun.GunState currentState = localGun.CurrentState; // 인스턴스 속성 사용
        
        // 장탄수 변화 감지
        if (newAmmo != currentAmmo)
        {
            previousAmmo = currentAmmo;
            currentAmmo = newAmmo;
            lastAmmoChangeTime = Time.time;
            
            // 페이드된 상태라면 복원
            if (isAmmoUIFaded)
            {
                RestoreAmmoUIVisibility();
            }
            
            // 장탄수 애니메이션 시작
            StartAmmoChangeAnimation();
        }
        
        // 재장전 상태 변화 감지
        if (currentState != previousGunState)
        {
            previousGunState = currentState;
            
            if (currentState == TestGun.GunState.Reloading && !isReloading)
            {
                // 재장전 시작
                StartReloadAnimation();
            }
            else if (currentState == TestGun.GunState.Ready && isReloading)
            {
                // 재장전 완료
                StopReloadAnimation();
            }
        }
    }
    
    /// <summary>
    /// 장탄수 텍스트 업데이트
    /// </summary>
    private void UpdateAmmoText()
    {
        if (currentAmmoCountText != null)
        {
            currentAmmoCountText.text = $"{currentAmmo}";
            maxAmmoCountText.text = $"/ {maxAmmo}";
        }
    }
    
    /// <summary>
    /// 장탄수 변화 애니메이션 시작
    /// </summary>
    private void StartAmmoChangeAnimation()
    {
        // 기존 애니메이션 정리
        ammoBarTween?.Kill();
        
        // ProgressBar 값 부드럽게 변경
        if (ammoBar != null)
        {
            ammoBarTween = DOTween.To(() => ammoBar.currentValue, x => {
                ammoBar.currentValue = x;
                ammoBar.UpdateUI();
            }, currentAmmo, 0.3f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => {
                // 애니메이션 완료 후 색상 체크
                CheckAmmoBarColor();
            });
        }
        
        // 텍스트 업데이트
        UpdateAmmoText();
    }
    
    /// <summary>
    /// 장탄수 바 및 텍스트 색상 체크 (20% 이하일 때 빨간색 깜박임)
    /// </summary>
    private void CheckAmmoBarColor()
    {
        if (ammoBar == null || ammoBar.barImage == null) return;
        
        float ammoRatio = (float)currentAmmo / maxAmmo;
        
        if (ammoRatio <= lowAmmoThreshold)
        {
            // 20% 이하 - 빨간색으로 변경하고 깜박임
            StartLowAmmoBlinking();
        }
        else
        {
            // 20% 초과 - 원래 색상으로 복원하고 깜박임 중지
            StopLowAmmoBlinking();
        }
    }
    
    /// <summary>
    /// 낮은 장탄수 깜박임 시작 (바 + 텍스트)
    /// </summary>
    private void StartLowAmmoBlinking()
    {
        if (ammoBar == null || ammoBar.barImage == null) return;
        
        // 기존 깜박임 중지
        ammoBarBlinkTween?.Kill();
        currentAmmoTextBlinkTween?.Kill();
        
        // 바 색상을 빨간색으로 변경
        ammoBar.barImage.color = lowAmmoColor;
        
        // 바 깜박임 시작
        ammoBarBlinkTween = DOTween.Sequence()
            .Append(ammoBar.barImage.DOFade(0.3f, 0.5f))
            .Append(ammoBar.barImage.DOFade(1f, 0.5f))
            .SetLoops(-1, LoopType.Yoyo);
        
        // 텍스트 색상을 빨간색으로 변경하고 깜박임
        if (currentAmmoCountText != null)
        {
            currentAmmoCountText.color = lowAmmoColor;
            
            // 텍스트 깜박임 시작 (바와 동일한 패턴)
            currentAmmoTextBlinkTween = DOTween.Sequence()
                .Append(currentAmmoCountText.DOFade(0.3f, 0.5f))
                .Append(currentAmmoCountText.DOFade(1f, 0.5f))
                .SetLoops(-1, LoopType.Yoyo);
        }
    }
    
    /// <summary>
    /// 낮은 장탄수 깜박임 중지 (바 + 텍스트)
    /// </summary>
    private void StopLowAmmoBlinking()
    {
        if (ammoBar == null || ammoBar.barImage == null) return;
        
        // 깜박임 중지
        ammoBarBlinkTween?.Kill();
        ammoBarBlinkTween = null;
        currentAmmoTextBlinkTween?.Kill();
        currentAmmoTextBlinkTween = null;
        
        // 바 원래 색상으로 복원
        ammoBar.barImage.DOColor(originalAmmoBarColor, 0.3f).SetEase(Ease.OutCubic);
        
        // 텍스트 원래 색상으로 복원
        if (currentAmmoCountText != null)
        {
            currentAmmoCountText.DOColor(originalAmmoTextColor, 0.3f).SetEase(Ease.OutCubic);
        }
    }
    
    /// <summary>
    /// 재장전 애니메이션 시작
    /// </summary>
    private void StartReloadAnimation()
    {
        if (reloadIcon == null) return;
        
        isReloading = true;
        
        // 기존 애니메이션 정리
        reloadIconFadeTween?.Kill();
        reloadIconRotateTween?.Kill();
        reloadIconBlinkTween?.Kill();
        zoomImageFadeTween?.Kill();
        
        // ✅ zoomImage 투명도를 0으로 변경 (안보이게)
        if (zoomImage != null)
        {
            zoomImageFadeTween = zoomImage.DOFade(0f, 0.2f).SetEase(Ease.OutCubic);
        }
        
        // 투명도를 100으로 변경 (255에서 100은 약 0.39)
        Color targetColor = reloadIcon.color;
        targetColor.a = 100f / 255f;
        
        reloadIconFadeTween = reloadIcon.DOColor(targetColor, 0.2f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => {
                // 회전 애니메이션 시작
                StartReloadRotation();
                
                // 깜박임 애니메이션 시작
                StartReloadBlinking();
            });
    }
    
    /// <summary>
    /// 리로드 아이콘 회전 애니메이션
    /// </summary>
    private void StartReloadRotation()
    {
        if (reloadIcon == null) return;
        
        // 360도 회전 (무한 반복)
        reloadIconRotateTween = reloadIcon.transform
            .DORotate(new Vector3(0f, 0f, -360f), 1f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }
    
    /// <summary>
    /// 리로드 아이콘 깜박임 애니메이션
    /// </summary>
    private void StartReloadBlinking()
    {
        if (reloadIcon == null) return;
        
        // 투명도 20~100 사이 깜박임
        reloadIconBlinkTween = DOTween.Sequence()
            .Append(reloadIcon.DOFade(20f / 255f, 0.5f))
            .Append(reloadIcon.DOFade(100f / 255f, 0.5f))
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }
    
    /// <summary>
    /// 재장전 애니메이션 중지
    /// </summary>
    private void StopReloadAnimation()
    {
        if (reloadIcon == null) return;
        
        isReloading = false;
        
        // 모든 애니메이션 중지
        reloadIconFadeTween?.Kill();
        reloadIconRotateTween?.Kill();
        reloadIconBlinkTween?.Kill();
        zoomImageFadeTween?.Kill();
        
        // 회전 초기화
        reloadIcon.transform.rotation = Quaternion.identity;
        
        // 투명도를 0으로 변경
        reloadIcon.DOFade(0f, 0.3f).SetEase(Ease.OutCubic);
        
        // ✅ zoomImage 투명도를 원래대로 복원
        if (zoomImage != null)
        {
            zoomImageFadeTween = zoomImage.DOFade(originalZoomImageAlpha, 0.3f).SetEase(Ease.OutCubic);
        }
    }
    
    /// <summary>
    /// 장탄수 UI 페이드 체크 (3초 동안 변화 없으면)
    /// </summary>
    private void CheckAmmoUIFade()
    {
        // 재장전 중이거나 이미 페이드된 상태면 체크하지 않음
        if (isReloading || isAmmoUIFaded) return;
        
        // 3초간 변화가 없었는지 확인
        if (Time.time - lastAmmoChangeTime >= ammoUIFadeDelay)
        {
            FadeAmmoUI();
        }
    }
    
    /// <summary>
    /// 장탄수 UI 페이드 아웃
    /// </summary>
    private void FadeAmmoUI()
    {
        if (isAmmoUIFaded) return;
        
        isAmmoUIFaded = true;
        
        // ammoIcon 페이드
        if (ammoIcon != null)
        {
            ammoIconFadeTween = ammoIcon.DOFade(100f / 255f, 0.5f).SetEase(Ease.OutCubic);
        }
        
        // ammoBar의 barImage 페이드
        if (ammoBar != null && ammoBar.barImage != null)
        {
            ammoBarImageFadeTween = ammoBar.barImage.DOFade(100f / 255f, 0.5f).SetEase(Ease.OutCubic);
        }
        
        // currentAmmoCountText 페이드
        if (currentAmmoCountText != null)
        {
            Color textColor = currentAmmoCountText.color;
            textColor.a = 100f / 255f;
            currentAmmoTextFadeTween = currentAmmoCountText.DOColor(textColor, 0.5f).SetEase(Ease.OutCubic);
        }

        if (maxAmmoCountText != null)
        {
            Color textColor = maxAmmoCountText.color;
            textColor.a = 100f / 255f;
            maxAmmoTextFadeTween = maxAmmoCountText.DOColor(textColor, 0.5f).SetEase(Ease.OutCubic);
        }
    }
    
    /// <summary>
    /// 장탄수 UI 투명도 복원
    /// </summary>
    private void RestoreAmmoUIVisibility()
    {
        if (!isAmmoUIFaded) return;
        
        isAmmoUIFaded = false;
        
        // 페이드 애니메이션 정리
        ammoIconFadeTween?.Kill();
        ammoBarImageFadeTween?.Kill();
        currentAmmoTextFadeTween?.Kill();
        maxAmmoTextFadeTween?.Kill();
        
        // ammoIcon 복원
        if (ammoIcon != null)
        {
            ammoIconFadeTween = ammoIcon.DOFade(1f, 0.2f).SetEase(Ease.OutCubic);
        }
        
        // ammoBar의 barImage 복원
        if (ammoBar != null && ammoBar.barImage != null)
        {
            Color currentColor = ammoBar.barImage.color;
            currentColor.a = 1f;
            ammoBarImageFadeTween = ammoBar.barImage.DOColor(currentColor, 0.2f).SetEase(Ease.OutCubic);
        }
        
        // currentAmmoCountText 복원
        if (currentAmmoCountText != null)
        {
            Color textColor = currentAmmoCountText.color;
            textColor.a = 1f;
            currentAmmoTextFadeTween = currentAmmoCountText.DOColor(textColor, 0.2f).SetEase(Ease.OutCubic);
        }

        if (maxAmmoCountText != null)
        {
            Color textColor = maxAmmoCountText.color;
            textColor.a = 1f;
            maxAmmoTextFadeTween = maxAmmoCountText.DOColor(textColor, 0.2f).SetEase(Ease.OutCubic);
        }
    }
    
    /// <summary>
    /// 장탄수 애니메이션 정리
    /// </summary>
    private void CleanupAmmoAnimations()
    {
        ammoBarTween?.Kill();
        ammoBarBlinkTween?.Kill();
        ammoIconFadeTween?.Kill();
        ammoBarImageFadeTween?.Kill();
        currentAmmoTextFadeTween?.Kill();
        maxAmmoTextFadeTween?.Kill();
        currentAmmoTextBlinkTween?.Kill();
        reloadIconFadeTween?.Kill();
        reloadIconRotateTween?.Kill();
        reloadIconBlinkTween?.Kill();
        zoomImageFadeTween?.Kill(); // ✅ zoomImage 애니메이션도 정리
        
        ammoBarTween = null;
        ammoBarBlinkTween = null;
        ammoIconFadeTween = null;
        ammoBarImageFadeTween = null;
        currentAmmoTextFadeTween = null;
        maxAmmoTextFadeTween = null;
        currentAmmoTextBlinkTween = null;
        reloadIconFadeTween = null;
        reloadIconRotateTween = null;
        reloadIconBlinkTween = null;
        zoomImageFadeTween = null; // ✅ null로 초기화
    }
    
    #endregion
    
    #region 아이콘 애니메이션
    
    /// <summary>
    /// 점수 아이콘 진동 애니메이션 (좌우로 기울어지며 흔들림)
    /// </summary>
    private void PlayScoreIconShakeAnimation()
    {
        if (scoreIcon == null) return;
        
        // 기존 애니메이션 중지
        scoreIconShakeTween?.Kill();
        
        // 원래 회전값으로 초기화
        scoreIcon.transform.rotation = Quaternion.identity;
        
        // 좌우 진동 애니메이션 (±15도 각도로 3번 왔다갔다)
        scoreIconShakeTween = DOTween.Sequence()
            .Append(scoreIcon.transform.DORotate(new Vector3(0f, 0f, 15f), 0.08f).SetEase(Ease.OutQuad))
            .Append(scoreIcon.transform.DORotate(new Vector3(0f, 0f, -15f), 0.08f).SetEase(Ease.InOutQuad))
            .Append(scoreIcon.transform.DORotate(new Vector3(0f, 0f, 10f), 0.08f).SetEase(Ease.InOutQuad))
            .Append(scoreIcon.transform.DORotate(new Vector3(0f, 0f, -10f), 0.08f).SetEase(Ease.InOutQuad))
            .Append(scoreIcon.transform.DORotate(new Vector3(0f, 0f, 5f), 0.08f).SetEase(Ease.InOutQuad))
            .Append(scoreIcon.transform.DORotate(new Vector3(0f, 0f, 0f), 0.08f).SetEase(Ease.InQuad))
            .OnComplete(() => {
                // 애니메이션 완료 후 회전값 완전히 초기화
                scoreIcon.transform.rotation = Quaternion.identity;
            });
    }
    
    /// <summary>
    /// 코인 아이콘 회전 애니메이션 (마리오 스타일)
    /// </summary>
    private void PlayCoinIconRotateAnimation()
    {
        if (coinIcon == null) return;
        
        // 기존 애니메이션 중지
        coinIconRotateTween?.Kill();
        coinIconScaleTween?.Kill();
        
        // 원래 크기와 회전값으로 초기화
        coinIcon.transform.localScale = Vector3.one;
        coinIcon.transform.rotation = Quaternion.identity;
        
        // Y축 180도 회전 (뒤집히는 효과)
        coinIconRotateTween = coinIcon.transform
            .DORotate(new Vector3(0f, 180f, 0f), 0.4f, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                // 회전 완료 후 원래대로 복원
                coinIcon.transform.rotation = Quaternion.identity;
            });
        
        // 크기 변화 애니메이션 (약간 커졌다가 작아짐)
        coinIconScaleTween = DOTween.Sequence()
            .Append(coinIcon.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutQuad))
            .Append(coinIcon.transform.DOScale(1f, 0.2f).SetEase(Ease.InQuad));
    }
    
    /// <summary>
    /// 아이콘 애니메이션 정리
    /// </summary>
    private void CleanupIconAnimations()
    {
        scoreIconShakeTween?.Kill();
        coinIconRotateTween?.Kill();
        coinIconScaleTween?.Kill();
        
        scoreIconShakeTween = null;
        coinIconRotateTween = null;
        coinIconScaleTween = null;
        
        // 원래 상태로 복원
        if (scoreIcon != null)
        {
            scoreIcon.transform.rotation = Quaternion.identity;
        }
        
        if (coinIcon != null)
        {
            coinIcon.transform.rotation = Quaternion.identity;
            coinIcon.transform.localScale = Vector3.one;
        }
    }
    
    #endregion
}



