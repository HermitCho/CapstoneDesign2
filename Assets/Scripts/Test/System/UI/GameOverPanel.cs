using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Febucci.UI;
using Michsky.UI.Heat;
using Photon.Pun;
using DG.Tweening;

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

    [Space(10)]
    [Tooltip("Exit 이미지")]
    [SerializeField] private Image _exitImage;

    [Header("게임 오버 컨트롤러")]
    [SerializeField] private GameOverController gameOverController;
    
    // EXIT 스티커 애니메이션 관련
    private Tween exitStickerTween;


#region Unity 생명주기

    void OnEnable()
    {
        // 패널 활성화 후 데이터 요청
        StartCoroutine(RequestGameOverData());
        
        // EXIT 이미지 초기화 (비활성화)
        if (_exitImage != null)
        {
            _exitImage.gameObject.SetActive(false);
        }
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
        }
        
        if(gameOverController != null)
        {
            // GameOverController의 현재 랭킹 데이터 가져오기
            var rankings = gameOverController.GetPlayerRankings();
            if (rankings != null && rankings.Count > 0)
            {
                SetPlayerRankings(rankings);
            }
        }
    }
    
    void OnDisable()
    {
        if(gameOverController != null)
        {
            gameOverController.ResetWinnerPlayer();
        }
        
        // EXIT 스티커 애니메이션 정리
        CleanupExitStickerAnimation();
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
        // 승자 정보 설정
        if(rankings.Count > 0)
        {
            var winner = rankings[0];
            if(winnerNameText != null)
            {
                winnerNameText.text = winner.nickname;
            }
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
            {
                nameText.text = displayName;
            }
            
            if(scoreText != null)
            {
                scoreText.text = $"{playerData.score:F0}";
            }
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
        // ✅ 왕관을 착용하고 있다면 먼저 떨어뜨리기
        DetachCrownIfWearing();
        
        // Player Properties 완전 초기화
        ClearAllPlayerProperties();
        
        StartCoroutine(LeaveRoomAndLoadLobby());
    }
    
    /// <summary>
    /// 로컬 플레이어가 왕관을 착용하고 있다면 떨어뜨리기
    /// </summary>
    private void DetachCrownIfWearing()
    {
        // Crown 오브젝트 찾기
        Crown crown = FindObjectOfType<Crown>();
        if (crown == null) return;
        
        // 왕관이 부착되어 있지 않으면 무시
        if (!crown.IsAttached()) return;
        
        // 로컬 플레이어 찾기
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject playerObj in allPlayers)
        {
            PhotonView pv = playerObj.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                // 왕관이 이 플레이어의 자식인지 확인
                if (crown.transform.parent != null && crown.transform.IsChildOf(playerObj.transform))
                {
                    Debug.Log("👑 GameOverPanel: 로비로 돌아가기 전 왕관 떨어뜨림");
                    crown.DetachFromPlayer();
                    return;
                }
            }
        }
    }
    
    /// <summary>
    /// 모든 Player Properties 완전 초기화
    /// </summary>
    private void ClearAllPlayerProperties()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null) return;
        
        var props = new ExitGames.Client.Photon.Hashtable();
        props[$"score_{PhotonNetwork.LocalPlayer.ActorNumber}"] = null;
        props[$"playerReady_{PhotonNetwork.LocalPlayer.ActorNumber}"] = null;
        props["nickname"] = null;
        
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
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
        }
        
        // Lobby 씬 로드
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }

#endregion

#region EXIT 스티커 애니메이션

    /// <summary>
    /// EXIT 스티커 표시 (승리 플레이어 퇴장 시)
    /// </summary>
    public void ShowExitSticker()
    {
        if (_exitImage == null) return;
        
        // 기존 애니메이션 정리
        CleanupExitStickerAnimation();
        
        // 초기 상태 설정
        _exitImage.gameObject.SetActive(true);
        _exitImage.transform.localScale = Vector3.zero;
        _exitImage.transform.rotation = Quaternion.Euler(0f, 0f, -15f); // 약간 기울어진 상태
        
        Color initialColor = _exitImage.color;
        initialColor.a = 0f;
        _exitImage.color = initialColor;
        
        // 스티커 부착 애니메이션 (탄성 효과)
        Sequence stickerSequence = DOTween.Sequence();
        
        // 1. 페이드 인 + 스케일 증가 (탄성 효과)
        stickerSequence.Append(_exitImage.DOFade(1f, 0.3f).SetEase(Ease.OutQuad));
        stickerSequence.Join(_exitImage.transform.DOScale(1.2f, 0.4f).SetEase(Ease.OutBack));
        
        // 2. 약간 튕기는 효과
        stickerSequence.Append(_exitImage.transform.DOScale(0.95f, 0.1f).SetEase(Ease.InQuad));
        stickerSequence.Append(_exitImage.transform.DOScale(1.05f, 0.1f).SetEase(Ease.OutQuad));
        stickerSequence.Append(_exitImage.transform.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad));
        
        // 3. 회전 보정 (0도로)
        stickerSequence.Join(_exitImage.transform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.OutQuad));
        
        exitStickerTween = stickerSequence;
    }
    
    /// <summary>
    /// EXIT 스티커 애니메이션 정리
    /// </summary>
    private void CleanupExitStickerAnimation()
    {
        exitStickerTween?.Kill();
        exitStickerTween = null;
    }

#endregion
}
