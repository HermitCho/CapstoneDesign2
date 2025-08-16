using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Febucci.UI;
using Michsky.UI.Heat;
using Photon.Pun;

public class GameOverPanel : MonoBehaviour
{
    [Header("점수 표시 UI")]
    [SerializeField] private TextMeshProUGUI[] finalScoreTexts;
    
    [Header("버튼 컴포넌트")]
    [SerializeField] private ButtonManager mainMenuButton;

    [Header("모달 매니저 컴포넌트")]
    [SerializeField] private ModalWindowManager modalWindowManager;

    private float currentFinalScore = 0f;
    private float bestScore = 0f;
    private const string BEST_SCORE_KEY = "BestScore";
    private float modalDelayTime = 5f;

    private List<PlayerScoreData> allPlayerScores = new List<PlayerScoreData>();
    private bool isModalOpened = false;
    
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
    
    void Awake()
    {
        currentFinalScore = 0f;
        isModalOpened = false;
        LoadBestScore();
        SetupButtons();
    }
    
    void OnEnable()
    {
        currentFinalScore = 0f;
        isModalOpened = false;
        GameManager.OnGameOver += OnGameOverReceived;
    }
    
    void OnDisable()
    {
        GameManager.OnGameOver -= OnGameOverReceived;
    }
    
    void SetupButtons()
    {
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        }
    }
    
    void OnGameOverReceived(float finalScore)
    {
        float latestScore = finalScore;
        if (GameManager.Instance != null)
        {
            latestScore = GameManager.Instance.GetTeddyBearScore();
        }
        
        currentFinalScore = latestScore;
        isModalOpened = false;
        
        UpdateBestScore(latestScore);
        
        allPlayerScores.Clear();
        allPlayerScores.Add(new PlayerScoreData("Player", latestScore, true));
        
        UpdateScoreUI();
        StartCoroutine(ShowResultModalAfterDelay());
    }
    
    void UpdateScoreUI()
    {
        if (finalScoreTexts != null && finalScoreTexts.Length > 0 && finalScoreTexts[0] != null)
        {
            finalScoreTexts[0].text = $"최종 점수: {currentFinalScore:F0}";
        }
    }
    
    void LoadBestScore()
    {
        bestScore = PlayerPrefs.GetFloat(BEST_SCORE_KEY, 0f);
    }
    
    void UpdateBestScore(float newScore)
    {
        if (newScore > bestScore)
        {
            bestScore = newScore;
            PlayerPrefs.SetFloat(BEST_SCORE_KEY, bestScore);
            PlayerPrefs.Save();
        }
    }
    
    public float GetFinalScore() => currentFinalScore;
    public float GetBestScore() => bestScore;
    
    IEnumerator ShowResultModalAfterDelay()
    {
        yield return new WaitForSeconds(modalDelayTime);
        ShowResultModal();
    }
    
    void ShowResultModal()
    {
        if (modalWindowManager != null && !isModalOpened)
        {
            modalWindowManager.OpenWindow();
            isModalOpened = true;
            UpdateModalScoreDisplay();
        }
    }
    
    void UpdateModalScoreDisplay()
    {
        if (finalScoreTexts == null || finalScoreTexts.Length == 0) return;
        
        for (int i = 0; i < allPlayerScores.Count && i < finalScoreTexts.Length; i++)
        {
            if (finalScoreTexts[i] != null)
            {
                PlayerScoreData playerData = allPlayerScores[i];
                TextAnimator_TMP textAnimator = finalScoreTexts[i].GetComponent<TextAnimator_TMP>();
                
                string rankText = GetRankDisplay(i + 1);
                string scoreText = $"{rankText} {playerData.playerName}: {playerData.score:F0}점";
                
                if (playerData.isLocalPlayer)
                {
                    if (textAnimator != null)
                    {
                        scoreText = $"<bounce>{rankText}</bounce> {playerData.playerName}: {playerData.score:F0}점";
                    }
                    scoreText = $"<color=white>{scoreText}</color>";
                }
                
                if (textAnimator != null)
                {
                    textAnimator.SetText(scoreText);
                }
                else
                {
                    finalScoreTexts[i].text = scoreText;
                }
                
                finalScoreTexts[i].gameObject.SetActive(true);
            }
        }
        
        for (int i = allPlayerScores.Count; i < finalScoreTexts.Length; i++)
        {
            if (finalScoreTexts[i] != null)
            {
                finalScoreTexts[i].gameObject.SetActive(false);
            }
        }
    }
    
    public void SetMultiPlayerScores(List<PlayerScoreData> playerScores)
    {
        if (playerScores == null || playerScores.Count == 0) return;
        
        allPlayerScores.Clear();
        allPlayerScores.AddRange(playerScores);
        allPlayerScores.Sort((a, b) => b.score.CompareTo(a.score));
        
        if (isModalOpened)
        {
            UpdateModalScoreDisplay();
        }
    }
    
    public void AddPlayerScore(string playerName, float score, bool isLocal = false)
    {
        int existingIndex = allPlayerScores.FindIndex(p => p.playerName == playerName);
        
        if (existingIndex >= 0)
        {
            PlayerScoreData updatedData = allPlayerScores[existingIndex];
            updatedData.score = score;
            allPlayerScores[existingIndex] = updatedData;
        }
        else
        {
            allPlayerScores.Add(new PlayerScoreData(playerName, score, isLocal));
        }
        
        allPlayerScores.Sort((a, b) => b.score.CompareTo(a.score));
        
        if (isModalOpened)
        {
            UpdateModalScoreDisplay();
        }
    }
    
    public int GetPlayerRank(string playerName)
    {
        for (int i = 0; i < allPlayerScores.Count; i++)
        {
            if (allPlayerScores[i].playerName == playerName)
            {
                return i + 1;
            }
        }
        return -1;
    }
    
    public int GetRequiredTextCount() => allPlayerScores.Count;
    
    public List<PlayerScoreData> GetAllPlayerScores()
    {
        return new List<PlayerScoreData>(allPlayerScores);
    }
    
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
    
    void OnMainMenuButtonClicked()
    {
        currentFinalScore = 0f;
        isModalOpened = false;
        
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.Disconnect();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }
}
