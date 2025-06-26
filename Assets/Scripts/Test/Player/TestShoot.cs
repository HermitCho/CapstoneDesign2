using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestShoot : MonoBehaviour
{

    

    void OnEnable()
    {
        InputManager.OnShootPressed += OnShootInput;
        InputManager.OnShootCanceledPressed += OnShootCanceledInput;

        InputManager.OnZoomPressed += OnZoomPressed;
        InputManager.OnZoomCanceledPressed += OnZoomPressedCanceled;


    }

    void OnDisable()
    {
        InputManager.OnShootPressed -= OnShootInput;
        InputManager.OnShootCanceledPressed -= OnShootCanceledInput;

        InputManager.OnZoomPressed -= OnZoomPressed;
        InputManager.OnZoomCanceledPressed -= OnZoomPressedCanceled;

    }


    #region 총 발사 관련 함수 - 줌 X
    void OnShootInput()
    {
        Debug.Log("총 발사");
    }

    void OnShootCanceledInput()
    {
        Debug.Log("총 발사 취소");
    }
    #endregion



    #region 총 발사 관련 함수 - 줌 O
    void OnZoomPressed()
    {
        Debug.Log("줌 버튼 눌림");
    }

    void OnZoomPressedCanceled()
    {
        Debug.Log("줌 버튼 취소");
    }
    #endregion
}
