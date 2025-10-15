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
    private bool isWinnerLocal = false; // 승리 플레이어가 로컬인지 추적
    private int winnerActorNumber = -1; // 승리 플레이어의 ActorNumber 추적

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
        isWinnerLocal = false;

        // 독립적으로 모든 플레이어의 점수 데이터 수집
        CollectAllPlayerScores();

        // 점수 기준으로 정렬
        SortPlayersByScore();

        // 로컬 플레이어가 승자인지 확인
        if (playerRankings.Count > 0)
        {
            isWinnerLocal = playerRankings[0].isLocalPlayer;
        }

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
            winnerActorNumber = winner.actorNumber; // ActorNumber 저장
        }
    }
    
    /// <summary>
    /// 플레이어 퇴장 감지 (Photon Callback)
    /// </summary>
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        // 승리 플레이어가 퇴장했는지 확인
        if (otherPlayer.ActorNumber == winnerActorNumber)
        {
            // GameOverPanel에 EXIT 스티커 표시 요청
            GameOverPanel gameOverPanel = FindObjectOfType<GameOverPanel>(true);
            if (gameOverPanel != null)
            {
                gameOverPanel.ShowExitSticker();
            }
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
        // 로컬 플레이어만 컨트롤 비활성화 (점프는 제외)
        DisableLocalPlayerControls(true); // true = 승리 플레이어 (점프 가능)
        
        yield return new WaitForSeconds(0.5f);
        
        if(winnerPlayer != null && winnerPosition != null)
        {
            SimpleTeleport(winnerPlayer, winnerPosition.position, winnerPosition.rotation);
            SetCameraPosition();
            
            // 승리 플레이어의 점프 다시 활성화
            EnableWinnerJump();
        }
    }
    
    /// <summary>
    /// 승리 플레이어의 점프 활성화
    /// </summary>
    private void EnableWinnerJump()
    {
        GameObject[] allPlayerObjects = GameObject.FindGameObjectsWithTag("Player");
        
        foreach(GameObject playerObj in allPlayerObjects)
        {
            PhotonView pv = playerObj.GetComponent<PhotonView>();
            if(pv != null && pv.IsMine) // 로컬 플레이어만
            {
                MoveController moveController = playerObj.GetComponent<MoveController>();
                if(moveController != null)
                {
                    moveController.EnableJump();
                }
                break;
            }
        }
    }

    /// <summary>
    /// 로컬 플레이어만 컨트롤 비활성화
    /// </summary>
    /// <param name="isWinner">승리 플레이어인 경우 점프만 허용</param>
    private void DisableLocalPlayerControls(bool isWinner = false)
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
                    if (isWinner)
                    {
                        // 승리 플레이어: 이동/마우스만 차단, 점프는 허용
                        moveController.DisableMovement();
                        moveController.DisableMouseControl();
                    }
                    else
                    {
                        // 일반 플레이어: 모든 조작 차단
                        moveController.DisableMoveControls();
                    }
                    
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
        // 로컬 플레이어 컨트롤 비활성화 (점프 포함 모두 차단)
        DisableLocalPlayerControls(false); // false = 일반 플레이어 (모든 조작 차단)
        
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
        // 비활성화된 오브젝트까지 포함해서 찾기
        GameOverPanel gameOverPanel = FindObjectOfType<GameOverPanel>(true);
        if(gameOverPanel != null)
        {
            gameOverPanel.SetPlayerRankings(playerRankings);
        }
    }

    private string GetPlayerNickname(Photon.Realtime.Player player)
    {
        if (player == null) return "Unknown";

        // 1. PhotonPlayer의 커스텀 프로퍼티에서 닉네임 가져오기 (최우선)
        if (player.CustomProperties != null && player.CustomProperties.TryGetValue("nickname", out object nicknameObj))
        {
            string nickname = nicknameObj?.ToString();
            if (!string.IsNullOrEmpty(nickname))
            {
                return nickname;
            }
        }
        
        // 2. Photon NickName 속성 확인
        if (!string.IsNullOrEmpty(player.NickName))
        {
            return player.NickName;
        }
        
        // 3. 로컬 플레이어인 경우 PlayerPrefs/CurrentUser에서 가져오기
        if (player.IsLocal)
        {
            string localNickname = "";
            
            // CurrentUser 확인
            if (CurrentUser.Instance != null && CurrentUser.Instance.IsLoggedIn())
            {
                localNickname = CurrentUser.Instance.GetNickname();
            }
            
            // PlayerPrefs 확인
            if (string.IsNullOrEmpty(localNickname))
            {
                localNickname = PlayerPrefs.GetString("NickName", "");
            }
            
            if (!string.IsNullOrEmpty(localNickname))
            {
                return localNickname;
            }
        }
        
        // 4. 기본값으로 Player + ActorNumber 사용
        return $"Player{player.ActorNumber}";
    }

    public void SetCameraPosition()
    {
        // Ready 단계에서는 카메라 위치 변경을 방지
        if (PhotonNetwork.CurrentRoom != null && 
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gamePhase", out object phase) &&
            phase.ToString() == "READY")
        {
            Debug.Log("GameOverController: Ready 단계에서 카메라 위치 변경 차단");
            return;
        }
        
        if(cameraPosition != null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.position = cameraPosition.position;
                mainCamera.transform.rotation = cameraPosition.rotation;
                Debug.Log($"GameOverController: 카메라 위치 설정 - Position: {cameraPosition.position}");
            }
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
            // 구글 시트에 게임 결과 업데이트
            if (GameResultManager.Instance != null)
            {
                GameResultManager.Instance.UpdateCurrentUserGameResult(localPlayerRank);
            }
        }
    }

    // 순위 정보 getter 메서드들
    public List<PlayerRankData> GetPlayerRankings() => playerRankings;
    public GameObject GetWinnerPlayer() => winnerPlayer;
    public float GetWinnerScore() => winnerScore;
}