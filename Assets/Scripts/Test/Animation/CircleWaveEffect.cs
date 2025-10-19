using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleWaveEffect : MonoBehaviour
{
    [Header("원형 머티리얼 순서 (0=배경, 1=원)")]
    [SerializeField] private int circleMaterialIndex = 1;

    [Header("물결 설정")]
    [SerializeField] private int waveCount = 5;
    [SerializeField] private float waveSpeed = 0.1f;
    [SerializeField] private float minTiling = 0.1f;
    [SerializeField] private float maxTiling = 2f;
    [SerializeField] private float waveInterval = 0.1f;

    private List<Material> waveMats = new List<Material>();
    private List<float> waveTimers = new List<float>();

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null || rend.materials.Length <= circleMaterialIndex)
        {
            Debug.LogError("CircleWaveEffect: Renderer나 머티리얼 인덱스가 잘못되었습니다.");
            enabled = false;
            return;
        }

        Material baseMat = rend.materials[circleMaterialIndex];
        waveMats.Clear();
        waveTimers.Clear();

        for (int i = 0; i < waveCount; i++)
        {
            Material newMat = new Material(baseMat);
            waveMats.Add(newMat);
            waveTimers.Add(i * waveInterval);
        }

        Material[] newMats = rend.materials;
        List<Material> matList = new List<Material>(newMats);

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
            if (waveTimers[i] > 1f)
                waveTimers[i] -= 1f;

            float t = waveTimers[i];
            float tiling = Mathf.Lerp(maxTiling, minTiling, t);
            mat.mainTextureScale = new Vector2(tiling, tiling);

            float offset = (1f - tiling) / 2f;
            mat.mainTextureOffset = new Vector2(offset, offset);
        }
    }
}