using UnityEditor;
using UnityEngine;
using System.IO;

public class RemoveLowerBodyCurves : MonoBehaviour
{
    [MenuItem("Tools/Animation/Remove Lower Body Curves From Selected .anim")]
    static void RemoveLowerBodyFromAnim()
    {
        var selected = Selection.activeObject;

        if (!(selected is AnimationClip clip))
        {
            Debug.LogError("선택한 파일이 AnimationClip이 아닙니다.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(clip);
        string backupPath = assetPath.Replace(".anim", "_Backup.anim");

        // ✅ 백업 생성
        if (!File.Exists(backupPath))
        {
            File.Copy(assetPath, backupPath);
            Debug.Log($"백업 생성됨: {backupPath}");
        }

        var bindings = AnimationUtility.GetCurveBindings(clip);
        int removedCount = 0;

        string[] lowerBodyKeywords = new string[]
        {
            "Hips",
            "UpperLeg",
            "LowerLeg",
            "Foot",
            "Toe"
        };

        foreach (var binding in bindings)
        {
            foreach (var keyword in lowerBodyKeywords)
            {
                if (binding.path.Contains(keyword))
                {
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    removedCount++;
                    break;
                }
            }
        }

        Debug.Log($"하체 관련 Curve {removedCount}개 제거 완료: {clip.name}");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}