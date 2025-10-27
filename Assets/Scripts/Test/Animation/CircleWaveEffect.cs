using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleWaveEffect : MonoBehaviour
{
    [Header("원형 머티리얼 순서 (0=배경, 1=원)")]
    [SerializeField] private int circleMaterialIndex = 1;

    [Header("물결 설정")]
    [SerializeField] private int waveCount = 15;

    [SerializeField, Tooltip("파동 속도 최소값")]
    private float minWaveSpeed = 0.05f;
    [SerializeField, Tooltip("파동 속도 최대값 (최고속도 제한)")]
    private float maxWaveSpeed = 0.2f;

    [SerializeField] private float minTiling = 0.01f;
    [SerializeField] private float maxTiling = 1.5f;

    [Header("파형 간 간격 랜덤 설정")]
    [SerializeField] private float minWaveInterval = 0.05f;
    [SerializeField] private float maxWaveInterval = 0.15f;

    private List<Material> waveMats = new List<Material>();
    private List<float> waveTimers = new List<float>();
    private List<float> waveSpeeds = new List<float>();

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
        waveSpeeds.Clear();

        float currentInterval = 0f;

        for (int i = 0; i < waveCount; i++)
        {
            Material newMat = new Material(baseMat);
            waveMats.Add(newMat);

            // 각 파형 간격 랜덤
            float interval = Random.Range(minWaveInterval, maxWaveInterval);
            currentInterval += interval;
            waveTimers.Add(currentInterval);

            // 각 파형 속도 랜덤
            float speed = Random.Range(minWaveSpeed, maxWaveSpeed);
            waveSpeeds.Add(speed);
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

            // 각 파형마다 속도 다르게 적용
            waveTimers[i] += Time.deltaTime * waveSpeeds[i];
            if (waveTimers[i] > 1f) waveTimers[i] -= 1f;

            float t = waveTimers[i];

            // 중앙에서 바깥으로 커지는 원
            float tiling = Mathf.Lerp(maxTiling, minTiling, t);
            mat.mainTextureScale = new Vector2(tiling, tiling);

            float offset = (1f - tiling) / 2f;
            mat.mainTextureOffset = new Vector2(offset, offset);
        }
    }
}