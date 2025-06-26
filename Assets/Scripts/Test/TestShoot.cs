using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestShoot : MonoBehaviour
{
    public TestGun gun;

    private bool isShooting = true;

    void OnEnable()
    {
        InputManager.OnShootPressed += OnShootInput;
        InputManager.OnShootCanceledPressed += OnShootCanceledInput;
        InputManager.OnReloadPressed += OnReloadInput;
    }

    void OnDisable()
    {
        InputManager.OnShootPressed -= OnShootInput;
        InputManager.OnShootCanceledPressed -= OnShootCanceledInput;
        InputManager.OnReloadPressed -= OnReloadInput;
    }

    void OnShootInput()
    {
        Debug.Log("총 발사");
        if (isShooting)
            gun.InputFire(true);
    }

    void OnShootCanceledInput()
    {
        Debug.Log("총 발사 취소");
        gun.InputFire(false);
    }

    void OnReloadInput()
    {
        Debug.Log("총 재장전");
        gun.Reload();
    }
}
