using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{

    [Header("맵 빈 오브젝트")]
    [SerializeField] private GameObject map;

    [Header("바닥, 상점 구역, 벽 프리팹 참조")]
    [SerializeField] private GameObject floor;
    [SerializeField] private GameObject middleFloor;
    [SerializeField] private GameObject wall;

    [Header("초기 바닥 생성 위치 (기준점)")]
    [SerializeField] private Vector3 firstFloorPosition;

    [Header("바닥 생성 개수")]
    [Tooltip("세로 바닥 생성 개수")]
    [SerializeField] private int verticalFloorCount = 10;
    [Tooltip("가로 바닥 생성 개수")]
    [SerializeField] private int horizontalFloorCount = 10;

    // 생성된 바닥들을 저장할 리스트
    private List<GameObject> generatedFloors = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        GenerateMap();
    }

    /// <summary>
    /// 맵 생성 메서드
    /// </summary>
    private void GenerateMap()
    {
        if (middleFloor == null)
        {
            Debug.LogError("❌ MapController - middleFloor 프리팹이 할당되지 않았습니다.");
            return;
        }

        if (floor == null)
        {
            Debug.LogError("❌ MapController - floor 프리팹이 할당되지 않았습니다.");
            return;
        }

        if (map == null)
        {
            Debug.LogError("❌ MapController - map 오브젝트가 할당되지 않았습니다.");
            return;
        }

        // 기존에 생성된 바닥들 정리
        ClearGeneratedFloors();

        // 1. 중앙에 middleFloor 생성
        GameObject centerFloor = Instantiate(middleFloor, firstFloorPosition, Quaternion.identity);
        centerFloor.transform.SetParent(map.transform); // map의 자식으로 설정
        generatedFloors.Add(centerFloor);
        Debug.Log($"✅ MapController - 중앙 바닥 생성 완료: {firstFloorPosition}");

        // 2. middleFloor의 크기 가져오기
        Vector3 middleFloorSize = GetObjectSize(middleFloor);
        Debug.Log($"📏 MapController - 중앙 바닥 크기: {middleFloorSize}");

        // 3. middleFloor 주변에 floor들 생성
        GenerateSurroundingFloors(firstFloorPosition, middleFloorSize);

        Debug.Log($"✅ MapController - 맵 생성 완료! 총 {generatedFloors.Count}개의 바닥 생성됨 (모두 {map.name}의 자식으로 설정됨)");
    }

    /// <summary>
    /// 중앙 바닥 주변에 floor들 생성
    /// </summary>
    /// <param name="centerPosition">중앙 바닥의 위치</param>
    /// <param name="centerSize">중앙 바닥의 크기</param>
    private void GenerateSurroundingFloors(Vector3 centerPosition, Vector3 centerSize)
    {
        // floor의 크기 가져오기
        Vector3 floorSize = GetObjectSize(floor);
        
        // 높이를 맞추기 위해 Y 위치를 centerPosition.y로 통일
        float unifiedY = centerPosition.y;
        
        // middleFloor의 경계 계산
        float middleFloorHalfWidth = centerSize.x * 0.5f;
        float middleFloorHalfDepth = centerSize.z * 0.5f;
        
        float leftBoundary = centerPosition.x - middleFloorHalfWidth;
        float rightBoundary = centerPosition.x + middleFloorHalfWidth;
        float bottomBoundary = centerPosition.z - middleFloorHalfDepth;
        float topBoundary = centerPosition.z + middleFloorHalfDepth;
        
        Debug.Log($"🔍 middleFloor 경계 - Left: {leftBoundary}, Right: {rightBoundary}, Bottom: {bottomBoundary}, Top: {topBoundary}");
        
        // 1. 위쪽 영역 (Top) - middleFloor 위쪽에 verticalFloorCount 줄
        for (int row = 1; row <= verticalFloorCount; row++)
        {
            float zPos = topBoundary + (row - 1) * floorSize.z + floorSize.z * 0.5f;
            
            // 전체 가로 길이 계산 (middleFloor + 양옆 floor 영역)
            int totalCols = horizontalFloorCount * 2 + Mathf.CeilToInt(centerSize.x / floorSize.x);
            float totalWidth = totalCols * floorSize.x;
            float startX = centerPosition.x - totalWidth * 0.5f + floorSize.x * 0.5f;
            
            for (int col = 0; col < totalCols; col++)
            {
                float xPos = startX + col * floorSize.x;
                Vector3 floorPosition = new Vector3(xPos, unifiedY, zPos);
                GameObject newFloor = Instantiate(floor, floorPosition, Quaternion.identity);
                newFloor.transform.SetParent(map.transform); // map의 자식으로 설정
                generatedFloors.Add(newFloor);
            }
        }
        
        // 2. 아래쪽 영역 (Bottom) - middleFloor 아래쪽에 verticalFloorCount 줄
        for (int row = 1; row <= verticalFloorCount; row++)
        {
            float zPos = bottomBoundary - (row - 1) * floorSize.z - floorSize.z * 0.5f;
            
            // 전체 가로 길이 계산
            int totalCols = horizontalFloorCount * 2 + Mathf.CeilToInt(centerSize.x / floorSize.x);
            float totalWidth = totalCols * floorSize.x;
            float startX = centerPosition.x - totalWidth * 0.5f + floorSize.x * 0.5f;
            
            for (int col = 0; col < totalCols; col++)
            {
                float xPos = startX + col * floorSize.x;
                Vector3 floorPosition = new Vector3(xPos, unifiedY, zPos);
                GameObject newFloor = Instantiate(floor, floorPosition, Quaternion.identity);
                newFloor.transform.SetParent(map.transform); // map의 자식으로 설정
                generatedFloors.Add(newFloor);
            }
        }
        
        // 3. 왼쪽 영역 (Left) - middleFloor 왼쪽에 horizontalFloorCount 열
        for (int col = 1; col <= horizontalFloorCount; col++)
        {
            float xPos = leftBoundary - (col - 1) * floorSize.x - floorSize.x * 0.5f;
            
            // middleFloor와 같은 높이 범위에만 배치
            int middleFloorRows = Mathf.CeilToInt(centerSize.z / floorSize.z);
            float startZ = centerPosition.z - (middleFloorRows * floorSize.z) * 0.5f + floorSize.z * 0.5f;
            
            for (int row = 0; row < middleFloorRows; row++)
            {
                float zPos = startZ + row * floorSize.z;
                Vector3 floorPosition = new Vector3(xPos, unifiedY, zPos);
                GameObject newFloor = Instantiate(floor, floorPosition, Quaternion.identity);
                newFloor.transform.SetParent(map.transform); // map의 자식으로 설정
                generatedFloors.Add(newFloor);
            }
        }
        
        // 4. 오른쪽 영역 (Right) - middleFloor 오른쪽에 horizontalFloorCount 열
        for (int col = 1; col <= horizontalFloorCount; col++)
        {
            float xPos = rightBoundary + (col - 1) * floorSize.x + floorSize.x * 0.5f;
            
            // middleFloor와 같은 높이 범위에만 배치
            int middleFloorRows = Mathf.CeilToInt(centerSize.z / floorSize.z);
            float startZ = centerPosition.z - (middleFloorRows * floorSize.z) * 0.5f + floorSize.z * 0.5f;
            
            for (int row = 0; row < middleFloorRows; row++)
            {
                float zPos = startZ + row * floorSize.z;
                Vector3 floorPosition = new Vector3(xPos, unifiedY, zPos);
                GameObject newFloor = Instantiate(floor, floorPosition, Quaternion.identity);
                newFloor.transform.SetParent(map.transform); // map의 자식으로 설정
                generatedFloors.Add(newFloor);
            }
        }
        
        Debug.Log($"✅ MapController - 주변 바닥 생성 완료! floorSize: {floorSize}, centerSize: {centerSize}");
    }

    /// <summary>
    /// 오브젝트의 크기를 가져오는 메서드
    /// </summary>
    /// <param name="obj">크기를 확인할 오브젝트</param>
    /// <returns>오브젝트의 크기 (Vector3)</returns>
    private Vector3 GetObjectSize(GameObject obj)
    {
        if (obj == null) return Vector3.one;

        // 프리팹인 경우 임시로 인스턴스화해서 크기 측정
        GameObject tempInstance = null;
        GameObject targetObj = obj;
        
        // 프리팹인지 확인 (씬에 없는 오브젝트)
        if (obj.scene.name == null || obj.scene.name == "")
        {
            // 임시로 인스턴스화
            tempInstance = Instantiate(obj);
            targetObj = tempInstance;
        }

        Vector3 size = Vector3.one;

        // Renderer 컴포넌트로 크기 측정
        Renderer renderer = targetObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            size = renderer.bounds.size;
        }
        // Collider 컴포넌트로 크기 측정
        else
        {
            Collider collider = targetObj.GetComponent<Collider>();
            if (collider != null)
            {
                size = collider.bounds.size;
            }
            else
            {
                // 모든 자식 오브젝트의 Renderer를 확인
                Renderer[] childRenderers = targetObj.GetComponentsInChildren<Renderer>();
                if (childRenderers.Length > 0)
                {
                    Bounds combinedBounds = childRenderers[0].bounds;
                    for (int i = 1; i < childRenderers.Length; i++)
                    {
                        combinedBounds.Encapsulate(childRenderers[i].bounds);
                    }
                    size = combinedBounds.size;
                }
                else
                {
                    // 최후의 수단으로 Transform의 localScale 사용
                    size = targetObj.transform.localScale;
                }
            }
        }

        // 임시 인스턴스 삭제
        if (tempInstance != null)
        {
            DestroyImmediate(tempInstance);
        }

        Debug.Log($"📏 GetObjectSize - {obj.name} 크기: {size}");
        return size;
    }

    /// <summary>
    /// 생성된 바닥들을 정리하는 메서드
    /// </summary>
    private void ClearGeneratedFloors()
    {
        foreach (GameObject floor in generatedFloors)
        {
            if (floor != null)
            {
                DestroyImmediate(floor);
            }
        }
        generatedFloors.Clear();
    }

    /// <summary>
    /// 맵을 다시 생성하는 공개 메서드
    /// </summary>
    public void RegenerateMap()
    {
        GenerateMap();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
