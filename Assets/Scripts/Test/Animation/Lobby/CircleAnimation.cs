using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleAnimation : MonoBehaviour
{
    [SerializeField] private Renderer rend;
    [SerializeField] private float speed = 0.3f;
    [SerializeField] private float startSize1 = 0.08f;
    [SerializeField] private float endSize1 = 0.8f;
    [SerializeField] private float startSize2 = 0.1f;
    [SerializeField] private float endSize2 = 1f;
    [SerializeField] private float fadeOutDuration = 1.0f; // 사라지는 속도

    private Material mat;
    private float current1;
    private float current2;
    private float alpha = 1f;
    private bool fadingOut = false;

    void Start()
    {
        if (rend == null)
            rend = GetComponent<Renderer>();

        mat = rend.material;
        ResetCircle();
    }

    void Update()
    {
        // 페이드 중이 아니면 크기 증가
        if (!fadingOut)
        {
            current1 = Mathf.MoveTowards(current1, endSize1, Time.deltaTime * speed);
            current2 = Mathf.MoveTowards(current2, endSize2, Time.deltaTime * speed);

            mat.SetFloat("_Circle_size", current1);
            mat.SetFloat("_Circle_size2", current2);

            // 끝까지 커지면 페이드 시작
            if (Mathf.Approximately(current1, endSize1))
                StartCoroutine(FadeOut());
        }
    }

    private System.Collections.IEnumerator FadeOut()
    {
        fadingOut = true;
        float t = 0f;
        Color c = mat.GetColor("_Color");

        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            alpha = Mathf.Lerp(1f, 0f, t / fadeOutDuration);
            c.a = alpha;
            mat.SetColor("_Color", c);
            yield return null;
        }

        // 끝나면 다시 초기화해서 루프 재시작
        ResetCircle();
    }

    private void ResetCircle()
    {
        fadingOut = false;
        alpha = 1f;

        current1 = startSize1;
        current2 = startSize2;

        Color c = mat.GetColor("_Color");
        c.a = alpha;
        mat.SetColor("_Color", c);

        mat.SetFloat("_Circle_size", current1);
        mat.SetFloat("_Circle_size2", current2);
    }
}