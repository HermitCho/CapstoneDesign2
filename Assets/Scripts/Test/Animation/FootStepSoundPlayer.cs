using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepSoundPlayer : MonoBehaviour
{
    [Header("발소리 클립 이름 (AudioManager Playlist에 있어야 함)")]
    [SerializeField] private string footstepClipName = "SFX_Game_FootStep";

    [Header("점프 사운드 클립 이름")]
    [SerializeField] private string jumpClipName = "SFX_Game_JumpSound";

    [Header("착지 사운드 클립 이름")]
    [SerializeField] private string landClipName = "SFX_Game_JumpDown";

    private bool isMoving = false;

    /// <summary>
    /// 외부에서 이동 여부 설정 (TestMoveAnimationController에서 호출)
    /// </summary>
    public void SetIsMoving(bool moving)
    {
        isMoving = moving;

        // 이동 멈추면 발소리만 정지
        if (!isMoving)
        {
            StopFootstepSound();
        }
    }

    /// <summary>
    /// 애니메이션 이벤트에서 호출됨
    /// </summary>
    public void FootStepSound()
    {
        // 이동 중이 아닐 때는 재생하지 않음
        if (!isMoving || string.IsNullOrEmpty(footstepClipName))
            return;

        AudioManager.Inst.PlaySFX(footstepClipName);
    }

    /// <summary>
    /// 점프 이벤트 (애니메이션 이벤트에서 호출)
    /// </summary>
    public void JumpSound()
    {
        if (string.IsNullOrEmpty(jumpClipName))
            return;

        AudioManager.Inst.PlaySFX(jumpClipName);
    }

    /// <summary>
    /// 착지 이벤트 (애니메이션 이벤트에서 호출)
    /// </summary>
    public void LandSound()
    {
        if (string.IsNullOrEmpty(landClipName))
            return;

        AudioManager.Inst.PlaySFX(landClipName);
    }

    /// <summary>
    /// 이동이 멈출 때 발소리만 정지
    /// </summary>
    public void StopFootstepSound()
    {
        // SoundFxPool에서 발소리 클립만 찾아서 정지
        var pool = AudioManager.Inst.SoundFxPool;
        for (int i = pool.Count - 1; i >= 0; i--)
        {
            // footstep 이름과 일치하는 클립만 멈춤
            if (pool[i].Name == footstepClipName || pool[i].Name.Contains("FootStep"))
            {
                pool[i].Source.Stop();
                Destroy(pool[i].gameObject);
                pool.RemoveAt(i);
            }
        }
    }
}