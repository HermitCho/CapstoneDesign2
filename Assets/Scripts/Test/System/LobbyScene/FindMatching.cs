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

    private const string ROOM_STATE_KEY = "GameState";
    private const string ROOM_STATE_WAITING = "Waiting";
    private const string ROOM_STATE_STARTING = "Starting";
    private const string ROOM_STATE_IN_GAME = "InGame";

    void Start()
    {
        if (findMatchButton != null)
            findMatchButton.onClick.AddListener(StartMatching);

        if (modalWindow != null)
        {
            modalWindow.onCancel.AddListener(CancelMatching);
            modalWindow.titleText = "매칭 중";
            modalWindow.descriptionText = "다른 플레이어를 찾고 있습니다...";
            modalWindow.showCancelButton = true;
            modalWindow.showConfirmButton = false;
            modalWindow.closeOnCancel = false;
        }

        UpdateUI("매칭 시작!");
    }

    public void StartMatching()
    {
        if (isMatching) return;

        isMatching = true;
        isGameStarting = false;
        matchingTimer = 0f;

        if (findMatchButton != null)
            findMatchButton.interactable = false;

        if (modalWindow != null)
            modalWindow.OpenWindow();

        UpdateUI("매칭 중...");

        // 연결 상태에 따른 단계별 처리
        StartCoroutine(HandleConnectionSequence());
        
        matchingCoroutine = StartCoroutine(MatchingTimer());
    }
    
    /// <summary>
    /// 포톤 연결 순서를 안전하게 처리하는 코루틴
    /// </summary>
    private IEnumerator HandleConnectionSequence()
    {
        // 1단계: 마스터 서버 연결
        if (!PhotonNetwork.IsConnected)
        {
            UpdateUI("서버 연결 중...");
            PhotonNetwork.ConnectUsingSettings();
            
            // 연결 완료 대기 (최대 10초)
            float connectionTimeout = 10f;
            float connectionTimer = 0f;
            
            while (!PhotonNetwork.IsConnectedAndReady && connectionTimer < connectionTimeout)
            {
                yield return new WaitForSeconds(0.1f);
                connectionTimer += 0.1f;
            }
            
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                UpdateUI("서버 연결 실패");
                CancelMatching();
                yield break;
            }
        }
        
        // 2단계: 로비 진입
        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
        {
            UpdateUI("로비 진입 중...");
            
            // 클라이언트 상태가 ConnectedToMasterServer인지 확인
            if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.ConnectedToMasterServer)
            {
                PhotonNetwork.JoinLobby();
                
                // 로비 진입 완료 대기 (최대 5초)
                float lobbyTimeout = 5f;
                float lobbyTimer = 0f;
                
                while (!PhotonNetwork.InLobby && lobbyTimer < lobbyTimeout && isMatching)
                {
                    yield return new WaitForSeconds(0.1f);
                    lobbyTimer += 0.1f;
                }
                
                if (!PhotonNetwork.InLobby && isMatching)
                {
                    UpdateUI("로비 진입 실패");
                    CancelMatching();
                    yield break;
                }
            }
            else
            {
                // 상태가 맞지 않으면 잠시 대기 후 재시도
                yield return new WaitForSeconds(0.5f);
                if (isMatching)
                {
                    StartCoroutine(HandleConnectionSequence());
                }
                yield break;
            }
        }
        
        // 3단계: 방 찾기/생성
        if (PhotonNetwork.InLobby && isMatching)
        {
            UpdateUI("방 찾는 중...");
            yield return new WaitForSeconds(0.2f); // 로비 안정화 대기
            TryJoinOrCreateRoom();
        }
    }

    public void CancelMatching()
    {
        if (!isMatching) return;

        isMatching = false;
        isGameStarting = false;

        if (matchingCoroutine != null)
        {
            StopCoroutine(matchingCoroutine);
            matchingCoroutine = null;
        }

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }

        ResetUI();
    }

    private void TryJoinOrCreateRoom()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InLobby)
        {
            return;
        }
        
        ExitGames.Client.Photon.Hashtable expectedCustomRoomProperties = new ExitGames.Client.Photon.Hashtable();
        expectedCustomRoomProperties[ROOM_STATE_KEY] = ROOM_STATE_WAITING;
        
        PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, targetPlayerCount);
    }

    private void CreateNewRoom()
    {
        string roomName = $"Room_{System.DateTime.Now.Ticks}_{Random.Range(1000, 9999)}";
        
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = targetPlayerCount,
            IsVisible = true,
            IsOpen = true,
            PublishUserId = true
        };

        ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
        roomProperties[ROOM_STATE_KEY] = ROOM_STATE_WAITING;
        roomProperties["masterReady"] = false; // 마스터 준비 상태
        roomOptions.CustomRoomProperties = roomProperties;
        roomOptions.CustomRoomPropertiesForLobby = new string[] { ROOM_STATE_KEY, "masterReady" };

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    private IEnumerator MatchingTimer()
    {
        while (isMatching && matchingTimer < maxWaitTime && !isGameStarting)
        {
            matchingTimer += Time.deltaTime;

            if (PhotonNetwork.InRoom)
            {
                int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
                float elapsedTime = matchingTimer;

                UpdateModalWindow(currentPlayers, elapsedTime);

                if (currentPlayers >= targetPlayerCount && !isGameStarting)
                {
                    StartGame();
                    yield break;
                }
            }
            else if (isMatching && !isGameStarting)
            {
                float elapsedTime = matchingTimer;
                UpdateModalWindow(0, elapsedTime);
            }

            yield return null;
        }

        if (isMatching && !isGameStarting)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        if (!isMatching || isGameStarting) return;

        isGameStarting = true;

        if (PhotonNetwork.IsMasterClient)
        {
            int playerCount = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 1;
            UpdateUI($"게임 시작! ({playerCount}명)");

            ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
            roomProperties[ROOM_STATE_KEY] = ROOM_STATE_STARTING;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

            photonView.RPC("OnGameStarting", RpcTarget.All, playerCount);
        }
    }

    [PunRPC]
    private void OnGameStarting(int playerCount)
    {
        isMatching = false;
        isGameStarting = true;

        if (matchingCoroutine != null)
        {
            StopCoroutine(matchingCoroutine);
            matchingCoroutine = null;
        }

        UpdateUI($"게임 시작! ({playerCount}명)");

        if (modalWindow != null)
            modalWindow.CloseWindow();

        StartCoroutine(LoadGameScene());
    }

    private IEnumerator LoadGameScene()
    {
        yield return new WaitForSeconds(0.1f);

        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
            roomProperties[ROOM_STATE_KEY] = ROOM_STATE_IN_GAME;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        }

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
    }

    private void UpdateModalWindow(int currentPlayers, float elapsedTime)
    {
        if (modalWindow != null && isMatching && !isGameStarting)
        {
            string matchingText = GetAnimatedMatchingText(elapsedTime);
            string description = $"</size>{matchingText} \n<size=80>플레이어: {currentPlayers}/{targetPlayerCount}\n\n</size>";

            modalWindow.descriptionText = description;
            modalWindow.UpdateUI();
        }
    }

    private string GetAnimatedMatchingText(float elapsedTime)
    {
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

    #region Photon 콜백

    public override void OnConnectedToMaster()
    {
        Debug.Log("FindMatching: 마스터 서버 연결 완료");
        // HandleConnectionSequence에서 처리하므로 여기서는 로그만 출력
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("FindMatching: 로비 진입 완료");
        // HandleConnectionSequence에서 이미 방 찾기를 시작하므로 중복 호출 방지
        // if (isMatching)
        // {
        //     StartCoroutine(DelayedJoinRoom());
        // }
    }

    private IEnumerator DelayedJoinRoom()
    {
        yield return new WaitForSeconds(0.1f);
        TryJoinOrCreateRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        CreateNewRoom();
    }

    public override void OnJoinedRoom()
    {
        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

        string roomState = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ROOM_STATE_KEY) 
            ? (string)PhotonNetwork.CurrentRoom.CustomProperties[ROOM_STATE_KEY] 
            : ROOM_STATE_WAITING;

        if (roomState == ROOM_STATE_STARTING || roomState == ROOM_STATE_IN_GAME)
        {
            PhotonNetwork.LeaveRoom();
            StartCoroutine(RetryJoinRoom());
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            // 마스터 클라이언트가 방 상태를 설정
            if (roomState != ROOM_STATE_WAITING)
            {
                ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
                roomProperties[ROOM_STATE_KEY] = ROOM_STATE_WAITING;
                roomProperties["masterReady"] = false;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
            }
            
            // 마스터 클라이언트 준비 완료를 잠시 후 설정
            StartCoroutine(SetMasterReady());
        }

        if (isMatching && !isGameStarting)
        {
            float elapsedTime = matchingTimer;
            UpdateModalWindow(currentPlayers, elapsedTime);
        }
    }
    
    /// <summary>
    /// 마스터 클라이언트 준비 완료 설정
    /// </summary>
    private IEnumerator SetMasterReady()
    {
        // 약간의 지연 후 마스터 준비 완료 설정
        yield return new WaitForSeconds(1f);
        
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
            roomProperties["masterReady"] = true;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
            
            Debug.Log("FindMatching: 마스터 클라이언트 준비 완료");
        }
    }

    private IEnumerator RetryJoinRoom()
    {
        yield return new WaitForSeconds(1f);
        if (isMatching)
        {
            TryJoinOrCreateRoom();
        }
    }

    public override void OnCreatedRoom()
    {
        if (isMatching && !isGameStarting)
        {
            float elapsedTime = matchingTimer;
            int currentPlayers = PhotonNetwork.CurrentRoom?.PlayerCount ?? 1;
            UpdateModalWindow(currentPlayers, elapsedTime);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

        if (isMatching && !isGameStarting)
        {
            matchingTimer = 0f;
            
            if (matchingCoroutine != null)
            {
                StopCoroutine(matchingCoroutine);
            }
            matchingCoroutine = StartCoroutine(MatchingTimer());
        }

        if (isMatching && !isGameStarting)
        {
            float elapsedTime = matchingTimer;
            UpdateModalWindow(currentPlayers, elapsedTime);
        }

        if (currentPlayers >= targetPlayerCount && isMatching && !isGameStarting)
        {
            StartGame();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (PhotonNetwork.InRoom && isMatching && !isGameStarting)
        {
            int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            float elapsedTime = matchingTimer;
            UpdateModalWindow(currentPlayers, elapsedTime);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        isMatching = false;
        isGameStarting = false;
        if (matchingCoroutine != null)
        {
            StopCoroutine(matchingCoroutine);
            matchingCoroutine = null;
        }
        
        ResetUI();
        UpdateUI("네트워크 연결이 끊어졌습니다.");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        UpdateUI("방 생성에 실패했습니다. 다시 시도해주세요.");
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
        UpdateUI("인증 실패! App ID를 확인하세요.");
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(ROOM_STATE_KEY))
        {
            string newRoomState = (string)propertiesThatChanged[ROOM_STATE_KEY];
            
            if (newRoomState == ROOM_STATE_STARTING && isMatching)
            {
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
        if (isMatching)
        {
            CancelMatching();
        }
        
        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.InRoom)
                PhotonNetwork.LeaveRoom();
            PhotonNetwork.Disconnect();
        }
        
        QuitGame();
    }
    
    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}
