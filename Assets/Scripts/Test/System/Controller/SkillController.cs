// SkillController.cs (리팩토링된 버전)
// 기존 코드 기반으로 최소한의 변경으로 "프리뷰 -> 좌클릭 확정" 흐름을 구현했습니다.
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
    private Skill skill;                // 플레이어 고유 스킬 (기존 방식 유지)
    private Skill activeItem;           // 현재 선택된 아이템(스킬 형태)
    private Skill[] Skills;
    private Skill[] Items;
    private Dictionary<int, Skill> skillDictionary = new Dictionary<int, Skill>();
    private Dictionary<int, Skill> itemDictionary = new Dictionary<int, Skill>();
    private PhotonView photonView;
    private bool dataBaseCached = false;

    // 기절/조작 제어 관련 변수들
    TestShoot testShoot;
    private bool canUseSkill = true;
    private bool canUseItem = true;
    private bool endSkillInProgress = false;

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
        if (!photonView.IsMine) return;
        InputManager.OnSkillPressed += OnSkillInput;
        InputManager.OnItemPressed += OnItemInput; // 아이템 사용 중앙 관리
        InputManager.OnChangeItemPressed += OnChangeItemInput;
    }

    void OnDisable()
    {
        if (!photonView.IsMine) return;
        InputManager.OnSkillPressed -= OnSkillInput;
        InputManager.OnItemPressed -= OnItemInput; // 아이템 사용 중앙 관리
        InputManager.OnChangeItemPressed -= OnChangeItemInput;
    }

    void Start()
    {
        testShoot = GetComponent<TestShoot>();
        skill = GetComponent<Skill>();
        CacheDataBaseInfo();
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // 프리뷰 업데이트 (위치/궤적)
        DeliverUpdatePreview();

        // 프리뷰가 활성화되어 있으면 좌클릭으로 확정
        if (isPreviewActive)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log("[SkillController - Update] 마우스 좌클릭 입력");
                ConfirmPreview();
            }
        }

        CheckDeleteItem();
    }
    #endregion

    #region 캐싱
    void CacheDataBaseInfo()
    {
        try
        {
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
                Skills = playerData.PlayerPrefabData.Select(prefab => prefab.transform.GetComponent<Skill>()).Where(s => s != null).ToArray();
                Items = itemData.ItemPrefabData.Select(prefab => prefab.transform.GetComponent<Skill>()).Where(i => i != null).ToArray();
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
            yield return new WaitForSeconds(0.1f);

            if (DataBase.Instance != null)
            {
                CacheDataBaseInfo();
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
        foreach (var s in Skills)
        {
            skillDictionary[s.Index] = s;
        }
        if (Items == null || Items.Count() == 0) return;
        foreach (var it in Items)
        {
            itemDictionary[it.Index] = it;
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

    #region 고유 스킬 관련 (입력 흐름 변경)
    // InputManager에서 스킬 입력 받기
    void OnSkillInput()
    {
        UseSkill();
    }

    /// <summary>
    /// 스킬 사용 흐름
    /// - HasPreview면 프리뷰 시작/토글(같은 키) / 다른 스킬이면 이전 프리뷰 취소 후 새 프리뷰 시작
    /// - 프리뷰가 없으면 즉시 Activate
    /// </summary>
    public void UseSkill()
    {
        if (skill == null) return;
        if (!CanUseSkill()) return;

        // 만약 다른 스킬의 프리뷰가 이미 활성 상태라면: 다른 스킬이면 이전 프리뷰 취소 후 새 프리뷰 시작
        if (skill.HasPreview)
        {
            if (isPreviewActive)
            {
                if (currentPreviewSkill == skill)
                {
                    // 같은 키를 다시 누르면 토글(취소)
                    DeliverEndPreview();
                }
                else
                {
                    // 다른 프리뷰가 켜져있고 지금 눌린 키가 다른 스킬이면 이전 취소 후 새 프리뷰 시작
                    DeliverEndPreview();
                    DeliverStartPreview(skill);
                }
            }
            else
            {
                TestShoot.SetIsShooting(false);
                // 프리뷰가 꺼져있으면 시작
                DeliverStartPreview(skill);
            }
        }
        else
        {
            TestShoot.SetIsShooting(true);
            // 즉발 스킬이면 바로 실행
            skill.ActivateSkill(this);
        }
    }

    /// <summary>
    /// RPC로 들어오는 실제 실행(기본: 자기 자신의 로직 실행)
    /// </summary>
    [PunRPC]
    public void ExecuteSkill(int skillIndex, Vector3 pos, Vector3 dir)
    {
        if (photonView.IsMine && skill != null && skill.Index == skillIndex)
        {
            skill.Execute(this, pos, dir);
        }

        PlaySkillEffectByIndex(skillIndex, pos, dir);
    }

    private void PlaySkillEffectByIndex(int skillIndex, Vector3 pos, Vector3 dir)
    {
        if (skillDictionary == null || skillDictionary.Count() == 0) return;
        if (!skillDictionary.TryGetValue(skillIndex, out Skill targetSkill))
        {
            Debug.LogWarning($"⚠️ ExecuteSkill - 스킬 인덱스 '{skillIndex}'을 찾을 수 없습니다.");
            return;
        }

        targetSkill.PlayEffectAtRemote(this, pos, dir);
    }

    [PunRPC]
    public void CastExecuteSkill(int skillIndex, Vector3 pos, Vector3 dir)
    {
        if (photonView.IsMine && skill != null && skill.Index == skillIndex)
        {
            skill.CastExecute(this, pos, dir);
        }

        PlaySkillCastEffectByIndex(skillIndex, pos, dir);
    }

    private void PlaySkillCastEffectByIndex(int skillIndex, Vector3 pos, Vector3 dir)
    {
        if (skillDictionary == null || skillDictionary.Count() == 0) return;
        if (!skillDictionary.TryGetValue(skillIndex, out Skill targetSkill))
        {
            Debug.LogWarning($"⚠️ CastExecuteSkill - 스킬 인덱스 '{skillIndex}'을 찾을 수 없습니다.");
            return;
        }

        targetSkill.PlayCastEffectAtRemote(this, pos, dir);
    }
    #endregion

    #region 아이템 사용 (동일한 프리뷰/확정 흐름)
    public void UseItem(ItemController itemController)
    {
        if (activeItem == null) return;
        if (!CanUseItem()) return;

        if (activeItem.HasPreview)
        {
            if (isPreviewActive && currentPreviewSkill != activeItem)
            {
                DeliverEndPreview();
                DeliverStartPreview(activeItem);
            }
            else
            {
                TestShoot.SetIsShooting(false);
                DeliverStartPreview(activeItem);
            }
        }
        else
        {
            // 즉시 아이템
            TestShoot.SetIsShooting(true);
            activeItem.ActivateItem(this);
        }
    }

    [PunRPC]
    public void ExecuteItem(int itemIndex, Vector3 pos, Vector3 dir)
    {
        if (photonView.IsMine && activeItem != null && activeItem.Index == itemIndex)
        {
            activeItem.Execute(this, pos, dir);
        }

        PlayItemEffectByIndex(itemIndex, pos, dir);
    }

    private void PlayItemEffectByIndex(int itemIndex, Vector3 pos, Vector3 dir)
    {
        if (itemDictionary == null || itemDictionary.Count() == 0) return;
        if (!itemDictionary.TryGetValue(itemIndex, out Skill targetItem))
        {
            Debug.LogWarning($"⚠️ ExecuteItem - 아이템 인덱스 '{itemIndex}'을 찾을 수 없습니다.");
            return;
        }

        targetItem.PlayEffectAtRemote(this, pos, dir);
    }

    [PunRPC]
    public void CastExecuteItem(int itemIndex, Vector3 pos, Vector3 dir)
    {
        if (photonView.IsMine && activeItem != null && activeItem.Index == itemIndex)
        {
            activeItem.CastExecute(this, pos, dir);
        }

        PlayItemCastEffectByIndex(itemIndex, pos, dir);
    }

    private void PlayItemCastEffectByIndex(int itemIndex, Vector3 pos, Vector3 dir)
    {
        if (itemDictionary == null || itemDictionary.Count() == 0) return;
        if (!itemDictionary.TryGetValue(itemIndex, out Skill targetItem))
        {
            Debug.LogWarning($"⚠️ CastExecuteItem - 아이템 인덱스 '{itemIndex}'을 찾을 수 없습니다.");
            return;
        }

        targetItem.PlayCastEffectAtRemote(this, pos, dir);
    }

    void OnItemInput()
    {
        ShopController shopController = GetComponent<ShopController>();
        if (shopController != null && shopController.IsShopOpen())
        {
            return;
        }

        ItemController itemController = FindCurrentPlayerItemController();
        if (itemController == null)
        {
            Debug.LogWarning("⚠️ MoveController - ItemController를 찾을 수 없습니다.");
            return;
        }

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

    private ItemController FindCurrentPlayerItemController()
    {
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

    private void CheckDeleteItem()
    {
        ItemController itemController = FindCurrentPlayerItemController();
        // 남은 횟수 0이면 아이템 제거 처리(원래 코드 재현)
        if(activeItem == null) return;
        
        if (activeItem.RemainingUses <= 0)
        {
            if (itemController != null && endSkillInProgress)
            {
                itemController.MoveUsedItemToTemp(activeItem.gameObject);
            }
            Destroy(activeItem.gameObject, activeItem.DestroyTime);
        }
    }
    #endregion



    #region 스킬 UI / 프리뷰 관리
    private void DeliverStartPreview(Skill s)
    {
        if (s == null) return;

        // 🔹 쿨타임 중이면 프리뷰 시작 안 함
        if (!s.CanUse)
        {
            Debug.LogWarning($"⚠️ {s.SkillName} 은(는) 쿨타임 중입니다!");
            // 여기서 UI 메시지 출력 함수 호출 가능 (예: UIManager.ShowMessage("스킬 쿨타임 중"))
            return;
        }

        currentPreviewSkill = s;
        Debug.Log($"[SkillController - StartPreview] currentPreviewSkill 변경{currentPreviewSkill}");
        isPreviewActive = true;
        s.StartPreview(this);

        Debug.Log($"✅ 프리뷰 시작: {s.SkillName}");
    }

    private void DeliverUpdatePreview()
    {
        if (!isPreviewActive || currentPreviewSkill == null) return;

        Vector3 direction = testShoot != null ? testShoot.CalculateShotDirection() : transform.forward;

        Vector3 origin = transform.position + transform.forward * 1.5f + transform.up * 1.5f;

        //Debug.Log($"[SkillController - UpdatePreview] currentPreviewSkill 변경{currentPreviewSkill}");
        currentPreviewSkill.UpdatePreview(this, origin, direction);
    }

    private void DeliverEndPreview()
    {
        if (currentPreviewSkill != null)
        {
            Debug.Log($"[SkillController - EndPreview] currentPreviewSkill 변경{currentPreviewSkill}");
            currentPreviewSkill.EndPreview(this);

        }

        TestShoot.SetIsShooting(true);
        currentPreviewSkill = null;
        Debug.Log($"[SkillController - EndPreview] currentPreviewSkill null로 변경{currentPreviewSkill}");
        isPreviewActive = false;

        Debug.Log("✅ 프리뷰 종료");
    }

    /// <summary>
    /// 좌클릭으로 프리뷰 확정 시 호출
    /// - 프리뷰 중이면 해당 스킬/아이템을 실제로 Activate 시킵니다.
    /// - 아이템의 경우 남은 횟수 0시 원래 하던 삭제 동작 재현
    /// </summary>
    private void ConfirmPreview()
    {
        if (!isPreviewActive || currentPreviewSkill == null) return;
        if (!photonView.IsMine) return;

        Debug.Log($"✅ 프리뷰 확정: {currentPreviewSkill.SkillName}");

        // 아이템 프리뷰인지 (activeItem 참조와 같은 객체인지) 확인
        if (currentPreviewSkill == activeItem)
        {
            // 아이템 실행
            currentPreviewSkill.ActivateItem(this);

            // 남은 횟수 0이면 아이템 제거 (ItemController가 있으면 MoveUsedItemToTemp 호출)
            if (currentPreviewSkill.RemainingUses <= 0)
            {
                var itemController = FindCurrentPlayerItemController();
                if (itemController != null)
                {
                    itemController.MoveUsedItemToTemp(currentPreviewSkill.gameObject);
                }
                Destroy(currentPreviewSkill.gameObject, currentPreviewSkill.DestroyTime);
            }
        }
        else
        {
            // 스킬 실행
            currentPreviewSkill.ActivateSkill(this);
        }

        DeliverEndPreview();
    }

    /// <summary>
    /// ESC 키 또는 외부에서 호출할 수 있는 프리뷰 취소
    /// </summary>
    public void CancelPreview()
    {
        if (isPreviewActive)
        {
            DeliverEndPreview();
        }
    }

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

    public void EnableSkill()
    {
        canUseSkill = true;
        Debug.Log("✅ 스킬 사용 허용");
    }

    public bool CanUseSkill()
    {
        return canUseSkill;
    }

    public void DisableItem()
    {
        canUseItem = false;
        Debug.Log("✅ 아이템 사용 차단");
    }

    public void EnableItem()
    {
        canUseItem = true;
        Debug.Log("✅ 아이템 사용 허용");
    }

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

    public void EndSkillInProgress()
    {
        endSkillInProgress = true;
    }
    #endregion
}
