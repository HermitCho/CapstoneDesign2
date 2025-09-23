using UnityEngine;

public class PlacementPreviewComponent : MonoBehaviour, IPlacementPreview
{
    protected GameObject ghostPrefab;
    protected GameObject ghostInstance;
    public Transform setTransform;


    public void GetGhostPrefab(GameObject placementPrefab)
    {
        ghostPrefab = placementPrefab;
    }
    public void StartPreview(SkillController owner)
    {
        if (ghostPrefab == null) return;
        ghostInstance = Instantiate(ghostPrefab);
        SetTransparent(ghostInstance, 0.45f);
    }

    public void UpdatePreview(Vector3 worldPos, Quaternion rot)
    {
        // 고스트 인스턴스가 없으면 함수 종료
        if (ghostInstance == null) return;

        // worldPos의 Y 값을 0으로 설정하여 지면에서 시작하는 레이 생성
        Vector3 rayOrigin = new Vector3(worldPos.x, 0f, worldPos.z);
        // 아래 방향으로 레이를 쏨
        Vector3 rayDirection = Vector3.down;

        // 레이 충돌 정보를 저장할 변수
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, 2))
        {
            ghostInstance.transform.position = hit.point;

            // Y축 회전만 적용
            Vector3 currentEuler = ghostInstance.transform.rotation.eulerAngles;
            // 회전 쿼터니언을 오일러 각으로 변환
            Vector3 rotEuler = rot.eulerAngles;
            // Y축 값만 새롭게 설정
            ghostInstance.transform.rotation = Quaternion.Euler(currentEuler.x, rotEuler.y, currentEuler.z);
        }
        else
        {
            // 만약 레이가 아무것도 충돌하지 않았다면,
            // (옵션) ghostInstance를 비활성화하거나 다른 처리를 할 수 있습니다.
            // 예: ghostInstance.SetActive(false);
            Debug.LogWarning("레이가 충돌한 지점을 찾지 못했습니다.");
        }
    }

    public void EndPreview()
    {
        if (ghostInstance != null) Destroy(ghostInstance);
    }

    void SetTransparent(GameObject go, float alpha)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        foreach (var r in rends)
        {
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor("_BaseColor", new Color(1f, 1f, 1f, alpha));
            r.SetPropertyBlock(block);
        }
    }

    // 인터페이스의 새로운 메서드 구현
    public Vector3 GetPlacementPosition()
    {
        // ghostInstance가 없으면 Vector3.zero를 반환하거나 다른 기본값을 설정합니다.
        if (ghostInstance == null) return Vector3.zero;
        return ghostInstance.transform.position;
    }

    public Quaternion GetPlacementRotation()
    {
        if (ghostInstance == null) return Quaternion.identity;
        return ghostInstance.transform.rotation;
    }
}
