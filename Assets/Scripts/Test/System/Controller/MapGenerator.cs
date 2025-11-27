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

    // 맵 조각의 종류를 식별하기 위한 상수 (MapPrefabs 배열의 인덱스와 일치해야 합니다)
    // 요청하신 인덱스로 설정합니다. (맵 레이아웃의 초기값 0과 충돌 방지를 위해 로직을 변경했습니다.)
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
        if (randomMapIndices.Count == 0) // 중복 초기화 방지
        {
            // RANDOM_MAP_START_INDEX (2)부터 MapPrefabs의 끝까지 인덱스를 추가
            for (int i = RANDOM_MAP_START_INDEX; i < MapPrefabs.Length; i++)
            {
                randomMapIndices.Add(i);
            }
        }
    }

    void Awake()
    {
        CacheNavMeshSurfaces();
        InitializeRandomMaps();
    }

    /// <summary>
    /// 마스터 클라이언트에서 맵 배치 정보를 생성하고 RPC로 전송합니다.
    /// </summary>
    private void GenerateMapLayout()
    {
        // 9x9 그리드에 어떤 프리셋 인덱스를 배치할지 저장하는 배열
        // 초기값은 모두 0입니다. (SPAWN_MAP_INDEX와 충돌하지만, 위치 체크로 해결합니다.)
        int[,] mapLayout = new int[MAP_GRID_SIZE, MAP_GRID_SIZE];

        // 1. 필수 맵 배치 (상점 및 스폰 지역)

        // 중앙 상점 맵 배치 (조건 2)
        mapLayout[CENTER_INDEX, CENTER_INDEX] = SHOP_MAP_INDEX; // (4, 4) = 1

        // 각 꼭짓점 스폰 맵 배치 (조건 3)
        mapLayout[0, 0] = SPAWN_MAP_INDEX;      // (0, 0) = 0
        mapLayout[0, MAP_GRID_SIZE - 1] = SPAWN_MAP_INDEX; // (0, 8) = 0
        mapLayout[MAP_GRID_SIZE - 1, 0] = SPAWN_MAP_INDEX; // (8, 0) = 0
        mapLayout[MAP_GRID_SIZE - 1, MAP_GRID_SIZE - 1] = SPAWN_MAP_INDEX; // (8, 8) = 0

        // 2. 나머지 구간 랜덤 배열 (중복 허용하여 필수 맵 위치 제외하고 모두 채우기)

        if (randomMapIndices.Count == 0)
        {
            Debug.LogError("오류: 랜덤 맵 프리셋이 할당되지 않았습니다. MapPrefabs 인덱스 2번 이후에 랜덤 맵을 할당해주세요.");
            return;
        }

        for (int x = 0; x < MAP_GRID_SIZE; x++)
        {
            for (int y = 0; y < MAP_GRID_SIZE; y++)
            {
                // 필수 맵 5개 위치인지 확인
                bool isMandatorySpot = 
                    (x == CENTER_INDEX && y == CENTER_INDEX) || // 중앙 상점
                    (x == 0 && y == 0) ||                       // 좌상단 스폰
                    (x == 0 && y == MAP_GRID_SIZE - 1) ||       // 우상단 스폰
                    (x == MAP_GRID_SIZE - 1 && y == 0) ||       // 좌하단 스폰
                    (x == MAP_GRID_SIZE - 1 && y == MAP_GRID_SIZE - 1); // 우하단 스폰

                // 필수 맵 위치가 아닌 경우에만 랜덤 맵 배치
                if (!isMandatorySpot)
                {
                    // 중복을 허용하며 무작위로 하나 선택하여 배치
                    int randomIndex = UnityEngine.Random.Range(0, randomMapIndices.Count);
                    mapLayout[x, y] = randomMapIndices[randomIndex];
                }
                // 필수 맵 위치(5곳)는 이미 1단계에서 정확한 인덱스가 할당되었으므로 건너뜁니다.
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
    /// 모든 클라이언트가 맵 배치 정보를 받아 맵을 인스턴스화하는 RPC 메서드입니다.
    /// </summary>
    [PunRPC]
    private void RPC_InstantiateMap(int[] flatLayout)
    {
        Debug.Log("맵 레이아웃 데이터를 수신했습니다. 맵 생성을 시작합니다.");

        for (int i = 0; i < flatLayout.Length; i++)
        {
            int mapPieceIndex = flatLayout[i];

            // 1차원 인덱스를 2차원 그리드 좌표 (x, y)로 변환
            int x = i / MAP_GRID_SIZE;
            int y = i % MAP_GRID_SIZE;

            // 월드 좌표 계산
            Vector3 position = new Vector3(x * PieceSize, 0f, y * PieceSize);

            // 프리팹 인스턴스화
            if (mapPieceIndex >= 0 && mapPieceIndex < MapPrefabs.Length)
            {
                GameObject prefabToInstantiate = MapPrefabs[mapPieceIndex];

                if (prefabToInstantiate != null)
                {
                    // 일반 Instantiate를 사용하여 로컬에서 맵을 그립니다.
                    Instantiate(prefabToInstantiate, position, Quaternion.identity);
                }
                else
                {
                    Debug.LogError($"맵 생성 실패: 인덱스 {mapPieceIndex}의 프리팹 참조가 깨졌습니다. 위치: ({x}, {y})");
                }
            }
            else
            {
                Debug.LogError($"잘못된 맵 프리셋 인덱스: {mapPieceIndex}. 위치: ({x}, {y})");
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