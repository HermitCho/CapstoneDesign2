using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 🎮 게임 오버 패널
/// 게임 종료 시 최종 점수를 표시하고 관련 UI를 관리
/// </summary>
public class GameOverPanel : MonoBehaviour
{
    #region 인스펙터 할당 변수
    
    [Header("점수 표시 UI")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    
    [Header("버튼들")]
    [SerializeField] private Button mainMenuButton;
    
    [Header("애니메이션 (선택사항)")]
    [SerializeField] private Animator panelAnimator;
    
    #endregion
    
    #region 변수
    
    private float currentFinalScore = 0f;
    private float bestScore = 0f;
    private const string BEST_SCORE_KEY = "BestScore"; // PlayerPrefs 키
    
    #endregion
    
    #region Unity 생명주기
    
    void Awake()
    {
        // 최고 점수 로드
        LoadBestScore();
        
        // 버튼 이벤트 연결
        SetupButtons();
    }
    
    void OnEnable()
    {
        // GameManager의 게임 오버 이벤트 구독
        GameManager.OnGameOver += OnGameOverReceived;
        
        Debug.Log("✅ GameOverPanel: 게임 오버 이벤트 구독");
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
        currentFinalScore = finalScore;
        
        // 최고 점수 업데이트 확인
        UpdateBestScore(finalScore);
        
        // UI 업데이트
        UpdateScoreUI();
        
        // 애니메이션 실행 (있다면)
        PlayShowAnimation();
        
        Debug.Log($"🎮 GameOverPanel: 게임 오버 처리 완료 - 최종 점수: {finalScore}");
    }
    
    /// <summary>
    /// 점수 UI 업데이트
    /// </summary>
    void UpdateScoreUI()
    {
        if (finalScoreText != null)
        {
            finalScoreText.text = $"최종 점수: {currentFinalScore:F0}";
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
    
    #region 버튼 이벤트 메서드

    /// <summary>
    /// 메인 메뉴 버튼 클릭
    /// </summary>
    void OnMainMenuButtonClicked()
    {
        Debug.Log("🏠 GameOverPanel: 메인 메뉴 버튼 클릭");
        
        // 메인 메뉴로 이동 (씬 이름은 프로젝트에 맞게 수정)
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }
    
    #endregion
    
    #region 애니메이션 메서드
    
    /// <summary>
    /// 패널 표시 애니메이션 재생
    /// </summary>
    void PlayShowAnimation()
    {
        if (panelAnimator != null)
        {
            panelAnimator.SetTrigger("Show");
        }
    }
    
    /// <summary>
    /// 패널 숨김 애니메이션 재생
    /// </summary>
    void PlayHideAnimation()
    {
        if (panelAnimator != null)
        {
            panelAnimator.SetTrigger("Hide");
        }
    }
    
    #endregion
    
    #region 개발자/디버그 메서드
    
    /// <summary>
    /// 테스트용 게임 오버 시뮬레이션
    /// </summary>
    [ContextMenu("Test Game Over")]
    void TestGameOver()
    {
        OnGameOverReceived(1234.5f);
    }
    
    /// <summary>
    /// 최고 점수 초기화 (개발자용)
    /// </summary>
    [ContextMenu("Reset Best Score")]
    void ResetBestScore()
    {
        PlayerPrefs.DeleteKey(BEST_SCORE_KEY);
        bestScore = 0f;
        UpdateScoreUI();
        Debug.Log("🗑️ GameOverPanel: 최고 점수 초기화");
    }
    
    #endregion
}
