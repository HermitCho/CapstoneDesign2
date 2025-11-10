using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnCoin : MonoBehaviour
{

    [Header("코인 프리팹")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject coin10Prefab;
    [SerializeField] private GameObject coin50Prefab;

     [Header("코인 생성 확률 (총합 1.0 이하 권장)")]
    [Range(0f, 1f)] [SerializeField] private float tenCoinChance = 0.05f;
    [Range(0f, 1f)] [SerializeField] private float fiftyCoinChance = 0.01f;

    [Header("코인 생성 높이")]
    [SerializeField] private float spawnHeight = 2f; // 물체 위에서 떠있는 높이

    private bool isSpawned = false;

    // Start is called before the first frame update
    void Start()
    {
        Spawn();

    }

    private void Spawn()
    {
        // 현재 오브젝트의 위치에서 위쪽으로 spawnHeight만큼 떨어진 위치 계산
        Vector3 spawnPosition = transform.position + Vector3.up * spawnHeight;

        float rand = Random.value; // 0~1 난수
        GameObject prefabToSpawn;

        // 확률 구간에 따라 코인 종류 결정
        if (rand < fiftyCoinChance)
        {
            prefabToSpawn = coin50Prefab; // 50코인
        }
        else if (rand < fiftyCoinChance + tenCoinChance)
        {
            prefabToSpawn = coin10Prefab; // 10코인
        }
        else
        {
            prefabToSpawn = coinPrefab; // 1코인
        }
        
        // 코인 생성
        GameObject spawnedCoin = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        
        // 생성된 코인을 현재 오브젝트의 자식으로 설정
        spawnedCoin.transform.SetParent(transform);
    }

    public IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Spawn();
    }
}
