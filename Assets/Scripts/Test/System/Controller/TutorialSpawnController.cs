using System.Collections;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// 튜토리얼 전용 스폰 컨트롤러
/// 1인 전용 환경에서 첫 번째 캐릭터 자동 스폰
/// Photon PUN2 최적화 (1인이므로 네트워크 동기화 최소화)
/// </summary>
public class TutorialSpawnController : MonoBehaviourPunCallbacks
{
    [Header("스폰 위치 설정")]
    [SerializeField] private Transform tutorialSpawnPosition;
    [SerializeField] private Transform spawnParent;
    
    [Header("스폰 설정")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    [SerializeField] private float spawnDelay = 0.5f;
    [SerializeField] private bool autoSpawnOnStart = true;
    
    // 캐싱된 데이터
    private DataBase.PlayerData playerData;
    private GameObject[] cachedPlayerPrefabData;
    private bool dataBaseCached = false;
    
    // 스폰 상태
    private GameObject spawnedCharacter = null;
    private bool hasSpawned = false;
    private bool isSpawning = false;
    
    #region Unity 생명주기
    
    void Awake()
    {
        // 튜토리얼 씬 진입 시마다 상태 초기화
        TutorialStateManager.ResetAll();
        Debug.Log("TutorialSpawnController: TutorialStateManager 초기화");
        
        // DataBase 정보 캐싱
        CacheDataBaseInfo();
        
        // 스폰 위치 검증
        ValidateSpawnPosition();
    }
    
    void Start()
    {
        // 튜토리얼 방인지 확인
        if (!IsTutorialRoom())
        {
            Debug.LogWarning("TutorialSpawnController: 튜토리얼 방이 아닙니다. 비활성화됩니다.");
            enabled = false;
            return;
        }
        
        // 자동 스폰 설정이 활성화되어 있다면
        if (autoSpawnOnStart)
        {
            StartCoroutine(AutoSpawnRoutine());
        }
    }
    
    void OnDrawGizmos()
    {
        if (tutorialSpawnPosition != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 position = tutorialSpawnPosition.position + spawnOffset;
            Gizmos.DrawWireSphere(position, 1f);
            
#if UNITY_EDITOR
            UnityEditor.Handles.Label(position + Vector3.up * 1.5f, "Tutorial Spawn");
#endif
        }
    }
    
    #endregion
    
    #region 초기화 및 검증
    
    /// <summary>
    /// DataBase 정보 캐싱
    /// </summary>
    private void CacheDataBaseInfo()
    {
        try
        {
            if (DataBase.Instance == null)
            {
                Debug.LogWarning("TutorialSpawnController: DataBase 인스턴스가 없습니다.");
                dataBaseCached = false;
                return;
            }
            
            if (DataBase.Instance.playerData != null)
            {
                playerData = DataBase.Instance.playerData;
                cachedPlayerPrefabData = playerData.PlayerPrefabData.ToArray();
                dataBaseCached = true;
                
                Debug.Log($"TutorialSpawnController: DataBase 캐싱 완료 - {cachedPlayerPrefabData.Length}개 캐릭터");
            }
            else
            {
                Debug.LogError("TutorialSpawnController: PlayerData가 null입니다.");
                dataBaseCached = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TutorialSpawnController: DataBase 캐싱 중 오류 - {e.Message}");
            dataBaseCached = false;
        }
    }
    
    /// <summary>
    /// 스폰 위치 검증
    /// </summary>
    private void ValidateSpawnPosition()
    {
        if (tutorialSpawnPosition == null)
        {
            Debug.LogError("TutorialSpawnController: 튜토리얼 스폰 위치가 설정되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 튜토리얼 방인지 확인
    /// </summary>
    private bool IsTutorialRoom()
    {
        if (!PhotonNetwork.InRoom) return false;
        
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        
        if (props.TryGetValue("isTutorial", out object isTutorialObj))
        {
            return (bool)isTutorialObj == true;
        }
        
        return false;
    }
    
    #endregion
    
    #region 자동 스폰
    
    /// <summary>
    /// 자동 스폰 루틴 (튜토리얼 입장 시)
    /// </summary>
    private IEnumerator AutoSpawnRoutine()
    {
        // 네트워크 안정화 대기
        yield return new WaitForSeconds(0.5f);
        
        // Photon 연결 상태 확인
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InRoom)
        {
            Debug.LogError("TutorialSpawnController: Photon 연결 상태가 준비되지 않음");
            yield break;
        }
        
        // 튜토리얼 방 재확인
        if (!IsTutorialRoom())
        {
            Debug.LogWarning("TutorialSpawnController: 튜토리얼 방이 아닙니다.");
            yield break;
        }
        
        // 이미 스폰했다면 중복 방지
        if (hasSpawned)
        {
            Debug.Log("TutorialSpawnController: 이미 캐릭터가 스폰되었습니다.");
            yield break;
        }
        
        // 첫 번째 캐릭터 스폰
        SpawnFirstCharacter();
    }
    
    #endregion
    
    #region 캐릭터 스폰
    
    /// <summary>
    /// 첫 번째 캐릭터 스폰 (튜토리얼 전용)
    /// </summary>
    public void SpawnFirstCharacter()
    {
        if (isSpawning)
        {
            Debug.Log("TutorialSpawnController: 이미 스폰 중입니다.");
            return;
        }
        
        if (hasSpawned)
        {
            Debug.Log("TutorialSpawnController: 이미 캐릭터가 스폰되었습니다.");
            return;
        }
        
        // DataBase 캐싱 확인
        if (!dataBaseCached)
        {
            CacheDataBaseInfo();
        }
        
        if (cachedPlayerPrefabData == null || cachedPlayerPrefabData.Length == 0)
        {
            Debug.LogError("TutorialSpawnController: 플레이어 프리팹 데이터가 없습니다!");
            return;
        }
        
        // ✅ 튜토리얼은 무조건 첫 번째 캐릭터 사용 (인덱스 0 고정)
        int tutorialCharacterIndex = 0;
        
        Debug.Log($"✅ TutorialSpawnController: 튜토리얼 전용 캐릭터 사용 - 인덱스 {tutorialCharacterIndex} 고정");
        
        // 첫 번째 캐릭터 프리팹 가져오기
        GameObject firstCharacterPrefab = cachedPlayerPrefabData[tutorialCharacterIndex];
        
        if (firstCharacterPrefab == null)
        {
            Debug.LogError($"TutorialSpawnController: 인덱스 {tutorialCharacterIndex}의 캐릭터 프리팹이 null입니다!");
            return;
        }
        
        Debug.Log($"TutorialSpawnController: 튜토리얼 캐릭터 스폰 - {firstCharacterPrefab.name} (인덱스: {tutorialCharacterIndex})");
        
        // 스폰 코루틴 시작
        StartCoroutine(SpawnCharacterCoroutine(firstCharacterPrefab));
    }
    
    /// <summary>
    /// 캐릭터 스폰 코루틴
    /// </summary>
    private IEnumerator SpawnCharacterCoroutine(GameObject prefab)
    {
        isSpawning = true;
        
        // 스폰 지연
        if (spawnDelay > 0f)
        {
            yield return new WaitForSeconds(spawnDelay);
        }
        
        // 스폰 위치 및 회전 계산
        Vector3 spawnPosition = GetSpawnPosition();
        Quaternion spawnRotation = GetSpawnRotation();
        
        // Photon을 통한 캐릭터 인스턴스 생성
        try
        {
            Debug.Log($"TutorialSpawnController: PhotonNetwork.Instantiate 호출 - {prefab.name}");
            
            spawnedCharacter = PhotonNetwork.Instantiate(
                $"Prefabs/InGameCharacter/{prefab.name}", 
                spawnPosition, 
                spawnRotation
            );
            
            // PhotonView 확인
            PhotonView pv = spawnedCharacter.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                Debug.Log("TutorialSpawnController: 로컬 플레이어 캐릭터 스폰 완료");
                
                // 닉네임 설정
                SetPlayerNicknameProperty();
                
                // 부모 설정
                if (spawnParent != null)
                {
                    spawnedCharacter.transform.SetParent(spawnParent);
                }
                
                // GameManager에 알림
                NotifyGameManager();
            }
            
            hasSpawned = true;
            Debug.Log("TutorialSpawnController: 캐릭터 스폰 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TutorialSpawnController: 캐릭터 스폰 실패 - {e.Message}");
        }
        
        isSpawning = false;
    }
    
    /// <summary>
    /// 스폰 위치 계산
    /// </summary>
    private Vector3 GetSpawnPosition()
    {
        if (tutorialSpawnPosition != null)
        {
            return tutorialSpawnPosition.position + spawnOffset;
        }
        
        // 기본 위치 (0, 1, 0)
        Debug.LogWarning("TutorialSpawnController: 스폰 위치가 설정되지 않아 기본 위치 사용");
        return new Vector3(0f, 1f, 0f) + spawnOffset;
    }
    
    /// <summary>
    /// 스폰 회전 계산
    /// </summary>
    private Quaternion GetSpawnRotation()
    {
        if (tutorialSpawnPosition != null)
        {
            return tutorialSpawnPosition.rotation;
        }
        
        // 기본 회전 (정면)
        return Quaternion.identity;
    }
    
    #endregion
    
    #region 플레이어 정보 설정
    
    /// <summary>
    /// 플레이어 닉네임을 Photon Custom Properties에 설정
    /// </summary>
    private void SetPlayerNicknameProperty()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null) return;
        
        string nickname = "";
        
        // 1. CurrentUser에서 닉네임 가져오기
        if (CurrentUser.Instance != null && CurrentUser.Instance.IsLoggedIn())
        {
            nickname = CurrentUser.Instance.GetNickname();
        }
        
        // 2. PlayerPrefs에서 닉네임 가져오기
        if (string.IsNullOrEmpty(nickname))
        {
            nickname = PlayerPrefs.GetString("NickName", "");
        }
        
        // 3. 기본값 설정
        if (string.IsNullOrEmpty(nickname))
        {
            nickname = $"Player{PhotonNetwork.LocalPlayer.ActorNumber}";
        }
        
        // Photon Custom Properties에 닉네임 설정
        var props = new ExitGames.Client.Photon.Hashtable();
        props["nickname"] = nickname;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        
        // PhotonNetwork.NickName도 설정
        PhotonNetwork.NickName = nickname;
        
        Debug.Log($"TutorialSpawnController: 플레이어 닉네임 설정 - {nickname}");
    }
    
    #endregion
    
    #region GameManager 연동
    
    /// <summary>
    /// GameManager에 스폰 알림
    /// </summary>
    private void NotifyGameManager()
    {
        if (spawnedCharacter == null) return;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.FindPlayerAfterSpawn();
            GameManager.Instance.NotifyCharacterSpawned();
            
            Debug.Log("TutorialSpawnController: GameManager에 스폰 알림 완료");
        }
        else
        {
            Debug.LogWarning("TutorialSpawnController: GameManager 인스턴스를 찾을 수 없습니다.");
        }
    }
    
    #endregion
    
    #region 공개 메서드
    
    /// <summary>
    /// 수동으로 캐릭터 스폰 (외부 호출용)
    /// </summary>
    public void ManualSpawnFirstCharacter()
    {
        if (hasSpawned)
        {
            Debug.LogWarning("TutorialSpawnController: 이미 캐릭터가 스폰되었습니다.");
            return;
        }
        
        SpawnFirstCharacter();
    }
    
    /// <summary>
    /// 현재 스폰된 캐릭터 가져오기
    /// </summary>
    public GameObject GetSpawnedCharacter()
    {
        return spawnedCharacter;
    }
    
    /// <summary>
    /// 스폰 완료 여부 확인
    /// </summary>
    public bool HasSpawned()
    {
        return hasSpawned;
    }
    
    /// <summary>
    /// 캐릭터 제거 (튜토리얼 재시작용)
    /// </summary>
    public void DestroyCurrentCharacter()
    {
        if (spawnedCharacter != null)
        {
            PhotonView pv = spawnedCharacter.GetComponent<PhotonView>();
            
            if (pv != null && pv.IsMine)
            {
                PhotonNetwork.Destroy(spawnedCharacter);
                Debug.Log("TutorialSpawnController: 캐릭터 제거 완료 (Photon)");
            }
            else
            {
                Destroy(spawnedCharacter);
                Debug.Log("TutorialSpawnController: 캐릭터 제거 완료 (Local)");
            }
            
            spawnedCharacter = null;
            hasSpawned = false;
        }
    }
    
    /// <summary>
    /// 튜토리얼 재시작 (캐릭터 리스폰)
    /// </summary>
    public void RestartTutorial()
    {
        DestroyCurrentCharacter();
        
        StartCoroutine(RestartSpawnRoutine());
    }
    
    /// <summary>
    /// 재시작 스폰 루틴
    /// </summary>
    private IEnumerator RestartSpawnRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        
        SpawnFirstCharacter();
    }
    
    #endregion
    
    #region Photon 콜백
    
    public override void OnJoinedRoom()
    {
        Debug.Log("TutorialSpawnController: 방 입장 완료");
        
        // 튜토리얼 방인지 확인
        if (!IsTutorialRoom())
        {
            Debug.Log("TutorialSpawnController: 튜토리얼 방이 아니므로 무시");
            return;
        }
        
        // 자동 스폰이 비활성화되어 있고 아직 스폰하지 않았다면
        if (!autoSpawnOnStart && !hasSpawned)
        {
            Debug.Log("TutorialSpawnController: 자동 스폰 비활성화됨 (수동 호출 대기)");
        }
    }
    
    public override void OnLeftRoom()
    {
        Debug.Log("TutorialSpawnController: 방 나가기 완료");
        
        // 상태 초기화
        hasSpawned = false;
        isSpawning = false;
        spawnedCharacter = null;
    }
    
    #endregion
}

