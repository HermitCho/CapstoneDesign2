using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 페이드 오버레이를 생성하는 유틸리티 클래스
/// Unity 에디터에서 Context Menu를 통해 페이드 오버레이를 쉽게 생성할 수 있습니다.
/// </summary>
public class FadeOverlayCreator : MonoBehaviour
{
    [Header("페이드 오버레이 설정")]
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private int sortingOrder = 1000; // 다른 UI보다 앞에 표시

    /// <summary>
    /// 페이드 오버레이 GameObject 생성
    /// </summary>
    [ContextMenu("페이드 오버레이 생성")]
    public GameObject CreateFadeOverlay()
    {
        // 기존 페이드 오버레이 확인
        GameObject existingOverlay = GameObject.Find("FadeOverlay");
        if (existingOverlay != null)
        {
            Debug.LogWarning("이미 FadeOverlay가 존재합니다: " + existingOverlay.name);
            return existingOverlay;
        }

        // Canvas 찾기 또는 생성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("씬에 Canvas가 없습니다. Canvas를 먼저 생성해주세요.");
            return null;
        }

        // 페이드 오버레이 GameObject 생성
        GameObject fadeOverlay = new GameObject("FadeOverlay");
        fadeOverlay.transform.SetParent(canvas.transform, false);

        // RectTransform 설정 (전체 화면 크기)
        RectTransform rectTransform = fadeOverlay.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;

        // Image 컴포넌트 추가
        Image image = fadeOverlay.AddComponent<Image>();
        image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f); // 초기에는 투명
        image.raycastTarget = false; // 마우스 이벤트 차단하지 않음

        // Canvas Group 추가 (선택사항 - 더 세밀한 제어를 위해)
        CanvasGroup canvasGroup = fadeOverlay.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // 렌더링 순서 설정
        Canvas overlayCanvas = fadeOverlay.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = sortingOrder;

        // GraphicRaycaster 추가 (UI 이벤트 처리용)
        fadeOverlay.AddComponent<GraphicRaycaster>();

        // 초기에는 비활성화
        fadeOverlay.SetActive(false);

        Debug.Log("페이드 오버레이가 생성되었습니다: " + fadeOverlay.name);
        
        // Unity 에디터에서 선택
        #if UNITY_EDITOR
        UnityEditor.Selection.activeGameObject = fadeOverlay;
        #endif

        return fadeOverlay;
    }

    /// <summary>
    /// 현재 씬의 모든 Canvas에 페이드 오버레이 생성
    /// </summary>
    [ContextMenu("모든 Canvas에 페이드 오버레이 생성")]
    public void CreateFadeOverlayForAllCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        
        if (canvases.Length == 0)
        {
            Debug.LogError("씬에 Canvas가 없습니다.");
            return;
        }

        foreach (Canvas canvas in canvases)
        {
            // 이미 페이드 오버레이가 있는지 확인
            bool hasOverlay = false;
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                if (canvas.transform.GetChild(i).name.Contains("FadeOverlay"))
                {
                    hasOverlay = true;
                    break;
                }
            }

            if (!hasOverlay)
            {
                CreateFadeOverlayForCanvas(canvas);
            }
        }
    }

    /// <summary>
    /// 특정 Canvas에 페이드 오버레이 생성
    /// </summary>
    private GameObject CreateFadeOverlayForCanvas(Canvas targetCanvas)
    {
        // 페이드 오버레이 GameObject 생성
        GameObject fadeOverlay = new GameObject($"FadeOverlay_{targetCanvas.name}");
        fadeOverlay.transform.SetParent(targetCanvas.transform, false);

        // RectTransform 설정 (전체 화면 크기)
        RectTransform rectTransform = fadeOverlay.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;

        // Image 컴포넌트 추가
        Image image = fadeOverlay.AddComponent<Image>();
        image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f); // 초기에는 투명
        image.raycastTarget = false;

        // 최상위 레이어로 설정
        fadeOverlay.transform.SetAsLastSibling();

        // 초기에는 비활성화
        fadeOverlay.SetActive(false);

        Debug.Log($"페이드 오버레이가 {targetCanvas.name}에 생성되었습니다: " + fadeOverlay.name);
        return fadeOverlay;
    }
}
