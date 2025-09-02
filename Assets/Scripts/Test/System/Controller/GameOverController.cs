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

        Debug.Log("🎮 GameOverController: 플레이어 순위 계산 시작");

        // 모든 플레이어의 정보 수집
        foreach(var player in GameManager.Instance.GetAllPlayerLivingEntities())
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if(pv != null)
            {
                float playerScore = player.GetComponent<CoinController>().GetCurrentScore();
                string nickname = GetPlayerNickname(pv.Owner);
                bool isLocal = pv.IsMine;
                int actorNumber = pv.Owner.ActorNumber;

                playerRankings.Add(new PlayerRankData(
                    player.gameObject,
                    nickname,
                    playerScore,
                    isLocal,
                    actorNumber
                ));

                Debug.Log($"📊 플레이어 정보: {nickname} (Actor: {actorNumber}) - 점수: {playerScore} {(isLocal ? "[로컬]" : "[원격]")}");

                // 최고 점수 플레이어 갱신
                if(playerScore > winnerScore)
                {
                    winnerScore = playerScore;
                    winnerPlayer = player.gameObject;
                }
            }
        }

        // 점수 기준으로 내림차순 정렬 (점수가 같으면 ActorNumber 순)
        playerRankings = playerRankings.OrderByDescending(p => p.score).ThenBy(p => p.actorNumber).ToList();

        Debug.Log($"🏆 승자 결정: {GetPlayerNickname(winnerPlayer?.GetComponent<PhotonView>()?.Owner)} - 점수: {winnerScore}");

        // 승자 플레이어를 winnerPosition으로 이동 (로컬 플레이어인 경우에만)
        if(winnerPlayer != null && winnerPosition != null)
        {
            PhotonView winnerPV = winnerPlayer.GetComponent<PhotonView>();
            if(winnerPV != null && winnerPV.IsMine)
            {
                Debug.Log("🎯 로컬 플레이어가 승자입니다. winnerPosition으로 이동합니다.");
                StartCoroutine(MoveWinnerToPosition());
            }
            else
            {
                Debug.Log("🎯 원격 플레이어가 승자입니다. 위치 이동은 하지 않습니다.");
            }
        }

        // GameOverPanel에 순위 정보 전달
        UpdateGameOverPanel();
    }

    private IEnumerator MoveWinnerToPosition()
    {
        // 모든 플레이어의 컨트롤 비활성화
        DisableAllPlayersControls();
        
        // 약간의 지연 후 이동
        yield return new WaitForSeconds(0.5f);
        
        if(winnerPlayer != null && winnerPosition != null)
        {
            // 간단한 텔레포트
            SimpleTeleport(winnerPlayer, winnerPosition.position, winnerPosition.rotation);
            
            // 카메라 위치 설정
            SetCameraPosition();
            
            Debug.Log($"🏆 승자 플레이어가 winnerPosition으로 이동 완료: {winnerPlayer.name}");
        }
    }

    /// <summary>
    /// 모든 플레이어의 컨트롤을 비활성화
    /// </summary>
    private void DisableAllPlayersControls()
    {
        Debug.Log("🚫 모든 플레이어 컨트롤 비활성화 시작");
        
        // 모든 플레이어 찾기
        GameObject[] allPlayerObjects = GameObject.FindGameObjectsWithTag("Player");
        
        foreach(GameObject playerObj in allPlayerObjects)
        {
            PhotonView pv = playerObj.GetComponent<PhotonView>();
            if(pv != null)
            {
                // MoveController 비활성화
                MoveController moveController = playerObj.GetComponent<MoveController>();
                if(moveController != null)
                {
                    moveController.DisableAllControls();
                    Debug.Log($"🚫 플레이어 {pv.Owner.ActorNumber} MoveController 비활성화");
                }
                
                // CameraController 비활성화 (로컬 플레이어만)
                if(pv.IsMine)
                {
                    CameraController cameraController = playerObj.GetComponent<CameraController>();
                    if(cameraController != null)
                    {
                        cameraController.DisableCameraControl();
                        // CameraController 컴포넌트 비활성화
                        cameraController.enabled = false;
                        Debug.Log($"🚫 로컬 플레이어 CameraController 비활성화");
                    }
                }
            }
        }
        
        // 전역 사격 시스템 비활성화
        TestShoot.SetIsShooting(false);
        
        // 마우스 커서 표시
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        Debug.Log("✅ 모든 플레이어 컨트롤 비활성화 완료");
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