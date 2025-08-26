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
    private Camera mainCamera;
    [SerializeField] private Transform fireTransform;
    private RectTransform aimPointUI;

    private MoveController moveController;
    
    #endregion

    #region Properties
    public GunState CurrentState { get; private set; }
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
    }

    protected virtual void OnEnable()
    {
        InitializeComponents();
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
    private void InitializeComponents()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        StartCoroutine(FindCrosshairUI());
    }

    private IEnumerator FindCrosshairUI()
    {
        int maxRetries = 100;
        int currentRetry = 0;

        while (aimPointUI == null && currentRetry < maxRetries)
        {
            GameObject crosshairObj = GameObject.FindGameObjectWithTag("Crosshair");
            if (crosshairObj != null)
            {
                aimPointUI = crosshairObj.GetComponent<RectTransform>();
                if (aimPointUI != null)
                {
                    Debug.Log("✅ TestGun: Crosshair UI 찾기 성공!");
                    break;
                }
            }

            currentRetry++;
            yield return new WaitForSeconds(0.1f);
        }

        if (aimPointUI == null)
        {
            Debug.LogWarning("⚠️ TestGun: Crosshair UI를 찾을 수 없습니다.");
        }
    }

    private void InitializeGunState()
    {
        isFiring = false;
        CurrentMagAmmo = gunData.currentAmmo;
        CurrentState = GunState.Ready;
        lastFireTime = 0f;
        IsShouldering = false;
        moveController = GetComponentInParent<MoveController>();
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
        // ✅ 발사자만 실행되도록 보장
        if (!photonViewCached.IsMine) return;

        if (CanFire())
        {
            Vector3 targetPosition = CalculateShotTarget();
            ExecuteFire(targetPosition);
        }
    }

    private Vector3 CalculateShotTarget()
    {
        Vector3 screenPoint = aimPointUI != null ? aimPointUI.position :
            new Vector3(Screen.width / 2f, Screen.height / 2f);

        Ray cameraRay = mainCamera.ScreenPointToRay(screenPoint);
        int layerMask = ~LayerMask.GetMask("PlayerPosition");

        if (Physics.Raycast(cameraRay, out RaycastHit hit, gunData.range, layerMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }
        else
        {
            return cameraRay.origin + cameraRay.direction * gunData.range;
        }
    }

    // ✅ 이름 변경: FireAtWorldPoint -> ExecuteFire (더 명확한 의미)
    private void ExecuteFire(Vector3 worldPoint)
    {
        if (!photonViewCached.IsMine) return;

        lastFireTime = Time.time;
        Vector3 direction = (worldPoint - fireTransform.position).normalized;

        // ✅ 발사 실행을 RPC로 전송
        photonViewCached.RPC("RPC_Shot", RpcTarget.All, direction);
    }

    private bool CanFire()
    {
        return CurrentState == GunState.Ready &&
               !moveController.IsStunned() &&
               Time.time >= lastFireTime + gunData.fireRate &&
               CurrentMagAmmo > 0;
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
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            PhotonView targetView = hit.collider.GetComponent<PhotonView>();

            if (target != null && targetView != null)
            {
                // LivingEntity 객체 대신 PhotonView.ViewID를 전달
                int attackerViewId = livingEntity.photonView.ViewID;
                
                // 마스터 클라이언트로 데미지 RPC 전송
                targetView.RPC("OnDamage", RpcTarget.All, damage, hit.point, hit.normal, attackerViewId);
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

        photonViewCached.RPC("RPC_Reload", RpcTarget.All);
        return true;
    }

    [PunRPC]
    protected void RPC_Reload()
    {
        StartCoroutine(ReloadRoutine());
    }

    protected virtual IEnumerator ReloadRoutine()
    {
        CurrentState = GunState.Reloading;
        PlayReloadSound();
        yield return new WaitForSeconds(gunData.reloadTime);

        photonViewCached.RPC("RPC_CompleteReload", RpcTarget.All);
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