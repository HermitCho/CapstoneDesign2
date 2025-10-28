using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using UnityEngine;
using Michsky.UI.Heat;


public class ModalTextEffect : MonoBehaviour
{
    [Header("Modal 참조 (비워두면 자동 탐색)")]
    [SerializeField] private ModalWindowManager modalWindow;

    [Header("Text Components (비워두면 ModalWindowManager 참조 사용)")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    [Header("Text Content")]
    [Tooltip("ModalWindowManager의 title/description을 사용할지 여부")] public bool useManagerContent = true;
    [TextArea] public string titleContent = "타이틀 강조!";
    [TextArea] public string descriptionContent = "이것은 설명 텍스트입니다.";

    [Header("Effect Settings")]
    public float typingDuration = 1.2f;
    public float scaleDuration = 0.4f;
    public float scaleAmount = 1.15f;

    [Header("Sequence Settings")]
    [Tooltip("열릴 때 바로 타이틀 강조를 실행할지")] public bool emphasizeTitleAtOpen = true;
    [Tooltip("타이틀 강조가 끝난 뒤 설명 타이핑을 시작할지")] public bool delayDescriptionUntilTitleEmphasis = true;

    [Header("Description Typing (Per-Char)")]
    [Tooltip("설명을 글자 단위로 노출하며 마지막 글자를 강조합니다")] public bool perCharDescriptionEmphasis = true;
    [Tooltip("글자당 간격(초). 0이면 총 타이핑 시간에 맞춰 자동 계산")] public float descriptionCharInterval = 0f;
    public Color descriptionNormalColor = Color.white;
    public Color descriptionEmphasisColor = new Color(1f, 1f, 1f, 1f);

    Coroutine descriptionRoutine;

    void Awake()
    {
        if (modalWindow == null)
            modalWindow = GetComponent<ModalWindowManager>();

        if (modalWindow != null)
        {
            modalWindow.onOpen.AddListener(HandleOpen);
            modalWindow.onClose.AddListener(HandleClose);
        }
    }

    void OnDestroy()
    {
        if (modalWindow != null)
        {
            modalWindow.onOpen.RemoveListener(HandleOpen);
            modalWindow.onClose.RemoveListener(HandleClose);
        }
    }

    /// <summary>
    /// 모달창이 열릴 때 실행되는 타이핑/강조 효과
    /// </summary>
    public void HandleOpen()
    {
        TextMeshProUGUI title = titleText != null ? titleText : modalWindow != null ? modalWindow.windowTitle : null;
        TextMeshProUGUI desc = descriptionText != null ? descriptionText : modalWindow != null ? modalWindow.windowDescription : null;

        string tContent = useManagerContent && modalWindow != null ? modalWindow.titleText : titleContent;
        string dContent = useManagerContent && modalWindow != null ? modalWindow.descriptionText : descriptionContent;

        float descDelay = 0f;

        if (title != null)
        {
            title.DOKill();
            title.transform.DOKill();
            title.text = string.Empty;

            // 타이틀 타이핑 즉시 시작
            title.DOText(tContent, typingDuration).SetEase(Ease.Linear);

            // 열림 시 즉시 강조 애니메이션 실행 (원한다면)
            if (emphasizeTitleAtOpen)
            {
                title.transform.localScale = Vector3.one;
                title.transform.DOScale(scaleAmount, scaleDuration)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetEase(Ease.OutQuad);

                if (delayDescriptionUntilTitleEmphasis)
                {
                    // 왕복 루프 시간만큼 설명 타이핑 지연
                    descDelay = scaleDuration * 2f;
                }
            }
        }

        if (desc != null)
        {
            desc.DOKill();
            if (descriptionRoutine != null)
            {
                StopCoroutine(descriptionRoutine);
                descriptionRoutine = null;
            }

            if (perCharDescriptionEmphasis)
            {
                descriptionRoutine = StartCoroutine(TypeDescriptionPerChar(desc, dContent, descDelay));
            }
            else
            {
                desc.text = string.Empty;
                var tween = desc.DOText(dContent, typingDuration).SetEase(Ease.Linear);
                if (descDelay > 0f)
                    tween.SetDelay(descDelay);
            }
        }
    }

    /// <summary>
    /// 모달창이 닫힐 때 트윈 정리
    /// </summary>
    public void HandleClose()
    {
        TextMeshProUGUI title = titleText != null ? titleText : modalWindow != null ? modalWindow.windowTitle : null;
        TextMeshProUGUI desc = descriptionText != null ? descriptionText : modalWindow != null ? modalWindow.windowDescription : null;

        if (title != null)
        {
            title.DOKill();
            title.transform.DOKill();
        }

        if (desc != null)
        {
            desc.DOKill();
            if (descriptionRoutine != null)
            {
                StopCoroutine(descriptionRoutine);
                descriptionRoutine = null;
            }
            // 색상 초기화
            ResetAllCharacterColors(desc, descriptionNormalColor);
        }
    }

    IEnumerator TypeDescriptionPerChar(TextMeshProUGUI tmp, string content, float startDelay)
    {
        if (tmp == null)
            yield break;

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        tmp.ForceMeshUpdate();
        tmp.text = content;
        tmp.maxVisibleCharacters = 0;
        tmp.ForceMeshUpdate();

        int totalChars = tmp.textInfo.characterCount;
        if (totalChars == 0)
            yield break;

        float interval = descriptionCharInterval > 0f ? descriptionCharInterval : Mathf.Max(0.001f, typingDuration / Mathf.Max(1, totalChars));

        int previousVisibleIndex = -1;

        for (int i = 0; i < totalChars; i++)
        {
            tmp.maxVisibleCharacters = i + 1;
            tmp.ForceMeshUpdate();

            // 이전 글자 색상 복원
            if (previousVisibleIndex >= 0)
            {
                SetCharacterColor(tmp, previousVisibleIndex, descriptionNormalColor);
            }

            // 현재 글자 강조
            SetCharacterColor(tmp, i, descriptionEmphasisColor);
            tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            previousVisibleIndex = i;
            yield return new WaitForSeconds(interval);
        }

        // 마지막 글자 색상 복원
        if (previousVisibleIndex >= 0)
        {
            SetCharacterColor(tmp, previousVisibleIndex, descriptionNormalColor);
            tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
    }

    void SetCharacterColor(TextMeshProUGUI tmp, int charIndex, Color32 color)
    {
        if (tmp == null || tmp.textInfo == null)
            return;

        if (charIndex < 0 || charIndex >= tmp.textInfo.characterCount)
            return;

        var charInfo = tmp.textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible)
            return;

        int meshIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;
        var meshInfo = tmp.textInfo.meshInfo[meshIndex];
        var colors = meshInfo.colors32;

        if (vertexIndex + 3 >= colors.Length)
            return;

        colors[vertexIndex + 0] = color;
        colors[vertexIndex + 1] = color;
        colors[vertexIndex + 2] = color;
        colors[vertexIndex + 3] = color;
    }

    void ResetAllCharacterColors(TextMeshProUGUI tmp, Color32 color)
    {
        if (tmp == null)
            return;
        tmp.ForceMeshUpdate();
        int totalChars = tmp.textInfo.characterCount;
        for (int i = 0; i < totalChars; i++)
        {
            SetCharacterColor(tmp, i, color);
        }
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}
