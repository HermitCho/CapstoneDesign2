using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime; // Player 클래스를 위해 추가

/// <summary>
/// 생명체의 기본 기능을 담당하는 클래스 (포톤 멀티플레이 고려)
/// 체력, 방어막, 데미지 처리, 사망 처리 등을 관리
/// </summary>
public class LivingEntity : MonoBehaviourPunCallbacks, IDamageable, IPunObservable // ✅ IPunObservable 추가
{
    // photonView는 MonoBehaviourPunCallbacks가 가지고 있습니다.
    // private PhotonView photonView; // ❌ 중복 선언이므로 제거 (this.photonView 사용)

    [Header("Character Data")]
    [SerializeField] private CharacterData characterData;
    // Health & Shield Properties
    // ✅ [PunRPC]를 통해 동기화될 public 변수이므로 set을 private으로 변경하지 않고,
    // OnPhotonSerializeView에서 동기화하거나 RPC로 변경하는 로직을 구현합니다.
    public float StartingHealth { get; private set; }
    public float StartingShield { get; private set; }
    public float CurrentHealth { get; private set; }
    public float CurrentShield { get; private set; }
    public bool IsDead { get; private set; }
    
    // ✅ LivingEntity의 체력 변화를 알리는 static 이벤트. GameManager가 구독합니다.
    public static event Action<float, float, LivingEntity> OnAnyLivingEntityHealthChanged;
    
    // ✅ 플레이어 사망을 알리는 static 이벤트. TestTeddyBear 등이 구독할 수 있습니다.
    public static event Action<LivingEntity> OnPlayerDied;


    [Header("스턴 제어")]
    private MoveController moveController;

    // Events
    public event Action OnDeath; // ✅ 각 인스턴스별 사망 이벤트

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        // photonView = GetComponent<PhotonView>(); // this.photonView를 사용하면 됩니다.
    }

    /// <summary>
    /// 오브젝트가 활성화될 때 호출되는 메서드
    /// 초기 상태를 설정합니다.
    /// </summary>
    protected virtual void OnEnable()
    {
        InitializeEntity();
    }



    #endregion

    #region Initialization

    /// <summary>
    /// 생명체의 초기 상태를 설정합니다.
    /// </summary>
    private void InitializeEntity()
    {
        IsDead = false;
        StartingHealth = characterData.startingHealth;
        CurrentHealth = StartingHealth;
        StartingShield = characterData.startingShield;
        CurrentShield = StartingShield;
        moveController = GetComponent<MoveController>();
    }

    #endregion

    #region Damage System

    /// <summary>
    /// 데미지를 입었을 때 호출되는 메서드.
    /// 로컬에서 호출되지만, 실제 데미지 적용은 RPC를 통해 소유자/마스터 클라이언트에서 처리됩니다.
    /// </summary>
    public virtual void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        Debug.Log($"[LivingEntity] OnDamage 호출됨! 대상: {gameObject.name}, 데미지: {damage}");

        // 데미지 요청은 항상 RPC를 통해 소유자에게 전달합니다.
        // 소유자가 아닌 클라이언트가 데미지를 줬을 경우, 피격당한 LivingEntity의 소유자에게 RPC를 보냅니다.
        // 또는 마스터 클라이언트가 모든 데미지 계산을 처리하게 할 수도 있습니다.
        // 여기서는 피격당한 LivingEntity의 소유자에게 데미지 적용을 요청합니다.
        
        // 사망한 상태이면 데미지 무시
        if (IsDead)
        {
            Debug.Log($"[LivingEntity] {gameObject.name}은(는) 이미 사망 상태이므로 데미지 무시.");
            return;
        }

        // ✅ RPC를 통해 데미지 적용 요청 (소유자 또는 마스터 클라이언트에서만 실제 처리)
        // RPC_ApplyDamage는 해당 LivingEntity의 PhotonView를 가진 클라이언트에서 호출될 것입니다.
        // 피격당한 오브젝트의 PhotonView를 통해 RPC를 호출합니다.
        photonView.RPC("RPC_ApplyDamage", RpcTarget.MasterClient, damage); // ✅ RpcTarget.MasterClient로 변경

        Debug.Log($"[LivingEntity] {gameObject.name}의 RPC_ApplyDamage 요청 (MasterClient 대상)");
    }

    /// <summary>
    /// 네트워크를 통해 데미지를 실제로 적용하는 RPC 메서드.
    /// MasterClient에서만 실행되도록 Target을 지정합니다.
    /// </summary>
    /// <param name="damage">적용할 데미지 양</param>
    [PunRPC]
    public void RPC_ApplyDamage(float damage)
    {
        // ✅ 이 RPC는 RpcTarget.MasterClient로 설정되었으므로, 마스터 클라이언트에서만 실행됩니다.
        // 마스터 클라이언트만 데미지 계산을 수행하고, 결과(CurrentHealth, IsDead)는 IPunObservable을 통해 모든 클라이언트에 동기화됩니다.
        
        if (IsDead) // ✅ IsDead 변수 사용
        {
            Debug.Log($"[LivingEntity:Master] {gameObject.name}은(는) 이미 사망 상태이므로 데미지 무시.");
            return;
        }

        float previousHealth = CurrentHealth; // UI 업데이트를 위한 이전 체력 저장

        // 방어막 처리 로직 (현재는 체력만 있으므로 생략, 필요시 여기에 구현)
        // CurrentShield = Mathf.Max(0f, CurrentShield - damage);
        // if (CurrentShield <= 0) { damage -= StartingShield; } // 방어막이 0이 되면 남은 데미지를 체력에 적용

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage); // ✅ 체력 감소
        
        Debug.Log($"[LivingEntity:Master] {gameObject.name} 데미지 적용: {damage}, 남은 체력: {CurrentHealth}");

        // 체력 변경 이벤트를 발생시킵니다.
        // 이 이벤트는 마스터 클라이언트에서 체력 변경 시 모든 리스너에게 알립니다.
        // GameManager는 이 이벤트를 받아 로컬 플레이어인지 확인하여 UI를 업데이트합니다.
        OnAnyLivingEntityHealthChanged?.Invoke(CurrentHealth, StartingHealth, this); // ✅ StartingHealth 전달

        if (CurrentHealth <= 0f)
        {
            // 사망 처리 RPC 호출은 마스터 클라이언트에서만 결정하여 모든 클라이언트에 알립니다.
            photonView.RPC("RPC_Die", RpcTarget.All);
        }
    }

    // ProcessDamage, LogDamageInfo, CheckDeathCondition은 이제 RPC_ApplyDamage 내에 통합되거나 필요 없어집니다.
    // ❌ ProcessDamage, LogDamageInfo, CheckDeathCondition 메서드는 삭제 또는 주석 처리.
    // private void LogDamageInfo(float damage) { ... }
    // private void ProcessDamage(float damage) { ... }
    // private void CheckDeathCondition() { ... }

    #endregion

    #region Health Management

    /// <summary>
    /// 체력을 회복하는 메서드. 이 메서드도 네트워크를 통해 호출되어야 합니다.
    /// </summary>
    /// <param name="healAmount">회복량</param>
    public virtual void RestoreHealth(float healAmount)
    {
        // ✅ 회복 요청도 MasterClient에서 처리하도록 RPC 호출
        if (healAmount <= 0f) return;
        if (IsDead) return;

        photonView.RPC("RPC_RestoreHealth", RpcTarget.MasterClient, healAmount);
    }

    [PunRPC] // ✅ RPC 메서드로 추가
    public void RPC_RestoreHealth(float healAmount)
    {
        if (IsDead || healAmount <= 0f) return; // ✅ IsDead 변수 사용

        float prevHealth = CurrentHealth;
        CurrentHealth = Mathf.Min(StartingHealth, CurrentHealth + healAmount);
        // float actualHealed = CurrentHealth - prevHealth; // 실제 회복량은 필요시 사용

        Debug.Log($"[LivingEntity:Master] {gameObject.name} 체력 회복: {healAmount}, 현재 체력: {CurrentHealth}");
        
        // 체력 변경 이벤트를 발생시켜 GameManager가 UI를 업데이트하도록 합니다.
        OnAnyLivingEntityHealthChanged?.Invoke(CurrentHealth, StartingHealth, this);
    }


    public virtual void RestoreShield(float shieldAmount)
    {
        // Shield 로직도 마찬가지로 RPC 또는 IPunObservable로 동기화 고려
        if (shieldAmount <= 0f) return;
        if (IsDead) return;

        photonView.RPC("RPC_RestoreShield", RpcTarget.MasterClient, shieldAmount);
    }

    [PunRPC] // ✅ RPC 메서드로 추가
    public void RPC_RestoreShield(float shieldAmount)
    {
        if (IsDead || shieldAmount <= 0f) return; // ✅ IsDead 변수 사용
        CurrentShield = Mathf.Min(StartingShield, CurrentShield + shieldAmount);
        Debug.Log($"[LivingEntity:Master] {gameObject.name} 방어막 회복: {shieldAmount}, 현재 방어막: {CurrentShield}");
        // 방어막 UI 업데이트가 필요하다면 이벤트를 발생시킬 수 있습니다.
        // OnAnyLivingEntityShieldChanged?.Invoke(CurrentShield, StartingShield, this);
    }

    #endregion

    #region Death System

    /// <summary>
    /// 사망 처리 메서드. 모든 클라이언트에서 호출되어야 합니다.
    /// </summary>
    /// <returns>사망 처리 성공 여부</returns>
    [PunRPC] // ✅ RPC로 선언하여 네트워크를 통해 호출 가능하게 합니다.
    public virtual bool RPC_Die() // 메서드 이름 변경: Die -> RPC_Die
    {
        if (IsDead) return false;

        IsDead = true;
        OnDeath?.Invoke(); // 이벤트는 각 클라이언트에서 개별적으로 발생

        // MoveController는 로컬에서만 제어해도 무방합니다. (stunned 상태가 물리적인 움직임에만 영향)
        if (moveController != null)
        {
            moveController.SetStunned(true);
        }
        
        // ✅ 플레이어 사망 이벤트 발생 (TestTeddyBear 등이 반응할 수 있도록)
        OnPlayerDied?.Invoke(this);
        
        // 플레이어가 죽었을 때 GameManager에 알릴 수 있습니다 (예: 게임 오버 처리)
        // if (photonView.IsMine && GameManager.Instance != null)
        // {
        //     GameManager.Instance.NotifyPlayerDied(); // GameManager에 필요한 메서드를 추가해야 합니다.
        // }

        // 부활 코루틴은 로컬에서만 시작하거나, 부활 로직을 서버(마스터 클라이언트)에서 관리해야 합니다.
        // 여기서는 각 클라이언트에서 부활 타이머를 시작합니다.
        StartCoroutine(ReviveAfterDelay(10f));

        if (photonView.IsMine) // 소유자 클라이언트에게만 메시지
            Debug.Log($"[LivingEntity] {gameObject.name} 사망! - 플레이어 사망 이벤트 발생");

        return true;
    }

    // ✅ 기존 Die() 메서드는 RPC_Die()로 대체되었습니다. 아래 메서드는 이제 필요 없습니다.
    // private void Die() { ... } // ❌ 삭제

    private IEnumerator ReviveAfterDelay(float delay)
    {
        float remaining = delay;
        while (remaining > 0f)
        {
            // ✅ 멀티플레이 수정: 부활 대기 메시지는 자신의 클라이언트에서만 보이도록 합니다.
            if (photonView.IsMine)
            {
                Debug.Log($"[부활 대기] 남은 시간: {Mathf.CeilToInt(remaining)}초");
            }
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }

        // 부활 로직도 RPC를 통해 모든 클라이언트에서 실행되어야 합니다.
        // 부활 결정은 소유자가 하거나 마스터 클라이언트가 합니다.
        if (photonView.IsMine)
        {
            photonView.RPC("RPC_Revive", RpcTarget.All); // ✅ RpcTarget.All로 부활 RPC 호출
        }
    }

    [PunRPC] // ✅ RPC 메서드로 추가
    public void RPC_Revive()
    {
        if (!IsDead) return; // ✅ IsDead 변수 사용

        IsDead = false;
        InitializeEntity(); // 체력 및 상태 초기화

        if (moveController != null)
        {
            moveController.SetStunned(false); // 스턴 해제
        }

        if (photonView.IsMine)
        {
            Debug.Log($"[LivingEntity] {gameObject.name} 부활!");
            // 부활 시 UI 업데이트 (GameManager에 알림)
            OnAnyLivingEntityHealthChanged?.Invoke(CurrentHealth, StartingHealth, this);
        }
    }

    #endregion

    #region Utility Methods
    // ... (기존 Utility Methods는 변경 없음)
    #endregion

    #region IPunObservable 구현

    /// <summary>
    /// 네트워크 데이터 스트림을 통해 변수를 동기화합니다.
    /// 이 메서드는 PhotonView의 Observed Components 목록에 이 스크립트가 추가되어 있을 때 호출됩니다.
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 마스터 클라이언트 또는 소유자(IsMine)가 쓰기 권한을 가집니다.
            // 데미지 계산은 마스터 클라이언트에서 수행하고, 그 결과를 여기서 동기화합니다.
            stream.SendNext(CurrentHealth);
            stream.SendNext(IsDead);
            // 필요하다면 실드도 동기화
            // stream.SendNext(CurrentShield);
        }
        else
        {
            // 다른 클라이언트들은 수신하여 업데이트합니다.
            this.CurrentHealth = (float)stream.ReceiveNext();
            this.IsDead = (bool)stream.ReceiveNext();
            // this.CurrentShield = (float)stream.ReceiveNext();

            // ✅ IPunObservable로 체력이 동기화될 때, 로컬 UI도 업데이트되도록 이벤트 발생
            // 이 부분이 중요합니다! 다른 클라이언트에서 체력이 변경될 때도 UI가 동기화됩니다.
            // 하지만 GameManager는 IsMine인지 다시 확인하여 로컬 플레이어만 UI 업데이트를 합니다.
            OnAnyLivingEntityHealthChanged?.Invoke(this.CurrentHealth, this.StartingHealth, this);
        }
    }

    #endregion
}