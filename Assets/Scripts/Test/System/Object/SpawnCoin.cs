// 파일: SpawnCoin.cs

using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime; 

// ⭐ 수정: MonoBehaviourPunCallbacks를 상속받아 마스터 변경 시 처리에 대비하며, photonView에 접근합니다.
public class SpawnCoin : MonoBehaviourPunCallbacks
{
    [Header("코인 프리팹 (PhotonView 포함)")]
    [SerializeField] private GameObject coin1Prefab;
    [SerializeField] private GameObject coin10Prefab;
    [SerializeField] private GameObject coin50Prefab;

    [Header("코인 생성 확률 (총합 1.0 이하 권장)")]
    [Range(0f, 1f)] [SerializeField] private float tenCoinChance = 0.05f;
    [Range(0f, 1f)] [SerializeField] private float fiftyCoinChance = 0.01f;

    [Header("코인 생성 높이")]
    [SerializeField] private float spawnHeight = 2f;
    
    // ⭐ 새로 추가: 현재 스폰된 코인의 PhotonView ID를 추적합니다.
    private int currentCoinViewID = 0; 

    void Start()
    {
        // 맵 생성 시점에 마스터 클라이언트만 초기 코인 생성
        if (PhotonNetwork.IsMasterClient)
        {
            if (photonView == null)
            {
                Debug.LogError("SpawnCoin에 PhotonView가 없습니다. 인스펙터에 추가해주세요!");
                return;
            }
            Spawn();
        }
    }

    private void Spawn()
    {
        // ⭐ 중복 생성 방지 및 PhotonView 확인
        if (currentCoinViewID != 0) return;
        if (photonView == null) return;
        
        Vector3 spawnPosition = transform.position + Vector3.up * spawnHeight;

        float rand = Random.value;
        GameObject prefabToSpawn;
        string prefabName;

        if (rand < fiftyCoinChance)
        {
            prefabToSpawn = coin50Prefab;
            prefabName = coin50Prefab.name;
        }
        else if (rand < fiftyCoinChance + tenCoinChance)
        {
            prefabToSpawn = coin10Prefab;
            prefabName = coin10Prefab.name;
        }
        else
        {
            prefabToSpawn = coin1Prefab;
            prefabName = coin1Prefab.name;
        }
        
        // ⭐ PhotonNetwork.Instantiate를 사용하여 네트워크 객체 생성 (Resources 폴더에 있어야 함)
        // 코인은 맵 조각의 자식 객체가 아니므로 RoomObject가 아닌 일반 Instantiate를 사용하고,
        // 마스터 클라이언트가 생성과 소유권을 가집니다.
        GameObject spawnedCoin = PhotonNetwork.Instantiate(
            "Prefabs/Coin/" + prefabName, // Prefabs/Coin/coin1PrefabName 와 같은 경로를 사용해야 함
            spawnPosition, 
            Quaternion.identity
        );
        
        // ⭐ 생성된 코인의 PhotonView ID를 저장합니다.
        PhotonView coinPV = spawnedCoin.GetComponent<PhotonView>();
        if (coinPV != null)
        {
            currentCoinViewID = coinPV.ViewID;
            
            // ⭐ 수정: 생성된 코인에게 이 SpawnCoin의 PhotonView ID를 전달합니다.
            Coin coinScript = spawnedCoin.GetComponent<Coin>();
            if (coinScript != null)
            {
                coinScript.SetSpawnCoinViewID(photonView.ViewID); 
            }
        }
    }

    // ⭐ 6. Coin이 파괴된 후, 마스터 클라이언트만 호출하는 코인 재생성 코루틴 시작 RPC
    [PunRPC]
    public void RPC_RequestRespawn(float delay)
    {
        // 이 RPC는 마스터 클라이언트에서만 실행됩니다.
        if (!PhotonNetwork.IsMasterClient) return;
        
        // 기존 코인 ID 초기화
        currentCoinViewID = 0; 
        
        StartCoroutine(RespawnRoutine(delay));
    }
    
    private IEnumerator RespawnRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Spawn();
    }
    
    // ⭐ 7. 마스터 클라이언트가 변경될 경우 새로운 마스터 클라이언트가 스폰을 이어받도록 처리
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // 새로 마스터가 된 클라이언트가 이 SpawnCoin이 관리하는 코인이 없는 경우, 초기 생성을 시도합니다.
        if (newMasterClient.IsLocal && currentCoinViewID == 0) 
        {
             Spawn();
        }
    }
}