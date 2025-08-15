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
        // gun이 할당되지 않았다면 자동으로 찾기
        if (gun == null)
        {
            gun = GetComponent<TestGun>();
            if (gun == null)
            {
                gun = GetComponentInChildren<TestGun>();
            }
            if (gun == null)
            {
                Debug.LogError("TestShoot: TestGun 컴포넌트를 찾을 수 없습니다. Inspector에서 직접 할당하거나 같은 GameObject나 자식 GameObject에 TestGun 컴포넌트를 추가하세요.");
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


    void OnShootInput()
    {
        if (!photonView.IsMine) return;
        if (isShooting && gun != null)
            gun.InputFire(true);
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

    public static void SetIsShooting(bool value)
    {
       isShooting = value;
    }

    public static bool GetIsShooting()
    {
        return isShooting;
    }

}
