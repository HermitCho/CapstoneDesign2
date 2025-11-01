using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using Photon.Pun;

public class TutorialUI : MonoBehaviourPun
{
    public System.Action OnTutorialClosed; // 튜토리얼 패널이 사용자 클릭으로 닫힐 때 알림
    /// <summary>
    /// UI 피벗 위치 열거형 (0, 0.5, 1 중 선택)
    /// </summary>
    public enum PivotPosition
    {
        Start = 0,      // 0.0 (왼쪽 또는 아래)
        Center = 1,     // 0.5 (중앙)
        End = 2         // 1.0 (오른쪽 또는 위)
    }
    
    [Header("-------------튜토리얼 창 설정-------------")]
    [Header("튜토리얼 패널 피벗 설정 - 설명하고자 하는 오브젝트의 피벗과 동일하게 설정")]
    [Tooltip("Start=왼쪽(0), Center=중앙(0.5), End=오른쪽(1)")]
    [SerializeField] private PivotPosition tutorialPanelPivotX = PivotPosition.Center;
    [Tooltip("Start=위(1), Center=중앙(0.5), End=아래(0)")]
    [SerializeField] private PivotPosition tutorialPanelPivotY = PivotPosition.Center;
    [Space(10)]
    
    [Header("튜토리얼 메세지 텍스트")]
    [SerializeField] private string tutorialMessageText;
    [Range(0.01f, 1f)]
    [SerializeField] private float textTypingSpeed = 0.05f;
    [Space(10)]

    [Header("튜토리얼 메세지 텍스트 위치")]
    [Range(-1000f, 1000f)]
    [SerializeField] private float messageTextPositionX = 0f;
    [Range(-600f, 600f)]
    [SerializeField] private float messageTextPositionY = 0f;
    [Space(10)]

    [Header("튜토리얼 메세지 텍스트 공간 크기")]
    [Range(0f, 1000f)]
    [SerializeField] private float messageTextSpaceWidth = 400f;
    [Range(0f, 1000f)]
    [SerializeField] private float messageTextSpaceHeight = 300f;
    [Space(10)]

    [Header("튜토리얼 창 위치")]
    [Range(-1000f, 1000f)]
    [SerializeField] private float windowPositionX = 0f;
    [Range(-600f, 600f)]
    [SerializeField] private float windowPositionY = 0f;
    [Space(10)]

    [Header("튜토리얼 창 크기")]
    [Range(0f, 1000f)]
    [SerializeField] private float windowSizeWidth = 400f;
    [Range(0f, 1000f)]
    [SerializeField] private float windowSizeHeight = 300f;
    [Space(10)]

    [Header("튜토리얼 화살표 위치")]
    [Range(-1000f, 1000f)]
    [SerializeField] private float arrowPositionX = 0f;
    [Range(-600f, 600f)]
    [SerializeField] private float arrowPositionY = -150f;
    [Space(10)]

    [Header("튜토리얼 화살표 회전 각도")]
    [Range(-180f, 180f)]
    [SerializeField] private float arrowRotationAngleY = 0f;
    [Range(-180f, 180f)]
    [SerializeField] private float arrowRotationAngleZ = 0f;
    [Space(10)]

    [Header("-------------튜토리얼 상시 창 설정-------------")]
    [Header("튜토리얼 상시 창 메세지 텍스트")]
    [SerializeField] private string tutorialAlwaysPlayMessageText;
    [Range(0.01f, 1f)]
    [SerializeField] private float tutorialAlwaysPlayMessageTextTypingSpeed = 0.05f;
    [Space(10)]

    [Header("튜토리얼 상시 창 메세지 텍스트 공간 크기 - 기본값 유지 권장")]
    [Range(0f, 1000f)]
    [SerializeField] private float alwaysPanelMessageTextSpaceWidth = 430f;
    [Range(0f, 1000f)]
    [SerializeField] private float alwaysPanelMessageTextSpaceHeight = 140f;
    [Space(10)]

    [Header("튜토리얼 상시 창 메세지 텍스트 위치 - 기본값 유지 권장")]
    [Range(-1000f, 1000f)]
    [SerializeField] private float alwaysPanelMessageTextPositionX = 880f;
    [Range(-600f, 600f)]
    [SerializeField] private float alwaysPanelMessageTextPositionY = 500f;
    [Space(10)]


    [Header("튜토리얼 상시 창 위치 - 기본값 유지 권장")]
    [Range(-1000f, 1000f)]
    [SerializeField] private float alwaysPanelPositionX = 900f;
    [Range(-600f, 600f)]
    [SerializeField] private float alwaysPanelPositionY = 500f;
    [Space(10)]

    [Header("튜토리얼 상시 창 크기 -기본값 유지 권장")]
    [Range(0f, 1000f)]
    [SerializeField] private float alwaysPanelSizeWidth = 500f;
    [Range(0f, 1000f)]
    [SerializeField] private float alwaysPanelSizeHeight = 150f;
    [Space(10)]

    [Header("튜토리얼 상시 창 완료 이미지 위치 - 기본값 유지 권장")]
    [Range(-1000f, 1000f)]
    [SerializeField] private float completeStickerPositionX = 1000f;
    [Range(-600f, 600f)]
    [SerializeField] private float completeStickerPositionY = 500f;
    [Space(10)]


    [Header("-------------튜토리얼 패널 설정-------------")]
    [Header("튜토리얼 패널 할당")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Image tutorialWindowImage;
    [SerializeField] private Image tutorialArrowImage;
    [SerializeField] private TextMeshProUGUI tutorialMessageTextUI;
    [SerializeField] private TextMeshProUGUI tutorialSkipMessageTextUI;
    [Space(10)]

    [Header("튜토리얼 상시 재생 패널 할당")]
    [SerializeField] private Image tutorialAlwaysPlayPanelImage;
    [SerializeField] private TextMeshProUGUI tutorialAlwaysPlayMessageTextUI;
    [SerializeField] private Image tutorialCompleteStickerImage;
    [Space(10)]

    [Header("-------------애니메이션 설정-------------")]
    [Header("애니메이션 설정")]
    [Range(0.1f, 2f)]
    [SerializeField] private float windowAnimDuration = 0.5f;
    [Range(0.1f, 2f)]
    [SerializeField] private float arrowFadeDuration = 0.3f;
    [Range(0.1f, 2f)]
    [SerializeField] private float skipBlinkDuration = 0.8f;
    [Range(0.1f, 2f)]
    [SerializeField] private float stickerAnimDuration = 0.4f;

    private static TutorialUI currentActiveTutorial;
    private static HashSet<string> completedTutorials = new HashSet<string>();
    
    private bool isActive = false;
    private bool isMessageComplete = false;
    private bool canSkip = false;
    
    private RectTransform tutorialPanelRect;
    private RectTransform windowRect;
    private RectTransform arrowRect;
    private RectTransform alwaysPanelRect;
    private RectTransform messageTextRect;
    private RectTransform alwaysPanelMessageTextRect;
    private CanvasGroup windowCanvasGroup;
    private CanvasGroup arrowCanvasGroup;
    private CanvasGroup alwaysPanelCanvasGroup;
    
    private MoveController playerMoveController;
    private CameraController playerCameraController;
    private TestShoot playerTestShoot;
    private TestMoveAnimationController playerAnimationController;
    
    private Sequence windowSequence;
    private Sequence arrowSequence;
    private Sequence skipBlinkSequence;
    private Sequence stickerSequence;
    private Sequence completeStickerSequence;
    private Coroutine typingCoroutine;
    private Coroutine alwaysPlayTypingCoroutine;
    
    private bool isCompleteStickerShown = false;
    
    private string tutorialId;

    void Awake()
    {
        tutorialId = $"{gameObject.name}_{transform.position}";
        
        InitializeComponents();
        InitializeUI();
    }

    void OnDestroy()
    {
        CleanupAnimations();
    }

    private void InitializeComponents()
    {
        // Tutorial Panel 피벗 및 앵커 설정
        if (tutorialPanel != null)
        {
            tutorialPanelRect = tutorialPanel.GetComponent<RectTransform>();
            if (tutorialPanelRect != null)
            {
                // Enum 값을 float로 변환 (X축: Start=0, Y축: Start=1)
                float pivotX = ConvertPivotPositionToFloat(tutorialPanelPivotX, false); // X축
                float pivotY = ConvertPivotPositionToFloat(tutorialPanelPivotY, true);  // Y축
                
                // 피벗 설정
                tutorialPanelRect.pivot = new Vector2(pivotX, pivotY);
                
                // 앵커 설정 (피벗과 동일하게 설정하여 해상도 대응)
                tutorialPanelRect.anchorMin = new Vector2(pivotX, pivotY);
                tutorialPanelRect.anchorMax = new Vector2(pivotX, pivotY);
            }
        }
        
        if (tutorialWindowImage != null)
        {
            windowRect = tutorialWindowImage.GetComponent<RectTransform>();
            windowCanvasGroup = tutorialWindowImage.GetComponent<CanvasGroup>();
            if (windowCanvasGroup == null)
            {
                windowCanvasGroup = tutorialWindowImage.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (tutorialArrowImage != null)
        {
            arrowRect = tutorialArrowImage.GetComponent<RectTransform>();
            arrowCanvasGroup = tutorialArrowImage.GetComponent<CanvasGroup>();
            if (arrowCanvasGroup == null)
            {
                arrowCanvasGroup = tutorialArrowImage.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (tutorialAlwaysPlayPanelImage != null)
        {
            alwaysPanelRect = tutorialAlwaysPlayPanelImage.GetComponent<RectTransform>();
            alwaysPanelCanvasGroup = tutorialAlwaysPlayPanelImage.GetComponent<CanvasGroup>();
            if (alwaysPanelCanvasGroup == null)
            {
                alwaysPanelCanvasGroup = tutorialAlwaysPlayPanelImage.gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        if (tutorialMessageTextUI != null)
        {
            messageTextRect = tutorialMessageTextUI.GetComponent<RectTransform>();
        }
        
        if (tutorialAlwaysPlayMessageTextUI != null)
        {
            alwaysPanelMessageTextRect = tutorialAlwaysPlayMessageTextUI.GetComponent<RectTransform>();
        }
    }

    private void InitializeUI()
    {
        // if (tutorialWindowImage != null)
        // {
        //     tutorialWindowImage.gameObject.SetActive(false);
        //     windowCanvasGroup.alpha = 0f;
        // }

        if (tutorialArrowImage != null)
        {
            tutorialArrowImage.gameObject.SetActive(false);
            arrowCanvasGroup.alpha = 0f;
        }

        if (tutorialMessageTextUI != null)
        {
            tutorialMessageTextUI.text = "";
            tutorialMessageTextUI.gameObject.SetActive(false);
        }

        if (tutorialSkipMessageTextUI != null)
        {
            tutorialSkipMessageTextUI.gameObject.SetActive(false);
        }

        if (tutorialAlwaysPlayPanelImage != null)
        {
            tutorialAlwaysPlayPanelImage.gameObject.SetActive(false);
            alwaysPanelCanvasGroup.alpha = 0f;
            alwaysPanelCanvasGroup.transform.localScale = Vector3.zero;
        }
        
        if (tutorialAlwaysPlayMessageTextUI != null)
        {
            tutorialAlwaysPlayMessageTextUI.text = "";
            tutorialAlwaysPlayMessageTextUI.gameObject.SetActive(false);
        }
        
        if (tutorialCompleteStickerImage != null)
        {
            tutorialCompleteStickerImage.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (isActive && canSkip && Input.GetMouseButtonDown(0))
        {
            CloseTutorial();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        PhotonView playerPhotonView = other.GetComponentInParent<PhotonView>();
        if (playerPhotonView == null || !playerPhotonView.IsMine) return;

        if (completedTutorials.Contains(tutorialId)) return;

        if (currentActiveTutorial != null && currentActiveTutorial != this) return;

        InitializeComponents();

        StartTutorial(other);
    }

    private void StartTutorial(Collider playerCollider)
    {
        if (isActive) return;

        isActive = true;
        currentActiveTutorial = this;
        completedTutorials.Add(tutorialId);

        FindPlayerComponents(playerCollider);
        DisablePlayerControls();
        HidePreviousAlwaysPanel();

        StartCoroutine(TutorialSequence());
    }

    private void FindPlayerComponents(Collider playerCollider)
    {
        Transform playerRoot = playerCollider.transform.root;

        playerMoveController = playerRoot.GetComponent<MoveController>();
        if (playerMoveController == null)
        {
            playerMoveController = playerRoot.GetComponentInChildren<MoveController>();
        }

        playerCameraController = playerRoot.GetComponent<CameraController>();
        if (playerCameraController == null)
        {
            playerCameraController = playerRoot.GetComponentInChildren<CameraController>();
        }

        playerTestShoot = playerRoot.GetComponent<TestShoot>();
        if (playerTestShoot == null)
        {
            playerTestShoot = playerRoot.GetComponentInChildren<TestShoot>();
        }
        
        playerAnimationController = playerRoot.GetComponent<TestMoveAnimationController>();
        if (playerAnimationController == null)
        {
            playerAnimationController = playerRoot.GetComponentInChildren<TestMoveAnimationController>();
        }
    }

    private void DisablePlayerControls()
    {
        if (playerMoveController != null)
        {
            playerMoveController.DisableMoveControls();
        }

        if (playerCameraController != null)
        {
            playerCameraController.DisableCameraControl();
        }

        if (playerTestShoot != null)
        {
            TestShoot.SetIsShooting(false);
        }

        // ✅ Q스킬 입력 차단용 전역 잠금
        SkillController.IsSkillLocked = true;
        
        // 애니메이션 정지
        if (playerAnimationController != null)
        {
            playerAnimationController.StopAllAnimations();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void EnablePlayerControls()
    {
        if (playerMoveController != null)
        {
            playerMoveController.EnableMoveControls();
        }

        if (playerCameraController != null)
        {
            playerCameraController.EnableCameraControl();
        }

        if (playerTestShoot != null)
        {
            TestShoot.SetIsShooting(true);
        }

        // ✅ Q스킬 잠금 해제
        SkillController.IsSkillLocked = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HidePreviousAlwaysPanel()
    {
        TutorialUI[] allTutorials = FindObjectsOfType<TutorialUI>();
        foreach (TutorialUI tutorial in allTutorials)
        {
            if (tutorial != this && tutorial.tutorialAlwaysPlayPanelImage != null)
            {
                tutorial.tutorialAlwaysPlayPanelImage.gameObject.SetActive(false);
                
                if (tutorial.alwaysPlayTypingCoroutine != null)
                {
                    tutorial.StopCoroutine(tutorial.alwaysPlayTypingCoroutine);
                    tutorial.alwaysPlayTypingCoroutine = null;
                }

                if (tutorial.tutorialAlwaysPlayMessageTextUI != null)
                {
                    tutorial.tutorialAlwaysPlayMessageTextUI.text = "";
                    tutorial.tutorialAlwaysPlayMessageTextUI.gameObject.SetActive(false);
                }
            }
            
            // Complete Sticker도 숨김
            if (tutorial != this && tutorial.tutorialCompleteStickerImage != null)
            {
                tutorial.tutorialCompleteStickerImage.gameObject.SetActive(false);
                tutorial.isCompleteStickerShown = false;
            }
        }
    }

    private IEnumerator TutorialSequence()
    {
        yield return StartCoroutine(AnimateWindow());
        
        yield return StartCoroutine(AnimateArrow());
        
        yield return StartCoroutine(TypeMessage());
        
        StartSkipBlink();
    }

    private IEnumerator AnimateWindow()
    {
        if (tutorialWindowImage == null || windowRect == null) yield break;

        // tutorialWindowImage.gameObject.SetActive(true);

        // Vector2 startSize = new Vector2(windowSizeWidth * 2f, windowSizeHeight * 2f);
        // Vector2 targetSize = new Vector2(windowSizeWidth, windowSizeHeight);
        // Vector2 targetPosition = new Vector2(windowPositionX, windowPositionY);

        // windowRect.sizeDelta = startSize;
        // windowRect.anchoredPosition = targetPosition;
        // windowCanvasGroup.alpha = 0f;

        // windowSequence = DOTween.Sequence();
        // windowSequence.Append(windowCanvasGroup.DOFade(1f, windowAnimDuration * 0.3f).SetEase(Ease.OutQuad));
        // windowSequence.Join(windowRect.DOSizeDelta(targetSize, windowAnimDuration).SetEase(Ease.OutBack));

        // yield return windowSequence.WaitForCompletion();
    }

    private IEnumerator AnimateArrow()
    {
        if (tutorialArrowImage == null || arrowRect == null) yield break;

        tutorialArrowImage.gameObject.SetActive(true);

        Vector2 targetPosition = new Vector2(arrowPositionX, arrowPositionY);
        arrowRect.anchoredPosition = targetPosition;
        
        // 화살표 회전 각도 설정
        Vector3 rotation = arrowRect.localEulerAngles;
        rotation.y = arrowRotationAngleY;
        rotation.z = arrowRotationAngleZ;
        arrowRect.localEulerAngles = rotation;
        
        arrowCanvasGroup.alpha = 0f;

        arrowSequence = DOTween.Sequence();
        arrowSequence.Append(arrowCanvasGroup.DOFade(1f, arrowFadeDuration).SetEase(Ease.OutQuad));

        yield return arrowSequence.WaitForCompletion();
    }

    private IEnumerator TypeMessage()
    {
        if (tutorialMessageTextUI == null || messageTextRect == null) yield break;

        tutorialMessageTextUI.gameObject.SetActive(true);
        
        // 메시지 텍스트 위치 설정
        Vector2 messageTextPosition = new Vector2(messageTextPositionX, messageTextPositionY);
        messageTextRect.anchoredPosition = messageTextPosition;
        
        // 메시지 텍스트 공간 크기 설정
        Vector2 messageTextSize = new Vector2(messageTextSpaceWidth, messageTextSpaceHeight);
        messageTextRect.sizeDelta = messageTextSize;
        
        tutorialMessageTextUI.text = "";

        for (int i = 0; i <= tutorialMessageText.Length; i++)
        {
            tutorialMessageTextUI.text = tutorialMessageText.Substring(0, i);
            yield return new WaitForSeconds(textTypingSpeed);
        }

        isMessageComplete = true;
    }

    private void StartSkipBlink()
    {
        if (tutorialSkipMessageTextUI == null) return;

        tutorialSkipMessageTextUI.gameObject.SetActive(true);
        canSkip = true;

        skipBlinkSequence = DOTween.Sequence();
        skipBlinkSequence.Append(tutorialSkipMessageTextUI.DOFade(0.3f, skipBlinkDuration).SetEase(Ease.InOutQuad));
        skipBlinkSequence.Append(tutorialSkipMessageTextUI.DOFade(1f, skipBlinkDuration).SetEase(Ease.InOutQuad));
        skipBlinkSequence.SetLoops(-1, LoopType.Restart);
    }

    private void CloseTutorial()
    {
        if (!isActive) return;

        CleanupAnimations();
        
        // if (tutorialWindowImage != null)
        // {
        //     tutorialWindowImage.gameObject.SetActive(false);
        // }
        
        if (tutorialArrowImage != null)
        {
            tutorialArrowImage.gameObject.SetActive(false);
        }
        
        if (tutorialMessageTextUI != null)
        {
            tutorialMessageTextUI.gameObject.SetActive(false);
        }
        
        if (tutorialSkipMessageTextUI != null)
        {
            tutorialSkipMessageTextUI.gameObject.SetActive(false);
        }

        EnablePlayerControls();

        if (tutorialAlwaysPlayPanelImage != null && tutorialAlwaysPlayMessageTextUI != null)
        {
            ShowAlwaysPanel();
        }

        isActive = false;
        canSkip = false;
        currentActiveTutorial = null;

        // 외부 시스템 알림 (코인 튜토리얼 등)
        OnTutorialClosed?.Invoke();
    }

    private void ShowAlwaysPanel()
    {
        // ✅ Always Panel이 null이면 텍스트도 출력 안함
        if (tutorialAlwaysPlayPanelImage == null || alwaysPanelRect == null) 
        {
            return;
        }

        tutorialAlwaysPlayPanelImage.gameObject.SetActive(true);
        
        // Always Panel 위치 설정
        Vector2 targetPosition = new Vector2(alwaysPanelPositionX, alwaysPanelPositionY);
        alwaysPanelRect.anchoredPosition = targetPosition;
        
        // Always Panel 크기 설정
        Vector2 targetSize = new Vector2(alwaysPanelSizeWidth, alwaysPanelSizeHeight);
        alwaysPanelRect.sizeDelta = targetSize;
        
        // 초기 상태 설정
        alwaysPanelCanvasGroup.alpha = 0f;
        alwaysPanelCanvasGroup.transform.localScale = Vector3.zero;

        // 스티커 애니메이션
        stickerSequence = DOTween.Sequence();
        stickerSequence.Append(alwaysPanelCanvasGroup.transform.DOScale(1f, stickerAnimDuration).SetEase(Ease.OutBack));
        stickerSequence.Join(alwaysPanelCanvasGroup.DOFade(1f, stickerAnimDuration * 0.5f).SetEase(Ease.OutQuad));
        stickerSequence.OnComplete(() => 
        {
            // ✅ 텍스트가 있을 때만 타이핑 시작
            if (!string.IsNullOrEmpty(tutorialAlwaysPlayMessageText) && tutorialAlwaysPlayMessageTextUI != null)
            {
                tutorialAlwaysPlayMessageTextUI.gameObject.SetActive(true);
                alwaysPlayTypingCoroutine = StartCoroutine(TypeAlwaysPlayMessage());
            }
        });
    }
    
    private IEnumerator TypeAlwaysPlayMessage()
    {
        if (tutorialAlwaysPlayMessageTextUI == null || alwaysPanelMessageTextRect == null) yield break;
        
        // Always Panel 메시지 텍스트 위치 설정
        Vector2 alwaysPanelMessageTextPosition = new Vector2(alwaysPanelMessageTextPositionX, alwaysPanelMessageTextPositionY);
        alwaysPanelMessageTextRect.anchoredPosition = alwaysPanelMessageTextPosition;
        
        // Always Panel 메시지 텍스트 공간 크기 설정
        Vector2 alwaysPanelMessageTextSize = new Vector2(alwaysPanelMessageTextSpaceWidth, alwaysPanelMessageTextSpaceHeight);
        alwaysPanelMessageTextRect.sizeDelta = alwaysPanelMessageTextSize;
        
        tutorialAlwaysPlayMessageTextUI.text = "";
        
        for (int i = 0; i <= tutorialAlwaysPlayMessageText.Length; i++)
        {
            tutorialAlwaysPlayMessageTextUI.text = tutorialAlwaysPlayMessageText.Substring(0, i);
            yield return new WaitForSeconds(tutorialAlwaysPlayMessageTextTypingSpeed);
        }
    }

    private void CleanupAnimations()
    {
        windowSequence?.Kill();
        arrowSequence?.Kill();
        skipBlinkSequence?.Kill();
        stickerSequence?.Kill();
        completeStickerSequence?.Kill();

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        if (alwaysPlayTypingCoroutine != null)
        {
            StopCoroutine(alwaysPlayTypingCoroutine);
            alwaysPlayTypingCoroutine = null;
        }
    }
    
    /// <summary>
    /// 외부에서 호출 가능한 Complete Sticker 표시 함수
    /// 특정 행동 완료 시 호출하면 Always Panel 위에 완료 스티커가 붙습니다
    /// </summary>
    public void ShowCompleteSticker()
    {
        // Always Panel이 활성화되어 있고 Complete Sticker가 아직 표시되지 않았을 때만
        if (tutorialCompleteStickerImage == null) return;
        if (tutorialAlwaysPlayPanelImage == null || !tutorialAlwaysPlayPanelImage.gameObject.activeSelf) return;
        if (isCompleteStickerShown) return;
        
        isCompleteStickerShown = true;
        tutorialCompleteStickerImage.gameObject.SetActive(true);
        
        // 초기 상태 설정
        RectTransform completeStickerRect = tutorialCompleteStickerImage.GetComponent<RectTransform>();
        CanvasGroup completeStickerCanvasGroup = tutorialCompleteStickerImage.GetComponent<CanvasGroup>();
        
        if (completeStickerCanvasGroup == null)
        {
            completeStickerCanvasGroup = tutorialCompleteStickerImage.gameObject.AddComponent<CanvasGroup>();
        }
        
        // Complete Sticker 위치 설정
        Vector2 completeStickerPosition = new Vector2(completeStickerPositionX, completeStickerPositionY);
        completeStickerRect.anchoredPosition = completeStickerPosition;
        
        completeStickerCanvasGroup.alpha = 0f;
        completeStickerRect.localScale = Vector3.zero;

        // Complete Sticker 애니메이션 (스티커가 붙는 감각 강화)
        completeStickerSequence = DOTween.Sequence();
        // 약간 회전 진동과 팝 효과
        completeStickerSequence.Append(completeStickerRect.DOScale(1.15f, stickerAnimDuration * 0.45f).SetEase(Ease.OutBack));
        completeStickerSequence.Join(completeStickerCanvasGroup.DOFade(1f, stickerAnimDuration * 0.35f).SetEase(Ease.OutQuad));
        completeStickerSequence.Join(completeStickerRect.DORotate(new Vector3(0f, 0f, 6f), stickerAnimDuration * 0.22f).SetEase(Ease.OutQuad));
        completeStickerSequence.Append(completeStickerRect.DORotate(Vector3.zero, stickerAnimDuration * 0.18f).SetEase(Ease.InOutQuad));
        completeStickerSequence.Append(completeStickerRect.DOScale(1f, stickerAnimDuration * 0.25f).SetEase(Ease.InOutQuad));
    }

    public static void ResetAllTutorials()
    {
        completedTutorials.Clear();
        currentActiveTutorial = null;
    }
    
    /// <summary>
    /// PivotPosition Enum을 float 값으로 변환
    /// </summary>
    /// <param name="position">변환할 PivotPosition</param>
    /// <param name="isYAxis">Y축인지 여부 (Y축은 Start=1, End=0)</param>
    /// <returns>0.0, 0.5, 또는 1.0</returns>
    private float ConvertPivotPositionToFloat(PivotPosition position, bool isYAxis = false)
    {
        switch (position)
        {
            case PivotPosition.Start:
                return isYAxis ? 1f : 0f;  // Y축: Start=1(위), X축: Start=0(왼쪽)
            case PivotPosition.Center:
                return 0.5f;               // 중앙은 항상 0.5
            case PivotPosition.End:
                return isYAxis ? 0f : 1f;  // Y축: End=0(아래), X축: End=1(오른쪽)
            default:
                return 0.5f; // 기본값: 중앙
        }
    }
}
