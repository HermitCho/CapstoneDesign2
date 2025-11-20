#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class FindLobbyModal : EditorWindow
{
    [MenuItem("Tools/Find LobbyModalController")]
    public static void FindAll()
    {
        var allControllers = Resources.FindObjectsOfTypeAll<LobbyModalController>();
        foreach (var controller in allControllers)
        {
            string path = controller.gameObject.scene.IsValid() 
                ? controller.gameObject.scene.name 
                : AssetDatabase.GetAssetPath(controller.gameObject);
            Debug.Log($"[Found] {controller.name} in {path}", controller);
        }
    }
}
#endif