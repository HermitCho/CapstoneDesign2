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
    private const int FIXED_NEIGHBOR_MAP_INDEX = 2; // Element 2 인덱스
    private const int RANDOM_MAP_START_INDEX = 3;

    private const int TYPE_FIXED = 10;
    private const int TYPE_CENTRAL_PATH = 0;
    private const int TYPE_BLOCKING = 1;
    private const int TYPE_NEUTRAL = 2;
    private const int TYPE_EMPTY = -1;

    private const int MAP_GRID_SIZE = 9;
    private const int CENTER_INDEX = MAP_GRID_SIZE / 2; // 4

    private GameObject mapParent;

    // 고정 맵 프리팹 참조 저장 (InitializeRandomMaps 후에도 사용하기 위해)
    private GameObject fixedNeighborMapPrefab;

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
        InitializeRandomMaps(); 
    }

    /// <summary>
    /// 🌟🌟🌟 [수정 로직] 맵 생성 시작 전 상태 초기화 및 RPC 버퍼 정리 (두 번째 판 문제 해결) 🌟🌟🌟
    /// </summary>
    public void CleanupMapState()
    {
        // 1. 내부 상태 초기화
        hasGeneratedLayout = false;
        isNavMeshReady = false;
        SetGlobalMapReady(false); 

        // NavMesh Bake 코루틴이 실행 중이면 중지
        if (navMeshBakeRoutine != null)
        {
            StopCoroutine(navMeshBakeRoutine);
            navMeshBakeRoutine = null;
        }

        // 2. 핵심: 마스터 클라이언트만 이전 맵 생성 RPC 버퍼를 제거
        if (PhotonNetwork.IsMasterClient)
        {
            if (photonView.ViewID != 0)
            {
                // RPC_InstantiateMap만 제거합니다.
                PhotonNetwork.RemoveBufferedRPCs(photonView.ViewID, nameof(RPC_InstantiateMap)); 
                Debug.Log("마스터 클라이언트: 이전 맵 생성 RPC 버퍼를 제거했습니다.");
            }
            else
            {
                Debug.LogWarning("MapGenerator: PhotonView ID가 0이어서 RPC 버퍼를 제거할 수 없습니다. (씬에 MapGenerator 오브젝트가 없을 수 있음)");
            }
            
            // 추가: 혹시 RoomObject가 남아있다면 제거 (Cleanup)
            GameObject[] roomObjects = GameObject.FindGameObjectsWithTag("RoomObject");
            foreach (var obj in roomObjects)
            {
                PhotonView pv = obj.GetComponent<PhotonView>();
                // 마스터 클라이언트 소유의 RoomObject만 파괴
                if (pv != null && pv.IsMine && obj.transform.parent != null && obj.transform.parent.name == "MapRoot") 
                {
                    PhotonNetwork.Destroy(obj);
                }
            }
        }

        // 3. 기존 맵 오브젝트 파괴
        if (mapParent != null)
        {
            Destroy(mapParent);
            mapParent = null;
            Debug.Log("MapRoot 오브젝트를 파괴했습니다.");
        }
        
        // 4. mapType 배열 초기화
        for (int x = 0; x < MAP_GRID_SIZE; x++)
        {
            for (int y = 0; y < MAP_GRID_SIZE; y++)
            {
                mapType[x, y] = TYPE_EMPTY;
            }
        }
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

        SetGlobalMapReady(false);
        hasGeneratedLayout = true;
        isNavMeshReady = !ShouldBakeNavMesh();
        
        if (!ShouldBakeNavMesh())
        {
            SetGlobalMapReady(true);
        }

        Debug.Log("마스터 클라이언트: 맵 레이아웃 생성을 시작합니다.");
        GenerateMapLayout();
        return true;
    }

    private void InitializeRandomMaps()
    {
        // 고정 맵 프리팹 참조 저장
        if (FIXED_NEIGHBOR_MAP_INDEX < MapPrefabs.Length)
        {
            fixedNeighborMapPrefab = MapPrefabs[FIXED_NEIGHBOR_MAP_INDEX];
        }

        List<GameObject> finalMapPrefabs = new List<GameObject>();

        // 고정 맵 (0, 1, 2)은 Null 여부와 관계없이 그대로 유지
        finalMapPrefabs.AddRange(MapPrefabs.Take(RANDOM_MAP_START_INDEX)); 

        // 나머지 랜덤 맵 통합 (Null 제거 후 통합)
        if (MapPrefabs.Length > RANDOM_MAP_START_INDEX)
        {
            finalMapPrefabs.AddRange(MapPrefabs.Skip(RANDOM_MAP_START_INDEX)
                                               .Where(p => p != null)
                                               .Except(finalMapPrefabs));
        }

        // Controlled Random Map Types 배열 통합 (Null 제거 후 통합)
        finalMapPrefabs.AddRange(CentralPathMaps.Where(p => p != null).Except(finalMapPrefabs));
        finalMapPrefabs.AddRange(BlockingMaps.Where(p => p != null).Except(finalMapPrefabs));
        finalMapPrefabs.AddRange(NeutralMaps.Where(p => p != null).Except(finalMapPrefabs));

        // MapPrefabs 배열 갱신
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
                if (mapType[x, y] != TYPE_FIXED) mapType[x, y] = TYPE_EMPTY;
            }
        }

        // 1. 필수/고정 맵 배치 (스폰 및 중앙 상점)
        // (4, 4) 중앙 상점
        if (SHOP_MAP_INDEX < MapPrefabs.Length && MapPrefabs[SHOP_MAP_INDEX] != null)
        {
            mapLayout[CENTER_INDEX, CENTER_INDEX] = SHOP_MAP_INDEX;
        }
        mapType[CENTER_INDEX, CENTER_INDEX] = TYPE_FIXED; 

        // 네 모서리 스폰 (0, 0), (0, 8), (8, 0), (8, 8)
        if (SPAWN_MAP_INDEX < MapPrefabs.Length && MapPrefabs[SPAWN_MAP_INDEX] != null)
        {
            mapLayout[0, 0] = SPAWN_MAP_INDEX; 
            mapLayout[0, MAP_GRID_SIZE - 1] = SPAWN_MAP_INDEX; 
            mapLayout[MAP_GRID_SIZE - 1, 0] = SPAWN_MAP_INDEX; 
            mapLayout[MAP_GRID_SIZE - 1, MAP_GRID_SIZE - 1] = SPAWN_MAP_INDEX;
        }
        mapType[0, 0] = TYPE_FIXED; mapType[0, MAP_GRID_SIZE - 1] = TYPE_FIXED; 
        mapType[MAP_GRID_SIZE - 1, 0] = TYPE_FIXED; mapType[MAP_GRID_SIZE - 1, MAP_GRID_SIZE - 1] = TYPE_FIXED;


        // 2. Element 2 고정 맵 배치 (총 8개 위치)
        if (FIXED_NEIGHBOR_MAP_INDEX < MapPrefabs.Length)
        {
            int fixedNeighborIndex = FIXED_NEIGHBOR_MAP_INDEX; 
            
            if (fixedNeighborIndex != -1 && MapPrefabs.Length > fixedNeighborIndex)
            {
                // A. 기존 중앙 인접 고정 맵 4곳
                mapLayout[CENTER_INDEX, CENTER_INDEX - 1] = fixedNeighborIndex; mapType[CENTER_INDEX, CENTER_INDEX - 1] = TYPE_FIXED; // (4, 3)
                mapLayout[CENTER_INDEX, CENTER_INDEX + 1] = fixedNeighborIndex; mapType[CENTER_INDEX, CENTER_INDEX + 1] = TYPE_FIXED; // (4, 5)
                mapLayout[CENTER_INDEX - 1, CENTER_INDEX] = fixedNeighborIndex; mapType[CENTER_INDEX - 1, CENTER_INDEX] = TYPE_FIXED; // (3, 4)
                mapLayout[CENTER_INDEX + 1, CENTER_INDEX] = fixedNeighborIndex; mapType[CENTER_INDEX + 1, CENTER_INDEX] = TYPE_FIXED; // (5, 4)

                // B. 경계 중앙에 위치하는 추가 고정 통로 4곳
                
                // (0, 4) - 좌측 중앙 경계
                if (mapType[0, CENTER_INDEX] == TYPE_EMPTY)
                {
                    mapLayout[0, CENTER_INDEX] = fixedNeighborIndex; mapType[0, CENTER_INDEX] = TYPE_FIXED;
                }
                
                // (4, 0) - 하단 중앙 경계
                if (mapType[CENTER_INDEX, 0] == TYPE_EMPTY)
                {
                    mapLayout[CENTER_INDEX, 0] = fixedNeighborIndex; mapType[CENTER_INDEX, 0] = TYPE_FIXED;
                }
                
                // (8, 4) - 우측 중앙 경계
                if (mapType[MAP_GRID_SIZE - 1, CENTER_INDEX] == TYPE_EMPTY)
                {
                    mapLayout[MAP_GRID_SIZE - 1, CENTER_INDEX] = fixedNeighborIndex; mapType[MAP_GRID_SIZE - 1, CENTER_INDEX] = TYPE_FIXED;
                }
                
                // (4, 8) - 상단 중앙 경계
                if (mapType[CENTER_INDEX, MAP_GRID_SIZE - 1] == TYPE_EMPTY)
                {
                    mapLayout[CENTER_INDEX, MAP_GRID_SIZE - 1] = fixedNeighborIndex; mapType[CENTER_INDEX, MAP_GRID_SIZE - 1] = TYPE_FIXED;
                }
                
            }
            else
            {
                Debug.LogError($"오류: MapPrefabs 배열 인덱스 {FIXED_NEIGHBOR_MAP_INDEX} 접근 오류.");
            }
        }
        else
        {
            Debug.LogWarning("경고: 고정 맵 인덱스가 MapPrefabs 배열 범위를 벗어납니다.");
        }
        
        // -------------------------------------------------------------


        // 3. 나머지 구간 랜덤 배열 및 회전 할당 (TYPE_FIXED로 지정된 위치는 제외)
        int[] rotations = new int[] { 0, 90, 180, 270 };

        for (int x = 0; x < MAP_GRID_SIZE; x++)
        {
            for (int y = 0; y < MAP_GRID_SIZE; y++)
            {
                // **TYPE_FIXED**로 지정된 위치는 건너뛰기
                if (mapType[x, y] == TYPE_FIXED) continue;
                
                if (x == CENTER_INDEX && y == CENTER_INDEX) continue; 
                
                
                // 인접한 맵 타입 확인
                bool isNeighborCentralOrFixed = CheckNeighborType(x, y, TYPE_CENTRAL_PATH) || CheckNeighborType(x, y, TYPE_FIXED);
                bool isNeighborBlocking = CheckNeighborType(x, y, TYPE_BLOCKING);

                GameObject selectedPrefab = null;
                int selectedType = TYPE_EMPTY;

                // 배치 가능한 맵 목록 필터링 (CentralPath와 Blocking이 절대 인접하지 않도록)
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
                    int prefabIndex = GetPrefabIndex(selectedPrefab); // 이제 public 메서드

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

        // 4. 맵 레이아웃 데이터를 1차원 배열로 변환
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

        // 5. RPC 호출
        photonView.RPC(nameof(RPC_InstantiateMap), RpcTarget.AllBuffered, flatLayout, flatRotation);
    }

    // --- 헬퍼 함수 ---

    /// <summary>
    /// MapPrefabs 배열에서 특정 GameObject의 인덱스를 찾아 반환합니다.
    /// </summary>
    // ⭐ 수정: private -> public으로 변경하여 CS0103 오류 해결
    public int GetPrefabIndex(GameObject prefab)
    {
        return Array.IndexOf(MapPrefabs, prefab);
    }

    /// <summary>
    /// 주어진 프리팹이 어떤 유형(Central, Blocking, Neutral)에 속하는지 반환합니다.
    /// 맵 인덱스 0, 1, 2는 고정 맵으로 간주하여 TYPE_FIXED를 반환합니다.
    /// </summary>
    private int GetPrefabType(GameObject prefab)
    {
        // Null이 MapPrefabs에 유지되므로, Null이면 TYPE_EMPTY 반환
        if (prefab == null) return TYPE_EMPTY;
        
        // 고정 맵 인덱스에 해당하는 프리팹인지 먼저 확인하여 TYPE_FIXED를 보장
        int prefabIndex = GetPrefabIndex(prefab);
        
        if (prefabIndex == SPAWN_MAP_INDEX || 
            prefabIndex == SHOP_MAP_INDEX || 
            prefabIndex == FIXED_NEIGHBOR_MAP_INDEX)
        {
            return TYPE_FIXED;
        }
        
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
        // Null이 의도적으로 유지되므로 여기서 Null 체크
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
        if (!photonView.IsMine)
        {
            Debug.Log($"비마스터 클라이언트가 맵 데이터를 수신했습니다. 레이아웃 크기: {flatLayout.Length}");
        }
        else
        {
            Debug.Log("마스터 클라이언트: 맵 레이아웃 데이터를 전송했습니다.");
        }

        bool isMaster = PhotonNetwork.IsMasterClient;

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
        float totalMapSize = MAP_GRID_SIZE * PieceSize;
        float centerOffset = totalMapSize / 2f;

        mapParent.transform.position = new Vector3(-centerOffset + halfPieceSize, 0f, -centerOffset + halfPieceSize);

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
                    // ⭐ 수정: PhotonNetwork.InstantiateRoomObject 사용
                    if (isMaster)
                    {
                        string prefabName = prefabToInstantiate.name;
                        
                        GameObject instantiatedPiece = PhotonNetwork.InstantiateRoomObject(
                            "Prefabs/Maps/Random Map/" + prefabName, 
                            position, 
                            rotation, 
                            group: 0, 
                            data: null
                        );
                        
                        if (instantiatedPiece != null)
                        {
                            instantiatedPiece.transform.SetParent(mapParent.transform);
                        }
                        else
                        {
                            Debug.LogError($"[CRITICAL] 맵 조각 생성 실패: '{prefabName}'을(를) Resources 폴더에서 찾거나 로드할 수 없습니다. (Photon DefaultPool Load Error)");
                        }
                    }
                }
            }
            else
            {
                Debug.LogError($"잘못된 맵 프리셋 인덱스: {mapPieceIndex}. [CRITICAL] 로컬 MapPrefabs 인덱스는 0 이상 {MapPrefabs.Length} 미만이어야 합니다.");
            }
        }

        // ⭐ 수정: 맵 생성 후 NavMesh Bake를 위한 코루틴 시작
        StartCoroutine(WaitForMapInstantiationAndBake());
    }

    // ⭐ 추가: 맵 조각 인스턴스화 완료 후 NavMesh Bake를 지연하는 코루틴
    private IEnumerator WaitForMapInstantiationAndBake()
    {
        // 맵 조각이 모두 로드되고 활성화되는 것을 기다립니다.
        yield return null; 
        yield return null; 
        
        // 0.2초의 추가 지연 (넉넉한 동기화 시간 확보)
        yield return new WaitForSeconds(0.2f); 

        // NavMeshSurface를 다시 캐싱하여 새로 생성된 맵 조각의 Surface를 확실히 포함
        CacheNavMeshSurfaces();
        
        if (!ShouldBakeNavMesh())
        {
            isNavMeshReady = true;
            SetGlobalMapReady(true);
            yield break;
        }

        // 비마스터 클라이언트는 NavMesh Bake를 수행하지 않고 마스터를 기다립니다.
        if (!PhotonNetwork.IsMasterClient)
        {
            SetGlobalMapReady(true); 
            yield break;
        }

        QueueNavMeshBake();
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

        // 씬 전체에서 다시 찾기
        navMeshSurfaces = GetComponentsInChildren<NavMeshSurface>(true); 
    }

    private bool ShouldBakeNavMesh()
    {
        return navMeshSurfaces != null && navMeshSurfaces.Length > 0;
    }

    private void OnMapPiecesInstantiated()
    {
        // 이 메서드는 이제 WaitForMapInstantiationAndBake() 코루틴이 대체합니다.
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
        // ⭐ 수정: NavMeshSurface가 지오메트리를 인식할 시간을 충분히 줍니다.
        if (navMeshBakeDelay > 0f)
        {
            yield return new WaitForSeconds(navMeshBakeDelay + 0.5f); // 0.5초 추가 대기
        }
        else
        {
            yield return new WaitForSeconds(0.7f); // 최소 0.7초 대기
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

        // 맵 중앙 정렬을 시뮬레이션
        float halfPieceSize = PieceSize / 2f;
        float totalMapSize = MAP_GRID_SIZE * PieceSize;
        float centerOffset = totalMapSize / 2f;
        Vector3 mapRootAdjustment = new Vector3(-centerOffset + halfPieceSize, 0f, -centerOffset + halfPieceSize);
        
        // 중앙 상점 (CENTER_INDEX, CENTER_INDEX)의 월드 좌표를 계산
        float centerPosLocal = (CENTER_INDEX * PieceSize) + halfPieceSize;
        Vector3 shopWorldPos = new Vector3(centerPosLocal, 0, centerPosLocal) + mapRootAdjustment;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(shopWorldPos, 0.5f); // 맵 중앙 (상점 위치)

        // 고정 맵 위치 계산을 위한 변수
        float z_minus_1 = ((CENTER_INDEX - 1) * PieceSize) + halfPieceSize; // 3
        float z_plus_1 = ((CENTER_INDEX + 1) * PieceSize) + halfPieceSize;  // 5
        float x_minus_1 = ((CENTER_INDEX - 1) * PieceSize) + halfPieceSize; // 3
        float x_plus_1 = ((CENTER_INDEX + 1) * PieceSize) + halfPieceSize;  // 5
        float x_0 = (0 * PieceSize) + halfPieceSize; // 0
        float x_8 = (8 * PieceSize) + halfPieceSize; // 8
        float z_0 = (0 * PieceSize) + halfPieceSize; // 0
        float z_8 = (8 * PieceSize) + halfPieceSize; // 8
        Vector3 pieceSizeV3 = new Vector3(PieceSize, 0.1f, PieceSize);


        // A. 중앙 인접 고정 맵 4곳 (Magenta)
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(new Vector3(centerPosLocal, 0, z_minus_1) + mapRootAdjustment, pieceSizeV3); // (4, 3)
        Gizmos.DrawWireCube(new Vector3(centerPosLocal, 0, z_plus_1) + mapRootAdjustment, pieceSizeV3);  // (4, 5)
        Gizmos.DrawWireCube(new Vector3(x_minus_1, 0, centerPosLocal) + mapRootAdjustment, pieceSizeV3);  // (3, 4)
        Gizmos.DrawWireCube(new Vector3(x_plus_1, 0, centerPosLocal) + mapRootAdjustment, pieceSizeV3);   // (5, 4)


        // B. 경계 중앙에 위치하는 추가 고정 통로 4곳 (Cyan)
        Gizmos.color = Color.cyan;
        
        // (0, 4) - 좌측 중앙 경계
        Gizmos.DrawWireCube(new Vector3(x_0, 0, centerPosLocal) + mapRootAdjustment, pieceSizeV3);

        // (4, 0) - 하단 중앙 경계
        Gizmos.DrawWireCube(new Vector3(centerPosLocal, 0, z_0) + mapRootAdjustment, pieceSizeV3);
        
        // (8, 4) - 우측 중앙 경계
        Gizmos.DrawWireCube(new Vector3(x_8, 0, centerPosLocal) + mapRootAdjustment, pieceSizeV3);
        
        // (4, 8) - 상단 중앙 경계
        Gizmos.DrawWireCube(new Vector3(centerPosLocal, 0, z_8) + mapRootAdjustment, pieceSizeV3);
        // -------------------------------------------------------------

        // 전체 그리드 선 그리기 (흰색)
        Gizmos.color = Color.white;
        float totalSize = MAP_GRID_SIZE * PieceSize;

        for (int i = 0; i <= MAP_GRID_SIZE; i++)
        {
            // Z 축으로 길게 (X 변화)
            Vector3 startX = new Vector3(i * PieceSize, 0, 0) + mapRootAdjustment;
            Vector3 endX = new Vector3(i * PieceSize, 0, totalSize) + mapRootAdjustment;
            Gizmos.DrawLine(startX, endX);

            // X 축으로 길게 (Z 변화)
            Vector3 startZ = new Vector3(0, 0, i * PieceSize) + mapRootAdjustment;
            Vector3 endZ = new Vector3(totalSize, 0, i * PieceSize) + mapRootAdjustment;
            Gizmos.DrawLine(startZ, endZ);
        }
    }
}