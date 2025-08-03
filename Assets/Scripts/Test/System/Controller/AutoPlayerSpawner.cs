using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic; // Added for List

/// <summary>
/// 🎯 자동 플레이어 스폰 시스템
/// 게임 씬에 입장한 플레이어를 자동으로 스폰하는 시스템
/// </summary>
public class AutoPlayerSpawner : MonoBehaviourPunCallbacks
{
    [Header("🎯 스폰 설정")]
    [SerializeField] private GameObject[] spawnPositions; // 스폰 가능한 위치들
    [SerializeField] private Transform spawnParent; // 스폰된 캐릭터들의 부모 오브젝트
    [SerializeField] private Vector3 spawnOffset = Vector3.zero; // 스폰 위치 오프셋
    [SerializeField] private bool randomizeRotation = false; // 랜덤 회전 여부

    [Header("🎮 디버그 설정")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.green;
    [SerializeField] private float gizmoSize = 1f;

    // 데이터베이스 참조
    private DataBase.PlayerData playerData;
    private GameObject[] cachedPlayerPrefabData;
    private bool dataBaseCached = false;

    // 내부 상태 변수들
    private bool hasSpawnedPlayer = false; // 플레이어가 이미 스폰되었는지 확인
    private GameObject currentSpawnedCharacter = null;

    #region Unity 생명주기

    void Awake()
    {
        ValidateSpawnPositions();
    }

    void Start()
    {
        if (debugMode)
            Debug.Log("[AutoPlayerSpawner] 🎯 자동 플레이어 스폰 시스템 초기화");

        // 방에 이미 입장되어 있다면 자동 스폰 시도
        if (PhotonNetwork.InRoom && !hasSpawnedPlayer)
        {
            StartCoroutine(AutoSpawnPlayerOnStart());
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || spawnPositions == null) return;

        Gizmos.color = gizmoColor;

        for (int i = 0; i < spawnPositions.Length; i++)
        {
            if (spawnPositions[i] != null)
            {
                Vector3 position = spawnPositions[i].transform.position + spawnOffset;
                Gizmos.DrawWireSphere(position, gizmoSize);

                // 스폰 포인트 번호 표시 (Scene 뷰에서)
#if UNITY_EDITOR
                UnityEditor.Handles.Label(position + Vector3.up * (gizmoSize + 0.5f), $"Spawn {i}");
#endif
            }
        }
    }

    #endregion

    #region Photon 콜백

    public override void OnJoinedRoom()
    {
        Debug.Log("[AutoPlayerSpawner] 🎉 방 입장 감지!");
        
        if (!hasSpawnedPlayer)
        {
            StartCoroutine(AutoSpawnPlayerOnJoinRoom());
        }
    }

    #endregion

    #region 자동 스폰 시스템

    private IEnumerator AutoSpawnPlayerOnStart()
    {
        // 씬 로드 완료 대기
        yield return new WaitForSeconds(0.5f);
        
        if (PhotonNetwork.InRoom && !hasSpawnedPlayer)
        {
            Debug.Log("[AutoPlayerSpawner] 시작 시 자동 플레이어 스폰 시도");
            AutoSpawnPlayer();
        }
    }

    private IEnumerator AutoSpawnPlayerOnJoinRoom()
    {
        // 방 입장 후 약간의 지연
        yield return new WaitForSeconds(0.2f);
        
        if (PhotonNetwork.InRoom && !hasSpawnedPlayer)
        {
            Debug.Log("[AutoPlayerSpawner] 방 입장 시 자동 플레이어 스폰 시도");
            AutoSpawnPlayer();
        }
    }

    private void AutoSpawnPlayer()
    {
        if (hasSpawnedPlayer) return;

        // DataBase 캐싱 확인
        CacheDataBaseInfo();

        if (cachedPlayerPrefabData == null || cachedPlayerPrefabData.Length == 0)
        {
            Debug.LogError("[AutoPlayerSpawner] 플레이어 프리팹 데이터가 없습니다!");
            return;
        }

        Debug.Log($"[AutoPlayerSpawner] 사용 가능한 프리팹 개수: {cachedPlayerPrefabData.Length}");
        for (int i = 0; i < cachedPlayerPrefabData.Length; i++)
        {
            if (cachedPlayerPrefabData[i] != null)
            {
                Debug.Log($"[AutoPlayerSpawner] 프리팹 {i}: {cachedPlayerPrefabData[i].name}");
            }
        }

        // 기본적으로 첫 번째 캐릭터를 스폰 (또는 랜덤 선택)
        int characterIndex = Random.Range(0, cachedPlayerPrefabData.Length);
        
        Debug.Log($"[AutoPlayerSpawner] 자동 스폰 - 캐릭터 인덱스: {characterIndex}, 프리팹: {cachedPlayerPrefabData[characterIndex]?.name}");
        
        SpawnCharacter(characterIndex);
        hasSpawnedPlayer = true;
    }

    #endregion

    #region 초기화 및 검증

    void CacheDataBaseInfo()
    {
        try
        {
            if (!dataBaseCached)
            {
                playerData = DataBase.Instance.playerData;
                cachedPlayerPrefabData = playerData.PlayerPrefabData.ToArray();
                dataBaseCached = true;
                Debug.Log("✅ AutoPlayerSpawner - DataBase 정보 캐싱 완료");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ AutoPlayerSpawner: DataBase 캐싱 중 오류: {e.Message}");
            dataBaseCached = false;
        }
    }

    /// <summary>
    /// 스폰 위치들 검증
    /// </summary>
    void ValidateSpawnPositions()
    {
        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            Debug.LogError("❌ AutoPlayerSpawner: 스폰 위치가 설정되지 않았습니다!");
            return;
        }

        // null 위치 제거
        List<GameObject> validPositions = new List<GameObject>();
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            if (spawnPositions[i] != null)
            {
                validPositions.Add(spawnPositions[i]);
            }
            else if (debugMode)
            {
                Debug.LogWarning($"⚠️ AutoPlayerSpawner: 스폰 위치 {i}번이 null입니다.");
            }
        }

        spawnPositions = validPositions.ToArray();

        if (debugMode)
            Debug.Log($"✅ AutoPlayerSpawner: {spawnPositions.Length}개의 유효한 스폰 위치 확인됨");
    }

    #endregion

    #region 캐릭터 스폰 메서드들

    public void SpawnCharacter(int characterIndex)
    {
        // DataBase 캐싱 확인
        CacheDataBaseInfo();

        if (!IsValidCharacterIndex(characterIndex))
        {
            Debug.LogError($"❌ AutoPlayerSpawner: 잘못된 캐릭터 인덱스: {characterIndex}");
            return;
        }

        if (spawnPositions.Length == 0)
        {
            Debug.LogError("❌ AutoPlayerSpawner: 스폰 위치가 없습니다!");
            return;
        }

        GameObject prefab = cachedPlayerPrefabData[characterIndex];
        StartCoroutine(SpawnCharacterPrefabCoroutine(prefab));

        Debug.Log($"✅ AutoPlayerSpawner: 캐릭터 인덱스 {characterIndex} 스폰 시작");
    }

    IEnumerator SpawnCharacterPrefabCoroutine(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("❌ AutoPlayerSpawner: 프리팹이 null입니다!");
            yield break;
        }

        try
        {
            Debug.Log($"🔍 AutoPlayerSpawner - 스폰할 프리팹: {prefab?.name}");

            // 스폰 위치 선택
            int spawnIndex = GetRandomSpawnIndex();
            Vector3 spawnPosition = GetSpawnPosition(spawnIndex);
            Quaternion spawnRotation = GetSpawnRotation(spawnIndex);

            // 프리팹 경로 생성
            string prefabPath = GetPrefabResourcePath(prefab);

            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError($"❌ AutoPlayerSpawner: 프리팹 {prefab.name}의 Resources 경로를 찾을 수 없습니다.");
                yield break;
            }

            Debug.Log($"[AutoPlayerSpawner] PhotonNetwork.Instantiate 호출: {prefabPath}");
            currentSpawnedCharacter = PhotonNetwork.Instantiate(prefabPath, spawnPosition, spawnRotation);

            if (spawnParent != null)
            {
                currentSpawnedCharacter.transform.SetParent(spawnParent);
            }

            Debug.Log($"✅ AutoPlayerSpawner - 네트워크 캐릭터 스폰 완료: {currentSpawnedCharacter.name}, PhotonViewID: {currentSpawnedCharacter.GetComponent<PhotonView>()?.ViewID}");

            // GameManager 알림
            NotifyGameManagerOfSpawnedCharacter();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ AutoPlayerSpawner - 캐릭터 생성 실패: {e.Message}");
        }
    }

    private string GetPrefabResourcePath(GameObject prefab)
    {
        if (prefab == null) return null;

        string prefabName = prefab.name;
        
        // 프리팹 이름에 따라 경로 결정
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
        
        // 기본적으로 Prefabs 폴더에 있다고 가정
        return $"Prefabs/{prefabName}";
    }

    #endregion

    #region 스폰 위치 관리

    int GetRandomSpawnIndex()
    {
        if (spawnPositions.Length == 1)
            return 0;

        int randomIndex;
        int attempts = 0;
        int lastUsedSpawnIndex = -1;

        do
        {
            randomIndex = Random.Range(0, spawnPositions.Length);
            attempts++;

            if (attempts > 10)
                break;

        } while (randomIndex == lastUsedSpawnIndex);

        return randomIndex;
    }

    /// <summary>
    /// 스폰 위치 계산
    /// </summary>
    Vector3 GetSpawnPosition(int spawnIndex)
    {
        Vector3 basePosition = spawnPositions[spawnIndex].transform.position;
        return basePosition + spawnOffset;
    }

    /// <summary>
    /// 스폰 회전 계산
    /// </summary>
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

    #endregion

    #region 유틸리티 메서드들

    /// <summary>
    /// 유효한 캐릭터 인덱스인지 확인
    /// </summary>
    bool IsValidCharacterIndex(int index)
    {
        return cachedPlayerPrefabData != null &&
               index >= 0 &&
               index < cachedPlayerPrefabData.Length &&
               cachedPlayerPrefabData[index] != null;
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
                Debug.Log("✅ AutoPlayerSpawner - 캐릭터 스폰 완료, GameManager에 알림");
            }
        }
    }

    #endregion
} 