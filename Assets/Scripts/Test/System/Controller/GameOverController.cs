using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Photon.Pun;

public class GameOverController : MonoBehaviour
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

        // HUDPanel에서 이미 계산된 점수판 데이터 가져오기
        HUDPanel hudPanel = FindObjectOfType<HUDPanel>();
        if(hudPanel != null)
        {
            // HUDPanel의 점수판 데이터를 활용
            var hudPlayerData = hudPanel.GetPlayerScoreDataList();
            
            foreach(var playerData in hudPlayerData)
            {
                GameObject playerObject = FindPlayerObjectByActorNumber(playerData.playerId);
                if(playerObject != null)
                {
                    playerRankings.Add(new PlayerRankData(
                        playerObject,
                        playerData.nickname,
                        playerData.score,
                        playerData.isLocalPlayer,
                        playerData.playerId
                    ));

                    // 최고 점수 플레이어 갱신
                    if(playerData.score > winnerScore)
                    {
                        winnerScore = playerData.score;
                        winnerPlayer = playerObject;
                    }
                }
            }

            // 이미 HUDPanel에서 정렬되어 있으므로 그대로 사용
            // playerRankings는 HUD 데이터 순서를 유지
        }

        // 로컬 플레이어가 승자인 경우에만 이동
        CheckAndMoveWinner();

        // GameOverPanel에 순위 정보 전달
        UpdateGameOverPanel();
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
                if(moveController != null)
                {
                    moveController.DisableAllControls();
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

    // 순위 정보 getter 메서드들
    public List<PlayerRankData> GetPlayerRankings() => playerRankings;
    public GameObject GetWinnerPlayer() => winnerPlayer;
    public float GetWinnerScore() => winnerScore;
}