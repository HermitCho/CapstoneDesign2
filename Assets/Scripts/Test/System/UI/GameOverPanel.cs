using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [Header("winner 배경 오브젝트")]
    [Tooltip("winner 배경 오브젝트 (자식에 이미지 및 텍스트 포함)")]
    [SerializeField] private GameObject _winnerBackgroundObject;

    [Header("리더보드 텍스트")]
    [SerializeField] private Image _1stCrownImage;
    [Tooltip("1등 랭킹 이미지")]
    [SerializeField] private Image _1stRankingImage;
    [Tooltip("1등 점수 텍스트")]
    [SerializeField] private TextMeshProUGUI _1stScoreText;
    [Tooltip("1등 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI _1stNameText;

    [Space(10)]
    [Tooltip("2등 랭킹 이미지")]
    [SerializeField] private Image _2ndRankingImage;
    [Tooltip("2등 점수 텍스트")]
    [SerializeField] private TextMeshProUGUI _2ndScoreText;
    [Tooltip("2등 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI _2ndNameText;

    [Space(10)]
    [Tooltip("3등 랭킹 이미지")]
    [SerializeField] private Image _3rdRankingImage;
    [Tooltip("3등 점수 텍스트")]
    [SerializeField] private TextMeshProUGUI _3rdScoreText;
    [Tooltip("3등 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI _3rdNameText;

    [Space(10)]
    [Tooltip("4등 랭킹 이미지")]
    [SerializeField] private Image _4thRankingImage;
    [Tooltip("4등 점수 텍스트")]
    [SerializeField] private TextMeshProUGUI _4thScoreText;
    [Tooltip("4등 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI _4thNameText;

    [Space(10)]
    [Tooltip("Exit 이미지")]
    [SerializeField] private Image _exitImage;

    [Space(10)]
    [Tooltip("Money 이미지")]
    [SerializeField] private Image _moneyImage;
    [Tooltip("Money 텍스트")]
    [SerializeField] private TextMeshProUGUI _moneyText;

    [Header("게임 오버 컨트롤러")]
    [SerializeField] private GameOverController gameOverController;
    
    [Header("애니메이션 설정")]
    [Tooltip("점수 증가 애니메이션 시간")]
    [SerializeField] private float scoreAnimationDuration = 0.8f; // 1.5초 → 0.8초로 빠르게
    [Tooltip("점수 10점당 사운드 재생 간격")]
    [SerializeField] private float scorePerSound = 10f;
    
    // EXIT 스티커 애니메이션 관련
    private Tween exitStickerTween;
    
    // 랭킹 데이터 저장
    private List<GameOverController.PlayerRankData> cachedRankings;
    
    // 애니메이션 완료 여부
    private bool isAnimationComplete = false;
    
    // Winner Background 원본 위치/회전 저장
    private Vector2 winnerOriginalPosition;
    private Vector3 winnerOriginalRotation;
    private bool winnerPositionSaved = false;
    
    // 왕관 호흡 애니메이션 Tween
    private Tween crownBreatheTween;
    
    // Money 애니메이션 Tween
    private Tween moneyImageBounceTween;
    private Tween moneyTextCountTween;


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
        
        // 왕관 호흡 애니메이션 정리
        CleanupCrownBreatheAnimation();
        
        // Money 애니메이션 정리
        CleanupMoneyAnimations();
    }
    
    /// <summary>
    /// 왕관 호흡 애니메이션 정리
    /// </summary>
    private void CleanupCrownBreatheAnimation()
    {
        if (crownBreatheTween != null)
        {
            crownBreatheTween.Kill();
            crownBreatheTween = null;
        }
    }
    
    /// <summary>
    /// Money 애니메이션 정리
    /// </summary>
    private void CleanupMoneyAnimations()
    {
        moneyImageBounceTween?.Kill();
        moneyImageBounceTween = null;
        
        moneyTextCountTween?.Kill();
        moneyTextCountTween = null;
    }

#endregion


#region UI 업데이트

    public void UpdateUI()
    {
        
    }

    /// <summary>
    /// 플레이어 순위 정보 설정 (애니메이션 시작)
    /// </summary>
    public void SetPlayerRankings(List<GameOverController.PlayerRankData> rankings)
    {
        // 랭킹 데이터 캐싱
        cachedRankings = rankings;
        
        // 모든 UI 초기화 (투명 + Scale 0)
        InitializeAllRankingUI();
        
        // 애니메이션 시작
        StartCoroutine(PlayRankingAnimationSequence());
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

#region 랭킹 애니메이션

    /// <summary>
    /// 모든 랭킹 UI 초기화 (투명 + Scale 0)
    /// </summary>
    private void InitializeAllRankingUI()
    {
        // Winner Background 오브젝트 초기화
        if (_winnerBackgroundObject != null)
        {
            // ✅ 오브젝트 활성화 (비활성화 상태면 애니메이션 안됨)
            _winnerBackgroundObject.SetActive(true);
            
            // ✅ 원본 위치/회전 저장 (최초 1회만)
            if (!winnerPositionSaved)
            {
                RectTransform winnerRect = _winnerBackgroundObject.GetComponent<RectTransform>();
                if (winnerRect != null)
                {
                    winnerOriginalPosition = winnerRect.anchoredPosition;
                    winnerOriginalRotation = winnerRect.localEulerAngles;
                    winnerPositionSaved = true;
                }
            }
            
            _winnerBackgroundObject.transform.localScale = Vector3.zero;
            
            // 오브젝트와 모든 자식 요소의 투명도 초기화
            CanvasGroup canvasGroup = _winnerBackgroundObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = _winnerBackgroundObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
        }
        
        // ✅ 왕관 이미지 초기화 (Scale 0 + 완전 투명)
        if (_1stCrownImage != null)
        {
            _1stCrownImage.transform.localScale = Vector3.zero;
            Color crownColor = _1stCrownImage.color;
            crownColor.a = 0f;
            _1stCrownImage.color = crownColor;
        }
        
        // ✅ Money UI 초기화 (비활성화)
        if (_moneyImage != null)
        {
            _moneyImage.gameObject.SetActive(false);
            _moneyImage.transform.localScale = Vector3.zero;
        }
        
        if (_moneyText != null)
        {
            _moneyText.gameObject.SetActive(false);
            _moneyText.text = "x 0";
        }
        
        // 모든 랭킹 UI 초기화
        InitializeRankingUI(_1stRankingImage, _1stNameText, _1stScoreText);
        InitializeRankingUI(_2ndRankingImage, _2ndNameText, _2ndScoreText);
        InitializeRankingUI(_3rdRankingImage, _3rdNameText, _3rdScoreText);
        InitializeRankingUI(_4thRankingImage, _4thNameText, _4thScoreText);
    }
    
    /// <summary>
    /// 개별 랭킹 UI 초기화
    /// </summary>
    private void InitializeRankingUI(Image rankingImage, TextMeshProUGUI nameText, TextMeshProUGUI scoreText)
    {
        if (rankingImage != null)
        {
            rankingImage.transform.localScale = Vector3.zero;
            Color imageColor = rankingImage.color;
            imageColor.a = 0f;
            rankingImage.color = imageColor;
        }
        
        if (nameText != null)
        {
            nameText.transform.localScale = Vector3.zero;
            nameText.alpha = 0f;
            nameText.text = "";
        }
        
        if (scoreText != null)
        {
            scoreText.transform.localScale = Vector3.zero;
            scoreText.alpha = 0f;
            scoreText.text = "0";
        }
    }
    
    /// <summary>
    /// 랭킹 애니메이션 시퀀스 (4등 → 3등 → 2등 → 1등 → Winner Background)
    /// </summary>
    private IEnumerator PlayRankingAnimationSequence()
    {
        isAnimationComplete = false;
        
        if (cachedRankings == null || cachedRankings.Count == 0)
        {
            isAnimationComplete = true;
            yield break;
        }
        
        // 4등부터 1등까지 역순으로 애니메이션
        int playerCount = cachedRankings.Count;
        
        // 4등 (인덱스 3)
        if (playerCount >= 4)
        {
            yield return StartCoroutine(AnimateRanking(3, _4thRankingImage, _4thNameText, _4thScoreText));
        }
        
        // 3등 (인덱스 2)
        if (playerCount >= 3)
        {
            yield return StartCoroutine(AnimateRanking(2, _3rdRankingImage, _3rdNameText, _3rdScoreText));
        }
        
        // 2등 (인덱스 1)
        if (playerCount >= 2)
        {
            yield return StartCoroutine(AnimateRanking(1, _2ndRankingImage, _2ndNameText, _2ndScoreText));
        }
        
        // 1등 (인덱스 0) - 왕관 포함 특별 애니메이션
        if (playerCount >= 1)
        {
            yield return StartCoroutine(AnimateFirstPlaceWithCrown(0));
        }
        
        // Winner Background 애니메이션
        yield return StartCoroutine(AnimateWinnerBackground());
        
        // ✅ Winner Background 애니메이션 완료 후 왕관 호흡 애니메이션 시작
        StartCrownBreatheAnimation();
        
        // Money UI 애니메이션 시작
        yield return StartCoroutine(AnimateMoneyUI());
        
        // 모든 애니메이션 완료
        isAnimationComplete = true;
        
        // GameOverController에 애니메이션 완료 알림
        if (gameOverController != null)
        {
            gameOverController.OnPanelAnimationComplete();
        }
    }
    
    /// <summary>
    /// 개별 랭킹 애니메이션 (RankingImage → NameText → ScoreText)
    /// </summary>
    private IEnumerator AnimateRanking(int rankIndex, Image rankingImage, TextMeshProUGUI nameText, TextMeshProUGUI scoreText)
    {
        if (rankIndex >= cachedRankings.Count) yield break;
        
        var playerData = cachedRankings[rankIndex];
        
        // 1. Ranking Image 뿅 튀어나오기
        if (rankingImage != null)
        {
            // 사운드 재생
            if (AudioManager.Inst != null)
            {
                AudioManager.Inst.PlayOneShot("SFX_UI_GameOver_PopScore");
            }
            
            Sequence imageSequence = DOTween.Sequence();
            imageSequence.Append(rankingImage.DOFade(1f, 0.2f).SetEase(Ease.OutQuad));
            imageSequence.Join(rankingImage.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack));
            imageSequence.Append(rankingImage.transform.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad));
            
            yield return imageSequence.WaitForCompletion();
        }
        
        yield return new WaitForSeconds(0.1f);
        
        // 2. Name Text 뿅 튀어나오기
        if (nameText != null)
        {
            // 사운드 재생
            if (AudioManager.Inst != null)
            {
                AudioManager.Inst.PlayOneShot("SFX_UI_GameOver_PopScore");
            }
            
            // 텍스트 설정
            string displayName = playerData.nickname;
            if (playerData.isLocalPlayer)
            {
                displayName = $"<color=yellow>{displayName}</color>";
            }
            nameText.text = displayName;
            
            Sequence nameSequence = DOTween.Sequence();
            nameSequence.Append(nameText.DOFade(1f, 0.2f).SetEase(Ease.OutQuad));
            nameSequence.Join(nameText.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack));
            nameSequence.Append(nameText.transform.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad));
            
            yield return nameSequence.WaitForCompletion();
        }
        
        yield return new WaitForSeconds(0.1f);
        
        // 3. Score Text 점수 증가 애니메이션
        if (scoreText != null)
        {
            scoreText.transform.localScale = Vector3.one;
            scoreText.alpha = 1f;
            
            yield return StartCoroutine(AnimateScoreIncrease(scoreText, playerData.score));
        }
        
        yield return new WaitForSeconds(0.3f); // 다음 랭킹까지 대기
    }
    
    /// <summary>
    /// 점수 증가 애니메이션 (0 → 목표 점수)
    /// </summary>
    private IEnumerator AnimateScoreIncrease(TextMeshProUGUI scoreText, float targetScore)
    {
        float currentScore = 0f;
        float elapsedTime = 0f;
        int lastDisplayedScore = 0; // 마지막으로 표시된 점수 (정수)
        float lastSoundTime = -1f; // 마지막 사운드 재생 시간
        float soundInterval = 0.2f; // 사운드 재생 간격 (0.2초)
        
        while (elapsedTime < scoreAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / scoreAnimationDuration);
            
            currentScore = Mathf.Lerp(0f, targetScore, t);
            int displayScore = Mathf.RoundToInt(currentScore);
            
            // 점수가 증가할 때마다 UI 업데이트
            if (displayScore > lastDisplayedScore)
            {
                lastDisplayedScore = displayScore;
                scoreText.text = $"{displayScore}";
            }
            
            // ✅ 0.2초마다 사운드 재생
            if (elapsedTime - lastSoundTime >= soundInterval)
            {
                lastSoundTime = elapsedTime;
                if (AudioManager.Inst != null)
                {
                    AudioManager.Inst.PlayOneShot("SFX_UI_LeaderboardRatingText");
                }
            }
            
            yield return null;
        }
        
        // 최종 점수 설정
        scoreText.text = $"{Mathf.RoundToInt(targetScore)}";
    }
    
    /// <summary>
    /// 1등 랭킹 애니메이션 (RankingImage → NameText → ScoreText → CrownImage)
    /// </summary>
    private IEnumerator AnimateFirstPlaceWithCrown(int rankIndex)
    {
        if (rankIndex >= cachedRankings.Count) yield break;
        
        var playerData = cachedRankings[rankIndex];
        
        // 1. Ranking Image 뿅 튀어나오기
        if (_1stRankingImage != null)
        {
            if (AudioManager.Inst != null)
            {
                AudioManager.Inst.PlayOneShot("SFX_UI_GameOver_PopScore");
            }
            
            Sequence imageSequence = DOTween.Sequence();
            imageSequence.Append(_1stRankingImage.DOFade(1f, 0.2f).SetEase(Ease.OutQuad));
            imageSequence.Join(_1stRankingImage.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack));
            imageSequence.Append(_1stRankingImage.transform.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad));
            
            yield return imageSequence.WaitForCompletion();
        }
        
        yield return new WaitForSeconds(0.1f);
        
        // 2. Name Text 뿅 튀어나오기
        if (_1stNameText != null)
        {
            if (AudioManager.Inst != null)
            {
                AudioManager.Inst.PlayOneShot("SFX_UI_GameOver_PopScore");
            }
            
            string displayName = playerData.nickname;
            if (playerData.isLocalPlayer)
            {
                displayName = $"<color=yellow>{displayName}</color>";
            }
            _1stNameText.text = displayName;
            
            Sequence nameSequence = DOTween.Sequence();
            nameSequence.Append(_1stNameText.DOFade(1f, 0.2f).SetEase(Ease.OutQuad));
            nameSequence.Join(_1stNameText.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack));
            nameSequence.Append(_1stNameText.transform.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad));
            
            yield return nameSequence.WaitForCompletion();
        }
        
        yield return new WaitForSeconds(0.1f);
        
        // 3. Score Text 점수 증가 애니메이션
        if (_1stScoreText != null)
        {
            _1stScoreText.transform.localScale = Vector3.one;
            _1stScoreText.alpha = 1f;
            
            yield return StartCoroutine(AnimateScoreIncrease(_1stScoreText, playerData.score));
        }
        
        yield return new WaitForSeconds(0.2f);
        
        // 4. Crown Image 뿅 튀어나오기 + 사운드
        if (_1stCrownImage != null)
        {
            // 사운드 재생
            if (AudioManager.Inst != null)
            {
                AudioManager.Inst.PlayOneShot("SFX_UI_GameOver_PopScore");
            }
            
            // 초기 상태 강제 설정 (확실하게)
            _1stCrownImage.transform.localScale = Vector3.zero;
            Color crownStartColor = _1stCrownImage.color;
            crownStartColor.a = 0f;
            _1stCrownImage.color = crownStartColor;
            
            // 애니메이션 시퀀스 (뿅 튀어나오기)
            Sequence crownSequence = DOTween.Sequence();
            
            // 페이드 인
            crownSequence.Append(_1stCrownImage.DOFade(1f, 0.2f).SetEase(Ease.OutQuad));
            
            // 스케일 애니메이션 (0 → 1.2 → 1.0)
            crownSequence.Join(_1stCrownImage.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack));
            crownSequence.Append(_1stCrownImage.transform.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad));
            
            yield return crownSequence.WaitForCompletion();
        }
        
        yield return new WaitForSeconds(0.3f); // 다음 애니메이션까지 대기
    }
    
    /// <summary>
    /// 왕관 호흡 애니메이션 시작 (무한 반복)
    /// </summary>
    private void StartCrownBreatheAnimation()
    {
        // 기존 애니메이션 정리
        CleanupCrownBreatheAnimation();
        
        // 왕관 호흡 애니메이션 (1.0 ↔ 1.1, 부드럽게 반복)
        if (_1stCrownImage != null)
        {
            crownBreatheTween = _1stCrownImage.transform
                .DOScale(Vector3.one * 1.1f, 1.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo); // 무한 반복 + 왕복
        }
    }
    
    /// <summary>
    /// Winner Background 애니메이션 (회전하면서 스티커 붙듯이 휘리릭 날아와 붙기)
    /// </summary>
    private IEnumerator AnimateWinnerBackground()
    {
        if (_winnerBackgroundObject == null || cachedRankings.Count == 0) yield break;
        
        // Winner 이름 설정
        var winner = cachedRankings[0];
        if (winnerNameText != null)
        {
            winnerNameText.text = winner.nickname;
        }
        
        // 사운드 재생
        if (AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayOneShot("SFX_UI_GameOver_Victory");
        }
        
        // RectTransform 및 CanvasGroup 가져오기
        RectTransform winnerRect = _winnerBackgroundObject.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = _winnerBackgroundObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = _winnerBackgroundObject.AddComponent<CanvasGroup>();
        }
        
        // ✅ 저장된 원본 위치/회전 사용
        Vector2 targetPosition = winnerOriginalPosition;
        Vector3 targetRotation = winnerOriginalRotation;
        
        // 시작 위치: 화면 위쪽 밖 + 오른쪽으로 약간 치우침
        Vector2 startPosition = new Vector2(targetPosition.x + 300f, targetPosition.y + 1000f);
        winnerRect.anchoredPosition = startPosition;
        winnerRect.localEulerAngles = new Vector3(0f, 0f, 0f); // 시작 회전 0도
        
        // 애니메이션 시퀀스
        Sequence winnerSequence = DOTween.Sequence();
        
        // 1. 페이드 인 + 위치 이동 + 2바퀴 회전 (빙글빙글 날아오는 느낌)
        winnerSequence.Append(canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad));
        winnerSequence.Join(winnerRect.DOAnchorPos(targetPosition, 0.8f).SetEase(Ease.OutQuad));
        
        // ✅ 2바퀴 회전 (720도 = 360도 × 2)
        winnerSequence.Join(winnerRect.DORotate(new Vector3(0f, 0f, 720f), 0.8f, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
        
        // 2. 최종 회전 위치로 보정 + 스케일 증가 (탄성 효과)
        winnerSequence.Append(winnerRect.DORotate(targetRotation, 0.2f).SetEase(Ease.OutBack));
        winnerSequence.Join(_winnerBackgroundObject.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack));
        
        // 3. 살짝 튕기는 효과
        winnerSequence.Append(_winnerBackgroundObject.transform.DOScale(0.95f, 0.1f).SetEase(Ease.InQuad));
        winnerSequence.Append(_winnerBackgroundObject.transform.DOScale(1.05f, 0.1f).SetEase(Ease.OutQuad));
        winnerSequence.Append(_winnerBackgroundObject.transform.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad));
        
        yield return winnerSequence.WaitForCompletion();
    }
    
    /// <summary>
    /// 애니메이션 완료 여부 확인
    /// </summary>
    public bool IsAnimationComplete()
    {
        return isAnimationComplete;
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

#region Money UI 애니메이션

    /// <summary>
    /// Money UI 애니메이션 (로컬 플레이어의 순위에 따른 재화 표시)
    /// </summary>
    private IEnumerator AnimateMoneyUI()
    {
        if (_moneyImage == null || _moneyText == null) yield break;
        if (cachedRankings == null || cachedRankings.Count == 0) yield break;
        
        // 로컬 플레이어 찾기
        var localPlayerData = cachedRankings.FirstOrDefault(p => p.isLocalPlayer);
        if (localPlayerData == null) yield break;
        
        // 순위에 따른 재화 계산
        int playerRank = cachedRankings.IndexOf(localPlayerData) + 1;
        int moneyReward = GetMoneyRewardByRank(playerRank);
        
        // 초기 상태 설정 (비활성화)
        _moneyImage.gameObject.SetActive(false);
        _moneyText.gameObject.SetActive(false);
        _moneyImage.transform.localScale = Vector3.zero;
        _moneyText.text = "x 0";
        
        // 1등 발표가 완료될 때까지 대기 (1등 왕관 애니메이션이 끝난 후)
        yield return new WaitForSeconds(0.5f);
        
        // Money UI 활성화
        _moneyImage.gameObject.SetActive(true);
        _moneyText.gameObject.SetActive(true);
        
        // Money 이미지 초기 등장 애니메이션
        moneyImageBounceTween = _moneyImage.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        
        // 사운드 재생
        if (AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayOneShot("SFX_UI_GameOver_PopScore");
        }
        
        yield return moneyImageBounceTween.WaitForCompletion();
        
        // Money 텍스트 카운트업과 동시에 이미지 통통 튀는 애니메이션 시작
        StartCoroutine(ContinuousBounceAnimation(moneyReward));
        
        // Money 텍스트 카운트업 애니메이션 (x0 → x목표 재화)
        yield return StartCoroutine(AnimateMoneyCountUp(moneyReward));
    }
    
    /// <summary>
    /// Money 이미지 연속 바운스 애니메이션 (공이 튀듯이)
    /// </summary>
    private IEnumerator ContinuousBounceAnimation(int targetMoney)
    {
        if (_moneyImage == null) yield break;
        
        // 재화량에 따라 바운스 지속 시간 계산 (최소 1초, 최대 3초)
        float bounceDuration = Mathf.Clamp(targetMoney / 50f, 1f, 3f);
        float elapsedTime = 0f;
        float bounceInterval = 0.3f; // 각 바운스 간격
        
        while (elapsedTime < bounceDuration)
        {
            // 위로 튀어오르기
            _moneyImage.transform.DOScale(1.15f, bounceInterval * 0.5f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(bounceInterval * 0.5f);
            
            // 아래로 내려오기
            _moneyImage.transform.DOScale(1f, bounceInterval * 0.5f).SetEase(Ease.InQuad);
            
            // 바운스 사운드 재생
            if (AudioManager.Inst != null)
            {
                AudioManager.Inst.PlayOneShot("SFX_UI_GameOver_Money");
            }
            
            yield return new WaitForSeconds(bounceInterval * 0.5f);
            
            elapsedTime += bounceInterval;
        }
        
        // 최종 크기로 복원
        _moneyImage.transform.DOScale(1f, 0.2f).SetEase(Ease.InOutQuad);
    }
    
    /// <summary>
    /// Money 텍스트 카운트업 애니메이션 (x0 → x목표)
    /// </summary>
    private IEnumerator AnimateMoneyCountUp(int targetMoney)
    {
        if (_moneyText == null) yield break;
        
        float currentMoney = 0f;
        float duration = Mathf.Clamp(targetMoney / 50f, 1f, 3f); // 재화량에 따라 지속 시간 조절
        float elapsedTime = 0f;
        int lastDisplayedMoney = 0;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            currentMoney = Mathf.Lerp(0f, targetMoney, t);
            int displayMoney = Mathf.RoundToInt(currentMoney);
            
            // 재화가 변경될 때마다 UI 업데이트 (x 접두사 포함)
            if (displayMoney > lastDisplayedMoney)
            {
                lastDisplayedMoney = displayMoney;
                _moneyText.text = $"x {displayMoney}";
            }
            
            yield return null;
        }
        
        // 최종 값 설정
        _moneyText.text = $"x {targetMoney}";
    }
    
    /// <summary>
    /// 순위에 따른 재화 지급량 반환
    /// </summary>
    private int GetMoneyRewardByRank(int rank)
    {
        if (GameResultManager.Instance != null)
        {
            // GameResultManager의 설정값 사용 (Reflection으로 접근)
            var field = typeof(GameResultManager).GetField("firstPlaceMoney", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                switch (rank)
                {
                    case 1: 
                        return (int)typeof(GameResultManager).GetField("firstPlaceMoney", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(GameResultManager.Instance);
                    case 2: 
                        return (int)typeof(GameResultManager).GetField("secondPlaceMoney", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(GameResultManager.Instance);
                    case 3: 
                        return (int)typeof(GameResultManager).GetField("thirdPlaceMoney", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(GameResultManager.Instance);
                    case 4: 
                        return (int)typeof(GameResultManager).GetField("fourthPlaceMoney", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(GameResultManager.Instance);
                }
            }
        }
        
        // 기본값 (GameResultManager가 없을 경우)
        switch (rank)
        {
            case 1: return 100;
            case 2: return 50;
            case 3: return 25;
            case 4: return 10;
            default: return 0;
        }
    }

#endregion
}
