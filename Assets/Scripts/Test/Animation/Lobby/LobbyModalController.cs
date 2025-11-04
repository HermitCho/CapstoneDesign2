using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Heat;
using TMPro;
using DG.Tweening;

public class LobbyModalController : MonoBehaviour
{
    [Header("Modal 연결")]
    [SerializeField] private ModalWindowManager abilityModalWindow;
    [SerializeField] private ModalWindowManager gunModalWindow;

    [Header("설명 텍스트 (캐릭터 순서대로 입력)")]
    [TextArea(2, 4)] public string[] abilityDescriptions;
    [TextArea(2, 4)] public string[] gunDescriptions;

    [Header("타이핑 설정")]
    public float typingDuration = 0.5f;  // 값 작을수록 빠름
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    private int currentIndex = 0;
    private Coroutine typingRoutine;

    void Awake()
    {
        currentIndex = PlayerPrefs.GetInt("LobbyModal_CurrentIndex", 0);
    }

    // ----------- 능력 버튼 ----------
    public void OnHoverAbilityButton()
    {

        if (abilityModalWindow == null || abilityModalWindow.windowDescription == null)
            return;
        
        abilityModalWindow.OpenWindow();
        string content = abilityDescriptions[currentIndex];
        StartTyping(abilityModalWindow.windowDescription, content);
    }

    // ----------- 총 버튼 ----------
    public void OnHoverGunButton()
    {
        if (gunModalWindow == null || gunModalWindow.windowDescription == null)
            return;

        gunModalWindow.OpenWindow();
        string content = gunDescriptions[currentIndex];
        StartTyping(gunModalWindow.windowDescription, content);
    }

    public void OnLeaveGunButton()
    {
        if (gunModalWindow != null)
            gunModalWindow.CloseWindow();
    }

    // ----------- 캐릭터 선택 ----------
    public void OnSetIndex(int index)
    {
        currentIndex = index;
        PlayerPrefs.SetInt("LobbyModal_CurrentIndex", currentIndex);
        PlayerPrefs.Save();
    }

    // ----------- 타이핑 애니메이션 ----------
    void StartTyping(TextMeshProUGUI tmp, string content)
    {
        if (tmp == null) return;

        tmp.DOKill();
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypePerChar(tmp, content));
    }

    IEnumerator TypePerChar(TextMeshProUGUI tmp, string content)
    {
        tmp.text = content;
        tmp.maxVisibleCharacters = 0;
        tmp.ForceMeshUpdate();

        int total = tmp.textInfo.characterCount;
        if (total == 0)
            yield break;

        float interval = Mathf.Max(0.01f, typingDuration / total);
        int prev = -1;

        for (int i = 0; i < total; i++)
        {
            tmp.maxVisibleCharacters = i + 1;
            tmp.ForceMeshUpdate();

            if (prev >= 0) SetCharColor(tmp, prev, normalColor);
            SetCharColor(tmp, i, highlightColor);
            tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            prev = i;
            yield return new WaitForSeconds(interval);
        }

        if (prev >= 0)
        {
            SetCharColor(tmp, prev, normalColor);
            tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
    }

    void SetCharColor(TextMeshProUGUI tmp, int index, Color color)
    {
        if (tmp == null || tmp.textInfo == null || index < 0 || index >= tmp.textInfo.characterCount)
            return;

        var info = tmp.textInfo.characterInfo[index];
        if (!info.isVisible) return;

        int meshIndex = info.materialReferenceIndex;
        int vertexIndex = info.vertexIndex;
        var colors = tmp.textInfo.meshInfo[meshIndex].colors32;
        for (int i = 0; i < 4; i++) colors[vertexIndex + i] = color;
    }
}