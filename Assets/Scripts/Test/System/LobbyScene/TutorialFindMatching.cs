using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Michsky.UI.Heat;

/// <summary>
/// 튜토리얼 전용 매칭 시스템
/// 1인 전용 방을 생성하여 다른 플레이어의 입장을 차단
/// HeatUI ButtonManager 사용
/// </summary>
public class TutorialFindMatching : MonoBehaviourPunCallbacks
{
    [Header("튜토리얼 설정")]
    [SerializeField] private string tutorialSceneName = "Tutorial";
    [SerializeField] private float connectionTimeout = 10f;
    
    [Header("UI (HeatUI)")]
    [SerializeField] private ButtonManager startTutorialButton;
    [SerializeField] private ModalWindowManager modalWindow;
    [SerializeField] private TextMeshProUGUI statusText;

    // 상태 관리
    private bool isConnecting = false;
    private bool isTutorialStarting = false;
    private bool isMasterServerConnected = false;
    private Coroutine connectionCoroutine;
    
    // 방 상태 키
    private const string ROOM_STATE_KEY = "GameState";
    private const string ROOM_STATE_TUTORIAL = "Tutorial";
    private const string ROOM_TYPE_KEY = "RoomType";
    private const string ROOM_TYPE_TUTORIAL = "TutorialRoom";
    
    #region Unity 생명주기
    
    void Start()
    {
        // Photon 네트워크 설정
        PhotonNetwork.SendRate = 40;
        PhotonNetwork.SerializationRate = 30;
        
        // HeatUI ButtonManager 이벤트 등록
        if (startTutorialButton != null)
        {
            startTutorialButton.onClick.AddListener(OnClickStartTutorialButton);
        }
        
        // 모달 윈도우 설정
        if (modalWindow != null)
        {
            modalWindow.onCancel.AddListener(OnClickCancelButton);
            modalWindow.titleText = "튜토리얼 준비 중";
            modalWindow.descriptionText = "튜토리얼을 시작하고 있습니다...";
            modalWindow.showCancelButton = true;
            modalWindow.showConfirmButton = false;
            modalWindow.closeOnCancel = false;
        }
        
        UpdateUI("튜토리얼 시작");
    }
    
    void OnDestroy()
    {
        // 코루틴 정리
        if (connectionCoroutine != null)
        {
            StopCoroutine(connectionCoroutine);
            connectionCoroutine = null;
        }
        
        // HeatUI ButtonManager 이벤트 해제
        if (startTutorialButton != null)
        {
            startTutorialButton.onClick.RemoveListener(OnClickStartTutorialButton);
        }
        
        if (modalWindow != null)
        {
            modalWindow.onCancel.RemoveListener(OnClickCancelButton);
        }
    }
    
    #endregion
    
    #region 버튼 클릭 이벤트
    
    /// <summary>
    /// 튜토리얼 시작 버튼 클릭
    /// </summary>
    public void OnClickStartTutorialButton()
    {
        if (isConnecting || isTutorialStarting) return;
        
        // 이미 방에 있거나 다른 매칭이 진행 중인지 확인 (안전 장치)
        if (PhotonNetwork.InRoom)
        {
            Debug.LogWarning("TutorialFindMatching: 이미 방에 입장해 있습니다. 튜토리얼을 시작할 수 없습니다.");
            UpdateUI("이미 방에 입장해 있습니다.");
            return;
        }
        
        Debug.Log("TutorialFindMatching: 튜토리얼 시작 요청");
        
        isConnecting = true;
        isTutorialStarting = false;
        
        // HeatUI ButtonManager 비활성화
        if (startTutorialButton != null)
        {
            startTutorialButton.Interactable(false);
        }
        
        // 모달 윈도우 표시
        if (modalWindow != null)
        {
            modalWindow.OpenWindow();
        }
        
        UpdateUI("연결 중...");
        
        // 연결 시퀀스 시작
        connectionCoroutine = StartCoroutine(HandleTutorialConnectionSequence());
    }
    
    /// <summary>
    /// 취소 버튼 클릭
    /// </summary>
   public void OnClickCancelButton()
   {
        if (!isConnecting) return;
        
        Debug.Log("TutorialFindMatching: 튜토리얼 시작 취소");
        
        CancelTutorial();
    }
    
    #endregion
    
    #region 튜토리얼 연결 로직
    
    /// <summary>
    /// 튜토리얼 연결 시퀀스 처리
    /// </summary>
    private IEnumerator HandleTutorialConnectionSequence()
    {
        Debug.Log("TutorialFindMatching: 연결 시퀀스 시작");
        
        // 1단계: 마스터 서버 연결
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("TutorialFindMatching: 마스터 서버 연결 시도");
            PhotonNetwork.ConnectUsingSettings();
            
            // 연결 완료 대기 (OnConnectedToMaster 콜백)
            float timer = 0f;
            
            while (!isMasterServerConnected && timer < connectionTimeout && isConnecting)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            
            if (!isMasterServerConnected || !isConnecting)
            {
                Debug.LogError("TutorialFindMatching: 마스터 서버 연결 실패 또는 취소");
                HandleConnectionFailure("서버 연결 실패");
                yield break;
            }
            
            Debug.Log("TutorialFindMatching: 마스터 서버 연결 성공");
        }
        else
        {
            // 이미 연결된 상태
            if (PhotonNetwork.IsConnectedAndReady)
            {
                isMasterServerConnected = true;
                Debug.Log("TutorialFindMatching: 이미 마스터 서버에 연결됨");
            }
        }
        
        // 2단계: 로비 진입 (선택적 - 튜토리얼은 로비 없이도 가능)
        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
        {
            // 튜토리얼은 로비를 거치지 않고 바로 방 생성 가능
            // 하지만 일관성을 위해 로비 진입 시도
            if (PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer)
            {
                PhotonNetwork.JoinLobby();
                
                // 로비 진입 대기
                float timer = 0f;
                float lobbyTimeout = 5f;
                
                while (!PhotonNetwork.InLobby && timer < lobbyTimeout && isConnecting)
                {
                    yield return new WaitForSeconds(0.1f);
                    timer += 0.1f;
                }
                
                // 로비 진입 실패해도 튜토리얼은 진행 가능
                if (!PhotonNetwork.InLobby && isConnecting)
                {
                    Debug.LogWarning("TutorialFindMatching: 로비 진입 실패, 직접 방 생성 시도");
                }
            }
        }
        
        // 3단계: 튜토리얼 전용 방 생성
        if (PhotonNetwork.IsConnectedAndReady && isConnecting && !isTutorialStarting)
        {
            yield return new WaitForSeconds(0.3f); // 안정화 대기
            
            Debug.Log("TutorialFindMatching: 튜토리얼 방 생성 시도");
            CreateTutorialRoom();
        }
    }
    
    /// <summary>
    /// 튜토리얼 전용 방 생성
    /// </summary>
    private void CreateTutorialRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogError("TutorialFindMatching: 네트워크 연결 상태가 준비되지 않음");
            HandleConnectionFailure("네트워크 연결 오류");
            return;
        }
        
        // 고유한 방 이름 생성 (플레이어 ID + 시간)
        string roomName = $"Tutorial_{PhotonNetwork.LocalPlayer.UserId}_{System.DateTime.Now.Ticks}";
        
        // 방 옵션 설정 (1인 전용)
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 1, // ✅ 1인 전용
            IsVisible = false, // ✅ 다른 플레이어에게 보이지 않음
            IsOpen = false, // ✅ 다른 플레이어 입장 차단
            PublishUserId = true
        };
        
        // 방 커스텀 프로퍼티 설정
        ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
        roomProperties[ROOM_STATE_KEY] = ROOM_STATE_TUTORIAL;
        roomProperties[ROOM_TYPE_KEY] = ROOM_TYPE_TUTORIAL;
        roomProperties["gamePhase"] = "TUTORIAL"; // 튜토리얼 단계
        roomProperties["isTutorial"] = true; // 튜토리얼 플래그
        
        roomOptions.CustomRoomProperties = roomProperties;
        roomOptions.CustomRoomPropertiesForLobby = new string[] 
        { 
            ROOM_STATE_KEY, 
            ROOM_TYPE_KEY, 
            "gamePhase",
            "isTutorial"
        };
        
        Debug.Log($"TutorialFindMatching: 방 생성 - {roomName} (1인 전용, 비공개)");
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    
    /// <summary>
    /// 튜토리얼 씬 로드
    /// </summary>
    private IEnumerator LoadTutorialScene()
    {
        Debug.Log("TutorialFindMatching: 튜토리얼 씬 로드 시작");
        
        // 방 상태 업데이트 (마스터 클라이언트)
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
            roomProperties[ROOM_STATE_KEY] = ROOM_STATE_TUTORIAL;
            roomProperties["gamePhase"] = "TUTORIAL";
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        }
        
        yield return new WaitForSeconds(0.2f);
        
        // LoadingController를 통한 씬 로드 (Photon 동기화)
        Debug.Log($"TutorialFindMatching: LoadingController로 씬 로드 - {tutorialSceneName}");
        LoadingController.LoadWithLoadingScene(tutorialSceneName, true);
    }
    
    #endregion
    
    #region 취소 및 오류 처리
    
    /// <summary>
    /// 튜토리얼 시작 취소
    /// </summary>
    private void CancelTutorial()
    {
        Debug.Log("TutorialFindMatching: 튜토리얼 취소 시작");
        
        isConnecting = false;
        isTutorialStarting = false;
        isMasterServerConnected = false;
        
        // 코루틴 중지
        if (connectionCoroutine != null)
        {
            StopCoroutine(connectionCoroutine);
            connectionCoroutine = null;
        }
        
        // 모든 코루틴 중지
        StopAllCoroutines();
        
        // 안전한 방 나가기 및 정리
        StartCoroutine(SafeCleanupAndLeaveRoom());
    }
    
    /// <summary>
    /// 안전한 방 정리 및 연결 완전 해제 (코루틴)
    /// </summary>
    private IEnumerator SafeCleanupAndLeaveRoom()
    {
        // 로컬 플레이어 Properties 정리
        if (PhotonNetwork.LocalPlayer != null)
        {
            var props = new ExitGames.Client.Photon.Hashtable();
            props["playerReady"] = null;
            props["nickname"] = null;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            
            Debug.Log("TutorialFindMatching: 로컬 플레이어 Properties 초기화");
        }
        
        // 방에 있다면 나가기
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("TutorialFindMatching: 튜토리얼 방 나가기 시작");
            PhotonNetwork.LeaveRoom();
            
            // 방 나가기 완료 대기 (최대 3초)
            float timeout = 3f;
            float timer = 0f;
            
            while (PhotonNetwork.InRoom && timer < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            
            if (PhotonNetwork.InRoom)
            {
                Debug.LogWarning("TutorialFindMatching: 방 나가기 타임아웃");
            }
            else
            {
                Debug.Log("TutorialFindMatching: 방 나가기 완료 - 튜토리얼 방 자동 삭제됨");
            }
        }
        
        // 로비에서도 나가기 (완전 연결 해제를 위해)
        if (PhotonNetwork.InLobby)
        {
            Debug.Log("TutorialFindMatching: 로비 나가기 시작");
            PhotonNetwork.LeaveLobby();
            
            // 로비 나가기 완료 대기 (최대 2초)
            float timeout = 2f;
            float timer = 0f;
            
            while (PhotonNetwork.InLobby && timer < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            
            if (PhotonNetwork.InLobby)
            {
                Debug.LogWarning("TutorialFindMatching: 로비 나가기 타임아웃");
            }
            else
            {
                Debug.Log("TutorialFindMatching: 로비 나가기 완료");
            }
        }
        
        // Photon 연결 완전 해제 (핵심!)
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("TutorialFindMatching: Photon 연결 해제 시작");
            PhotonNetwork.Disconnect();
            
            // 연결 해제 완료 대기 (최대 3초)
            float timeout = 3f;
            float timer = 0f;
            
            while (PhotonNetwork.IsConnected && timer < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            
            if (PhotonNetwork.IsConnected)
            {
                Debug.LogWarning("TutorialFindMatching: 연결 해제 타임아웃");
            }
            else
            {
                Debug.Log("TutorialFindMatching: Photon 연결 완전 해제 완료");
            }
        }
        
        // UI 리셋
        ResetUI();
        UpdateUI("튜토리얼 시작 취소");
        
        Debug.Log("TutorialFindMatching: 튜토리얼 취소 완료 (완전 연결 해제)");
    }
    
    /// <summary>
    /// 연결 실패 처리 (연결 완전 해제 포함)
    /// </summary>
    private void HandleConnectionFailure(string errorMessage)
    {
        Debug.LogError($"TutorialFindMatching: 연결 실패 - {errorMessage}");
        
        isConnecting = false;
        isTutorialStarting = false;
        isMasterServerConnected = false;
        
        // 모달 윈도우 표시 (오류 메시지)
        if (modalWindow != null)
        {
            modalWindow.titleText = "연결 실패";
            modalWindow.descriptionText = errorMessage;
            modalWindow.showCancelButton = true;
            modalWindow.showConfirmButton = false;
        }
        
        UpdateUI($"오류: {errorMessage}");
        
        // 연결 완전 해제 (실패 시에도 필수!)
        StartCoroutine(DisconnectAndCleanup());
    }
    
    /// <summary>
    /// 연결 해제 및 정리 (실패 시)
    /// </summary>
    private IEnumerator DisconnectAndCleanup()
    {
        // 방에서 나가기
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            
            float timer = 0f;
            while (PhotonNetwork.InRoom && timer < 2f)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
        }
        
        // 로비에서 나가기
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
            
            float timer = 0f;
            while (PhotonNetwork.InLobby && timer < 2f)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
        }
        
        // Photon 연결 완전 해제
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("TutorialFindMatching: 실패 후 Photon 연결 해제");
            PhotonNetwork.Disconnect();
            
            float timer = 0f;
            while (PhotonNetwork.IsConnected && timer < 3f)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            
            Debug.Log("TutorialFindMatching: Photon 연결 완전 해제 완료");
        }
        
        // UI 리셋
        ResetUI();
        
        // 3초 후 모달 닫기
        yield return new WaitForSeconds(3f);
        
        if (modalWindow != null)
        {
            modalWindow.CloseWindow();
        }
    }
    
    #endregion
    
    #region UI 업데이트
    
    /// <summary>
    /// UI 리셋
    /// </summary>
    private void ResetUI()
    {
        // HeatUI ButtonManager 활성화
        if (startTutorialButton != null)
        {
            startTutorialButton.Interactable(true);
        }
        
        // 모달 윈도우 닫기
        if (modalWindow != null)
        {
            modalWindow.CloseWindow();
        }
    }
    
    /// <summary>
    /// 상태 텍스트 업데이트
    /// </summary>
    private void UpdateUI(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
    
    /// <summary>
    /// 모달 윈도우 업데이트
    /// </summary>
    private void UpdateModalWindow(string message)
    {
        if (modalWindow != null && isConnecting)
        {
            modalWindow.descriptionText = message;
            modalWindow.UpdateUI();
        }
    }
    
    #endregion
    
    #region Photon 콜백
    
    public override void OnConnectedToMaster()
    {
        Debug.Log("TutorialFindMatching: 마스터 서버 연결 완료 (콜백)");
        isMasterServerConnected = true;
        
        UpdateModalWindow("서버 연결 완료!");
        
        Debug.Log($"TutorialFindMatching: 연결 상태 - IsConnected: {PhotonNetwork.IsConnected}, IsConnectedAndReady: {PhotonNetwork.IsConnectedAndReady}");
    }
    
    public override void OnJoinedLobby()
    {
        Debug.Log("TutorialFindMatching: 로비 진입 완료");
        UpdateModalWindow("로비 진입 완료!");
    }
    
    public override void OnCreatedRoom()
    {
        Debug.Log("TutorialFindMatching: 튜토리얼 방 생성 완료");
        UpdateModalWindow("튜토리얼 방 생성 완료!");
    }
    
    public override void OnJoinedRoom()
    {
        Debug.Log("TutorialFindMatching: 방 입장 완료");
        
        // 튜토리얼 진행 중이 아니라면 무시 (FindMatching으로 입장한 경우)
        if (!isConnecting)
        {
            Debug.Log("TutorialFindMatching: 튜토리얼 진행 중이 아니므로 무시 (다른 시스템에서 방 입장)");
            return;
        }
        
        // 이미 시작 중이라면 중복 실행 방지
        if (isTutorialStarting)
        {
            Debug.Log("TutorialFindMatching: 이미 튜토리얼 시작 중 (중복 방지)");
            return;
        }
        
        // 튜토리얼 방인지 확인
        if (PhotonNetwork.CurrentRoom != null && 
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("isTutorial", out object isTutorialObj) &&
            (bool)isTutorialObj == true)
        {
            Debug.Log("TutorialFindMatching: 튜토리얼 방 입장 확인됨");
            
            isTutorialStarting = true;
            isConnecting = false;
            
            UpdateModalWindow("튜토리얼 시작!");
            
            // 튜토리얼 씬 로드
            StartCoroutine(LoadTutorialScene());
        }
        else
        {
            Debug.LogWarning("TutorialFindMatching: 튜토리얼 방이 아닌 방에 입장 - 무시");
        }
    }
    
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"TutorialFindMatching: 방 생성 실패 - {returnCode}: {message}");
        HandleConnectionFailure($"방 생성 실패: {message}");
    }
    
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"TutorialFindMatching: 방 입장 실패 - {returnCode}: {message}");
        HandleConnectionFailure($"방 입장 실패: {message}");
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"TutorialFindMatching: 연결 해제 - {cause}");
        
        isConnecting = false;
        isTutorialStarting = false;
        isMasterServerConnected = false;
        
        // 코루틴 중지
        if (connectionCoroutine != null)
        {
            StopCoroutine(connectionCoroutine);
            connectionCoroutine = null;
        }
        
        StopAllCoroutines();
        
        ResetUI();
        UpdateUI($"연결 해제: {cause}");
    }
    
    public override void OnLeftRoom()
    {
        Debug.Log("TutorialFindMatching: 방 나가기 완료");
    }
    
    public override void OnLeftLobby()
    {
        Debug.Log("TutorialFindMatching: 로비 나가기 완료");
    }
    
    #endregion
    
    #region 공개 메서드
    
    /// <summary>
    /// 외부에서 튜토리얼 시작 (다른 스크립트에서 호출 가능)
    /// </summary>
    public void StartTutorialFromExternal()
    {
        OnClickStartTutorialButton();
    }
    
    /// <summary>
    /// 현재 튜토리얼 진행 상태 확인
    /// </summary>
    public bool IsTutorialStarting()
    {
        return isTutorialStarting || isConnecting;
    }
    
    #endregion
}
