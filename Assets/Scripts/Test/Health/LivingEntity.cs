using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime; // Player 클래스를 위해 추가
using Febucci.UI;
using Michsky.UI.Heat;

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

    private LivingEntity currentAttacker;

    // Health & Shield Properties
    // ✅ [PunRPC]를 통해 동기화될 public 변수이므로 set을 private으로 변경하지 않고,
    // OnPhotonSerializeView에서 동기화하거나 RPC로 변경하는 로직을 구현합니다.
    public float StartingHealth { get; private set; }
    public CharacterData CharacterData { get; private set; }
    public float StartingShield { get; private set; }
    public float CurrentHealth { get; private set; }
    public float CurrentShield { get; private set; }
    public bool IsDead { get; private set; }

    // ✅ LivingEntity의 체력 변화를 알리는 static 이벤트. GameManager가 구독합니다.
    public static event Action<float, float, LivingEntity> OnAnyLivingEntityHealthChanged;

    // ✅ 플레이어 사망을 알리는 static 이벤트. TestTeddyBear 등이 구독할 수 있습니다.
    public static event Action<LivingEntity> OnPlayerDied;

    public event Action OnRevive;
    


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
        CharacterData = characterData;
        moveController = GetComponent<MoveController>();
    }

    #endregion

    #region Change Health

    /// <summary>
    /// 데미지를 입었을 때 호출되는 메서드.
    /// 로컬에서 호출되지만, 실제 데미지 적용은 RPC를 통해 소유자/마스터 클라이언트에서 처리됩니다.
    /// </summary>
    /// 
    [PunRPC]
    public void UpdateHealth(float newHealth, bool newDead)
    {
        CurrentHealth = newHealth;
        IsDead = newDead;
        OnAnyLivingEntityHealthChanged?.Invoke(CurrentHealth, StartingShield, this);
    }

    [PunRPC]
    public virtual void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal, int attackerViewId)
    {
        // 이미 사망한 상태라면 데미지 처리하지 않음
        if (IsDead) return;

        // ViewID를 통해 attacker LivingEntity 찾기
        PhotonView attackerPV = PhotonView.Find(attackerViewId);
        LivingEntity attacker = attackerPV?.GetComponent<LivingEntity>();

        // 체력이 0 이하라면 이미 사망한 상태
        if (CurrentHealth <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        Debug.Log($"[LivingEntity:Master] {gameObject.name} 데미지 적용: {damage}, 남은 체력: {CurrentHealth}");

        // 모든 클라이언트에게 체력 변경을 알리는 RPC 호출
        photonView.RPC("UpdateHealth", RpcTarget.AllViaServer, CurrentHealth, IsDead);
        
        if (CurrentHealth <= 0f && !IsDead)
        {
            currentAttacker = attacker;
            Debug.Log($"[LivingEntity] {gameObject.name} 사망 처리 시작 - attacker: {attacker?.name ?? "null"}");
            
            // 사망 처리 RPC 호출 (attacker도 ViewID로 전달)
            int attackerId = attacker != null ? attacker.photonView.ViewID : -1;

            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_Die", RpcTarget.AllViaServer, attackerId);
            }
        }
    }

    /// <summary>
    /// 체력을 회복하는 메서드. 이 메서드도 네트워크를 통해 호출되어야 합니다.
    /// </summary>
    /// <param name="healAmount">회복량</param>

    [PunRPC]
    public virtual void RestoreHealth(float healAmount)
    {
        if (IsDead || healAmount <= 0f) return; // ✅ IsDead 변수 사용

        float prevHealth = CurrentHealth;
        CurrentHealth = Mathf.Min(StartingHealth, CurrentHealth + healAmount);
        // float actualHealed = CurrentHealth - prevHealth; // 실제 회복량은 필요시 사용

        Debug.Log($"[LivingEntity:Master] {gameObject.name} 체력 회복: {healAmount}, 현재 체력: {CurrentHealth}");

        // 체력 변경 이벤트를 발생시켜 GameManager가 UI를 업데이트하도록 합니다.
        OnAnyLivingEntityHealthChanged?.Invoke(CurrentHealth, StartingHealth, this);
    }

    [PunRPC]
    public virtual void RestoreShield(float shieldAmount)
    {
        if (IsDead || shieldAmount <= 0f) return; // ✅ IsDead 변수 사용
        CurrentShield = Mathf.Min(StartingShield, CurrentShield + shieldAmount);
        Debug.Log($"[LivingEntity:Master] {gameObject.name} 방어막 회복: {shieldAmount}, 현재 방어막: {CurrentShield}");
    }

    #endregion

    #region Death System

    /// <summary>
    /// 사망 처리 메서드. 모든 클라이언트에서 호출되어야 합니다.
    /// </summary>
    /// <returns>사망 처리 성공 여부</returns>
    [PunRPC]
    public virtual bool RPC_Die(int attackerViewId)
    {
        // 이미 사망한 상태라면 처리하지 않음
        if (IsDead) return false;

        // ViewID를 통해 attacker LivingEntity 찾기
        PhotonView attackerPV = PhotonView.Find(attackerViewId);
        LivingEntity attacker = attackerPV?.GetComponent<LivingEntity>();
        
        // 사망 상태 설정
        IsDead = true;
        currentAttacker = attacker;
        
        Debug.Log($"[LivingEntity] {gameObject.name} 사망 처리 완료 - attacker: {currentAttacker?.name ?? "null"}");
        
        OnDeath?.Invoke(); // 이벤트는 각 클라이언트에서 개별적으로 발생

        // MoveController는 로컬에서만 제어해도 무방합니다. (stunned 상태가 물리적인 움직임에만 영향)
        if (moveController != null)
        {
            moveController.SetStunned(true);
        }

        // 플레이어 사망 이벤트 발생
        OnPlayerDied?.Invoke(this);

        // 부활 코루틴 시작
        StartCoroutine(ReviveAfterDelay(10f));

        Debug.Log($"[LivingEntity] {gameObject.name} 사망! - 플레이어 사망 이벤트 발생");

        return true;
    }

    private IEnumerator ReviveAfterDelay(float delay)
    {
        Debug.Log($"[LivingEntity] {gameObject.name} 부활 대기 시작 - {delay}초");
        
        yield return new WaitForSeconds(delay);
        
        Debug.Log($"[LivingEntity] {gameObject.name} 부활 대기 완료 - 부활 RPC 호출");
        
        // 부활 로직도 RPC를 통해 모든 클라이언트에서 실행되어야 합니다.
        // 부활 결정은 소유자가 하거나 마스터 클라이언트가 합니다.
        if (photonView.IsMine)
        {
            photonView.RPC("RPC_Revive", RpcTarget.All); // ✅ RpcTarget.All로 부활 RPC 호출
            Debug.Log($"[LivingEntity] {gameObject.name} 부활 RPC 호출 완료");
        }
        else
        {
a            Debug.Log($"[LivingEntity] {gameObject.name} 부활 RPC 호출 실패 - IsMine이 아님");
        }
    }

    [PunRPC] // ✅ RPC 메서드로 추가
    public void RPC_Revive()
    {
        Debug.Log($"[LivingEntity] {gameObject.name} RPC_Revive 호출됨 - IsDead: {IsDead}");
        
        if (!IsDead) 
        {
            Debug.Log($"[LivingEntity] {gameObject.name} 이미 살아있음 - 부활 취소");
            return;
        }
        
        Debug.Log($"[LivingEntity] {gameObject.name} 실제 부활 시작");
        
        // 사망 상태 해제
        IsDead = false;
        
        // 체력 및 상태 초기화
        InitializeEntity();

        if (moveController != null)
        {
            moveController.SetStunned(false); // 스턴 해제
        }

        // 모든 클라이언트에서 UI 업데이트
        OnAnyLivingEntityHealthChanged?.Invoke(CurrentHealth, StartingHealth, this);

        if (photonView.IsMine)
        {
            Debug.Log($"[LivingEntity] {gameObject.name} 부활 완료!");
            OnRevive?.Invoke();
        }
    }


    public LivingEntity GetAttacker()
    {
        return currentAttacker;
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
            bool wasDead = this.IsDead; // 이전 사망 상태 저장
            this.CurrentHealth = (float)stream.ReceiveNext();
            this.IsDead = (bool)stream.ReceiveNext();
            // this.CurrentShield = (float)stream.ReceiveNext();

            // ✅ IPunObservable로 체력이 동기화될 때, 로컬 UI도 업데이트되도록 이벤트 발생
            // 이 부분이 중요합니다! 다른 클라이언트에서 체력이 변경될 때도 UI가 동기화됩니다.
            // 하지만 GameManager는 IsMine인지 다시 확인하여 로컬 플레이어만 UI 업데이트를 합니다.
            OnAnyLivingEntityHealthChanged?.Invoke(this.CurrentHealth, this.StartingHealth, this);

            // ✅ 사망 상태가 변경된 경우에만 사망 이벤트 발생 (중복 방지)
            if (!wasDead && this.IsDead)
            {
                OnDeath?.Invoke();
                OnPlayerDied?.Invoke(this);

                // 로컬 플레이어 사망 시에만 손실 처리 (중복 방지)
                if (photonView.IsMine && GameManager.Instance != null)
                {
                    GameManager.Instance.HandlePlayerDeathPenalty();
                }
            }
        }
    }



    #endregion
}