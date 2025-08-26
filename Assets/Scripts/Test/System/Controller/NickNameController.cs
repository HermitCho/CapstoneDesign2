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
    /// 닉네임 설정 (로컬 저장 + Photon Custom Properties)
    /// </summary>
    /// <param name="name">설정할 닉네임</param>
    public void SetNickName(string name) 
    { 
        // 로컬에 저장
        PlayerPrefs.SetString("NickName", name);
        PlayerPrefs.Save();
        
        // Photon Custom Properties에도 저장 (멀티플레이어용)
        if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
        {
            var props = new ExitGames.Client.Photon.Hashtable();
            props["nickname"] = name;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            
            Debug.Log($"✅ NickNameController: 닉네임 설정 완료 - {name}");
        }
        else
        {
            Debug.LogWarning("⚠️ NickNameController: Photon 연결되지 않음, 로컬에만 저장");
        }
    }
    
    /// <summary>
    /// 닉네임 가져오기 (로컬 저장소에서)
    /// </summary>
    /// <returns>저장된 닉네임 (기본값: "Player")</returns>
    public string GetNickName() 
    { 
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
            Debug.LogWarning($"⚠️ NickNameController: 닉네임이 12자로 제한됨 - {name}");
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
        
        // InputField에 저장된 닉네임 표시
        if (nicknameInputField != null && !string.IsNullOrWhiteSpace(savedNickname) && savedNickname != "Player")
        {
            nicknameInputField.text = savedNickname;
            Debug.Log($"✅ NickNameController: 저장된 닉네임 로드 완료 - {savedNickname}");
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
            Debug.LogWarning("⚠️ NickNameController: InputField를 찾을 수 없습니다.");
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
