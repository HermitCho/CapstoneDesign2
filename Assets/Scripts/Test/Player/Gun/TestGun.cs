using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// 총기 시스템을 관리하는 클래스
/// 발사, 재장전, 조준, 효과 재생 등의 기능을 담당
/// </summary>
public class TestGun : MonoBehaviourPun
{
    public static System.Action OnLocalReloadStarted; // 로컬 플레이어가 재장전을 실제로 시작했을 때

    #region Enums
    public enum GunState
    {
        Ready,
        Empty,
        Reloading
    }
    #endregion

    #region Serialized Fields
    [Header("Living Entity")]
    [SerializeField] private LivingEntity livingEntity;

    [Header("Gun Configuration")]
    [SerializeField] private GunData gunData;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem muzzleFlashEffect;
    [SerializeField] private ParticleSystem shellEjectEffect;

    [Header("Aiming System")]
    [SerializeField] private Transform fireTransform;

    private MoveController moveController;
    private SkillController skillController;
    private TestShoot testShoot; // TestShoot 스크립트 참조 추가

    #endregion

    #region Properties
    public GunState CurrentState { get; private set; } // 인스턴스 속성 (멀티플레이어 필수!)
    [HideInInspector] public int CurrentMagAmmo { get; private set; }
    public bool IsShouldering { get; private set; }
    #endregion

    #region Private Fields
    private PhotonView photonViewCached;
    private bool isFiring;
    private float lastFireTime;
    private float damage;
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        photonViewCached = GetComponent<PhotonView>();
        damage = gunData.damage;
        testShoot = GetComponentInParent<TestShoot>(); // TestShoot 스크립트 찾기

        if (testShoot == null)
        {
            Debug.LogError("TestShoot 스크립트를 찾을 수 없습니다.");
        }
    }

    protected virtual void OnEnable()
    {
        InitializeGunState();
    }

    protected virtual void OnDisable()
    {
        CurrentState = GunState.Empty;
    }

    private void Update()
    {
        // ✅ 발사자만 Update에서 발사 처리
        if (!photonViewCached.IsMine) return;

        if (isFiring)
        {
            ProcessFiring();
        }
    }
    #endregion

    #region Initialization
    private void InitializeGunState()
    {
        isFiring = false;
        CurrentMagAmmo = gunData.currentAmmo;
        CurrentState = GunState.Ready;
        lastFireTime = 0f;
        IsShouldering = false;
        moveController = GetComponentInParent<MoveController>();
        skillController = GetComponentInParent<SkillController>();
    }
    #endregion

    #region Input Handling
    public void InputFire(bool shouldFire)
    {
        if (!photonViewCached.IsMine) return;
        // ✅ 발사 입력은 All로 동기화 (UI 피드백 등을 위해)
        photonViewCached.RPC("RPC_InputFire", RpcTarget.All, shouldFire);
    }

    [PunRPC]
    public void RPC_InputFire(bool shouldFire)
    {
        isFiring = shouldFire;
    }
    #endregion

    #region Firing System
    private void ProcessFiring()
    {
        if (!photonViewCached.IsMine) return;

        if (CanFire())
        {
            Vector3 shootDirection = testShoot.CalculateShotDirection();
            ExecuteFire(shootDirection);
        }
    }
    public GunData GetGunData()
    {
        return gunData;
    }
    public Transform GetFireTransform()
    {
        return fireTransform;
    }

    // ✅ 이름 변경: FireAtWorldPoint -> ExecuteFire (더 명확한 의미)
    private void ExecuteFire(Vector3 shootDirection)
    {
        if (!photonViewCached.IsMine) return;

        lastFireTime = Time.time;

        // ✅ 발사 실행을 RPC로 전송
        photonViewCached.RPC("RPC_Shot", RpcTarget.All, shootDirection);
    }

    private bool CanFire()
    {
        return CurrentState == GunState.Ready &&
           !moveController.IsStunned() &&
           !IsSkillBeingUsed() &&
           Time.time >= lastFireTime + gunData.fireRate &&
           CurrentMagAmmo > 0;
    }

    /// <summary>
    /// 스킬이 사용 중인지 확인
    /// </summary>
    private bool IsSkillBeingUsed()
    {
        if (skillController == null) return false;

        // MoveController에서 프리뷰가 활성화되어 있거나 스킬 사용이 차단된 상태인지 확인
        return !skillController.CanUseSkill() || skillController.IsPreviewActive();
    }

    [PunRPC]
    public void RPC_Shot(Vector3 shootDirection)
    {
        if (CurrentMagAmmo <= 0) return;

        Shot(shootDirection);

        // ✅ 탄약 소모는 발사자만 처리하고 RPC로 동기화
        if (photonViewCached.IsMine)
        {
            CurrentMagAmmo--;
            if (CurrentMagAmmo <= 0)
                CurrentState = GunState.Empty;

            // 탄약 상태를 다른 클라이언트에게 동기화
            photonViewCached.RPC("RPC_SyncAmmo", RpcTarget.Others, CurrentMagAmmo, CurrentState);
        }
    }

    [PunRPC]
    private void RPC_SyncAmmo(int newAmmoCount, GunState newState)
    {
        CurrentMagAmmo = newAmmoCount;
        CurrentState = newState;
    }

    protected virtual void Shot(Vector3 shootDirection)
    {
        Debug.Log("[TestGun - Shot] - 샷");
        for (int i = 0; i < gunData.pelletCount; i++)
        {
            Vector3 pelletDirection = CalculatePelletDirection(shootDirection);
            Vector3 pelletHitPosition = CalculatePelletHitPosition(pelletDirection);

            // ✅ 데미지 판정은 발사자만 처리
            if (photonViewCached.IsMine)
            {
                ProcessPelletHit(pelletDirection);
            }

            // ✅ 이펙트는 모든 클라이언트가 실행 (하지만 Shot이 이미 RPC로 호출되므로 추가 RPC 불필요)
            StartCoroutine(ShotEffect(fireTransform.position, pelletHitPosition));
        }
    }

    private Vector3 CalculatePelletDirection(Vector3 baseDirection)
    {
        float currentSpreadAngle = GetCurrentSpreadAngle();
        if (currentSpreadAngle <= 0f)
            return baseDirection;

        return Quaternion.Euler(
          Random.Range(-currentSpreadAngle, currentSpreadAngle),
          Random.Range(-currentSpreadAngle, currentSpreadAngle),
          0f
        ) * baseDirection;
    }

    private float GetCurrentSpreadAngle()
    {
        if (gunData.isShotgun)
            return gunData.spreadAngle / 1.5f;

        if (CameraController.isZoomed)
            return 0f;
        return gunData.spreadAngle;
    }

    private Vector3 CalculatePelletHitPosition(Vector3 direction)
    {
        int layerMask = ~LayerMask.GetMask("PlayerPosition");
        if (Physics.Raycast(fireTransform.position, direction, out RaycastHit hit, gunData.range, layerMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }
        return fireTransform.position + direction * gunData.range;
    }

    private void ProcessPelletHit(Vector3 direction)
    {
        int layerMask = ~LayerMask.GetMask("PlayerPosition");
        if (Physics.Raycast(fireTransform.position, direction, out RaycastHit hit, gunData.range, layerMask, QueryTriggerInteraction.Ignore))
        {
            // --- ✅ [튜토리얼 전용 감지 코드] ---
            // 현재 씬 이름이 Tutorial을 포함하면, 네트워크 판정 대신 로컬 파괴 실행
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tutorial"))
            {
                TargetMove targetMove = hit.collider.GetComponent<TargetMove>();
                if (targetMove != null)
                {
                    // 튜토리얼용 목표 카운트 처리
                    var tutorialShoot = FindObjectOfType<TutorialShoot>();
                    if (tutorialShoot != null)
                        tutorialShoot.OnTargetDestroyed();

                    // 피격 이펙트
                    GameObject effect = Resources.Load<GameObject>("HitEffect");
                    if (effect != null)
                    {
                        GameObject fx = Instantiate(effect, hit.point, Quaternion.LookRotation(hit.normal));
                        Destroy(fx, 2f);
                    }
                    AudioManager.Inst.PlayOneShot("SFX_Game_Tutorial_Target");
                    // 과녁 오브젝트 파괴
                    Destroy(targetMove.gameObject);
                    Debug.Log($"[TestGun] 튜토리얼용 과녁 {targetMove.name} 파괴됨");
                    return; // ✅ 튜토리얼에서는 여기서 종료 (Photon 로직 실행 안 함)
                }
            }
            
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            PhotonView targetView = hit.collider.GetComponent<PhotonView>();

            if (target != null && targetView != null)
            {
                // 자기 자신 피격 방지: 동일한 소유자면 무시
                if (targetView.OwnerActorNr == photonViewCached.OwnerActorNr)
                {
                    return;
                }

                int attackerActorNumber = photonViewCached.OwnerActorNr;

                // 마스터 클라이언트로 데미지 RPC 전송
                targetView.RPC("OnDamage", RpcTarget.All, damage, hit.point, hit.normal, attackerActorNumber);
            }
        }
    }

    #endregion

    #region Visual Effects
    protected virtual IEnumerator ShotEffect(Vector3 start, Vector3 end)
    {
        PlayMuzzleEffects();
        PlayAudioEffect();

        GameObject trailObject = CreateBulletTrail(start, end);
        yield return new WaitForSeconds(gunData.bulletTrailDuration);
        Destroy(trailObject);
    }

    private void PlayMuzzleEffects()
    {
        if (muzzleFlashEffect != null) muzzleFlashEffect.Play();
        if (shellEjectEffect != null) shellEjectEffect.Play();
    }

    private void PlayAudioEffect()
    {
        if (gunData.shotClip != null)
        {
            AudioManager.Inst?.PlayClipAtPoint(gunData.shotClip, fireTransform.position, 1f, 1f, null, fireTransform);
        }
    }

    private GameObject CreateBulletTrail(Vector3 start, Vector3 end)
    {
        GameObject trailObject = new GameObject("BulletTrail");
        LineRenderer lineRenderer = trailObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.material = gunData.bulletTrailMaterial;
        lineRenderer.startWidth = gunData.bulletTrailStartWidth;
        lineRenderer.endWidth = gunData.bulletTrailEndWidth;
        lineRenderer.useWorldSpace = true;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        return trailObject;
    }
    #endregion

    #region Reload System
    public bool Reload()
    {
        if (!photonViewCached.IsMine) return false;
        if (CurrentState == GunState.Reloading || CurrentMagAmmo >= gunData.maxAmmo)
            return false;

        // 소유자만 재장전 시작 (상태 변경은 RPC로 동기화)
        StartCoroutine(ReloadRoutine());

        // 로컬 재장전 시작 이벤트 발행 (실제 재장전이 시작될 때만)
        OnLocalReloadStarted?.Invoke();

        return true;
    }

    protected virtual IEnumerator ReloadRoutine()
    {
        // 재장전 상태를 모든 클라이언트에 동기화
        photonViewCached.RPC("RPC_SetReloadingState", RpcTarget.All, true);

        // 소유자만 사운드 재생
        if (photonViewCached.IsMine)
        {
            PlayReloadSound();
        }

        yield return new WaitForSeconds(gunData.reloadTime);

        // 재장전 완료를 모든 클라이언트에 동기화
        photonViewCached.RPC("RPC_CompleteReload", RpcTarget.All);
    }

    /// <summary>
    /// 재장전 상태 설정 (모든 클라이언트)
    /// </summary>
    [PunRPC]
    private void RPC_SetReloadingState(bool isReloading)
    {
        CurrentState = isReloading ? GunState.Reloading : GunState.Ready;
    }

    [PunRPC]
    protected void RPC_CompleteReload()
    {
        CurrentMagAmmo = gunData.maxAmmo;
        CurrentState = GunState.Ready;
    }

    private void PlayReloadSound()
    {
        if (gunData.reloadClip != null)
        {
            AudioManager.Inst?.PlayClipAtPoint(gunData.reloadClip, fireTransform.position, 1f, 1f, null, fireTransform);
        }
    }
    #endregion
}