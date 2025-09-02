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

    [Header("winner 텍스트")]
    [Tooltip("winner 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI winnerNameText;
    [Space(10)]

    [Header("리더보드 텍스트")]
    [Tooltip("1등 점수 텍스트")]
    [SerializeField] private TextMeshProUGUI _1stScoreText;
    [Tooltip("1등 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI _1stNameText;

    [Space(10)]
    [Tooltip("2등 점수 텍스트")]
    [SerializeField] private TextMeshProUGUI _2ndScoreText;
    [Tooltip("2등 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI _2ndNameText;

    [Space(10)]
    [Tooltip("3등 점수 텍스트")]
    [SerializeField] private TextMeshProUGUI _3rdScoreText;
    [Tooltip("3등 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI _3rdNameText;

    [Space(10)]
    [Tooltip("4등 점수 텍스트")]
    [SerializeField] private TextMeshProUGUI _4thScoreText;
    [Tooltip("4등 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI _4thNameText;




    [Header("게임 오버 컨트롤러")]
    [SerializeField] private GameOverController gameOverController;


#region Unity 생명주기

    void OnEnable()
    {
        if(gameOverController != null)
        {
            gameOverController.SetWinnerPlayer();
        }
    }
    
    void OnDisable()
    {
        if(gameOverController != null)
        {
            gameOverController.ResetWinnerPlayer();
        }
    }

#endregion


#region UI 업데이트

    public void UpdateUI()
    {
        
    }

    /// <summary>
    /// 플레이어 순위 정보 설정
    /// </summary>
    public void SetPlayerRankings(List<GameOverController.PlayerRankData> rankings)
    {
        Debug.Log($"📋 GameOverPanel: 순위 정보 업데이트 - {rankings.Count}명");

        // 승자 정보 설정
        if(rankings.Count > 0)
        {
            var winner = rankings[0];
            if(winnerNameText != null)
                winnerNameText.text = winner.nickname;
        }

        // 순위별 정보 설정
        SetRankInfo(0, rankings, _1stNameText, _1stScoreText);
        SetRankInfo(1, rankings, _2ndNameText, _2ndScoreText);
        SetRankInfo(2, rankings, _3rdNameText, _3rdScoreText);
        SetRankInfo(3, rankings, _4thNameText, _4thScoreText);
    }

    /// <summary>
    /// 특정 순위의 정보 설정
    /// </summary>
    private void SetRankInfo(int rankIndex, List<GameOverController.PlayerRankData> rankings, 
                            TextMeshProUGUI nameText, TextMeshProUGUI scoreText)
    {
        if(rankIndex < rankings.Count)
        {
            var playerData = rankings[rankIndex];
            string displayName = playerData.nickname;
            
            // 로컬 플레이어인 경우 하이라이트
            if(playerData.isLocalPlayer)
            {
                displayName = $"<color=yellow>{displayName}</color>";
            }

            if(nameText != null)
                nameText.text = displayName;
            if(scoreText != null)
                scoreText.text = $"{playerData.score:F0}";

            Debug.Log($"🏅 {rankIndex + 1}등: {playerData.nickname} - {playerData.score:F0}점 {(playerData.isLocalPlayer ? "[나]" : "")}");
        }
        else
        {
            // 플레이어가 없는 순위는 비워둠
            if(nameText != null)
                nameText.text = "";
            if(scoreText != null)
                scoreText.text = "";
        }
    }

#endregion



#region 버튼 클릭 이벤트

    public void OnMainMenuButtonClicked()
    {  
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.Disconnect();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }

#endregion
}
