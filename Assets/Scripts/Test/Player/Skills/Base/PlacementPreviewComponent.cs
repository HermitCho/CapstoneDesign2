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
        if (ghostInstance == null) return;

        Vector3 rayOrigin = worldPos + Vector3.up * 1.0f;
        Vector3 rayDirection = Vector3.down;

        RaycastHit hit;

        int layerMask = 1 << LayerMask.NameToLayer("Ground");

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, 5f, layerMask))
        {

            Vector3 targetPos = hit.point;

            float smoothY = Mathf.Lerp(ghostInstance.transform.position.y, targetPos.y, Time.deltaTime * 20f);

            ghostInstance.transform.position = new Vector3(targetPos.x, smoothY, targetPos.z);


            Vector3 currentEuler = ghostInstance.transform.rotation.eulerAngles;
            Vector3 rotEuler = rot.eulerAngles;
            ghostInstance.transform.rotation = Quaternion.Euler(currentEuler.x, rotEuler.y, currentEuler.z);
        }
        else
        {

            Vector3 fallbackPos = new Vector3(worldPos.x, ghostInstance.transform.position.y, worldPos.z);
            ghostInstance.transform.position = fallbackPos;
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
