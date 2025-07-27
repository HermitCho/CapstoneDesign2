using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepSoundPlayer : MonoBehaviour
{
    [Header("발소리 클립")]
    [SerializeField] private AudioClip footstepClip;

    [Header("재장전 소리 클립")]
    [SerializeField] private AudioClip reloadClip;

    private AudioSource audioSource;
    private bool isMoving = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// 외부에서 이동 여부 설정 (TestMoveAnimationController에서 호출)
    /// </summary>
    public void SetIsMoving(bool moving)
    {
        isMoving = moving;

        // 이동 멈추면 현재 소리도 정지
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
        // 이동 중일 때만 재생
        if (!isMoving || footstepClip == null)
            return;

        // 발소리 클립이 재생 중이면 새로 재생하지 않음
        if (!audioSource.isPlaying)
        {
            audioSource.clip = footstepClip;
            audioSource.PlayOneShot(footstepClip);
        }
    }

    public void StopFootstepSound()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    public void PlayReloadSound()
    {
        if (reloadClip != null)
        {
            audioSource.PlayOneShot(reloadClip);
        }
    }
}