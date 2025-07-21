using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class LoadingAudioManager : MonoBehaviour
{
    [Header("Background Music")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip[] bgmClips;
    [SerializeField] private float bgmVolume = 0.5f;
    [SerializeField] private float bgmFadeTime = 1f;
    
    [Header("Sound Effects")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip logoAppearSFX;
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip transitionSFX;
    [SerializeField] private AudioClip ambientSFX;
    [SerializeField] private float sfxVolume = 0.7f;
    
    [Header("Ambient Sounds")]
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioClip[] ambientClips;
    [SerializeField] private float ambientVolume = 0.3f;
    [SerializeField] private bool loopAmbient = true;
    
    [Header("Audio Settings")]
    [SerializeField] private bool enableAudioVisualization = true;
    [SerializeField] private float audioSyncThreshold = 0.1f;
    
    private bool isInitialized = false;
    private int currentBGMIndex = 0;
    private Coroutine bgmFadeCoroutine;
    
    void Start()
    {
        InitializeAudioManager();
    }
    
    void InitializeAudioManager()
    {
        if (isInitialized) return;
        
        // 오디오 소스 초기 설정
        SetupAudioSources();
        
        // BGM 시작
        if (bgmClips.Length > 0)
        {
            PlayBGM(0);
        }
        
        // 앰비언트 사운드 시작
        if (ambientClips.Length > 0)
        {
            PlayAmbientSound(0);
        }
        
        isInitialized = true;
    }
    
    void SetupAudioSources()
    {
        // BGM 소스 설정
        if (bgmSource != null)
        {
            bgmSource.volume = 0f; // 페이드 인을 위해 0으로 시작
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        
        // SFX 소스 설정
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
        
        // 앰비언트 소스 설정
        if (ambientSource != null)
        {
            ambientSource.volume = ambientVolume;
            ambientSource.loop = loopAmbient;
            ambientSource.playOnAwake = false;
        }
    }
    
    public void PlayBGM(int index)
    {
        if (bgmSource == null || bgmClips.Length == 0) return;
        
        if (index >= 0 && index < bgmClips.Length)
        {
            currentBGMIndex = index;
            bgmSource.clip = bgmClips[index];
            bgmSource.Play();
            
            // 페이드 인
            bgmSource.DOFade(bgmVolume, bgmFadeTime);
        }
    }
    
    public void PlayNextBGM()
    {
        if (bgmClips.Length <= 1) return;
        
        int nextIndex = (currentBGMIndex + 1) % bgmClips.Length;
        StartCoroutine(CrossfadeBGM(nextIndex));
    }
    
    IEnumerator CrossfadeBGM(int nextIndex)
    {
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }
        
        // 현재 BGM 페이드 아웃
        bgmSource.DOFade(0f, bgmFadeTime * 0.5f);
        yield return new WaitForSeconds(bgmFadeTime * 0.5f);
        
        // 다음 BGM으로 변경
        PlayBGM(nextIndex);
    }
    
    public void PlayAmbientSound(int index)
    {
        if (ambientSource == null || ambientClips.Length == 0) return;
        
        if (index >= 0 && index < ambientClips.Length)
        {
            ambientSource.clip = ambientClips[index];
            ambientSource.Play();
        }
    }
    
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
    
    public void PlayLogoAppearSFX()
    {
        PlaySFX(logoAppearSFX);
    }
    
    public void PlayButtonClickSFX()
    {
        PlaySFX(buttonClickSFX);
    }
    
    public void PlayTransitionSFX()
    {
        PlaySFX(transitionSFX);
    }
    
    public void PlayAmbientSFX()
    {
        PlaySFX(ambientSFX);
    }
    
    public void StopAllAudio()
    {
        if (bgmSource != null) bgmSource.Stop();
        if (sfxSource != null) sfxSource.Stop();
        if (ambientSource != null) ambientSource.Stop();
    }
    
    public void FadeOutAllAudio(float fadeTime)
    {
        if (bgmSource != null)
        {
            bgmSource.DOFade(0f, fadeTime).OnComplete(() => bgmSource.Stop());
        }
        
        if (ambientSource != null)
        {
            ambientSource.DOFade(0f, fadeTime).OnComplete(() => ambientSource.Stop());
        }
    }
    
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }
    
    public void SetAmbientVolume(float volume)
    {
        ambientVolume = Mathf.Clamp01(volume);
        if (ambientSource != null)
        {
            ambientSource.volume = ambientVolume;
        }
    }
    
    public void SetMasterVolume(float volume)
    {
        float normalizedVolume = Mathf.Clamp01(volume);
        SetBGMVolume(normalizedVolume);
        SetSFXVolume(normalizedVolume);
        SetAmbientVolume(normalizedVolume);
    }
    
    // 오디오 시각화를 위한 메서드 (선택사항)
    public float GetAudioLevel()
    {
        if (bgmSource == null || !bgmSource.isPlaying) return 0f;
        
        float[] samples = new float[256];
        bgmSource.GetOutputData(samples, 0);
        
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += Mathf.Abs(samples[i]);
        }
        
        return sum / samples.Length;
    }
    
    // 오디오 비트 감지 (선택사항)
    public bool IsBeat()
    {
        if (!enableAudioVisualization) return false;
        
        float currentLevel = GetAudioLevel();
        return currentLevel > audioSyncThreshold;
    }
    
    // 외부에서 호출 가능한 유틸리티 메서드들
    public bool IsAnyAudioPlaying()
    {
        return (bgmSource != null && bgmSource.isPlaying) ||
               (sfxSource != null && sfxSource.isPlaying) ||
               (ambientSource != null && ambientSource.isPlaying);
    }
    
    public float GetCurrentBGMTime()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            return bgmSource.time;
        }
        return 0f;
    }
    
    public float GetCurrentBGMLength()
    {
        if (bgmSource != null && bgmSource.clip != null)
        {
            return bgmSource.clip.length;
        }
        return 0f;
    }
    
    void OnDestroy()
    {
        StopAllAudio();
        DOTween.Kill(this);
    }
} 