using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToxicFloorScroll : MonoBehaviour
{
    [SerializeField] private float scrollSpeedX = 0.1f;
    [SerializeField] private float scrollSpeedY = 0.05f;
    [SerializeField] private Color emissionColor = new Color(0.3f, 1f, 0.2f);
    [SerializeField] private float pulseSpeed = 2f;

    private Renderer rend;
    private Vector2 offset;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogError("Renderer가 없습니다!");
            enabled = false;
            return;
        }

        // 머티리얼 인스턴스화 (공유 머티리얼 영향 방지)
        rend.material = new Material(rend.material);
    }

    void Update()
    {
        // ✅ 텍스처 흐름 (슬라임 움직임)
        offset.x += Time.deltaTime * scrollSpeedX;
        offset.y += Time.deltaTime * scrollSpeedY;

        if (rend.material.HasProperty("_BaseMap"))
            rend.material.SetTextureOffset("_BaseMap", offset);
        else if (rend.material.HasProperty("_MainTex"))
            rend.material.SetTextureOffset("_MainTex", offset);

        // ✅ Emission 깜빡임 (빛나는 독 느낌)
        float glow = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        Color finalColor = emissionColor * Mathf.LinearToGammaSpace(glow * 2f);
        rend.material.SetColor("_EmissionColor", finalColor);
    }
}