using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleWaveEffect : MonoBehaviour
{
    [Header("원형 머티리얼 순서 (0=배경, 1=원)")]
    [SerializeField] private int circleMaterialIndex = 1;

    [Header("물결 설정")]
    [SerializeField] private int waveCount = 3;          // 동시에 나타나는 파동 개수
    [SerializeField] private float waveSpeed = 1f;       // 커지는 속도
    [SerializeField] private float minTiling = 0.1f;    // 최소 크기
    [SerializeField] private float maxTiling = 2f;      // 최대 크기
    [SerializeField] private float waveInterval = 0.5f; // 파동 간격

    private Material circleMat;
    private List<float> waveTimers = new List<float>();

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null && rend.materials.Length > circleMaterialIndex)
        {
            circleMat = rend.materials[circleMaterialIndex];
        }
        else
        {
            Debug.LogError("CircleWaveBackground: Renderer나 머티리얼 인덱스가 잘못되었습니다.");
            enabled = false;
            return;
        }

        // 각 파동 타이머 초기화
        waveTimers.Clear();
        for (int i = 0; i < waveCount; i++)
        {
            waveTimers.Add(i * waveInterval);
        }
    }

    void Update()
    {
        if (circleMat == null) return;

        Color baseColor = circleMat.color;
        baseColor.a = 0f; // 초기 투명도
        circleMat.color = baseColor;

        for (int i = 0; i < waveTimers.Count; i++)
        {
            // 시간 누적
            waveTimers[i] += Time.deltaTime * waveSpeed;

            // PingPong으로 루프
            float t = Mathf.PingPong(waveTimers[i], 1f);

            // 타일링 계산
            float tiling = Mathf.Lerp(minTiling, maxTiling, t);
            circleMat.mainTextureScale = new Vector2(tiling, tiling);

            // 중심 보정
            float offset = (1f - tiling) / 2f;
            circleMat.mainTextureOffset = new Vector2(offset, offset);

            // 투명도 계산 (커질수록 사라짐)
            float alpha = 1f - t;

            // 겹치면서 자연스럽게 합산
            Color color = circleMat.color;
            color.a = Mathf.Clamp01(color.a + alpha / waveCount);
            circleMat.color = color;
        }
    }
}