using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 총기 시스템을 관리하는 클래스
/// 발사, 재장전, 조준, 효과 재생 등의 기능을 담당
/// </summary>
public class TestGun : MonoBehaviour
{
    #region Enums

    /// <summary>
    /// 총기의 현재 상태를 나타내는 열거형
    /// </summary>
    public enum GunState
    {
        Ready,       // 발사 준비 완료
        Empty,       // 탄창이 빔
        Reloading    // 재장전 중
    }

    #endregion

    #region Serialized Fields

    [Header("Gun Configuration")]
    [SerializeField] private GunData gunData;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem muzzleFlashEffect;
    [SerializeField] private ParticleSystem shellEjectEffect;

    [Header("Aiming System")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform fireTransform;
    [SerializeField] private RectTransform aimPointUI;
    [SerializeField] private MuzzleDirectionController muzzleDirectionController;

    [Header("스턴 제어")]
    private MoveController moveController;
    #endregion

    #region Properties

    /// <summary>
    /// 현재 총기의 상태
    /// </summary>
    public GunState CurrentState { get; private set; }

    /// <summary>
    /// 현재 탄창에 남아있는 총알 수
    /// </summary>
    [HideInInspector] public int CurrentMagAmmo { get; private set; }

    /// <summary>
    /// 견착 상태 여부
    /// </summary>
    public bool IsShouldering { get; private set; }

    #endregion

    #region Private Fields

    private AudioSource gunAudioPlayer;
    private bool isFiring;
    private float lastFireTime;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    protected virtual void Awake()
    {
    
    }

    /// <summary>
    /// 총기 활성화 시 초기화
    /// </summary>
    protected virtual void OnEnable()
    {
        InitializeComponents();
        InitializeGunState();
    }

    /// <summary>
    /// 총기 비활성화 시 상태 변경
    /// </summary>
    protected virtual void OnDisable()
    {
        CurrentState = GunState.Empty;
    }

    /// <summary>
    /// 매 프레임 업데이트
    /// </summary>
    private void Update()
    {
        if (isFiring)
        {
            ProcessFiring();
        }

        // 디버그 레이

        //DrawDebugRays();
    }

    //디버그 용 레이를 그려주는 함수, 필요하면 주석 해제
    // private void DrawDebugRays()
    // {
    //     if (aimPointUI == null || mainCamera == null || fireTransform == null) return;

    //     Vector3 screenPoint = aimPointUI.position;
    //     Ray cameraRay = mainCamera.ScreenPointToRay(screenPoint);
    //     Debug.DrawRay(cameraRay.origin, cameraRay.direction * gunData.range, Color.blue);

    //     int layerMask = ~LayerMask.GetMask("PlayerPosition");

    //     Vector3 targetPoint;
    //     if (Physics.Raycast(cameraRay, out RaycastHit hit, gunData.range, layerMask))
    //     {
    //         targetPoint = hit.point;
    //     }
    //     else
    //     {
    //         targetPoint = cameraRay.origin + cameraRay.direction * gunData.range;
    //     }

    //     // 빨간색 레이: fireTransform에서 targetPoint로
    //     Vector3 fireDirection = (targetPoint - fireTransform.position).normalized;
    //     Debug.DrawRay(fireTransform.position, fireDirection * gunData.range, Color.red);

    //     // 노란색 레이: Shot 함수의 실제 발사 방향(실시간)
    //     Debug.DrawRay(fireTransform.position, fireDirection * gunData.range, Color.yellow);
    // }

    #endregion

    #region Initialization

    /// <summary>
    /// 컴포넌트들을 초기화합니다.
    /// </summary>
    private void InitializeComponents()
    {
        gunAudioPlayer = GetComponent<AudioSource>();
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        
        // ✅ Crosshair UI를 비동기로 찾기
        StartCoroutine(FindCrosshairUI());
        muzzleDirectionController = GetComponentInChildren<MuzzleDirectionController>();
    }
    
    /// <summary>
    /// Crosshair UI를 찾는 코루틴
    /// </summary>
    private IEnumerator FindCrosshairUI()
    {
        int maxRetries = 100; // 최대 3초 대기 (30 * 0.1초)
        int currentRetry = 0;
        
        while (aimPointUI == null && currentRetry < maxRetries)
        {
            // Crosshair 태그로 찾기
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
            yield return new WaitForSeconds(0.1f); // 0.1초마다 재시도
        }
        
        if (aimPointUI == null)
        {
            Debug.LogWarning("⚠️ TestGun: Crosshair UI를 찾을 수 없습니다. 카메라 중앙을 사용합니다.");
        }
    }

    /// <summary>
    /// 총기 상태를 초기화합니다.
    /// </summary>
    private void InitializeGunState()
    {
        isFiring = false;
        CurrentMagAmmo = gunData.currentAmmo;
        CurrentState = GunState.Ready;
        lastFireTime = 0f;
        IsShouldering = false;
        moveController = GetComponentInParent<MoveController>();
        Debug.Log("???" + moveController.gameObject);
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// 발사 입력을 처리합니다.
    /// </summary>
    /// <param name="shouldFire">발사 여부</param>
    public void InputFire(bool shouldFire)
    {
        isFiring = shouldFire;
    }

    #endregion

    #region Firing System

    /// <summary>
    /// 발사 처리를 수행합니다.
    /// </summary>
    private void ProcessFiring()
    {
        Vector3 targetPosition = CalculateShotTarget();
        FireAtWorldPoint(targetPosition);
    }

    /// <summary>
    /// 조준점을 기반으로 발사 목표점을 계산합니다.
    /// </summary>
    /// <returns>발사할 월드 좌표</returns>
    private Vector3 CalculateShotTarget()
    {
        Vector3 screenPoint = aimPointUI.position;
        Ray cameraRay = mainCamera.ScreenPointToRay(screenPoint);

        // 카메라에서 나가는 레이 시각화 (파란색)
        Debug.DrawRay(cameraRay.origin, cameraRay.direction * gunData.range, Color.blue, 1f);

        // IgnoreRaycast 레이어를 무시하는 마스크 생성
        int layerMask = ~LayerMask.GetMask("PlayerPosition");

        if (Physics.Raycast(cameraRay, out RaycastHit hit, gunData.range, layerMask))
        {
            return hit.point;
        }
        else
        {
            return cameraRay.origin + cameraRay.direction * gunData.range;
        }
    }

    /// <summary>
    /// 지정된 월드 좌표로 발사합니다.
    /// </summary>
    /// <param name="worldPoint">발사할 월드 좌표</param>
    public void FireAtWorldPoint(Vector3 worldPoint)
    {
        if (CanFire())
        {
            lastFireTime = Time.time;
            Vector3 direction;
            Collider fireCollider = fireTransform.GetComponent<BoxCollider>();
            bool isBlocked = false;
            if (fireCollider != null)
            {
                Collider[] hits = Physics.OverlapBox(
                    fireCollider.bounds.center,
                    fireCollider.bounds.extents,
                    fireTransform.rotation,
                    ~0, // 모든 레이어
                    QueryTriggerInteraction.Collide
                );
            }

            if (isBlocked)
            {
                direction = fireTransform.forward;
            }
            else
            {
                direction = (worldPoint - fireTransform.position).normalized;
            }

            muzzleDirectionController?.SetDirection(direction);

            Debug.DrawRay(fireTransform.position, direction * gunData.range, Color.red, 1f);
            Shot(direction);
        }
    }

    /// <summary>
    /// 발사 가능한지 확인합니다.
    /// </summary>
    /// <returns>발사 가능 여부</returns>
    private bool CanFire()
    {
        return CurrentState == GunState.Ready && !moveController.IsStunned() &&
               Time.time >= lastFireTime + gunData.fireRate;
    }

    /// <summary>
    /// 실제 발사를 수행합니다.
    /// </summary>
    /// <param name="shootDirection">발사 방향</param>
    /// <param name="hitPosition">목표 위치</param>
    protected virtual void Shot(Vector3 shootDirection)
    {
        // 노란색 레이(실제 발사 방향)로만 총알이 나가도록 함
        for (int i = 0; i < gunData.pelletCount; i++)
        {
            Vector3 pelletDirection = CalculatePelletDirection(shootDirection, i);
            Vector3 pelletHitPosition = CalculatePelletHitPosition(pelletDirection, fireTransform.position + pelletDirection * gunData.range, i);
            ProcessPelletHit(pelletDirection);
            StartCoroutine(ShotEffect(fireTransform.position, pelletHitPosition));
        }
        ConsumeAmmo();
    }

    /// <summary>
    /// 개별 펠릿의 방향을 계산합니다.
    /// </summary>
    /// <param name="baseDirection">기본 발사 방향</param>
    /// <param name="pelletIndex">펠릿 인덱스</param>
    /// <returns>펠릿 방향</returns>
    private Vector3 CalculatePelletDirection(Vector3 baseDirection, int pelletIndex)
    {
        // 줌 상태에 따라 분산도 결정
        float currentSpreadAngle = GetCurrentSpreadAngle();

        if (currentSpreadAngle <= 0f)
        {
            return baseDirection;
        }

        return Quaternion.Euler(
            Random.Range(-currentSpreadAngle, currentSpreadAngle),
            Random.Range(-currentSpreadAngle, currentSpreadAngle),
            0f
        ) * baseDirection;
    }

    /// <summary>
    /// 현재 줌 상태에 따른 분산도를 반환합니다.
    /// </summary>
    /// <returns>현재 분산도</returns>
    private float GetCurrentSpreadAngle()
    {
        // 줌 상태일 때는 분산도 0 (정확한 조준)
        if (CameraController.isZoomed)
        {
            return 0f;
        }

        // 일반 상태일 때는 설정된 분산도 사용
        return gunData.spreadAngle;
    }

    /// <summary>
    /// 펠릿의 충돌 위치를 계산합니다.
    /// </summary>
    /// <param name="direction">펠릿 방향</param>
    /// <param name="defaultHitPosition">기본 충돌 위치</param>
    /// <param name="pelletIndex">펠릿 인덱스</param>
    /// <returns>충돌 위치</returns>
    private Vector3 CalculatePelletHitPosition(Vector3 direction, Vector3 defaultHitPosition, int pelletIndex)
    {
        if (Physics.Raycast(fireTransform.position, direction, out RaycastHit hit, gunData.range))
        {
            return hit.point;
        }

        // 첫 번째 펠릿은 지정된 위치 사용, 나머지는 방향 기반 계산
        return pelletIndex == 0 ? defaultHitPosition : fireTransform.position + direction * gunData.range;
    }

    /// <summary>
    /// 펠릿의 충돌을 처리합니다.
    /// </summary>
    /// <param name="direction">펠릿 방향</param>
    private void ProcessPelletHit(Vector3 direction)
    {
        if (Physics.Raycast(fireTransform.position, direction, out RaycastHit hit, gunData.range))
        {
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            target?.OnDamage(gunData.damage, hit.point, hit.normal);
        }
    }

    /// <summary>
    /// 탄약을 소모합니다.
    /// </summary>
    private void ConsumeAmmo()
    {
        CurrentMagAmmo--;
        if (CurrentMagAmmo <= 0)
        {
            CurrentState = GunState.Empty;
        }
    }

    #endregion

    #region Visual Effects

    /// <summary>
    /// 발사 효과를 재생합니다.
    /// </summary>
    /// <param name="start">시작 위치</param>
    /// <param name="end">끝 위치</param>
    /// <returns>코루틴</returns>
    protected virtual IEnumerator ShotEffect(Vector3 start, Vector3 end)
    {
        PlayMuzzleEffects();
        PlayAudioEffect();

        GameObject trailObject = CreateBulletTrail(start, end);

        yield return new WaitForSeconds(gunData.bulletTrailDuration);

        Destroy(trailObject);
    }

    /// <summary>
    /// 총구 효과를 재생합니다.
    /// </summary>
    private void PlayMuzzleEffects()
    {
        if (muzzleFlashEffect != null && !muzzleFlashEffect.isPlaying)
        {
            muzzleFlashEffect.Play();
        }

        if (shellEjectEffect != null && !shellEjectEffect.isPlaying)
        {
            shellEjectEffect.Play();
        }
    }

    /// <summary>
    /// 발사 사운드를 재생합니다.
    /// </summary>
    private void PlayAudioEffect()
    {
        if (gunAudioPlayer != null && gunData.shotClip != null)
        {
            gunAudioPlayer.PlayOneShot(gunData.shotClip);
        }
    }

    /// <summary>
    /// 총알 궤적을 생성합니다.
    /// </summary>
    /// <param name="start">시작 위치</param>
    /// <param name="end">끝 위치</param>
    /// <returns>궤적 오브젝트</returns>
    private GameObject CreateBulletTrail(Vector3 start, Vector3 end)
    {
        GameObject trailObject = new GameObject("BulletTrail");
        LineRenderer lineRenderer = trailObject.AddComponent<LineRenderer>();

        ConfigureLineRenderer(lineRenderer);
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        return trailObject;
    }

    /// <summary>
    /// 라인 렌더러를 설정합니다.
    /// </summary>
    /// <param name="lineRenderer">설정할 라인 렌더러</param>
    private void ConfigureLineRenderer(LineRenderer lineRenderer)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.material = gunData.bulletTrailMaterial;
        lineRenderer.startWidth = gunData.bulletTrailStartWidth;
        lineRenderer.endWidth = gunData.bulletTrailEndWidth;
        lineRenderer.useWorldSpace = true;
    }

    #endregion

    #region Reload System

    /// <summary>
    /// 재장전을 시작합니다.
    /// </summary>
    /// <returns>재장전 시작 성공 여부</returns>
    public bool Reload()
    {
        if (CurrentState == GunState.Reloading || CurrentMagAmmo >= gunData.currentAmmo)
        {
            return false;
        }

        StartCoroutine(ReloadRoutine());
        return true;
    }

    /// <summary>
    /// 재장전 루틴을 수행합니다.
    /// </summary>
    /// <returns>코루틴</returns>
    protected virtual IEnumerator ReloadRoutine()
    {
        CurrentState = GunState.Reloading;

        PlayReloadSound();

        yield return new WaitForSeconds(gunData.reloadTime);

        CompleteReload();
    }

    /// <summary>
    /// 재장전 사운드를 재생합니다.
    /// </summary>
    private void PlayReloadSound()
    {
        if (gunAudioPlayer != null && gunData.reloadClip != null)
        {
            gunAudioPlayer.PlayOneShot(gunData.reloadClip);
        }
    }

    /// <summary>
    /// 재장전을 완료합니다.
    /// </summary>
    private void CompleteReload()
    {
        CurrentMagAmmo = gunData.maxAmmo;
        CurrentState = GunState.Ready;
    }

    #endregion
}
