using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleWaveEffect : MonoBehaviour
{
    [Header("🎨 기본 설정")]
    [SerializeField] private int circleMaterialIndex = 1;

    [Header("🌊 물결 설정")]
    [SerializeField] private int maxWaveCount = 10;     // 최대 원 개수
    [SerializeField] private float spawnDelay = 0.6f;   // 다음 원 간격
    [SerializeField] private float fadeDuration = 6.5f; // 전체 생명주기
    [SerializeField] private float minTiling = 0.6f;    // 시작 크기 (중앙)
    [SerializeField] private float maxTiling = 7.0f;    // 끝 크기 (바깥)
    [SerializeField] private float maxAlpha = 0.45f;    // 최대 투명도

    [Header("⚡ 속도 제어 (랜덤 범위)")]
    [SerializeField, Range(0.1f, 5f)] private float minWaveSpeed = 0.8f;
    [SerializeField, Range(0.1f, 5f)] private float maxWaveSpeed = 1.2f;

    private Renderer rend;
    private Material baseMat;
    private List<Material> activeWaves = new List<Material>();

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend == null || rend.materials.Length <= circleMaterialIndex)
        {
            Debug.LogError("❌ CircleWaveEffect: Renderer나 circleMaterialIndex 오류");
            enabled = false;
            return;
        }

        baseMat = rend.materials[circleMaterialIndex];

        // 기본 원 투명 처리
        Color baseColor = baseMat.color;
        baseColor.a = 0f;
        baseMat.color = baseColor;

        StartCoroutine(WaveSpawner());
    }

    IEnumerator WaveSpawner()
    {
        while (true)
        {
            if (activeWaves.Count < maxWaveCount)
            {
                SpawnWave();
                yield return new WaitForSeconds(spawnDelay);
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    void SpawnWave()
    {
        Material newMat = new Material(baseMat);

        Color c = newMat.color;
        c.a = 0f;
        newMat.color = c;

        var mats = new List<Material>(rend.materials);
        mats.Insert(circleMaterialIndex, newMat);
        rend.materials = mats.ToArray();

        activeWaves.Add(newMat);

        // ✅ 속도 랜덤 지정
        float randomSpeed = Random.Range(minWaveSpeed, maxWaveSpeed);

        StartCoroutine(AnimateWave(newMat, randomSpeed));
    }

    IEnumerator AnimateWave(Material mat, float waveSpeed)
    {
        float t = 0f;
        Color c = mat.color;
        c.a = 0f;
        mat.color = c;

        while (t < fadeDuration)
        {
            t += Time.deltaTime * waveSpeed; // ✅ 랜덤 속도 적용
            float normalized = t / fadeDuration;

            // ✅ 중앙 → 바깥 커짐
            float currentTiling = Mathf.Lerp(maxTiling, minTiling, normalized);
            mat.mainTextureScale = new Vector2(currentTiling, currentTiling);

            // ✅ 중심 유지
            float offset = (1f - currentTiling) * 0.5f;
            mat.mainTextureOffset = new Vector2(offset, offset);

            // ✅ 자연스러운 페이드
            float alpha = 0f;
            if (normalized < 0.2f)
                alpha = Mathf.Lerp(0f, maxAlpha, normalized / 0.2f);
            else if (normalized > 0.8f)
                alpha = Mathf.Lerp(maxAlpha, 0f, (normalized - 0.8f) / 0.2f);
            else
                alpha = maxAlpha;

            c.a = alpha;
            mat.color = c;

            yield return null;
        }

        // 🔹 파동 제거
        activeWaves.Remove(mat);
        var mats = new List<Material>(rend.materials);
        mats.Remove(mat);
        rend.materials = mats.ToArray();
        Destroy(mat);
    }
}