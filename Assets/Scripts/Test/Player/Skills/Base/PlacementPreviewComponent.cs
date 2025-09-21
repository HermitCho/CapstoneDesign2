using UnityEngine;

public class PlacementPreviewComponent : MonoBehaviour, IPlacementPreview
{
    protected GameObject ghostPrefab;
    protected GameObject ghostInstance;


    public void GetGhostPrefab(GameObject placementPrefab)
    {
        ghostPrefab = placementPrefab;
    }
    public void StartPreview(MoveController owner)
    {
        if (ghostPrefab == null) return;
        ghostInstance = Instantiate(ghostPrefab);
        SetTransparent(ghostInstance, 0.45f);
    }

    public void UpdatePreview(Vector3 worldPos, Quaternion rot)
    {
        if (ghostInstance == null) return;
        ghostInstance.transform.SetPositionAndRotation(worldPos, rot);
    }

    public void EndPreview()
    {
        if (ghostInstance != null) Destroy(ghostInstance);
    }

    void SetTransparent(GameObject go, float alpha)
    {
        // 간단한 방법: 각 Renderer의 material color alpha 변경 (MaterialPropertyBlock 권장)
        var rends = go.GetComponentsInChildren<Renderer>();
        foreach (var r in rends)
        {
            foreach (var mat in r.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                }
            }
        }
    }
}
