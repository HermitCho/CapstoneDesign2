using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class LoadingManager : MonoBehaviour
{
    [Header("3D Animation Settings")]
    [SerializeField] private GameObject[] animationObjects;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float animationDuration = 3f;
    [SerializeField] private AnimationCurve cameraMoveCurve;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI pressAnyKeyText;
    [SerializeField] private CanvasGroup uiCanvasGroup;
    [SerializeField] private Image fadeOverlay;
    
    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName = "Lobby";
    [SerializeField] private float fadeTransitionTime = 1f;
    [SerializeField] private bool allowSkipAnimation = true;
    
    [Header("Audio")]
    [SerializeField] private LoadingAudioManager audioManager;
    
    [Header("Animation Controllers")]
    [SerializeField] private LoadingAnimationController animationController;
    [SerializeField] private LoadingCinematicController cinematicController;
    
    [Header("Cinematic Settings")]
    [SerializeField] private bool useCinematicMode = true;
    [SerializeField] private float cinematicDelay = 1f;
    
    private bool isAnimationComplete = false;
    private bool isLoadingNextScene = false;
    private bool canSkip = false;
    private bool isCinematicPlaying = false;
    
    void Start()
    {
        StartCoroutine(InitializeLoadingSequence());
    }
    
    void Update()
    {
        // 시네마틱 모드에서는 시네마틱이 끝난 후에만 입력 허용
        if (useCinematicMode && cinematicController != null)
        {
            if (cinematicController.IsPlayingCinematic())
            {
                isCinematicPlaying = true;
                return;
            }
            else if (isCinematicPlaying)
            {
                // 시네마틱이 방금 끝남
                isCinematicPlaying = false;
                isAnimationComplete = true;
                ShowPressAnyKeyText();
            }
        }
        
        // 애니메이션 완료 후 또는 스킵 가능 상태에서 입력 감지
        if ((isAnimationComplete || (allowSkipAnimation && canSkip)) && !isLoadingNextScene)
        {
            if (Input.anyKeyDown)
            {
                StartLoadingNextScene();
            }
        }
    }
    
    IEnumerator InitializeLoadingSequence()
    {
        // 초기 설정
        SetupInitialState();
        
        // 페이드 인
        yield return StartCoroutine(FadeIn());
        
        if (useCinematicMode && cinematicController != null)
        {
            // 시네마틱 모드
            yield return new WaitForSeconds(cinematicDelay);
            cinematicController.StartCinematic();
        }
        else
        {
            // 일반 3D 애니메이션 모드
            StartCoroutine(Start3DAnimation());
            
            // 스킵 가능 상태 활성화 (애니메이션 시작 1초 후)
            yield return new WaitForSeconds(1f);
            canSkip = true;
            
            // UI 텍스트 표시
            ShowPressAnyKeyText();
            
            // 애니메이션 완료 대기
            yield return new WaitForSeconds(animationDuration - 1f);
            isAnimationComplete = true;
        }
        
        // 자동 전환 (선택사항)
        if (!isLoadingNextScene && !useCinematicMode)
        {
            // 5초 후 자동으로 다음 씬으로 이동 (일반 모드에서만)
            yield return new WaitForSeconds(5f);
            if (!isLoadingNextScene)
            {
                StartLoadingNextScene();
            }
        }
    }
    
    void SetupInitialState()
    {
        // UI 초기 상태 설정
        if (uiCanvasGroup) uiCanvasGroup.alpha = 0f;
        if (pressAnyKeyText) pressAnyKeyText.alpha = 0f;
        if (fadeOverlay) fadeOverlay.color = Color.black;
        
        // 3D 오브젝트 초기 상태 설정
        foreach (var obj in animationObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        
        // 애니메이션 컨트롤러 시작 (일반 모드에서만)
        if (!useCinematicMode && animationController != null)
        {
            animationController.StartLoadingAnimation();
        }
        
        // 오디오 매니저 초기화
        if (audioManager != null)
        {
            audioManager.PlayLogoAppearSFX();
        }
    }
    
    IEnumerator FadeIn()
    {
        if (fadeOverlay)
        {
            fadeOverlay.DOFade(0f, fadeTransitionTime);
            yield return new WaitForSeconds(fadeTransitionTime);
        }
    }
    
    IEnumerator Start3DAnimation()
    {
        // 카메라 애니메이션 (일반 모드에서만)
        if (mainCamera && !useCinematicMode)
        {
            Vector3 startPos = mainCamera.transform.position;
            Vector3 endPos = startPos + Vector3.forward * 5f;
            
            mainCamera.transform.DOMove(endPos, animationDuration)
                .SetEase(cameraMoveCurve);
            
            mainCamera.transform.DORotate(new Vector3(0, 360, 0), animationDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.InOutQuad);
        }
        
        // 3D 오브젝트들 애니메이션 (일반 모드에서만)
        if (!useCinematicMode)
        {
            for (int i = 0; i < animationObjects.Length; i++)
            {
                if (animationObjects[i] != null)
                {
                    var obj = animationObjects[i];
                    float delay = i * 0.2f;
                    
                    // 회전 애니메이션
                    obj.transform.DORotate(new Vector3(0, 360, 0), animationDuration, RotateMode.FastBeyond360)
                        .SetDelay(delay)
                        .SetEase(Ease.InOutQuad);
                    
                    // 스케일 애니메이션
                    obj.transform.DOScale(Vector3.one * 1.2f, animationDuration * 0.5f)
                        .SetDelay(delay)
                        .SetLoops(2, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine);
                }
            }
        }
        
        yield return new WaitForSeconds(animationDuration);
    }
    
    void ShowPressAnyKeyText()
    {
        if (pressAnyKeyText)
        {
            pressAnyKeyText.DOFade(1f, 0.5f);
            
            // 깜빡이는 효과
            pressAnyKeyText.DOFade(0.3f, 1f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        
        if (uiCanvasGroup)
        {
            uiCanvasGroup.DOFade(1f, 0.5f);
        }
    }
    
    void StartLoadingNextScene()
    {
        if (isLoadingNextScene) return;
        
        isLoadingNextScene = true;
        StartCoroutine(LoadNextScene());
    }
    
    IEnumerator LoadNextScene()
    {
        // 버튼 클릭 사운드
        if (audioManager != null)
        {
            audioManager.PlayButtonClickSFX();
        }
        
        // 시네마틱 중지
        if (useCinematicMode && cinematicController != null)
        {
            cinematicController.StopCinematic();
        }
        
        // 페이드 아웃
        if (fadeOverlay)
        {
            fadeOverlay.DOFade(1f, fadeTransitionTime);
        }
        
        // 오디오 페이드 아웃
        if (audioManager != null)
        {
            audioManager.FadeOutAllAudio(fadeTransitionTime);
        }
        
        // 애니메이션 중지
        if (animationController != null)
        {
            animationController.StopLoadingAnimation();
        }
        
        yield return new WaitForSeconds(fadeTransitionTime);
        
        // 씬 로드
        SceneManager.LoadScene(targetSceneName);
    }
    
    // 외부에서 호출 가능한 메서드
    public void LoadScene(string sceneName)
    {
        targetSceneName = sceneName;
        StartLoadingNextScene();
    }
    
    public void SkipAnimation()
    {
        if (allowSkipAnimation && canSkip)
        {
            StartLoadingNextScene();
        }
    }
    
    public void SetCinematicMode(bool enable)
    {
        useCinematicMode = enable;
    }
    
    public void StartCinematic()
    {
        if (cinematicController != null)
        {
            cinematicController.StartCinematic();
        }
    }
    
    public void StopCinematic()
    {
        if (cinematicController != null)
        {
            cinematicController.StopCinematic();
        }
    }
}
