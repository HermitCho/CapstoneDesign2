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
            currentSpawnedLobbyCharacter = Instantiate(cachedPlayerPrefabData[0], currentSpawnedLobbyCharacter.transform.position, currentSpawnedLobbyCharacter.transform.rotation);
        }
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
            Debug.Log("✅ SelectCharController: DataBase 정보 캐싱 완료");
        }
    }
    catch (System.Exception e)
    {
        Debug.LogError($"❌ SelectCharController: DataBase 캐싱 중 오류: {e.Message}");
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
}
