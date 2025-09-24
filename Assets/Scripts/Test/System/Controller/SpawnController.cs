using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnController : MonoBehaviourPunCallbacks
{
    [Header("스폰 위치 설정")]
    [SerializeField] private GameObject[] spawnPositions;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private Transform crownSpawnPosition;

    [Header("스폰 설정")]
    [SerializeField] private bool destroyPreviousCharacter = true;
    [SerializeField] private bool randomizeRotation = false;
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    [SerializeField] private float spawnDelay = 0.1f;
    [SerializeField] private GameObject crownPrefab;

    [Header("UI 프리팹")]
    [SerializeField] private GameObject hudPanelPrefab;
    [SerializeField] private GameObject shopPanelPrefab;
    [SerializeField] private GameObject itemModalPrefab;
    [SerializeField] private GameObject pausePanelPrefab;
    [SerializeField] private GameObject gameOverPanelPrefab;

    private DataBase.PlayerData playerData;
    private GameObject[] cachedPlayerPrefabData;
    private bool dataBaseCached = false;

    private GameObject currentSpawnedCharacter = null;
     private GameObject spawnedCrown;
    private int lastUsedSpawnIndex = -1;
    private bool isSpawning = false;
    private int currentSpawnedCharacterIndex = -1;
    private bool hasSpawnedPlayer = false;

    void Awake()
    {
        ValidateSpawnPositions();
        CacheDataBaseInfo();
        // SpawnSelectedCharacterOnAwake(); // 즉시 스폰 제거

    }

    void Start()
    {
        Debug.Log($"🔄 SpawnController: Start 호출 - 네트워크 상태: {PhotonNetwork.NetworkClientState}");
        
        // 네트워크 연결 상태 확인 후 크라운 스폰
        if(PhotonNetwork.IsMasterClient && PhotonNetwork.IsConnectedAndReady)
        {
            StartCoroutine(SpawnCrownCoroutine());
        }
        
        // OnJoinedRoom 콜백이 호출되지 않는 경우를 대비한 백업 로직
        if (PhotonNetwork.InRoom && !hasSpawnedPlayer)
        {
            Debug.Log("🔧 SpawnController: 백업 스폰 로직 실행 (OnJoinedRoom 미호출 대비)");
            StartCoroutine(BackupSpawnLogic());
        }
    }
    
    /// <summary>
    /// OnJoinedRoom 콜백이 호출되지 않는 경우를 대비한 백업 스폰 로직
    /// </summary>
    private IEnumerator BackupSpawnLogic()
    {
        // 약간 더 긴 대기 시간
        yield return new WaitForSeconds(2f);
        
        // 아직 스폰되지 않았고 방에 있다면 스폰 시도
        if (!hasSpawnedPlayer && !isSpawning && PhotonNetwork.InRoom)
        {
            Debug.Log("🚨 SpawnController: 백업 로직으로 캐릭터 스폰 시도");
            SpawnSelectedCharacterOnAwake();
        }
    }

    IEnumerator SpawnCrownCoroutine()
    {
        yield return new WaitForSeconds(3f);
        SpawnCrown();
    }

    void OnDrawGizmos()
    {
        if (spawnPositions == null) return;

        Gizmos.color = Color.green;

        for (int i = 0; i < spawnPositions.Length; i++)
        {
            if (spawnPositions[i] != null)
            {
                Vector3 position = spawnPositions[i].transform.position + spawnOffset;
                Gizmos.DrawWireSphere(position, 1f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(position + Vector3.up * 1.5f, $"Spawn {i}");
#endif
            }
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"🏠 SpawnController: 방 입장 완료!");
        Debug.Log($"   - 방 이름: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"   - 플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}");
        Debug.Log($"   - 마스터 클라이언트: {PhotonNetwork.IsMasterClient}");
        Debug.Log($"   - 네트워크 연결 상태: {PhotonNetwork.IsConnectedAndReady}");
        Debug.Log($"   - 현재 스폰 상태: hasSpawnedPlayer={hasSpawnedPlayer}, isSpawning={isSpawning}");
        
        // 네트워크 상태 확인
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogError("❌ SpawnController: 네트워크가 준비되지 않음!");
            return;
        }
        
        // 방 입장 후 잠시 대기 후 캐릭터 스폰
        StartCoroutine(WaitAndSpawnCharacter());
    }
    
    /// <summary>
    /// 방 입장 후 안정화를 위해 대기 후 캐릭터 스폰
    /// </summary>
    private IEnumerator WaitAndSpawnCharacter()
    {
        Debug.Log("⏳ SpawnController: 네트워크 안정화 대기 중...");
        
        // 네트워크 안정화 대기
        yield return new WaitForSeconds(1f); // 대기 시간을 1초로 증가
        
        Debug.Log($"🎯 SpawnController: 스폰 시도 - hasSpawnedPlayer={hasSpawnedPlayer}, isSpawning={isSpawning}");
        
        // 아직 스폰하지 않았다면 스폰
        if (!hasSpawnedPlayer && !isSpawning)
        {
            Debug.Log("🚀 SpawnController: 캐릭터 스폰 시작!");
            SpawnSelectedCharacterOnAwake();
        }
        else if (hasSpawnedPlayer)
        {
            Debug.Log("⚠️ SpawnController: 이미 스폰 완료됨");
        }
        else if (isSpawning)
        {
            Debug.Log("⚠️ SpawnController: 이미 스폰 진행 중");
        }
    }

    void CacheDataBaseInfo()
    {
        try
        {
            if (!dataBaseCached)
            {
                playerData = DataBase.Instance.playerData;
                cachedPlayerPrefabData = playerData.PlayerPrefabData.ToArray();
                dataBaseCached = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ SpawnController: DataBase 캐싱 중 오류: {e.Message}");
            dataBaseCached = false;
        }
    }

    void ValidateSpawnPositions()
    {
        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            Debug.LogError("❌ SpawnController: 스폰 위치가 설정되지 않았습니다!");
            return;
        }

        List<GameObject> validPositions = new List<GameObject>();
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            if (spawnPositions[i] != null)
            {
                validPositions.Add(spawnPositions[i]);
            }
        }

        spawnPositions = validPositions.ToArray();
    }

    public void SpawnCharacterPrefab(GameObject prefab)
    {
        if (isSpawning || prefab == null || spawnPositions.Length == 0) return;

        StartCoroutine(SpawnCharacterPrefabCoroutine(prefab));
    }

    public void SpawnCharacter(int characterIndex)
    {
        Debug.Log($"🎮 SpawnController: SpawnCharacter 호출 - 인덱스: {characterIndex}");
        
        if (isSpawning)
        {
            Debug.LogWarning("⚠️ SpawnController: 이미 스폰 진행 중이므로 중단");
            return;
        }

        CacheDataBaseInfo();
        
        Debug.Log($"📋 SpawnController: DataBase 캐싱 상태 - cached: {dataBaseCached}, prefabCount: {cachedPlayerPrefabData?.Length ?? 0}");

        if (!IsValidCharacterIndex(characterIndex))
        {
            Debug.LogError($"❌ SpawnController: 잘못된 캐릭터 인덱스: {characterIndex} (최대: {cachedPlayerPrefabData?.Length ?? 0})");
            return;
        }

        if (spawnPositions.Length == 0)
        {
            Debug.LogError("❌ SpawnController: 스폰 위치가 없습니다!");
            return;
        }

        GameObject prefab = cachedPlayerPrefabData[characterIndex];
        Debug.Log($"🎯 SpawnController: 프리팹 선택 완료 - {prefab?.name ?? "null"}");
        
        SpawnCharacterPrefab(prefab);
        currentSpawnedCharacterIndex = characterIndex;
    }

    IEnumerator SpawnCharacterPrefabCoroutine(GameObject prefab)
    {
        Debug.Log($"🏗️ SpawnController: 스폰 코루틴 시작 - {prefab?.name ?? "null"}");
        isSpawning = true;

        if (spawnDelay > 0f)
        {
            Debug.Log($"⏳ SpawnController: 스폰 지연 대기 - {spawnDelay}초");
            yield return new WaitForSeconds(spawnDelay);
        }

        if (destroyPreviousCharacter && currentSpawnedCharacter != null)
        {
            Debug.Log($"🗑️ SpawnController: 이전 캐릭터 제거 - {currentSpawnedCharacter.name}");
            if (currentSpawnedCharacter.GetComponent<PhotonView>() != null)
            {
                PhotonNetwork.Destroy(currentSpawnedCharacter);
            }
            else
            {
                Destroy(currentSpawnedCharacter);
            }
            currentSpawnedCharacter = null;
        }

        int spawnIndex = GetRandomSpawnIndex();
        Vector3 spawnPosition = GetSpawnPosition(spawnIndex);
        Quaternion spawnRotation = GetSpawnRotation(spawnIndex);
        
        Debug.Log($"📍 SpawnController: 스폰 위치 선택 - 인덱스: {spawnIndex}, 위치: {spawnPosition}");

        try
        {
            string prefabPath = GetPrefabResourcePath(prefab);
            Debug.Log($"📂 SpawnController: 프리팹 경로 - {prefabPath}");

            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError($"❌ SpawnController: 프리팹 {prefab.name}의 Resources 경로를 찾을 수 없습니다.");
                isSpawning = false;
                yield break;
            }

            Debug.Log($"🌐 SpawnController: PhotonNetwork.Instantiate 호출 중...");
            currentSpawnedCharacter = PhotonNetwork.Instantiate(prefabPath, spawnPosition, spawnRotation);
            Debug.Log($"✅ SpawnController: PhotonNetwork.Instantiate 성공 - {currentSpawnedCharacter.name}");

            PhotonView pv = currentSpawnedCharacter.GetComponent<PhotonView>();
            if(pv != null && pv.IsMine)
            {
                Debug.Log($"👤 SpawnController: 로컬 플레이어 캐릭터 생성됨 - ViewID: {pv.ViewID}");
                // HUD 패널은 약간의 지연을 두고 생성 (네트워크 안정화 대기)
                StartCoroutine(SpawnHUDWithDelay());
            }
            else if (pv != null)
            {
                Debug.Log($"🌐 SpawnController: 원격 플레이어 캐릭터 생성됨 - ViewID: {pv.ViewID}, Owner: {pv.Owner?.NickName ?? "Unknown"}");
            }

            if (spawnParent != null)
            {
                currentSpawnedCharacter.transform.SetParent(spawnParent);
                Debug.Log($"📁 SpawnController: 부모 오브젝트 설정 완료");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ SpawnController - 캐릭터 생성 실패: {e.Message}");
            Debug.LogError($"스택 트레이스: {e.StackTrace}");
            isSpawning = false;
            yield break;
        }

        NotifyGameManagerOfSpawnedCharacter();
        hasSpawnedPlayer = true; // 스폰 완료 플래그 설정
        isSpawning = false;
        
        Debug.Log($"🎉 SpawnController: 캐릭터 스폰 완료! - {currentSpawnedCharacter.name}");
    }

    private string GetPrefabResourcePath(GameObject prefab)
    {
        if (prefab == null) return null;

        string prefabName = prefab.name;
        
        if (prefabName.Contains("Player"))
        {
            return $"Prefabs/{prefabName}";
        }
        else if (prefabName.Contains("Character"))
        {
            return $"Prefabs/Character/{prefabName}";
        }
        else if (prefabName.Contains("Test"))
        {
            return $"Prefabs/{prefabName}";
        }
        
        return $"Prefabs/{prefabName}";
    }

    public void DestroyCurrentCharacter()
    {
        if (currentSpawnedCharacter != null)
        {
            Destroy(currentSpawnedCharacter);
            currentSpawnedCharacter = null;
        }
    }

    int GetRandomSpawnIndex()
    {
        if (spawnPositions.Length == 1)
            return 0;

        int randomIndex;
        int attempts = 0;

        do
        {
            randomIndex = Random.Range(0, spawnPositions.Length);
            attempts++;

            if (attempts > 10)
                break;

        } while (randomIndex == lastUsedSpawnIndex);

        lastUsedSpawnIndex = randomIndex;
        return randomIndex;
    }

    Vector3 GetSpawnPosition(int spawnIndex)
    {
        Vector3 basePosition = spawnPositions[spawnIndex].transform.position;
        return basePosition + spawnOffset;
    }

    Quaternion GetSpawnRotation(int spawnIndex)
    {
        if (randomizeRotation)
        {
            return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
        else
        {
            return spawnPositions[spawnIndex].transform.rotation;
        }
    }

    bool IsValidCharacterIndex(int index)
    {
        return cachedPlayerPrefabData != null &&
               index >= 0 &&
               index < cachedPlayerPrefabData.Length &&
               cachedPlayerPrefabData[index] != null;
    }

    public void AddSpawnPosition(GameObject newPosition)
    {
        if (newPosition == null) return;

        List<GameObject> positions = new List<GameObject>(spawnPositions);
        positions.Add(newPosition);
        spawnPositions = positions.ToArray();
    }

    public void RemoveSpawnPosition(GameObject position)
    {
        if (position == null) return;

        List<GameObject> positions = new List<GameObject>(spawnPositions);
        positions.Remove(position);
        spawnPositions = positions.ToArray();
    }

    public GameObject GetCurrentSpawnedCharacter()
    {
        return currentSpawnedCharacter;
    }

    public Skill GetCurrentSpawnedCharacterSkill()
    {
        if (currentSpawnedCharacter != null)
        {
            return currentSpawnedCharacter.GetComponent<Skill>();
        }
        return null;
    }

    public bool IsSpawning()
    {
        return isSpawning;
    }

    public int GetSpawnPositionCount()
    {
        return spawnPositions?.Length ?? 0;
    }

    public int GetAvailableCharacterCount()
    {
        return cachedPlayerPrefabData?.Length ?? 0;
    }

    void NotifyGameManagerOfSpawnedCharacter()
    {
        if (currentSpawnedCharacter != null)
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.FindPlayerAfterSpawn();
                gameManager.NotifyCharacterSpawned();
            }
        }
    }

    public int NotifyHUDOfCharacterSpawn()
    {
        return currentSpawnedCharacterIndex;
    }

    private void SpawnSelectedCharacterOnAwake()
    {
        if (hasSpawnedPlayer)
        {
            Debug.Log("SpawnController: 이미 스폰됨 - 중복 스폰 방지");
            return;
        }

        if (cachedPlayerPrefabData == null || cachedPlayerPrefabData.Length == 0)
        {
            Debug.LogError("❌ SpawnController: 플레이어 프리팹 데이터가 없습니다!");
            // DataBase 재캐싱 시도
            CacheDataBaseInfo();
            if (cachedPlayerPrefabData == null || cachedPlayerPrefabData.Length == 0)
            {
                Debug.LogError("❌ SpawnController: DataBase 재캐싱 후에도 프리팹 데이터가 없습니다!");
                return;
            }
        }

        int selectedCharacterIndex = PlayerPrefs.GetInt("SelectChar_CurrentIndex", 0);
        Debug.Log($"SpawnController: 캐릭터 스폰 시도 - 인덱스: {selectedCharacterIndex}");
        
        if (selectedCharacterIndex >= 0 && selectedCharacterIndex < cachedPlayerPrefabData.Length)
        {
            SpawnCharacter(selectedCharacterIndex);
        }
        else
        {
            Debug.LogWarning($"SpawnController: 잘못된 캐릭터 인덱스 {selectedCharacterIndex}, 기본값 0으로 스폰");
            SpawnCharacter(0);
        }
        
        // 스폰 완료 플래그는 SpawnCharacter 메서드 내에서 설정되므로 여기서는 제거
    }

    private void SpawnCrown()
    {
        string prefabName = crownPrefab.name;

        // RoomObject 모드로 생성 → 룸 내 모든 클라이언트가 공유
        spawnedCrown = PhotonNetwork.InstantiateRoomObject(
            $"Prefabs/{prefabName}",
            crownSpawnPosition.position,
            Quaternion.identity
        );
        
    }

    private string GetCrownPrefabResourcePath()
    {
        if(crownPrefab == null) return null;

        string prefabName = crownPrefab.name;

        if(Resources.Load($"Prefabs/{prefabName}") != null)
        {
            return $"Prefabs/{prefabName}";
        }
        else if(Resources.Load($"Prefabs/Items/{prefabName}") != null)
        {
            return $"Prefabs/Items/{prefabName}";
        }
        return null;
    }
    
    /// <summary>
    /// HUD 패널 지연 생성 (네트워크 안정화 대기)
    /// </summary>
    private IEnumerator SpawnHUDWithDelay()
    {
        yield return new WaitForSeconds(1f); // 1초 대기
        
        if (hudPanelPrefab != null)
        {
            GameObject hud = Instantiate(hudPanelPrefab);
            Debug.Log("SpawnController: HUD 패널 생성 완료");
        }
    }
}