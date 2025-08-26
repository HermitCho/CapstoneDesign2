using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.Heat;
using Photon.Pun;
using Febucci.UI;
using System.Collections;

/// <summary>
/// 로컬 플레이어의 기본 정보만을 표시하는 간단한 HUD
/// 다른 플레이어와 완전히 독립적으로 동작
/// </summary>
public class HUDPanel : MonoBehaviour
{
    [Header("체력 UI")]
    [SerializeField] private ProgressBar healthProgressBar;
    [SerializeField] private TextMeshProUGUI healthText;
    
    [Header("점수 UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI scoreMultiplierText;
    
    [Header("코인 UI")]
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

    // 로컬 플레이어 참조
    private GameObject localPlayer;
    private LivingEntity localLivingEntity;
    private CoinController localCoinController;
    private Skill localCharacterSkill;
    private ItemController localItemController;
    
    // UI 상태
    private float currentHealth = 100f;
    private float maxHealth = 100f;
    private int currentCoin = 0;
    private float currentScore = 0f;
    
    // 시간 관련 (GameManager에서 받아옴)
    private float gameTime = 0f;
    private float lastTimeUpdate = 0f;
    
    // 스킬 관련
    private float lastSkillUpdate = 0f;
    
    // 아이템 관련
    private float lastItemUpdate = 0f;
    
    void Start()
    {
        // 로컬 플레이어 찾기 시작
        StartCoroutine(FindLocalPlayerRoutine());
        
        // GameManager 이벤트 구독 (시간 정보만)
        if (GameManager.Instance != null)
        {
            GameManager.OnGameTimeUpdated += UpdateGameTime;
        }
        
        // LivingEntity 사망 이벤트 구독
        LivingEntity.OnPlayerDied += HandlePlayerDeath;
        Debug.Log("HUD: LivingEntity.OnPlayerDied 이벤트 구독 완료");
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.OnGameTimeUpdated -= UpdateGameTime;
        }
        
        // LivingEntity 사망 이벤트 구독 해제
        LivingEntity.OnPlayerDied -= HandlePlayerDeath;
        Debug.Log("HUD: LivingEntity.OnPlayerDied 이벤트 구독 해제 완료");
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
            UpdateHealthDisplay();
        }
        
        if (localCoinController != null)
        {
            currentCoin = localCoinController.GetCurrentCoin();
            UpdateCoinDisplay();
        }
        
        if (localCharacterSkill != null)
        {
            UpdateSkillDisplay();
        }
        
        // 초기 점수 표시
        UpdateScoreDisplay();

        // 초기 아이템 UI 표시
        UpdateItemUI();
    }
    
    /// <summary>
    /// 체력 업데이트
    /// </summary>
    void UpdateHealth()
    {
        if (localLivingEntity == null) return;
        
        float newHealth = localLivingEntity.CurrentHealth;
        float newMaxHealth = localLivingEntity.StartingHealth;
        
        if (Mathf.Abs(newHealth - currentHealth) > 0.1f || 
            Mathf.Abs(newMaxHealth - maxHealth) > 0.1f)
        {
            currentHealth = newHealth;
            maxHealth = newMaxHealth;
            UpdateHealthDisplay();
        }
    }
    
    /// <summary>
    /// 체력 UI 업데이트
    /// </summary>
    void UpdateHealthDisplay()
    {
        if (healthProgressBar != null)
        {
            healthProgressBar.currentValue = currentHealth;
            healthProgressBar.maxValue = maxHealth;
            healthProgressBar.UpdateUI();
        }
        
        if (healthText != null)
        {
            healthText.text = $"{currentHealth:F0} / {maxHealth:F0}";
        }
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
            currentScore = newScore;
            UpdateScoreDisplay();
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

            
            // 킬로그 텍스트 설정
            questItem.questText = $"{attacker.CharacterData.characterName}       {victim.CharacterData.characterName}";
            questItem.UpdateUI();

            // Animate quest
            questItem.AnimateQuest();

            
            // 3초 후 킬로그 제거
            //StartCoroutine(DestroyKillLogAfterDelay(killLog, 5f));
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

} 



