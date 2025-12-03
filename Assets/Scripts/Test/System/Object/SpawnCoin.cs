using System.Collections;
using UnityEngine;
using Photon.Pun; // ⭐ 1. Photon PUN 네임스페이스 추가

// ⭐ 2. MonoBehaviourPunCallbacks 또는 MonoBehaviour 상속 유지
public class SpawnCoin : MonoBehaviour
{
    // 필드들은 그대로 유지
    [Header("코인 프리팹 (PhotonView 포함)")]
    // 이 프리팹들은 Resources 폴더에 있어야 하며, PhotonView 컴포넌트가 있어야 합니다.
    [SerializeField] private GameObject coin1Prefab;
    [SerializeField] private GameObject coin10Prefab;
    [SerializeField] private GameObject coin50Prefab;

    [Header("코인 생성 확률 (총합 1.0 이하 권장)")]
    [Range(0f, 1f)] [SerializeField] private float tenCoinChance = 0.05f;
    [Range(0f, 1f)] [SerializeField] private float fiftyCoinChance = 0.01f;

    [Header("코인 생성 높이")]
    [SerializeField] private float spawnHeight = 2f;
    
    // ✅ 재생성 스케줄링을 위한 변수
    private Coroutine respawnCoroutine = null;
    private bool isRespawning = false;
    
    // ✅ 재생성 횟수 추적 (재생성마다 다른 랜덤 값 생성)
    private int respawnCount = 0;

    // Start는 그대로 사용합니다.
    void Start()
    {
        // ⭐ 3. 마스터 클라이언트에서만 Spawn 로직을 실행하도록 합니다.
        if (PhotonNetwork.IsMasterClient)
        {
            Spawn();
        }
    }

    /// <summary>
    /// 코인 스폰 (마스터 클라이언트에서만 실행)
    /// </summary>
    private void Spawn()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        // 현재 오브젝트의 위치에서 위쪽으로 spawnHeight만큼 떨어진 위치 계산
        Vector3 spawnPosition = transform.position + Vector3.up * spawnHeight;

        // ✅ 동일한 시드로 난수 생성하여 모든 클라이언트에서 같은 코인이 생성되도록 보장
        // 재생성 횟수를 시드에 포함시켜 재생성마다 다른 랜덤 값 생성
        int baseSeed = 0;
        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null && pv.ViewID != 0)
        {
            // PhotonView가 있으면 ViewID를 기본 시드로 사용 (모든 클라이언트에서 동일)
            baseSeed = pv.ViewID;
        }
        else
        {
            // PhotonView가 없으면 위치 기반 해시를 기본 시드로 사용
            baseSeed = (int)(spawnPosition.x * 1000f) + (int)(spawnPosition.z * 1000f);
        }
        
        // ✅ 재생성 횟수를 시드에 추가하여 재생성마다 다른 랜덤 값 생성
        int seed = baseSeed + respawnCount;
        
        System.Random random = new System.Random(seed);
        float rand = (float)random.NextDouble(); // 0~1 난수
        
        GameObject prefabToSpawn;
        string prefabName; // ⭐ PhotonNetwork.Instantiate는 프리팹 이름(string)을 사용합니다.

        // 확률 구간에 따라 코인 종류 결정
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
        
        // ⭐ 4. PhotonNetwork.Instantiate를 사용하여 네트워크 객체 생성
        // 이 함수는 마스터 클라이언트가 호출하면 모든 클라이언트에 해당 객체를 동기화합니다.
        // 첫 번째 인자는 Resources 폴더 내의 프리팹 이름이어야 합니다.
        GameObject spawnedCoin = PhotonNetwork.Instantiate(
            "Prefabs/Coin/" + prefabName, // Resources 폴더 내의 프리팹 이름
            spawnPosition, 
            Quaternion.identity
        );
        
        Debug.Log($"[SpawnCoin] 코인 스폰: {prefabName} at {spawnPosition} (재생성 횟수: {respawnCount}, 시드: {seed}, 랜덤값: {rand})");
    }
    
    /// <summary>
    /// 코인 재생성 스케줄링 (마스터 클라이언트에서만 호출)
    /// </summary>
    public void ScheduleRespawn(float delay)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (isRespawning) return; // 이미 재생성 중이면 무시
        
        // 기존 코루틴이 있으면 취소
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
        }
        
        respawnCoroutine = StartCoroutine(RespawnAfterDelay(delay));
    }

    private IEnumerator RespawnAfterDelay(float delay)
    {
        if (!PhotonNetwork.IsMasterClient) yield break;
        
        isRespawning = true;
        yield return new WaitForSeconds(delay);
        
        // ✅ 재생성 횟수 증가 (재생성마다 다른 랜덤 값 생성)
        respawnCount++;
        
        Spawn();
        isRespawning = false;
        respawnCoroutine = null;
    }
    
    // ⭐ 마스터 클라이언트가 변경될 경우 새로운 마스터 클라이언트가 리스폰 관리를 이어받을 수 있도록 처리
    // 이 스크립트를 Photon.Pun.MonoBehaviourPunCallbacks를 상속받도록 변경해야 합니다.
    /* public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (newMasterClient.IsLocal && !alreadySpawned) // alreadySpawned 플래그는 추가적으로 구현해야 합니다.
        {
             // 새로 마스터가 된 클라이언트가 스폰을 이어받도록 처리 (상황에 따라)
        }
    }
    */
}