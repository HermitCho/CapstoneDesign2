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
        SpawnSelectedCharacterOnAwake();

    }

    void Start()
    {
        if(PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(SpawnCrownCoroutine());
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

        //SpawnCrown();
        // 캐릭터는 이미 Awake에서 스폰되었으므로 추가 작업 불필요
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
        if (isSpawning) return;

        CacheDataBaseInfo();

        if (!IsValidCharacterIndex(characterIndex))
        {
            Debug.LogError($"❌ SpawnController: 잘못된 캐릭터 인덱스: {characterIndex}");
            return;
        }

        if (spawnPositions.Length == 0)
        {
            Debug.LogError("❌ SpawnController: 스폰 위치가 없습니다!");
            return;
        }

        GameObject prefab = cachedPlayerPrefabData[characterIndex];
        SpawnCharacterPrefab(prefab);
        currentSpawnedCharacterIndex = characterIndex;
    }

    IEnumerator SpawnCharacterPrefabCoroutine(GameObject prefab)
    {
        isSpawning = true;

        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        if (destroyPreviousCharacter && currentSpawnedCharacter != null)
        {
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

        try
        {
            string prefabPath = GetPrefabResourcePath(prefab);

            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError($"❌ SpawnController: 프리팹 {prefab.name}의 Resources 경로를 찾을 수 없습니다.");
                isSpawning = false;
                yield break;
            }

            currentSpawnedCharacter = PhotonNetwork.Instantiate(prefabPath, spawnPosition, spawnRotation);

            PhotonView pv = currentSpawnedCharacter.GetComponent<PhotonView>();
            if(pv.IsMine)
            {
                GameObject hud = Instantiate(hudPanelPrefab);
            }

            if (spawnParent != null)
            {
                currentSpawnedCharacter.transform.SetParent(spawnParent);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ SpawnController - 캐릭터 생성 실패: {e.Message}");
            isSpawning = false;
            yield break;
        }

        NotifyGameManagerOfSpawnedCharacter();
        isSpawning = false;
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
        if (hasSpawnedPlayer) return;

        if (cachedPlayerPrefabData == null || cachedPlayerPrefabData.Length == 0)
        {
            Debug.LogError("[SpawnController] 플레이어 프리팹 데이터가 없습니다!");
            return;
        }

        int selectedCharacterIndex = PlayerPrefs.GetInt("SelectChar_CurrentIndex", 0);
        
        if (selectedCharacterIndex >= 0 && selectedCharacterIndex < cachedPlayerPrefabData.Length)
        {
            SpawnCharacter(selectedCharacterIndex);
            hasSpawnedPlayer = true;
        }
        else
        {
            SpawnCharacter(0);
            hasSpawnedPlayer = true;
        }
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
}