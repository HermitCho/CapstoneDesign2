using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestShoot : MonoBehaviour
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
    }

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

    public static void SetIsShooting(bool value)
    {
       isShooting = value;
    }

    public static bool GetIsShooting()
    {
        return isShooting;
    }

}
