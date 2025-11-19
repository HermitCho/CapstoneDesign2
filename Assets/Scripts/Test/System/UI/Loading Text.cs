using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // 필수

public class LoadingText : MonoBehaviour
{
    [Header("Settings")]
    // 변경점 1: UI용 텍스트를 담기 위해 타입을 TextMeshProUGUI로 변경
    public TextMeshProUGUI textMeshPro; 
    
    public string baseText = "Loading";
    public float updateInterval = 0.8f;

    private void Start()
    {
        if (textMeshPro == null)
        {
            // 변경점 2: GetComponent 할 때도 타입을 맞춰주어야 함
            textMeshPro = GetComponent<TextMeshProUGUI>();
        }

        StartCoroutine(AnimateLoadingText());
    }

    private IEnumerator AnimateLoadingText()
    {
        int dotCount = 0;

        while (true)
        {
            int currentDots = dotCount % 4;
            textMeshPro.text = baseText + new string('.', currentDots);
            dotCount++;
            yield return new WaitForSeconds(updateInterval);
        }
    }
}