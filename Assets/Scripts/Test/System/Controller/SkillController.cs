using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

public class SkillController : MonoBehaviourPun
{

    #region 변수
    private DataBase.ItemData itemData;
    private DataBase.PlayerMoveData playerMoveData;
    private DataBase.PlayerData playerData;
    private Skill skill;
    private Skill activeItem;
    private Skill[] Skills;
    private Skill[] Items;
    private Dictionary<int, Skill> skillDictionary = new Dictionary<int, Skill>();
    private Dictionary<int, Skill> itemDictionary = new Dictionary<int, Skill>();
    private PhotonView photonView;
    private bool dataBaseCached = false;

    // ✅ 기절/조작 제어 관련 변수들
    private bool canUseSkill = true;
    private bool canUseItem = true;

    // 아이템 사용 쿨타임 관련 변수
    private float lastItemUseTime = 0f; // 마지막 아이템 사용 시간
    private const float itemUseCooldown = 0.5f; // 아이템 사용 쿨타임 (0.5초)

    // 프리뷰 관련 변수
    private bool isPreviewActive = false;
    private Skill currentPreviewSkill = null;

    #endregion





    #region 생명 주기
    void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    void OnEnable()
    {
        InputManager.OnSkillPressed += OnSkillInput;
        InputManager.OnItemPressed += OnItemInput; // 아이템 사용 중앙 관리
        InputManager.OnChangeItemPressed += OnChangeItemInput;
    }

    void OnDisable()
    {
        InputManager.OnSkillPressed -= OnSkillInput;
        InputManager.OnItemPressed -= OnItemInput; // 아이템 사용 중앙 관리
        InputManager.OnChangeItemPressed -= OnChangeItemInput;
    }

    void Start()
    {
        skill = GetComponent<Skill>();
        CacheDataBaseInfo();
        CacheDictionary();
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        // 프리뷰 업데이트
        UpdatePreview();
    }
    #endregion








    #region 캐싱
    // ========================================
    // === DataBase 캐싱 유틸리티 메서드들 ===
    // ========================================
    void CacheDataBaseInfo()
    {
        try
        {
            // DataBase 인스턴스가 없으면 잠시 대기 후 재시도
            if (DataBase.Instance == null)
            {
                Debug.LogWarning("⚠️ SkillController - DataBase 인스턴스가 아직 초기화되지 않음, 재시도 예정");
                StartCoroutine(RetryCacheDataBaseInfo());
                return;
            }

            if (DataBase.Instance.playerMoveData != null && DataBase.Instance.playerData != null && DataBase.Instance.itemData != null)
            {
                playerMoveData = DataBase.Instance.playerMoveData;
                playerData = DataBase.Instance.playerData;
                itemData = DataBase.Instance.itemData;
                Skills = playerData.PlayerPrefabData.Select(prefab => prefab.transform.GetComponent<Skill>()).Where(skill => skill != null).ToArray();
                Items = itemData.ItemPrefabData.Select(prefab => prefab.transform.GetComponent<Skill>()).Where(item => item != null).ToArray();
                dataBaseCached = true;
                Debug.Log("✅ SkillController - DataBase 정보 캐싱 완료");
            }
            else
            {
                Debug.LogWarning("⚠️ SkillController - DataBase 접근 실패, 기본값 사용");
                dataBaseCached = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ SkillController - DataBase 캐싱 중 오류: {e.Message}");
            dataBaseCached = false;
        }
    }

    IEnumerator RetryCacheDataBaseInfo()
    {
        int maxRetries = 10;
        int currentRetry = 0;

        while (currentRetry < maxRetries)
        {
            yield return new WaitForSeconds(0.1f); // 0.1초 대기

            if (DataBase.Instance != null)
            {
                CacheDataBaseInfo(); // 재귀 호출로 다시 시도
                yield break;
            }

            currentRetry++;
        }

        Debug.LogError("❌ MoveController - DataBase 캐싱 최대 재시도 횟수 초과, 기본값 사용");
        dataBaseCached = false;
    }

    void CacheDictionary()
    {
        skillDictionary.Clear();
        itemDictionary.Clear();

        if (Skills == null || Skills.Count() == 0) return;
        foreach (var skill in Skills)
        {
            if (!skillDictionary.ContainsKey(skill.Index))
                skillDictionary.Add(skill.Index, skill);
            else
                Debug.LogWarning($"중복된 Skill Index 발견: {skill.Index} ({skill.SkillName})");
            Debug.Log("[CacheDictionary] 캐시 스킬 " + skillDictionary.Count);
            foreach (var skill2 in skillDictionary)
            {
                Debug.Log($"[SkillDictionary] Key: {skill2.Key}, Value: {skill2.Value}");
            }
        }
        if (Items == null || Items.Count() == 0) return;
        foreach (var item in Items)
        {
            itemDictionary.Add(item.Index, item);
            Debug.Log("[CacheDictionary] 캐시 스킬 " + itemDictionary.Count);
            foreach (var item2 in skillDictionary)
            {
                Debug.Log($"[SkillDictionary] Key: {item2.Key}, Value: {item2.Value}");
            }
        }
    }

    public bool IsDataBaseCached()
    {
        return dataBaseCached;
    }

    public void RefreshDataBaseCache()
    {
        CacheDataBaseInfo();
    }
    #endregion






    #region 고유 스킬 관련
    // InputManager에서 스킬 입력 받기
    void OnSkillInput()
    {
        UseSkill();
    }

    public void UseSkill()
    {
        if (skill == null || !photonView.IsMine) return;

        if (skill.HasPreview)
        {
            if (isPreviewActive)
            {
                skill.ActivateSkill(this);
                EndPreview();
            }
            else
            {
                StartPreview(skill);
            }
        }
        else
        {
            skill.ActivateSkill(this);
        }
    }

    [PunRPC]
    public void ExecuteSkill(int skillIndex, Vector3 pos, Vector3 dir)
    {
        // 실질 동작은 자기 자신만
        if (photonView.IsMine && skill != null && skill.Index == skillIndex)
        {
            skill.Execute(this, pos, dir);
        }

        // 이펙트/사운드는 모든 클라이언트에서 실행

        PlaySkillEffectByIndex(skillIndex, pos, dir);
    }

    /// <summary>
    /// 스킬 타입 이름으로 이펙트 재생
    /// </summary>
    private void PlaySkillEffectByIndex(int skillIndex, Vector3 pos, Vector3 dir)
    {
        if (skillDictionary == null || skillDictionary.Count() == 0) return;
        // 현재 플레이어의 스킬 컴포넌트 찾기
        Skill targetSkill = skillDictionary[skillIndex];

        if (targetSkill != null)
        {
            Debug.Log($"✅ ExecuteSkill - 스킬 '{skillIndex}' 이펙트 재생");
            targetSkill.PlayEffectAtRemote(this, pos, dir);
        }
        else
        {
            Debug.LogWarning($"⚠️ ExecuteSkill - 스킬 인덱스 '{skillIndex}'을 찾을 수 없습니다.");
        }
    }

    [PunRPC]
    public void CastExecuteSkill(int skillIndex, Vector3 pos, Vector3 dir)
    {
        // 실질 동작은 자기 자신만
        if (photonView.IsMine && skill != null && skill.Index == skillIndex)
        {
            skill.CastExecute(this, pos, dir);
        }

        // 캐스팅 이펙트/사운드는 모든 클라이언트에서 실행
        PlaySkillCastEffectByIndex(skillIndex, pos, dir);
    }

    /// <summary>
    /// 스킬 타입 이름으로 캐스팅 이펙트 재생
    /// </summary>
    private void PlaySkillCastEffectByIndex(int skillIndex, Vector3 pos, Vector3 dir)
    {
        if (skillDictionary == null || skillDictionary.Count() == 0) return;
        // 현재 플레이어의 스킬 컴포넌트 찾기
        Skill targetSkill = skillDictionary[skillIndex];

        if (targetSkill != null)
        {
            Debug.Log($"✅ CastExecuteSkill - 스킬 '{skillIndex}' 캐스팅 이펙트 재생");
            targetSkill.PlayCastEffectAtRemote(this, pos, dir);
        }
        else
        {
            Debug.LogWarning($"⚠️ CastExecuteSkill - 스킬 인덱스 '{skillIndex}'을 찾을 수 없습니다.");
        }
    }
    #endregion




    #region 아이템 사용
    public void UseItem(ItemController itemController)
    {
        if (activeItem == null || !photonView.IsMine) return;

        if (activeItem.HasPreview)
        {
            if (isPreviewActive)
            {
                Debug.Log("[SkillController] UI 표시 아이템 사용!");
                activeItem.ActivateItem(this);

                // ✅ 남은 횟수가 0이면 삭제
                if (activeItem.RemainingUses <= 0)
                {
                    itemController.MoveUsedItemToTemp(activeItem.gameObject);
                    Destroy(activeItem.gameObject, activeItem.DestroyTime);
                }

                EndPreview();
            }
            else
            {
                Debug.Log("[SkillController] UI 표시 아이템");
                StartPreview(activeItem);
            }
        }
        else
        {
            Debug.Log("[SkillController] 즉발 아이템");
            activeItem.ActivateItem(this);

            // ✅ 남은 횟수가 0이면 삭제
            if (activeItem.RemainingUses <= 0)
            {
                itemController.MoveUsedItemToTemp(activeItem.gameObject);
                Destroy(activeItem.gameObject, activeItem.DestroyTime);
            }
        }
    }

    [PunRPC]
    public void ExecuteItem(int itemIndex, Vector3 pos, Vector3 dir)
    {
        if (photonView.IsMine && activeItem != null && activeItem.Index == itemIndex)
        {
            Debug.Log("[SkillController] 아이템 사용!");
            activeItem.Execute(this, pos, dir);
        }

        // 이펙트/사운드는 모든 클라이언트에서 실행
        PlayItemEffectByIndex(itemIndex, pos, dir);
    }

    /// <summary>
    /// 아이템 타입 이름으로 이펙트 재생
    /// </summary>
    private void PlayItemEffectByIndex(int itemIndex, Vector3 pos, Vector3 dir)
    {
        if (itemDictionary == null || itemDictionary.Count() == 0) return;
        // 현재 플레이어의 아이템 컴포넌트 찾기
        Skill targetItem = itemDictionary[itemIndex];

        if (targetItem != null)
        {
            Debug.Log($"✅ ExecuteItem - 아이템 '{itemIndex}' 이펙트 재생");
            targetItem.PlayEffectAtRemote(this, pos, dir);
        }
        else
        {
            Debug.LogWarning($"⚠️ ExecuteItem - 아이템 인덱스 '{itemIndex}'을 찾을 수 없습니다.");
        }
    }

    [PunRPC]
    public void CastExecuteItem(int itemIndex, Vector3 pos, Vector3 dir)
    {
        if (photonView.IsMine && activeItem != null && activeItem.Index == itemIndex)
        {
            activeItem.CastExecute(this, pos, dir);
        }

        // 캐스팅 이펙트/사운드는 모든 클라이언트에서 실행
        PlayItemCastEffectByIndex(itemIndex, pos, dir);
    }

    /// <summary>
    /// 아이템 타입 이름으로 캐스팅 이펙트 재생
    /// </summary>
    private void PlayItemCastEffectByIndex(int itemIndex, Vector3 pos, Vector3 dir)
    {
        if (itemDictionary == null || itemDictionary.Count() == 0) return;
        // 현재 플레이어의 아이템 컴포넌트 찾기
        Skill targetItem = itemDictionary[itemIndex];

        if (targetItem != null)
        {
            Debug.Log($"✅ CastExecuteItem - 아이템 '{itemIndex}' 캐스팅 이펙트 재생");
            targetItem.PlayCastEffectAtRemote(this, pos, dir);
        }
        else
        {
            Debug.LogWarning($"⚠️ CastExecuteItem - 아이템 인덱스 '{itemIndex}'을 찾을 수 없습니다.");
        }
    }

    // InputManager에서 아이템 입력 받기
    void OnItemInput()
    {
        // 상점이 열려있으면 아이템 사용 차단
        ShopController shopController = GetComponent<ShopController>();
        if (shopController != null && shopController.IsShopOpen())
        {
            return;
        }

        // 현재 플레이어의 활성화된 아이템 찾기
        ItemController itemController = FindCurrentPlayerItemController();
        if (itemController == null)
        {
            Debug.LogWarning("⚠️ MoveController - ItemController를 찾을 수 없습니다.");
            return;
        }

        // 활성화된 아이템 가져오기
        activeItem = itemController.GetFirstActiveItem();

        if (activeItem == null)
        {
            Debug.LogWarning("⚠️ MoveController - 활성화된 아이템이 없습니다.");
            return;
        }

        lastItemUseTime = Time.time;
        UseItem(itemController);
    }

    void OnChangeItemInput()
    {
        ItemController itemController = FindCurrentPlayerItemController();
        if (itemController == null)
        {
            Debug.LogWarning("⚠️ MoveController - ItemController를 찾을 수 없습니다.");
            return;
        }

        itemController.SwapFirstAndSecondItems();

    }

    /// <summary>
    /// 현재 플레이어의 ItemController 찾기
    /// </summary>
    /// <returns>현재 플레이어의 ItemController</returns>
    private ItemController FindCurrentPlayerItemController()
    {
        // 자신 기준으로 ItemController 찾기 (태그 기반 탐색 대신)
        ItemController itemController = GetComponent<ItemController>();
        if (itemController == null)
        {
            itemController = GetComponentInChildren<ItemController>();
        }

        if (itemController != null)
        {
            Debug.Log($"✅ MoveController - ItemController 찾음: {itemController.name}");
            return itemController;
        }

        // Fallback: 태그 기반 탐색 (기존 방식)
        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
        if (currentPlayer != null)
        {
            itemController = currentPlayer.GetComponent<ItemController>();
            if (itemController == null)
            {
                itemController = currentPlayer.GetComponentInChildren<ItemController>();
            }
            if (itemController != null)
            {
                Debug.Log($"⚠️ MoveController - 태그 기반으로 ItemController 찾음: {itemController.name}");
                return itemController;
            }
        }

        Debug.LogWarning("⚠️ MoveController - 플레이어의 ItemController를 찾을 수 없습니다.");
        return null;
    }
    #endregion










    #region 스킬 UI
    private void StartPreview(Skill skill)
    {
        if (skill == null) return;

        currentPreviewSkill = skill;
        isPreviewActive = true;
        skill.StartPreview(this);

        Debug.Log($"✅ 프리뷰 시작: {skill.SkillName}");
    }

    /// <summary>
    /// 프리뷰 업데이트
    /// </summary>
    private void UpdatePreview()
    {
        if (!isPreviewActive || currentPreviewSkill == null) return;

        // TestShoot 컴포넌트를 통해 정확한 조준 방향 계산
        TestShoot testShoot = GetComponent<TestShoot>();
        Vector3 direction = testShoot != null ? testShoot.CalculateShotDirection() : transform.forward;

        Vector3 origin = transform.position + transform.forward * 1.5f + transform.up * 1.5f;

        currentPreviewSkill.UpdatePreview(this, origin, direction);
    }

    /// <summary>
    /// 프리뷰 종료
    /// </summary>
    private void EndPreview()
    {
        if (currentPreviewSkill != null)
        {
            currentPreviewSkill.EndPreview(this);
        }

        currentPreviewSkill = null;
        isPreviewActive = false;

        Debug.Log("✅ 프리뷰 종료");
    }

    /// <summary>
    /// ESC 키로 프리뷰 취소 (InputManager에서 호출)
    /// </summary>
    public void CancelPreview()
    {
        if (isPreviewActive)
        {
            EndPreview();
        }
    }

    /// <summary>
    /// 프리뷰가 활성화되어 있는지 확인 (TestGun에서 사용)
    /// </summary>
    public bool IsPreviewActive()
    {
        return isPreviewActive;
    }
    #endregion





    #region 스킬 관련 캐릭터 상태
    public void DisableSkill()
    {
        canUseSkill = false;
        Debug.Log("✅ 스킬 사용 차단");
    }

    /// <summary>
    /// 스킬 사용 허용
    /// </summary>
    public void EnableSkill()
    {
        canUseSkill = true;
        Debug.Log("✅ 스킬 사용 허용");
    }

    /// <summary>
    /// 스킬 사용 가능 여부 확인
    /// </summary>
    /// <returns>스킬 사용 가능 여부</returns>
    public bool CanUseSkill()
    {
        return canUseSkill;
    }

    // --- 아이템 제어 메서드들 ---

    /// <summary>
    /// 아이템 사용 차단
    /// </summary>
    public void DisableItem()
    {
        canUseItem = false;
        Debug.Log("✅ 아이템 사용 차단");
    }

    /// <summary>
    /// 아이템 사용 허용
    /// </summary>
    public void EnableItem()
    {
        canUseItem = true;
        Debug.Log("✅ 아이템 사용 허용");
    }

    /// <summary>
    /// 아이템 사용 가능 여부 확인
    /// </summary>
    /// <returns>아이템 사용 가능 여부</returns>
    public bool CanUseItem()
    {
        return canUseItem;
    }


    public void DisableSkillControls()
    {
        DisableSkill();
        DisableItem();
        Debug.Log("✅ 스킬 조작 차단");
    }

    /// <summary>
    /// 모든 조작 허용
    /// </summary>
    public void EnableSkillControls()
    {
        EnableSkill();
        EnableItem();
        Debug.Log("✅ 스킬 조작 허용");
    }

    public void LogControlStatus()
    {
        Debug.Log($"스킬: {canUseSkill}");
        Debug.Log($"아이템: {canUseItem}");
    }
    #endregion
}
