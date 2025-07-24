using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnCoin : MonoBehaviour
{

    [Header("코인 프리팹")]
    [SerializeField] private GameObject coinPrefab;

    [Header("코인 생성 높이")]
    [SerializeField] private float spawnHeight = 2f; // 물체 위에서 떠있는 높이

    //내부 상태 변수
    private float spawnTimer = 0f;
    private bool isSpawned = false;

    // Start is called before the first frame update
    void Start()
    {
        Spawn();

    }

    private void Spawn()
    {
        if(!isSpawned)
        {
            SpawnCoinAtPosition();
            isSpawned = true;
        }
        
        
    }

    private void SpawnCoinAtPosition()
    {
        // 현재 오브젝트의 위치에서 위쪽으로 spawnHeight만큼 떨어진 위치 계산
        Vector3 spawnPosition = transform.position + Vector3.up * spawnHeight;
        
        // 코인 생성
        GameObject spawnedCoin = Instantiate(coinPrefab, spawnPosition, Quaternion.identity);
        
        // 생성된 코인을 현재 오브젝트의 자식으로 설정
        spawnedCoin.transform.SetParent(transform);
        
    }
}
