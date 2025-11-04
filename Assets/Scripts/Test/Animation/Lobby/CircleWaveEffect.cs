using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleWaveEffect : MonoBehaviour
{
    [Header("🎨 머티리얼 설정")]
    [SerializeField] private int circleMaterialIndex = 1;

    [Header("🌊 애니메이션 설정")]
    [SerializeField] private float waveSpeed = 0.1f;   // 전체 속도 (느릴수록 차분함)
    [SerializeField] private float minTiling = 0.6f;     // 원 시작 크기
    [SerializeField] private float maxTiling = 3.0f;     // 원 끝 크기

    [Header("⚙️ 자동 루프 설정")]
    [SerializeField] private int waveCount = 9;         
    [SerializeField] private float fullCycleDuration = 12f; 
    [SerializeField] private float scaleStep = 0.02f;    
    [SerializeField] private float basePhaseOffset = 0.5f;

    [Header("🕶️ 페이드 설정")]
    [SerializeField, Range(0f, 1f)] private float fadeInRatio = 0.15f;   // 나타나는 구간 비율
    [SerializeField, Range(0f, 1f)] private float fadeOutRatio = 0.15f;  // 사라지는 구간 비율
    [SerializeField] private float baseAlpha = 0.45f;    // 최대 투명도

    [Header("🖼️ 텍스처 연결")]
    [SerializeField] private Texture2D thinTexture;
    [SerializeField] private Texture2D thickTexture;

    private List<Material> waveMats = new List<Material>();

    void Awake()
    {
        if (Application.isPlaying)
            SetupRuntimeMaterials();
    }

    void SetupRuntimeMaterials()
    {
        Renderer rend = GetComponent<Renderer>();
        if (!rend) return;

        Material[] mats = rend.materials;
        if (circleMaterialIndex < 0 || circleMaterialIndex >= mats.Length)
        {
            Debug.LogError("CircleWaveEffect: circleMaterialIndex 잘못됨");
            return;
        }

        Material baseMat = mats[circleMaterialIndex];
        waveMats.Clear();
        var matList = new List<Material>(mats);
        matList.RemoveAt(circleMaterialIndex);

        for (int i = 0; i < waveCount; i++)
        {
            Material newMat = new Material(baseMat);

            if (i % 2 == 0 && thinTexture)
                newMat.mainTexture = thinTexture;
            else if (thickTexture)
                newMat.mainTexture = thickTexture;

            Color c = newMat.color;
            c.a = 0f; // 시작 시 완전 투명
            newMat.color = c;

            waveMats.Add(newMat);
        }

        matList.InsertRange(circleMaterialIndex, waveMats);
        rend.materials = matList.ToArray();
    }

    void Update()
    {
        if (!Application.isPlaying || waveMats.Count == 0) return;

        // 각 원의 간격 자동 계산
        float delayBetween = fullCycleDuration / waveCount;

        for (int i = 0; i < waveMats.Count; i++)
        {
            Material mat = waveMats[i];
            if (!mat) continue;

            float localTime = (Time.time - (i * delayBetween)) * waveSpeed;
            localTime %= 1f;
            if (localTime < 0f) localTime += 1f;

            // 부드럽게 확장
            float t = Mathf.SmoothStep(0f, 1f, localTime);
            float tiling = Mathf.Lerp(maxTiling, minTiling, t);

            // 원 크기 단계
            float scaleFactor = 1f + (i - waveCount / 2f) * scaleStep;
            tiling *= scaleFactor;

            mat.mainTextureScale = new Vector2(tiling, tiling);
            float offset = (1f - tiling) * 0.5f;
            mat.mainTextureOffset = new Vector2(offset, offset);

            // ✨ 페이드인 / 페이드아웃
            Color c = mat.color;
            float alpha = baseAlpha;

            if (localTime < fadeInRatio)
            {
                float fadeT = localTime / fadeInRatio;
                alpha = Mathf.Lerp(0f, baseAlpha, fadeT);
            }
            else if (localTime > 1f - fadeOutRatio)
            {
                float fadeT = (localTime - (1f - fadeOutRatio)) / fadeOutRatio;
                alpha = Mathf.Lerp(baseAlpha, 0f, fadeT);
            }

            c.a = alpha;
            mat.color = c;
        }
    }
}