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

    // UI 프리팹 제거 - HeatUI PanelManager 사용

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
        // 네트워크 연결 상태 확인 후 크라운 스폰
        if(PhotonNetwork.IsMasterClient && PhotonNetwork.IsConnectedAndReady)
        {
            StartCoroutine(SpawnCrownCoroutine());
        }
        
        // OnJoinedRoom 콜백이 호출되지 않는 경우를 대비한 백업 로직
        if (PhotonNetwork.InRoom && !hasSpawnedPlayer)
        {
            StartCoroutine(BackupSpawnLogic());
        }
    }
    
    void Update()
    {
        // Room Properties 기반으로 게임 시작 감지
        if (!hasSpawnedPlayer && !isSpawning && PhotonNetwork.InRoom)
        {
            CheckGamePhaseAndSpawn();
        }
    }
    
    // OnDestroy 제거 - 이벤트 구독 없음
    
    /// <summary>
    /// OnJoinedRoom 콜백이 호출되지 않는 경우를 대비한 백업 스폰 로직
    /// </summary>
    private IEnumerator BackupSpawnLogic()
    {
        yield return new WaitForSeconds(2f);
        
        if (!hasSpawnedPlayer && !isSpawning && PhotonNetwork.InRoom)
        {
            CheckGamePhaseAndSpawn();
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
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            return;
        }
        
        CheckGamePhaseAndSpawn();
    }
    
    /// <summary>
    /// 게임 단계를 확인하고 적절한 시점에 스폰
    /// </summary>
    private void CheckGamePhaseAndSpawn()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gamePhase", out object phase))
        {
            string gamePhase = phase.ToString();
            
            if (gamePhase == "PLAYING")
            {
                StartCoroutine(WaitAndSpawnCharacter());
            }
            // READY 상태에서는 대기 (Room Properties 변경으로 PLAYING 감지)
        }
        else
        {
            // 게임 단계가 설정되지 않은 경우 기본 동작
            StartCoroutine(WaitAndSpawnCharacter());
        }
    }
    
    // OnGameActuallyStarted 메서드 제거 - Room Properties 기반으로 변경
    
    /// <summary>
    /// 방 입장 후 안정화를 위해 대기 후 캐릭터 스폰
    /// </summary>
    private IEnumerator WaitAndSpawnCharacter()
    {
        yield return new WaitForSeconds(1f);
        
        if (!hasSpawnedPlayer && !isSpawning)
        {
            SpawnSelectedCharacterOnAwake();
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
            Debug.LogError($"SpawnController: DataBase 캐싱 중 오류: {e.Message}");
            dataBaseCached = false;
        }
    }

    void ValidateSpawnPositions()
    {
        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            Debug.LogError("SpawnController: 스폰 위치가 설정되지 않았습니다!");
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
        if (isSpawning)
        {
            return;
        }

        CacheDataBaseInfo();

        if (!IsValidCharacterIndex(characterIndex))
        {
            Debug.LogError($"SpawnController: 잘못된 캐릭터 인덱스: {characterIndex}");
            return;
        }

        if (spawnPositions.Length == 0)
        {
            Debug.LogError("SpawnController: 스폰 위치가 없습니다!");
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
        {
            yield return new WaitForSeconds(spawnDelay);
        }

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

        int spawnIndex = GetPlayerSpawnIndex(); // 고유 스폰 위치 사용
        Vector3 spawnPosition = GetSpawnPosition(spawnIndex);
        Quaternion spawnRotation = GetSpawnRotation(spawnIndex);

        try
        {
            currentSpawnedCharacter = PhotonNetwork.Instantiate($"Prefabs/InGameCharacter/{prefab.name}", spawnPosition, spawnRotation);

            PhotonView pv = currentSpawnedCharacter.GetComponent<PhotonView>();
            if(pv != null && pv.IsMine)
            {
                // 로컬 플레이어 스폰 시 닉네임을 Photon Custom Properties에 설정
                SetPlayerNicknameProperty();
            }

            if (spawnParent != null)
            {
                currentSpawnedCharacter.transform.SetParent(spawnParent);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SpawnController - 캐릭터 생성 실패: {e.Message}");
            isSpawning = false;
            yield break;
        }

        NotifyGameManagerOfSpawnedCharacter();
        hasSpawnedPlayer = true; // 스폰 완료 플래그 설정
        isSpawning = false;
    }
    
    /// <summary>
    /// 플레이어 닉네임을 Photon Custom Properties에 설정
    /// </summary>
    private void SetPlayerNicknameProperty()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null) return;
        
        string nickname = "";
        
        // 1. CurrentUser에서 닉네임 가져오기 (우선순위 1)
        if (CurrentUser.Instance != null && CurrentUser.Instance.IsLoggedIn())
        {
            nickname = CurrentUser.Instance.GetNickname();
        }
        
        // 2. PlayerPrefs에서 닉네임 가져오기 (우선순위 2)
        if (string.IsNullOrEmpty(nickname))
        {
            nickname = PlayerPrefs.GetString("NickName", "");
        }
        
        // 3. 기본값 설정 (우선순위 3)
        if (string.IsNullOrEmpty(nickname))
        {
            nickname = $"Player{PhotonNetwork.LocalPlayer.ActorNumber}";
        }
        
        // Photon Custom Properties에 닉네임 설정
        var props = new ExitGames.Client.Photon.Hashtable();
        props["nickname"] = nickname;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        
        // PhotonNetwork.NickName도 설정 (Photon 기본 시스템 호환)
        PhotonNetwork.NickName = nickname;
    }

    public void DestroyCurrentCharacter()
    {
        if (currentSpawnedCharacter != null)
        {
            Destroy(currentSpawnedCharacter);
            currentSpawnedCharacter = null;
        }
    }

    /// <summary>
    /// 플레이어별 고유 스폰 위치 인덱스 할당
    /// </summary>
    int GetPlayerSpawnIndex()
    {
        if (spawnPositions.Length == 1)
            return 0;
            
        // 플레이어 ActorNumber를 기반으로 고유 스폰 위치 할당
        int playerActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        int spawnIndex = (playerActorNumber - 1) % spawnPositions.Length;
        
        return spawnIndex;
    }
    
    /// <summary>
    /// 기존 랜덤 스폰 인덱스 (백업용)
    /// </summary>
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
            return;
        }

        if (cachedPlayerPrefabData == null || cachedPlayerPrefabData.Length == 0)
        {
            Debug.LogError("SpawnController: 플레이어 프리팹 데이터가 없습니다!");
            CacheDataBaseInfo();
            if (cachedPlayerPrefabData == null || cachedPlayerPrefabData.Length == 0)
            {
                return;
            }
        }

        int selectedCharacterIndex = PlayerPrefs.GetInt("SelectChar_CurrentIndex", 0);
        
        if (selectedCharacterIndex >= 0 && selectedCharacterIndex < cachedPlayerPrefabData.Length)
        {
            SpawnCharacter(selectedCharacterIndex);
        }
        else
        {
            SpawnCharacter(0);
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
    
    // SpawnHUDWithDelay 메서드 제거 - HeatUI PanelManager 사용
}