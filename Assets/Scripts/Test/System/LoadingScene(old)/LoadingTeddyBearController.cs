using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingTeddyBearController : MonoBehaviour
{
    [Header("Physics Settings")]
    [SerializeField] private float bounceForce = 0.3f;
    [SerializeField] private float maxBounces = 3;
    [SerializeField] private float bounceReduction = 0.7f;
    [SerializeField] private float settleTime = 3f;
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem impactParticles;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private GameObject dustPrefab;
    [SerializeField] private float trailDuration = 2f;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] bounceSounds;
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private float volumeRange = 0.5f;
    
    [Header("Animation")]
    [SerializeField] private AnimationCurve bounceCurve;
    [SerializeField] private float squeezeFactor = 0.8f;
    [SerializeField] private float squeezeRecoveryTime = 0.3f;
    
    private Rigidbody rb;
    private Collider col;
    private Vector3 originalScale;
    private int currentBounces = 0;
    private bool isSettling = false;
    private bool hasLanded = false;
    
    // 물리 상태 추적
    private Vector3 lastVelocity;
    private float lastImpactTime;
    private float impactCooldown = 0.1f;
    
    void Start()
    {
        InitializeTeddyBear();
    }
    
    void InitializeTeddyBear()
    {
        // 컴포넌트 참조 가져오기
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        
        // 오리지널 스케일 저장
        originalScale = transform.localScale;
        
        // 오디오 소스 설정
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 3D 사운드
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.maxDistance = 10f;
        
        // 트레일 렌더러 설정
        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
        }
        SetupTrailRenderer();
        
        // 물리 설정 최적화
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        
        // 자동 정착 시작
        StartCoroutine(AutoSettle());
    }
    
    void SetupTrailRenderer()
    {
        if (trailRenderer != null)
        {
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            
            // Unity 최신 버전에서는 startColor와 endColor 사용
            Color trailColor = new Color(1, 1, 1, 0.8f);
            Color trailEndColor = new Color(1, 1, 1, 0.1f);
            trailRenderer.startColor = trailColor;
            trailRenderer.endColor = trailEndColor;
            
            trailRenderer.startWidth = 0.1f;
            trailRenderer.endWidth = 0.05f;
            trailRenderer.time = trailDuration;
            trailRenderer.enabled = true;
        }
    }
    
    void Update()
    {
        if (rb != null)
        {
            lastVelocity = rb.velocity;
        }
        
        // 속도가 매우 낮으면 정착 시작
        if (rb != null && !isSettling && rb.velocity.magnitude < 0.5f && hasLanded)
        {
            StartSettling();
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // 임팩트 쿨다운 체크
        if (Time.time - lastImpactTime < impactCooldown) return;
        lastImpactTime = Time.time;
        
        hasLanded = true;
        
        // 충돌 강도 계산
        float impactForce = lastVelocity.magnitude;
        
        // 바운스 처리
        if (currentBounces < maxBounces && impactForce > 1f)
        {
            HandleBounce(collision, impactForce);
        }
        
        // 효과 생성
        CreateImpactEffects(collision, impactForce);
        
        // 사운드 재생
        PlayImpactSound(impactForce);
        
        // 변형 애니메이션
        StartCoroutine(SqueezeAnimation(impactForce));
    }
    
    void HandleBounce(Collision collision, float impactForce)
    {
        if (rb == null) return;
        
        currentBounces++;
        
        // 바운스 방향 계산
        Vector3 bounceDirection = Vector3.Reflect(lastVelocity.normalized, collision.contacts[0].normal);
        
        // 바운스 힘 계산 (점점 약해짐)
        float bounceStrength = bounceForce * Mathf.Pow(bounceReduction, currentBounces - 1);
        
        // 바운스 적용
        rb.velocity = bounceDirection * bounceStrength * impactForce;
        
        // 회전 추가
        Vector3 randomTorque = new Vector3(
            Random.Range(-50f, 50f),
            Random.Range(-50f, 50f),
            Random.Range(-50f, 50f)
        );
        rb.AddTorque(randomTorque * bounceStrength);
    }
    
    void CreateImpactEffects(Collision collision, float impactForce)
    {
        Vector3 impactPoint = collision.contacts[0].point;
        Vector3 impactNormal = collision.contacts[0].normal;
        
        // 파티클 효과
        if (impactParticles != null)
        {
            ParticleSystem particles = Instantiate(impactParticles, impactPoint, Quaternion.LookRotation(impactNormal));
            
            // 파티클 강도 조절
            var main = particles.main;
            main.startLifetime = 0.5f + (impactForce * 0.3f);
            main.startSpeed = 2f + (impactForce * 0.5f);
            
            particles.Play();
            
            // 자동 파괴
            Destroy(particles.gameObject, 2f);
        }
        
        // 먼지 효과
        if (dustPrefab != null && impactForce > 2f)
        {
            GameObject dust = Instantiate(dustPrefab, impactPoint, Quaternion.LookRotation(impactNormal));
            Destroy(dust, 2f);
        }
    }
    
    void PlayImpactSound(float impactForce)
    {
        if (audioSource == null) return;
        
        AudioClip soundToPlay = null;
        
        // 임팩트 사운드와 바운스 사운드 중 선택
        if (impactForce > 3f && impactSound != null)
        {
            soundToPlay = impactSound;
        }
        else if (bounceSounds.Length > 0)
        {
            soundToPlay = bounceSounds[Random.Range(0, bounceSounds.Length)];
        }
        
        if (soundToPlay != null)
        {
            // 볼륨을 임팩트 강도에 따라 조절
            float volume = Mathf.Clamp(impactForce / 10f, 0.1f, volumeRange);
            audioSource.PlayOneShot(soundToPlay, volume);
        }
    }
    
    IEnumerator SqueezeAnimation(float impactForce)
    {
        // 스케일 압축 정도 계산
        float squeezeAmount = Mathf.Clamp(impactForce / 10f, 0.1f, 1f);
        Vector3 targetScale = new Vector3(
            originalScale.x * (1f + squeezeAmount * 0.2f),
            originalScale.y * (1f - squeezeAmount * squeezeFactor),
            originalScale.z * (1f + squeezeAmount * 0.2f)
        );
        
        // 압축
        float elapsedTime = 0f;
        while (elapsedTime < squeezeRecoveryTime * 0.3f)
        {
            float progress = elapsedTime / (squeezeRecoveryTime * 0.3f);
            transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 복원
        elapsedTime = 0f;
        while (elapsedTime < squeezeRecoveryTime * 0.7f)
        {
            float progress = elapsedTime / (squeezeRecoveryTime * 0.7f);
            transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 최종 스케일 보정
        transform.localScale = originalScale;
    }
    
    void StartSettling()
    {
        if (isSettling) return;
        
        isSettling = true;
        StartCoroutine(SettleDown());
    }
    
    IEnumerator SettleDown()
    {
        yield return new WaitForSeconds(settleTime);
        
        if (rb != null)
        {
            // 물리 시뮬레이션 중지
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // 트레일 렌더러 비활성화
        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;
        }
        
        Debug.Log("테디베어가 정착했습니다.");
    }
    
    IEnumerator AutoSettle()
    {
        // 일정 시간 후 자동 정착
        yield return new WaitForSeconds(10f);
        
        if (!isSettling)
        {
            StartSettling();
        }
    }
    
    // 외부에서 호출 가능한 메서드들
    public void SetBounceForce(float force)
    {
        bounceForce = force;
    }
    
    public void SetMaxBounces(int bounces)
    {
        maxBounces = bounces;
    }
    
    public void ResetTeddyBear()
    {
        currentBounces = 0;
        isSettling = false;
        hasLanded = false;
        
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        
        if (trailRenderer != null)
        {
            trailRenderer.enabled = true;
        }
        
        transform.localScale = originalScale;
    }
    
    public bool IsSettled()
    {
        return isSettling;
    }
    
    void OnDestroy()
    {
        if (trailRenderer != null && trailRenderer.material != null)
        {
            DestroyImmediate(trailRenderer.material);
        }
    }
} 