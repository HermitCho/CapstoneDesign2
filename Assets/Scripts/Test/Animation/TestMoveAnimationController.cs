using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMoveAnimationController : MonoBehaviour
{
    void OnEnable()
    {
        // InputManager 이벤트 구독
        InputManager.OnMoveInput += OnMoveInput;
        InputManager.OnXMouseInput += OnMouseInput;
        InputManager.OnJumpPressed += OnJumpInput;
    }

    void OnDisable()
    {
        // InputManager 이벤트 구독 해제
        InputManager.OnMoveInput -= OnMoveInput;
        InputManager.OnXMouseInput -= OnMouseInput;
        InputManager.OnJumpPressed -= OnJumpInput;
    }


    void OnMoveInput(Vector2 moveInput)
    {
        //캐릭터 이동 애니메이션 재생
    }

    void OnMouseInput(Vector2 mouseInput)
    {
        //캐릭터 회전 애니메이션 재생
        // 위아래 회전 애니메이션 재생




        
    }
    
    void OnJumpInput()
    {      
        //캐릭터 점프 애니메이션 재생
    }
}
