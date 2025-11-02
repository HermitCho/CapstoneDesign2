using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로비 BGM 컨트롤러
/// BGM_Lobby_Main을 페이드 인/아웃과 함께 무한 반복 재생
/// </summary>
public class LobbyBGMController : MonoBehaviour
{
    void Start()
    {
        PlayMainBGM();
    }

    void OnDestroy()
    {
        // 로비 BGM 반복 재생 중지
        if (AudioManager.Inst != null)
        {
            AudioManager.Inst.StopBGMLoop();
        }
    }

    /// <summary>
    /// 로비 메인 BGM 재생 (페이드 인/아웃 + 무한 반복)
    /// </summary>
    public void PlayMainBGM()
    {
        if (AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayBGMWithLoop("BGM_Lobby_Main");
        }
    }
}
