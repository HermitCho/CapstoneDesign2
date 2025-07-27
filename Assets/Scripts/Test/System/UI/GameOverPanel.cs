using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Febucci.UI;
using Michsky.UI.Heat;
using Photon.Pun;

/// <summary>
/// 🎮 게임 오버 패널
/// 게임 종료 시 최종 점수를 표시하고 관련 UI를 관리
/// </summary>
public class GameOverPanel : MonoBehaviour
{
    #region 인스펙터 할당 변수
    
    [Header("점수 표시 UI")]
    [SerializeField] private TextMeshProUGUI[] finalScoreTexts;
    
    [Header("버튼 컴포넌트")]
    [SerializeField] private ButtonManager mainMenuButton;

    [Header("모달 매니저 컴포넌트")]
    [SerializeField] private ModalWindowManager modalWindowManager;



    
    #endregion
    
    #region 변수
    
    private float currentFinalScore = 0f;
    private float bestScore = 0f;
    private const string BEST_SCORE_KEY = "BestScore"; // PlayerPrefs 키
    private float modalDelayTime = 5f;

    // 플레이어 점수 데이터 (멀티플레이 확장용)
    private List<PlayerScoreData> allPlayerScores = new List<PlayerScoreData>();
    private bool isModalOpened = false;
    
    #endregion
    
    #region 데이터 구조체
    
    /// <summary>
    /// 플레이어 점수 데이터 구조체 (멀티플레이 확장용)
    /// </summary>
    [System.Serializable]
    public struct PlayerScoreData
    {
        public string playerName;
        public float score;
        public bool isLocalPlayer;
        
        public PlayerScoreData(string name, float playerScore, bool isLocal = false)
        {
            playerName = name;
            score = playerScore;
            isLocalPlayer = isLocal;
        }
    }
    
    #endregion
    
    #region Unity 생명주기
    
    void Awake()
    {
        // 점수 초기화
        currentFinalScore = 0f;
        isModalOpened = false;
        
        // 최고 점수 로드
        LoadBestScore();
        
        // 버튼 이벤트 연결
        SetupButtons();
        
        Debug.Log("✅ GameOverPanel: 초기화 완료 - 점수 초기화됨");
    }
    
    void OnEnable()
    {
        // 점수 초기화 (씬 전환 시 안전장치)
        currentFinalScore = 0f;
        isModalOpened = false;
        
        // GameManager의 게임 오버 이벤트 구독
        GameManager.OnGameOver += OnGameOverReceived;
        
        Debug.Log("✅ GameOverPanel: 게임 오버 이벤트 구독 및 점수 초기화");
    }
    
    void OnDisable()
    {
        // 이벤트 구독 해제
       GameManager.OnGameOver -= OnGameOverReceived;
        
        Debug.Log("❌ GameOverPanel: 게임 오버 이벤트 구독 해제");
    }
    
    #endregion
    
    #region 초기화 메서드
    
    /// <summary>
    /// 버튼들 초기화
    /// </summary>
    void SetupButtons()
    {

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        }
    }
    
    #endregion
    
    #region 게임 오버 처리 메서드
    
    /// <summary>
    /// GameManager로부터 게임 오버 이벤트를 받았을 때
    /// </summary>
    /// <param name="finalScore">최종 점수</param>
    void OnGameOverReceived(float finalScore)
    {
        // 점수 초기화 및 새 점수 설정
        currentFinalScore = finalScore;
        isModalOpened = false; // 모달 상태 초기화
        
        // 최고 점수 업데이트 확인
        UpdateBestScore(finalScore);
        
        // 현재는 싱글플레이어 환경이므로 로컬 플레이어 점수만 추가
        allPlayerScores.Clear();
        allPlayerScores.Add(new PlayerScoreData("Player", finalScore, true));
        
        // UI 업데이트
        UpdateScoreUI();
        
        // 모달창 지연 표시 시작
        StartCoroutine(ShowResultModalAfterDelay());
        
        Debug.Log($"🎮 GameOverPanel: 게임 오버 처리 완료 - 최종 점수: {finalScore}");
    }
    
    /// <summary>
    /// 점수 UI 업데이트 (초기 표시용)
    /// </summary>
    void UpdateScoreUI()
    {
        // 초기 게임오버 패널에 표시할 기본 점수 (첫 번째 텍스트만 사용)
        if (finalScoreTexts != null && finalScoreTexts.Length > 0 && finalScoreTexts[0] != null)
        {
            finalScoreTexts[0].text = $"최종 점수: {currentFinalScore:F0}";
        }
        else
        {
            Debug.LogWarning("⚠️ GameOverPanel: finalScoreTexts 배열이 비어있거나 null입니다.");
        }
    }
    
    #endregion
    
    #region 점수 관리 메서드
    
    /// <summary>
    /// 최고 점수 로드
    /// </summary>
    void LoadBestScore()
    {
        bestScore = PlayerPrefs.GetFloat(BEST_SCORE_KEY, 0f);
        Debug.Log($"✅ GameOverPanel: 최고 점수 로드 - {bestScore}");
    }
    
    /// <summary>
    /// 최고 점수 업데이트 (필요시)
    /// </summary>
    /// <param name="newScore">새로운 점수</param>
    void UpdateBestScore(float newScore)
    {
        if (newScore > bestScore)
        {
            bestScore = newScore;
            PlayerPrefs.SetFloat(BEST_SCORE_KEY, bestScore);
            PlayerPrefs.Save();
            
            Debug.Log($"🏆 GameOverPanel: 새로운 최고 점수 갱신! {bestScore}");
        }
    }
    
    /// <summary>
    /// 현재 최종 점수 가져오기
    /// </summary>
    /// <returns>최종 점수</returns>
    public float GetFinalScore()
    {
        return currentFinalScore;
    }
    
    /// <summary>
    /// 최고 점수 가져오기
    /// </summary>
    /// <returns>최고 점수</returns>
    public float GetBestScore()
    {
        return bestScore;
    }
    
    #endregion
    
    #region 모달창 관리 메서드
    
    /// <summary>
    /// 지연 후 결과 모달창 표시
    /// </summary>
    IEnumerator ShowResultModalAfterDelay()
    {
        // modalDelayTime만큼 대기
        yield return new WaitForSeconds(modalDelayTime);
        
        // 모달창 열기
        ShowResultModal();
    }
    
    /// <summary>
    /// 결과 모달창 표시
    /// </summary>
    void ShowResultModal()
    {
        if (modalWindowManager != null && !isModalOpened)
        {
            // 모달창 열기
            modalWindowManager.OpenWindow();
            isModalOpened = true;
            
            // 모달창에 점수 데이터 표시
            UpdateModalScoreDisplay();
            
            Debug.Log("✅ GameOverPanel: 결과 모달창 표시 완료");
        }
        else if (modalWindowManager == null)
        {
            Debug.LogError("❌ GameOverPanel: modalWindowManager가 할당되지 않았습니다.");
        }
    }
    
    /// <summary>
    /// 모달창 내 점수 표시 업데이트
    /// </summary>
    void UpdateModalScoreDisplay()
    {
        if (finalScoreTexts == null || finalScoreTexts.Length == 0)
        {
            Debug.LogWarning("⚠️ GameOverPanel: finalScoreTexts 배열이 비어있습니다.");
            return;
        }
        
        // 현재 싱글플레이어 환경에서는 첫 번째 플레이어만 표시
        for (int i = 0; i < allPlayerScores.Count && i < finalScoreTexts.Length; i++)
        {
            if (finalScoreTexts[i] != null)
            {
                PlayerScoreData playerData = allPlayerScores[i];
                
                // TextAnimator 사용 가능 시 애니메이션 효과 적용
                TextAnimator_TMP textAnimator = finalScoreTexts[i].GetComponent<TextAnimator_TMP>();
                
                                 // 순위와 함께 점수 표시
                 string rankText = GetRankDisplay(i + 1);
                 string scoreText = $"{rankText} {playerData.playerName}: {playerData.score:F0}점";
                 
                                 // 로컬 플레이어는 특별 표시
                if (playerData.isLocalPlayer)
                {
                    // TextAnimator 효과 추가 (있다면)
                    if (textAnimator != null)
                    {
                        scoreText = $"<bounce>{rankText}</bounce> {playerData.playerName}: {playerData.score:F0}점";
                    }
                    
                    scoreText = $"<color=white>{scoreText}</color>";
                }
                
                // 텍스트 설정
                if (textAnimator != null)
                {
                    textAnimator.SetText(scoreText);
                }
                else
                {
                    finalScoreTexts[i].text = scoreText;
                }
                
                // 텍스트 활성화
                finalScoreTexts[i].gameObject.SetActive(true);
                
                Debug.Log($"✅ GameOverPanel: 플레이어 {i} 점수 표시 - {playerData.playerName}: {playerData.score}");
            }
        }
        
        // 사용하지 않는 텍스트는 비활성화
        for (int i = allPlayerScores.Count; i < finalScoreTexts.Length; i++)
        {
            if (finalScoreTexts[i] != null)
            {
                finalScoreTexts[i].gameObject.SetActive(false);
            }
        }
    }
    
    #endregion
    
    #region 멀티플레이어 확장 메서드 (포톤2용)
    
    /// <summary>
    /// 멀티플레이어 점수 데이터 설정 (포톤2 확장용)
    /// </summary>
    /// <param name="playerScores">모든 플레이어의 점수 데이터</param>
    public void SetMultiPlayerScores(List<PlayerScoreData> playerScores)
    {
        if (playerScores == null || playerScores.Count == 0)
        {
            Debug.LogWarning("⚠️ GameOverPanel: 플레이어 점수 데이터가 비어있습니다.");
            return;
        }
        
        // 점수 데이터 복사 및 정렬 (점수 높은 순으로)
        allPlayerScores.Clear();
        allPlayerScores.AddRange(playerScores);
        allPlayerScores.Sort((a, b) => b.score.CompareTo(a.score));
        
        Debug.Log($"✅ GameOverPanel: 멀티플레이어 점수 데이터 설정 완료 - {allPlayerScores.Count}명");
        
        // 모달이 이미 열려있다면 즉시 업데이트
        if (isModalOpened)
        {
            UpdateModalScoreDisplay();
        }
    }
    
    /// <summary>
    /// 단일 플레이어 점수 추가 (포톤2 실시간 업데이트용)
    /// </summary>
    /// <param name="playerName">플레이어 이름</param>
    /// <param name="score">플레이어 점수</param>
    /// <param name="isLocal">로컬 플레이어 여부</param>
    public void AddPlayerScore(string playerName, float score, bool isLocal = false)
    {
        // 기존 플레이어 데이터가 있는지 확인
        int existingIndex = allPlayerScores.FindIndex(p => p.playerName == playerName);
        
        if (existingIndex >= 0)
        {
            // 기존 플레이어 점수 업데이트
            PlayerScoreData updatedData = allPlayerScores[existingIndex];
            updatedData.score = score;
            allPlayerScores[existingIndex] = updatedData;
        }
        else
        {
            // 새 플레이어 추가
            allPlayerScores.Add(new PlayerScoreData(playerName, score, isLocal));
        }
        
        // 점수 순으로 재정렬
        allPlayerScores.Sort((a, b) => b.score.CompareTo(a.score));
        
        Debug.Log($"✅ GameOverPanel: 플레이어 '{playerName}' 점수 추가/업데이트 - {score}점");
        
        // 모달이 열려있다면 즉시 업데이트
        if (isModalOpened)
        {
            UpdateModalScoreDisplay();
        }
    }
    
    /// <summary>
    /// 현재 플레이어 순위 확인 (포톤2용)
    /// </summary>
    /// <param name="playerName">확인할 플레이어 이름</param>
    /// <returns>순위 (1부터 시작, 없으면 -1)</returns>
    public int GetPlayerRank(string playerName)
    {
        for (int i = 0; i < allPlayerScores.Count; i++)
        {
            if (allPlayerScores[i].playerName == playerName)
            {
                return i + 1; // 1부터 시작하는 순위
            }
        }
        return -1; // 플레이어를 찾을 수 없음
    }
    
    /// <summary>
    /// 필요한 finalScoreTexts 배열 크기 계산 (포톤2용)
    /// </summary>
    /// <returns>필요한 텍스트 개수</returns>
    public int GetRequiredTextCount()
    {
        return allPlayerScores.Count;
    }
    
    /// <summary>
    /// 모든 플레이어 점수 데이터 가져오기 (포톤2용)
    /// </summary>
    /// <returns>플레이어 점수 데이터 리스트</returns>
    public List<PlayerScoreData> GetAllPlayerScores()
    {
        return new List<PlayerScoreData>(allPlayerScores); // 복사본 반환
    }
    
    #endregion
    
    #region 유틸리티 메서드
    
    /// <summary>
    /// 순위 표시 문자열 생성
    /// </summary>
    /// <param name="rank">순위 (1부터 시작)</param>
    /// <returns>순위 표시 문자열</returns>
    string GetRankDisplay(int rank)
    {
        switch (rank)
        {
            case 1: return "1st";
            case 2: return "2nd";
            case 3: return "3rd";
            default: return $"{rank}th";
        }
    }
    
    #endregion
    
    #region 버튼 이벤트 메서드

    /// <summary>
    /// 메인 메뉴 버튼 클릭
    /// </summary>
    void OnMainMenuButtonClicked()
    {
        Debug.Log("🏠 GameOverPanel: 메인 메뉴 버튼 클릭");
        
        // 점수 초기화
        currentFinalScore = 0f;
        isModalOpened = false;
        
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.Disconnect();
        // 메인 메뉴로 이동 (씬 이름은 프로젝트에 맞게 수정)
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }
    
    #endregion

}
