using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TestShoot : MonoBehaviourPun
{
    [SerializeField] private TestGun gun;
    private static bool isShooting = true;
    //수정 사항 - 제거 예정
    void Awake()
    {
        if (gun == null)
        {
            gun = GetComponent<TestGun>();
            if (gun == null)
            {
                gun = GetComponentInChildren<TestGun>();
            }
        }

        SetIsShooting(true);
    }

    void OnEnable()
    {
        if (!photonView.IsMine) return;
        InputManager.OnShootPressed += OnShootInput;
        InputManager.OnShootCanceledPressed += OnShootCanceledInput;
        InputManager.OnReloadPressed += OnReloadInput;
    }

    void OnDisable()
    {
        if (!photonView.IsMine) return;
        InputManager.OnShootPressed -= OnShootInput;
        InputManager.OnShootCanceledPressed -= OnShootCanceledInput;
        InputManager.OnReloadPressed -= OnReloadInput;
    }

    private void Update()
    {
        if (photonView.IsMine)
        {
            DrawDebugRay();
        }
    }
    void OnShootInput()
    {
        if (!photonView.IsMine) return;
        if (TestGun.CurrentState == TestGun.GunState.Ready) // TestGun의 정적 속성 사용
        {
            if (gun != null && isShooting)
                gun.InputFire(true);
        }

    }

    void OnShootCanceledInput()
    {
        if (!photonView.IsMine) return;
        if (gun != null)
            gun.InputFire(false);
    }

    void OnReloadInput()
    {
        if (!photonView.IsMine) return;
        if (gun != null)
            gun.Reload();
    }

    // TestGun에서 접근할 수 있도록 퍼블릭 메서드 추가
    public Vector3 CalculateShotDirection()
    {
        Camera mainCamera = Camera.main;
        GameObject crosshairObj = GameObject.FindGameObjectWithTag("Crosshair");
        RectTransform aimPointUI = crosshairObj != null ? crosshairObj.GetComponent<RectTransform>() : null;

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera를 찾을 수 없습니다.");
            return Vector3.zero;
        }

        Vector3 screenPoint = aimPointUI != null ? aimPointUI.position :
            new Vector3(Screen.width / 2f, Screen.height / 2f);

        Ray cameraRay = mainCamera.ScreenPointToRay(screenPoint);
        int layerMask = ~LayerMask.GetMask("PlayerPosition");

        Vector3 targetPosition;

        if (Physics.Raycast(cameraRay, out RaycastHit hit, gun.GetGunData().range, layerMask, QueryTriggerInteraction.Ignore))
        {
            targetPosition = hit.point;
        }
        else
        {
            targetPosition = cameraRay.origin + cameraRay.direction * gun.GetGunData().range;
        }

        Vector3 firePoint = gun.GetFireTransform().position;

        return (targetPosition - firePoint).normalized;
    }

    private void DrawDebugRay()
    {
        Vector3 firePoint = gun.GetFireTransform().position;
        Vector3 direction = CalculateShotDirection();
        float range = gun.GetGunData().range;

        // 총구에서 목표 지점까지 빨간색 선을 그립니다.
        Debug.DrawRay(firePoint, direction * range, Color.red);
    }
    public static void SetIsShooting(bool value)
    {
        isShooting = value;
    }

    public static bool GetIsShooting()
    {
        return isShooting;
    }
}