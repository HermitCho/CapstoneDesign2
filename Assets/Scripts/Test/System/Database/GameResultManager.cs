using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// 게임 결과 처리 및 통계 업데이트 관리 클래스
/// 게임 종료 시 순위에 따라 Win/Lose/Rate를 구글 스프레드시트에 업데이트
/// </summary>
public class GameResultManager : MonoBehaviourPunCallbacks
{
    private static GameResultManager _instance;
    public static GameResultManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GameResultManager");
                _instance = go.AddComponent<GameResultManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("레이팅 변화량 설정")]
    [SerializeField] private int firstPlaceRate = 14;   // 1등 레이팅 증가
    [SerializeField] private int secondPlaceRate = 6;   // 2등 레이팅 증가
    [SerializeField] private int thirdPlaceRate = 0;    // 3등 레이팅 변화 없음
    [SerializeField] private int fourthPlaceRate = -9;  // 4등 레이팅 감소
    [Space(10)]
    [Header("Money 변화량 설정")]
    [SerializeField] private int firstPlaceMoney = 100;   // 1등 머니 증가
    [SerializeField] private int secondPlaceMoney = 50;   // 2등 머니 증가
    [SerializeField] private int thirdPlaceMoney = 25;   // 3등 머니 증가
    [SerializeField] private int fourthPlaceMoney = 10;  // 4등 머니 감소

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 게임 종료 시 현재 로그인된 사용자의 결과를 업데이트
    /// </summary>
    /// <param name="playerRank">플레이어 순위 (1~4등)</param>
    public void UpdateCurrentUserGameResult(int playerRank)
    {
        Debug.Log($"GameResultManager: 게임 결과 업데이트 시작 - 순위: {playerRank}등");
        
        // CurrentUser 인스턴스 확인
        if (CurrentUser.Instance == null)
        {
            Debug.LogError("GameResultManager: CurrentUser 인스턴스가 없습니다.");
            return;
        }
        
        // 로그인 상태 확인
        if (!CurrentUser.Instance.IsLoggedIn())
        {
            Debug.LogWarning("GameResultManager: 로그인된 사용자가 없습니다.");
            return;
        }

        string userId = CurrentUser.Instance.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("GameResultManager: 사용자 ID가 비어있습니다.");
            return;
        }
        
        Debug.Log($"GameResultManager: 사용자 ID 확인 완료 - {userId}");

        // GoogleSheetsManager 연결 상태 확인
        if (GoogleSheetsManager.Instance == null)
        {
            Debug.LogError("GameResultManager: GoogleSheetsManager 인스턴스가 없습니다.");
            return;
        }
        
        if (!GoogleSheetsManager.Instance.IsConnected())
        {
            Debug.LogError("GameResultManager: GoogleSheetsManager가 연결되지 않았습니다.");
            return;
        }

        StartCoroutine(UpdateGameResultCoroutine(userId, playerRank));
    }

    /// <summary>
    /// 특정 사용자의 게임 결과 업데이트
    /// </summary>
    /// <param name="userId">사용자 ID</param>
    /// <param name="playerRank">플레이어 순위 (1~4등)</param>
    public void UpdateUserGameResult(string userId, int playerRank)
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("GameResultManager: 사용자 ID가 비어있습니다.");
            return;
        }

        StartCoroutine(UpdateGameResultCoroutine(userId, playerRank));
    }

    /// <summary>
    /// 게임 결과 업데이트 코루틴 (재시도 로직 포함)
    /// </summary>
    private IEnumerator UpdateGameResultCoroutine(string userId, int playerRank)
    {
        Debug.Log($"GameResultManager: 게임 결과 업데이트 시작 - 사용자: {userId}, 순위: {playerRank}등");

        // 구글 스프레드시트 연결 확인
        if (!GoogleSheetsManager.Instance.IsConnected())
        {
            Debug.LogError("GameResultManager: 구글 스프레드시트에 연결되지 않았습니다.");
            yield break;
        }

        // 순위 유효성 검사
        if (playerRank < 1 || playerRank > 4)
        {
            Debug.LogError($"GameResultManager: 잘못된 순위입니다 - {playerRank}등 (1~4등만 유효)");
            yield break;
        }

        // 최대 3번 재시도
        int maxRetries = 3;
        int currentRetry = 0;
        bool updateSuccess = false;
        string updateMessage = "";

        while (currentRetry < maxRetries && !updateSuccess)
        {
            if (currentRetry > 0)
            {
                Debug.Log($"GameResultManager: 재시도 {currentRetry}/{maxRetries - 1}");
                yield return new WaitForSeconds(2f); // 2초 대기 후 재시도
            }

            bool updateCompleted = false;

            // 순위에 따른 재화 지급량 결정
            int moneyReward = GetMoneyRewardByRank(playerRank);
            
            // 구글 스프레드시트에 게임 결과 업데이트 (재화 포함)
            GoogleSheetsManager.Instance.UpdateGameResult(userId, playerRank, moneyReward, (success, message) =>
            {
                updateSuccess = success;
                updateMessage = message;
                updateCompleted = true;
            });

            // 업데이트 완료까지 대기 (최대 30초)
            float timeout = 30f;
            float timer = 0f;
            
            while (!updateCompleted && timer < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            
            // 타임아웃 처리
            if (!updateCompleted)
            {
                Debug.LogWarning($"GameResultManager: 업데이트 타임아웃 - 시도 {currentRetry + 1}");
                updateMessage = "타임아웃";
            }

            currentRetry++;
        }

        if (updateSuccess)
        {
            Debug.Log($"GameResultManager: 게임 결과 업데이트 성공 - {userId}: {playerRank}등 (시도: {currentRetry})");
            
            // 현재 로그인된 사용자인 경우 Google Sheets에서 최신 데이터를 가져와서 동기화
            if (CurrentUser.Instance != null && CurrentUser.Instance.IsLoggedIn() && CurrentUser.Instance.GetUserId() == userId)
            {
                // Google Sheets 업데이트가 완료된 후 최신 데이터로 동기화
                yield return StartCoroutine(SyncCurrentUserFromSheets(userId));
            }
        }
        else
        {
            Debug.LogError($"GameResultManager: 게임 결과 업데이트 최종 실패 - {updateMessage} (총 {currentRetry}회 시도)");
        }
    }

    /// <summary>
    /// Google Sheets에서 최신 사용자 데이터를 가져와서 CurrentUser 동기화
    /// (LeaderboardPanel.cs와 동일한 방식)
    /// </summary>
    private IEnumerator SyncCurrentUserFromSheets(string userId)
    {
        Debug.Log($"[GameResultManager] Google Sheets에서 최신 데이터 동기화 시작 - UserId: {userId}");
        
        // Google Sheets 서버 반영 시간을 고려하여 대기
        yield return new WaitForSeconds(1f);
        
        // GoogleSheetsManager에서 데이터 강제 재로드
        if (GoogleSheetsManager.Instance != null)
        {
            GoogleSheetsManager.Instance.LoadUserData();
            
            // 데이터 로드 완료 대기 (최대 5초)
            float timeout = 5f;
            float elapsed = 0f;
            
            while (!GoogleSheetsManager.Instance.IsDataLoaded() && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            // 최신 데이터 가져오기
            var allUserData = GoogleSheetsManager.Instance.GetAllUserData();
            var userData = allUserData.FirstOrDefault(u => u.userId == userId);
            
            if (userData != null)
            {
                // CurrentUser를 Google Sheets의 최신 데이터로 동기화
                CurrentUser.Instance.UpdateGameStats(userData.win, userData.lose, userData.rate, userData.money);
                
                Debug.Log($"[GameResultManager] CurrentUser 동기화 완료 - Win: {userData.win}, Lose: {userData.lose}, Rate: {userData.rate}, Money: {userData.money}");
            }
            else
            {
                Debug.LogWarning($"[GameResultManager] Google Sheets에서 사용자 데이터를 찾을 수 없음: {userId}");
            }
        }
    }

    /// <summary>
    /// 순위에 따른 레이팅 변화량 반환
    /// </summary>
    private int GetRateChangeByRank(int rank)
    {
        switch (rank)
        {
            case 1: return firstPlaceRate;   // +14
            case 2: return secondPlaceRate;  // +6
            case 3: return thirdPlaceRate;   // 0
            case 4: return fourthPlaceRate;  // -9
            default: return 0;
        }
    }

    /// <summary>
    /// 순위에 따른 재화 지급량 반환
    /// </summary>
    private int GetMoneyRewardByRank(int rank)
    {
        switch (rank)
        {
            case 1: return firstPlaceMoney;   // 100
            case 2: return secondPlaceMoney;  // 50
            case 3: return thirdPlaceMoney;   // 25
            case 4: return fourthPlaceMoney;  // 10
            default: return 0;
        }
    }

    /// <summary>
    /// 순위에 따른 결과 문자열 반환
    /// </summary>
    private string GetRankResultText(int rank)
    {
        switch (rank)
        {
            case 1: return "승리 (1등)";
            case 2: return "2등";
            case 3: return "3등";
            case 4: return "4등";
            default: return "알 수 없음";
        }
    }

    /// <summary>
    /// 멀티플레이어 환경에서 모든 플레이어의 게임 결과 업데이트
    /// GameOverController에서 호출됨
    /// </summary>
    /// <param name="playerRankings">플레이어 순위 정보</param>
    public void UpdateAllPlayersGameResult(List<GameOverController.PlayerRankData> playerRankings)
    {
        if (playerRankings == null || playerRankings.Count == 0)
        {
            Debug.LogWarning("GameResultManager: 플레이어 순위 정보가 없습니다.");
            return;
        }

        Debug.Log($"GameResultManager: 전체 플레이어 게임 결과 업데이트 시작 - {playerRankings.Count}명");

        // 각 플레이어의 결과를 순위에 따라 업데이트
        for (int i = 0; i < playerRankings.Count; i++)
        {
            var playerData = playerRankings[i];
            int rank = i + 1; // 순위는 1부터 시작

            // Photon 플레이어에서 사용자 ID 가져오기 (닉네임을 사용자 ID로 사용)
            string userId = GetUserIdFromPlayerData(playerData);
            
            if (!string.IsNullOrEmpty(userId))
            {
                // 로컬 플레이어인 경우에만 업데이트 (다른 플레이어는 각자의 클라이언트에서 처리)
                if (playerData.isLocalPlayer)
                {
                    UpdateUserGameResult(userId, rank);
                }
            }
        }
    }

    /// <summary>
    /// PlayerRankData에서 사용자 ID 추출
    /// </summary>
    private string GetUserIdFromPlayerData(GameOverController.PlayerRankData playerData)
    {
        // 현재 로그인된 사용자인 경우
        if (playerData.isLocalPlayer && CurrentUser.Instance.IsLoggedIn())
        {
            return CurrentUser.Instance.GetUserId();
        }

        // 다른 플레이어의 경우 닉네임을 사용자 ID로 사용 (임시)
        // 실제로는 Photon Custom Properties에서 사용자 ID를 가져와야 함
        return playerData.nickname;
    }

    /// <summary>
    /// 디버그용: 테스트 게임 결과 업데이트
    /// </summary>
    [ContextMenu("테스트: 1등 결과 업데이트")]
    private void TestFirstPlace()
    {
        UpdateCurrentUserGameResult(1);
    }

    [ContextMenu("테스트: 4등 결과 업데이트")]
    private void TestLastPlace()
    {
        UpdateCurrentUserGameResult(4);
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
