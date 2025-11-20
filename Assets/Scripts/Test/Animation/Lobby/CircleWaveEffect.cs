using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleWaveEffect : MonoBehaviour
{
    [Header("🎨 기본 설정")]
    [SerializeField] private int circleMaterialIndex = 0;

    [Header("🌊 파동 설정")]
    [SerializeField] private int maxWaveCount = 10;
    [SerializeField] private float spawnDelay = 0.5f;
    [SerializeField] private float growSpeed = 0.5f;
    [SerializeField] private float startTiling = 7f;   // 처음엔 작게 보이는 (바깥쪽)
    [SerializeField] private float endTiling = 0.2f;   // 작을수록 원이 커짐 (중앙으로 확산)
    [SerializeField] private Renderer targetRenderer;
    
    private Renderer rend;
    private Material baseMat;

    void Start()
    {
        rend = rend = targetRenderer;
        if (rend == null || rend.materials.Length <= circleMaterialIndex)
        {
            Debug.LogError("❌ CircleWaveEffect: Renderer나 circleMaterialIndex 오류");
            enabled = false;
            return;
        }

        baseMat = rend.materials[circleMaterialIndex];

        // 기본 원은 투명하게 처리
        Color baseColor = baseMat.color;
        baseColor.a = 0f;
        baseMat.color = baseColor;

        StartCoroutine(WaveSpawner());
    }

    IEnumerator WaveSpawner()
    {
        int count = 0;
        while (count < maxWaveCount)
        {
            SpawnWave();
            count++;
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    void SpawnWave()
    {
        Material newMat = new Material(baseMat);
        Color c = newMat.color;
        c.a = 1f;
        newMat.color = c;

        var mats = new List<Material>(rend.materials);
        mats.Insert(circleMaterialIndex, newMat);
        rend.materials = mats.ToArray();

        StartCoroutine(GrowWave(newMat));
    }

    IEnumerator GrowWave(Material mat)
    {
        float currentTiling = startTiling;

        while (currentTiling > endTiling)
        {
            // ✅ 작아지는 방향으로 (시각적으로는 커짐)
            currentTiling -= Time.deltaTime * growSpeed;

            mat.mainTextureScale = new Vector2(currentTiling, currentTiling);

            // 중심 유지
            float offset = (1f - currentTiling) * 0.5f;
            mat.mainTextureOffset = new Vector2(offset, offset);

            yield return null;
        }

        // 제거
        var mats = new List<Material>(rend.materials);
        mats.Remove(mat);
        rend.materials = mats.ToArray();
        Destroy(mat);
    }
}