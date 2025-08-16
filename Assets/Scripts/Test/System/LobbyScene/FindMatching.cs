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
    private bool isGameStarting = false;

    // 방 상태 추적을 위한 변수들
    private const string ROOM_STATE_KEY = "GameState";
    private const string ROOM_STATE_WAITING = "Waiting";
    private const string ROOM_STATE_STARTING = "Starting";
    private const string ROOM_STATE_IN_GAME = "InGame";

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

        UpdateUI("매칭 시작!");
    }

    public void StartMatching()
    {
        if (isMatching) return;

        Debug.Log("[매칭] 매칭 시작!");
        isMatching = true;
        isGameStarting = false;
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
            Debug.Log("[매칭] Photon 서버에 연결 중...");
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (!PhotonNetwork.InLobby)
        {
            Debug.Log("[매칭] 로비에 입장 중...");
            PhotonNetwork.JoinLobby();
        }
        else
        {
            TryJoinOrCreateRoom();
        }

        // 매칭 타이머 시작
        matchingCoroutine = StartCoroutine(MatchingTimer());
    }

    public void CancelMatching()
    {
        if (!isMatching) return;

        Debug.Log("[매칭] 매칭 취소!");
        isMatching = false;
        isGameStarting = false;

        // 타이머 정지
        if (matchingCoroutine != null)
        {
            StopCoroutine(matchingCoroutine);
            matchingCoroutine = null;
        }

        // 방에서 나가기
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            Debug.Log("[매칭] 방에서 나갔습니다.");
        }

        // 로비에서 나가기 (선택적)
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
            Debug.Log("[매칭] 로비에서 나갔습니다.");
        }

        ResetUI();
    }

    private void TryJoinOrCreateRoom()
    {
        // Photon 연결 상태 확인
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InLobby)
        {
            Debug.LogWarning("[매칭] Photon이 아직 준비되지 않았습니다. 연결을 기다립니다.");
            return;
        }

        Debug.Log("[매칭] 대기 중인 방을 찾는 중...");
        
        // 게임이 시작되지 않은 방만 필터링하여 입장 시도
        ExitGames.Client.Photon.Hashtable expectedCustomRoomProperties = new ExitGames.Client.Photon.Hashtable();
        expectedCustomRoomProperties[ROOM_STATE_KEY] = ROOM_STATE_WAITING;
        
        // 방 입장 시도 (최대 3번까지 재시도)
        PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, targetPlayerCount);
    }

    private void CreateNewRoom()
    {
        Debug.Log("[매칭] 새로운 방을 생성하는 중...");
        
        // 고유한 방 이름 생성 (타임스탬프 + 랜덤 숫자)
        string roomName = $"Room_{System.DateTime.Now.Ticks}_{Random.Range(1000, 9999)}";
        
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = targetPlayerCount,
            IsVisible = true,
            IsOpen = true,
            PublishUserId = true // 사용자 ID 공개로 더 나은 매칭
        };

        // 방 생성 시 초기 상태를 "Waiting"으로 설정
        ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
        roomProperties[ROOM_STATE_KEY] = ROOM_STATE_WAITING;
        roomOptions.CustomRoomProperties = roomProperties;
        roomOptions.CustomRoomPropertiesForLobby = new string[] { ROOM_STATE_KEY };

        Debug.Log($"[매칭] 새로운 방 생성: {roomName}");
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    private IEnumerator MatchingTimer()
    {
        while (isMatching && matchingTimer < maxWaitTime && !isGameStarting)
        {
            matchingTimer += Time.deltaTime;

            // UI 업데이트
            if (PhotonNetwork.InRoom)
            {
                int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
                float elapsedTime = matchingTimer;

                UpdateModalWindow(currentPlayers, elapsedTime);

                // 4명 모이면 즉시 게임 시작
                if (currentPlayers >= targetPlayerCount && !isGameStarting)
                {
                    StartGame();
                    yield break;
                }
            }
            else if (isMatching && !isGameStarting)
            {
                // 방에 입장하지 못한 경우에도 시간 업데이트
                float elapsedTime = matchingTimer;
                UpdateModalWindow(0, elapsedTime);
            }

            yield return null;
        }

        // maxWaitTime 후 현재 인원으로 게임 시작
        if (isMatching && !isGameStarting)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        if (!isMatching || isGameStarting) return;

        isGameStarting = true;
        Debug.Log("[매칭] 게임 시작 프로세스 시작!");

        if (PhotonNetwork.IsMasterClient)
        {
            int playerCount = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 1;
            UpdateUI($"게임 시작! ({playerCount}명)");

            // 방 상태를 "Starting"으로 변경하여 새로운 플레이어의 입장을 방지
            ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
            roomProperties[ROOM_STATE_KEY] = ROOM_STATE_STARTING;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

            // 모든 클라이언트에게 게임 시작을 알림
            photonView.RPC("OnGameStarting", RpcTarget.All, playerCount);
        }
    }

    [PunRPC]
    private void OnGameStarting(int playerCount)
    {
        Debug.Log($"[매칭] RPC: 게임 시작 알림 받음! 플레이어 수: {playerCount}");
        
        isMatching = false;
        isGameStarting = true;

        if (matchingCoroutine != null)
        {
            StopCoroutine(matchingCoroutine);
            matchingCoroutine = null;
        }

        UpdateUI($"게임 시작! ({playerCount}명)");

        // 모달 윈도우 닫기
        if (modalWindow != null)
            modalWindow.CloseWindow();

        // 2초 후 게임 씬 로드
        StartCoroutine(LoadGameScene());
    }

    private IEnumerator LoadGameScene()
    {
        yield return new WaitForSeconds(2f);

        // 방 상태를 "InGame"으로 변경
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
            roomProperties[ROOM_STATE_KEY] = ROOM_STATE_IN_GAME;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        }

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

    private void UpdateModalWindow(int currentPlayers, float elapsedTime)
    {
        if (modalWindow != null && isMatching && !isGameStarting)
        {
            // 매칭 중 텍스트 애니메이션 (1초마다 변화)
            string matchingText = GetAnimatedMatchingText(elapsedTime);
            
            // 모달창 설명에 진행 상황 표시
            string description = $"<size=80>{elapsedTime:F0}초\n\n</size>{matchingText} \n<size=80>플레이어: {currentPlayers}/{targetPlayerCount}\n\n</size>";

            modalWindow.descriptionText = description;
            modalWindow.UpdateUI(); // 모달창 UI 새로고침
        }
    }

    /// <summary>
    /// 매칭 중 텍스트 애니메이션 생성
    /// </summary>
    private string GetAnimatedMatchingText(float elapsedTime)
    {
        // 1초마다 텍스트 변화
        int animationIndex = Mathf.FloorToInt(elapsedTime) % 3;
        
        switch (animationIndex)
        {
            case 0:
                return "매칭 중.";
            case 1:
                return "매칭 중..";
            case 2:
                return "매칭 중...";
            default:
                return "매칭 중.";
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
            // 약간의 지연을 두어 상태가 안정화되도록 함
            StartCoroutine(DelayedJoinRoom());
        }
    }

    /// <summary>
    /// 지연된 방 입장 (상태 안정화를 위해)
    /// </summary>
    private IEnumerator DelayedJoinRoom()
    {
        yield return new WaitForSeconds(0.1f); // 0.1초 지연
        TryJoinOrCreateRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log($"[매칭] OnJoinRandomFailed - 코드: {returnCode}, 메시지: {message}");
        
        // 방 찾기 실패 원인 분석
        switch (returnCode)
        {
            case 32760: // NoMatchFound
                Debug.Log("[매칭] 대기 중인 방이 없어 새 방 생성");
                CreateNewRoom();
                break;
            case 32758: // NoRandomRoomFound
                Debug.Log("[매칭] 랜덤 방을 찾을 수 없어 새 방 생성");
                CreateNewRoom();
                break;
            default:
                Debug.LogWarning($"[매칭] 알 수 없는 오류 코드: {returnCode}, 새 방 생성 시도");
                CreateNewRoom();
                break;
        }
    }

    public override void OnJoinedRoom()
    {
        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        Debug.Log($"[매칭] 🎉 OnJoinedRoom - 방 입장 성공! 현재 플레이어: {currentPlayers}");
        Debug.Log($"[매칭] 방 이름: {PhotonNetwork.CurrentRoom.Name}, 최대: {PhotonNetwork.CurrentRoom.MaxPlayers}");

        // 방 상태 확인 (안전장치)
        string roomState = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ROOM_STATE_KEY) 
            ? (string)PhotonNetwork.CurrentRoom.CustomProperties[ROOM_STATE_KEY] 
            : ROOM_STATE_WAITING;

        Debug.Log($"[매칭] 방 상태: {roomState}");

        // 게임이 이미 시작 중이거나 진행 중인 방에 입장한 경우 (안전장치)
        if (roomState == ROOM_STATE_STARTING || roomState == ROOM_STATE_IN_GAME)
        {
            Debug.Log("[매칭] 게임이 이미 시작된 방입니다. 방을 나갑니다.");
            PhotonNetwork.LeaveRoom();
            
            // 잠시 후 다시 방 찾기 시도
            StartCoroutine(RetryJoinRoom());
            return;
        }

        // 방 상태가 Waiting이 아니면 Waiting으로 설정 (방장인 경우)
        if (PhotonNetwork.IsMasterClient && roomState != ROOM_STATE_WAITING)
        {
            Debug.Log("[매칭] 방 상태를 Waiting으로 초기화");
            ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
            roomProperties[ROOM_STATE_KEY] = ROOM_STATE_WAITING;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        }

        Debug.Log($"[매칭] 방 입장 완료! ({currentPlayers}/{targetPlayerCount})");

        // 모달창 즉시 업데이트
        if (isMatching && !isGameStarting)
        {
            float elapsedTime = matchingTimer;
            UpdateModalWindow(currentPlayers, elapsedTime);
        }
    }

    private IEnumerator RetryJoinRoom()
    {
        yield return new WaitForSeconds(1f);
        if (isMatching)
        {
            Debug.Log("[매칭] 다시 방 찾기 시도");
            TryJoinOrCreateRoom();
        }
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("[매칭] 🎉 OnCreatedRoom - 방 생성 성공!");
        Debug.Log($"[매칭] 방 이름: {PhotonNetwork.CurrentRoom?.Name}");
        Debug.Log($"[매칭] 현재 플레이어 수: {PhotonNetwork.CurrentRoom?.PlayerCount ?? 0}");
        Debug.Log($"[매칭] 방 최대 인원: {PhotonNetwork.CurrentRoom?.MaxPlayers ?? 0}");

        // 방 상태가 제대로 설정되었는지 확인
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ROOM_STATE_KEY))
        {
            string roomState = (string)PhotonNetwork.CurrentRoom.CustomProperties[ROOM_STATE_KEY];
            Debug.Log($"[매칭] 방 상태 설정 확인: {roomState}");
        }
        else
        {
            Debug.LogWarning("[매칭] 방 상태가 설정되지 않았습니다!");
        }

        Debug.Log("[매칭] 다른 플레이어를 기다리는 중...");

        // 모달창 즉시 업데이트 (방장 1명)
        if (isMatching && !isGameStarting)
        {
            float elapsedTime = matchingTimer;
            int currentPlayers = PhotonNetwork.CurrentRoom?.PlayerCount ?? 1;
            UpdateModalWindow(currentPlayers, elapsedTime);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[매칭] 플레이어 입장: {newPlayer.NickName}");
        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        Debug.Log($"[매칭] 현재 플레이어 수: {currentPlayers}");

        // 새로운 플레이어가 입장하면 타이머 재시작
        if (isMatching && !isGameStarting)
        {
            Debug.Log("[매칭] 새로운 플레이어 입장! 타이머 재시작");
            matchingTimer = 0f; // 타이머 초기화
            
            // 기존 타이머 코루틴 정지 후 재시작
            if (matchingCoroutine != null)
            {
                StopCoroutine(matchingCoroutine);
            }
            matchingCoroutine = StartCoroutine(MatchingTimer());
        }

        // 모달창 즉시 업데이트
        if (isMatching && !isGameStarting)
        {
            float elapsedTime = matchingTimer;
            UpdateModalWindow(currentPlayers, elapsedTime);
        }

        // 4명 모이면 즉시 게임 시작
        if (currentPlayers >= targetPlayerCount && isMatching && !isGameStarting)
        {
            StartGame();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[매칭] 플레이어 퇴장: {otherPlayer.NickName}");
        if (PhotonNetwork.InRoom && isMatching && !isGameStarting)
        {
            int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            float elapsedTime = matchingTimer;
            UpdateModalWindow(currentPlayers, elapsedTime);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[매칭] ❌ OnDisconnected - 연결 끊김: {cause}");
        isMatching = false;
        isGameStarting = false;
        if (matchingCoroutine != null)
        {
            StopCoroutine(matchingCoroutine);
            matchingCoroutine = null;
        }
        
        // 연결이 끊어졌을 때 UI 리셋
        ResetUI();
        
        // 연결 끊김 상태를 사용자에게 알림
        UpdateUI("네트워크 연결이 끊어졌습니다.");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log($"[매칭] ❌ OnCreateRoomFailed - 코드: {returnCode}, 메시지: {message}");
        UpdateUI("방 생성에 실패했습니다. 다시 시도해주세요.");
        
        // 방 생성 실패 시 잠시 후 다시 시도
        StartCoroutine(RetryCreateRoom());
    }

    private IEnumerator RetryCreateRoom()
    {
        yield return new WaitForSeconds(1f);
        if (isMatching)
        {
            CreateNewRoom();
        }
    }

    public override void OnCustomAuthenticationFailed(string debugMessage)
    {
        Debug.Log($"[매칭] ❌ OnCustomAuthenticationFailed - {debugMessage}");
        UpdateUI("인증 실패! App ID를 확인하세요.");
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        Debug.Log("[매칭] 방 속성 업데이트 감지");
        
        // 방 상태 변경 확인
        if (propertiesThatChanged.ContainsKey(ROOM_STATE_KEY))
        {
            string newRoomState = (string)propertiesThatChanged[ROOM_STATE_KEY];
            Debug.Log($"[매칭] 방 상태 변경: {newRoomState}");
            
            // 게임 시작 중이면 매칭 중단
            if (newRoomState == ROOM_STATE_STARTING && isMatching)
            {
                Debug.Log("[매칭] 방 상태가 Starting으로 변경되어 매칭 중단");
                isMatching = false;
                isGameStarting = true;
                
                if (matchingCoroutine != null)
                {
                    StopCoroutine(matchingCoroutine);
                    matchingCoroutine = null;
                }
            }
        }
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
