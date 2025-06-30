using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 테디베어 점수 UI 관리 스크립트
/// HeatUI와 연동하기 위한 예제 템플릿
/// </summary>
public class TeddyBearScoreUI : MonoBehaviour
{
    [Header("UI 요소들")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI multiplierText;
    [SerializeField] private TextMeshProUGUI gameTimeText;
    [SerializeField] private TextMeshProUGUI attachStatusText;
    [SerializeField] private TextMeshProUGUI scoreStatusText;
    
    [Header("점수 표시 설정")]
    [SerializeField] private string scoreFormat = "점수: {0:F0}";
    [SerializeField] private string multiplierFormat = "배율: {0:F2}x";
    [SerializeField] private string gameTimeFormat = "시간: {0:F0}초";
    
    private void OnEnable()
    {
        // GameManager의 이벤트 구독
        GameManager.OnScoreUpdated += UpdateScoreDisplay;
        GameManager.OnScoreMultiplierUpdated += UpdateMultiplierDisplay;
    }
    
    private void OnDisable()
    {
        // 이벤트 구독 해제
        GameManager.OnScoreUpdated -= UpdateScoreDisplay;
        GameManager.OnScoreMultiplierUpdated -= UpdateMultiplierDisplay;
    }
    
    void Start()
    {
        // 초기 UI 업데이트
        UpdateAllUI();
    }
    
    void Update()
    {
        // 매 프레임마다 게임 시간과 부착 상태 업데이트
        UpdateGameTimeDisplay();
        UpdateAttachStatusDisplay();
        UpdateScoreStatusDisplay();
    }
    
    // 점수 표시 업데이트
    void UpdateScoreDisplay(float score)
    {
        if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, score);
        }
    }
    
    // 배율 표시 업데이트
    void UpdateMultiplierDisplay(float multiplier)
    {
        if (multiplierText != null)
        {
            multiplierText.text = string.Format(multiplierFormat, multiplier);
        }
    }
    
    // 게임 시간 표시 업데이트
    void UpdateGameTimeDisplay()
    {
        if (gameTimeText != null && GameManager.Instance != null)
        {
            float gameTime = GameManager.Instance.GetGameTime();
            gameTimeText.text = string.Format(gameTimeFormat, gameTime);
        }
    }
    
    // 부착 상태 표시 업데이트
    void UpdateAttachStatusDisplay()
    {
        if (attachStatusText != null && GameManager.Instance != null)
        {
            bool isAttached = GameManager.Instance.IsTeddyBearAttached();
            
            if (isAttached)
            {
                attachStatusText.text = "테디베어 부착됨";
                attachStatusText.color = Color.green;
            }
            else
            {
                float timeUntilReattach = GameManager.Instance.GetTimeUntilReattach();
                if (timeUntilReattach > 0)
                {
                    attachStatusText.text = $"테디베어 미부착 (재부착까지 {timeUntilReattach:F1}초)";
                    attachStatusText.color = Color.yellow;
                }
                else
                {
                    attachStatusText.text = "테디베어 미부착 (재부착 가능)";
                    attachStatusText.color = Color.red;
                }
            }
        }
    }
    
    // 점수 상태 표시 업데이트
    void UpdateScoreStatusDisplay()
    {
        if (scoreStatusText != null && GameManager.Instance != null)
        {
            float gameTime = GameManager.Instance.GetGameTime();
            var teddyBearData = DataBase.Instance.teddyBearData;
            
            if (gameTime >= teddyBearData.ScoreIncreaseTime)
            {
                scoreStatusText.text = $"증가한 점수 ({teddyBearData.InitialScore * teddyBearData.ScoreIncreaseRate:F1}/틱)";
                scoreStatusText.color = Color.yellow;
            }
            else
            {
                float timeRemaining = teddyBearData.ScoreIncreaseTime - gameTime;
                scoreStatusText.text = $"기본 점수 ({teddyBearData.InitialScore:F1}/틱) - 증가까지 {timeRemaining:F1}초";
                scoreStatusText.color = Color.white;
            }
        }
    }
    
    // 모든 UI 요소 업데이트
    void UpdateAllUI()
    {
        if (GameManager.Instance != null)
        {
            UpdateScoreDisplay(GameManager.Instance.GetTeddyBearScore());
            UpdateMultiplierDisplay(GameManager.Instance.GetScoreMultiplier());
            UpdateGameTimeDisplay();
            UpdateAttachStatusDisplay();
            UpdateScoreStatusDisplay();
        }
    }
    
    // HeatUI에서 호출할 수 있는 메서드들
    public float GetCurrentScore()
    {
        return GameManager.Instance != null ? GameManager.Instance.GetTeddyBearScore() : 0f;
    }
    
    public float GetCurrentMultiplier()
    {
        return GameManager.Instance != null ? GameManager.Instance.GetScoreMultiplier() : 1f;
    }
    
    public float GetGameTime()
    {
        return GameManager.Instance != null ? GameManager.Instance.GetGameTime() : 0f;
    }
    
    public bool IsAttached()
    {
        return GameManager.Instance != null ? GameManager.Instance.IsTeddyBearAttached() : false;
    }
    
    // 개발자용 테스트 버튼들
    [Header("개발자 테스트 버튼들")]
    [SerializeField] private Button resetScoreButton;
    
    void Awake()
    {
        // 리셋 버튼 이벤트 연결
        if (resetScoreButton != null)
        {
            resetScoreButton.onClick.AddListener(() => {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ResetAllScores();
                }
            });
        }
    }
} 