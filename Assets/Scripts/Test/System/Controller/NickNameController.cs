using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Michsky.UI.Heat;
using Photon.Pun;

public class NickNameController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField nicknameInputField;
    /// <summary>
    /// 닉네임 설정 (데이터베이스 기반 시스템 호환)
    /// 현재 로그인된 사용자의 닉네임을 사용
    /// </summary>
    /// <param name="name">설정할 닉네임 (데이터베이스 시스템에서는 무시됨)</param>
    public void SetNickName(string name) 
    {
        // 데이터베이스 기반 시스템에서는 CurrentUser에서 닉네임을 가져옴
        if (CurrentUser.Instance.IsLoggedIn())
        {
            string dbNickname = CurrentUser.Instance.GetNickname();
            
            // 로컬에 저장 (기존 시스템과의 호환성을 위해)
            PlayerPrefs.SetString("NickName", dbNickname);
            PlayerPrefs.Save();
            
            // Photon Custom Properties에도 저장 (멀티플레이어용)
            if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
            {
                var props = new ExitGames.Client.Photon.Hashtable();
                props["nickname"] = dbNickname;
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                
                Debug.Log($"NickNameController: 데이터베이스 닉네임 설정 완료 - {dbNickname}");
            }
            else
            {
                Debug.LogWarning("NickNameController: Photon 연결되지 않음, 로컬에만 저장");
            }
        }
        else
        {
            // 로그인되지 않은 경우 기존 방식 사용 (하위 호환성)
            PlayerPrefs.SetString("NickName", name);
            PlayerPrefs.Save();
            
            if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
            {
                var props = new ExitGames.Client.Photon.Hashtable();
                props["nickname"] = name;
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                
                Debug.Log($"NickNameController: 기존 방식 닉네임 설정 완료 - {name}");
            }
            
            Debug.LogWarning("NickNameController: 로그인되지 않은 상태에서 닉네임 설정");
        }
    }
    
    /// <summary>
    /// 닉네임 가져오기 (데이터베이스 기반 시스템 우선)
    /// </summary>
    /// <returns>저장된 닉네임 (기본값: "Player")</returns>
    public string GetNickName() 
    {
        // 데이터베이스 기반 시스템에서 로그인된 경우 DB 닉네임 우선 사용
        if (CurrentUser.Instance.IsLoggedIn())
        {
            return CurrentUser.Instance.GetNickname();
        }
        
        // 로그인되지 않은 경우 기존 방식 사용
        return PlayerPrefs.GetString("NickName", "Player"); 
    }

    /// <summary>
    /// UI 입력 필드에서 닉네임 변경 시 호출되는 메서드
    /// </summary>
    /// <param name="name">입력된 닉네임</param>
    public void OnNickNameChange(string name) 
    { 
       
        
        // 닉네임 길이 제한 (최대 12자)
        if (name.Length > 12)
        {
            name = name.Substring(0, 12);
            Debug.LogWarning($"NickNameController: 닉네임이 12자로 제한됨 - {name}");
        }
        
        SetNickName(name); 
    }
    
 
    /// <summary>
    /// 게임 시작 시 저장된 닉네임을 UI에 로드하고 Photon에 동기화
    /// </summary>
    void Start()
    {
        // 저장된 닉네임 로드
        LoadSavedNickname();
        
        // InputField 이벤트 연결
        SetupInputFieldEvents();
    }
    
    /// <summary>
    /// 저장된 닉네임을 InputField에 로드
    /// </summary>
    private void LoadSavedNickname()
    {
        string savedNickname = GetNickName();
        
        // 데이터베이스 기반 시스템에서는 InputField를 읽기 전용으로 처리
        if (CurrentUser.Instance.IsLoggedIn())
        {
            if (nicknameInputField != null)
            {
                nicknameInputField.text = savedNickname;
                nicknameInputField.interactable = false; // 데이터베이스 닉네임은 수정 불가
                Debug.Log($"NickNameController: 데이터베이스 닉네임 로드 완료 - {savedNickname} (읽기 전용)");
            }
        }
        else
        {
            // 기존 방식: InputField에 저장된 닉네임 표시 (수정 가능)
            if (nicknameInputField != null && !string.IsNullOrWhiteSpace(savedNickname) && savedNickname != "Player")
            {
                nicknameInputField.text = savedNickname;
                nicknameInputField.interactable = true;
                Debug.Log($"NickNameController: 기존 방식 닉네임 로드 완료 - {savedNickname}");
            }
        }
        
        // Photon에 동기화 (연결되어 있다면)
        if (!string.IsNullOrWhiteSpace(savedNickname))
        {
            SetNickName(savedNickname);
        }
    }
    
    /// <summary>
    /// InputField 이벤트 설정
    /// </summary>
    private void SetupInputFieldEvents()
    {
        if (nicknameInputField != null)
        {
            // OnValueChanged 이벤트에 연결 (Inspector에서 설정하지 않은 경우 백업)
            nicknameInputField.onValueChanged.AddListener(OnNickNameChange);
            
            // OnEndEdit 이벤트에도 연결 (입력 완료 시)
            nicknameInputField.onEndEdit.AddListener(OnNickNameEndEdit);
            
            Debug.Log("✅ NickNameController: InputField 이벤트 연결 완료");
        }
        else
        {
            Debug.LogWarning("NickNameController: InputField를 찾을 수 없습니다.");
        }
    }
    
    /// <summary>
    /// 입력 완료 시 호출되는 메서드
    /// </summary>
    private void OnNickNameEndEdit(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            OnNickNameChange(name);
        }
    }
}
