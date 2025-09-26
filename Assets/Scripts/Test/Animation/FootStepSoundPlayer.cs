using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(AudioSource))]
public class FootstepSoundPlayer : MonoBehaviour
{

    private bool isMoving = false;
    
    // 너무 짧은 간격으로 중복 재생되는 것을 방지하기 위한 쿨다운
    [SerializeField] private float footstepMinInterval = 0.15f;
    private float lastFootstepTime = -999f;
    
    // 같은 발 구간(반 주기)에서 2번 재생되는 것을 방지하기 위한 위상 체크
    private Animator cachedAnimator;
    private int lastStepPhase = int.MinValue;
    
    // 이벤트 타이밍 보정용 지연(발 접지 프레임과 정확히 맞추기 위함)
    [SerializeField] private float footstepDelay = 0f;

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
        if (!PhotonView.Get(this).IsMine) return;
        // 이동 중이 아닐 때는 재생하지 않음
        if (!isMoving)
        {
            return;
        }
        // Animator 캐시
        if (cachedAnimator == null)
        {
            cachedAnimator = GetComponent<Animator>();
        }
        
        // 같은 반 주기(왼발/오른발 구간) 내 중복 이벤트 차단 (경계값 보정 포함)
        if (cachedAnimator != null)
        {
            var state = cachedAnimator.GetCurrentAnimatorStateInfo(0);
            float norm = state.normalizedTime % 1f; // 0~1 구간으로 고정
            int currentPhase = Mathf.FloorToInt(norm * 2f + 0.01f); // 경계에서 같은 구간으로 인식되도록 epsilon 추가
            if (currentPhase == lastStepPhase)
            {
                // 같은 구간에서 또 왔다면 무시
                return;
            }
            lastStepPhase = currentPhase;
        }
        
        // 최소 간격 보호 (프레임/네트워크 요인으로 빠르게 두 번 올 때)
        if (Time.time - lastFootstepTime < footstepMinInterval)
        {
            return;
        }
        lastFootstepTime = Time.time;
        
        if (footstepDelay > 0f)
        {
            StartCoroutine(DelayedFootstep(footstepDelay));
        }
        else
        {
            PhotonView.Get(this).RPC("RPC_PlayFootStepSound", RpcTarget.All);
        }
    }

    private IEnumerator DelayedFootstep(float delay)
    {
        yield return new WaitForSeconds(delay);
        PhotonView.Get(this).RPC("RPC_PlayFootStepSound", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_PlayFootStepSound()
    {
        AudioManager.Inst.PlayClipAtPoint("SFX_Game_FootStep", transform.position, null, transform);
    }

    /// <summary>
    /// 점프 이벤트 (애니메이션 이벤트에서 호출)
    /// </summary>
    public void JumpSound()
    {
        if (!PhotonView.Get(this).IsMine) return;
        PhotonView.Get(this).RPC("RPC_PlayJumpSound", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_PlayJumpSound()
    {
        AudioManager.Inst.PlayClipAtPoint("SFX_Game_JumpUp", transform.position, null, transform);
    }

    /// <summary>
    /// 착지 이벤트 (애니메이션 이벤트에서 호출)
    /// </summary>
    public void LandSound()
    {
        if (!PhotonView.Get(this).IsMine) return;
        PhotonView.Get(this).RPC("RPC_PlayLandSound", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_PlayLandSound()
    {
        AudioManager.Inst.PlayClipAtPoint("SFX_Game_JumpDown", transform.position, null, transform);
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