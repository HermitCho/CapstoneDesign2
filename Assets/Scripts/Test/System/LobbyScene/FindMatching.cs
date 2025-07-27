using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using Michsky.UI.Heat;

public class FindMatching : MonoBehaviourPunCallbacks
{
    [Header("매칭 설정")]
    [SerializeField] private int targetPlayerCount = 4;
    [SerializeField] private float maxWaitTime = 20f;
    [SerializeField] private string gameSceneName = "Prototype";

    [Header("UI")]
    [SerializeField] private Button findMatchButton;
    [SerializeField] private ModalWindowManager modalWindow;
    [SerializeField] private TextMeshProUGUI statusText;

    private bool isMatching = false;
    private float matchingTimer = 0f;
    private Coroutine matchingCoroutine;

    void Start()
    {
        // 버튼 이벤트 연결
        if (findMatchButton != null)
            findMatchButton.onClick.AddListener(StartMatching);

        // 모달 윈도우 이벤트 연결 및 설정
        if (modalWindow != null)
        {
            modalWindow.onCancel.AddListener(CancelMatching);

            // 모달창 기본 설정
            modalWindow.titleText = "매칭 중";
            modalWindow.descriptionText = "다른 플레이어를 찾고 있습니다...";
            modalWindow.showCancelButton = true;
            modalWindow.showConfirmButton = false;
            modalWindow.closeOnCancel = false; // 직접 제어
        }

        UpdateUI("매칭 취소!");
    }

    public void StartMatching()
    {
        if (isMatching) return;

        Debug.Log("[매칭] 매칭 시작!");
        isMatching = true;
        matchingTimer = 0f;

        // UI 업데이트
        if (findMatchButton != null)
            findMatchButton.interactable = false;

        if (modalWindow != null)
            modalWindow.OpenWindow();

        UpdateUI("매칭 중...");

        // 포톤 서버 연결 및 매칭 시작
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            JoinOrCreateRoom();
        }

        // 매칭 타이머 시작
        matchingCoroutine = StartCoroutine(MatchingTimer());
    }

    public void CancelMatching()
    {
        if (!isMatching) return;

        Debug.Log("매칭 취소!");
        isMatching = false;

        // 타이머 정지
        if (matchingCoroutine != null)
        {
            StopCoroutine(matchingCoroutine);
            matchingCoroutine = null;
        }

        // 방에서 나가기
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();

        ResetUI();
    }

    private void JoinOrCreateRoom()
    {
        UpdateUI("방을 찾는 중...");
        PhotonNetwork.JoinRandomRoom();
    }

    private IEnumerator MatchingTimer()
    {
        while (isMatching && matchingTimer < maxWaitTime)
        {
            matchingTimer += Time.deltaTime;

            // UI 업데이트
            if (PhotonNetwork.InRoom)
            {
                int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
                float remainingTime = matchingTimer;

                UpdateUI($"플레이어 대기 중... ({currentPlayers}/{targetPlayerCount})");
                UpdateModalWindow(currentPlayers, remainingTime);

                // 4명 모이면 즉시 게임 시작
                if (currentPlayers >= targetPlayerCount)
                {
                    StartGame();
                    yield break;
                }
            }
            else if (isMatching)
            {
                // 방에 입장하지 못한 경우에도 시간 업데이트
                float remainingTime = matchingTimer;
                UpdateModalWindow(0, remainingTime);
            }

            yield return null;
        }

        // 20초 후 현재 인원으로 게임 시작
        if (isMatching)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        if (!isMatching) return;

        isMatching = false;

        if (matchingCoroutine != null)
        {
            StopCoroutine(matchingCoroutine);
            matchingCoroutine = null;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            int playerCount = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 1;
            UpdateUI($"게임 시작! ({playerCount}명)");

            // 모달 윈도우 닫기
            if (modalWindow != null)
                modalWindow.CloseWindow();

            // 2초 후 게임 씬 로드
            StartCoroutine(LoadGameScene());
        }
    }

    private IEnumerator LoadGameScene()
    {
        yield return new WaitForSeconds(2f);

        // 다음에 열 게임 씬 이름을 static 변수에 저장
        LoadingController.LoadWithLoadingScene(gameSceneName, true);
    }

    private void ResetUI()
    {
        if (findMatchButton != null)
            findMatchButton.interactable = true;

        if (modalWindow != null)
            modalWindow.CloseWindow();

        UpdateUI("매칭 취소");
    }

    private void UpdateUI(string message)
    {
        if (statusText != null)
            statusText.text = message;
        Debug.Log($"[매칭] {message}");
    }

    private void UpdateModalWindow(int currentPlayers, float remainingTime)
    {
        if (modalWindow != null && isMatching)
        {
            // 모달창 제목 업데이트
            modalWindow.titleText = "매칭 중";

            // 모달창 설명에 진행 상황 표시
            string description = $"플레이어: {currentPlayers}/{targetPlayerCount}\n";
            description += $"남은 시간: {Mathf.Max(0, remainingTime):F0}초\n\n";

            if (currentPlayers == 0)
            {
                description += "방을 찾는 중...";
            }
            else if (currentPlayers < targetPlayerCount)
            {
                description += "다른 플레이어를 기다리는 중...";
            }
            else
            {
                description += "모든 플레이어가 모였습니다!";
            }

            modalWindow.descriptionText = description;
            modalWindow.UpdateUI(); // 모달창 UI 새로고침
        }
    }

    #region 포톤 콜백 (Override)

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[매칭] 🎉 OnJoinedLobby - 로비 입장 성공!");
        Debug.Log($"[매칭] InLobby: {PhotonNetwork.InLobby}");

        if (isMatching)
        {
            Debug.Log("[매칭] 매칭 중이므로 방 찾기 시작");
            JoinOrCreateRoom();
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log($"[매칭] OnJoinRandomFailed - 코드: {returnCode}, 메시지: {message}");
        Debug.Log("[매칭] 참가할 방이 없어 새 방 생성");

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = targetPlayerCount,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(null, roomOptions);
    }

    public override void OnJoinedRoom()
    {
        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        Debug.Log($"[매칭] 🎉 OnJoinedRoom - 방 입장 성공! 현재 플레이어: {currentPlayers}");
        Debug.Log($"[매칭] 방 이름: {PhotonNetwork.CurrentRoom.Name}, 최대: {PhotonNetwork.CurrentRoom.MaxPlayers}");

        UpdateUI($"방 입장! ({currentPlayers}/{targetPlayerCount})");

        // 모달창 즉시 업데이트
        if (isMatching)
        {
            float remainingTime = maxWaitTime - matchingTimer;
            UpdateModalWindow(currentPlayers, remainingTime);
        }
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("[매칭] 🎉 OnCreatedRoom - 방 생성 성공!");
        Debug.Log($"[매칭] 현재 플레이어 수: {PhotonNetwork.CurrentRoom?.PlayerCount ?? 0}");

        UpdateUI("방 생성 완료! 다른 플레이어를 기다리는 중...");

        // 모달창 즉시 업데이트 (방장 1명)
        if (isMatching)
        {
            float remainingTime = maxWaitTime - matchingTimer;
            int currentPlayers = PhotonNetwork.CurrentRoom?.PlayerCount ?? 1;
            UpdateModalWindow(currentPlayers, remainingTime);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[매칭] 플레이어 입장: {newPlayer.NickName}");
        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        Debug.Log($"[매칭] 현재 플레이어 수: {currentPlayers}");

        // 모달창 즉시 업데이트
        if (isMatching)
        {
            float remainingTime = maxWaitTime - matchingTimer;
            UpdateModalWindow(currentPlayers, remainingTime);
        }

        // 4명 모이면 즉시 게임 시작
        if (currentPlayers >= targetPlayerCount && isMatching)
        {
            StartGame();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[매칭] 플레이어 퇴장: {otherPlayer.NickName}");
        if (PhotonNetwork.InRoom && isMatching)
        {
            int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            float remainingTime = maxWaitTime - matchingTimer;
            UpdateModalWindow(currentPlayers, remainingTime);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[매칭] ❌ OnDisconnected - 연결 끊김: {cause}");
        isMatching = false;
        if (matchingCoroutine != null)
        {
            StopCoroutine(matchingCoroutine);
            matchingCoroutine = null;
        }
        ResetUI();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log($"[매칭] ❌ OnCreateRoomFailed - 코드: {returnCode}, 메시지: {message}");
        UpdateUI("방 생성에 실패했습니다. 다시 시도해주세요.");
    }

    public override void OnCustomAuthenticationFailed(string debugMessage)
    {
        Debug.Log($"[매칭] ❌ OnCustomAuthenticationFailed - {debugMessage}");
        UpdateUI("인증 실패! App ID를 확인하세요.");
    }

    public void OnExitButtonClick()
    {
        Debug.Log("[매칭] 게임 종료 버튼 클릭됨");
        
        // 매칭 중이면 매칭 취소
        if (isMatching)
        {
            CancelMatching();
        }
        
        // Photon 연결 해제
        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.InRoom)
                PhotonNetwork.LeaveRoom();
            PhotonNetwork.Disconnect();
        }
        
        // 게임 종료 처리
        QuitGame();
    }
    
    /// <summary>
    /// 게임 종료 처리
    /// </summary>
    private void QuitGame()
    {
        Debug.Log("[매칭] 게임 종료 처리 시작");
        
        // Unity 에디터에서는 플레이 모드 종료, 빌드에서는 애플리케이션 종료
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.Log("[매칭] Unity 에디터에서 플레이 모드 종료");
#else
        Application.Quit();
        Debug.Log("[매칭] 애플리케이션 종료");
#endif
    }


    #endregion
}
