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
        Debug.Log("GameOverPanel: 패널 활성화됨");
        
        // 패널 활성화 후 데이터 요청
        StartCoroutine(RequestGameOverData());
    }
    
    /// <summary>
    /// GameOverController에게 데이터 요청
    /// </summary>
    private System.Collections.IEnumerator RequestGameOverData()
    {
        // 약간의 지연 후 데이터 요청 (패널 완전 활성화 대기)
        yield return new WaitForSeconds(0.2f);
        
        // gameOverController가 설정되지 않은 경우 자동으로 찾기
        if(gameOverController == null)
        {
            gameOverController = FindObjectOfType<GameOverController>();
            Debug.Log("GameOverPanel: GameOverController 자동 탐색");
        }
        
        if(gameOverController != null)
        {
            Debug.Log("GameOverPanel: GameOverController에게 데이터 재요청");
            // GameOverController의 현재 랭킹 데이터 가져오기
            var rankings = gameOverController.GetPlayerRankings();
            if (rankings != null && rankings.Count > 0)
            {
                Debug.Log($"GameOverPanel: 순위 데이터 {rankings.Count}개 수신");
                SetPlayerRankings(rankings);
            }
            else
            {
                Debug.LogWarning("GameOverPanel: 순위 데이터가 비어있습니다.");
            }
        }
        else
        {
            Debug.LogError("GameOverPanel: GameOverController를 찾을 수 없습니다!");
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
        Debug.Log($"📋 GameOverPanel: 순위 정보 업데이트 시작 - {rankings.Count}명");

        // 승자 정보 설정
        if(rankings.Count > 0)
        {
            var winner = rankings[0];
            if(winnerNameText != null)
            {
                winnerNameText.text = winner.nickname;
                Debug.Log($"🏆 승자 설정: {winner.nickname}");
            }
            else
            {
                Debug.LogError("GameOverPanel: winnerNameText가 null입니다!");
            }
        }

        // 순위별 정보 설정
        Debug.Log("📊 순위별 정보 설정 시작");
        SetRankInfo(0, rankings, _1stNameText, _1stScoreText);
        SetRankInfo(1, rankings, _2ndNameText, _2ndScoreText);
        SetRankInfo(2, rankings, _3rdNameText, _3rdScoreText);
        SetRankInfo(3, rankings, _4thNameText, _4thScoreText);
        
        Debug.Log("✅ GameOverPanel: 모든 순위 정보 설정 완료");
    }

    /// <summary>
    /// 특정 순위의 정보 설정
    /// </summary>
    private void SetRankInfo(int rankIndex, List<GameOverController.PlayerRankData> rankings, 
                            TextMeshProUGUI nameText, TextMeshProUGUI scoreText)
    {
        Debug.Log($"🔍 SetRankInfo 호출 - Rank {rankIndex + 1}, NameText: {(nameText != null ? "OK" : "NULL")}, ScoreText: {(scoreText != null ? "OK" : "NULL")}");
        
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
            {
                nameText.text = displayName;
                Debug.Log($"✅ {rankIndex + 1}등 이름 설정: {displayName}");
            }
            else
            {
                Debug.LogError($"❌ {rankIndex + 1}등 nameText가 null입니다!");
            }
            
            if(scoreText != null)
            {
                scoreText.text = $"{playerData.score:F0}";
                Debug.Log($"✅ {rankIndex + 1}등 점수 설정: {playerData.score:F0}점");
            }
            else
            {
                Debug.LogError($"❌ {rankIndex + 1}등 scoreText가 null입니다!");
            }

            Debug.Log($"🏅 {rankIndex + 1}등 완료: {playerData.nickname} - {playerData.score:F0}점 {(playerData.isLocalPlayer ? "[나]" : "")}");
        }
        else
        {
            // 플레이어가 없는 순위는 비워둠
            Debug.Log($"📭 {rankIndex + 1}등: 플레이어 없음 - 텍스트 비우기");
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
        Debug.Log("GameOverPanel: 메인 메뉴로 이동 - Player Properties 완전 초기화");
        
        // Player Properties 완전 초기화 (핵심!)
        ClearAllPlayerProperties();
        
        StartCoroutine(LeaveRoomAndLoadLobby());
    }
    
    /// <summary>
    /// 모든 Player Properties 완전 초기화
    /// </summary>
    private void ClearAllPlayerProperties()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null) return;
        
        Debug.Log($"GameOverPanel: 플레이어 {PhotonNetwork.LocalPlayer.ActorNumber} Properties 완전 초기화");
        
        var props = new ExitGames.Client.Photon.Hashtable();
        props[$"score_{PhotonNetwork.LocalPlayer.ActorNumber}"] = null;
        props[$"playerReady_{PhotonNetwork.LocalPlayer.ActorNumber}"] = null;
        props["nickname"] = null;
        
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        
        Debug.Log("GameOverPanel: Player Properties 초기화 완료");
    }
    
    /// <summary>
    /// 방 나가기 후 Lobby 씬 로드
    /// </summary>
    private System.Collections.IEnumerator LeaveRoomAndLoadLobby()
    {
        // Properties 초기화 완료 대기
        yield return new WaitForSeconds(0.5f);
        
        // 방 나가기
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            
            // 방 나가기 완료 대기 (최대 5초)
            float timeout = 5f;
            float timer = 0f;
            while (PhotonNetwork.InRoom && timer < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            
            Debug.Log($"GameOverPanel: 방 나가기 완료 (대기: {timer}초)");
        }
        
        // 연결 해제
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            
            // 연결 해제 완료 대기 (최대 5초)
            float timeout = 5f;
            float timer = 0f;
            while (PhotonNetwork.IsConnected && timer < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            
            Debug.Log($"GameOverPanel: 연결 해제 완료 (대기: {timer}초)");
        }
        
        // Lobby 씬 로드
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }

#endregion
}
