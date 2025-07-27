using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepSoundPlayer : MonoBehaviour
{

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
        if (!isMoving)
        {
            return;
        }
            
        AudioManager.Inst.PlaySFX("SFX_Game_FootStep");
    }

    /// <summary>
    /// 점프 이벤트 (애니메이션 이벤트에서 호출)
    /// </summary>
    public void JumpSound()
    {
        AudioManager.Inst.PlaySFX("SFX_Game_JumpUp");
    }

    /// <summary>
    /// 착지 이벤트 (애니메이션 이벤트에서 호출)
    /// </summary>
    public void LandSound()
    {
        AudioManager.Inst.PlaySFX("SFX_Game_JumpDown");
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
            if (pool[i].Name == "SFX_Game_FootStep")
            {
                pool[i].Source.Stop();
                Destroy(pool[i].gameObject);
                pool.RemoveAt(i);
            }
        }
    }
}