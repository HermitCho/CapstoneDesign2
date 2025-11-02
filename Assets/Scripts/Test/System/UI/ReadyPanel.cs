using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using System;
using DG.Tweening;

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
    
    [Header("카운트다운 애니메이션 설정")]
    [SerializeField] private float countdownPopScale = 1.5f;
    [SerializeField] private float countdownRotationAngle = 15f;
    [SerializeField] private float countdownPopDuration = 0.3f;
    
    private int currentPlayerCount = 0;
    
    private bool isCountdownStarted = false;
    private bool isGameStarted = false;
    private bool areCharactersSpawned = false;
    private Coroutine countdownCoroutine;
    private Coroutine waitingCoroutine;
    private Coroutine waitingTextAnimationCoroutine;
    private Tween countdownTween;
    
    // 이벤트 제거 - Room Properties 기반으로 변경
    
    /// <summary>
    /// 플레이어 Ready 상태 초기화 (두 번째 게임 문제 해결)
    /// </summary>
    private void ClearPlayerReadyState()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null) return;
        
        // 로컬 플레이어의 Ready 상태만 초기화
        var props = new ExitGames.Client.Photon.Hashtable();
        props[$"playerReady_{PhotonNetwork.LocalPlayer.ActorNumber}"] = null;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        
        Debug.Log($"ReadyPanel: 플레이어 {PhotonNetwork.LocalPlayer.ActorNumber} Ready 상태 초기화");
    }
    
    void Start()
    {
        ClearPlayerReadyState();
        
        InitializeReadyState();
        SetupCamera();
        UpdatePlayerCountDisplay();
        
        if (PhotonNetwork.InRoom)
        {
            CheckGamePhase();
        }
        
        if (PhotonNetwork.IsMasterClient)
        {
            StartPlayerLoadingTracker();
        }
        else
        {
            StartNonMasterUITracker();
        }
        
        currentPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        
        UpdatePlayerCountUI();
        
        StartCoroutine(NotifyPlayerReady());
        
        StartWaitingTextAnimation();
    }
    
    void OnDestroy()
    {
        countdownTween?.Kill();
        countdownTween = null;
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
    /// 비마스터 클라이언트용 UI 업데이트 추적기
    /// </summary>
    private void StartNonMasterUITracker()
    {
        if (waitingCoroutine != null)
        {
            StopCoroutine(waitingCoroutine);
        }
        waitingCoroutine = StartCoroutine(NonMasterUIUpdateCoroutine());
    }
    
    /// <summary>
    /// 비마스터 클라이언트용 UI 업데이트 코루틴
    /// </summary>
    private IEnumerator NonMasterUIUpdateCoroutine()
    {
        while (!areCharactersSpawned && !isGameStarted)
        {
            var allPlayers = PhotonNetwork.PlayerList;
            currentPlayerCount = allPlayers.Length;
            int readyPlayerCount = 0;
            
            foreach (var player in allPlayers)
            {
                if (player.CustomProperties.TryGetValue($"playerReady_{player.ActorNumber}", out object readyValue))
                {
                    if (readyValue != null && readyValue is bool boolValue && boolValue)
                    {
                        readyPlayerCount++;
                    }
                }
            }
            
            UpdatePlayerReadyStatus(readyPlayerCount, currentPlayerCount);
            
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    /// <summary>
    /// 모든 플레이어 로딩 완료 대기 또는 타임아웃
    /// </summary>
    private IEnumerator WaitForPlayersOrTimeout()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < minWaitingPlayerTime && !areCharactersSpawned && !isGameStarted)
        {
            var allPlayers = PhotonNetwork.PlayerList;
            currentPlayerCount = allPlayers.Length;
            int readyPlayerCount = 0;
            
            foreach (var player in allPlayers)
            {
                if (player.CustomProperties.TryGetValue($"playerReady_{player.ActorNumber}", out object readyValue))
                {
                    if (readyValue != null && readyValue is bool boolValue && boolValue)
                    {
                        readyPlayerCount++;
                    }
                }
            }
            
            UpdatePlayerReadyStatus(readyPlayerCount, currentPlayerCount);
            
            if (readyPlayerCount >= currentPlayerCount && currentPlayerCount > 0)
            {
                TriggerCharacterSpawn();
                yield break;
            }
            
            yield return new WaitForSeconds(0.5f);
            elapsedTime += 0.5f;
        }
        
        if (!areCharactersSpawned && !isGameStarted)
        {
            TriggerCharacterSpawn();
        }
    }
    
    /// <summary>
    /// 캐릭터 스폰 트리거
    /// </summary>
    private void TriggerCharacterSpawn()
    {
        if (areCharactersSpawned) return;
        
        areCharactersSpawned = true;
        StopWaitingTextAnimation();
        
        if (readyText != null)
        {
            readyText.text = "Get Ready!";
            readyText.fontSize = 100;
        }
        
        if (playerCountText != null)
        {
            playerCountText.gameObject.SetActive(false);
        }
        
        if (PhotonNetwork.IsMasterClient)
        {
            var props = new ExitGames.Client.Photon.Hashtable();
            props["spawnCharacters"] = true;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            
            StartCoroutine(WaitForSpawnAndStartCountdown());
        }
    }
    
    /// <summary>
    /// 캐릭터 스폰 대기 후 카운트다운 시작
    /// </summary>
    private IEnumerator WaitForSpawnAndStartCountdown()
    {
        yield return new WaitForSeconds(2f);
        
        UnityEngine.Debug.Log("ReadyPanel: 스폰 대기 완료 - 카운트다운 시작");
        StartCountdown();
    }
    
    /// <summary>
    /// 플레이어 준비 상태 UI 업데이트
    /// </summary>
    private void UpdatePlayerReadyStatus(int readyCount, int totalCount)
    {
        if (readyCount > 0)
        {
            StopWaitingTextAnimation();
            
            if (readyText != null)
            {
                readyText.text = $"Loading... ({readyCount}/{totalCount})";
                readyText.fontSize = 100;
            }
        }
        else
        {
            if (waitingTextAnimationCoroutine == null)
            {
                StartWaitingTextAnimation();
            }
        }
        
        if (playerCountText != null)
        {
            playerCountText.text = $"{readyCount}/{totalCount} 준비됨!";
        }
    }
    
    /// <summary>
    /// 플레이어 수 변경 시 UI 업데이트 (모든 클라이언트용)
    /// </summary>
    private void UpdatePlayerCountUI()
    {
        var allPlayers = PhotonNetwork.PlayerList;
        int totalCount = allPlayers.Length;
        int readyCount = 0;
        
        foreach (var player in allPlayers)
        {
            if (player.CustomProperties.TryGetValue($"playerReady_{player.ActorNumber}", out object readyValue))
            {
                if (readyValue != null && readyValue is bool boolValue && boolValue)
                {
                    readyCount++;
                }
            }
        }
        
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
        
        UnityEngine.Debug.Log("ReadyPanel: StartCountdown 호출됨 (마스터)");
        
        var props = new ExitGames.Client.Photon.Hashtable();
        props["countdownStarted"] = true;
        props["countdownStartTime"] = PhotonNetwork.Time;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
        UnityEngine.Debug.Log($"ReadyPanel: Room Properties 설정 완료 - countdownStartTime: {PhotonNetwork.Time}");
    }
    
    /// <summary>
    /// 카운트다운 시작 처리 (Room Properties 기반)
    /// </summary>
    private void HandleCountdownStart()
    {
        UnityEngine.Debug.Log("ReadyPanel: HandleCountdownStart 호출됨");
        
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        
        isCountdownStarted = true;
        StopWaitingTextAnimation();
        
        UnityEngine.Debug.Log("ReadyPanel: 카운트다운 코루틴 시작");
        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }
    
    /// <summary>
    /// 카운트다운 코루틴 (간단한 방식)
    /// </summary>
    private IEnumerator CountdownCoroutine()
    {
        UnityEngine.Debug.Log($"ReadyPanel: 카운트다운 시작 - countdownDuration: {countdownDuration}");
        
        for (int i = (int)countdownDuration; i > 0; i--)
        {
            UnityEngine.Debug.Log($"ReadyPanel: 카운트 {i} 애니메이션 시작");
            yield return StartCoroutine(AnimateCountdownNumber(i));
            UnityEngine.Debug.Log($"ReadyPanel: 카운트 {i} 애니메이션 완료");
            
            if (i > 1)
            {
                UnityEngine.Debug.Log($"ReadyPanel: 다음 카운트까지 대기 중...");
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        UnityEngine.Debug.Log("ReadyPanel: START 애니메이션 시작");
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(AnimateStartText());
        
        UnityEngine.Debug.Log("ReadyPanel: 게임 시작 호출");
        StartActualGame();
    }
    
    /// <summary>
    /// 카운트다운 숫자 애니메이션 (카트라이더 스타일)
    /// </summary>
    private IEnumerator AnimateCountdownNumber(int number)
    {
        if (readyText == null) yield break;
        
        readyText.text = number.ToString();
        readyText.fontSize = 200;
        readyText.alpha = 1f;
        
        if (AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayOneShot("SFX_UI_Ready_Countdown");
        }
        
        countdownTween?.Kill();
        
        readyText.transform.localScale = Vector3.zero;
        readyText.transform.localRotation = Quaternion.Euler(0f, 0f, -countdownRotationAngle);
        
        Sequence seq = DOTween.Sequence();
        
        seq.Append(readyText.transform.DOScale(countdownPopScale, 0.2f)
            .SetEase(Ease.OutBack));
        seq.Join(readyText.transform.DORotate(new Vector3(0f, 0f, countdownRotationAngle), 0.15f)
            .SetEase(Ease.OutQuad));
        seq.Append(readyText.transform.DORotate(Vector3.zero, 0.1f)
            .SetEase(Ease.InOutQuad));
        seq.Join(readyText.transform.DOScale(1f, 0.15f)
            .SetEase(Ease.InOutQuad));
        
        seq.Append(readyText.transform.DOScale(0.8f, 0.25f)
            .SetEase(Ease.InBack));
        seq.Join(readyText.DOFade(0f, 0.2f)
            .SetEase(Ease.InQuad));
        
        countdownTween = seq;
        
        yield return seq.WaitForCompletion();
    }
    
    /// <summary>
    /// START 텍스트 애니메이션
    /// </summary>
    private IEnumerator AnimateStartText()
    {
        if (readyText == null) yield break;
        
        readyText.text = "START!";
        readyText.fontSize = 200;
        readyText.alpha = 1f;
        
        if (AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayOneShot("SFX_UI_Ready_Start");
        }
        
        countdownTween?.Kill();
        
        readyText.transform.localScale = Vector3.zero;
        readyText.transform.localRotation = Quaternion.identity;
        
        Sequence seq = DOTween.Sequence();
        
        seq.Append(readyText.transform.DOScale(countdownPopScale * 1.2f, 0.25f)
            .SetEase(Ease.OutBack));
        seq.Join(readyText.DOFade(1f, 0.15f)
            .SetEase(Ease.OutQuad));
        seq.Append(readyText.transform.DOScale(1f, 0.2f)
            .SetEase(Ease.InOutQuad));
        
        countdownTween = seq;
        
        yield return seq.WaitForCompletion();
    }
    
    /// <summary>
    /// 실제 게임 시작
    /// </summary>
    private void StartActualGame()
    {
        if (isGameStarted) return;
        
        isGameStarted = true;
        
        StopWaitingTextAnimation();
        
        GameOverController gameOverController = FindObjectOfType<GameOverController>();
        if (gameOverController != null)
        {
            gameOverController.enabled = true;
        }
        
        GameOverPanel gameOverPanel = FindObjectOfType<GameOverPanel>(true);
        if (gameOverPanel != null)
        {
            gameOverPanel.gameObject.SetActive(true);
            gameOverPanel.gameObject.SetActive(false);
        }
        
        if (PhotonNetwork.IsMasterClient)
        {
            var props = new ExitGames.Client.Photon.Hashtable();
            props["gamePhase"] = "PLAYING";
            props["gameStartTime"] = PhotonNetwork.Time;
            props["playTime"] = DataBase.Instance != null && DataBase.Instance.gameData != null 
                ? DataBase.Instance.gameData.PlayTime 
                : 360f;
            
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        
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
                MoveController moveController = player.GetComponent<MoveController>();
                if (moveController != null)
                {
                    moveController.EnableMoveControls();
                }
                
                SkillController skillController = player.GetComponent<SkillController>();
                if (skillController != null)
                {
                    skillController.EnableSkillControls();
                }
                
                TestGun gun = player.GetComponentInChildren<TestGun>();
                if (gun != null)
                {
                    gun.enabled = true;
                }
                
                CameraController cameraController = player.GetComponent<CameraController>();
                if (cameraController != null)
                {
                    cameraController.enabled = true;
                }
            }
        }
        
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
        currentPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        
        UpdatePlayerCountUI();
        
        if (!areCharactersSpawned)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                StartPlayerLoadingTracker();
            }
            else
            {
                StartNonMasterUITracker();
            }
        }
    }
    
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        currentPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        
        UpdatePlayerCountUI();
        
        if (!areCharactersSpawned)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                StartPlayerLoadingTracker();
            }
            else
            {
                StartNonMasterUITracker();
            }
        }
        
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
        foreach (var prop in changedProps)
        {
            if (prop.Key.ToString().StartsWith("playerReady_"))
            {
                UpdatePlayerCountUI();
                
                if (PhotonNetwork.IsMasterClient && !areCharactersSpawned)
                {
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
        currentPlayerCount = allPlayers.Length;
        int readyPlayerCount = 0;
        
        foreach (var player in allPlayers)
        {
            if (player.CustomProperties.TryGetValue($"playerReady_{player.ActorNumber}", out object readyValue))
            {
                if (readyValue != null && readyValue is bool boolValue && boolValue)
                {
                    readyPlayerCount++;
                }
            }
        }
        
        UpdatePlayerReadyStatus(readyPlayerCount, currentPlayerCount);
        
        if (readyPlayerCount >= currentPlayerCount && currentPlayerCount > 0 && !areCharactersSpawned)
        {
            TriggerCharacterSpawn();
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
        UnityEngine.Debug.Log($"ReadyPanel: OnRoomPropertiesUpdate - Keys: {string.Join(", ", propertiesThatChanged.Keys)}");
        
        if (propertiesThatChanged.ContainsKey("gamePhase"))
        {
            CheckGamePhase();
        }
        
        if (propertiesThatChanged.ContainsKey("spawnCharacters"))
        {
            UnityEngine.Debug.Log("ReadyPanel: spawnCharacters 감지됨");
            
            if (!areCharactersSpawned)
            {
                areCharactersSpawned = true;
                StopWaitingTextAnimation();
                
                if (readyText != null)
                {
                    readyText.text = "Get Ready!";
                    readyText.fontSize = 100;
                }
                
                if (playerCountText != null)
                {
                    playerCountText.gameObject.SetActive(false);
                }
            }
        }
        
        if (propertiesThatChanged.ContainsKey("countdownStarted"))
        {
            bool countdownStarted = (bool)propertiesThatChanged["countdownStarted"];
            
            UnityEngine.Debug.Log($"ReadyPanel: countdownStarted = {countdownStarted}, isCountdownStarted = {isCountdownStarted}");
            
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
