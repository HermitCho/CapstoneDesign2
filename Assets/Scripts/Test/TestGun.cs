using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGun : MonoBehaviour
{
    public enum State
    {
        Ready,       // 발사 준비 완료
        Empty,       // 탄창이 빔
        Reloading    // 재장전 중
    }
    public State state { get; set; } // 현재 총의 상태
    public GunData gunData;                      // 총의 데이터 기본값

    [Header("총 효과")]
    public ParticleSystem muzzleFlashEffect; // 총구 화염 효과
    public ParticleSystem shellEjectEffect;  // 탄피 배출 효과
    protected AudioSource gunAudioPlayer;        // 총 소리를 재생할 오디오 소스

    [Header("해당 스크립트가 가진 현재 총 정보")]
    protected bool isShot;
    [HideInInspector] public int magAmmo;    // 현재 탄창에 남아있는 총알 수
    protected float lastFireTime;              // 마지막으로 총을 발사한 시각

    [Header("총알 적중 관련 요소")]
    public Camera mainCamera;
    public Transform fireTransform; // 총알이 발사될 위치
    public RectTransform aimPointUI; // UI상의 조준점


    // 컴포넌트 초기화
    protected void Awake()
    {
        gunAudioPlayer = GetComponent<AudioSource>();
    }

    // 총 활성화 시 초기화
    protected void OnEnable()
    {
        isShot = false;
        magAmmo = gunData.currentAmmo;      // 탄창을 가득 채움
        state = State.Ready;                // 상태를 준비 완료로 설정
        lastFireTime = 0;                   // 마지막 발사 시각 초기화
    }

    protected void OnDisable()
    {
        state = State.Empty; // 비활성화 시 상태를 Empty로
    }

    //발사 효과 코루틴 (총구 화염, 탄피, 궤적, 사운드)
    //start와 end 위치를 매개변수로 받아서 동적으로 LineRenderer를 생성합니다.
    protected virtual IEnumerator ShotEffect(Vector3 start, Vector3 end)
    {
        // 총구 화염 및 탄피 배출은 한 번의 발사에 한 번만 재생
        // (펠릿마다 반복되면 과함)
        if (muzzleFlashEffect != null && !muzzleFlashEffect.isPlaying)
            muzzleFlashEffect.Play();
        if (shellEjectEffect != null && !shellEjectEffect.isPlaying)
            shellEjectEffect.Play();
        if (gunAudioPlayer != null && gunData.shotClip != null)
            gunAudioPlayer.PlayOneShot(gunData.shotClip);

        // 새로운 LineRenderer 오브젝트 생성 (펠릿당 하나)
        GameObject lineObj = new GameObject("PelletTrail");
        LineRenderer line = lineObj.AddComponent<LineRenderer>();

        line.positionCount = 2;
        // GunData에서 라인 렌더러 속성을 가져와 설정
        line.material = gunData.bulletTrailMaterial;
        line.startWidth = gunData.bulletTrailStartWidth;
        line.endWidth = gunData.bulletTrailEndWidth;
        line.useWorldSpace = true; // 월드 공간에서 궤적을 그림

        line.SetPosition(0, start);
        line.SetPosition(1, end);

        yield return new WaitForSeconds(gunData.bulletTrailDuration); // 궤적 유지 시간

        Destroy(lineObj); // 궤적 오브젝트 제거
    }



    public void InputFire(bool inputBool)
    {
        Debug.Log(inputBool);
        isShot = inputBool;
    }

    void Update()
    {
        Debug.Log(isShot);
        if(isShot)
            SetShotPoint();
    }

    //조준점과 타겟 맞추기
    void SetShotPoint()
    {
        Vector3 screenPoint;
        if (aimPointUI != null)
        {
            // UI 좌표 → 스크린 좌표
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                aimPointUI, aimPointUI.position, mainCamera, out Vector3 worldPoint);
            screenPoint = mainCamera.WorldToScreenPoint(worldPoint);
        }
        else
        {
            // 화면 중앙
            screenPoint = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        }

        // 스크린 좌표에서 ray 생성
        Ray ray = mainCamera.ScreenPointToRay(screenPoint);
        RaycastHit hit;
        Vector3 hitPosition;
        if (Physics.Raycast(ray, out hit, gunData.range))
        {
            hitPosition = hit.point;
        }
        else
        {
            // 충돌이 없으면 최대 사거리로
            hitPosition = ray.origin + ray.direction * gunData.range;
        }

        // 총구에서 hitPosition까지 궤적
        FireAtWorldPoint(hitPosition);
    }

    // 새로운 메서드 추가: 월드 좌표로 발사
    public void FireAtWorldPoint(Vector3 worldPoint)
    {
        if (state == State.Ready && (Time.time >= lastFireTime + gunData.fireRate))
        {
            lastFireTime = Time.time;
            Vector3 direction = (worldPoint - fireTransform.position).normalized;
            Shot(direction, worldPoint);
        }
    }

    // Shot 오버로드 추가 (끝점 지정)
    protected virtual void Shot(Vector3 shootDirection, Vector3 hitPosition)
    {
        for (int i = 0; i < gunData.pelletCount; i++)
        {
            // 퍼짐 적용
            Vector3 direction = shootDirection;
            if (gunData.spreadAngle > 0)
            {
                direction = Quaternion.Euler(
                    Random.Range(-gunData.spreadAngle, gunData.spreadAngle),
                    Random.Range(-gunData.spreadAngle, gunData.spreadAngle),
                    0f
                ) * direction;
            }

            Vector3 pelletHitPosition = fireTransform.position + direction * gunData.range;
            RaycastHit hit;
            if (Physics.Raycast(fireTransform.position, direction, out hit, gunData.range))
            {
                pelletHitPosition = hit.point;
            }
            else
            {
                // 첫 펠릿은 지정된 hitPosition 사용
                if (i == 0)
                    pelletHitPosition = hitPosition;
            }
            StartCoroutine(ShotEffect(fireTransform.position, pelletHitPosition));
        }
        magAmmo--;
        if (magAmmo <= 0)
            state = State.Empty;
    }

    public bool Reload()
    {
        if (state == State.Reloading || magAmmo >= gunData.currentAmmo)
        {
            return false; // 이미 재장전 중이거나 탄창이 가득 찼으면 리턴
        }

        StartCoroutine(ReloadRoutine());
        Debug.Log("Reload Time: " + gunData.reloadTime); // 리로드 시간 디버깅
        return true;
    }

    // 재장전 처리 루틴
    protected IEnumerator ReloadRoutine()
    {
        state = State.Reloading;
        if (gunAudioPlayer != null && gunData.reloadClip != null)
            gunAudioPlayer.PlayOneShot(gunData.reloadClip); // 재장전 소리

        yield return new WaitForSeconds(gunData.reloadTime); // 리로드 대기

        magAmmo = gunData.maxAmmo; // 탄창을 가득 채움

        state = State.Ready; // 상태를 준비 완료로 변경
    }
}
