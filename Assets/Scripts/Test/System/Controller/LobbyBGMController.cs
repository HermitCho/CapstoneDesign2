using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로비 BGM 컨트롤러
/// BGM_Lobby_Main과 BGM_Lobby_Main2 중 랜덤으로 선택하여 페이드 인/아웃과 함께 무한 반복 재생
/// </summary>
public class LobbyBGMController : MonoBehaviour
{
    void Start()
    {
        PlayRandomBGM();
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
    /// 로비 랜덤 BGM 재생 (페이드 인/아웃 + 무한 반복)
    /// BGM_Lobby_Main과 BGM_Lobby_Main2 중 랜덤 선택
    /// </summary>
    public void PlayRandomBGM()
    {
        if (AudioManager.Inst != null)
        {
            // AudioManager의 새로운 랜덤 BGM 기능 사용
            AudioManager.Inst.PlayBGMWithRandomLoop("BGM_Lobby_Main", "BGM_Lobby_Main2");
        }
    }
    
    /// <summary>
    /// 특정 BGM만 재생 (외부 호출용)
    /// </summary>
    public void PlaySingleBGM(string bgmName)
    {
        if (AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayBGMWithLoop(bgmName);
        }
    }
}
