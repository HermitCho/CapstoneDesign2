using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectCharController : MonoBehaviour
{
    private GameObject[] cachedPlayerPrefabData;
    private bool dataBaseCached = false;
    private DataBase.LobbyData lobbyData;

    private GameObject currentSelectedPrefab;

    [HideInInspector] public int currentSelectedIndex = 0;

    [Header("로비 캐릭터 스폰 위치")]
    [SerializeField] private GameObject currentSpawnedLobbyCharacter;
    
    [Header("캐릭터 선택 버튼들")]
    [SerializeField] private ClickableButton[] characterButtons;
    
    [Header("캐릭터별 애니메이션 컨트롤러")]
    [Tooltip("각 캐릭터 선택 화면의 LobbyAnimationController를 순서대로 할당하세요")]
    [SerializeField] private LobbyAnimationController[] characterAnimControllers;

    void Awake()
    {
        // 저장된 선택된 캐릭터 인덱스 복원
        currentSelectedIndex = PlayerPrefs.GetInt("SelectChar_CurrentIndex", 0);
    }

    void OnEnable()
    {
        CacheDataBaseInfo();
    }

    void Start()
    {
        // 저장된 인덱스에 해당하는 캐릭터 스폰
        if (currentSelectedIndex >= 0 && currentSelectedIndex < cachedPlayerPrefabData.Length)
        {
            currentSelectedPrefab = cachedPlayerPrefabData[currentSelectedIndex];
            currentSpawnedLobbyCharacter = Instantiate(currentSelectedPrefab, currentSpawnedLobbyCharacter.transform.position, currentSpawnedLobbyCharacter.transform.rotation);
        }
        else
        {
            // 저장된 인덱스가 유효하지 않으면 기본값(0번째) 사용
            currentSelectedIndex = 0;
            currentSpawnedLobbyCharacter = Instantiate(cachedPlayerPrefabData[0], currentSpawnedLobbyCharacter.transform.position, currentSpawnedLobbyCharacter.transform.rotation);
        }
        
        // 초기 선택된 버튼 상태 설정
        UpdateButtonStates();
    }



   void CacheDataBaseInfo()
   {
    try
    {
        if (!dataBaseCached)
        {
            lobbyData = DataBase.Instance.lobbyData;
            cachedPlayerPrefabData = lobbyData.LobbyCharacterPrefabData.ToArray();
            dataBaseCached = true;
        }
    }
    catch (System.Exception e)
    {
        dataBaseCached = false;
    }
   }


    public void OnSelectChar(int index)
    {
        if (index < 0 || index >= cachedPlayerPrefabData.Length)
        {
            return;
        }

        currentSelectedPrefab = cachedPlayerPrefabData[index];
        currentSelectedIndex = index;
        
        // 선택한 캐릭터 인덱스 저장
        PlayerPrefs.SetInt("SelectChar_CurrentIndex", currentSelectedIndex);
        PlayerPrefs.Save();
        
        // 버튼 상태 업데이트
        UpdateButtonStates();
        
        // ✅ 캐릭터 목소리 사운드 재생
        PlayCharacterVoice(index);
        
        // ✅ 선택된 캐릭터의 Select 애니메이션 재생
        if (characterAnimControllers != null && index < characterAnimControllers.Length)
        {
            LobbyAnimationController animController = characterAnimControllers[index];
            if (animController != null)
            {
                animController.PlaySelectAnimation();
            }
        }
    }

    public void OnUpdateButton()
    {
        currentSelectedIndex = PlayerPrefs.GetInt("SelectChar_CurrentIndex", 0);
        StartCoroutine(OnUpdateButtonCoroutine());
    }

    IEnumerator OnUpdateButtonCoroutine()
    {
        yield return new WaitForSeconds(0.2f);
        UpdateButtonStates();
    }


    public void OnClickLobbyUpdateButton()
    {

        if (currentSelectedPrefab != null)
        {
            if (currentSpawnedLobbyCharacter != null)
            {
                Destroy(currentSpawnedLobbyCharacter);
            }

            currentSpawnedLobbyCharacter = Instantiate(currentSelectedPrefab, currentSpawnedLobbyCharacter.transform.position, currentSpawnedLobbyCharacter.transform.rotation);
        }
    }
    
    /// <summary>
    /// 버튼 상태를 업데이트합니다. 현재 선택된 버튼만 클릭 상태로 유지합니다.
    /// </summary>
    private void UpdateButtonStates()
    {
        if (characterButtons == null) return;
        
        for (int i = 0; i < characterButtons.Length; i++)
        {
            if (characterButtons[i] != null)
            {
                if (i == currentSelectedIndex)
                {
                    // 현재 선택된 버튼은 클릭 상태로 설정 (Highlighted 유지)
                    characterButtons[i].SetClicked();
                }
                else
                {
                    // 다른 버튼들은 클릭 상태 해제
                    characterButtons[i].SetUnclicked();
                }
            }
        }
    }
    
    /// <summary>
    /// 캐릭터 목소리 사운드 재생 (index에 따라 Char1 ~ Char4 재생)
    /// </summary>
    /// <param name="index">캐릭터 인덱스 (0부터 시작)</param>
    private void PlayCharacterVoice(int index)
    {
        if (AudioManager.Inst == null) return;
        
        // 인덱스를 1부터 시작하는 캐릭터 번호로 변환
        int charNumber = index + 1;
        
        // SFX_Game_Char1, SFX_Game_Char2, SFX_Game_Char3, SFX_Game_Char4
        string soundName = $"SFX_Game_Char{charNumber}";
        
        // AudioManager를 통해 사운드 재생
        AudioManager.Inst.PlayOneShot(soundName);
    }
}
