using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealEffectUI : MonoBehaviour
{
    public RawImage overlay;
    private Material mat;

    // HealEffectUI.cs
    void OnEnable()
    {
        GameEvents.OnLocalPlayerHeal += OnPlayerHeal;
    }

    void OnDisable()
    {
        GameEvents.OnLocalPlayerHeal -= OnPlayerHeal;
    }

    private void OnPlayerHeal()
    {
        ShowHeal();
    }

    void Start()
    {
        if (overlay == null || overlay.material == null)
        {
            Debug.LogError("HealEffectUI 초기화 실패 - overlay나 material 없음");
            return;
        }

        mat = Instantiate(overlay.material);
        overlay.material = mat;
        mat.SetFloat("_Intensity", 0f);
    }

    public void ShowHeal()
    {
        if (mat == null) return;

        mat.SetFloat("_Intensity", 1f);
        mat.SetColor("_Color", new Color(0.3f, 1f, 0.3f, 0.6f));

        StopAllCoroutines();
        StartCoroutine(FadeOutCoroutine());
    }

    IEnumerator FadeOutCoroutine()
    {
        float t = 0f;
        float duration = 1.2f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float intensity = Mathf.Lerp(1f, 0f, t / duration);
            mat.SetFloat("_Intensity", intensity);
            yield return null;
        }

        mat.SetFloat("_Intensity", 0f);
    }
}
