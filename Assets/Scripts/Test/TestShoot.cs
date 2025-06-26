using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestShoot : MonoBehaviour
{

    

    void OnEnable()
    {
        InputManager.OnShootPressed += OnShootInput;
        InputManager.OnShootCanceledPressed += OnShootCanceledInput;
    }

    void OnDisable()
    {
        InputManager.OnShootPressed -= OnShootInput;
        InputManager.OnShootCanceledPressed -= OnShootCanceledInput;
    }



    void OnShootInput()
    {
        Debug.Log("총 발사");
    }

    void OnShootCanceledInput()
    {
        Debug.Log("총 발사 취소");
    }
}
