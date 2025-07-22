using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Photon.Pun;

public class GameManager : Singleton<GameManager>
{
    #region 자동할당 변수

    // 플레이어 관리
    private LivingEntity localPlayerLivingEntity;

    // 테디베어 점수 관리
    private float totalTeddyBearScore = 0f;
    private TestTeddyBear currentTeddyBear;

    // 게임 시간 관리
    private float gameStartTime = 0f;
    private bool useGameManagerTime = true; // GameManager에서 시간 관리 여부

    // 플레이어 상태 관리
    private float playerHealth = 100f;
    private float maxPlayerHealth = 100f;

    // 코인 컨트롤러 관리
    private CoinController currentPlayerCoinController;

    #endregion





    #region 캐싱 변수

    private float cachedScoreIncreaseTime = 20f; // 기본값
    private float cachedScoreIncreaseRate = 2f; // 기본
    private float cachedPlayTime = 360f; // 기본
    private bool dataBaseCached = false;

    #endregion





    #region 이벤트
    // ✅ UI 시스템을 위한 이벤트들
    // 점수 관련 이벤트
    public static event Action<float> OnScoreUpdated;
    public static event Action<float> OnScoreMultiplierUpdated;
    public static event Action<float> OnGameTimeUpdated;

    // 테디베어 관련 이벤트
    public static event Action<bool> OnTeddyBearAttachmentChanged;
    public static event Action<float> OnTeddyBearReattachTimeChanged;

    // 플레이어 상태 이벤트
    public static event Action<float, float> OnPlayerHealthChanged; // current, max

    // 아이템 UI 이벤트
    public static event Action<bool> OnItemUIToggled;

    // 크로스헤어 이벤트
    public static event Action<bool> OnCrosshairTargetingChanged;

    // 스킬 이벤트 (구현 예정)
    public static event Action OnSkillUsed;
    public static event Action<int, float> OnSkillCooldownStarted;

    public static event Action OnCharacterSpawned;

    #endregion






    #region 생명주기

    void Start()
    {
        // 게임 시작 시간 기록
        gameStartTime = Time.time;

        // DataBase 정보 캐싱
        CacheDataBaseInfo();

        // 테디베어 찾기
        FindTeddyBear();
    }

    void OnDestroy() // ✅ 수정: 오류 해결
    {
        LivingEntity.OnAnyLivingEntityHealthChanged -= HandleAnyLivingEntityHealthChanged;

        Debug.Log("❌ GameManager - OnDestroy: 이벤트 구독 해제");
    }


    #endregion





    #region 데이터 받아오기 메서드
    // DataBase 정보 캐싱 (안전한 접근)
    void CacheDataBaseInfo()
    {
        try
        {
            if (DataBase.Instance != null && DataBase.Instance.teddyBearData != null && DataBase.Instance.gameData != null)
            {
                cachedScoreIncreaseTime = DataBase.Instance.teddyBearData.ScoreIncreaseTime;
                cachedScoreIncreaseRate = DataBase.Instance.teddyBearData.ScoreIncreaseRate;
                cachedPlayTime = DataBase.Instance.gameData.PlayTime;
                dataBaseCached = true;
                Debug.Log($"✅ DataBase 정보 캐싱 완료 - Time: {cachedScoreIncreaseTime}, Rate: {cachedScoreIncreaseRate}");
            }
            else
            {
                Debug.LogWarning("⚠️ DataBase 접근 실패 - 기본값 사용");
                dataBaseCached = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ DataBase 캐싱 중 오류: {e.Message} - 기본값 사용");
            dataBaseCached = false;
        }
    }

    #endregion





    #region 캐싱 데이터 받아오기 메서드

    /// <summary>
    /// 점수 증가 시간 가져오기 (캐싱된 값 사용)
    /// </summary>
    public float GetScoreIncreaseTime()
    {
        if (dataBaseCached)
        {
            return cachedScoreIncreaseTime;
        }

        // 캐싱되지 않았다면 재시도
        CacheDataBaseInfo();
        return cachedScoreIncreaseTime;
    }

    /// <summary>
    /// 점수 증가 배율 가져오기 (캐싱된 값 사용)
    /// </summary>
    public float GetScoreIncreaseRate()
    {
        if (dataBaseCached)
        {
            return cachedScoreIncreaseRate;
        }

        // 캐싱되지 않았다면 재시도
        CacheDataBaseInfo();
        return cachedScoreIncreaseRate;
    }

    /// <summary>
    /// DataBase가 성공적으로 캐싱되었는지 확인
    /// </summary>
    public bool IsDataBaseCached()
    {
        return dataBaseCached;
    }

    /// <summary>
    /// DataBase 정보 강제 새로고침
    /// </summary>
    public void RefreshDataBaseCache()
    {
        CacheDataBaseInfo();
    }

    #endregion





    #region 테디베어 관련 메서드

    // 테디베어 점수 업데이트 (TestTeddyBear에서 호출)
    public void UpdateTeddyBearScore(float newScore)
    {
        totalTeddyBearScore = newScore;

        // HeatUI에 점수 업데이트 이벤트 발생
        OnScoreUpdated?.Invoke(totalTeddyBearScore);

        // 점수 배율도 실시간 계산으로 업데이트
        float currentMultiplier = GetScoreMultiplier();
        OnScoreMultiplierUpdated?.Invoke(currentMultiplier);
    }

    // 현재 테디베어 점수 가져오기
    public float GetTeddyBearScore()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.GetCurrentScore();
        }
        return totalTeddyBearScore;
    }

    // 테디베어가 부착되어 있는지 확인
    public bool IsTeddyBearAttached()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.IsAttached();
        }
        return false;
    }

    // 테디베어 재부착까지 남은 시간 가져오기
    public float GetTimeUntilReattach()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.GetTimeUntilReattach();
        }
        return 0f;
    }

    // 테디베어 재부착 가능 여부 확인
    public bool CanTeddyBearReattach()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.CanReattach();
        }
        return true;
    }

    #endregion





    #region 점수 관련 메서드

    // 점수 초기화 (개발자용)
    public void ResetAllScores()
    {
        totalTeddyBearScore = 0f;
        if (currentTeddyBear != null)
        {
            currentTeddyBear.ResetScore();
        }
        OnScoreUpdated?.Invoke(0f);
        OnScoreMultiplierUpdated?.Invoke(1f);
    }

    public float GetPlayTime()
    {
        return cachedPlayTime;
    }

    // 현재 점수 배율 가져오기 (실시간 계산)
    public float GetScoreMultiplier()
    {
        // 실시간 게임 시간 기반으로 배율 계산
        float currentGameTime = GetGameTime();
        float scoreIncreaseTime = GetScoreIncreaseTime();

        float multiplier;
        if (currentGameTime >= scoreIncreaseTime)
        {
            // 점수 증가 시점 이후: 캐싱된 배율 사용
            multiplier = cachedScoreIncreaseRate;
        }
        else
        {
            // 점수 증가 시점 이전: 기본 배율 1.0
            multiplier = 1f;
        }
        return multiplier;
    }

    // 게임 시간 가져오기
    public float GetGameTime()
    {
        if (useGameManagerTime)
        {
            // GameManager에서 관리하는 게임 시간 사용
            return Time.time - gameStartTime;
        }
        else
        {
            // 기존 방식: 테디베어에서 시간 가져오기
            if (currentTeddyBear != null)
            {
                return currentTeddyBear.GetGameTime();
            }
            return Time.time - gameStartTime;
        }
    }

    #endregion





    #region 플레이어 체력 관리 메서드

    /// <summary>
    /// 플레이어 체력 설정
    /// </summary>
    public void SetPlayerHealth(float current, float max)
    {
        playerHealth = Mathf.Clamp(current, 0f, max);
        maxPlayerHealth = Mathf.Max(max, 1f);

        OnPlayerHealthChanged?.Invoke(playerHealth, maxPlayerHealth);

    }

    /// <summary>
    /// 플레이어 현재 체력만 변경
    /// </summary>
    public void SetPlayerCurrentHealth(float health)
    {
        SetPlayerHealth(health, maxPlayerHealth);
    }

    /// <summary>
    /// 플레이어 체력 변경 (데미지/힐)
    /// </summary>
    public void ChangePlayerHealth(float amount)
    {
        Debug.Log(amount);
        SetPlayerHealth(playerHealth + amount, maxPlayerHealth);
    }

    /// <summary>
    /// 플레이어 체력 정보 가져오기
    /// </summary>
    public float GetPlayerHealth() => playerHealth;
    public float GetMaxPlayerHealth() => maxPlayerHealth;
    public float GetPlayerHealthRatio() => playerHealth / maxPlayerHealth;

    #endregion


    #region 이벤트 발생 메서드들

    /// <summary>
    /// 테디베어 부착 상태 변경 알림
    /// </summary>
    public void NotifyTeddyBearAttachmentChanged(bool isAttached)
    {
        OnTeddyBearAttachmentChanged?.Invoke(isAttached);

    }

    /// <summary>
    /// 테디베어 재부착 시간 변경 알림
    /// </summary>
    public void NotifyTeddyBearReattachTime(float timeRemaining)
    {
        OnTeddyBearReattachTimeChanged?.Invoke(timeRemaining);
    }

    /// <summary>
    /// 게임 시간 업데이트 알림
    /// </summary>
    public void NotifyGameTimeUpdated(float gameTime)
    {
        OnGameTimeUpdated?.Invoke(gameTime);
    }

    /// <summary>
    /// 점수 배율 업데이트 알림 (외부에서 호출용)
    /// </summary>
    public void NotifyScoreMultiplierUpdated()
    {
        float currentMultiplier = GetScoreMultiplier();
        OnScoreMultiplierUpdated?.Invoke(currentMultiplier);
    }

    /// <summary>
    /// 아이템 UI 토글 알림
    /// </summary>
    public void NotifyItemUIToggled(bool isOpen)
    {
        OnItemUIToggled?.Invoke(isOpen);

    }

    /// <summary>
    /// 크로스헤어 타겟팅 상태 변경 알림
    /// </summary>
    public void NotifyCrosshairTargeting(bool isTargeting)
    {
        OnCrosshairTargetingChanged?.Invoke(isTargeting);
    }

    /// <summary>
    /// 스킬 사용 알림
    /// </summary>
    public void NotifySkillUsed()
    {
        OnSkillUsed?.Invoke();

    }

    /// <summary>
    /// 스킬 쿨다운 시작 알림
    /// </summary>
    public void NotifySkillCooldownStarted(int skillIndex, float cooldownTime)
    {
        OnSkillCooldownStarted?.Invoke(skillIndex, cooldownTime);
    }

    /// <summary>
    /// 캐릭터 스폰 이벤트 알림
    /// </summary>
    public void NotifyCharacterSpawned()
    {
        OnCharacterSpawned?.Invoke();
    }


    #endregion




    #region 컴포넌트 찾기 메서드

    // 테디베어 찾기
    void FindTeddyBear()
    {
        if (currentTeddyBear == null)
        {
            currentTeddyBear = FindObjectOfType<TestTeddyBear>();
            if (currentTeddyBear != null)
            {
                Debug.Log("테디베어를 찾았습니다!");
            }
        }
    }

    /// 스폰 후 플레이어 찾기 (SpawnController에서 호출)
    public void FindPlayerAfterSpawn() // ✅ 기존 메서드명 유지 (내부 로직 변경)
    {
        try
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                PhotonView pv = playerObject.GetComponent<PhotonView>(); // ✅ PhotonView 가져오기
                // ✅ 로컬 플레이어의 오브젝트인지 확인하는 조건 추가
                if (pv != null && pv.IsMine)
                {
                    // 기존에 구독되어 있었다면 해제 (중복 구독 방지)
                    if (localPlayerLivingEntity != null)
                    {
                        LivingEntity.OnAnyLivingEntityHealthChanged -= HandleAnyLivingEntityHealthChanged;
                    }

                    localPlayerLivingEntity = playerObject.GetComponent<LivingEntity>(); // ✅ localPlayerLivingEntity에 할당
                    if (localPlayerLivingEntity != null)
                    {
                        // ✅ 이곳에서 이벤트 구독: 로컬 플레이어의 LivingEntity가 확정되었을 때!
                        LivingEntity.OnAnyLivingEntityHealthChanged += HandleAnyLivingEntityHealthChanged;

                        // player = playerObject.GetComponent<LivingEntity>(); // ❌ 삭제: 더 이상 사용하지 않음
                        playerHealth = localPlayerLivingEntity.CurrentHealth; // ✅ localPlayerLivingEntity 사용
                        maxPlayerHealth = localPlayerLivingEntity.StartingHealth; // ✅ localPlayerLivingEntity 사용

                        // 플레이어의 CoinController 찾기
                        FindPlayerCoinController(playerObject);

                        Debug.Log($"✅ GameManager: 로컬 플레이어를 찾았고 이벤트 구독 완료 - {playerObject.name}");

                        // HUD에 스킬 데이터 업데이트 알림
                        NotifyHUDToUpdateSkillData();
                    }
                    else
                    {
                        Debug.LogError("❌ GameManager: 플레이어 오브젝트에 LivingEntity 컴포넌트가 없습니다!");
                    }
                }
                else
                {
                    Debug.Log($"⚠️ GameManager: 'Player' 태그를 가진 오브젝트를 찾았지만 로컬 플레이어가 아닙니다: {playerObject.name}");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ GameManager: 'Player' 태그를 가진 오브젝트를 찾을 수 없습니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ GameManager: 플레이어 찾기 중 오류 발생 - {e.Message}");
        }
    }


    // ✅ 추가: LivingEntity의 체력 변경 이벤트를 처리하는 핸들러
    private void HandleAnyLivingEntityHealthChanged(float current, float max, LivingEntity changedEntity)
    {
        // 변경된 LivingEntity가 로컬 플레이어의 LivingEntity와 동일한지 확인
        if (localPlayerLivingEntity != null && changedEntity == localPlayerLivingEntity)
        {
            // 로컬 플레이어의 체력이므로 HUD에 업데이트 이벤트를 발생시킵니다.
            playerHealth = current;
            maxPlayerHealth = max;
            OnPlayerHealthChanged?.Invoke(playerHealth, maxPlayerHealth);
            Debug.Log($"[GameManager] 로컬 플레이어 체력 업데이트: {playerHealth}/{maxPlayerHealth}");
        }
        else
        {
            // 로컬 플레이어의 체력 변화가 아니므로 HUD에 알리지 않습니다. (예: 적의 체력 변화)
            Debug.Log($"[GameManager] 비-로컬 LivingEntity 체력 변화 감지: {changedEntity?.gameObject.name} -> {current}/{max}");
        }
    }

    /// <summary>
    /// 플레이어의 CoinController 찾기
    /// </summary>
    /// <param name="playerObject">플레이어 오브젝트</param>
    private void FindPlayerCoinController(GameObject playerObject)
    {
        if (playerObject == null) return;

        // 플레이어 오브젝트에서 CoinController 찾기
        currentPlayerCoinController = playerObject.GetComponent<CoinController>();

        // 직접 찾지 못한 경우 자식 오브젝트에서 찾기
        if (currentPlayerCoinController == null)
        {
            currentPlayerCoinController = playerObject.GetComponentInChildren<CoinController>();
        }

        if (currentPlayerCoinController != null)
        {
            Debug.Log($"✅ GameManager: 플레이어의 CoinController를 찾았습니다 - {currentPlayerCoinController.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ GameManager: 플레이어에서 CoinController를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 현재 플레이어의 CoinController 가져오기
    /// </summary>
    /// <returns>현재 플레이어의 CoinController</returns>
    public CoinController GetCurrentPlayerCoinController()
    {
        return currentPlayerCoinController;
    }

    /// <summary>
    /// 플레이어의 CoinController 설정 (외부에서 호출)
    /// </summary>
    /// <param name="coinController">설정할 CoinController</param>
    public void SetCurrentPlayerCoinController(CoinController coinController)
    {
        currentPlayerCoinController = coinController;
        if (coinController != null)
        {
            Debug.Log($"✅ GameManager: CoinController가 설정되었습니다 - {coinController.name}");
        }
    }


    #endregion


    #region HUD 업데이트 메서드

    /// <summary>
    /// HUD에 스킬 데이터 업데이트 알림
    /// </summary>
    private void NotifyHUDToUpdateSkillData()
    {
        HUDPanel hudPanel = FindObjectOfType<HUDPanel>();

        if (hudPanel != null)
        {
            hudPanel.UpdateSkillDataFromSpawnedCharacter();
            Debug.Log("HUDPanel을 찾았습니다!");
        }
        else
        {
            Debug.LogWarning("⚠️ GameManager: HUDPanel을 찾을 수 없습니다.");
        }
    }

    #endregion

}
