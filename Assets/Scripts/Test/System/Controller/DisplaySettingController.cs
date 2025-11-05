using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 시작 시 저장된 디스플레이 설정(해상도 등)을 자동으로 적용
/// 첫 씬(로그인/메인메뉴)에 할당하여 사용
/// </summary>
public class DisplaySettingController : MonoBehaviour
{
    // PlayerPrefs 키
    private const string PREF_RESOLUTION_INDEX = "ResolutionIndex";
    private const int DEFAULT_RESOLUTION_INDEX = 0; // 0: FHD, 1: QHD
    
    void Awake()
    {
        // 게임 시작 시 저장된 해상도 적용
        ApplySavedResolution();
    }
    
    /// <summary>
    /// 저장된 해상도를 불러와서 적용
    /// </summary>
    private void ApplySavedResolution()
    {
        // PlayerPrefs에서 저장된 해상도 인덱스 가져오기
        int savedIndex = PlayerPrefs.GetInt(PREF_RESOLUTION_INDEX, DEFAULT_RESOLUTION_INDEX);
        
        // 해상도 적용
        ApplyResolution(savedIndex);
        
        Debug.Log($"DisplaySettingController: 저장된 해상도 적용 - Index: {savedIndex}");
    }
    
    /// <summary>
    /// 해상도 인덱스에 따라 해상도 적용
    /// </summary>
    /// <param name="resolutionIndex">0: FHD, 1: QHD</param>
    public static void ApplyResolution(int resolutionIndex)
    {
        int width = 1920;
        int height = 1080;
        
        switch (resolutionIndex)
        {
            case 0: // FHD (1920 x 1080)
                width = 1920;
                height = 1080;
                break;
            case 1: // QHD (2560 x 1440)
                width = 2560;
                height = 1440;
                break;
            default:
                Debug.LogWarning($"DisplaySettingController: 알 수 없는 해상도 인덱스 - {resolutionIndex}, FHD로 설정");
                width = 1920;
                height = 1080;
                break;
        }
        
        Debug.Log($"DisplaySettingController: 해상도 변경 시작 - {width} x {height}");
        
        // 현재 전체화면 모드 가져오기
        FullScreenMode currentMode = Screen.fullScreenMode;
        
        // 해상도 변경 (전체화면 모드 유지)
        Screen.SetResolution(width, height, currentMode);
        
        Debug.Log($"DisplaySettingController: 해상도 변경 완료 - {width} x {height} (모드: {currentMode})");
        Debug.Log($"DisplaySettingController: 실제 적용된 해상도 - {Screen.width} x {Screen.height}");
    }
    
    /// <summary>
    /// 해상도 이름 가져오기
    /// </summary>
    public static string GetResolutionName(int resolutionIndex)
    {
        switch (resolutionIndex)
        {
            case 0: return "FHD (1920 x 1080)";
            case 1: return "QHD (2560 x 1440)";
            default: return "Unknown";
        }
    }
}
