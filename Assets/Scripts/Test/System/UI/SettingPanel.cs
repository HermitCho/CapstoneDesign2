using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.Heat;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using DG.Tweening;
/// <summary>
/// 환경설정 패널 - 감도, 볼륨, 키 바인딩 설정
/// HeatUI, PlayerPrefs, Input System을 활용한 실무 수준 구현
/// </summary>
public class SettingPanel : MonoBehaviour
{
    #region 인스펙터 변수
    
    [Header("감도 설정 슬라이더")]
    [SerializeField] private SliderManager xSensivitySlider;
    [SerializeField] private SliderManager ySensivitySlider;
    [Space(10)]

    [Header("볼륨 설정 슬라이더")]
    [SerializeField] private SliderManager masterVolumeSlider;
    [SerializeField] private SliderManager musicVolumeSlider;
    [SerializeField] private SliderManager sfxVolumeSlider;
    [SerializeField] private SliderManager uiVolumeSlider;
    [Space(10)] 

    [Header("키 바인딩 설정")]
    [SerializeField] private ButtonManager forwardKeyButton;
    [SerializeField] private ButtonManager backwardKeyButton;
    [SerializeField] private ButtonManager leftKeyButton;
    [SerializeField] private ButtonManager rightKeyButton;
    [SerializeField] private ButtonManager jumpKeyButton;
    [SerializeField] private ButtonManager reloadKeyButton;
    [SerializeField] private ButtonManager skillKeyButton;
    [SerializeField] private ButtonManager itemKeyButton;
    [SerializeField] private ButtonManager itemChangeKeyButton;
    [SerializeField] private ButtonManager dropCrownButton;
    [Space(10)]

    [Header("키 바인딩 텍스트")]
    [SerializeField] private TextMeshProUGUI keyBindingText;

    #endregion
    
    #region 내부 변수
    
    // 키 바인딩 관련
    private bool isWaitingForKeyInput = false;
    private string currentBindingAction = "";
    private ButtonManager currentBindingButton = null;
    
    // 키 바인딩 매핑 (ActionName -> ButtonManager)
    private Dictionary<string, ButtonManager> keyBindingButtons = new Dictionary<string, ButtonManager>();
    
    // 사용 중인 키 추적 (중복 방지)
    private Dictionary<string, string> usedKeys = new Dictionary<string, string>(); // Key -> ActionName
    
    // UI 애니메이션 관련
    private DG.Tweening.Tween keyBindingTextBlinkTween;
    
    // 볼륨 관련 (마스터 볼륨 비율 적용용)
    private float currentMasterVolume = 1f;
    private float currentMusicVolume = 0.8f;
    private float currentSFXVolume = 0.8f;
    private float currentUIVolume = 0.8f;
    
    // PlayerPrefs 키
    private const string PREF_X_SENSITIVITY = "XSensitivity";
    private const string PREF_Y_SENSITIVITY = "YSensitivity";
    private const string PREF_MASTER_VOLUME = "MasterVolume";
    private const string PREF_MUSIC_VOLUME = "MusicVolume";
    private const string PREF_SFX_VOLUME = "SFXVolume";
    private const string PREF_UI_VOLUME = "UIVolume";
    
    // 기본값
    private const float DEFAULT_SENSITIVITY = 1f;
    private const float DEFAULT_VOLUME = 0.8f;
    
    #endregion
    
    #region Unity 생명주기
    
    void Awake()
    {
        // 키 바인딩 버튼 매핑 초기화
        InitializeKeyBindingMap();
        
        // 키 바인딩 텍스트 초기화 (비활성화)
        if (keyBindingText != null)
        {
            keyBindingText.gameObject.SetActive(false);
        }
    }
    
    void OnEnable()
    {
        // 설정값 로드
        LoadAllSettings();
        
        // UI 업데이트
        UpdateAllUI();
        
        // 슬라이더 이벤트 등록
        RegisterSliderEvents();
    }
    
    void OnDisable()
    {
        // 슬라이더 이벤트 해제
        UnregisterSliderEvents();
        
        // 키 입력 대기 중이라면 취소
        if (isWaitingForKeyInput)
        {
            CancelKeyBinding();
        }
        
        // 애니메이션 정리
        CleanupKeyBindingAnimation();
    }
    
    void Update()
    {
        // 키 바인딩 입력 대기 중일 때
        if (isWaitingForKeyInput)
        {
            CheckForKeyInput();
        }
    }
    
    void OnDestroy()
    {
        // InputManager가 PlayerAction을 관리하므로 여기서는 정리하지 않음
    }
    
    #endregion
    
    #region 초기화
    
    /// <summary>
    /// 키 바인딩 버튼 매핑 초기화
    /// </summary>
    private void InitializeKeyBindingMap()
    {
        keyBindingButtons.Clear();
        
        if (forwardKeyButton != null) keyBindingButtons["Up"] = forwardKeyButton;
        if (backwardKeyButton != null) keyBindingButtons["Down"] = backwardKeyButton;
        if (leftKeyButton != null) keyBindingButtons["Left"] = leftKeyButton;
        if (rightKeyButton != null) keyBindingButtons["Right"] = rightKeyButton;
        if (jumpKeyButton != null) keyBindingButtons["Jump"] = jumpKeyButton;
        if (reloadKeyButton != null) keyBindingButtons["Reload"] = reloadKeyButton;
        if (skillKeyButton != null) keyBindingButtons["Skill"] = skillKeyButton;
        if (itemKeyButton != null) keyBindingButtons["Item"] = itemKeyButton;
        if (itemChangeKeyButton != null) keyBindingButtons["ChangeItem"] = itemChangeKeyButton;
        if (dropCrownButton != null) keyBindingButtons["Detach"] = dropCrownButton;
    }
    
    #endregion
    
    #region 설정 로드/저장
    
    /// <summary>
    /// 모든 설정 로드
    /// </summary>
    private void LoadAllSettings()
    {
        LoadSensitivitySettings();
        LoadVolumeSettings();
        LoadKeyBindings();
    }
    
    /// <summary>
    /// 감도 설정 로드
    /// </summary>
    private void LoadSensitivitySettings()
    {
        float xSens = PlayerPrefs.GetFloat(PREF_X_SENSITIVITY, DEFAULT_SENSITIVITY);
        float ySens = PlayerPrefs.GetFloat(PREF_Y_SENSITIVITY, DEFAULT_SENSITIVITY);
        
        if (xSensivitySlider != null)
        {
            xSensivitySlider.mainSlider.value = xSens;
        }
        
        if (ySensivitySlider != null)
        {
            ySensivitySlider.mainSlider.value = ySens;
        }
        
        // DataBase에 적용
        ApplySensitivityToDataBase(xSens, ySens);
    }
    
    /// <summary>
    /// 볼륨 설정 로드
    /// </summary>
    private void LoadVolumeSettings()
    {
        currentMasterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1f);
        currentMusicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, DEFAULT_VOLUME);
        currentSFXVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, DEFAULT_VOLUME);
        currentUIVolume = PlayerPrefs.GetFloat(PREF_UI_VOLUME, DEFAULT_VOLUME);
        
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.mainSlider.value = currentMasterVolume;
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.mainSlider.value = currentMusicVolume;
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.mainSlider.value = currentSFXVolume;
        }
        
        if (uiVolumeSlider != null)
        {
            uiVolumeSlider.mainSlider.value = currentUIVolume;
        }
        
        // AudioManager에 적용 (마스터 볼륨 비율 적용)
        ApplyVolumeToAudioManager();
    }
    
    /// <summary>
    /// 키 바인딩 설정 로드
    /// </summary>
    private void LoadKeyBindings()
    {
        usedKeys.Clear();
        
        foreach (var kvp in keyBindingButtons)
        {
            string actionName = kvp.Key;
            ButtonManager button = kvp.Value;
            
            if (button == null) continue;
            
            // PlayerPrefs에서 키 바인딩 로드
            string savedKey = PlayerPrefs.GetString($"KeyBinding_{actionName}", "");
            
            if (!string.IsNullOrEmpty(savedKey))
            {
                // Input System에 바인딩 적용
                ApplyKeyBinding(actionName, savedKey);
                
                // 버튼 텍스트 업데이트
                button.SetText(savedKey);
                
                // 사용 중인 키 등록
                usedKeys[savedKey] = actionName;
            }
            else
            {
                // 기본 키 표시 (현재 바인딩에서 읽어오기)
                string currentKey = GetCurrentKeyBinding(actionName);
                button.SetText(currentKey);
                usedKeys[currentKey] = actionName;
            }
        }
    }
    
    /// <summary>
    /// PlayerPrefs에서 저장된 키 바인딩 가져오기 (또는 기본값)
    /// </summary>
    private string GetCurrentKeyBinding(string actionName)
    {
        // PlayerPrefs에서 저장된 키 확인
        string savedKey = PlayerPrefs.GetString($"KeyBinding_{actionName}", "");
        
        if (!string.IsNullOrEmpty(savedKey))
        {
            return savedKey;
        }
        
        // 저장된 값이 없으면 기본 키 반환
        return GetDefaultKeyForAction(actionName);
    }
    
    
    /// <summary>
    /// 모든 설정 저장
    /// </summary>
    private void SaveAllSettings()
    {
        SaveSensitivitySettings();
        SaveVolumeSettings();
        SaveKeyBindings();
        
        PlayerPrefs.Save();
        Debug.Log("SettingPanel: 모든 설정 저장 완료");
    }
    
    /// <summary>
    /// 감도 설정 저장
    /// </summary>
    private void SaveSensitivitySettings()
    {
        if (xSensivitySlider != null)
        {
            PlayerPrefs.SetFloat(PREF_X_SENSITIVITY, xSensivitySlider.mainSlider.value);
        }
        
        if (ySensivitySlider != null)
        {
            PlayerPrefs.SetFloat(PREF_Y_SENSITIVITY, ySensivitySlider.mainSlider.value);
        }
    }
    
    /// <summary>
    /// 볼륨 설정 저장
    /// </summary>
    private void SaveVolumeSettings()
    {
        if (masterVolumeSlider != null)
        {
            PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, masterVolumeSlider.mainSlider.value);
        }
        
        if (musicVolumeSlider != null)
        {
            PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, musicVolumeSlider.mainSlider.value);
        }
        
        if (sfxVolumeSlider != null)
        {
            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, sfxVolumeSlider.mainSlider.value);
        }
        
        if (uiVolumeSlider != null)
        {
            PlayerPrefs.SetFloat(PREF_UI_VOLUME, uiVolumeSlider.mainSlider.value);
        }
    }
    
    /// <summary>
    /// 키 바인딩 설정 저장
    /// </summary>
    private void SaveKeyBindings()
    {
        foreach (var kvp in keyBindingButtons)
        {
            string actionName = kvp.Key;
            ButtonManager button = kvp.Value;
            
            if (button != null)
            {
                string keyText = button.buttonText;
                if (!string.IsNullOrEmpty(keyText) && keyText != "<None>")
                {
                    PlayerPrefs.SetString($"KeyBinding_{actionName}", keyText);
                }
            }
        }
    }
    
    #endregion
    
    #region UI 업데이트
    
    /// <summary>
    /// 모든 UI 업데이트
    /// </summary>
    private void UpdateAllUI()
    {
        UpdateSensitivityUI();
        UpdateVolumeUI();
        UpdateKeyBindingUI();
    }
    
    /// <summary>
    /// 감도 UI 업데이트
    /// </summary>
    private void UpdateSensitivityUI()
    {
        if (xSensivitySlider != null)
        {
            xSensivitySlider.UpdateUI();
        }
        
        if (ySensivitySlider != null)
        {
            ySensivitySlider.UpdateUI();
        }
    }
    
    /// <summary>
    /// 볼륨 UI 업데이트
    /// </summary>
    private void UpdateVolumeUI()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.UpdateUI();
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.UpdateUI();
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.UpdateUI();
        }
        
        if (uiVolumeSlider != null)
        {
            uiVolumeSlider.UpdateUI();
        }
    }
    
    /// <summary>
    /// 키 바인딩 UI 업데이트
    /// </summary>
    private void UpdateKeyBindingUI()
    {
        foreach (var kvp in keyBindingButtons)
        {
            ButtonManager button = kvp.Value;
            if (button != null)
            {
                button.UpdateUI();
            }
        }
    }
    
    #endregion
    
    #region 슬라이더 이벤트
    
    /// <summary>
    /// 슬라이더 이벤트 등록
    /// </summary>
    private void RegisterSliderEvents()
    {
        if (xSensivitySlider != null)
        {
            xSensivitySlider.mainSlider.onValueChanged.AddListener(OnXSensitivityChanged);
        }
        
        if (ySensivitySlider != null)
        {
            ySensivitySlider.mainSlider.onValueChanged.AddListener(OnYSensitivityChanged);
        }
        
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.mainSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.mainSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.mainSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
        
        if (uiVolumeSlider != null)
        {
            uiVolumeSlider.mainSlider.onValueChanged.AddListener(OnUIVolumeChanged);
        }
    }
    
    /// <summary>
    /// 슬라이더 이벤트 해제
    /// </summary>
    private void UnregisterSliderEvents()
    {
        if (xSensivitySlider != null)
        {
            xSensivitySlider.mainSlider.onValueChanged.RemoveListener(OnXSensitivityChanged);
        }
        
        if (ySensivitySlider != null)
        {
            ySensivitySlider.mainSlider.onValueChanged.RemoveListener(OnYSensitivityChanged);
        }
        
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.mainSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.mainSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.mainSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        }
        
        if (uiVolumeSlider != null)
        {
            uiVolumeSlider.mainSlider.onValueChanged.RemoveListener(OnUIVolumeChanged);
        }
    }
    
    #endregion
    
    #region 감도 설정
    
    /// <summary>
    /// X축 감도 변경
    /// </summary>
    private void OnXSensitivityChanged(float value)
    {
        // DataBase에 적용
        if (DataBase.Instance != null && DataBase.Instance.playerMoveData != null)
        {
            DataBase.Instance.playerMoveData.RotationSpeed = value * 10f; // 0-1 범위를 0-10으로 변환
        }
        
        // 즉시 저장
        PlayerPrefs.SetFloat(PREF_X_SENSITIVITY, value);
        
        Debug.Log($"SettingPanel: X축 감도 변경 - {value}");
    }
    
    /// <summary>
    /// Y축 감도 변경
    /// </summary>
    private void OnYSensitivityChanged(float value)
    {
        // DataBase에 적용
        if (DataBase.Instance != null && DataBase.Instance.cameraData != null)
        {
            DataBase.Instance.cameraData.MouseSensitivityY = value * 10f; // 0-1 범위를 0-10으로 변환
        }
        
        // 즉시 저장
        PlayerPrefs.SetFloat(PREF_Y_SENSITIVITY, value);
        
        Debug.Log($"SettingPanel: Y축 감도 변경 - {value}");
    }
    
    /// <summary>
    /// 감도를 DataBase에 적용
    /// </summary>
    private void ApplySensitivityToDataBase(float xSens, float ySens)
    {
        if (DataBase.Instance == null) return;
        
        if (DataBase.Instance.playerMoveData != null)
        {
            DataBase.Instance.playerMoveData.RotationSpeed = xSens * 10f;
        }
        
        if (DataBase.Instance.cameraData != null)
        {
            DataBase.Instance.cameraData.MouseSensitivityY = ySens * 10f;
        }
    }
    
    #endregion
    
    #region 볼륨 설정
    
    /// <summary>
    /// 마스터 볼륨 변경 (비율로 적용)
    /// </summary>
    private void OnMasterVolumeChanged(float value)
    {
        currentMasterVolume = value;
        
        // 즉시 저장
        PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, value);
        
        // AudioManager에 적용 (개별 볼륨 * 마스터 볼륨)
        ApplyVolumeToAudioManager();
        
        Debug.Log($"SettingPanel: 마스터 볼륨 변경 - {value} (Music: {currentMusicVolume * currentMasterVolume}, SFX: {currentSFXVolume * currentMasterVolume})");
    }
    
    /// <summary>
    /// 음악 볼륨 변경
    /// </summary>
    private void OnMusicVolumeChanged(float value)
    {
        currentMusicVolume = value;
        
        // 즉시 저장
        PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, value);
        
        // AudioManager에 적용 (마스터 볼륨 비율 적용)
        ApplyVolumeToAudioManager();
        
        Debug.Log($"SettingPanel: 음악 볼륨 변경 - {value} (실제: {value * currentMasterVolume})");
    }
    
    /// <summary>
    /// 효과음 볼륨 변경
    /// </summary>
    private void OnSFXVolumeChanged(float value)
    {
        currentSFXVolume = value;
        
        // 즉시 저장
        PlayerPrefs.SetFloat(PREF_SFX_VOLUME, value);
        
        // AudioManager에 적용 (마스터 볼륨 비율 적용)
        ApplyVolumeToAudioManager();
        
        Debug.Log($"SettingPanel: 효과음 볼륨 변경 - {value} (실제: {value * currentMasterVolume})");
    }
    
    /// <summary>
    /// UI 볼륨 변경
    /// </summary>
    private void OnUIVolumeChanged(float value)
    {
        currentUIVolume = value;
        
        // 즉시 저장
        PlayerPrefs.SetFloat(PREF_UI_VOLUME, value);
        
        Debug.Log($"SettingPanel: UI 볼륨 변경 - {value} (실제: {value * currentMasterVolume})");
    }
    
    /// <summary>
    /// 볼륨을 AudioManager에 적용 (마스터 볼륨 비율 적용)
    /// </summary>
    private void ApplyVolumeToAudioManager()
    {
        if (AudioManager.Inst == null) return;
        
        // 개별 볼륨 * 마스터 볼륨 = 최종 볼륨
        float finalMusicVolume = currentMusicVolume * currentMasterVolume;
        float finalSFXVolume = currentSFXVolume * currentMasterVolume;
        
        AudioManager.Inst.MusicVolume = finalMusicVolume;
        AudioManager.Inst.SoundVolume = finalSFXVolume;
        
        Debug.Log($"SettingPanel: 최종 볼륨 적용 - Music: {finalMusicVolume}, SFX: {finalSFXVolume}");
    }
    
    #endregion
    
    #region 키 바인딩 시스템
    
    /// <summary>
    /// 키 바인딩 버튼 클릭 (HeatUI ButtonManager의 onClick 이벤트에 연결)
    /// </summary>
    public void OnClickKeyBindingButton(string actionName)
    {
        if (!keyBindingButtons.ContainsKey(actionName))
        {
            Debug.LogWarning($"SettingPanel: 알 수 없는 액션 - {actionName}");
            return;
        }
        
        ButtonManager button = keyBindingButtons[actionName];
        if (button == null) return;
        
        // 이미 키 입력 대기 중이라면 취소
        if (isWaitingForKeyInput)
        {
            CancelKeyBinding();
        }
        
        // 키 입력 대기 시작
        StartKeyBinding(actionName, button);
    }
    
    /// <summary>
    /// 키 바인딩 시작
    /// </summary>
    private void StartKeyBinding(string actionName, ButtonManager button)
    {
        isWaitingForKeyInput = true;
        currentBindingAction = actionName;
        currentBindingButton = button;
        
        // 버튼 텍스트를 <None>으로 변경
        button.SetText("<None>");
        
        // 모든 게임 입력 차단
        DisableGameInput();
        
        // UI 피드백 표시
        ShowKeyBindingUI(actionName);
        
        Debug.Log($"SettingPanel: 키 바인딩 대기 시작 - {actionName} (게임 입력 차단됨)");
    }
    
    /// <summary>
    /// 키 입력 체크
    /// </summary>
    private void CheckForKeyInput()
    {
        // ESC로 취소
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelKeyBinding();
            return;
        }
        
        // 모든 키보드 키 체크
        foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
        {
            // 마우스 버튼은 제외
            if (keyCode >= KeyCode.Mouse0 && keyCode <= KeyCode.Mouse6)
                continue;
            
            // 조이스틱 입력은 제외
            if (keyCode >= KeyCode.JoystickButton0)
                continue;
            
            if (Input.GetKeyDown(keyCode))
            {
                string keyName = GetKeyDisplayName(keyCode);
                
                // 중복 키 체크
                if (IsKeyAlreadyUsed(keyName, currentBindingAction))
                {
                    Debug.LogWarning($"SettingPanel: 키 '{keyName}'는 이미 사용 중입니다!");
                    CancelKeyBinding();
                    return;
                }
                
                // 키 바인딩 적용
                ApplyNewKeyBinding(currentBindingAction, keyName);
                return;
            }
        }
    }
    
    /// <summary>
    /// 새 키 바인딩 적용
    /// </summary>
    private void ApplyNewKeyBinding(string actionName, string keyName)
    {
        // 이전 키 바인딩 제거
        RemoveOldKeyBinding(actionName);
        
        // Input System에 바인딩 적용
        ApplyKeyBinding(actionName, keyName);
        
        // 버튼 텍스트 업데이트
        if (currentBindingButton != null)
        {
            currentBindingButton.SetText(keyName);
        }
        
        // 사용 중인 키 등록
        usedKeys[keyName] = actionName;
        
        // PlayerPrefs에 저장
        PlayerPrefs.SetString($"KeyBinding_{actionName}", keyName);
        PlayerPrefs.Save();
        
        Debug.Log($"SettingPanel: 키 바인딩 완료 - {actionName}: {keyName}");
        
        // UI 숨기기 및 게임 입력 복원
        HideKeyBindingUI();
        EnableGameInput();
        
        // 대기 상태 해제
        isWaitingForKeyInput = false;
        currentBindingAction = "";
        currentBindingButton = null;
    }
    
    /// <summary>
    /// 키 바인딩 취소
    /// </summary>
    private void CancelKeyBinding()
    {
        if (currentBindingButton != null)
        {
            // 이전 키로 복원
            string savedKey = PlayerPrefs.GetString($"KeyBinding_{currentBindingAction}", "");
            if (string.IsNullOrEmpty(savedKey))
            {
                savedKey = GetDefaultKeyForAction(currentBindingAction);
            }
            
            currentBindingButton.SetText(savedKey);
        }
        
        // UI 숨기기 및 게임 입력 복원
        HideKeyBindingUI();
        EnableGameInput();
        
        isWaitingForKeyInput = false;
        currentBindingAction = "";
        currentBindingButton = null;
        
        Debug.Log("SettingPanel: 키 바인딩 취소 (게임 입력 복원됨)");
    }
    
    /// <summary>
    /// 이전 키 바인딩 제거
    /// </summary>
    private void RemoveOldKeyBinding(string actionName)
    {
        // usedKeys에서 현재 액션이 사용하던 키 찾아서 제거
        string oldKey = "";
        foreach (var kvp in usedKeys)
        {
            if (kvp.Value == actionName)
            {
                oldKey = kvp.Key;
                break;
            }
        }
        
        if (!string.IsNullOrEmpty(oldKey))
        {
            usedKeys.Remove(oldKey);
        }
    }
    
    /// <summary>
    /// 키가 이미 사용 중인지 확인
    /// </summary>
    private bool IsKeyAlreadyUsed(string keyName, string excludeAction)
    {
        if (!usedKeys.ContainsKey(keyName))
        {
            return false;
        }
        
        // 자기 자신의 이전 키는 허용
        return usedKeys[keyName] != excludeAction;
    }
    
    /// <summary>
    /// 키 바인딩을 PlayerPrefs에 저장 (InputManager가 자동으로 로드)
    /// </summary>
    private void ApplyKeyBinding(string actionName, string keyName)
    {
        // PlayerPrefs에 저장만 하면 InputManager가 자동으로 로드
        PlayerPrefs.SetString($"KeyBinding_{actionName}", keyName);
        PlayerPrefs.Save();
        
        Debug.Log($"SettingPanel: 키 바인딩 저장 - {actionName}: {keyName}");
        Debug.Log("⚠️ 키 바인딩은 다음 씬 로드 또는 플레이어 스폰 시 적용됩니다.");
    }
    
    
    /// <summary>
    /// 액션의 기본 키 가져오기
    /// </summary>
    private string GetDefaultKeyForAction(string actionName)
    {
        switch (actionName)
        {
            case "Up": return "W";
            case "Down": return "S";
            case "Left": return "A";
            case "Right": return "D";
            case "Jump": return "Space";
            case "Reload": return "R";
            case "Skill": return "Q";
            case "Item": return "E";
            case "ChangeItem": return "Tab";
            case "Detach": return "LeftShift";
            default: return "None";
        }
    }
    
    /// <summary>
    /// KeyCode를 표시용 이름으로 변환
    /// </summary>
    private string GetKeyDisplayName(KeyCode keyCode)
    {
        // 특수 키 이름 변환
        switch (keyCode)
        {
            case KeyCode.Space: return "Space";
            case KeyCode.LeftShift: return "LeftShift";
            case KeyCode.RightShift: return "RightShift";
            case KeyCode.LeftControl: return "LeftCtrl";
            case KeyCode.RightControl: return "RightCtrl";
            case KeyCode.LeftAlt: return "LeftAlt";
            case KeyCode.RightAlt: return "RightAlt";
            case KeyCode.Return: return "Enter";
            case KeyCode.Escape: return "Escape";
            case KeyCode.Backspace: return "Backspace";
            case KeyCode.Tab: return "Tab";
            default:
                return keyCode.ToString();
        }
    }
    
    /// <summary>
    /// 키 이름을 Input System 경로로 변환
    /// </summary>
    private string GetKeyPath(string keyName)
    {
        // 특수 키 처리
        switch (keyName)
        {
            case "Space": return "<Keyboard>/space";
            case "LeftShift": return "<Keyboard>/leftShift";
            case "RightShift": return "<Keyboard>/rightShift";
            case "LeftCtrl": return "<Keyboard>/leftCtrl";
            case "RightCtrl": return "<Keyboard>/rightCtrl";
            case "LeftAlt": return "<Keyboard>/leftAlt";
            case "RightAlt": return "<Keyboard>/rightAlt";
            case "Enter": return "<Keyboard>/enter";
            case "Escape": return "<Keyboard>/escape";
            case "Backspace": return "<Keyboard>/backspace";
            case "Tab": return "<Keyboard>/tab";
            default:
                // 일반 키 (소문자로 변환)
                return $"<Keyboard>/{keyName.ToLower()}";
        }
    }
    
    #endregion
    
    #region 게임 입력 차단/복원
    
    // HotkeyEvent 캐시 (비활성화/활성화 추적용)
    private List<Michsky.UI.Heat.HotkeyEvent> cachedHotkeyEvents = new List<Michsky.UI.Heat.HotkeyEvent>();
    
    /// <summary>
    /// 모든 게임 입력 차단 (키 바인딩 중)
    /// </summary>
    private void DisableGameInput()
    {
        // 1. InputManager 비활성화 (게임 조작 차단)
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject player in players)
        {
            Photon.Pun.PhotonView pv = player.GetComponent<Photon.Pun.PhotonView>();
            if (pv != null && pv.IsMine)
            {
                InputManager inputManager = player.GetComponent<InputManager>();
                if (inputManager != null)
                {
                    inputManager.DisableInput();
                    Debug.Log("SettingPanel: 게임 입력 차단 완료");
                }
            }
        }
        
        // 2. 모든 HotkeyEvent 비활성화 (HeatUI 단축키 차단)
        DisableAllHotkeyEvents();
    }
    
    /// <summary>
    /// 게임 입력 복원
    /// </summary>
    private void EnableGameInput()
    {
        // 1. InputManager 활성화 (게임 조작 복원)
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject player in players)
        {
            Photon.Pun.PhotonView pv = player.GetComponent<Photon.Pun.PhotonView>();
            if (pv != null && pv.IsMine)
            {
                InputManager inputManager = player.GetComponent<InputManager>();
                if (inputManager != null)
                {
                    inputManager.EnableInput();
                    Debug.Log("SettingPanel: 게임 입력 복원 완료");
                }
            }
        }
        
        // 2. HotkeyEvent 복원
        EnableAllHotkeyEvents();
    }
    
    /// <summary>
    /// 모든 HotkeyEvent 비활성화 (HeatUI 단축키 차단)
    /// </summary>
    private void DisableAllHotkeyEvents()
    {
        cachedHotkeyEvents.Clear();
        
        // 씬의 모든 HotkeyEvent 찾기
        Michsky.UI.Heat.HotkeyEvent[] allHotkeys = GameObject.FindObjectsOfType<Michsky.UI.Heat.HotkeyEvent>(true);
        
        foreach (var hotkey in allHotkeys)
        {
            // 현재 활성화된 HotkeyEvent만 비활성화
            if (hotkey.enabled)
            {
                cachedHotkeyEvents.Add(hotkey);
                hotkey.enabled = false;
            }
        }
        
        Debug.Log($"SettingPanel: HeatUI 단축키 차단 완료 ({cachedHotkeyEvents.Count}개)");
    }
    
    /// <summary>
    /// 비활성화했던 HotkeyEvent 복원
    /// </summary>
    private void EnableAllHotkeyEvents()
    {
        foreach (var hotkey in cachedHotkeyEvents)
        {
            if (hotkey != null)
            {
                hotkey.enabled = true;
            }
        }
        
        Debug.Log($"SettingPanel: HeatUI 단축키 복원 완료 ({cachedHotkeyEvents.Count}개)");
        cachedHotkeyEvents.Clear();
    }
    
    #endregion
    
    #region UI 피드백
    
    /// <summary>
    /// 키 바인딩 UI 표시 (깜박임 애니메이션)
    /// </summary>
    private void ShowKeyBindingUI(string actionName)
    {
        if (keyBindingText == null) return;
        
        // 텍스트 설정
        string actionDisplayName = GetActionDisplayName(actionName);
        keyBindingText.text = $"<b>{actionDisplayName}</b> 키를 입력하세요...\n(ESC로 취소)";
        
        // 활성화
        keyBindingText.gameObject.SetActive(true);
        
        // 초기 투명도 설정
        Color textColor = keyBindingText.color;
        textColor.a = 1f;
        keyBindingText.color = textColor;
        
        // 깜박임 애니메이션 시작
        keyBindingTextBlinkTween = keyBindingText.DOFade(0.3f, 0.5f)
            .SetEase(DG.Tweening.Ease.InOutSine)
            .SetLoops(-1, DG.Tweening.LoopType.Yoyo);
        
        Debug.Log($"SettingPanel: UI 피드백 표시 - {actionDisplayName}");
    }
    
    /// <summary>
    /// 키 바인딩 UI 숨기기
    /// </summary>
    private void HideKeyBindingUI()
    {
        if (keyBindingText == null) return;
        
        // 애니메이션 중지
        CleanupKeyBindingAnimation();
        
        // 비활성화
        keyBindingText.gameObject.SetActive(false);
        
        Debug.Log("SettingPanel: UI 피드백 숨김");
    }
    
    /// <summary>
    /// 키 바인딩 애니메이션 정리
    /// </summary>
    private void CleanupKeyBindingAnimation()
    {
        keyBindingTextBlinkTween?.Kill();
        keyBindingTextBlinkTween = null;
        
        // 투명도 복원
        if (keyBindingText != null)
        {
            Color textColor = keyBindingText.color;
            textColor.a = 1f;
            keyBindingText.color = textColor;
        }
    }
    
    /// <summary>
    /// 액션 이름을 표시용 이름으로 변환
    /// </summary>
    private string GetActionDisplayName(string actionName)
    {
        switch (actionName)
        {
            case "Up": return "앞으로 이동";
            case "Down": return "뒤로 이동";
            case "Left": return "왼쪽 이동";
            case "Right": return "오른쪽 이동";
            case "Jump": return "점프";
            case "Reload": return "재장전";
            case "Skill": return "스킬";
            case "Item": return "아이템 사용";
            case "ChangeItem": return "아이템 변경";
            case "Detach": return "왕관 떨어뜨리기";
            default: return actionName;
        }
    }
    
    #endregion
    
    #region 공개 메서드
    
    /// <summary>
    /// 설정 초기화 (기본값으로)
    /// </summary>
    public void ResetToDefault()
    {
        // 감도 초기화
        if (xSensivitySlider != null)
        {
            xSensivitySlider.mainSlider.value = DEFAULT_SENSITIVITY;
        }
        
        if (ySensivitySlider != null)
        {
            ySensivitySlider.mainSlider.value = DEFAULT_SENSITIVITY;
        }
        
        // 볼륨 초기화
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.mainSlider.value = DEFAULT_VOLUME;
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.mainSlider.value = DEFAULT_VOLUME;
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.mainSlider.value = DEFAULT_VOLUME;
        }
        
        if (uiVolumeSlider != null)
        {
            uiVolumeSlider.mainSlider.value = DEFAULT_VOLUME;
        }
        
        // 키 바인딩 초기화
        ResetKeyBindings();
        
        // 저장
        SaveAllSettings();
        
        Debug.Log("SettingPanel: 모든 설정 초기화 완료");
    }
    
    /// <summary>
    /// 키 바인딩 초기화
    /// </summary>
    private void ResetKeyBindings()
    {
        foreach (var kvp in keyBindingButtons)
        {
            string actionName = kvp.Key;
            ButtonManager button = kvp.Value;
            
            if (button == null) continue;
            
            // 기본 키로 설정
            string defaultKey = GetDefaultKeyForAction(actionName);
            button.SetText(defaultKey);
            
            // Input System에 적용
            ApplyKeyBinding(actionName, defaultKey);
            
            // PlayerPrefs 삭제
            PlayerPrefs.DeleteKey($"KeyBinding_{actionName}");
        }
        
        // 사용 중인 키 다시 로드
        LoadKeyBindings();
    }
    
    /// <summary>
    /// 설정 패널 닫을 때 호출 (저장)
    /// </summary>
    public void OnClosePanel()
    {
        SaveAllSettings();
        Debug.Log("SettingPanel: 패널 닫기 - 설정 저장");
    }
    
    #endregion
}
