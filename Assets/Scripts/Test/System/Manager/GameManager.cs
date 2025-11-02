using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using Photon.Pun;
using ExitGames.Client.Photon;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class GameManager : Singleton<GameManager>
{
    #region 자동할당 변수

    // 플레이어 관리
    private LivingEntity localPlayerLivingEntity;

    private LivingEntity[] allPlayerLivingEntities;

    // 테디베어 관리 (점수는 CoinController에서 관리)
    private Crown currentTeddyBear;
    
    // 게임 시간 관리
    private double gameStartTime = 0f;
    private bool isGameActuallyStarted = false;

    // 상점 관리 (제거됨 - Shop.cs에서 직접 관리)

    // 플레이어 상태 관리
    private float playerHealth = 100f;
    private float maxPlayerHealth = 100f;
    
    // 코인 컨트롤러 관리
    private CoinController currentPlayerCoinController;
    
    // 게임 오버 관리
    private bool isGameOver = false;
    private InGameUIManager inGameUIManager;
    
    // 씬 전환 감지를 위한 변수
    private string lastSceneName = "";

    #endregion





    #region 캐싱 변수

    private float cachedScoreIncreaseTime = 20f; // 기본값
    private float cachedScoreIncreaseRate = 2f; // 기본
    private float cachedInitialScore = 1f; // 기본
    private float cachedPlayTime = 360f; // 기본
    // 상점 시간 캐싱 제거 (Shop.cs에서 직접 관리)
    private GameObject[] cachedItemData;
    private bool dataBaseCached = false;
    

    #endregion





    #region 이벤트
    // ✅ UI 시스템을 위한 이벤트들
    // 점수 관련 이벤트
    public static event Action<float> OnScoreUpdated;
    public static event Action<float> OnScoreMultiplierUpdated;
    public static event Action<float> OnGameTimeUpdated;

    // 테디베어 관련 이벤트
    public static event Action<bool> OnTeddyBearAttachmentChanged;
    public static event Action<float> OnTeddyBearReattachTimeChanged;

    // 플레이어 상태 이벤트
    public static event Action<float, float> OnPlayerHealthChanged; // current, max
    // 아이템 UI 이벤트
    public static event Action<bool> OnItemUIToggled;

    // 크로스헤어 이벤트
    public static event Action<bool> OnCrosshairTargetingChanged;

    // 스킬 이벤트 (구현 예정)
    public static event Action OnSkillUsed;
    public static event Action<int, float> OnSkillCooldownStarted;

    public static event Action OnCharacterSpawned;

    // 상점 이벤트 (제거됨 - Shop.cs에서 직접 관리)
    
    // 게임 오버 이벤트
    public static event Action<float> OnGameOver; // 최종 점수와 함께 게임 오버 알림

    #endregion






    #region 생명주기

    void Awake()
    {
        // 포톤 네트워크 설정 최적화
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.SendRate = 20; // 20 FPS로 전송률 조정
            PhotonNetwork.SerializationRate = 10; // 10 FPS로 직렬화율 조정
        }
        
        // 씬 로드 이벤트 구독 (싱글톤이므로 한번만 구독됨)
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // 이벤트 구독 제거 - Room Properties 기반으로 변경
        
        // 현재 씬이 게임 씬이라면 즉시 초기화
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (IsGameScene(currentSceneName))
        {
            ResetGameState();
            lastSceneName = currentSceneName;
        }
    }

    void Start()
    {
        // DataBase 정보 캐싱 (항상 수행)
        CacheDataBaseInfo();
    }
    
    void Update()
    {
        // Room Properties 기반으로 게임 시작 상태 체크
        CheckGameStartFromRoomProperties();
        
        // 게임이 실제로 시작되고 게임 오버 상태가 아닐 때만 시간 체크
        if (isGameActuallyStarted && !isGameOver)
        {
            // 마스터 클라이언트는 권위적 시간 관리
            if (PhotonNetwork.IsMasterClient)
            {
                UpdateMasterGameTime();
            }
            
            // 게임 씬에서 필요한 컴포넌트들이 null인지 주기적으로 체크
            CheckAndFindMissingComponents();
            
            if (inGameUIManager == null || currentTeddyBear == null)
            {
                return; 
            }
            
            CheckGameTimeForGameOver();
        }
        else
        {
            // 게임이 시작되지 않았을 때는 컴포넌트 체크만
            CheckAndFindMissingComponents();
        }
    }
    
    /// <summary>
    /// Room Properties를 통해 게임 시작 상태 체크 (실무 스타일)
    /// </summary>
    private void CheckGameStartFromRoomProperties()
    {
        if (!isGameActuallyStarted && PhotonNetwork.InRoom)
        {
            var props = PhotonNetwork.CurrentRoom.CustomProperties;
            if (props.TryGetValue("gamePhase", out object gamePhaseObj) && 
                gamePhaseObj.ToString() == "PLAYING")
            {
                // 게임이 시작되었음을 감지
                StartGame();
            }
        }
    }
    
    /// <summary>
    /// 게임 시작 (Room Properties 기반)
    /// </summary>
    private void StartGame()
    {
        if (isGameActuallyStarted)
        {
            return;
        }
        
        isGameActuallyStarted = true;
        
        // ✅ Room Properties에서 게임 시작 시간 가져오기 (마스터가 설정한 값)
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameStartTime", out object startTimeObj))
        {
            if (startTimeObj is double)
            {
                gameStartTime = (double)startTimeObj;
            }
            else
            {
                gameStartTime = PhotonNetwork.Time;
            }
        }
        else
        {
            // Room Properties에 없으면 현재 시간 사용 (백업)
            gameStartTime = PhotonNetwork.Time;
            
            // 마스터 클라이언트가 설정하지 않았다면 직접 설정
            if (PhotonNetwork.IsMasterClient)
            {
                SetMasterGameTime();
            }
        }
        
        EnableGameUI();
        
        StartCoroutine(EnablePlayerControlsDelayed());
    }
    
    /// <summary>
    /// 게임 시작 시 플레이어 조작 활성화
    /// </summary>
    private IEnumerator EnablePlayerControlsDelayed()
    {
        yield return new WaitForSeconds(0.2f);
        
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                MoveController moveController = player.GetComponent<MoveController>();
                if (moveController != null)
                {
                    moveController.EnableMoveControls();
                }
                
                SkillController skillController = player.GetComponent<SkillController>();
                if (skillController != null)
                {
                    skillController.EnableSkillControls();
                }
                
                TestGun gun = player.GetComponentInChildren<TestGun>();
                if (gun != null)
                {
                    gun.enabled = true;
                }
                
                CameraController cameraController = player.GetComponent<CameraController>();
                if (cameraController != null)
                {
                    cameraController.enabled = true;
                }
            }
        }
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    /// <summary>
    /// 누락된 컴포넌트들을 주기적으로 체크하고 찾기
    /// </summary>
    void CheckAndFindMissingComponents()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (!IsGameScene(currentSceneName)) return;
        
        // InGameUIManager 체크 (가장 중요)
        if (inGameUIManager == null)
        {
            FindInGameUIManager();
        }
        
        // 테디베어 체크
        if (currentTeddyBear == null)
        {
        FindTeddyBear();
        }
    }

    void OnDestroy() // ✅ 수정: 오류 해결
    {
        // 씬 로드 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // 이벤트 구독 해제 제거 - Room Properties 기반으로 변경
        
        LivingEntity.OnAnyLivingEntityHealthChanged -= HandleAnyLivingEntityHealthChanged;
    }


    #endregion





    #region 씬 전환 및 게임 상태 초기화 메서드
    
    /// <summary>
    /// 씬 로드 이벤트 콜백 (씬이 로드될 때마다 호출됨)
    /// </summary>
    /// <param name="scene">로드된 씬</param>
    /// <param name="mode">씬 로드 모드</param>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string currentSceneName = scene.name;
        
        // ✅ 씬이 바뀔 때마다 게임 오버 상태 리셋 (튜토리얼 씬 등에서 애니메이션 작동하도록)
        if (lastSceneName != currentSceneName)
        {
            isGameOver = false;
        }
        
        // 씬이 바뀌었고, 게임 씬인 경우
        if (lastSceneName != currentSceneName && IsGameScene(currentSceneName))
        {         
            // 게임 상태 초기화
            ResetGameState();
            
            // 약간의 지연 후 컴포넌트 찾기 (씬 로드 완료 대기)
            StartCoroutine(FindComponentsAfterSceneLoad());
        }
        
        // 현재 씬 이름 저장
        lastSceneName = currentSceneName;
    }
    
    /// <summary>
    /// 씬 로드 후 컴포넌트 찾기 (지연 호출)
    /// </summary>
    IEnumerator FindComponentsAfterSceneLoad()
    {
        // 씬 로드 완료 대기
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame(); // 추가 대기로 안전성 확보
        
        // 컴포넌트 찾기
        FindInGameUIManager();
        FindTeddyBear();
        
        // 필수 컴포넌트 확인 시작
        StartCoroutine(VerifyEssentialComponents());
    }
    
    /// <summary>
    /// 필수 컴포넌트들이 모두 찾아졌는지 확인 (안전장치)
    /// </summary>
    IEnumerator VerifyEssentialComponents()
    {
        float checkTime = 0f;
        float maxCheckTime = 5f; // 최대 5초간 체크
        
        while (checkTime < maxCheckTime)
        {
            yield return new WaitForSeconds(0.5f); // 0.5초마다 체크
            checkTime += 0.5f;
            
            // 필수 컴포넌트 체크
            bool allFound = true;
            
            if (inGameUIManager == null)
            {
                FindInGameUIManager();
                allFound = false;
            }
            
            if (currentTeddyBear == null)
            {
                FindTeddyBear();
                allFound = false;
            }
            
            // 모든 컴포넌트를 찾았다면 종료
            if (allFound)
            {
                break;
            }
        }
    }
    
    /// <summary>
    /// 게임 씬인지 확인
    /// </summary>
    /// <param name="sceneName">씬 이름</param>
    /// <returns>게임 씬 여부</returns>
    bool IsGameScene(string sceneName)
    {
        // 게임 씬 목록 (프로젝트에 맞게 수정)
        string[] gameScenes = { "InGame", "Prototype", "GameScene", "Main" };
        
        foreach (string gameScene in gameScenes)
        {
            if (sceneName.Contains(gameScene))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 게임 상태 초기화 (새 게임 시작)
    /// </summary>
    void ResetGameState()
    {
        Debug.Log("GameManager: ResetGameState() 호출 - 게임 상태 완전 초기화");
        
        // 1. DataBase 정보 먼저 캐싱 (PlayTime 확보)
        CacheDataBaseInfo();
        
        // 2. 게임 시간 완전 초기화
        gameStartTime = 0f; // ✅ 0으로 초기화 (StartGame에서 PhotonNetwork.Time으로 재설정)
        isGameActuallyStarted = false; // ✅ 게임 시작 플래그 초기화 (중요!)
        isGameOver = false;
        
        Debug.Log($"GameManager: 게임 시작 플래그 초기화 - isGameActuallyStarted: {isGameActuallyStarted}, isGameOver: {isGameOver}");
        
        // 마스터 클라이언트가 권위적 시간을 방 속성에서 제거 (초기화)
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            PhotonHashtable props = new PhotonHashtable();
            props["gameStartTime"] = null;
            props["currentGameTime"] = null;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        
        // 3. 점수 완전 초기화
        // totalTeddyBearScore = 0f; // 점수 관련 필드 제거
        ResetAllScores(); // 테디베어 점수도 함께 초기화
        
        // 4. 플레이어 상태 초기화
        playerHealth = 100f;
        maxPlayerHealth = 100f;
        
        // 5. 이벤트 구독 해제 (컴포넌트 참조 초기화 전에 수행)
        if (localPlayerLivingEntity != null)
        {
            LivingEntity.OnAnyLivingEntityHealthChanged -= HandleAnyLivingEntityHealthChanged;
        }
        
        // 6. 컴포넌트 참조 초기화 (새로 찾아야 함)
        localPlayerLivingEntity = null;
        currentPlayerCoinController = null;
        currentTeddyBear = null;
        
        // InGameUIManager 상태 리셋 (새 게임 시작 시)
        if (inGameUIManager != null)
        {
            inGameUIManager.ResetGameState();
        }
        else
        {
            // 찾아서 리셋
            FindInGameUIManager();
            if (inGameUIManager != null)
            {
                inGameUIManager.ResetGameState();
            }
        }
        // 상점 상태 초기화 제거 (Shop.cs에서 직접 관리)
        
        // 8. UI 이벤트 발생 (초기값으로) - 약간의 지연을 두어 확실히 적용
        StartCoroutine(SendInitialUIEvents());
    }
    
    /// <summary>
    /// 초기 UI 이벤트 발생 (약간의 지연으로 확실한 적용)
    /// </summary>
    IEnumerator SendInitialUIEvents()
    {
        yield return new WaitForEndOfFrame();
        
        // UI 이벤트 발생 전 최종 상태 확인
        float currentPlayTime = GetPlayTime();
        float currentGameTime = GetGameTime();
        float remainingTime = currentPlayTime - currentGameTime;   
        // UI 이벤트 발생 (초기값으로)
        OnScoreUpdated?.Invoke(0f);
        OnScoreMultiplierUpdated?.Invoke(1f);
        OnGameTimeUpdated?.Invoke(remainingTime); // 남은 시간으로 초기화
    }
    
    /// <summary>
    /// 외부에서 호출 가능한 강제 게임 상태 초기화 (디버그용)
    /// </summary>
    public void ForceResetGameState()
    {
        ResetGameState();
        
        // 테디베어 다시 찾기 및 초기화
        currentTeddyBear = null;
        FindTeddyBear();
        
        // InGameUIManager 다시 찾기
        inGameUIManager = null;
        FindInGameUIManager();
    }

    #endregion





    #region 데이터 받아오기 메서드
    // DataBase 정보 캐싱 (안전한 접근)
    void CacheDataBaseInfo()
    {
        try
        {
            if (DataBase.Instance != null && DataBase.Instance.teddyBearData != null && DataBase.Instance.gameData != null && DataBase.Instance.itemData != null)
            {
                cachedScoreIncreaseTime = DataBase.Instance.teddyBearData.ScoreIncreaseTime;
                cachedScoreIncreaseRate = DataBase.Instance.teddyBearData.ScoreIncreaseRate;
                cachedInitialScore = DataBase.Instance.teddyBearData.InitialScore;
                cachedPlayTime = DataBase.Instance.gameData.PlayTime;
                // 상점 시간 캐싱 제거 (Shop.cs에서 직접 관리)
                cachedItemData = DataBase.Instance.itemData.ItemPrefabData.ToArray();
                dataBaseCached = true;
            }
            else
            {
                dataBaseCached = false;
            }
        }
        catch (System.Exception e)
        {
            dataBaseCached = false;
        }
    }

    #endregion





    #region 캐싱 데이터 받아오기 메서드

    /// <summary>
    /// 점수 증가 시간 가져오기 (캐싱된 값 사용)
    /// </summary>
    public float GetScoreIncreaseTime()
    {
        if (dataBaseCached)
        {
            return cachedScoreIncreaseTime;
        }

        // 캐싱되지 않았다면 재시도
        CacheDataBaseInfo();
        return cachedScoreIncreaseTime;
    }

    /// <summary>
    /// 점수 증가 배율 가져오기 (캐싱된 값 사용)
    /// </summary>
    public float GetScoreIncreaseRate()
    {
        if (dataBaseCached)
        {
            return cachedScoreIncreaseRate;
        }

        // 캐싱되지 않았다면 재시도
        CacheDataBaseInfo();
        return cachedScoreIncreaseRate;
    }

    /// <summary>
    /// DataBase가 성공적으로 캐싱되었는지 확인
    /// </summary>
    public bool IsDataBaseCached()
    {
        return dataBaseCached;
    }

    /// <summary>
    /// DataBase 정보 강제 새로고침
    /// </summary>
    public void RefreshDataBaseCache()
    {
        CacheDataBaseInfo();
    }

    #endregion





    #region 테디베어 관련 메서드

    // 테디베어 점수 업데이트 (TestTeddyBear에서 호출)
    public void UpdateTeddyBearScore(float newScore)
    {
        // totalTeddyBearScore = newScore; // 점수 관련 필드 제거

        // 테디베어의 currentScore도 동기화
        if (currentTeddyBear != null)
        {
            // TestTeddyBear의 AddScore 메서드를 통해 동기화
            currentTeddyBear.AddScore(0f); // 0을 더해서 동기화만 수행
        }

        // HeatUI에 점수 업데이트 이벤트 발생
        OnScoreUpdated?.Invoke(0f); // 점수 관련 필드 제거

        // 점수 배율도 실시간 계산으로 업데이트
        float currentMultiplier = GetScoreMultiplier();
        OnScoreMultiplierUpdated?.Invoke(currentMultiplier);
    }

    // 테디베어 점수 추가/차감 (코인 획득, 사망 시 손실 등) - 제거됨
    // 점수는 CoinController에서 직접 관리

    // 현재 테디베어 점수 가져오기
    public float GetTeddyBearScore()
    {
        if (currentPlayerCoinController != null)
        {
            // CoinController에서 직접 점수 가져오기
            return currentPlayerCoinController.GetCurrentScore();
        }
        return 0f;
    }

    // 테디베어가 부착되어 있는지 확인
    public bool IsTeddyBearAttached()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.IsAttached();
        }
        return false;
    }

    // 테디베어 재부착까지 남은 시간 가져오기
    public float GetTimeUntilReattach()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.GetTimeUntilReattach();
        }
        return 0f;
    }

    // 테디베어 재부착 가능 여부 확인
    public bool CanTeddyBearReattach()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.CanReattach();
        }
        return true;
    }

    #endregion





    #region 점수 관련 메서드

    // 점수 초기화 (개발자용)
    public void ResetAllScores()
    {
        if (currentPlayerCoinController != null)
        {
            // CoinController를 통해 점수 초기화
            currentPlayerCoinController.ResetScore();
        }
        else
        {
            // CoinController가 없으면 기본값으로 초기화
            OnScoreUpdated?.Invoke(0f);
            OnScoreMultiplierUpdated?.Invoke(1f);
        }
    }

    public float GetPlayTime()
    {
        return cachedPlayTime;
    }

    // 현재 점수 배율 가져오기 (테디베어 부착 상태에 따른 배율)
    public float GetScoreMultiplier()
    {
        // 테디베어가 부착되어 있다면 배율 적용, 아니면 기본값 1.0
        if (IsTeddyBearAttached())
        {
            return cachedScoreIncreaseRate;
        }
        return cachedInitialScore;
    }

    // 게임 시간 가져오기
    public float GetGameTime()
    {
        // 방 속성에서 권위적 시간 가져오기 (마스터가 아닌 클라이언트용)
        if (!PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
        {
            var props = PhotonNetwork.CurrentRoom.CustomProperties;
            if (props.ContainsKey("gameStartTime") && props.ContainsKey("playTime"))
            {
                double masterStartTime = (double)props["gameStartTime"];
                float playTime = (float)props["playTime"];
                double elapsed = PhotonNetwork.Time - masterStartTime;
                return (float)elapsed;
            }
        }
        
        // 마스터 클라이언트이거나 방 속성이 없는 경우 로컬 시간 사용
        return (float)(PhotonNetwork.Time - gameStartTime);
    }
    
    // ✅ 남은 게임 시간 가져오기 (HUDPanel에서 사용)
    public float GetRemainingTime()
    {
        float currentGameTime = GetGameTime();
        return Mathf.Max(0f, cachedPlayTime - currentGameTime);
    }

    // GetShopTime 제거 (Shop.cs에서 직접 관리)
    
    /// <summary>
    /// 마스터 클라이언트가 권위적 게임 시간 설정
    /// </summary>
    private void SetMasterGameTime()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) return;
        
        PhotonHashtable props = new PhotonHashtable();
        props["gameStartTime"] = PhotonNetwork.Time;
        props["playTime"] = cachedPlayTime;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }
    
    /// <summary>
    /// 마스터 클라이언트가 게임 시간을 주기적으로 업데이트
    /// </summary>
    private void UpdateMasterGameTime()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) return;
        
        // 5초마다 시간 동기화 업데이트
        if (Time.time - lastTimeSync > 5f)
        {
            PhotonHashtable props = new PhotonHashtable();
            props["currentGameTime"] = GetGameTime();
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            lastTimeSync = Time.time;
        }
    }
    
    private float lastTimeSync = 0f;

    #endregion
    
    
    
    
    
    #region 게임 오버 관리 메서드
    
    /// <summary>
    /// 플레이어 사망 시 코인 및 점수 손실 처리
    /// </summary>
    public void HandlePlayerDeathPenalty()
    {
        // ✅ 중복 실행 방지 (정적 변수로 플레이어별 사망 상태 추적)
        if (localPlayerLivingEntity != null && localPlayerLivingEntity.IsDead)
        {
            return;
        }
        
        try
        {
            if (DataBase.Instance == null || DataBase.Instance.gameData == null)
            {
                return;
            }

            // 손실률 가져오기
            float coinLossRate = DataBase.Instance.gameData.CoinLossRate;
            float scoreLossRate = DataBase.Instance.gameData.ScoreLossRate;

            // 코인과 점수 손실 처리 (CoinController를 통해)
            if (currentPlayerCoinController != null)
            {
                // 코인 손실 처리
                int currentCoins = currentPlayerCoinController.GetCurrentCoin();
                int coinsToLose = Mathf.RoundToInt(currentCoins * coinLossRate);
                
                if (coinsToLose > 0)
                {
                    currentPlayerCoinController.SubtractCoin(coinsToLose);
                }
                
                // 점수 손실 처리 (CoinController를 통해)
                float currentScore = currentPlayerCoinController.GetCurrentScore();
                float scoreToLose = currentScore * scoreLossRate;
                
                if (scoreToLose > 0f)
                {
                    currentPlayerCoinController.SubtractScore(scoreToLose);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ GameManager: 플레이어 사망 손실 처리 중 오류 발생 - {e.Message}");
        }
    }
    
    /// <summary>
    /// 게임 시간을 체크하여 게임 오버 조건 확인
    /// </summary>
    void CheckGameTimeForGameOver()
    {
        float currentGameTime = GetGameTime();
        float remainingTime = cachedPlayTime - currentGameTime;
        
        // 시간이 0 이하가 되면 게임 오버
        if (remainingTime <= 0f && !isGameOver)
        {
            TriggerGameOver();
        }
        
        // 게임 시간 업데이트 이벤트 (남은 시간으로 전달)
        OnGameTimeUpdated?.Invoke(Mathf.Max(0f, remainingTime));
    }
    
    /// <summary>
    /// 게임 오버 트리거
    /// </summary>
    public void TriggerGameOver()
    {
        if (isGameOver) return; // 이미 게임 오버 상태라면 중복 실행 방지
        
        isGameOver = true;
        
        // 게임 단계를 GAMEOVER로 설정 (마스터 클라이언트)
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            var props = new PhotonHashtable();
            props["gamePhase"] = "GAMEOVER";
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log("GameManager: gamePhase를 GAMEOVER로 설정");
        }
        
        // 최종 점수 가져오기
        float finalScore = GetTeddyBearScore();
        
        // 플레이어 조작 비활성화
        DisablePlayerControls();
        
        // EventSystem 중복 제거
        EnsureSingleEventSystem();
        
        
        // UI 표시
        ShowGameOverUI(finalScore);
        
        // 게임 오버 이벤트 발생 (최종 점수와 함께)
        OnGameOver?.Invoke(finalScore);
        

    }
    
    /// <summary>
    /// 모든 플레이어 조작 비활성화
    /// </summary>
    void DisablePlayerControls()
    {
        try
        {
            // 모든 플레이어 찾기
            GameObject[] allPlayerObjects = GameObject.FindGameObjectsWithTag("Player");
            
            foreach(GameObject playerObj in allPlayerObjects)
            {
                PhotonView pv = playerObj.GetComponent<PhotonView>();
                if(pv != null)
                {
                    // 애니메이션 정지 (모든 플레이어)
                    TestMoveAnimationController animController = playerObj.GetComponent<TestMoveAnimationController>();
                    if (animController != null)
                    {
                        animController.StopAllAnimations();
                    }
                    
                    // MoveController 비활성화
                    MoveController moveController = playerObj.GetComponent<MoveController>();
                    SkillController skillController = playerObj.GetComponent<SkillController>();
                    if(moveController != null && skillController != null)
                    {
                        moveController.DisableMoveControls();
                        skillController.DisableSkillControls();
                    }
                    
                    // CameraController 비활성화 (로컬 플레이어만)
                    if(pv.IsMine)
                    {
                        CameraController cameraController = playerObj.GetComponent<CameraController>();
                        if(cameraController != null)
                        {
                            cameraController.DisableCameraControl();
                            cameraController.enabled = false;
                        }
                    }
                    
                    // ✅ VictoryAnimationController 비활성화 (모든 플레이어 - 승리자만 나중에 활성화)
                    VictoryAnimationController victoryController = playerObj.GetComponent<VictoryAnimationController>();
                    if (victoryController != null)
                    {
                        victoryController.DisableVictoryControl();
                    }
                }
            }
            
            // 전역 사격 시스템 비활성화
            TestShoot.SetIsShooting(false);
            
            // 마우스 커서 표시
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameManager: 플레이어 조작 비활성화 중 오류 - {e.Message}");
        }
    }
    
    /// <summary>
    /// 게임 오버 UI 표시 (HeatUI 호환)
    /// </summary>
    /// <param name="finalScore">최종 점수</param>
    void ShowGameOverUI(float finalScore)
    {
        // InGameUIManager가 null이면 즉시 찾기 시도
        if (inGameUIManager == null)
        {
            FindInGameUIManager();
        }
        
        if (inGameUIManager != null)
        {
            // HeatUI를 통해 GameOverPanel 활성화 (SetWinnerPlayer는 InGameUIManager에서 처리)
            inGameUIManager.ShowGameOverPanel(finalScore);
        }
        else
        {
            Debug.LogError("GameManager: InGameUIManager를 찾을 수 없어 GameOverPanel을 표시할 수 없습니다.");
        }
    }
    
    
    /// <summary>
    /// 게임 오버 상태 확인
    /// </summary>
    public bool IsGameOver() => isGameOver;
    
    /// <summary>
    /// EventSystem 중복 제거 (씬에 하나만 존재하도록)
    /// </summary>
    private void EnsureSingleEventSystem()
    {
        UnityEngine.EventSystems.EventSystem[] eventSystems = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
        
        if (eventSystems.Length > 1)
        {
            // 첫 번째 EventSystem만 유지하고 나머지 제거
            for (int i = 1; i < eventSystems.Length; i++)
            {
                if (eventSystems[i] != null)
                {
                    Destroy(eventSystems[i].gameObject);
                }
            }
        }
    }

    #endregion





    #region 플레이어 체력 관리 메서드

    /// <summary>
    /// 플레이어 체력 설정
    /// </summary>
    public void SetPlayerHealth(float current, float max)
    {
        playerHealth = Mathf.Clamp(current, 0f, max);
        maxPlayerHealth = Mathf.Max(max, 1f);

        OnPlayerHealthChanged?.Invoke(playerHealth, maxPlayerHealth);

    }

    /// <summary>
    /// 플레이어 현재 체력만 변경
    /// </summary>
    public void SetPlayerCurrentHealth(float health)
    {
        SetPlayerHealth(health, maxPlayerHealth);
    }

    /// <summary>
    /// 플레이어 체력 변경 (데미지/힐)
    /// </summary>
    public void ChangePlayerHealth(float amount)
    {
        Debug.Log(amount);
        SetPlayerHealth(playerHealth + amount, maxPlayerHealth);
    }

    /// <summary>
    /// 플레이어 체력 정보 가져오기 
    /// </summary>
    public float GetPlayerHealth() => playerHealth;
    public float GetMaxPlayerHealth() => maxPlayerHealth;
    public float GetPlayerHealthRatio() => playerHealth / maxPlayerHealth;

    #endregion


    #region 이벤트 발생 메서드들

    /// <summary>
    /// 테디베어 부착 상태 변경 알림
    /// </summary>
    public void NotifyTeddyBearAttachmentChanged(bool isAttached)
    {
        OnTeddyBearAttachmentChanged?.Invoke(isAttached);

    }

    /// <summary>
    /// 테디베어 재부착 시간 변경 알림
    /// </summary>
    public void NotifyTeddyBearReattachTime(float timeRemaining)
    {
        OnTeddyBearReattachTimeChanged?.Invoke(timeRemaining);
    }

    /// <summary>
    /// 게임 시간 업데이트 알림
    /// </summary>
    public void NotifyGameTimeUpdated(float gameTime)
    {
        OnGameTimeUpdated?.Invoke(gameTime);
    }

    /// <summary>
    /// 점수 배율 업데이트 알림 (외부에서 호출용)
    /// </summary>
    public void NotifyScoreMultiplierUpdated()
    {
        float currentMultiplier = GetScoreMultiplier();
        OnScoreMultiplierUpdated?.Invoke(currentMultiplier);
    }

    /// <summary>
    /// 아이템 UI 토글 알림
    /// </summary>
    public void NotifyItemUIToggled(bool isOpen)
    {
        OnItemUIToggled?.Invoke(isOpen);

    }

    /// <summary>
    /// 크로스헤어 타겟팅 상태 변경 알림
    /// </summary>
    public void NotifyCrosshairTargeting(bool isTargeting)
    {
        OnCrosshairTargetingChanged?.Invoke(isTargeting);
    }

    /// <summary>
    /// 스킬 사용 알림
    /// </summary>
    public void NotifySkillUsed()
    {
        OnSkillUsed?.Invoke();

    }

    /// <summary>
    /// 스킬 쿨다운 시작 알림
    /// </summary>
    public void NotifySkillCooldownStarted(int skillIndex, float cooldownTime)
    {
        OnSkillCooldownStarted?.Invoke(skillIndex, cooldownTime);
    }

    /// <summary>
    /// 캐릭터 스폰 이벤트 알림
    /// </summary>
    public void NotifyCharacterSpawned()
    {
        OnCharacterSpawned?.Invoke();
    }


    #endregion




    #region 컴포넌트 찾기 메서드

    // 테디베어 찾기
    void FindTeddyBear()
    {
        if (currentTeddyBear == null)
        {
            currentTeddyBear = FindObjectOfType<Crown>();
            if (currentTeddyBear != null)
            {     
                // 게임 씬에서는 항상 점수 초기화
                currentTeddyBear.ResetScore();
            }
        }
    }
    
    /// <summary>
    /// InGameUIManager 찾기 (강화된 버전)
    /// </summary>
    void FindInGameUIManager()
    {
        if (inGameUIManager == null)
        {
            // 1차 시도: 기본 FindObjectOfType
            inGameUIManager = FindObjectOfType<InGameUIManager>();
            
            if (inGameUIManager != null)
            {
                return;
            }
            
            // 2차 시도: 비활성화된 오브젝트까지 포함해서 찾기
            InGameUIManager[] allManagers = Resources.FindObjectsOfTypeAll<InGameUIManager>();
            foreach (var manager in allManagers)
            {
                // 씬에 있는 오브젝트인지 확인 (프리팹이나 삭제된 오브젝트 제외)
                if (manager.gameObject.scene.isLoaded)
                {
                    inGameUIManager = manager;
                    
                    // 비활성화되어 있다면 활성화
                    if (!manager.gameObject.activeInHierarchy)
                    {
                        manager.gameObject.SetActive(true);
                    }
                    return;
                }
            }
        }
    }

    /// 스폰 후 플레이어 찾기 (SpawnController에서 호출)
    public void FindPlayerAfterSpawn() // ✅ 기존 메서드명 유지 (내부 로직 변경)
    {
        try
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                PhotonView pv = playerObject.GetComponent<PhotonView>(); // ✅ PhotonView 가져오기
                // ✅ 로컬 플레이어의 오브젝트인지 확인하는 조건 추가
                if (pv != null && pv.IsMine)
                {
                    // 기존에 구독되어 있었다면 해제 (중복 구독 방지)
                    if (localPlayerLivingEntity != null)
                    {
                        LivingEntity.OnAnyLivingEntityHealthChanged -= HandleAnyLivingEntityHealthChanged;
                    }

                    localPlayerLivingEntity = playerObject.GetComponent<LivingEntity>(); // ✅ localPlayerLivingEntity에 할당
                    if (localPlayerLivingEntity != null)
                    {
                        // ✅ 이곳에서 이벤트 구독: 로컬 플레이어의 LivingEntity가 확정되었을 때!
                        LivingEntity.OnAnyLivingEntityHealthChanged += HandleAnyLivingEntityHealthChanged;

                        // player = playerObject.GetComponent<LivingEntity>(); // ❌ 삭제: 더 이상 사용하지 않음
                        playerHealth = localPlayerLivingEntity.CurrentHealth; // ✅ localPlayerLivingEntity 사용
                        maxPlayerHealth = localPlayerLivingEntity.StartingHealth; // ✅ localPlayerLivingEntity 사용

                        // 플레이어의 CoinController 찾기
                        FindPlayerCoinController(playerObject);

                        // HUD에 스킬 데이터 업데이트 알림
                        NotifyHUDToUpdateSkillData();
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ GameManager: 플레이어 찾기 중 오류 발생 - {e.Message}");
        }
    }

    public LivingEntity[] GetAllPlayerLivingEntities()
    {
        // 실시간으로 모든 플레이어 찾기 (멀티플레이어 환경에서 안전)
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        List<LivingEntity> livingEntities = new List<LivingEntity>();
        
        foreach(GameObject playerObj in playerObjects)
        {
            LivingEntity livingEntity = playerObj.GetComponent<LivingEntity>();
            if(livingEntity != null)
            {
                livingEntities.Add(livingEntity);
            }
        }
        
        allPlayerLivingEntities = livingEntities.ToArray();
        return allPlayerLivingEntities;
    }

    // ✅ 추가: LivingEntity의 체력 변경 이벤트를 처리하는 핸들러
    private void HandleAnyLivingEntityHealthChanged(float current, float max, LivingEntity changedEntity)
    {
        // 변경된 LivingEntity가 로컬 플레이어의 LivingEntity와 동일한지 확인
        if (localPlayerLivingEntity != null && changedEntity == localPlayerLivingEntity)
        {
            // 로컬 플레이어의 체력이므로 HUD에 업데이트 이벤트를 발생시킵니다.
            playerHealth = current;
            maxPlayerHealth = max;
            OnPlayerHealthChanged?.Invoke(playerHealth, maxPlayerHealth);
        }
    }

    /// <summary>
    /// 플레이어의 CoinController 찾기
    /// </summary>
    /// <param name="playerObject">플레이어 오브젝트</param>
    private void FindPlayerCoinController(GameObject playerObject)
    {
        if (playerObject == null) return;

        // 플레이어 오브젝트에서 CoinController 찾기
        currentPlayerCoinController = playerObject.GetComponent<CoinController>();

        // 직접 찾지 못한 경우 자식 오브젝트에서 찾기
        if (currentPlayerCoinController == null)
        {
            currentPlayerCoinController = playerObject.GetComponentInChildren<CoinController>();
        }
    }

    /// <summary>
    /// 현재 플레이어의 CoinController 가져오기
    /// </summary>
    /// <returns>현재 플레이어의 CoinController</returns>
    public CoinController GetCurrentPlayerCoinController()
    {
        if (currentPlayerCoinController == null)
        {
            return null;
        }

        return currentPlayerCoinController;
    }

    #endregion


    #region HUD 업데이트 메서드

    /// <summary>
    /// HUD에 스킬 데이터 업데이트 알림 (이벤트 기반으로 변경)
    /// </summary>
    private void NotifyHUDToUpdateSkillData()
    {
        // 이벤트를 통해 HUD에 알림 (직접 호출 대신)
        OnCharacterSpawned?.Invoke();
    }

    #endregion
    
    
    /// <summary>
    /// 게임 UI 활성화
    /// </summary>
    private void EnableGameUI()
    {
        // 게임 시작 이벤트들 발생
        OnCharacterSpawned?.Invoke();
    }



    // 상점 시간 관리 메서드 제거 (Shop.cs에서 직접 관리)
}

