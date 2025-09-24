using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Photon.Pun;

public class GameOverController : MonoBehaviourPunCallbacks
{
    [SerializeField] private Transform cameraPosition;
    [SerializeField] private Transform winnerPosition;

    private float winnerScore;
    private GameObject winnerPlayer;
    private List<PlayerRankData> playerRankings = new List<PlayerRankData>();

    // 플레이어 순위 데이터 구조체
    [System.Serializable]
    public class PlayerRankData
    {
        public GameObject playerObject;
        public string nickname;
        public float score;
        public bool isLocalPlayer;
        public int actorNumber;

        public PlayerRankData(GameObject player, string nick, float playerScore, bool isLocal, int actorNum)
        {
            playerObject = player;
            nickname = nick;
            score = playerScore;
            isLocalPlayer = isLocal;
            actorNumber = actorNum;
        }
    }

    public void SetWinnerPlayer()
    {
        winnerScore = 0;
        winnerPlayer = null;
        playerRankings.Clear();

        // 독립적으로 모든 플레이어의 점수 데이터 수집
        CollectAllPlayerScores();

        // 점수 기준으로 정렬
        SortPlayersByScore();

        // 로컬 플레이어가 승자인 경우에만 이동
        CheckAndMoveWinner();

        // GameOverPanel에 순위 정보 전달
        UpdateGameOverPanel();
        
        // 모든 클라이언트에서 자신의 순위를 직접 계산하고 구글 시트 업데이트
        UpdateGameResultForLocalPlayer();
    }

    /// <summary>
    /// 독립적으로 모든 플레이어의 점수 데이터 수집
    /// </summary>
    private void CollectAllPlayerScores()
    {
        // Photon 네트워크의 모든 플레이어 가져오기
        var allPlayers = PhotonNetwork.PlayerList;
        
        foreach (var player in allPlayers)
        {
            // 플레이어의 GameObject 찾기
            GameObject playerObject = FindPlayerObjectByPhotonPlayer(player);
            if (playerObject == null) continue;
            
            // 점수 가져오기
            float playerScore = GetPlayerScoreFromObject(playerObject, player);
            
            // 닉네임 가져오기
            string nickname = GetPlayerNickname(player);
            
            // 로컬 플레이어인지 확인
            PhotonView pv = playerObject.GetComponent<PhotonView>();
            bool isLocal = pv != null && pv.IsMine;
            
            // 플레이어 데이터 생성
            PlayerRankData playerData = new PlayerRankData(
                playerObject,
                nickname,
                playerScore,
                isLocal,
                player.ActorNumber
            );
            
            playerRankings.Add(playerData);
            
            Debug.Log($"GameOverController: 플레이어 점수 수집 - {nickname}: {playerScore}점 (로컬: {isLocal})");
        }
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
            if (pv != null && pv.Owner != null && pv.Owner.ActorNumber == player.ActorNumber)
            {
                return playerObj;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 플레이어 오브젝트에서 직접 점수 가져오기
    /// </summary>
    private float GetPlayerScoreFromObject(GameObject playerObject, Photon.Realtime.Player player)
    {
        // 1. CoinController에서 직접 가져오기 (로컬 플레이어)
        PhotonView pv = playerObject.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            CoinController coinController = playerObject.GetComponent<CoinController>();
            if (coinController != null)
            {
                return coinController.GetCurrentScore();
            }
        }
        
        // 2. Photon Custom Properties에서 가져오기 (원격 플레이어)
        if (player.CustomProperties.TryGetValue($"score_{player.ActorNumber}", out object scoreObj))
        {
            if (float.TryParse(scoreObj.ToString(), out float networkScore))
            {
                return networkScore;
            }
        }
        
        // 3. 마지막 시도: CoinController에서 강제로 가져오기
        CoinController fallbackController = playerObject.GetComponent<CoinController>();
        if (fallbackController != null)
        {
            return fallbackController.GetCurrentScore();
        }
        
        return 0f;
    }
    
    /// <summary>
    /// 점수 기준으로 플레이어 정렬
    /// </summary>
    private void SortPlayersByScore()
    {
        playerRankings = playerRankings
            .OrderByDescending(p => p.score)
            .ThenBy(p => p.actorNumber) // 점수가 같으면 ActorNumber 순으로
            .ToList();
        
        // 승자 설정
        if (playerRankings.Count > 0)
        {
            var winner = playerRankings[0];
            winnerScore = winner.score;
            winnerPlayer = winner.playerObject;
            
            Debug.Log($"GameOverController: 승자 결정 - {winner.nickname} ({winner.score}점)");
        }
    }

    /// <summary>
    /// ActorNumber로 플레이어 오브젝트 찾기
    /// </summary>
    private GameObject FindPlayerObjectByActorNumber(int actorNumber)
    {
        foreach(var player in GameManager.Instance.GetAllPlayerLivingEntities())
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if(pv != null && pv.Owner.ActorNumber == actorNumber)
            {
                return player.gameObject;
            }
        }
        return null;
    }

    /// <summary>
    /// 승자 확인 및 처리
    /// </summary>
    private void CheckAndMoveWinner()
    {
        if(winnerPlayer != null)
        {
            PhotonView winnerPV = winnerPlayer.GetComponent<PhotonView>();
            if(winnerPV != null && winnerPV.IsMine)
            {
                // 로컬 플레이어가 승자인 경우 - 플레이어 이동 + 카메라 설정
                StartCoroutine(MoveWinnerToPosition());
            }
            else
            {
                // 로컬 플레이어가 승자가 아닌 경우 - 카메라만 이동
                StartCoroutine(SetupNonWinnerView());
            }
        }
        else
        {
            // 승자를 찾을 수 없는 경우에도 카메라 이동
            StartCoroutine(SetupNonWinnerView());
        }
    }

    private IEnumerator MoveWinnerToPosition()
    {
        // 로컬 플레이어만 컨트롤 비활성화
        DisableLocalPlayerControls();
        
        yield return new WaitForSeconds(0.5f);
        
        if(winnerPlayer != null && winnerPosition != null)
        {
            SimpleTeleport(winnerPlayer, winnerPosition.position, winnerPosition.rotation);
            SetCameraPosition();
        }
    }

    /// <summary>
    /// 로컬 플레이어만 컨트롤 비활성화
    /// </summary>
    private void DisableLocalPlayerControls()
    {
        GameObject[] allPlayerObjects = GameObject.FindGameObjectsWithTag("Player");
        
        foreach(GameObject playerObj in allPlayerObjects)
        {
            PhotonView pv = playerObj.GetComponent<PhotonView>();
            if(pv != null && pv.IsMine) // 로컬 플레이어만
            {
                MoveController moveController = playerObj.GetComponent<MoveController>();
                SkillController skillController = playerObj.GetComponent<SkillController>();
                if(moveController != null && skillController != null)
                {
                    moveController.DisableMoveControls();
                    skillController.DisableSkillControls();
                }
                
                CameraController cameraController = playerObj.GetComponent<CameraController>();
                if(cameraController != null)
                {
                    cameraController.DisableCameraControl();
                    cameraController.enabled = false;
                }
                break; // 로컬 플레이어 하나만 처리하고 종료
            }
        }
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// 승자가 아닌 플레이어들을 위한 게임 오버 처리
    /// </summary>
    private IEnumerator SetupNonWinnerView()
    {
        // 로컬 플레이어 컨트롤 비활성화
        DisableLocalPlayerControls();
        
        yield return new WaitForSeconds(0.5f);
        
        // 카메라를 cameraPosition으로 이동
        SetCameraPosition();
    }


    /// <summary>
    /// 간단한 플레이어 텔레포트
    /// </summary>
    private void SimpleTeleport(GameObject player, Vector3 targetPosition, Quaternion targetRotation)
    {

        player.GetComponent<CapsuleCollider>().enabled = false;
        player.transform.position = targetPosition;
        player.transform.rotation = targetRotation;
        player.GetComponent<CapsuleCollider>().enabled = true;
    }

    private void UpdateGameOverPanel()
    {
        GameOverPanel gameOverPanel = FindObjectOfType<GameOverPanel>();
        if(gameOverPanel != null)
        {
            gameOverPanel.SetPlayerRankings(playerRankings);
        }
    }

    private string GetPlayerNickname(Photon.Realtime.Player player)
    {
        if (player == null) return "Unknown";

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
                return localNickname;
            }
        }
        
        // 기본값으로 Player + ActorNumber 사용
        return $"Player{player.ActorNumber}";
    }

    public void SetCameraPosition()
    {
        if(cameraPosition != null)
        {
            Camera.main.transform.position = cameraPosition.position;
            Camera.main.transform.rotation = cameraPosition.rotation;
        }
    }

    public void ResetWinnerPlayer()
    {
        winnerScore = 0;
        winnerPlayer = null;
        playerRankings.Clear();
    }

    /// <summary>
    /// 로컬 플레이어의 순위를 직접 계산하고 구글 시트 업데이트
    /// </summary>
    private void UpdateGameResultForLocalPlayer()
    {
        if (playerRankings == null || playerRankings.Count == 0)
        {
            Debug.LogWarning("GameOverController: 순위 데이터가 없습니다.");
            return;
        }
        
        // 로컬 플레이어의 순위 찾기
        int localPlayerRank = -1;
        string localPlayerNickname = "";
        float localPlayerScore = 0f;
        
        // 현재 로그인된 사용자의 닉네임 가져오기
        string currentUserNickname = "";
        if (CurrentUser.Instance != null && CurrentUser.Instance.IsLoggedIn())
        {
            var userData = CurrentUser.Instance.GetUserGameData();
            if (userData != null)
            {
                currentUserNickname = userData.nickname;
            }
        }
        
        // PlayerPrefs에서 닉네임 가져오기 (백업)
        if (string.IsNullOrEmpty(currentUserNickname))
        {
            currentUserNickname = PlayerPrefs.GetString("NickName", "");
        }
        
        Debug.Log($"GameOverController: 로컬 플레이어 닉네임 확인 - '{currentUserNickname}'");
        
        // 순위 리스트에서 로컬 플레이어 찾기
        for (int i = 0; i < playerRankings.Count; i++)
        {
            var playerData = playerRankings[i];
            
            // 로컬 플레이어인지 확인 (isLocalPlayer 또는 닉네임 매칭)
            if (playerData.isLocalPlayer || 
                (!string.IsNullOrEmpty(currentUserNickname) && playerData.nickname.Equals(currentUserNickname, System.StringComparison.OrdinalIgnoreCase)))
            {
                localPlayerRank = i + 1; // 순위는 1부터 시작
                localPlayerNickname = playerData.nickname;
                localPlayerScore = playerData.score;
                break;
            }
        }
        
        if (localPlayerRank > 0)
        {
            Debug.Log($"GameOverController: 로컬 플레이어 순위 확인 - {localPlayerRank}등 (닉네임: {localPlayerNickname}, 점수: {localPlayerScore})");
            
            // 구글 시트에 게임 결과 업데이트
            if (GameResultManager.Instance != null)
            {
                GameResultManager.Instance.UpdateCurrentUserGameResult(localPlayerRank);
                Debug.Log($"GameOverController: 구글 시트 업데이트 요청 - {localPlayerRank}등");
            }
            else
            {
                Debug.LogError("GameOverController: GameResultManager 인스턴스를 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogWarning($"GameOverController: 로컬 플레이어를 순위에서 찾을 수 없습니다. 현재 닉네임: '{currentUserNickname}'");
            
            // 디버그용: 모든 플레이어 순위 출력
            for (int i = 0; i < playerRankings.Count; i++)
            {
                var playerData = playerRankings[i];
                Debug.Log($"  {i + 1}등: {playerData.nickname} (로컬: {playerData.isLocalPlayer}, 점수: {playerData.score})");
            }
        }
    }

    // 순위 정보 getter 메서드들
    public List<PlayerRankData> GetPlayerRankings() => playerRankings;
    public GameObject GetWinnerPlayer() => winnerPlayer;
    public float GetWinnerScore() => winnerScore;
}