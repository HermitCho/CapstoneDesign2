using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// 로비 BGM 컨트롤러 (Photon 멀티플레이어 환경)
/// BGM_Lobby_Main과 BGM_Lobby_Main2 중 랜덤으로 선택하여 페이드 인/아웃과 함께 무한 반복 재생
/// 로컬 클라이언트만 BGM 재생 (각자 자신의 AudioManager 사용)
/// </summary>
public class LobbyBGMController : MonoBehaviour
{
    private bool isBGMPlaying = false;
    
    void Start()
    {
        // ✅ BGM은 각 클라이언트에서 로컬로 재생
        // AudioManager가 싱글톤이므로 중복 재생 방지됨
        PlayRandomBGM();
    }

    void OnDestroy()
    {
        // ✅ 각 클라이언트에서 자신의 BGM 중지
        StopLobbyBGM();
    }

    /// <summary>
    /// 로비 랜덤 BGM 재생 (페이드 인/아웃 + 무한 반복)
    /// BGM_Lobby_Main과 BGM_Lobby_Main2 중 랜덤 선택
    /// </summary>
    public void PlayRandomBGM()
    {
        if (AudioManager.Inst != null && !isBGMPlaying)
        {
            isBGMPlaying = true;
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
            isBGMPlaying = true;
        }
    }
    
    /// <summary>
    /// BGM 정지
    /// </summary>
    public void StopLobbyBGM()
    {
        if (AudioManager.Inst != null && isBGMPlaying)
        {
            AudioManager.Inst.StopBGMLoop();
            isBGMPlaying = false;
        }
    }
}
