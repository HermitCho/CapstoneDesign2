using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TitleAnimation : MonoBehaviour, IPointerClickHandler
{
    [Header("타이틀 이미지")]
    [SerializeField] private Image titleImage;
    
    [Header("등장 애니메이션 설정")]
    [SerializeField] private float appearDelay = 1.3f;
    [SerializeField] private float appearDuration = 0.5f;
    [SerializeField] private float appearScale = 1.2f;
    
    [Header("호흡 애니메이션 설정 (자동 반복)")]
    [SerializeField] private float breatheScale = 1.05f;
    [SerializeField] private float breatheDuration = 2f;
    
    [Header("클릭 애니메이션 설정")]
    [SerializeField] private float shakeStrength = 20f;
    [SerializeField] private int shakeVibrato = 10;
    [SerializeField] private float shakeDuration = 0.5f;
    
    private Vector3 originalScale;
    private Tween breatheTween;
    private Tween shakeTween;
    private Tween appearTween;
    private bool hasAppeared = false;
    
    void Awake()
    {
        // ✅ EventSystem 확인 및 생성 (UI 이벤트 작동을 위해 필수)
        EnsureSingleEventSystem();
        
        // ✅ Canvas와 Graphic Raycaster 확인
        EnsureGraphicRaycaster();
    }
    
    void Start()
    {
        // 타이틀 이미지가 할당되지 않았으면 자동으로 찾기
        if (titleImage == null)
        {
            titleImage = GetComponent<Image>();
        }
        
        if (titleImage != null)
        {
            // 원본 스케일 저장
            originalScale = titleImage.transform.localScale;
            
            // ✅ Raycast Target 확인
            if (!titleImage.raycastTarget)
            {
                titleImage.raycastTarget = true;
            }
            
            // ✅ Canvas Sort Order 자동 조정
            EnsureCanvasSortOrder();
            
            // ✅ 타이틀 초기 상태 설정 (투명 + Scale 0)
            titleImage.transform.localScale = Vector3.zero;
            Color initialColor = titleImage.color;
            initialColor.a = 0f;
            titleImage.color = initialColor;
            
            // ✅ 등장 애니메이션 시작 (Invoke 사용)
            Invoke(nameof(PlayAppearAnimation), appearDelay);
        }
    }
    
    /// <summary>
    /// 타이틀 등장 애니메이션 (최초 1회만)
    /// </summary>
    private void PlayAppearAnimation()
    {
        if (hasAppeared) return;
        
        hasAppeared = true;
        
        // 등장 사운드 재생
        if (AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayOneShot("SFX_UI_TitleClick2");
        }
        
        // 뿅 나타나는 애니메이션 (0 → 1.2 → 1.0)
        Sequence appearSequence = DOTween.Sequence();
        
        // 페이드 인 (투명 → 불투명)
        appearSequence.Append(titleImage.DOFade(1f, appearDuration * 0.3f)
            .SetEase(Ease.OutQuad));
        
        // 1단계: 빠르게 확대 (0 → 1.2)
        appearSequence.Join(titleImage.transform.DOScale(originalScale * appearScale, appearDuration * 0.6f)
            .SetEase(Ease.OutBack));
        
        // 2단계: 살짝 축소 (1.2 → 1.0)
        appearSequence.Append(titleImage.transform.DOScale(originalScale, appearDuration * 0.4f)
            .SetEase(Ease.InOutQuad));
        
        // ✅ 등장 애니메이션 완료 후 호흡 애니메이션 시작
        appearSequence.OnComplete(() => StartBreatheAnimation());
        
        appearTween = appearSequence;
    }
    
    /// <summary>
    /// 호흡 애니메이션 (자동으로 커졌다 작아졌다 반복)
    /// </summary>
    private void StartBreatheAnimation()
    {
        if (titleImage == null) return;
        
        // 기존 호흡 애니메이션 정리
        breatheTween?.Kill();
        
        // 부드럽게 커졌다 작아졌다 (1.0 ↔ 1.05) 무한 반복
        breatheTween = titleImage.transform.DOScale(originalScale * breatheScale, breatheDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo); // 무한 반복 + 왕복
    }
    
    /// <summary>
    /// Title Canvas의 Sort Order를 다른 Canvas보다 높게 설정
    /// </summary>
    private void EnsureCanvasSortOrder()
    {
        Canvas titleCanvas = GetComponentInParent<Canvas>();
        if (titleCanvas == null) return;
        
        // 모든 Canvas 찾기
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        
        int maxSortOrder = 0;
        
        foreach (Canvas canvas in allCanvases)
        {
            // 최대 Sort Order 찾기
            if (canvas != titleCanvas && canvas.sortingOrder > maxSortOrder)
            {
                maxSortOrder = canvas.sortingOrder;
            }
        }
        
        // Title Canvas를 최상위로 설정
        int newSortOrder = maxSortOrder + 10;
        
        if (titleCanvas.sortingOrder < newSortOrder)
        {
            titleCanvas.sortingOrder = newSortOrder;
        }
    }
    
    /// <summary>
    /// EventSystem이 씬에 1개만 존재하도록 보장
    /// </summary>
    private void EnsureSingleEventSystem()
    {
        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();
        
        if (eventSystems.Length == 0)
        {
            // EventSystem이 없으면 생성
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
        else if (eventSystems.Length > 1)
        {
            // EventSystem이 여러 개면 첫 번째만 유지하고 나머지 제거
            for (int i = 1; i < eventSystems.Length; i++)
            {
                if (eventSystems[i] != null)
                {
                    Destroy(eventSystems[i].gameObject);
                }
            }
        }
    }
    
    /// <summary>
    /// Canvas에 Graphic Raycaster가 있는지 확인
    /// </summary>
    private void EnsureGraphicRaycaster()
    {
        // 부모 Canvas 찾기
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;
        
        // Graphic Raycaster 확인
        GraphicRaycaster raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            // 없으면 추가
            raycaster = parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }
    
    void OnDestroy()
    {
        // 애니메이션 정리
        CleanupAnimations();
    }
    
    /// <summary>
    /// 마우스 왼쪽 클릭 시
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (titleImage == null) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        
        // 랜덤 클릭 사운드 재생
        PlayRandomClickSound();
        
        // 기존 흔들림 애니메이션 중지
        shakeTween?.Kill();
        
        // 좌우로 흔들리는 애니메이션
        shakeTween = titleImage.transform.DOShakeRotation(shakeDuration, new Vector3(0, 0, shakeStrength), shakeVibrato)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // 애니메이션 완료 후 회전 초기화
                titleImage.transform.localRotation = Quaternion.identity;
            });
    }
    
    /// <summary>
    /// 랜덤 클릭 사운드 재생
    /// </summary>
    private void PlayRandomClickSound()
    {
        if (AudioManager.Inst == null) return;
        
        string[] clickSounds = { "SFX_UI_TitleClick1", "SFX_UI_TitleClick2", "SFX_UI_TitleClick3" };
        string randomSound = clickSounds[Random.Range(0, clickSounds.Length)];
        
        AudioManager.Inst.PlayOneShot(randomSound);
    }
    
    /// <summary>
    /// 애니메이션 정리
    /// </summary>
    private void CleanupAnimations()
    {
        appearTween?.Kill();
        breatheTween?.Kill();
        shakeTween?.Kill();
        appearTween = null;
        breatheTween = null;
        shakeTween = null;
    }
}
