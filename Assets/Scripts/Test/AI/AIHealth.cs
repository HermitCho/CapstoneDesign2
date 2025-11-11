using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// AI 전용 Health 시스템
/// - LivingEntity와 완전히 독립적
/// - MoveController 등 플레이어 전용 컴포넌트 의존성 제거
/// - CharacterData만 사용하여 스탯 공유
/// - 완벽한 Photon2 동기화
/// - IDamageable 구현으로 TestGun과 호환
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class AIHealth : MonoBehaviourPunCallbacks, IPunObservable, IDamageable
{
    [Header("Character Data")]
    [SerializeField] private CharacterData characterData;
    
    // Health 상태
    private float maxHealth;
    private float currentHealth;
    private bool isDead;
    
    // 이벤트
    public System.Action OnDeath;
    public System.Action OnRevive;
    public System.Action<float, float> OnHealthChanged; // current, max
    
    // AI 무적 상태
    private bool isInvincible;
    private float invincibilityEndTime;
    
    // 컴포넌트 캐시
    private PhotonView pv;
    
    #region Properties
    
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public CharacterData CharacterData => characterData;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        
        if (characterData != null)
        {
            maxHealth = characterData.startingHealth;
            currentHealth = maxHealth;
        }
    }
    
    private void OnEnable()
    {
        // 초기화
        if (characterData != null)
        {
            maxHealth = characterData.startingHealth;
            currentHealth = maxHealth;
        }
        isDead = false;
    }
    
    #endregion
    
    #region Damage System
    
    /// <summary>
    /// 데미지를 받는 RPC (모든 클라이언트에서 호출됨)
    /// - ViewID와 ActorNr 모두 지원
    /// </summary>
    [PunRPC]
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal, int attackerID)
    {
        // 마스터 클라이언트만 데미지 계산
        if (!PhotonNetwork.IsMasterClient)
            return;
            
        if (isDead)
            return;
            
        if (currentHealth <= 0)
            return;
        
        // 무적 상태 체크
        if (isInvincible && Time.time < invincibilityEndTime)
            return;
        
        // 데미지 적용
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        // 모든 클라이언트에 체력 동기화
        pv.RPC("RPC_SyncHealth", RpcTarget.All, currentHealth);
        
        // 사망 체크
        if (currentHealth <= 0 && !isDead)
        {
            pv.RPC("RPC_Die", RpcTarget.All);
        }
    }
    
    /// <summary>
    /// LivingEntity와 호환을 위한 OnDamage RPC (ActorNr 사용)
    /// IDamageable 인터페이스 구현
    /// </summary>
    [PunRPC]
    public void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal, int attackerActorNr)
    {
        Debug.Log($"[AIHealth] {gameObject.name} OnDamage 호출됨! Damage: {damage}, Attacker: {attackerActorNr}");
        // TakeDamage로 리다이렉트
        TakeDamage(damage, hitPoint, hitNormal, attackerActorNr);
    }
    
    [PunRPC]
    private void RPC_SyncHealth(float newHealth)
    {
        currentHealth = newHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    [PunRPC]
    private void RPC_Die()
    {
        if (isDead)
            return;
            
        isDead = true;
        OnDeath?.Invoke();
        
        // AI 사망 시 왕관 떨어뜨리기
        DropCrownIfAttached();
        
        // 마스터 클라이언트만 부활 코루틴 시작
        if (PhotonNetwork.IsMasterClient && pv.IsMine)
        {
            StartCoroutine(ReviveCoroutine(10f));
        }
    }
    
    private IEnumerator ReviveCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (PhotonNetwork.IsMasterClient && pv.IsMine)
        {
            pv.RPC("RPC_Revive", RpcTarget.All);
        }
    }
    
    [PunRPC]
    private void RPC_Revive()
    {
        isDead = false;
        currentHealth = maxHealth;
        
        // 부활 시 3초 무적 시작
        StartInvincibility(5f);
        
        OnRevive?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    /// <summary>
    /// 무적 상태 시작
    /// </summary>
    private void StartInvincibility(float duration)
    {
        isInvincible = true;
        invincibilityEndTime = Time.time + duration;
    }
    
    /// <summary>
    /// AI가 왕관을 가지고 있으면 떨어뜨리기
    /// </summary>
    private void DropCrownIfAttached()
    {
        Crown crown = FindObjectOfType<Crown>();
        if (crown != null && crown.IsAttachedToPlayer(transform))
        {
            crown.DetachFromPlayerOnDeath(); // 사망으로 인한 자동 분리
        }
    }
    
    #endregion
    
    #region Photon Callbacks
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 마스터가 상태 전송
            stream.SendNext(currentHealth);
            stream.SendNext(isDead);
        }
        else
        {
            // 클라이언트가 상태 수신
            currentHealth = (float)stream.ReceiveNext();
            isDead = (bool)stream.ReceiveNext();
        }
    }
    
    #endregion
    
    #region Public Methods
    
    public void SetCharacterData(CharacterData data)
    {
        characterData = data;
        if (characterData != null)
        {
            maxHealth = characterData.startingHealth;
            currentHealth = maxHealth;
        }
    }
    
    #endregion
}

