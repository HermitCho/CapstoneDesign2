using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🎯 캐릭터 스폰 컨트롤러
/// 캐릭터 프리팹을 지정된 위치들 중 랜덤으로 스폰하는 시스템
/// </summary>
public class SpawnController : MonoBehaviour
{
    [Header("🎯 스폰 위치 설정")]
    [SerializeField] private GameObject[] spawnPositions; // 스폰 가능한 위치들
    [SerializeField] private Transform spawnParent; // 스폰된 캐릭터들의 부모 오브젝트 (선택적)
    
    [Header("📦 캐릭터 프리팹 데이터")]
    [SerializeField] private GameObject[] characterPrefabs; // 사용 가능한 캐릭터 프리팹들
    
    [Header("⚙️ 스폰 설정")]
    [SerializeField] private bool destroyPreviousCharacter = true; // 이전 캐릭터 제거 여부
    [SerializeField] private bool randomizeRotation = false; // 랜덤 회전 여부
    [SerializeField] private Vector3 spawnOffset = Vector3.zero; // 스폰 위치 오프셋
    [SerializeField] private float spawnDelay = 0.1f; // 스폰 딜레이
    
    [Header("🎮 디버그 설정")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.green;
    [SerializeField] private float gizmoSize = 1f;
    
    // 내부 상태 변수들
    private GameObject currentSpawnedCharacter = null;
    private int lastUsedSpawnIndex = -1;
    private bool isSpawning = false;
    
    #region Unity 생명주기
    
    void Awake()
    {
        ValidateSpawnPositions();
    }
    
    void Start()
    {
        if (debugMode)
            Debug.Log("🎯 SpawnController 초기화 완료");
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
    
    #region 초기화 및 검증
    
    /// <summary>
    /// 스폰 위치들 검증
    /// </summary>
    void ValidateSpawnPositions()
    {
        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            Debug.LogError("❌ SpawnController: 스폰 위치가 설정되지 않았습니다!");
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
                Debug.LogWarning($"⚠️ SpawnController: 스폰 위치 {i}번이 null입니다.");
            }
        }
        
        spawnPositions = validPositions.ToArray();
        
        if (debugMode)
            Debug.Log($"✅ SpawnController: {spawnPositions.Length}개의 유효한 스폰 위치 확인됨");
    }
    
    #endregion
    
    #region 캐릭터 스폰 메서드들
    
    public void SpawnCharacterPrefab(GameObject prefab)
    {
        if (isSpawning)
        {
            return;
        }
        
        if (prefab == null)
        {
            Debug.LogError("❌ SpawnController: 프리팹이 null입니다!");
            return;
        }
        
        if (spawnPositions.Length == 0)
        {
            Debug.LogError("❌ SpawnController: 스폰 위치가 없습니다!");
            return;
        }
        
        StartCoroutine(SpawnCharacterPrefabCoroutine(prefab));
    }
    
    public void SpawnCharacter(int characterIndex)
    {
        if (isSpawning)
        {
            return;
        }
        
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
        
        GameObject prefab = characterPrefabs[characterIndex];
        SpawnCharacterPrefab(prefab);
    }
    
    IEnumerator SpawnCharacterPrefabCoroutine(GameObject prefab)
    {
        isSpawning = true;
        
        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);
        
        if (destroyPreviousCharacter && currentSpawnedCharacter != null)
        {
            DestroyCurrentCharacter();
        }
        
        int spawnIndex = GetRandomSpawnIndex();
        Vector3 spawnPosition = GetSpawnPosition(spawnIndex);
        Quaternion spawnRotation = GetSpawnRotation(spawnIndex);
        
        currentSpawnedCharacter = Instantiate(prefab, spawnPosition, spawnRotation);
        
        if (spawnParent != null)
        {
            currentSpawnedCharacter.transform.SetParent(spawnParent);
        }
        
        currentSpawnedCharacter.name = $"{prefab.name}_Spawned";
        
        NotifyGameManagerOfSpawnedCharacter();
        
        isSpawning = false;
    }
    
    IEnumerator SpawnCharacterCoroutine(int characterIndex)
    {
        GameObject prefab = characterPrefabs[characterIndex];
        yield return StartCoroutine(SpawnCharacterPrefabCoroutine(prefab));
    }
    
    public void DestroyCurrentCharacter()
    {
        if (currentSpawnedCharacter != null)
        {
            Destroy(currentSpawnedCharacter);
            currentSpawnedCharacter = null;
        }
    }
    
    #endregion
    
    #region 스폰 위치 관리
    
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
        return characterPrefabs != null && 
               index >= 0 && 
               index < characterPrefabs.Length && 
               characterPrefabs[index] != null;
    }
    
    /// <summary>
    /// 특정 스폰 위치에 캐릭터 스폰 (디버그용)
    /// </summary>
    public void SpawnCharacterAtPosition(int characterIndex, int spawnIndex)
    {
        if (!IsValidCharacterIndex(characterIndex))
        {
            Debug.LogError($"❌ SpawnController: 잘못된 캐릭터 인덱스: {characterIndex}");
            return;
        }
        
        if (spawnIndex < 0 || spawnIndex >= spawnPositions.Length)
        {
            Debug.LogError($"❌ SpawnController: 잘못된 스폰 인덱스: {spawnIndex}");
            return;
        }
        
        StartCoroutine(SpawnCharacterAtPositionCoroutine(characterIndex, spawnIndex));
    }
    
    /// <summary>
    /// 특정 위치 스폰 코루틴
    /// </summary>
    IEnumerator SpawnCharacterAtPositionCoroutine(int characterIndex, int spawnIndex)
    {
        isSpawning = true;
        
        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);
        
        if (destroyPreviousCharacter && currentSpawnedCharacter != null)
        {
            DestroyCurrentCharacter();
        }
        
        Vector3 spawnPosition = GetSpawnPosition(spawnIndex);
        Quaternion spawnRotation = GetSpawnRotation(spawnIndex);
        
        GameObject prefab = characterPrefabs[characterIndex];
        
        if (prefab != null)
        {
            currentSpawnedCharacter = Instantiate(prefab, spawnPosition, spawnRotation);
            
            if (spawnParent != null)
            {
                currentSpawnedCharacter.transform.SetParent(spawnParent);
            }
            
            currentSpawnedCharacter.name = $"{prefab.name}_Spawned_At_{spawnIndex}";
        }
        
        isSpawning = false;
    }
    
    #endregion
    
    #region 공개 메서드들
    
    /// <summary>
    /// 스폰 위치 추가
    /// </summary>
    public void AddSpawnPosition(GameObject newPosition)
    {
        if (newPosition == null) return;
        
        List<GameObject> positions = new List<GameObject>(spawnPositions);
        positions.Add(newPosition);
        spawnPositions = positions.ToArray();
    }
    
    /// <summary>
    /// 스폰 위치 제거
    /// </summary>
    public void RemoveSpawnPosition(GameObject position)
    {
        if (position == null) return;
        
        List<GameObject> positions = new List<GameObject>(spawnPositions);
        positions.Remove(position);
        spawnPositions = positions.ToArray();
    }
    
    /// <summary>
    /// 현재 스폰된 캐릭터 반환
    /// </summary>
    public GameObject GetCurrentSpawnedCharacter()
    {
        return currentSpawnedCharacter;
    }
    
    /// <summary>
    /// 스폰 중인지 확인
    /// </summary>
    public bool IsSpawning()
    {
        return isSpawning;
    }
    
    /// <summary>
    /// 스폰 위치 개수 반환
    /// </summary>
    public int GetSpawnPositionCount()
    {
        return spawnPositions?.Length ?? 0;
    }
    
    /// <summary>
    /// 사용 가능한 캐릭터 프리팹 개수 반환
    /// </summary>
    public int GetAvailableCharacterCount()
    {
        return characterPrefabs?.Length ?? 0;
    }
    
    void NotifyGameManagerOfSpawnedCharacter()
    {
        if (currentSpawnedCharacter != null)
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.FindPlayerAfterSpawn();
            }
        }
    }
    
    #endregion
} 