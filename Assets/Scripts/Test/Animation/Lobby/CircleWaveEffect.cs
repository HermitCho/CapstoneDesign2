using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleWaveEffect : MonoBehaviour
{
    [Header("원형 머티리얼 인덱스 (0=배경, 1=첫 원)")]
    [SerializeField] private int circleMaterialIndex = 1;
    [SerializeField] private int waveCount = 15;
    [SerializeField] private float waveSpeed = 0.3f;
    [SerializeField] private float minTiling = 0.5f;
    [SerializeField] private float maxTiling = 2.5f;
    [SerializeField] private float waveInterval = 0.8f;

    [Header("투명도 조절")]
    [SerializeField] private float alphaMin = 0.0f;
    [SerializeField] private float alphaMax = 0.6f;

    private List<Material> waveMats = new List<Material>();
    private List<float> waveTimers = new List<float>();

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        Material baseMat = rend.materials[circleMaterialIndex];

        // 🟣 원본 머티리얼 복사
        waveMats.Clear();
        waveTimers.Clear();
        for (int i = 0; i < waveCount; i++)
        {
            Material newMat = new Material(baseMat);
            waveMats.Add(newMat);
            waveTimers.Add(i * waveInterval);
        }

        // MeshRenderer에 머티리얼 배열로 추가
        Material[] mats = rend.materials;
        List<Material> matList = new List<Material>(mats);
        matList.RemoveAt(circleMaterialIndex);
        matList.InsertRange(circleMaterialIndex, waveMats);
        rend.materials = matList.ToArray();
    }

    void Update()
    {
        for (int i = 0; i < waveCount; i++)
        {
            Material mat = waveMats[i];
            if (mat == null) continue;

            waveTimers[i] += Time.deltaTime * waveSpeed;
            if (waveTimers[i] > 1f) waveTimers[i] -= 1f;

            float t = waveTimers[i];

            // 🌀 안쪽에서 바깥으로 퍼지는 형태
            float tiling = Mathf.Lerp(maxTiling, minTiling, t);
            mat.mainTextureScale = new Vector2(tiling, tiling);

            float offset = (1f - tiling) / 2f;
            mat.mainTextureOffset = new Vector2(offset, offset);

            // ✨ 투명도 부드럽게 Fade Out
            Color c = mat.color;
            c.a = Mathf.Lerp(alphaMax, alphaMin, t);
            mat.color = c;
        }
    }
}