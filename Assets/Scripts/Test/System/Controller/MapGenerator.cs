using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Unity.AI.Navigation;
using UnityEngine;

public class MapGenerator : MonoBehaviourPunCallbacks
{
    [Header("Map Prefabs & Size")]
    [Tooltip("맵 조각 프리팹 목록입니다. 인덱스 0: 스폰 맵, 1: 상점 맵, 2: 중앙 인접 고정 맵, 3부터 랜덤 맵.")]
    public GameObject[] MapPrefabs;

    [Tooltip("각 맵 조각의 실제 월드 크기입니다. 이 값은 그리드 간격 및 배치 위치 계산에 사용됩니다.")]
    public float PieceSize = 15f;

    [Header("Hierarchy Settings")]
    [Tooltip("생성된 모든 맵 조각의 부모(MapRoot)가 될 오브젝트의 부모입니다. NavMeshSurface가 이 오브젝트에 있는 것이 권장됩니다.")]
    [SerializeField] private Transform planesParent; // Planes 오브젝트를 여기에 할당합니다.

    [Header("NavMesh Settings")]
    [Tooltip("맵 배치 직후 NavMesh를 다시 빌드할 NavMeshSurface 컴포넌트 목록입니다. 주로 planesParent 오브젝트에 할당됩니다.")]
    [SerializeField] private NavMeshSurface[] navMeshSurfaces;
    [Tooltip("맵 조각 인스턴스화 직후 NavMesh를 굽기 전 대기 시간 (초)입니다. 환경에 따라 조정이 필요할 수 있습니다.")]
    [SerializeField] private float navMeshBakeDelay = 0.2f;

    // 생성된 모든 맵 조각의 부모가 될 게임 오브젝트 (RPC 수신 시 동적 생성됨)
    private GameObject mapParent;

    // 맵 조각의 종류를 식별하기 위한 상수 (MapPrefabs 배열의 인덱스와 일치해야 합니다)
    private const int SPAWN_MAP_INDEX = 0;       // MapPrefabs[0]에 스폰 맵 할당 필요 (네 코너)
    private const int SHOP_MAP_INDEX = 1;        // MapPrefabs[1]에 상점 맵 할당 필요 (중앙)
    private const int FIXED_NEIGHBOR_MAP_INDEX = 2; // MapPrefabs[2]에 중앙 인접 고정 맵 할당 필요 (상하좌우)
    
    private const int RANDOM_MAP_START_INDEX = 3; // MapPrefabs[3]부터 랜덤 맵 할당 필요

    // 맵 크기
    private const int MAP_GRID_SIZE = 9; // 9x9 크기
    private const int CENTER_INDEX = MAP_GRID_SIZE / 2; // 중앙 인덱스 (4)

    // 랜덤 맵 생성을 위한 리스트
    private List<int> randomMapIndices = new List<int>();
    private bool hasGeneratedLayout = false;
    private bool isNavMeshReady = false;
    private Coroutine navMeshBakeRoutine;

    // 외부에서 맵 생성 완료 여부를 확인할 수 있는 정적 속성
    public static bool GlobalMapReady { get; private set; } = false;
    // 맵 생성 및 NavMesh 빌드가 완료되었을 때 호출되는 이벤트
    public static event Action OnGlobalMapReady;

    // --- 실행 ---

    void Awake()
    {
        CacheNavMeshSurfaces();
        InitializeRandomMaps();
    }

    [Tooltip("마스터 클라이언트에서 맵 생성 프로세스를 시작합니다.")]
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
        if (randomMapIndices.Count == 0)
        {
            for (int i = RANDOM_MAP_START_INDEX; i < MapPrefabs.Length; i++)
            {
                randomMapIndices.Add(i);
            }
        }
    }

    /// <summary>
    /// 마스터 클라이언트에서 맵 배치 정보를 생성하고 RPC로 전송합니다.
    /// </summary>
    private void GenerateMapLayout()
    {
        // 9x9 그리드에 어떤 프리셋 인덱스를 배치할지 저장하는 배열
        int[,] mapLayout = new int[MAP_GRID_SIZE, MAP_GRID_SIZE];
        for (int x = 0; x < MAP_GRID_SIZE; x++)
        {
            for (int y = 0; y < MAP_GRID_SIZE; y++)
            {
                mapLayout[x, y] = -1; // 초기값을 -1 (빈 공간)으로 설정
            }
        }

        // 1. 필수/고정 맵 배치

        // 상점 맵 배치 (맵 중앙)
        if (SHOP_MAP_INDEX < MapPrefabs.Length && MapPrefabs[SHOP_MAP_INDEX] != null)
        {
            mapLayout[CENTER_INDEX, CENTER_INDEX] = SHOP_MAP_INDEX;
        }
        else
        {
            Debug.LogWarning("경고: 상점 맵 프리셋이 할당되지 않았거나 null입니다. 맵 중앙에 상점을 생성하지 않습니다.");
        }

        // 스폰 맵 배치 (네 코너)
        bool isSpawnPrefabAvailable = SPAWN_MAP_INDEX < MapPrefabs.Length && MapPrefabs[SPAWN_MAP_INDEX] != null;
        if (isSpawnPrefabAvailable)
        {
            mapLayout[0, 0] = SPAWN_MAP_INDEX;
            mapLayout[0, MAP_GRID_SIZE - 1] = SPAWN_MAP_INDEX;
            mapLayout[MAP_GRID_SIZE - 1, 0] = SPAWN_MAP_INDEX;
            mapLayout[MAP_GRID_SIZE - 1, MAP_GRID_SIZE - 1] = SPAWN_MAP_INDEX;
        }
        else
        {
            Debug.LogWarning("경고: 스폰 맵 프리셋이 할당되지 않았거나 null입니다. 맵 코너에 스폰 지점을 생성하지 않습니다.");
        }
        
        // 중앙 인접 맵 배치 (상하좌우)
        bool isFixedNeighborPrefabAvailable = FIXED_NEIGHBOR_MAP_INDEX < MapPrefabs.Length && MapPrefabs[FIXED_NEIGHBOR_MAP_INDEX] != null;
        if (isFixedNeighborPrefabAvailable)
        {
            mapLayout[CENTER_INDEX, CENTER_INDEX - 1] = FIXED_NEIGHBOR_MAP_INDEX; // 상 (Y-1)
            mapLayout[CENTER_INDEX, CENTER_INDEX + 1] = FIXED_NEIGHBOR_MAP_INDEX; // 하 (Y+1)
            mapLayout[CENTER_INDEX - 1, CENTER_INDEX] = FIXED_NEIGHBOR_MAP_INDEX; // 좌 (X-1)
            mapLayout[CENTER_INDEX + 1, CENTER_INDEX] = FIXED_NEIGHBOR_MAP_INDEX; // 우 (X+1)
        }
        else
        {
            Debug.LogWarning("경고: 중앙 인접 고정 맵 프리셋이 할당되지 않았거나 null입니다. 해당 위치를 랜덤 또는 빈 공간으로 남깁니다.");
        }

        // 2. 나머지 구간 랜덤 배열
        if (randomMapIndices.Count == 0)
        {
            Debug.LogError("오류: 랜덤 맵 프리셋이 할당되지 않았습니다.");
        }

        for (int x = 0; x < MAP_GRID_SIZE; x++)
        {
            for (int y = 0; y < MAP_GRID_SIZE; y++)
            {
                // 모든 고정 맵 위치를 명시적으로 제외합니다. (중앙 상점 포함)
                bool isFixedSpot = 
                    (x == CENTER_INDEX && y == CENTER_INDEX) || // 중앙 (상점)
                    (x == 0 && y == 0) || (x == 0 && y == MAP_GRID_SIZE - 1) ||
                    (x == MAP_GRID_SIZE - 1 && y == 0) || (x == MAP_GRID_SIZE - 1 && y == MAP_GRID_SIZE - 1) || // 네 모서리 (스폰)
                    (x == CENTER_INDEX && y == CENTER_INDEX - 1) || (x == CENTER_INDEX && y == CENTER_INDEX + 1) ||
                    (x == CENTER_INDEX - 1 && y == CENTER_INDEX) || (x == CENTER_INDEX + 1 && y == CENTER_INDEX); // 중앙 인접 (고정 맵)

                if (isFixedSpot)
                {
                    // 고정된 위치라면 현재 할당된 값을 유지하고, 랜덤 맵 할당을 건너뜁니다.
                    continue;
                }

                // 고정 위치가 아니며, 아직 -1 (빈 공간)인 경우에만 랜덤 맵을 할당합니다.
                if (mapLayout[x, y] == -1)
                {
                    if (randomMapIndices.Count > 0)
                    {
                        int randomIndex = UnityEngine.Random.Range(0, randomMapIndices.Count);
                        mapLayout[x, y] = randomMapIndices[randomIndex];
                    }
                }
            }
        }

        // 3. 맵 레이아웃 데이터를 1차원 배열로 변환
        int[] flatLayout = new int[MAP_GRID_SIZE * MAP_GRID_SIZE];
        for (int x = 0; x < MAP_GRID_SIZE; x++)
        {
            for (int y = 0; y < MAP_GRID_SIZE; y++)
            {
                flatLayout[x * MAP_GRID_SIZE + y] = mapLayout[x, y];
            }
        }

        // 4. RPC 호출을 통해 모든 클라이언트에게 맵 정보 전송 및 생성 지시
        photonView.RPC("RPC_InstantiateMap", RpcTarget.AllBuffered, flatLayout);
    }

    /// <summary>
    /// 모든 클라이언트가 맵 배치 정보를 받아 맵 부모 오브젝트를 로컬에서 동적으로 생성하고,
    /// 맵 조각을 로컬에서 인스턴스화하여 부모의 자식으로 배치하는 RPC 메서드입니다.
    /// </summary>
    [PunRPC]
    private void RPC_InstantiateMap(int[] flatLayout)
    {
        Debug.Log("맵 레이아웃 데이터를 수신했습니다. 맵 부모와 조각 생성을 시작합니다.");

        // 1. 기존 맵 청소 및 새 부모 오브젝트 동적 생성
        if (mapParent != null)
        {
            Destroy(mapParent);
        }

        mapParent = new GameObject("MapRoot");
        mapParent.transform.position = Vector3.zero;

        // MapRoot를 Planes 오브젝트의 하위로 설정
        if (planesParent != null)
        {
            // MapRoot의 월드 위치(0,0,0)를 유지하며 planesParent의 자식으로 설정
            mapParent.transform.SetParent(planesParent, false); 
            Debug.Log("MapRoot를 Planes 부모 오브젝트의 자식으로 설정했습니다.");
        }
        else
        {
            Debug.LogWarning("경고: Planes Parent 오브젝트가 할당되지 않아 MapRoot가 씬 루트에 생성됩니다.");
        }

        // 2. 맵 조각 인스턴스화 및 부모 설정 (로컬 Instantiate)
        float halfPieceSize = PieceSize / 2f; // 맵 중앙 배치 오프셋 계산

        for (int i = 0; i < flatLayout.Length; i++)
        {
            int mapPieceIndex = flatLayout[i];

            // 인덱스가 -1 (빈 공간)인 경우 인스턴스화를 건너뜜
            if (mapPieceIndex == -1)
            {
                continue;
            }

            int x = i / MAP_GRID_SIZE;
            int y = i % MAP_GRID_SIZE;

            // 맵 조각의 중심이 그리드 사각형의 중앙에 오도록 오프셋 적용
            Vector3 position = new Vector3(
                (x * PieceSize) + halfPieceSize,
                0f,
                (y * PieceSize) + halfPieceSize
            );

            if (mapPieceIndex >= 0 && mapPieceIndex < MapPrefabs.Length)
            {
                GameObject prefabToInstantiate = MapPrefabs[mapPieceIndex];

                if (prefabToInstantiate != null)
                {
                    Instantiate(prefabToInstantiate, position, Quaternion.identity, mapParent.transform);
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

    [Tooltip("맵 레이아웃 생성이 완료되었고 NavMesh를 구울 필요가 없거나 굽기가 완료되었는지 여부를 반환합니다.")]
    public bool IsMapReady => hasGeneratedLayout && (!ShouldBakeNavMesh() || isNavMeshReady);

    private void CacheNavMeshSurfaces()
    {
        // 1. planesParent에서 NavMeshSurface를 최우선으로 찾습니다. (사용자 의도 반영)
        if (planesParent != null)
        {
            NavMeshSurface surfaceOnPlanes = planesParent.GetComponent<NavMeshSurface>();
            if (surfaceOnPlanes != null)
            {
                // Planes 오브젝트에 단일 Surface가 있다면 그것만 사용합니다.
                navMeshSurfaces = new NavMeshSurface[] { surfaceOnPlanes };
                Debug.Log("NavMeshSurface: Planes Parent에서 유효한 Surface를 찾았습니다.");
                return;
            }
        }

        // 2. Planes Parent에 없거나 설정되지 않았다면, 기존 로직대로 MapGenerator 자체 및 자식에서 찾습니다.
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
        // 맵 조각 인스턴스화 후 NavMeshSurface를 다시 캐시하여,
        // MapRoot가 Planes Parent에 연결된 후 Surface가 올바르게 인식되도록 합니다.
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

        // NavMesh를 굽는 코루틴을 시작합니다.
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
            // BuildNavMesh() 호출은 새로 생성된 모든 맵 바닥을 포함하여 NavMesh를 생성합니다.
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

    // --- 유니티 에디터 확인용 (Gizmo) ---
    // [Header("Editor Debugging (Gizmos)")] 속성은 메서드 위에 사용할 수 없으므로 제거했습니다.
    
    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        // 중앙 조각의 실제 중앙 위치를 표시하도록 Gizmo 좌표 업데이트
        Gizmos.color = Color.yellow;
        float centerOffset = PieceSize / 2f;
        float centerPosition = (CENTER_INDEX * PieceSize) + centerOffset;
        Gizmos.DrawSphere(new Vector3(centerPosition, 0, centerPosition), 0.5f);

        // 중앙 인접 고정 위치 강조 (새로운 기능)
        Gizmos.color = Color.magenta;
        // 상하좌우
        Gizmos.DrawWireCube(new Vector3(centerPosition, 0, (CENTER_INDEX - 1) * PieceSize + centerOffset), new Vector3(PieceSize, 0.1f, PieceSize));
        Gizmos.DrawWireCube(new Vector3(centerPosition, 0, (CENTER_INDEX + 1) * PieceSize + centerOffset), new Vector3(PieceSize, 0.1f, PieceSize));
        Gizmos.DrawWireCube(new Vector3((CENTER_INDEX - 1) * PieceSize + centerOffset, 0, centerPosition), new Vector3(PieceSize, 0.1f, PieceSize));
        Gizmos.DrawWireCube(new Vector3((CENTER_INDEX + 1) * PieceSize + centerOffset, 0, centerPosition), new Vector3(PieceSize, 0.1f, PieceSize));

        // 그리드 선 그리기 (맵 조각의 경계는 그대로 유지됨)
        Gizmos.color = Color.white;
        float totalSize = MAP_GRID_SIZE * PieceSize;

        for (int i = 0; i <= MAP_GRID_SIZE; i++)
        {
            // X축 선
            Vector3 startX = new Vector3(i * PieceSize, 0, 0);
            Vector3 endX = new Vector3(i * PieceSize, 0, totalSize);
            Gizmos.DrawLine(startX, endX);

            // Z축 선
            Vector3 startZ = new Vector3(0, 0, i * PieceSize);
            Vector3 endZ = new Vector3(totalSize, 0, i * PieceSize);
            Gizmos.DrawLine(startZ, endZ);
        }
    }
}