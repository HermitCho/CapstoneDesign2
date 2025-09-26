using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using System;

/// <summary>
/// 게임 시작 전 준비 단계를 관리하는 패널
/// 모든 플레이어가 입장할 때까지 대기하고, 카운트다운 후 게임 시작
/// </summary>
public class ReadyPanel : MonoBehaviourPunCallbacks
{
    [Header("카메라 설정")]
    [SerializeField] private Transform readyCameraPosition;
    [SerializeField] private Camera readyCamera;
    
    [Header("UI 요소")]
    [SerializeField] private TextMeshProUGUI readyText;
    [SerializeField] private TextMeshProUGUI playerCountText;

    
    [Header("게임 설정")]
    [SerializeField] private float minWaitingPlayerTime = 20;
    [SerializeField] private float countdownDuration = 5f;
    
    private int currentPlayerCount = 0;
    
    private bool isCountdownStarted = false;
    private bool isGameStarted = false;
    private Coroutine countdownCoroutine;
    private Coroutine waitingCoroutine;
    private Coroutine waitingTextAnimationCoroutine;
    private Coroutine cameraFixCoroutine;
    
    // 이벤트 제거 - Room Properties 기반으로 변경
    
    void Start()
    {
        InitializeReadyState();
        SetupCamera();
        UpdatePlayerCountDisplay();
        
        // 방 속성 변경 감지
        if (PhotonNetwork.InRoom)
        {
            CheckGamePhase();
        }
        
        // 플레이어 로딩 상태 추적 시작
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("ReadyPanel: 마스터 클라이언트 - 플레이어 로딩 추적 시작");
            StartPlayerLoadingTracker();
        }
        else
        {
            Debug.Log("ReadyPanel: 비마스터 클라이언트 - 로딩 추적 대기");
        }
        
        // 현재 방의 플레이어 수 설정
        currentPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        
        // 초기 UI 업데이트
        UpdatePlayerCountUI();
        
        // 로컬 플레이어 로딩 완료 알림
        StartCoroutine(NotifyPlayerReady());
        
        // Waiting 텍스트 애니메이션 시작
        StartWaitingTextAnimation();
    }
    
    /// <summary>
    /// Ready 상태 초기화
    /// </summary>
    private void InitializeReadyState()
    {
        if (readyText != null)
        {
            readyText.text = "Waiting for players.";
            readyText.fontSize = 100;
        }
 
        // 플레이어 움직임 비활성화
        DisablePlayerMovement();
    }
    
    /// <summary>
    /// Waiting 텍스트 애니메이션 시작
    /// </summary>
    private void StartWaitingTextAnimation()
    {
        if (waitingTextAnimationCoroutine != null)
        {
            StopCoroutine(waitingTextAnimationCoroutine);
        }
        waitingTextAnimationCoroutine = StartCoroutine(WaitingTextAnimationCoroutine());
    }
    
    /// <summary>
    /// Waiting 텍스트 애니메이션 코루틴
    /// </summary>
    private IEnumerator WaitingTextAnimationCoroutine()
    {
        string[] waitingTexts = { "Waiting for players.", "Waiting for players..", "Waiting for players..." };
        int currentIndex = 0;
        
        while (!isCountdownStarted && !isGameStarted)
        {
            if (readyText != null)
            {
                readyText.text = waitingTexts[currentIndex];
                readyText.fontSize = 100;
            }
            
            currentIndex = (currentIndex + 1) % waitingTexts.Length;
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    /// <summary>
    /// Waiting 텍스트 애니메이션 중지
    /// </summary>
    private void StopWaitingTextAnimation()
    {
        if (waitingTextAnimationCoroutine != null)
        {
            StopCoroutine(waitingTextAnimationCoroutine);
            waitingTextAnimationCoroutine = null;
        }
    }
    
    /// <summary>
    /// Cinemachine Virtual Camera들을 비활성화
    /// </summary>
    private void DisableCinemachineVirtualCameras()
    {
        try
        {
            // Cinemachine Virtual Camera 컴포넌트들을 모두 찾아서 비활성화
            var virtualCameras = FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>();
            foreach (var vcam in virtualCameras)
            {
                vcam.enabled = false;
                Debug.Log($"ReadyPanel: Cinemachine Virtual Camera 비활성화 - {vcam.name}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"ReadyPanel: Cinemachine이 설치되지 않았거나 Virtual Camera를 찾을 수 없음 - {e.Message}");
        }
        
        // 혹시 GameObject 자체를 비활성화해야 하는 경우
        GameObject[] virtualCameraObjects = GameObject.FindGameObjectsWithTag("VirtualCamera");
        foreach (var vcamObj in virtualCameraObjects)
        {
            vcamObj.SetActive(false);
            Debug.Log($"ReadyPanel: Virtual Camera GameObject 비활성화 - {vcamObj.name}");
        }
    }
    
    /// <summary>
    /// Cinemachine Virtual Camera들을 다시 활성화
    /// </summary>
    private void EnableCinemachineVirtualCameras()
    {
        try
        {
            // Cinemachine Virtual Camera 컴포넌트들을 모두 찾아서 활성화
            var virtualCameras = FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>(true); // 비활성화된 것도 찾기
            foreach (var vcam in virtualCameras)
            {
                vcam.enabled = true;
                Debug.Log($"ReadyPanel: Cinemachine Virtual Camera 활성화 - {vcam.name}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"ReadyPanel: Cinemachine이 설치되지 않았거나 Virtual Camera를 찾을 수 없음 - {e.Message}");
        }
        
        // GameObject 자체가 비활성화된 경우 다시 활성화
        GameObject[] virtualCameraObjects = GameObject.FindGameObjectsWithTag("VirtualCamera");
        foreach (var vcamObj in virtualCameraObjects)
        {
            vcamObj.SetActive(true);
            Debug.Log($"ReadyPanel: Virtual Camera GameObject 활성화 - {vcamObj.name}");
        }
    }
    
    /// <summary>
    /// 카메라를 Ready 위치로 설정
    /// </summary>
    private void SetupCamera()
    {
        StartCoroutine(SetupCameraWithDelay());
    }
    
    /// <summary>
    /// 약간의 지연 후 카메라 설정 (다른 컴포넌트들이 카메라를 설정하기 전에)
    /// </summary>
    private IEnumerator SetupCameraWithDelay()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (readyCameraPosition != null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
            
            if (mainCamera != null)
            {
                readyCamera = mainCamera;
                
                // 강제로 카메라 위치 설정
                readyCamera.transform.position = readyCameraPosition.position;
                readyCamera.transform.rotation = readyCameraPosition.rotation;
                
                // 카메라 컨트롤러 비활성화
                CameraController camController = readyCamera.GetComponent<CameraController>();
                if (camController != null)
                {
                    camController.enabled = false;
                }
                
                // GameOverController가 카메라를 설정하지 못하도록 방지
                GameOverController gameOverController = FindObjectOfType<GameOverController>();
                if (gameOverController != null)
                {
                    gameOverController.enabled = false;
                }
                
                // GameOverPanel도 비활성화
                GameOverPanel gameOverPanel = FindObjectOfType<GameOverPanel>();
                if (gameOverPanel != null)
                {
                    gameOverPanel.gameObject.SetActive(false);
                    Debug.Log("ReadyPanel: GameOverPanel 비활성화");
                }
                
                // Cinemachine Virtual Camera들을 비활성화 (사용하지 않으므로 주석 처리)
                // DisableCinemachineVirtualCameras();
                
                Debug.Log($"ReadyPanel: 카메라 위치 설정 완료 - Position: {readyCamera.transform.position}");
            }
        }
    }
    
    /// <summary>
    /// 로컬 플레이어 로딩 완료 알림
    /// </summary>
    private IEnumerator NotifyPlayerReady()
    {
        // 씬 로딩 완료 대기
        yield return new WaitForSeconds(1f);
        
        Debug.Log($"ReadyPanel: 플레이어 {PhotonNetwork.LocalPlayer.ActorNumber} 로딩 완료 알림");
        
        // 로컬 플레이어 로딩 완료를 Custom Properties에 설정
        var props = new ExitGames.Client.Photon.Hashtable();
        props[$"playerReady_{PhotonNetwork.LocalPlayer.ActorNumber}"] = true;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
    
    /// <summary>
    /// 플레이어 로딩 상태 추적 시작 (마스터 클라이언트만)
    /// </summary>
    private void StartPlayerLoadingTracker()
    {
        if (waitingCoroutine != null)
        {
            StopCoroutine(waitingCoroutine);
        }
        waitingCoroutine = StartCoroutine(WaitForPlayersOrTimeout());
    }
    
    /// <summary>
    /// 모든 플레이어 로딩 완료 대기 또는 타임아웃
    /// </summary>
    private IEnumerator WaitForPlayersOrTimeout()
    {
        float elapsedTime = 0f;
        Debug.Log($"ReadyPanel: 플레이어 로딩 추적 시작 - 최대 대기 시간: {minWaitingPlayerTime}초");
        
        while (elapsedTime < minWaitingPlayerTime && !isCountdownStarted && !isGameStarted)
        {
            // 현재 방의 모든 플레이어 확인 (동적으로 업데이트)
            var allPlayers = PhotonNetwork.PlayerList;
            currentPlayerCount = allPlayers.Length; // 자동으로 플레이어 수 업데이트
            int readyPlayerCount = 0;
            
            foreach (var player in allPlayers)
            {
                if (player.CustomProperties.ContainsKey($"playerReady_{player.ActorNumber}"))
                {
                    readyPlayerCount++;
                    Debug.Log($"ReadyPanel: 플레이어 {player.ActorNumber} 준비 완료");
                }
            }
            
            Debug.Log($"ReadyPanel: 로딩 상태 체크 - {readyPlayerCount}/{currentPlayerCount} 준비됨, 경과 시간: {elapsedTime:F1}초");
            
            // 플레이어 상태 UI 업데이트
            UpdatePlayerReadyStatus(readyPlayerCount, currentPlayerCount);
            
            // 모든 플레이어가 준비되었다면 카운트다운 시작
            if (readyPlayerCount >= currentPlayerCount && currentPlayerCount > 0)
            {
                Debug.Log("ReadyPanel: 모든 플레이어 준비 완료 - 카운트다운 시작");
                StartCountdown();
                yield break;
            }
            
            yield return new WaitForSeconds(0.5f);
            elapsedTime += 0.5f;
        }
        
        // 타임아웃 도달 - 준비된 플레이어들끼리 게임 시작
        if (!isCountdownStarted && !isGameStarted)
        {
            Debug.Log("ReadyPanel: 타임아웃 도달 - 강제 카운트다운 시작");
            StartCountdown();
        }
    }
    
    /// <summary>
    /// 플레이어 준비 상태 UI 업데이트
    /// </summary>
    private void UpdatePlayerReadyStatus(int readyCount, int totalCount)
    {
        Debug.Log($"ReadyPanel: 플레이어 준비 상태 업데이트 - {readyCount}/{totalCount}");
        
        if (readyCount > 0)
        {
            // 일부 플레이어가 준비되었으면 Loading 표시
            StopWaitingTextAnimation();
            
            if (readyText != null)
            {
                readyText.text = $"Loading... ({readyCount}/{totalCount})";
                readyText.fontSize = 100;
            }
        }
        else
        {
            // 아무도 준비되지 않았으면 계속 Waiting 애니메이션
            if (waitingTextAnimationCoroutine == null)
            {
                StartWaitingTextAnimation();
            }
        }
        
        if (playerCountText != null)
        {
            playerCountText.text = $"{readyCount}/{totalCount} Ready";
        }
    }
    
    /// <summary>
    /// 플레이어 수 변경 시 UI 업데이트 (모든 클라이언트용)
    /// </summary>
    private void UpdatePlayerCountUI()
    {
        // 현재 방의 모든 플레이어 확인
        var allPlayers = PhotonNetwork.PlayerList;
        int totalCount = allPlayers.Length;
        int readyCount = 0;
        
        // 준비된 플레이어 수 계산
        foreach (var player in allPlayers)
        {
            if (player.CustomProperties.ContainsKey($"playerReady_{player.ActorNumber}"))
            {
                readyCount++;
            }
        }
        
        Debug.Log($"ReadyPanel: UI 업데이트 - {readyCount}/{totalCount} Ready (클라이언트: {(PhotonNetwork.IsMasterClient ? "마스터" : "비마스터")})");
        
        // UI 업데이트
        UpdatePlayerReadyStatus(readyCount, totalCount);
    }
    
    /// <summary>
    /// 플레이어 수 표시 업데이트 (사용되지 않음 - UpdatePlayerReadyStatus로 대체)
    /// </summary>
    private void UpdatePlayerCountDisplay()
    {
        // 로딩 상태 추적 시스템으로 대체됨
        // UpdatePlayerReadyStatus에서 UI 업데이트 처리
    }
    
    /// <summary>
    /// 카운트다운 시작 (마스터 클라이언트만)
    /// </summary>
    private void StartCountdown()
    {
        if (!PhotonNetwork.IsMasterClient || isCountdownStarted) return;
        
        Debug.Log("ReadyPanel: 마스터 클라이언트가 카운트다운 시작");
        
        // Room Properties로 카운트다운 시작 알림 (isCountdownStarted는 HandleCountdownStart에서 설정)
        var props = new ExitGames.Client.Photon.Hashtable();
        props["countdownStarted"] = true;
        props["countdownStartTime"] = PhotonNetwork.Time;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
        // 마스터 클라이언트도 즉시 카운트다운 시작
        HandleCountdownStart();
    }
    
    /// <summary>
    /// 카운트다운 시작 처리 (Room Properties 기반)
    /// </summary>
    private void HandleCountdownStart()
    {
        Debug.Log("ReadyPanel: HandleCountdownStart 호출됨");
        
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        
        isCountdownStarted = true;
        
        // Waiting 애니메이션 중지
        StopWaitingTextAnimation();
        
        Debug.Log("ReadyPanel: 카운트다운 코루틴 시작");
        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }
    
    /// <summary>
    /// 카운트다운 코루틴
    /// </summary>
    private IEnumerator CountdownCoroutine()
    {
        Debug.Log($"ReadyPanel: CountdownCoroutine 시작 - countdownDuration: {countdownDuration}");

        // 5-4-3-2-1 카운트다운
        for (int i = (int)countdownDuration; i > 0; i--)
        {
            Debug.Log($"ReadyPanel: 카운트다운 - {i}");
            
            if (readyText != null)
            {
                readyText.text = i.ToString();
                readyText.fontSize = 200;
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        // START!
        Debug.Log("ReadyPanel: START! 표시");
        if (readyText != null)
        {
            readyText.text = "START!";
            readyText.fontSize = 200;
        }
        
        yield return new WaitForSeconds(1f);
        
        // 게임 실제 시작
        Debug.Log("ReadyPanel: StartActualGame 호출");
        StartActualGame();
    }
    
    /// <summary>
    /// 실제 게임 시작
    /// </summary>
    private void StartActualGame()
    {
        if (isGameStarted) return;
        
        Debug.Log("ReadyPanel: 실제 게임 시작!");
        
        isGameStarted = true;
        
        // 모든 애니메이션 중지
        StopWaitingTextAnimation();
        
        // GameOverController 다시 활성화
        GameOverController gameOverController = FindObjectOfType<GameOverController>();
        if (gameOverController != null)
        {
            gameOverController.enabled = true;
        }
        
        // GameOverPanel 다시 활성화 (비활성화 상태로 두되, 컴포넌트는 활성화)
        GameOverPanel gameOverPanel = FindObjectOfType<GameOverPanel>(true); // 비활성화된 것도 찾기
        if (gameOverPanel != null)
        {
            gameOverPanel.gameObject.SetActive(true);
            gameOverPanel.gameObject.SetActive(false); // 즉시 다시 비활성화 (컴포넌트만 활성화)
            Debug.Log("ReadyPanel: GameOverPanel 컴포넌트 활성화");
        }
        
        // Cinemachine Virtual Camera들을 다시 활성화 (사용하지 않으므로 주석 처리)
        // EnableCinemachineVirtualCameras();
        
        // 마스터 클라이언트가 게임 단계를 PLAYING으로 변경
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("ReadyPanel: 마스터 클라이언트가 게임 단계를 PLAYING으로 변경");
            var props = new ExitGames.Client.Photon.Hashtable();
            props["gamePhase"] = "PLAYING";
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        
        // ReadyPanel 비활성화 (Room Properties로 게임 시작이 전달됨)
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 플레이어 움직임 비활성화
    /// </summary>
    private void DisablePlayerMovement()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                // 로컬 플레이어의 움직임 컨트롤러 비활성화
                MoveController moveController = player.GetComponent<MoveController>();
                if (moveController != null)
                {
                    moveController.enabled = false;
                }
                
                // 스킬 컨트롤러 비활성화
                SkillController skillController = player.GetComponent<SkillController>();
                if (skillController != null)
                {
                    skillController.enabled = false;
                }
            }
        }
        
        // 마우스 커서 표시
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    /// <summary>
    /// 플레이어 움직임 활성화
    /// </summary>
    public void EnablePlayerMovement()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                // 로컬 플레이어의 움직임 컨트롤러 활성화
                MoveController moveController = player.GetComponent<MoveController>();
                if (moveController != null)
                {
                    moveController.enabled = true;
                }
                
                // 스킬 컨트롤러 활성화
                SkillController skillController = player.GetComponent<SkillController>();
                if (skillController != null)
                {
                    skillController.enabled = true;
                }
                
                // 카메라 컨트롤러 활성화
                if (readyCamera != null)
                {
                    CameraController camController = readyCamera.GetComponent<CameraController>();
                    if (camController != null)
                    {
                        camController.enabled = true;
                    }
                }
            }
        }
        
        // 마우스 커서 숨김
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    /// <summary>
    /// 게임 단계 확인
    /// </summary>
    private void CheckGamePhase()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gamePhase", out object phase))
        {
            string gamePhase = phase.ToString();
            
            if (gamePhase == "PLAYING" && !isGameStarted)
            {
                StartActualGame();
            }
        }
    }
    
    #region Photon 콜백
    
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        // 플레이어 수 업데이트
        currentPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        Debug.Log($"ReadyPanel: 플레이어 입장 - 현재 플레이어 수: {currentPlayerCount}");
        
        // 모든 클라이언트에서 UI 업데이트
        UpdatePlayerCountUI();
        
        // 새 플레이어가 들어왔을 때 로딩 추적 재시작 (마스터만)
        if (PhotonNetwork.IsMasterClient && !isCountdownStarted)
        {
            StartPlayerLoadingTracker();
        }
    }
    
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        // 플레이어 수 업데이트
        currentPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        Debug.Log($"ReadyPanel: 플레이어 퇴장 - 현재 플레이어 수: {currentPlayerCount}");
        
        // 모든 클라이언트에서 UI 업데이트
        UpdatePlayerCountUI();
        
        // 플레이어가 나갔을 때 로딩 추적 재시작 (마스터만)
        if (PhotonNetwork.IsMasterClient && !isCountdownStarted)
        {
            StartPlayerLoadingTracker();
        }
        
        // 카운트다운 중이었다면 중단
        if (isCountdownStarted && !isGameStarted)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                var props = new ExitGames.Client.Photon.Hashtable();
                props["countdownStarted"] = false;
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            }
        }
    }
    
    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // 플레이어 로딩 상태가 변경되었을 때
        foreach (var prop in changedProps)
        {
            if (prop.Key.ToString().StartsWith("playerReady_"))
            {
                Debug.Log($"ReadyPanel: 플레이어 {targetPlayer.ActorNumber} 준비 상태 변경됨");
                
                // 모든 클라이언트에서 UI 업데이트
                UpdatePlayerCountUI();
                
                // 마스터 클라이언트만 카운트다운 로직 처리
                if (PhotonNetwork.IsMasterClient && !isCountdownStarted)
                {
                    // 로딩 상태 변경 시 즉시 확인
                    CheckAllPlayersReady();
                }
                break;
            }
        }
    }
    
    /// <summary>
    /// 모든 플레이어 준비 상태 즉시 확인
    /// </summary>
    private void CheckAllPlayersReady()
    {
        var allPlayers = PhotonNetwork.PlayerList;
        currentPlayerCount = allPlayers.Length; // 플레이어 수 업데이트
        int readyPlayerCount = 0;
        
        foreach (var player in allPlayers)
        {
            if (player.CustomProperties.ContainsKey($"playerReady_{player.ActorNumber}"))
            {
                readyPlayerCount++;
            }
        }
        
        // UI 업데이트
        UpdatePlayerReadyStatus(readyPlayerCount, currentPlayerCount);
        
        // 모든 플레이어가 준비되었다면 즉시 카운트다운 시작
        if (readyPlayerCount >= currentPlayerCount && currentPlayerCount > 0 && !isCountdownStarted)
        {
            StartCountdown();
        }
    }
    
    /// <summary>
    /// 카운트다운 취소 처리 (Room Properties 기반)
    /// </summary>
    private void HandleCountdownCancel()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        
        isCountdownStarted = false;

        if (readyText != null)
        {
            readyText.text = "Waiting for players.";
            readyText.fontSize = 100;
        }
        
        // Waiting 텍스트 애니메이션 다시 시작
        StartWaitingTextAnimation();
    }
    
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("gamePhase"))
        {
            CheckGamePhase();
        }
        
        // 카운트다운 시작/취소 처리
        if (propertiesThatChanged.ContainsKey("countdownStarted"))
        {
            bool countdownStarted = (bool)propertiesThatChanged["countdownStarted"];
            
            if (countdownStarted && !isCountdownStarted)
            {
                HandleCountdownStart();
            }
            else if (!countdownStarted && isCountdownStarted)
            {
                HandleCountdownCancel();
            }
        }
    }
    
    #endregion
}
