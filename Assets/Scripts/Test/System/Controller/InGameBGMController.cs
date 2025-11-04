using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// 인게임 BGM 컨트롤러 (Photon 멀티플레이어 환경)
/// BGM_InGame_Ready를 페이드 인/아웃과 함께 무한 반복 재생
/// 로컬 클라이언트만 BGM 재생 (각자 자신의 AudioManager 사용)
/// </summary>
public class InGameBGMController : MonoBehaviour
{
    private bool isBGMPlaying = false;
    
    void Start()
    {
        // ✅ BGM은 각 클라이언트에서 로컬로 재생
        // AudioManager가 싱글톤이므로 중복 재생 방지됨
        PlayInGameBGM();
    }

    void OnDestroy()
    {
        // ✅ 각 클라이언트에서 자신의 BGM 중지
        StopInGameBGM();
    }

    /// <summary>
    /// 인게임 BGM 재생 (페이드 인/아웃 + 무한 반복)
    /// </summary>
    public void PlayInGameBGM()
    {
        if (AudioManager.Inst != null && !isBGMPlaying)
        {
            isBGMPlaying = true;
            // AudioManager의 BGM 반복 재생 기능 사용
            AudioManager.Inst.PlayBGMWithLoop("BGM_InGame_Ready");
        }
    }
    
    /// <summary>
    /// BGM 정지
    /// </summary>
    public void StopInGameBGM()
    {
        if (AudioManager.Inst != null && isBGMPlaying)
        {
            AudioManager.Inst.StopBGMLoop();
            isBGMPlaying = false;
        }
    }
}
