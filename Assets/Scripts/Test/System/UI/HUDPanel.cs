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
    
    // 점수판 관련
    private List<PlayerScoreData> playerScoreDataList = new List<PlayerScoreData>();
    private List<GameObject> scoreBoardObjects = new List<GameObject>();
    private List<TextMeshProUGUI> scoreBoardTexts = new List<TextMeshProUGUI>();
    private float lastScoreBoardUpdate = 0f;
    private float scoreBoardUpdateInterval = 1f; // 1초마다 업데이트
    private bool isAnimating = false;
    
    // 성능 최적화 관련
    private List<PlayerScoreData> previousPlayerDataList = new List<PlayerScoreData>();
    private bool hasScoreChanged = false;
    
    void Start()
    {
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
        
        // 점수판 업데이트 (1초마다)
        if (currentTime - lastScoreBoardUpdate > scoreBoardUpdateInterval)
        {
            UpdateScoreBoard();
            lastScoreBoardUpdate = currentTime;
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
        
        Debug.Log($"✅ HUD: 점수판 초기화 완료 - {scoreBoardObjects.Count}개의 점수판 등록");
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
        
        Debug.Log($"🔍 플레이어 데이터 수집 시작 - 총 {allPlayers.Length}명");
        
        foreach (var player in allPlayers)
        {
            // 플레이어의 GameObject 찾기
            GameObject playerObject = FindPlayerObjectByPhotonPlayer(player);
            if (playerObject == null) 
            {
                Debug.LogWarning($"⚠️ 플레이어 오브젝트를 찾을 수 없음 - ActorNumber: {player.ActorNumber}");
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
            
            Debug.Log($"📊 플레이어 데이터: {nickname} (ID: {player.ActorNumber}) - 점수: {playerScore} {(isLocal ? "[로컬]" : "[원격]")}");
        }
        
        Debug.Log($"✅ HUD: 플레이어 데이터 수집 완료 - {playerScoreDataList.Count}명");
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
    /// 플레이어의 점수 가져오기
    /// </summary>
    private float GetPlayerScore(GameObject playerObject)
    {
        CoinController coinController = playerObject.GetComponent<CoinController>();
        if (coinController != null)
        {
            return coinController.GetCurrentScore();
        }
        
        return 0f;
    }
    
    /// <summary>
    /// 플레이어의 닉네임 가져오기
    /// </summary>
    private string GetPlayerNickname(Photon.Realtime.Player player)
    {
        // PhotonPlayer의 커스텀 프로퍼티에서 닉네임 가져오기
        if (player.CustomProperties.TryGetValue("nickname", out object nicknameObj))
        {
            return nicknameObj.ToString();
        }
        
        // 커스텀 프로퍼티가 없으면 로컬 플레이어의 경우 PlayerPrefs에서 가져오기
        if (player.IsLocal)
        {
            string localNickname = PlayerPrefs.GetString("NickName", "Player");
            if (!string.IsNullOrEmpty(localNickname))
            {
                // 로컬 닉네임을 커스텀 프로퍼티에 설정
                var props = new ExitGames.Client.Photon.Hashtable();
                props["nickname"] = localNickname;
                player.SetCustomProperties(props);
                return localNickname;
            }
        }
        
        // 기본값으로 Player + ActorNumber 사용
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
            string displayText = $"{i + 1}. {playerData.nickname}: {playerData.score:F0}";
            
            // 로컬 플레이어인 경우 하이라이트
            if (playerData.isLocalPlayer)
            {
                displayText = $"<color=yellow>{displayText}</color>";
            }
            
            scoreBoardTexts[i].text = displayText;
            
            Debug.Log($"🎯 점수판 업데이트: 순위 {i + 1} - {playerData.nickname} ({playerData.playerId}) : {playerData.score:F0}점 {(playerData.isLocalPlayer ? "[나]" : "")}");
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
        
        // 현재 위치 저장
        List<Vector3> originalPositions = new List<Vector3>();
        foreach (var scoreBoard in scoreBoardObjects)
        {
            originalPositions.Add(scoreBoard.transform.localPosition);
        }
        
        // Hierarchy 순서 변경
        for (int i = 0; i < playerScoreDataList.Count && i < scoreBoardObjects.Count; i++)
        {
            scoreBoardObjects[i].transform.SetSiblingIndex(i);
        }
        
        // Layout Group 강제 업데이트
        LayoutRebuilder.ForceRebuildLayoutImmediate(scoreBoardParent.GetComponent<RectTransform>());
        
        // 새로운 목표 위치 계산
        List<Vector3> targetPositions = new List<Vector3>();
        foreach (var scoreBoard in scoreBoardObjects)
        {
            targetPositions.Add(scoreBoard.transform.localPosition);
        }
        
        // 원래 위치로 되돌리기 (애니메이션 시작점)
        for (int i = 0; i < scoreBoardObjects.Count; i++)
        {
            if (i < originalPositions.Count)
            {
                scoreBoardObjects[i].transform.localPosition = originalPositions[i];
            }
        }
        
        // DOTween을 사용한 부드러운 이동 애니메이션
        List<Tween> tweens = new List<Tween>();
        
        for (int i = 0; i < scoreBoardObjects.Count; i++)
        {
            if (i < targetPositions.Count)
            {
                var tween = scoreBoardObjects[i].transform
                    .DOLocalMove(targetPositions[i], 0.5f)
                    .SetEase(Ease.OutCubic);
                tweens.Add(tween);
            }
        }
        
        // 모든 애니메이션이 완료될 때까지 대기
        yield return new WaitForSeconds(0.5f);
        
        // 애니메이션 완료 후 정리
        foreach (var tween in tweens)
        {
            if (tween != null)
                tween.Kill();
        }
        
        // Layout Group 다시 활성화
        LayoutRebuilder.ForceRebuildLayoutImmediate(scoreBoardParent.GetComponent<RectTransform>());
        
        isAnimating = false;
        
        Debug.Log("✅ HUD: 점수판 순서 변경 애니메이션 완료");
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
        // 점수가 변경되면 즉시 점수판 업데이트
        ForceUpdateScoreBoard();
        Debug.Log($"🎯 점수 변경 감지 - 점수판 즉시 업데이트 요청");
    }
    
    #endregion

} 



