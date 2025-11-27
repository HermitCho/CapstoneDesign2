using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Unity.AI.Navigation;
using UnityEngine;

public class MapGenerator : MonoBehaviourPunCallbacks
{
    // 맵 조각 프리팹 목록. 인덱스로 맵 타입을 식별합니다.
    public GameObject[] MapPrefabs;
    
    // 각 맵 조각의 실제 월드 크기 (Gizmo의 흰 선 간격)
    public float PieceSize = 15f;

    [Header("NavMesh Settings")]
    [Tooltip("맵 배치 직후 다시 빌드할 NavMeshSurface 목록입니다.")]
    [SerializeField] private NavMeshSurface[] navMeshSurfaces;
    [Tooltip("프리팹 배치 직후 기다렸다가 네브메시를 굽기 위한 딜레이(초)")]
    [SerializeField] private float navMeshBakeDelay = 0.2f;

    // 생성된 모든 맵 조각의 부모가 될 게임 오브젝트 (RPC 수신 시 동적 생성됨)
    private GameObject mapParent; 

    // 맵 조각의 종류를 식별하기 위한 상수 (MapPrefabs 배열의 인덱스와 일치해야 합니다)
    private const int SPAWN_MAP_INDEX = 0;      // MapPrefabs[0]에 스폰 맵 할당 필요
    private const int SHOP_MAP_INDEX = 1;       // MapPrefabs[1]에 상점 맵 할당 필요
    private const int RANDOM_MAP_START_INDEX = 2; // MapPrefabs[2]부터 랜덤 맵 할당 필요

    // 맵 크기
    private const int MAP_GRID_SIZE = 9; // 9x9 크기
    private const int CENTER_INDEX = MAP_GRID_SIZE / 2; // 중앙 인덱스 (4)

    // 랜덤 맵 생성을 위한 리스트
    private List<int> randomMapIndices = new List<int>();
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

        // 1. 필수 맵 배치
        mapLayout[CENTER_INDEX, CENTER_INDEX] = SHOP_MAP_INDEX; 
        mapLayout[0, 0] = SPAWN_MAP_INDEX;
        mapLayout[0, MAP_GRID_SIZE - 1] = SPAWN_MAP_INDEX;
        mapLayout[MAP_GRID_SIZE - 1, 0] = SPAWN_MAP_INDEX;
        mapLayout[MAP_GRID_SIZE - 1, MAP_GRID_SIZE - 1] = SPAWN_MAP_INDEX;

        // 2. 나머지 구간 랜덤 배열
        if (randomMapIndices.Count == 0)
        {
            Debug.LogError("오류: 랜덤 맵 프리셋이 할당되지 않았습니다.");
            return;
        }

        for (int x = 0; x < MAP_GRID_SIZE; x++)
        {
            for (int y = 0; y < MAP_GRID_SIZE; y++)
            {
                // 필수 맵 위치 확인
                bool isMandatorySpot = 
                     (x == CENTER_INDEX && y == CENTER_INDEX) || 
                     (x == 0 && y == 0) || 
                     (x == 0 && y == MAP_GRID_SIZE - 1) || 
                     (x == MAP_GRID_SIZE - 1 && y == 0) || 
                     (x == MAP_GRID_SIZE - 1 && y == MAP_GRID_SIZE - 1);
                
                if (!isMandatorySpot)
                {
                    int randomIndex = UnityEngine.Random.Range(0, randomMapIndices.Count);
                    mapLayout[x, y] = randomMapIndices[randomIndex];
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
            // 기존 맵 오브젝트들이 있다면 파괴하고 새로 만듭니다.
            Destroy(mapParent);
        }

        // 새 부모 오브젝트를 동적으로 생성합니다. (모든 클라이언트의 Hierarchy를 정리)
        mapParent = new GameObject("MapRoot");
        mapParent.transform.position = Vector3.zero;
        
        // 2. 맵 조각 인스턴스화 및 부모 설정 (로컬 Instantiate)
        for (int i = 0; i < flatLayout.Length; i++)
        {
            int mapPieceIndex = flatLayout[i];

            int x = i / MAP_GRID_SIZE;
            int y = i % MAP_GRID_SIZE;

            Vector3 position = new Vector3(x * PieceSize, 0f, y * PieceSize);

            if (mapPieceIndex >= 0 && mapPieceIndex < MapPrefabs.Length)
            {
                GameObject prefabToInstantiate = MapPrefabs[mapPieceIndex];

                if (prefabToInstantiate != null)
                {
                    // 맵 조각을 로컬 Instantiate로 생성하고 부모를 지정합니다.
                    // 맵 배치는 flatLayout 데이터에 의해 동기화됩니다.
                    Instantiate(prefabToInstantiate, position, Quaternion.identity, mapParent.transform);
                }
                else
                {
                    Debug.LogError($"맵 생성 실패: 인덱스 {mapPieceIndex}의 프리팹 참조가 깨졌습니다.");
                }
            }
            else
            {
                Debug.LogError($"잘못된 맵 프리셋 인덱스: {mapPieceIndex}.");
            }
        }

        OnMapPiecesInstantiated();
    }
    
    public bool IsMapReady => hasGeneratedLayout && (!ShouldBakeNavMesh() || isNavMeshReady);

    private void CacheNavMeshSurfaces()
    {
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
        if (!ShouldBakeNavMesh())
        {
            isNavMeshReady = true;
            SetGlobalMapReady(true);
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            SetGlobalMapReady(true);
            return; // 네브메시는 마스터에서만 필요
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

    // --- 유니티 에디터 확인용 (Gizmo) ---

    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        // 중앙 (4, 4)와 꼭짓점 (0,0, 0,8, 8,0, 8,8)을 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(new Vector3(CENTER_INDEX * PieceSize, 0, CENTER_INDEX * PieceSize), 0.5f);

        // 그리드 선 그리기 (9x9 크기 확인)
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