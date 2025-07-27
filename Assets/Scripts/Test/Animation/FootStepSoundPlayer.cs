using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 애니메이션 이벤트에서 호출되어 발소리를 재생하는 컴포넌트
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class FootstepSoundPlayer : MonoBehaviour
{
    [Tooltip("재생할 발소리 오디오 클립")]
    [SerializeField] private AudioClip footstepClip;

    [Tooltip("발소리 재생용 AudioSource (비워두면 자동 할당)")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip reloadClip;

    private bool isMoving = false;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// 애니메이션 이벤트에서 호출 (이벤트 이름과 동일해야 함)
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
            audioSource.Play();
        }
    }

    /// <summary>
    /// 외부에서 이동 여부 설정 (TestMoveAnimationController에서 호출)
    /// </summary>
    public void SetIsMoving(bool moving)
    {
        isMoving = moving;

        // 이동 멈추면 현재 소리도 정지
        if (!isMoving && audioSource != null && audioSource.isPlaying)
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