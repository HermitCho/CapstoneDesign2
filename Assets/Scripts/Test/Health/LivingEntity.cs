using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime; // Player 클래스를 위해 추가
using Febucci.UI;
using Michsky.UI.Heat;
using System.Threading;
using Ricimi;

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
    private bool IsInvincivilityActive;
    private int IsInvincivilityCount;
    [SerializeField] AudioClip hitSound;

    // ✅ LivingEntity의 체력 변화를 알리는 static 이벤트. GameManager가 구독합니다.
    public static event Action<float, float, LivingEntity> OnAnyLivingEntityHealthChanged;

    // ✅ 플레이어 사망을 알리는 static 이벤트. TestTeddyBear 등이 구독할 수 있습니다.
    public static event Action<LivingEntity> OnPlayerDied;

    public event Action OnRevive;




    [Header("스턴 제어")]
    private MoveController moveController;

    [Header("카메라 컨트롤러")]
    CameraController cameraController;

    // Events
    public event Action OnDeath; // ✅ 각 인스턴스별 사망 이벤트

    [Header("반짝임 이펙트")]
    private Renderer[] renderers; // 모든 Renderer 컴포넌트
    private Color[] originalColors; // 원본 색상들
    private Color[] originalEmissionColors; // ✅ 원본 Emission 색상들
    private Coroutine hitFlashCoroutine; // 피격 반짝임 코루틴
    private Coroutine invincibilityFlashCoroutine; // 무적 반짝임 코루틴
    private float lastHitSoundTime; // 마지막 피격 사운드 재생 시간
    private const float HIT_SOUND_COOLDOWN = 0.1f; // 피격 사운드 쿨타임 (1초)
    private const float EMISSION_INTENSITY = 10f;

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        // photonView = GetComponent<PhotonView>(); // this.photonView를 사용하면 됩니다.

        // Renderer 컴포넌트들 초기화
        InitializeRenderers();
    }

    /// <summary>
    /// Renderer 컴포넌트들을 찾아서 원본 색상과 Emission 색상을 저장합니다.
    /// </summary>
    private void InitializeRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        originalEmissionColors = new Color[renderers.Length]; // ✅ 초기화

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                Material mat = renderers[i].material;

                // Material의 _Color 프로퍼티가 있으면 사용, 없으면 기본 색상
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
                mat.EnableKeyword("_EMISSION");
            }
        }
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
        IsInvincivilityActive = false;
        IsInvincivilityCount = 0;

        OnAnyLivingEntityHealthChanged?.Invoke(CurrentHealth, StartingHealth, this);
        cameraController = FindObjectOfType<CameraController>();
    }

    #endregion

    #region Change Health

    /// <summary>
    /// 데미지를 입었을 때 호출되는 메서드.
    /// 로컬에서 호출되지만, 실제 데미지 적용은 RPC를 통해 소유자/마스터 클라이언트에서 처리됩니다.
    /// </summary>
    /// 
    [PunRPC]
    public void RPC_UpdateHealth(float newHealth, bool newDead, int newInvincibilityCount)
    {
        CurrentHealth = newHealth;
        IsDead = newDead;

        // 🌟 무적 카운트 동기화 (RPC로 즉시 반영)
        IsInvincivilityCount = newInvincibilityCount;

        OnAnyLivingEntityHealthChanged?.Invoke(CurrentHealth, StartingHealth, this);
        // Debug.Log($"[LivingEntity] {gameObject.name} 체력/상태 동기화 - H:{CurrentHealth}, D:{IsDead}, IC:{IsInvincivilityCount}");
    }

    [PunRPC]
    public void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal, int attackerViewId)
    {
        // 🚨 데미지 계산 및 상태 변경은 마스터 클라이언트에서만 수행합니다.
        if (!PhotonNetwork.IsMasterClient) return;
        if (IsDead) return;
        if (CurrentHealth <= 0f) return;

        // 무적 검사 (무제한 무적)
        if (IsInvincivilityActive) return;

        // 무적 검사 (횟수 무적)
        if (IsInvincivilityCount > 0)
        {
            IsInvincivilityCount -= 1;
            // 🌟 무적 카운트 감소 상태를 모든 클라이언트에 즉시 동기화합니다.
            photonView.RPC("RPC_UpdateHealth", RpcTarget.All, CurrentHealth, IsDead, IsInvincivilityCount);
            return;
        }

        PhotonView attackerPV = PhotonView.Find(attackerViewId);
        LivingEntity attacker = attackerPV?.GetComponent<LivingEntity>();

        // 데미지 적용
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

        // 🌟 변경된 체력과 상태(Dead, InvincibilityCount)를 모든 클라이언트에 동기화합니다.
        // 마스터 클라이언트가 계산했기 때문에 RpcTarget.All로 보내면 됩니다.
        bool died = CurrentHealth <= 0f;
        if (died && !IsDead)
        {
            currentAttacker = attacker;
            int attackerId = attacker != null ? attacker.photonView.ViewID : -1;
            Debug.Log($"[LivingEntity - OnDamage] {attackerId}");
            photonView.RPC("RPC_Die", RpcTarget.All, attackerId);
        }

        photonView.RPC("RPC_UpdateHealth", RpcTarget.All, CurrentHealth, died, IsInvincivilityCount);

        // 피격 효과 RPC는 로컬에서만 실행되도록 Owner에게 전송
        photonView.RPC("RPC_OnHitEffect", photonView.Owner, (hitNormal.normalized));

        // 피격 반짝임 이펙트 시작 (모든 클라이언트에서)
        photonView.RPC("RPC_StartHitFlash", RpcTarget.All);

        if (cameraController != null)
        {
            cameraController.TriggerCameraShake(0.5f, 0.5f); // (지속시간, 세기)
        }
    }

    /// <summary>
    /// 체력을 회복하는 메서드. 이 메서드도 네트워크를 통해 호출되어야 합니다.
    /// </summary>
    /// <param name="healAmount">회복량</param>

    [PunRPC]
    public void RestoreHealth(float healAmount)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (IsDead || healAmount <= 0f) return; // ✅ IsDead 변수 사용

        float prevHealth = CurrentHealth;
        CurrentHealth = Mathf.Min(StartingHealth, CurrentHealth + healAmount);
        // float actualHealed = CurrentHealth - prevHealth; // 실제 회복량은 필요시 사용

        bool died = CurrentHealth <= 0f;
        photonView.RPC("RPC_UpdateHealth", RpcTarget.All, CurrentHealth, died, IsInvincivilityCount);

        photonView.RPC("RPC_OnHealEffect", photonView.Owner);
    }

    [PunRPC]
    public void RestoreShield(float shieldAmount)
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
    public bool RPC_Die(int attackerViewId)
    {
        Debug.Log("[LivingEntity] - RPC_Die 실행");
        // 이미 사망한 상태라면 처리하지 않음
        if (IsDead)
        {
            Debug.Log($"[LivingEntity] {gameObject.name} 이미 사망한 상태 - 중복 사망 처리 방지");
            return false;
        }

        // ViewID를 통해 attacker LivingEntity 찾기
        PhotonView attackerPV = PhotonView.Find(attackerViewId);
        LivingEntity attacker = attackerPV?.gameObject.GetComponent<LivingEntity>();

        // 사망 상태 설정
        IsDead = true;
        Debug.Log($"[LivingEntity] {attackerPV}");
        Debug.Log($"[LivingEntity] {gameObject.name} 사망 처리 완료 - attacker: {attacker}, IsDead: {IsDead}");

        //마스터 클라이언트에서 공격자에게 점수 부여
        if (PhotonNetwork.IsMasterClient && attacker != null)
        {
            PhotonView attackerView = attacker.photonView;
            float killScore = 100f;

            attackerView.RPC("RPC_GrantKillScore", attackerView.Owner, killScore);
            Debug.Log($"[LivingEntity:Master] {gameObject.name} 사망 -> {attacker.gameObject.name}에게 {killScore} 점수 부여 요청 RPC 전송");
        }

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

        OnDeath?.Invoke(); // 이벤트는 각 클라이언트에서 개별적으로 발생

        //스턴 작동
        if (moveController != null)
        {
            moveController.SetStunned(true);
        }

        // 플레이어 사망 이벤트 발생
        OnPlayerDied?.Invoke(this);

        // 부활 코루틴 시작 (마스터 클라이언트에서만)
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(ReviveAfterDelay(5f));
        }

        Debug.Log($"[LivingEntity] {gameObject.name} 사망! - 플레이어 사망 이벤트 발생");

        return true;
    }

    private IEnumerator ReviveAfterDelay(float delay)
    {
        Debug.Log($"[LivingEntity] {gameObject.name} 부활 대기 시작 - {delay}초 (마스터 클라이언트에서 실행)");

        yield return new WaitForSeconds(delay);

        // 부활 중에 마스터 클라이언트가 변경될 수 있으므로 다시 체크
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[LivingEntity] {gameObject.name} 마스터 클라이언트가 아니므로 부활 처리 중단");
            yield break;
        }

        // 이미 부활한 상태인지 체크
        if (!IsDead)
        {
            Debug.Log($"[LivingEntity] {gameObject.name} 이미 부활한 상태 - 부활 처리 중단");
            yield break;
        }

        Debug.Log($"[LivingEntity] {gameObject.name} 부활 대기 완료 - 부활 RPC 호출");

        // 부활 RPC 호출 (마스터 클라이언트에서만)
        photonView.RPC("RPC_Revive", RpcTarget.All);
        Debug.Log($"[LivingEntity] {gameObject.name} 부활 RPC 호출 완료");
    }

    [PunRPC]
    public void RPC_Revive()
    {
        if (!IsDead)
        {
            return;
        }

        // 사망 상태 해제
        IsDead = false;
        currentAttacker = null; // 공격자 정보 초기화

        // 체력 및 상태 초기화
        InitializeEntity();

        // 반짝임 코루틴 중지 및 색상 복원
        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = null;
        }
        RestoreOriginalColors();

        if (moveController != null)
        {
            moveController.SetStunned(false); // 스턴 해제
        }

        //  부활 직후 무적 상태 활성화 (모든 클라이언트 동기화)
        photonView.RPC("RPC_SetInvincibility", RpcTarget.All, true);


        //  마스터 클라이언트만 무적 해제 타이머 실행
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(DisableInvincibilityAfterDelay(3f));
        }


        // 모든 클라이언트에서 UI 업데이트
        OnAnyLivingEntityHealthChanged?.Invoke(CurrentHealth, StartingHealth, this);

        if (photonView.IsMine)
        {
            OnRevive?.Invoke();
        }
    }

    [PunRPC]
    public void RPC_SetInvincibility(bool active)
    {
        IsInvincivilityActive = active;
        // 무적 상태가 활성화되면 하얀색 반짝임 코루틴 시작
        if (active)
        {
            // 기존 무적 반짝임 코루틴이 실행 중이면 중지
            if (invincibilityFlashCoroutine != null)
            {
                StopCoroutine(invincibilityFlashCoroutine);
            }

            // 무적 반짝임 코루틴 시작
            invincibilityFlashCoroutine = StartCoroutine(InvincibilityFlashCoroutine());
        }
        else
        {
            // 무적 상태가 비활성화되면 반짝임 코루틴 중지
            if (invincibilityFlashCoroutine != null)
            {
                StopCoroutine(invincibilityFlashCoroutine);
                invincibilityFlashCoroutine = null;
            }

            // 원본 색상으로 복원
            RestoreOriginalColors();
        }
    }

    /// <summary>
    /// 무적 상태일 때 하얀색으로 반짝거리는 코루틴 (1초마다 재생)
    /// </summary>
    private IEnumerator InvincibilityFlashCoroutine()
    {
        if (renderers == null || renderers.Length == 0) yield break;

        float flashDuration = 0.1f; // 반짝임 지속 시간
        Color flashColor = Color.white; // 하얀색으로 반짝임
        Color flashEmissionColor = flashColor * EMISSION_INTENSITY * 0.5f; // ✅ Emission 색상 설정

        while (IsInvincivilityActive && !IsDead)
        {
            // 색상을 하얀색으로 변경
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
                        mat.SetColor("_EmissionColor", flashEmissionColor);
                    }
                }
            }

            yield return new WaitForSeconds(flashDuration);

            // 원본 색상으로 복원
            RestoreOriginalColors();

            // 1초 대기 후 다시 반짝임
            yield return new WaitForSeconds(1f - flashDuration);
        }

        // 코루틴 종료 시 원본 색상으로 복원
        RestoreOriginalColors();
        invincibilityFlashCoroutine = null;
    }

    /// <summary>
    /// 모든 Renderer의 색상과 Emission을 원본 색상으로 복원합니다.
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

    /// <summary>
    /// 일정 시간 후 무적 해제 (마스터만 실행)
    /// </summary>
    private IEnumerator DisableInvincibilityAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 마스터 클라이언트만 무적 해제 RPC 호출
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_SetInvincibility", RpcTarget.All, false);
        }
    }

    //무제한 무적
    [PunRPC]
    public void Set_Count_invincibility(int count)
    {
        // 🚨 이 함수는 OneTimeDefense.InitializeShield에서 RPC로 호출되어야 합니다.
        // 모든 클라이언트에서 동일하게 카운트를 설정합니다.
        if (IsInvincivilityCount <= 0)
        {
            IsInvincivilityCount = count;
        }
        // Debug.Log($"[LivingEntity] Set_Count_invincibility called. Count: {IsInvincivilityCount}");
    }

    public bool HasInvincibilityCount()
    {
        return IsInvincivilityCount > 0;
    }

    public LivingEntity GetAttacker()
    {
        return currentAttacker;
    }

    /// 피격 이펙트 (빨간색 반짝임 + 사운드)
    /// </summary>
    [PunRPC]
    private void RPC_OnHitEffect(Vector3 hitDirection)
    {
        // 해당 클라이언트에서만 실행되는 UI 이벤트
        if (photonView.IsMine)
        {
            GameEvents.OnLocalPlayerHit?.Invoke(hitDirection);
        }

        // 피격 반짝임
        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = null;
        }
        hitFlashCoroutine = StartCoroutine(HitFlashOnceCoroutine());

        // 피격 사운드 (쿨타임 체크)
        if (Time.time - lastHitSoundTime >= HIT_SOUND_COOLDOWN)
        {
            if (hitSound != null)
            {
                AudioManager.Inst?.PlayClipAtPoint(hitSound, transform.position, 1f, 1f, null, transform);
            }
            lastHitSoundTime = Time.time;
        }
    }

    [PunRPC]
    public void RPC_StartHitFlash()
    {
        // 이미 실행 중인 코루틴이 있다면 즉시 중지
        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = null;
        }

        // 피격 중이 아닐 때만 반짝임 시작
        hitFlashCoroutine = StartCoroutine(HitFlashOnceCoroutine());
    }

    /// <summary>
    /// 피격 시 1회만 반짝거리는 코루틴 (짧고 즉시 종료)
    /// </summary>
    private IEnumerator HitFlashOnceCoroutine()
    {
        if (renderers == null || renderers.Length == 0 || IsDead) yield break;

        float flashDuration = 0.1f; // 반짝임 지속 시간
        Color flashColor = Color.red; // 빨간색으로 반짝임
        Color flashEmissionColor = flashColor * EMISSION_INTENSITY; // ✅ Emission 색상 설정

        // 색상을 빨간색으로 변경
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
                    mat.SetColor("_EmissionColor", flashEmissionColor);
                }
            }
        }

        yield return new WaitForSeconds(flashDuration);

        // 원본 색상으로 복원
        RestoreOriginalColors();

        // 종료 시점에서 코루틴 null 처리
        hitFlashCoroutine = null;
    }

    [PunRPC]
    public void RPC_OnHealEffect()
    {
        // 해당 클라이언트에서만 실행되는 UI 이벤트
        if (photonView.IsMine)
        {
            GameEvents.OnLocalPlayerHeal?.Invoke();
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
        // if (stream.IsWriting)
        // {
        //     // 마스터 클라이언트 또는 소유자(IsMine)가 쓰기 권한을 가집니다.
        //     // 데미지 계산은 마스터 클라이언트에서 수행하고, 그 결과를 여기서 동기화합니다.
        //     stream.SendNext(CurrentHealth);
        //     stream.SendNext(IsDead);
        //     // 필요하다면 실드도 동기화
        //     // stream.SendNext(CurrentShield);

        // }
        // else
        // {
        //     // 다른 클라이언트들은 수신하여 업데이트합니다.
        //     bool wasDead = this.IsDead; // 이전 사망 상태 저장
        //     this.CurrentHealth = (float)stream.ReceiveNext();
        //     this.IsDead = (bool)stream.ReceiveNext();
        //     // this.CurrentShield = (float)stream.ReceiveNext();

        //     // ✅ IPunObservable로 체력이 동기화될 때, 로컬 UI도 업데이트되도록 이벤트 발생
        //     // 이 부분이 중요합니다! 다른 클라이언트에서 체력이 변경될 때도 UI가 동기화됩니다.
        //     // 하지만 GameManager는 IsMine인지 다시 확인하여 로컬 플레이어만 UI 업데이트를 합니다.
        //     OnAnyLivingEntityHealthChanged?.Invoke(this.CurrentHealth, this.StartingHealth, this);

        //     // ✅ 사망 상태가 변경된 경우에만 사망 이벤트 발생 (중복 방지)
        //     if (!wasDead && this.IsDead)
        //     {
        //         OnDeath?.Invoke();
        //         OnPlayerDied?.Invoke(this);

        //         // 로컬 플레이어 사망 시에만 손실 처리 (중복 방지)
        //         if (photonView.IsMine && GameManager.Instance != null)
        //         {
        //             GameManager.Instance.HandlePlayerDeathPenalty();
        //         }
        //     }
        // }
    }



    #endregion
}