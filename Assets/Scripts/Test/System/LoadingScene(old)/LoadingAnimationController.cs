using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class LoadingAnimationController : MonoBehaviour
{
    [Header("Logo Animation")]
    [SerializeField] private Transform logoTransform;
    [SerializeField] private float logoFloatHeight = 1f;
    [SerializeField] private float logoFloatDuration = 2f;
    [SerializeField] private float logoRotationSpeed = 30f;
    
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem[] particleSystems;
    [SerializeField] private float particleStartDelay = 0.5f;
    
    [Header("Rotating Objects")]
    [SerializeField] private Transform[] rotatingObjects;
    [SerializeField] private float[] rotationSpeeds;
    [SerializeField] private Vector3[] rotationAxes;
    
    [Header("Lighting Animation")]
    [SerializeField] private Light mainLight;
    [SerializeField] private AnimationCurve lightIntensityCurve;
    [SerializeField] private float lightAnimationDuration = 3f;
    
    [Header("Environment")]
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private AnimationCurve skyboxRotationCurve;
    
    private bool isAnimating = false;
    private Coroutine lightAnimationCoroutine;
    
    void Start()
    {
        InitializeAnimations();
    }
    
    void Update()
    {
        if (isAnimating)
        {
            UpdateRotatingObjects();
        }
    }
    
    public void StartLoadingAnimation()
    {
        if (isAnimating) return;
        
        isAnimating = true;
        StartCoroutine(PlayLoadingSequence());
    }
    
    public void StopLoadingAnimation()
    {
        isAnimating = false;
        StopAllCoroutines();
        
        // 파티클 시스템 중지
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                ps.Stop();
            }
        }
    }
    
    void InitializeAnimations()
    {
        // 로고 초기 위치 설정
        if (logoTransform != null)
        {
            logoTransform.position = Vector3.zero;
        }
        
        // 회전 축 기본값 설정
        if (rotationAxes == null || rotationAxes.Length != rotatingObjects.Length)
        {
            rotationAxes = new Vector3[rotatingObjects.Length];
            for (int i = 0; i < rotationAxes.Length; i++)
            {
                rotationAxes[i] = Vector3.up;
            }
        }
        
        // 회전 속도 기본값 설정
        if (rotationSpeeds == null || rotationSpeeds.Length != rotatingObjects.Length)
        {
            rotationSpeeds = new float[rotatingObjects.Length];
            for (int i = 0; i < rotationSpeeds.Length; i++)
            {
                rotationSpeeds[i] = 45f;
            }
        }
    }
    
    IEnumerator PlayLoadingSequence()
    {
        // 로고 애니메이션 시작
        if (logoTransform != null)
        {
            StartLogoAnimation();
        }
        
        // 파티클 시스템 시작 (지연)
        yield return new WaitForSeconds(particleStartDelay);
        StartParticleEffects();
        
        // 라이트 애니메이션 시작
        if (mainLight != null)
        {
            lightAnimationCoroutine = StartCoroutine(AnimateLight());
        }
        
        // 스카이박스 애니메이션 시작
        if (skyboxMaterial != null)
        {
            StartSkyboxAnimation();
        }
    }
    
    void StartLogoAnimation()
    {
        // 위아래 떠오르는 애니메이션
        logoTransform.DOMoveY(logoFloatHeight, logoFloatDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
        
        // 회전 애니메이션
        logoTransform.DORotate(new Vector3(0, 360, 0), logoFloatDuration * 2, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.Linear);
        
        // 스케일 펄스 애니메이션
        logoTransform.DOScale(Vector3.one * 1.1f, logoFloatDuration * 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }
    
    void StartParticleEffects()
    {
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                ps.Play();
            }
        }
    }
    
    IEnumerator AnimateLight()
    {
        float elapsedTime = 0f;
        float initialIntensity = mainLight.intensity;
        
        while (isAnimating)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = (elapsedTime % lightAnimationDuration) / lightAnimationDuration;
            
            float curveValue = lightIntensityCurve.Evaluate(normalizedTime);
            mainLight.intensity = initialIntensity * curveValue;
            
            yield return null;
        }
    }
    
    void StartSkyboxAnimation()
    {
        if (skyboxMaterial.HasProperty("_Rotation"))
        {
            float currentRotation = skyboxMaterial.GetFloat("_Rotation");
            
            DOTween.To(() => currentRotation, x => skyboxMaterial.SetFloat("_Rotation", x), 
                      currentRotation + 360f, lightAnimationDuration)
                .SetLoops(-1, LoopType.Incremental)
                .SetEase(Ease.Linear);
        }
    }
    
    void UpdateRotatingObjects()
    {
        for (int i = 0; i < rotatingObjects.Length; i++)
        {
            if (rotatingObjects[i] != null)
            {
                Vector3 rotationAxis = rotationAxes[i];
                float rotationSpeed = rotationSpeeds[i];
                
                rotatingObjects[i].Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    // 외부에서 호출 가능한 메서드들
    public void SetLogoFloatHeight(float height)
    {
        logoFloatHeight = height;
    }
    
    public void SetAnimationSpeed(float speedMultiplier)
    {
        DOTween.timeScale = speedMultiplier;
    }
    
    public void AddParticleSystem(ParticleSystem ps)
    {
        if (ps != null)
        {
            System.Array.Resize(ref particleSystems, particleSystems.Length + 1);
            particleSystems[particleSystems.Length - 1] = ps;
        }
    }
    
    public void RemoveParticleSystem(ParticleSystem ps)
    {
        if (ps != null)
        {
            var list = new List<ParticleSystem>(particleSystems);
            list.Remove(ps);
            particleSystems = list.ToArray();
        }
    }
    
    void OnDestroy()
    {
        StopLoadingAnimation();
    }
} 