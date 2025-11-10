using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;


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

    [Header(" AI 스폰 설정")]
    [SerializeField] private bool spawnAI = false;
    [SerializeField] private GameObject[] aiPrefabs;

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
    //수정 시작
    private bool botsSpawned = false;

    private readonly List<GameObject> spawnedBots = new List<GameObject>();
    private readonly HashSet<int> reservedHumanSpawnIndices = new HashSet<int>();
    private readonly HashSet<int> reservedBotSpawnIndices = new HashSet<int>();

    private const string RoomBotCountKey = "botFillCount";
    private const float BotSpawnClearRadius = 1.5f;
    private int pendingBotCount = 0;
    private string currentGamePhase = string.Empty;
    private Coroutine botSpawnCoroutine = null;
    //수정 끝
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
            //수정
            InitializeBotSpawnState();
        }
        
        // OnJoinedRoom 콜백이 호출되지 않는 경우를 대비한 백업 로직
        if (PhotonNetwork.InRoom && !hasSpawnedPlayer)
        {
            StartCoroutine(BackupSpawnLogic());
        }
    }
    
    void Update()
    {
        if (!hasSpawnedPlayer && !isSpawning && PhotonNetwork.InRoom)
        {
            CheckSpawnTrigger();
        }
    }
    
    /// <summary>
    /// Room Properties에서 스폰 트리거 확인
    /// </summary>
    private void CheckSpawnTrigger()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("spawnCharacters", out object spawnFlag))
        {
            if (spawnFlag is bool shouldSpawn && shouldSpawn)
            {
                SpawnSelectedCharacterOnAwake();
            }
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
            CheckSpawnTrigger();
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
        
        CheckSpawnTrigger();
        //수정 시작
        if (PhotonNetwork.IsMasterClient)
        {
            InitializeBotSpawnState();
        }
        //수정 끝
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
                SetPlayerNicknameProperty();
                DisableCharacterControlsOnSpawn(currentSpawnedCharacter);
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
        hasSpawnedPlayer = true;
        isSpawning = false;
        //수정
        MarkHumanSpawnReserved(spawnIndex);
    }
    
    /// <summary>
    /// 캐릭터 스폰 시 모든 컨트롤 비활성화
    /// </summary>
    private void DisableCharacterControlsOnSpawn(GameObject character)
    {
        if (character == null) return;
        
        MoveController moveController = character.GetComponent<MoveController>();
        if (moveController != null)
        {
            moveController.DisableMoveControls();
        }
        
        SkillController skillController = character.GetComponent<SkillController>();
        if (skillController != null)
        {
            skillController.DisableSkillControls();
        }
        
        TestGun gun = character.GetComponentInChildren<TestGun>();
        if (gun != null)
        {
            gun.enabled = false;
        }
        
        CameraController cameraController = character.GetComponent<CameraController>();
        if (cameraController != null)
        {
            cameraController.enabled = true;
        }
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
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
            //수정
            randomIndex = UnityEngine.Random.Range(0, spawnPositions.Length);
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
            //수정
            return Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
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

    //수정 시작
    private void InitializeBotSpawnState()
    {
        botsSpawned = false;
        pendingBotCount = 0;
        currentGamePhase = string.Empty;

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        ExitGames.Client.Photon.Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;
        if (properties != null)
        {
            if (properties.TryGetValue(RoomBotCountKey, out object botCountObj))
            {
                pendingBotCount = ConvertBotCount(botCountObj);
            }

            if (properties.TryGetValue("gamePhase", out object phaseObj))
            {
                currentGamePhase = phaseObj as string ?? string.Empty;
            }
        }

        TryScheduleBotSpawn();
    }

    private void TryScheduleBotSpawn()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (!spawnAI || aiPrefabs == null || aiPrefabs.Length == 0)
        {
            return;
        }

        if (pendingBotCount <= 0)
        {
            botsSpawned = true;
            return;
        }

        if (!string.Equals(currentGamePhase, "PLAYING", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            return;
        }

        if (botSpawnCoroutine != null)
        {
            return;
        }

        botSpawnCoroutine = StartCoroutine(SpawnBotsAfterDelay(0.5f));
    }

    private IEnumerator SpawnBotsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        botSpawnCoroutine = null;
        SpawnBotsInternal();
    }

    private void SpawnBotsInternal()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (pendingBotCount <= 0)
        {
            botsSpawned = true;
            return;
        }

        ReserveHumanSpawnIndices();

        int spawnedCount = 0;

        for (int i = 0; i < pendingBotCount; i++)
        {
            GameObject prefab = aiPrefabs[UnityEngine.Random.Range(0, aiPrefabs.Length)];
            if (prefab == null)
            {
                continue;
            }

            if (!TryGetBotSpawnPoint(out Vector3 spawnPosition, out Quaternion spawnRotation, out int spawnIndex))
            {
                continue;
            }

            string resourcePath = GetAIPrefabResourcePath(prefab);
            if (string.IsNullOrEmpty(resourcePath))
            {
                continue;
            }

            GameObject botInstance = PhotonNetwork.Instantiate(resourcePath, spawnPosition, spawnRotation);
            if (botInstance != null)
            {
                spawnedBots.Add(botInstance);
                if (spawnIndex >= 0)
                {
                    reservedBotSpawnIndices.Add(spawnIndex);
                }
                spawnedCount++;
            }
        }

        pendingBotCount = Mathf.Max(0, pendingBotCount - spawnedCount);
        botsSpawned = pendingBotCount <= 0;

        if (PhotonNetwork.CurrentRoom != null)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { RoomBotCountKey, pendingBotCount }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        if (!botsSpawned)
        {
            TryScheduleBotSpawn();
        }
    }

    private int ConvertBotCount(object value)
    {
        if (value == null)
        {
            return 0;
        }

        switch (value)
        {
            case int i:
                return Mathf.Max(0, i);
            case byte b:
                return Mathf.Max(0, (int)b);
            case short s:
                return Mathf.Max(0, (int)s);
            case long l:
                return Mathf.Max(0, (int)l);
            case string str when int.TryParse(str, out int parsed):
                return Mathf.Max(0, parsed);
            default:
                return 0;
        }
    }

    private bool TryGetBotSpawnPoint(out Vector3 position, out Quaternion rotation, out int index)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        index = -1;

        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            return false;
        }

        List<int> indices = new List<int>();
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            indices.Add(i);
        }
        ShuffleIndices(indices);

        foreach (int candidate in indices)
        {
            if (reservedHumanSpawnIndices.Contains(candidate) || reservedBotSpawnIndices.Contains(candidate))
            {
                continue;
            }

            Vector3 candidatePosition = GetSpawnPosition(candidate);
            if (!IsSpawnPointClear(candidatePosition))
            {
                continue;
            }

            position = candidatePosition;
            rotation = GetSpawnRotation(candidate);
            index = candidate;
            return true;
        }

        return false;
    }

    private void ShuffleIndices(List<int> indices)
    {
        for (int i = 0; i < indices.Count - 1; i++)
        {
            int swapIndex = UnityEngine.Random.Range(i, indices.Count);
            int temp = indices[i];
            indices[i] = indices[swapIndex];
            indices[swapIndex] = temp;
        }
    }

    private bool IsSpawnPointClear(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, BotSpawnClearRadius, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit == null || hit.transform == null)
            {
                continue;
            }

            if (hit.CompareTag("Player") || hit.GetComponent<LivingEntity>() != null || hit.GetComponent<BotLivingEntity>() != null)
            {
                return false;
            }
        }
        return true;
    }

    private string GetAIPrefabResourcePath(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        string prefabName = prefab.name;
        string[] searchPaths = new[]
        {
            $"Prefabs/InGameBot/{prefabName}",
            $"Prefabs/Bots/{prefabName}",
            $"Prefabs/{prefabName}"
        };

        foreach (string path in searchPaths)
        {
            if (Resources.Load(path) != null)
            {
                return path;
            }
        }

        return null;
    }

    private void ReserveHumanSpawnIndices()
    {
        reservedHumanSpawnIndices.Clear();
        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            return;
        }

        Player[] players = PhotonNetwork.PlayerList;
        foreach (Player player in players)
        {
            if (player == null)
            {
                continue;
            }

            int actorNumber = player.ActorNumber;
            int index = (actorNumber - 1) % spawnPositions.Length;
            reservedHumanSpawnIndices.Add(index);
        }
    }

    private void MarkHumanSpawnReserved(int spawnIndex)
    {
        if (spawnIndex >= 0)
        {
            reservedHumanSpawnIndices.Add(spawnIndex);
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);

        if (!PhotonNetwork.IsMasterClient || propertiesThatChanged == null)
        {
            return;
        }

        bool shouldCheckSpawn = false;

        if (propertiesThatChanged.ContainsKey(RoomBotCountKey))
        {
            pendingBotCount = ConvertBotCount(propertiesThatChanged[RoomBotCountKey]);
            if (pendingBotCount > 0)
            {
                botsSpawned = false;
            }
            shouldCheckSpawn = true;
        }

        if (propertiesThatChanged.ContainsKey("gamePhase"))
        {
            currentGamePhase = propertiesThatChanged["gamePhase"] as string ?? string.Empty;
            shouldCheckSpawn = true;
        }

        if (shouldCheckSpawn)
        {
            TryScheduleBotSpawn();
        }
    }
    //수정 끝
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