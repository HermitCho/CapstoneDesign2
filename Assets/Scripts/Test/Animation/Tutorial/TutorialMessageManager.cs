using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialMessageManager : MonoBehaviour
{
    public static TutorialMessageManager Instance; // 어디서든 접근 가능하게

    [Header("UI 설정")]
    public TextMeshProUGUI messageText;

    [Header("튜토리얼 메시지 목록")]
    [TextArea]
    public string[] tutorialMessages;

    [Header("타이밍 설정")]
    public float messageDuration = 3f;
    public float fadeTime = 1f;

    private Coroutine currentRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 시작 메시지 자동 출력 (0번)
        ShowMessage(0);
    }

    public void ShowMessage(int index)
    {
        if (index < 0 || index >= tutorialMessages.Length) return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(DisplayMessage(tutorialMessages[index]));
    }

    IEnumerator DisplayMessage(string text)
    {
        messageText.text = text;
        messageText.alpha = 0;

        // Fade In
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            messageText.alpha = Mathf.Lerp(0, 1, t / fadeTime);
            yield return null;
        }

        messageText.alpha = 1;
        yield return new WaitForSeconds(messageDuration);

        // Fade Out
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            messageText.alpha = Mathf.Lerp(1, 0, t / fadeTime);
            yield return null;
        }
        messageText.alpha = 0;
    }
}