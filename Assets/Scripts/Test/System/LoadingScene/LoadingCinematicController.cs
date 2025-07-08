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
    [SerializeField] private LoadingRunnerController runnerController;
    
    [Header("Cinemachine Setup")]
    [SerializeField] private CinemachineVirtualCamera mainCamera;
    [SerializeField] private CinemachineVirtualCamera runnerCloseUpCamera; // 러너 클로즈업용
    [SerializeField] private CinemachineVirtualCamera teddyBearTrackingCamera; // 테디베어 추적용
    [SerializeField] private CinemachineVirtualCamera chaserJumpCamera; // 체이서 점프용
    [SerializeField] private CinemachineTrackedDolly trackedDolly;
    [SerializeField] private CinemachinePath dollyPath;
    [SerializeField] private CinemachineBasicMultiChannelPerlin noiseComponent;
    
    [Header("Camera Settings")]
    [SerializeField] private float runnerCloseUpFOV = 35f; // 러너 클로즈업 시야각
    [SerializeField] private float teddyBearTrackingFOV = 45f; // 테디베어 추적 시야각
    [SerializeField] private float chaserJumpFOV = 50f; // 체이서 점프 시야각
    [SerializeField] private float cameraTransitionTime = 0.5f; // 카메라 전환 시간
    
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
    
    [Header("TeddyBear Cinematic")]
    [SerializeField] private float teddyBearFocusDelay = 0.7f; // 테디베어 포커스 지연시간
    [SerializeField] private float teddyBearTrackingDuration = 2f; // 테디베어 추적 지속시간
    [SerializeField] private float chaserJumpFocusDelay = 1f; // 체이서 점프 포커스 지연시간
    
    private bool isPlayingCinematic = false;
    private Coroutine cinematicCoroutine;
    private Transform currentTeddyBear;
    
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
        
        // Runner Controller 참조 설정
        if (runnerController == null)
        {
            runnerController = runnerCharacter.GetComponent<LoadingRunnerController>();
        }
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
        
        // 메인 카메라 설정
        mainCamera.Follow = runnerCharacter;
        mainCamera.LookAt = runnerCharacter;
        mainCamera.Priority = 10; // 기본 우선순위
        
        // 러너 클로즈업 카메라 설정
        if (runnerCloseUpCamera != null)
        {
            runnerCloseUpCamera.Follow = runnerCharacter;
            runnerCloseUpCamera.LookAt = runnerCharacter;
            runnerCloseUpCamera.m_Lens.FieldOfView = runnerCloseUpFOV;
            runnerCloseUpCamera.Priority = 5; // 낮은 우선순위로 시작
            
            // 클로즈업 카메라 위치 조정
            var composer = runnerCloseUpCamera.GetCinemachineComponent<CinemachineComposer>();
            if (composer != null)
            {
                composer.m_TrackedObjectOffset = new Vector3(0, 1.5f, 0); // 얼굴 높이로 조정
            }
        }
        
        // 테디베어 추적 카메라 설정
        if (teddyBearTrackingCamera != null)
        {
            teddyBearTrackingCamera.m_Lens.FieldOfView = teddyBearTrackingFOV;
            teddyBearTrackingCamera.Priority = 5; // 낮은 우선순위로 시작
        }
        
        // 체이서 점프 카메라 설정
        if (chaserJumpCamera != null)
        {
            chaserJumpCamera.m_Lens.FieldOfView = chaserJumpFOV;
            chaserJumpCamera.Priority = 5; // 낮은 우선순위로 시작
        }
        
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
        
        // 2단계: 넘어지기 + 테디베어 드롭 시네마틱
        yield return StartCoroutine(FallingAndTeddyBearSequence());
        
        // 3단계: 쫓는 캐릭터들 점프 + 테디베어 캐치 시도
        yield return StartCoroutine(ChaserJumpAndCatchSequence());
        
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
        float runDuration = animationDuration * 0.4f; // 전체 시간의 40%로 단축 (테디베어 시퀀스를 위해)
        
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
            
            // 카메라 돌리 이동
            if (trackedDolly != null)
            {
                trackedDolly.m_PathPosition = progress;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
    
    IEnumerator FallingAndTeddyBearSequence()
    {
        // 러너 클로즈업 카메라 활성화
        if (runnerCloseUpCamera != null)
        {
            runnerCloseUpCamera.Priority = 15; // 최우선순위로 설정
            Debug.Log("러너 클로즈업 카메라 활성화");
        }
        
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
        
        // 테디베어 드롭 대기
        yield return new WaitForSeconds(teddyBearFocusDelay);
        
        // 테디베어 추적 카메라로 전환
        if (runnerController != null)
        {
            currentTeddyBear = runnerController.GetTeddyBear()?.transform;
            if (currentTeddyBear != null && teddyBearTrackingCamera != null)
            {
                teddyBearTrackingCamera.Follow = currentTeddyBear;
                teddyBearTrackingCamera.LookAt = currentTeddyBear;
                teddyBearTrackingCamera.Priority = 20; // 가장 높은 우선순위
                
                // 러너 클로즈업 카메라 비활성화
                runnerCloseUpCamera.Priority = 5;
                
                Debug.Log("테디베어 추적 카메라 활성화");
            }
        }
        
        // 테디베어 추적 지속시간
        yield return new WaitForSeconds(teddyBearTrackingDuration);
    }
    
    IEnumerator ChaserJumpAndCatchSequence()
    {
        // 체이서 점프 카메라 활성화
        if (chaserJumpCamera != null && chaserCharacters.Length > 0)
        {
            // 체이서들의 중심점 계산
            Vector3 chaserCenter = Vector3.zero;
            int activeCharacters = 0;
            
            foreach (var chaser in chaserCharacters)
            {
                if (chaser != null)
                {
                    chaserCenter += chaser.position;
                    activeCharacters++;
                }
            }
            
            if (activeCharacters > 0)
            {
                chaserCenter /= activeCharacters;
                
                // 체이서 점프 카메라 설정
                chaserJumpCamera.Follow = chaserCharacters[0]; // 첫 번째 체이서를 메인 타겟으로
                chaserJumpCamera.LookAt = chaserCharacters[0];
                chaserJumpCamera.Priority = 25; // 최우선순위
                
                // 테디베어 추적 카메라 비활성화
                if (teddyBearTrackingCamera != null)
                {
                    teddyBearTrackingCamera.Priority = 5;
                }
                
                Debug.Log("체이서 점프 카메라 활성화");
            }
        }
        
        // 쫓는 캐릭터들 점프 준비
        for (int i = 0; i < chaserCharacters.Length; i++)
        {
            if (chaserCharacters[i] != null && chaserAnimators[i] != null)
            {
                chaserAnimators[i].SetBool(runHash, false);
                
                // 점프 애니메이션 (위치 이동 제거 - Root Motion 사용)
                float delay = i * 0.2f;
                DOVirtual.DelayedCall(delay, () => {
                    chaserAnimators[i].SetTrigger(jumpHash);
                });
            }
        }
        
        // 체이서 점프 동작 시청
        yield return new WaitForSeconds(2f);
        
        // 카메라를 다시 테디베어로 전환 (떨어지는 모습)
        if (currentTeddyBear != null && teddyBearTrackingCamera != null)
        {
            teddyBearTrackingCamera.Priority = 20;
            if (chaserJumpCamera != null)
            {
                chaserJumpCamera.Priority = 5;
            }
            
            Debug.Log("테디베어 최종 추적 카메라 활성화");
        }
        
        yield return new WaitForSeconds(1f);
    }
    
    IEnumerator DramaticPause()
    {
        // 메인 카메라로 다시 전환
        if (mainCamera != null)
        {
            mainCamera.Priority = 15;
            
            // 다른 모든 카메라 비활성화
            if (runnerCloseUpCamera != null) runnerCloseUpCamera.Priority = 5;
            if (teddyBearTrackingCamera != null) teddyBearTrackingCamera.Priority = 5;
            if (chaserJumpCamera != null) chaserJumpCamera.Priority = 5;
        }
        
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
        
        Debug.Log("극적인 정지 시퀀스 완료");
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
    
    public void SetTeddyBearFocusDelay(float delay)
    {
        teddyBearFocusDelay = delay;
    }
    
    public void SetTeddyBearTrackingDuration(float duration)
    {
        teddyBearTrackingDuration = duration;
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