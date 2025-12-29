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

    [Header("Hit Effect")]
    [SerializeField] private AudioClip hitSound;

    // Health 상태
    private float maxHealth;
    private float currentHealth;
    private bool isDead;

    // 이벤트
    public System.Action OnDeath;
    public System.Action OnRevive;
    public System.Action<float, float> OnHealthChanged; // current, max
    
    // ✅ 킬로그를 위한 static 사망 이벤트 (LivingEntity와 동일한 패턴)
    // 매개변수: victimAI, attackerID
    public static System.Action<AIHealth, int> OnAIDied;

    // AI 무적 상태
    private bool isInvincible;
    private float invincibilityEndTime;

    // 컴포넌트 캐시
    private PhotonView pv;

    // IDamageable의 속성 구현
    public int photonViewID
    {
        get => base.photonView.ViewID;
        set => Debug.Log("ViewID는 설정할 수 없습니다.");
    }

    // 반짝임 이펙트
    private Renderer[] renderers;
    private Color[] originalColors;
    private Color[] originalEmissionColors;
    private Coroutine hitFlashCoroutine;
    private Coroutine invincibilityFlashCoroutine;
    private float lastHitSoundTime;
    private const float HIT_SOUND_COOLDOWN = 1f;
    private const float EMISSION_INTENSITY = 10f;

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
        
        // ✅ CRITICAL DEBUG: 빌드 네트워크 문제 진단
        if (pv == null)
        {
            Debug.LogError($"[AIHealth - Awake] ❌ PhotonView를 찾을 수 없습니다! GameObject: {gameObject.name}");
        }
        else
        {
            Debug.Log($"[AIHealth - Awake] PhotonView 초기화 - GameObject: {gameObject.name}, ViewID: {pv.ViewID}, IsMine: {pv.IsMine}, OwnerActorNr: {pv.OwnerActorNr}");
        }
        
        if (characterData != null)
        {
            maxHealth = characterData.startingHealth;
            currentHealth = maxHealth;
        }

        // Renderer 초기화
        InitializeRenderers();
    }
    
    private void Start()
    {
        // ✅ CRITICAL DEBUG: 빌드에서 PhotonView가 제대로 초기화되었는지 확인
        if (pv != null)
        {
            Debug.Log($"[AIHealth - Start] PhotonView 상태 확인 - GameObject: {gameObject.name}, ViewID: {pv.ViewID}, IsMine: {pv.IsMine}, OwnerActorNr: {pv.OwnerActorNr}");
            Debug.Log($"[AIHealth - Start] 네트워크 상태 - IsConnected: {PhotonNetwork.IsConnected}, InRoom: {PhotonNetwork.InRoom}, IsMasterClient: {PhotonNetwork.IsMasterClient}");
            
            if (pv.ViewID == 0)
            {
                Debug.LogError($"[AIHealth - Start] ❌ PhotonView.ViewID가 0입니다! 빌드에서 RPC가 작동하지 않을 수 있습니다. GameObject: {gameObject.name}");
            }
        }
    }

    /// <summary>
    /// Renderer 컴포넌트들을 찾아서 원본 색상 및 Emission 색상을 저장합니다.
    /// </summary>
    private void InitializeRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        originalEmissionColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {

                Material mat = renderers[i].material;

                if (mat.HasProperty("_Color"))
                {
                    originalColors[i] = mat.color;
                }
                else
                {
                    originalColors[i] = Color.white;
                }

                // ✅ Emission 색상 저장
                if (mat.HasProperty("_EmissionColor"))
                {
                    originalEmissionColors[i] = mat.GetColor("_EmissionColor");
                }
                else
                {
                    originalEmissionColors[i] = Color.black; // Emission이 없으면 검은색으로 간주
                }

                // ✅ 런타임에 Emission을 변경할 수 있도록 활성화
                if (mat.IsKeywordEnabled("_EMISSION"))
                {
                    mat.EnableKeyword("_EMISSION");
                }
            }
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
        // ✅ CRITICAL DEBUG: 빌드 네트워크 문제 진단
        Debug.Log($"[AIHealth - TakeDamage] ✅ 호출됨! GameObject: {gameObject.name}, Damage: {damage}, AttackerID: {attackerID}");
        Debug.Log($"[AIHealth - TakeDamage] PhotonView 상태 - ViewID: {pv.ViewID}, IsMine: {pv.IsMine}, OwnerActorNr: {pv.OwnerActorNr}");
        Debug.Log($"[AIHealth - TakeDamage] 네트워크 상태 - IsConnected: {PhotonNetwork.IsConnected}, InRoom: {PhotonNetwork.InRoom}, IsMasterClient: {PhotonNetwork.IsMasterClient}");
        Debug.Log($"[AIHealth - TakeDamage] 현재 상태 - IsDead: {isDead}, CurrentHealth: {currentHealth}, MaxHealth: {maxHealth}");
        
        // 마스터 클라이언트만 데미지 계산
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[AIHealth - TakeDamage] ⚠️ 마스터 클라이언트가 아님 - 무시 (현재 IsMasterClient: {PhotonNetwork.IsMasterClient})");
            return;
        }

        if (isDead)
        {
            Debug.Log($"[AIHealth - TakeDamage] 이미 죽음 - 무시");
            return;
        }

        if (currentHealth <= 0)
        {
            Debug.Log($"[AIHealth - TakeDamage] 체력이 0 이하 - 무시");
            return;
        }

        // 무적 상태 체크
        if (isInvincible && Time.time < invincibilityEndTime)
        {
            Debug.Log($"[AIHealth - TakeDamage] 무적 상태 - 무시");
            return;
        }

        // 데미지 적용
        float oldHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log($"[AIHealth - TakeDamage] 데미지 적용! {oldHealth} -> {currentHealth}");

        // 모든 클라이언트에 체력 동기화
        pv.RPC("RPC_SyncHealth", RpcTarget.All, currentHealth);

        // 피격 이펙트 (모든 클라이언트)
        pv.RPC("RPC_OnHitEffect", RpcTarget.All);

        // 사망 체크
        if (currentHealth <= 0 && !isDead)
        {
            // 킬 사운드 재생 (공격자가 플레이어인 경우에만)
            PhotonView attackerPV = PhotonView.Find(attackerID);
            if (attackerPV != null && attackerPV.Owner != null)
            {
                // 공격자가 플레이어(LivingEntity)인지 확인
                LivingEntity attackerLivingEntity = attackerPV.GetComponent<LivingEntity>();
                if (attackerLivingEntity != null)
                {
                    // 공격자의 LivingEntity에 RPC 전송 (공격자에게만)
                    attackerPV.RPC("RPC_PlayKillSound", attackerPV.Owner);
                }
            }
            
            pv.RPC("RPC_Die", RpcTarget.All, attackerID);
        }
    }

    /// <summary>
    /// LivingEntity와 호환을 위한 OnDamage RPC (ActorNr 사용)
    /// IDamageable 인터페이스 구현
    /// </summary>
    [PunRPC]
    public void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal, int attackerActorNr)
    {
        // ✅ CRITICAL DEBUG: 빌드 네트워크 문제 진단
        Debug.Log($"[AIHealth - OnDamage] ✅ RPC 수신 성공! GameObject: {gameObject.name}, Damage: {damage}, AttackerID: {attackerActorNr}");
        Debug.Log($"[AIHealth - OnDamage] PhotonView 상태 - ViewID: {pv.ViewID}, IsMine: {pv.IsMine}, OwnerActorNr: {pv.OwnerActorNr}");
        Debug.Log($"[AIHealth - OnDamage] 네트워크 상태 - IsConnected: {PhotonNetwork.IsConnected}, InRoom: {PhotonNetwork.InRoom}, IsMasterClient: {PhotonNetwork.IsMasterClient}");
        Debug.Log($"[AIHealth - OnDamage] 현재 상태 - IsDead: {isDead}, CurrentHealth: {currentHealth}, MaxHealth: {maxHealth}");
        
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
    private void RPC_Die(int attackerID)
    {
        if (isDead)
            return;

        isDead = true;

        // 반짝임 코루틴 중지 및 색상 복원
        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = null;
        }
        if (invincibilityFlashCoroutine != null)
        {
            StopCoroutine(invincibilityFlashCoroutine);
            invincibilityFlashCoroutine = null;
        }
        RestoreOriginalColors();

        OnDeath?.Invoke();
        
        // ✅ 킬로그를 위한 static 이벤트 발생 (모든 클라이언트에서 호출됨)
        // attackerID를 함께 전달하여 공격자 정보를 알 수 있도록 함
        OnAIDied?.Invoke(this, attackerID);

        // 사망 사운드 재생 (모든 클라이언트에서 캐릭터 위치에서)
        if (AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayClipAtPoint("SFX_Game_Death", transform.position);
            Debug.Log($"[AIHealth] 사망 사운드 재생 - 위치: {transform.position}");
        }

        // AI 사망 시 왕관 떨어뜨리기
        DropCrownIfAttached();

        // 공격자에게 킬 점수 부여 (마스터 클라이언트만)
        if (PhotonNetwork.IsMasterClient)
        {
            GrantKillScoreToAttacker(attackerID);
        }

        // 마스터 클라이언트만 부활 코루틴 시작
        if (PhotonNetwork.IsMasterClient && pv.IsMine)
        {
            StartCoroutine(ReviveCoroutine(5f));
        }
    }

    /// <summary>
    /// 공격자에게 킬 점수 부여
    /// </summary>
    private void GrantKillScoreToAttacker(int attackerID)
    {
        // ViewID로 공격자의 PhotonView 찾기
        PhotonView attackerPV = PhotonView.Find(attackerID);
        if (attackerPV != null)
        {
            // 공격자가 플레이어인지 확인 (LivingEntity가 있으면 플레이어)
            LivingEntity attackerEntity = attackerPV.GetComponent<LivingEntity>();
            if (attackerEntity != null && !attackerEntity.IsDead)
            {
                // CoinController 찾기
                CoinController coinController = attackerPV.GetComponent<CoinController>();
                if (coinController != null)
                {
                    float killScore = 20f;
                    attackerPV.RPC("RPC_GrantKillScore", attackerPV.Owner, killScore);
                    Debug.Log($"[AIHealth] AI {gameObject.name} 사망 → 플레이어 {attackerPV.name}에게 {killScore} 킬 점수 부여");
                }
            }
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

        // 반짝임 코루틴 중지 및 색상 복원
        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = null;
        }
        RestoreOriginalColors();

        // 부활 시 5초 무적 시작
        StartInvincibility(3f);

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

        // 무적 반짝임 시작 (모든 클라이언트)
        if (invincibilityFlashCoroutine != null)
        {
            StopCoroutine(invincibilityFlashCoroutine);
        }
        invincibilityFlashCoroutine = StartCoroutine(InvincibilityFlashCoroutine(duration));
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

    #region Visual Effects
    
    /// <summary>
    /// 킬 사운드 재생 (공격자에게만 호출됨)
    /// </summary>
    [PunRPC]
    private void RPC_PlayKillSound()
    {
        if (AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayOneShot("SFX_Game_Kill");
            Debug.Log("[AIHealth] 킬 사운드 재생");
        }
    }
    
    /// <summary>
    /// 피격 이펙트 (빨간색 반짝임 + 사운드)
    /// </summary>
    [PunRPC]
    private void RPC_OnHitEffect()
    {
        // 1. 빨간색 반짝임 이펙트 (기존 코드 유지)
        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = null;
        }
        hitFlashCoroutine = StartCoroutine(HitFlashOnceCoroutine());
    }

    /// <summary>
    /// 피격 시 1회만 빨간색으로 반짝거리는 코루틴
    /// </summary>
    private IEnumerator HitFlashOnceCoroutine()
    {
        if (renderers == null || renderers.Length == 0 || isDead) yield break;

        float flashDuration = 0.1f;
        Color flashColor = Color.red;
        Color flashEmissionColor = flashColor * EMISSION_INTENSITY; // ✅ Emission 색상 설정 (강도 적용)

        // 빨간색으로 변경
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                Material mat = renderers[i].material;
                if (mat.HasProperty("_Color"))
                {
                    mat.color = flashColor;
                }
                // ✅ Emission 색상 변경
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", flashEmissionColor);
                }
            }
        }

        yield return new WaitForSeconds(flashDuration);

        // 원본 색상으로 복원
        RestoreOriginalColors();

        hitFlashCoroutine = null;
    }

    /// <summary>
    /// 무적 상태일 때 하얀색으로 반짝거리는 코루틴
    /// </summary>
    private IEnumerator InvincibilityFlashCoroutine(float duration)
    {
        if (renderers == null || renderers.Length == 0) yield break;

        float flashDuration = 0.1f;
        Color flashColor = Color.white;
        Color flashEmissionColor = flashColor * EMISSION_INTENSITY * 0.5f; // ✅ 무적은 약간 약하게
        float endTime = Time.time + duration;

        while (Time.time < endTime && isInvincible && !isDead)
        {
            // 하얀색으로 변경 (일반 색상 + Emission)
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    Material mat = renderers[i].material;
                    if (mat.HasProperty("_Color"))
                    {
                        mat.color = flashColor;
                    }
                    // ✅ Emission 색상 변경
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", flashEmissionColor);
                    }
                }
            }

            yield return new WaitForSeconds(flashDuration);

            // 원본 색상으로 복원
            RestoreOriginalColors();

            yield return new WaitForSeconds(1f - flashDuration);
        }

        // 무적 해제
        isInvincible = false;
        Debug.Log("InvincibilityFlashCoroutine 무적해제");
        RestoreOriginalColors();
        invincibilityFlashCoroutine = null;
    }

    /// <summary>
    /// 모든 Renderer의 색상과 Emission을 원본 색상으로 복원
    /// </summary>
    private void RestoreOriginalColors()
    {
        if (renderers == null || originalColors == null || originalEmissionColors == null) return; // ✅ Null 체크

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                Material mat = renderers[i].material;
                if (mat.HasProperty("_Color"))
                {
                    mat.color = originalColors[i];
                }
                // ✅ Emission 색상 복원
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", originalEmissionColors[i]);
                    // Emission이 원래 검은색이면 비활성화하여 성능 최적화
                    if (originalEmissionColors[i] == Color.black)
                    {
                        mat.DisableKeyword("_EMISSION");
                    }
                }
            }
        }
    }

    #endregion
}

