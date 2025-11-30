using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; 
using Photon.Pun;
using Unity.AI.Navigation;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapGenerator : MonoBehaviourPunCallbacks
{
    // --- 설정 변수 ---
    [Header("Map Prefabs & Size")]
    [Tooltip("맵 조각 프리팹 목록입니다. 인덱스 0: 스폰 맵, 1: 상점 맵, 2: 중앙 인접 고정 맵, 3부터 랜덤 맵.")]
    public GameObject[] MapPrefabs;

    [Tooltip("각 맵 조각의 실제 월드 크기입니다. 이 값은 그리드 간격 및 배치 위치 계산에 사용됩니다.")]
    public float PieceSize = 15f;

    [Header("Hierarchy Settings")]
    [Tooltip("생성된 모든 맵 조각의 부모(MapRoot)가 될 오브젝트의 부모입니다.")]
    [SerializeField] private Transform planesParent;

    [Header("NavMesh Settings")]
    [Tooltip("맵 배치 직후 NavMesh를 다시 빌드할 NavMeshSurface 컴포넌트 목록입니다.")]
    [SerializeField] private NavMeshSurface[] navMeshSurfaces;
    [Tooltip("맵 조각 인스턴스화 직후 NavMesh를 굽기 전 대기 시간 (초)입니다.")]
    [SerializeField] private float navMeshBakeDelay = 0.2f;

    // 🌟🌟🌟 새로운 랜덤 맵 유형 분류 🌟🌟🌟
    [Header("Controlled Random Map Types")]
    [Tooltip("1. 중앙 통로를 반드시 포함하며, 주변 통로와 연결되어야 하는 맵 조각 (예: 십자 길, T자 길).")]
    public GameObject[] CentralPathMaps; 

    [Tooltip("2. 중앙 통로를 막을 수 있는 구조를 가지며, CentralPathMaps에 인접하여 배치될 수 없는 맵 조각 (예: 변의 중심에 벽이 있는 프리셋).")]
    public GameObject[] BlockingMaps;

    [Tooltip("3. CentralPathMaps와 BlockingMaps 사이에 들어가 연결을 중재할 수 있는 맵 조각 (예: 단순히 사방이 뚫린 빈 공간, 길).")]
    public GameObject[] NeutralMaps; 
    
    // --- 내부 상수 및 변수 ---
    
    private const int SPAWN_MAP_INDEX = 0; 
    private const int SHOP_MAP_INDEX = 1; 
    private const int FIXED_NEIGHBOR_MAP_INDEX = 2; 
    private const int RANDOM_MAP_START_INDEX = 3; 

    private const int TYPE_FIXED = 10;
    private const int TYPE_CENTRAL_PATH = 0;
    private const int TYPE_BLOCKING = 1;    
    private const int TYPE_NEUTRAL = 2;     
    private const int TYPE_EMPTY = -1;      

    private const int MAP_GRID_SIZE = 9; 
    private const int CENTER_INDEX = MAP_GRID_SIZE / 2; 

    private GameObject mapParent;

    // 맵 배치 제어용 (어떤 타입의 맵이 배치되었는지 기록)
    private int[,] mapType = new int[MAP_GRID_SIZE, MAP_GRID_SIZE];

    private bool hasGeneratedLayout = false;
    private bool isNavMeshReady = false;
    private Coroutine navMeshBakeRoutine;

    public static bool GlobalMapReady { get; private set; } = false;
    public static event Action OnGlobalMapReady;

    // --- 실행 ---

    void Awake()
    {
        CacheNavMeshSurfaces();
    }

    public bool StartMapGeneration()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return false;
        }

        if (hasGeneratedLayout)
        {
            if (!isNavMeshReady && ShouldBakeNavMesh())
            {
                QueueNavMeshBake();
            }
            return false;
        }

        InitializeRandomMaps(); 
        SetGlobalMapReady(false);
        hasGeneratedLayout = true;
        isNavMeshReady = !ShouldBakeNavMesh();
        SetGlobalMapReady(isNavMeshReady);

        Debug.Log("마스터 클라이언트: 맵 레이아웃 생성을 시작합니다.");
        GenerateMapLayout();
        return true;
    }

    private void InitializeRandomMaps()
    {
        List<GameObject> finalMapPrefabs = new List<GameObject>();
        
        // 1. 고정 맵 (0, 1, 2) 유지
        finalMapPrefabs.AddRange(MapPrefabs.Take(RANDOM_MAP_START_INDEX).Where(p => p != null));

        // 2. 랜덤 맵 통합 (기존 MapPrefabs에 없는 프리팹만 추가하여 중복 방지)
        finalMapPrefabs.AddRange(CentralPathMaps.Where(p => p != null).Except(finalMapPrefabs));
        finalMapPrefabs.AddRange(BlockingMaps.Where(p => p != null).Except(finalMapPrefabs));
        finalMapPrefabs.AddRange(NeutralMaps.Where(p => p != null).Except(finalMapPrefabs));
        
        // 3. MapPrefabs 배열 갱신
        MapPrefabs = finalMapPrefabs.ToArray();
        
        if (MapPrefabs.Length <= RANDOM_MAP_START_INDEX)
        {
            Debug.LogError("오류: Central/Blocking/Neutral 맵 프리셋이 할당되지 않아 랜덤 맵을 생성할 수 없습니다.");
        }
    }


    private void GenerateMapLayout()
    {
        int[,] mapLayout = new int[MAP_GRID_SIZE, MAP_GRID_SIZE];
        int[,] mapRotation = new int[MAP_GRID_SIZE, MAP_GRID_SIZE]; 
        
        // 맵 및 타입 배열 초기화
        for (int x = 0; x < MAP_GRID_SIZE; x++)
        {
            for (int y = 0; y < MAP_GRID_SIZE; y++)
            {
                mapLayout[x, y] = TYPE_EMPTY; 
                mapRotation[x, y] = 0; 
                mapType[x, y] = TYPE_EMPTY;
            }
        }

        // 1. 필수/고정 맵 배치 
        if (SHOP_MAP_INDEX < MapPrefabs.Length && MapPrefabs[SHOP_MAP_INDEX] != null)
        {
            mapLayout[CENTER_INDEX, CENTER_INDEX] = SHOP_MAP_INDEX;
            mapType[CENTER_INDEX, CENTER_INDEX] = TYPE_FIXED;
        }

        if (SPAWN_MAP_INDEX < MapPrefabs.Length && MapPrefabs[SPAWN_MAP_INDEX] != null)
        {
            mapLayout[0, 0] = SPAWN_MAP_INDEX; mapType[0, 0] = TYPE_FIXED;
            mapLayout[0, MAP_GRID_SIZE - 1] = SPAWN_MAP_INDEX; mapType[0, MAP_GRID_SIZE - 1] = TYPE_FIXED;
            mapLayout[MAP_GRID_SIZE - 1, 0] = SPAWN_MAP_INDEX; mapType[MAP_GRID_SIZE - 1, 0] = TYPE_FIXED;
            mapLayout[MAP_GRID_SIZE - 1, MAP_GRID_SIZE - 1] = SPAWN_MAP_INDEX; mapType[MAP_GRID_SIZE - 1, MAP_GRID_SIZE - 1] = TYPE_FIXED;
        }
        
        if (FIXED_NEIGHBOR_MAP_INDEX < MapPrefabs.Length && MapPrefabs[FIXED_NEIGHBOR_MAP_INDEX] != null)
        {
            mapLayout[CENTER_INDEX, CENTER_INDEX - 1] = FIXED_NEIGHBOR_MAP_INDEX; mapType[CENTER_INDEX, CENTER_INDEX - 1] = TYPE_FIXED;
            mapLayout[CENTER_INDEX, CENTER_INDEX + 1] = FIXED_NEIGHBOR_MAP_INDEX; mapType[CENTER_INDEX, CENTER_INDEX + 1] = TYPE_FIXED;
            mapLayout[CENTER_INDEX - 1, CENTER_INDEX] = FIXED_NEIGHBOR_MAP_INDEX; mapType[CENTER_INDEX - 1, CENTER_INDEX] = TYPE_FIXED;
            mapLayout[CENTER_INDEX + 1, CENTER_INDEX] = FIXED_NEIGHBOR_MAP_INDEX; mapType[CENTER_INDEX + 1, CENTER_INDEX] = TYPE_FIXED;
        }

        // 2. 나머지 구간 랜덤 배열 및 회전 할당 (개선된 로직)
        int[] rotations = new int[] { 0, 90, 180, 270 };

        for (int x = 0; x < MAP_GRID_SIZE; x++)
        {
            for (int y = 0; y < MAP_GRID_SIZE; y++)
            {
                if (mapType[x, y] == TYPE_FIXED) continue;

                // 🌟 인접한 맵 타입 확인
                bool isNeighborCentralOrFixed = CheckNeighborType(x, y, TYPE_CENTRAL_PATH) || CheckNeighborType(x, y, TYPE_FIXED);
                bool isNeighborBlocking = CheckNeighborType(x, y, TYPE_BLOCKING);

                GameObject selectedPrefab = null;
                int selectedType = TYPE_EMPTY;

                // 🌟 배치 가능한 맵 목록 필터링 (CentralPath와 Blocking이 절대 인접하지 않도록)
                List<GameObject> availableMaps = new List<GameObject>();
                
                if (isNeighborCentralOrFixed && isNeighborBlocking)
                {
                    // Case 1: Central/Fixed와 Blocking 둘 다 인접한 경우 -> Neutral만 사용 가능
                    availableMaps.AddRange(NeutralMaps.Where(p => p != null));
                }
                else if (isNeighborCentralOrFixed)
                {
                    // Case 2: Central/Fixed에 인접한 경우 -> Blocking 제외 (CentralPath, Neutral만 사용)
                    availableMaps.AddRange(CentralPathMaps.Where(p => p != null));
                    availableMaps.AddRange(NeutralMaps.Where(p => p != null));
                }
                else if (isNeighborBlocking)
                {
                    // Case 3: Blocking에 인접한 경우 -> CentralPath 제외 (Blocking, Neutral만 사용)
                    availableMaps.AddRange(BlockingMaps.Where(p => p != null));
                    availableMaps.AddRange(NeutralMaps.Where(p => p != null));
                }
                else
                {
                    // Case 4: 특정 타입에 인접하지 않은 경우 -> 모든 맵 사용 가능
                    availableMaps.AddRange(CentralPathMaps.Where(p => p != null));
                    availableMaps.AddRange(BlockingMaps.Where(p => p != null));
                    availableMaps.AddRange(NeutralMaps.Where(p => p != null));
                }

                // 🌟 배치 가능한 맵이 있는지 확인하고 선택
                if (availableMaps.Count > 0)
                {
                    selectedPrefab = GetRandomPrefab(availableMaps.ToArray());
                    selectedType = GetPrefabType(selectedPrefab);
                }
                else
                {
                    Debug.LogWarning($"경고: 위치 ({x},{y})에 배치할 수 있는 맵이 없습니다. 빈 공간으로 남깁니다.");
                    continue;
                }

                if (selectedPrefab != null)
                {
                    int prefabIndex = GetPrefabIndex(selectedPrefab); 
                    
                    if (prefabIndex != -1)
                    {
                        mapLayout[x, y] = prefabIndex;
                        mapType[x, y] = selectedType;
                        
                        int randomRotationIndex = Random.Range(0, rotations.Length);
                        mapRotation[x, y] = rotations[randomRotationIndex];
                    }
                    else
                    {
                        Debug.LogError($"오류: MapPrefabs 배열에서 {selectedPrefab.name}의 인덱스를 찾을 수 없습니다. InitializeRandomMaps 로직 오류.");
                    }
                }
            }
        }

        // 3. 맵 레이아웃 데이터를 1차원 배열로 변환
        int[] flatLayout = new int[MAP_GRID_SIZE * MAP_GRID_SIZE];
        int[] flatRotation = new int[MAP_GRID_SIZE * MAP_GRID_SIZE]; 
        
        for (int x = 0; x < MAP_GRID_SIZE; x++)
        {
            for (int y = 0; y < MAP_GRID_SIZE; y++)
            {
                flatLayout[x * MAP_GRID_SIZE + y] = mapLayout[x, y];
                flatRotation[x * MAP_GRID_SIZE + y] = mapRotation[x, y];
            }
        }

        // 4. RPC 호출
        photonView.RPC("RPC_InstantiateMap", RpcTarget.AllBuffered, flatLayout, flatRotation);
    }
    
    // --- 헬퍼 함수 ---

    /// <summary>
    /// MapPrefabs 배열에서 특정 GameObject의 인덱스를 찾아 반환합니다.
    /// </summary>
    private int GetPrefabIndex(GameObject prefab)
    {
        // Linq 대신 Array.IndexOf를 사용하며, MapPrefabs 배열은 InitializeRandomMaps에서 이미 통합되어 있습니다.
        return Array.IndexOf(MapPrefabs, prefab);
    }
    
    /// <summary>
    /// 주어진 프리팹이 어떤 유형(Central, Blocking, Neutral)에 속하는지 반환합니다.
    /// </summary>
    private int GetPrefabType(GameObject prefab)
    {
        if (CentralPathMaps.Contains(prefab)) return TYPE_CENTRAL_PATH;
        if (BlockingMaps.Contains(prefab)) return TYPE_BLOCKING;
        if (NeutralMaps.Contains(prefab)) return TYPE_NEUTRAL;
        return TYPE_EMPTY;
    }

    /// <summary>
    /// 프리팹 인덱스로 맵 타입을 반환합니다.
    /// </summary>
    private int GetPrefabTypeByIndex(int prefabIndex)
    {
        if (prefabIndex < 0 || prefabIndex >= MapPrefabs.Length) return TYPE_EMPTY;
        
        GameObject prefab = MapPrefabs[prefabIndex];
        if (prefab == null) return TYPE_EMPTY;
        
        return GetPrefabType(prefab);
    }

    /// <summary>
    /// 지정된 배열에서 랜덤 프리팹을 하나 가져옵니다.
    /// </summary>
    private GameObject GetRandomPrefab(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0) return null;
        return prefabs[Random.Range(0, prefabs.Length)];
    }

    /// <summary>
    /// 주변 4방향에 특정 타입의 맵 조각이 있는지 확인합니다.
    /// </summary>
    private bool CheckNeighborType(int x, int y, int checkType)
    {
        int[] dx = { 0, 0, 1, -1 }; // 상, 하, 우, 좌
        int[] dy = { 1, -1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];

            if (nx >= 0 && nx < MAP_GRID_SIZE && ny >= 0 && ny < MAP_GRID_SIZE)
            {
                // 이미 배치된 맵만 확인 (mapType이 -1이 아닌 경우)
                if (mapType[nx, ny] == checkType)
                {
                    return true;
                }
            }
        }
        return false;
    }

    // --- RPC 및 NavMesh 관련 함수 ---

    [PunRPC]
    private void RPC_InstantiateMap(int[] flatLayout, int[] flatRotation) 
    {
        Debug.Log("맵 레이아웃 데이터를 수신했습니다. 맵 부모와 조각 생성을 시작합니다.");

        if (mapParent != null)
        {
            Destroy(mapParent);
        }

        mapParent = new GameObject("MapRoot");
        mapParent.transform.position = Vector3.zero;

        if (planesParent != null)
        {
            mapParent.transform.SetParent(planesParent, false); 
            Debug.Log("MapRoot를 Planes 부모 오브젝트의 자식으로 설정했습니다.");
        }
        else
        {
            Debug.LogWarning("경고: Planes Parent 오브젝트가 할당되지 않아 MapRoot가 씬 루트에 생성됩니다.");
        }

        float halfPieceSize = PieceSize / 2f; 

        for (int i = 0; i < flatLayout.Length; i++)
        {
            int mapPieceIndex = flatLayout[i];
            
            if (mapPieceIndex == TYPE_EMPTY)
            {
                continue;
            }

            int rotationY = (flatRotation.Length > i) ? flatRotation[i] : 0; 
            
            int x = i / MAP_GRID_SIZE;
            int y = i % MAP_GRID_SIZE;

            Vector3 position = new Vector3(
                (x * PieceSize) + halfPieceSize,
                0f,
                (y * PieceSize) + halfPieceSize
            );
            
            Quaternion rotation = Quaternion.Euler(0f, rotationY, 0f); 

            if (mapPieceIndex >= 0 && mapPieceIndex < MapPrefabs.Length)
            {
                GameObject prefabToInstantiate = MapPrefabs[mapPieceIndex];

                if (prefabToInstantiate != null)
                {
                    Instantiate(prefabToInstantiate, position, rotation, mapParent.transform);
                }
                else
                {
                    Debug.LogError($"맵 생성 실패: 인덱스 {mapPieceIndex}의 프리팹 참조가 깨졌습니다. (MapPrefabs 배열의 값: null)");
                }
            }
            else
            {
                Debug.LogError($"잘못된 맵 프리셋 인덱스: {mapPieceIndex}. 맵 프리팹 인덱스는 0 이상 {MapPrefabs.Length} 미만이어야 합니다.");
            }
        }

        OnMapPiecesInstantiated();
    }

    public bool IsMapReady => hasGeneratedLayout && (!ShouldBakeNavMesh() || isNavMeshReady);

    private void CacheNavMeshSurfaces()
    {
        if (planesParent != null)
        {
            NavMeshSurface surfaceOnPlanes = planesParent.GetComponent<NavMeshSurface>();
            if (surfaceOnPlanes != null)
            {
                navMeshSurfaces = new NavMeshSurface[] { surfaceOnPlanes };
                return;
            }
        }

        if (navMeshSurfaces != null && navMeshSurfaces.Length > 0)
        {
            return;
        }

        NavMeshSurface[] localSurfaces = GetComponents<NavMeshSurface>();
        if (localSurfaces != null && localSurfaces.Length > 0)
        {
            navMeshSurfaces = localSurfaces;
            return;
        }

        navMeshSurfaces = GetComponentsInChildren<NavMeshSurface>();
    }

    private bool ShouldBakeNavMesh()
    {
        return navMeshSurfaces != null && navMeshSurfaces.Length > 0;
    }

    private void OnMapPiecesInstantiated()
    {
        CacheNavMeshSurfaces();

        if (!ShouldBakeNavMesh())
        {
            isNavMeshReady = true;
            SetGlobalMapReady(true);
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            SetGlobalMapReady(true);
            return;
        }

        QueueNavMeshBake();
    }

    private void QueueNavMeshBake()
    {
        isNavMeshReady = false;

        if (navMeshBakeRoutine != null)
        {
            StopCoroutine(navMeshBakeRoutine);
        }

        navMeshBakeRoutine = StartCoroutine(BakeNavMeshRoutine());
    }

    private IEnumerator BakeNavMeshRoutine()
    {
        if (navMeshBakeDelay > 0f)
        {
            yield return new WaitForSeconds(navMeshBakeDelay);
        }
        else
        {
            yield return null;
        }

        foreach (var surface in navMeshSurfaces)
        {
            if (surface == null) continue;
            surface.RemoveData();
            surface.BuildNavMesh(); 
        }

        isNavMeshReady = true;
        SetGlobalMapReady(true);
        Debug.Log("MapGenerator: NavMesh 재베이크가 완료되었습니다.");
    }

    private void SetGlobalMapReady(bool ready)
    {
        GlobalMapReady = ready;
        if (ready)
        {
            OnGlobalMapReady?.Invoke();
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        Gizmos.color = Color.yellow;
        float centerOffset = PieceSize / 2f;
        float centerPosition = (CENTER_INDEX * PieceSize) + centerOffset;
        Gizmos.DrawSphere(new Vector3(centerPosition, 0, centerPosition), 0.5f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(new Vector3(centerPosition, 0, (CENTER_INDEX - 1) * PieceSize + centerOffset), new Vector3(PieceSize, 0.1f, PieceSize));
        Gizmos.DrawWireCube(new Vector3(centerPosition, 0, (CENTER_INDEX + 1) * PieceSize + centerOffset), new Vector3(PieceSize, 0.1f, PieceSize));
        Gizmos.DrawWireCube(new Vector3((CENTER_INDEX - 1) * PieceSize + centerOffset, 0, centerPosition), new Vector3(PieceSize, 0.1f, PieceSize));
        Gizmos.DrawWireCube(new Vector3((CENTER_INDEX + 1) * PieceSize + centerOffset, 0, centerPosition), new Vector3(PieceSize, 0.1f, PieceSize));

        Gizmos.color = Color.white;
        float totalSize = MAP_GRID_SIZE * PieceSize;

        for (int i = 0; i <= MAP_GRID_SIZE; i++)
        {
            Vector3 startX = new Vector3(i * PieceSize, 0, 0);
            Vector3 endX = new Vector3(i * PieceSize, 0, totalSize);
            Gizmos.DrawLine(startX, endX);

            Vector3 startZ = new Vector3(0, 0, i * PieceSize);
            Vector3 endZ = new Vector3(totalSize, 0, i * PieceSize);
            Gizmos.DrawLine(startZ, endZ);
        }
    }
}