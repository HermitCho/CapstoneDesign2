using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Cinemachine;
using DG.Tweening;

public class LoadingCinematicController : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private Transform runnerCharacter; // 쫓기는 캐릭터
    [SerializeField] private Transform[] chaserCharacters; // 쫓는 캐릭터들 (3명)
    [SerializeField] private Animator runnerAnimator;
    [SerializeField] private Animator[] chaserAnimators;
    
    [Header("Cinemachine Setup")]
    [SerializeField] private CinemachineVirtualCamera mainCamera;
    [SerializeField] private CinemachineTrackedDolly trackedDolly;
    [SerializeField] private CinemachinePath dollyPath;
    [SerializeField] private CinemachineBasicMultiChannelPerlin noiseComponent;
    
    [Header("Animation Settings")]
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float chaseDistance = 3f;
    [SerializeField] private float animationDuration = 8f;
    [SerializeField] private AnimationCurve cameraShakeCurve;
    
    [Header("Path Points")]
    [SerializeField] private Transform[] pathPoints;
    [SerializeField] private Transform fallPosition;
    [SerializeField] private Transform[] chaserJumpPositions;
    
    [Header("Timeline")]
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private TimelineAsset cinematicTimeline;
    
    private bool isPlayingCinematic = false;
    private Coroutine cinematicCoroutine;
    
    // 애니메이션 상태 해시
    private readonly int runHash = Animator.StringToHash("Run");
    private readonly int fallHash = Animator.StringToHash("Fall");
    private readonly int jumpHash = Animator.StringToHash("Jump");
    private readonly int idleHash = Animator.StringToHash("Idle");
    
    void Start()
    {
        InitializeCinematic();
    }
    
    void InitializeCinematic()
    {
        // 초기 캐릭터 위치 설정
        SetupInitialPositions();
        
        // 시네머신 설정
        SetupCinemachine();
        
        // Timeline 설정
        SetupTimeline();
    }
    
    void SetupInitialPositions()
    {
        if (runnerCharacter != null && pathPoints.Length > 0)
        {
            runnerCharacter.position = pathPoints[0].position;
            runnerCharacter.LookAt(pathPoints[1].position);
        }
        
        // 쫓는 캐릭터들을 뒤에 배치
        for (int i = 0; i < chaserCharacters.Length; i++)
        {
            if (chaserCharacters[i] != null)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-1f, 1f), 
                    0, 
                    -chaseDistance - (i * 1.5f)
                );
                chaserCharacters[i].position = pathPoints[0].position + offset;
            }
        }
    }
    
    void SetupCinemachine()
    {
        if (mainCamera == null) return;
        
        // Follow와 LookAt 설정
        mainCamera.Follow = runnerCharacter;
        mainCamera.LookAt = runnerCharacter;
        
        // Tracked Dolly 설정
        if (trackedDolly == null)
        {
            trackedDolly = mainCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        }
        
        if (trackedDolly != null && dollyPath != null)
        {
            trackedDolly.m_Path = dollyPath;
            trackedDolly.m_PathPosition = 0f;
        }
        
        // 카메라 흔들림 설정
        if (noiseComponent == null)
        {
            noiseComponent = mainCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        }
        
        if (noiseComponent != null)
        {
            noiseComponent.m_AmplitudeGain = 0.5f;
            noiseComponent.m_FrequencyGain = 2f;
        }
    }
    
    void SetupTimeline()
    {
        if (playableDirector != null && cinematicTimeline != null)
        {
            playableDirector.playableAsset = cinematicTimeline;
            playableDirector.stopped += OnTimelineStopped;
        }
    }
    
    public void StartCinematic()
    {
        if (isPlayingCinematic) return;
        
        isPlayingCinematic = true;
        
        if (playableDirector != null)
        {
            // Timeline 사용
            playableDirector.Play();
        }
        else
        {
            // 코루틴 사용
            cinematicCoroutine = StartCoroutine(PlayCinematicSequence());
        }
    }
    
    public void StopCinematic()
    {
        if (!isPlayingCinematic) return;
        
        isPlayingCinematic = false;
        
        if (playableDirector != null)
        {
            playableDirector.Stop();
        }
        
        if (cinematicCoroutine != null)
        {
            StopCoroutine(cinematicCoroutine);
            cinematicCoroutine = null;
        }
    }
    
    IEnumerator PlayCinematicSequence()
    {
        // 1단계: 달리기 시작
        yield return StartCoroutine(RunningSequence());
        
        // 2단계: 넘어지기
        yield return StartCoroutine(FallingSequence());
        
        // 3단계: 쫓는 캐릭터들 점프
        yield return StartCoroutine(ChaserJumpSequence());
        
        // 4단계: 정지 (극적인 순간)
        yield return StartCoroutine(DramaticPause());
        
        isPlayingCinematic = false;
    }
    
    IEnumerator RunningSequence()
    {
        // 달리기 애니메이션 시작
        if (runnerAnimator != null)
        {
            runnerAnimator.SetBool(runHash, true);
        }
        
        foreach (var chaser in chaserAnimators)
        {
            if (chaser != null)
            {
                chaser.SetBool(runHash, true);
            }
        }
        
        // 카메라 흔들림 강화
        if (noiseComponent != null)
        {
            DOTween.To(() => noiseComponent.m_AmplitudeGain, x => noiseComponent.m_AmplitudeGain = x, 1.5f, 1f);
        }
        
        // 경로를 따라 이동
        float elapsedTime = 0f;
        float runDuration = animationDuration * 0.6f; // 전체 시간의 60%
        
        while (elapsedTime < runDuration)
        {
            float progress = elapsedTime / runDuration;
            
            // 러너 이동
            if (runnerCharacter != null && pathPoints.Length > 1)
            {
                Vector3 currentPos = Vector3.Lerp(pathPoints[0].position, fallPosition.position, progress);
                runnerCharacter.position = currentPos;
                
                // 이동 방향으로 회전
                if (progress < 0.95f)
                {
                    Vector3 direction = (fallPosition.position - currentPos).normalized;
                    runnerCharacter.rotation = Quaternion.LookRotation(direction);
                }
            }
            
            // 쫓는 캐릭터들 이동
            for (int i = 0; i < chaserCharacters.Length; i++)
            {
                if (chaserCharacters[i] != null)
                {
                    Vector3 targetPos = runnerCharacter.position + Vector3.back * (chaseDistance + i * 1.5f);
                    chaserCharacters[i].position = Vector3.Lerp(chaserCharacters[i].position, targetPos, Time.deltaTime * runSpeed);
                    
                    // 러너를 바라보도록 회전
                    Vector3 lookDirection = (runnerCharacter.position - chaserCharacters[i].position).normalized;
                    chaserCharacters[i].rotation = Quaternion.LookRotation(lookDirection);
                }
            }
            
            // 카메라 돌리 이동
            if (trackedDolly != null)
            {
                trackedDolly.m_PathPosition = progress;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
    
    IEnumerator FallingSequence()
    {
        // 넘어지기 애니메이션
        if (runnerAnimator != null)
        {
            runnerAnimator.SetBool(runHash, false);
            runnerAnimator.SetTrigger(fallHash);
        }
        
        // 러너를 넘어진 위치로 이동
        if (runnerCharacter != null)
        {
            runnerCharacter.DOMove(fallPosition.position, 0.5f).SetEase(Ease.OutQuad);
            runnerCharacter.DORotate(new Vector3(0, 0, -90), 0.5f).SetEase(Ease.OutBounce);
        }
        
        // 카메라 흔들림 증가 (넘어지는 임팩트)
        if (noiseComponent != null)
        {
            DOTween.To(() => noiseComponent.m_AmplitudeGain, x => noiseComponent.m_AmplitudeGain = x, 3f, 0.3f)
                .OnComplete(() => {
                    DOTween.To(() => noiseComponent.m_AmplitudeGain, x => noiseComponent.m_AmplitudeGain = x, 0.5f, 0.7f);
                });
        }
        
        yield return new WaitForSeconds(1f);
    }
    
    IEnumerator ChaserJumpSequence()
    {
        // 쫓는 캐릭터들 점프 준비
        for (int i = 0; i < chaserCharacters.Length; i++)
        {
            if (chaserCharacters[i] != null && chaserAnimators[i] != null)
            {
                chaserAnimators[i].SetBool(runHash, false);
                
                // 점프 위치로 이동
                Vector3 jumpPos = chaserJumpPositions[i].position;
                chaserCharacters[i].DOMove(jumpPos, 0.5f).SetEase(Ease.OutQuad);
                
                // 점프 애니메이션 (약간의 지연을 두고)
                float delay = i * 0.2f;
                DOVirtual.DelayedCall(delay, () => {
                    chaserAnimators[i].SetTrigger(jumpHash);
                });
            }
        }
        
        yield return new WaitForSeconds(1.5f);
    }
    
    IEnumerator DramaticPause()
    {
        // 극적인 정지 - 카메라 흔들림 중지
        if (noiseComponent != null)
        {
            DOTween.To(() => noiseComponent.m_AmplitudeGain, x => noiseComponent.m_AmplitudeGain = x, 0f, 0.5f);
        }
        
        // 모든 캐릭터 정지
        foreach (var animator in chaserAnimators)
        {
            if (animator != null)
            {
                animator.SetBool(idleHash, true);
            }
        }
        
        // 카메라 줌인 효과 (선택사항)
        if (mainCamera != null)
        {
            var lens = mainCamera.m_Lens;
            DOTween.To(() => lens.FieldOfView, x => {
                lens.FieldOfView = x;
                mainCamera.m_Lens = lens;
            }, 30f, 1f).SetEase(Ease.InOutQuad);
        }
        
        yield return new WaitForSeconds(2f);
    }
    
    void OnTimelineStopped(PlayableDirector director)
    {
        isPlayingCinematic = false;
    }
    
    // 외부에서 호출 가능한 메서드들
    public void SetRunSpeed(float speed)
    {
        runSpeed = speed;
    }
    
    public void SetCameraShakeIntensity(float intensity)
    {
        if (noiseComponent != null)
        {
            noiseComponent.m_AmplitudeGain = intensity;
        }
    }
    
    public bool IsPlayingCinematic()
    {
        return isPlayingCinematic;
    }
    
    void OnDestroy()
    {
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnTimelineStopped;
        }
        
        DOTween.Kill(this);
    }
} 