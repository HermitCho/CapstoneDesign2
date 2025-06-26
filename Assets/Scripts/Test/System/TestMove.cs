using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;



public class TestMove : MonoBehaviour
{

    [Serializable]
    public class Player
    {
        [Tooltip("캐릭터 이동 속도 조절")]
        [Header("캐릭터 이동 속도")]
        [Range(0, 10)]
        public float speed = 5f;

    
        [Tooltip("캐릭터 점프 힘 조절")]
        [Header("캐릭터 점프 힘")]
        [Range(0, 10)]
        public float jumpForce = 5f;

        [Tooltip("캐릭터 점프 관성력 조절")]
        [Header("캐릭터 점프 관성력")]
        [Range(0, 1)]
        public float inertiaForce = 0.5f;

        [Tooltip("캐릭터 x감도 조절")]
        [Header("캐릭터 회전 속도")]
        [Range(0.1f, 20f)]
        public float rotationSpeed = 6f;

        [Tooltip("캐릭터 줌 x감도 조절")]
        [Header("캐릭터 줌 회전 속도")]
        [Range(0.1f, 20f)]
        public float zoomRotationSpeed = 2f;
        
    }


    [SerializeField]
    private Player player = new Player();
    private Rigidbody playerRigidbody;
    private Vector3 moveDirection; // 캐릭터 이동 방향
    private float rotationAmount;
    
    // 마우스 입력 타이머 (마우스 입력이 없으면 정지)
    private float lastMouseInputTime;
    [SerializeField]
    [Tooltip("캐릭터 카메라 회전 감지 시간")]
    [Header("캐릭터 카메라 회전 감지 (초)")]
    [Range(0, 5)]
    private float mouseInputTimeout = 0.1f; // 0.1초 동안 입력이 없으면 정지

    // InputManager 이벤트 구독
    void OnEnable()
    {
        // InputManager 이벤트 구독
        InputManager.OnMoveInput += OnMoveInput;
        InputManager.OnXMouseInput += OnMouseInput;
        InputManager.OnJumpPressed += OnJumpInput;
        InputManager.OnSkillPressed += OnSkillInput;
        InputManager.OnItemPressed += OnItemInput;

        MouseLock();
    }

    void OnDisable()
    {
        // InputManager 이벤트 구독 해제
        InputManager.OnMoveInput -= OnMoveInput;
        InputManager.OnXMouseInput -= OnMouseInput;
        InputManager.OnJumpPressed -= OnJumpInput;
        InputManager.OnSkillPressed -= OnSkillInput;
        InputManager.OnItemPressed -= OnItemInput;
    }

    void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
    }

    

    // Update is called once per frame
    void Update()
    {
        PlayerMove();
        HandleRotation(); // 회전 처리
        

    }

    // InputManager에서 이동 입력 받기
    void OnMoveInput(Vector2 moveInput)
    {
        // 이동 방향 설정
        moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
        moveDirection = moveDirection.normalized;
    }

    // InputManager에서 마우스 입력 받기
    void OnMouseInput(Vector2 mouseInput)
    {
        // 마우스 X축 회전 처리
        float mouseX = mouseInput.x;
        
        rotationAmount = mouseX * Time.deltaTime * player.rotationSpeed;

        if (TestCamController.isZoomed)
        {
            rotationAmount = mouseX * Time.deltaTime * player.zoomRotationSpeed;
        }


        // 마우스 입력 타이머 리셋
        lastMouseInputTime = Time.time;
    }

    //실제 플레이어 움직임
    void PlayerMove()
    {
        // 속도 계산 및 움직임 실행
        Vector3 movement = moveDirection * player.speed * Time.deltaTime;
        transform.Translate(movement);
    }

    // 회전 처리 (별도 메서드로 분리)
    void HandleRotation()
    {   
        if (Time.time - lastMouseInputTime > mouseInputTimeout )
        {
            rotationAmount = 0;
        }
        transform.Rotate(Vector3.up, rotationAmount);
    }

    // InputManager에서 점프 입력 받기
    void OnJumpInput()
    {      
            // 수직 점프 힘
            Vector3 jumpForce = Vector3.up * Mathf.Sqrt(player.jumpForce * -Physics.gravity.y);
            
            // 현재 이동 방향을 플레이어의 로컬 좌표계로 변환하여 점프 방향 설정
            Vector3 localMoveDirection = transform.TransformDirection(moveDirection);
            Vector3 horizontalForce = localMoveDirection * player.speed * player.inertiaForce; 
            
            // 최종 점프 힘 = 수직 힘 + 수평 힘
            Vector3 finalJumpForce = jumpForce + horizontalForce;
            if (CheckGrounded())
            {
                playerRigidbody.AddForce(finalJumpForce, ForceMode.Impulse);
            }            
    }

    // InputManager에서 스킬 입력 받기
    void OnSkillInput()
    {
        Debug.Log("E키가 눌렸습니다 - 스킬 사용");
    }

    // InputManager에서 아이템 입력 받기
    void OnItemInput()
    {
        Debug.Log("Q키가 눌렸습니다 - 아이템 사용");
        // TODO: 선택된 아이템 사용 로직
    }

    private bool CheckGrounded()
    {
        RaycastHit hit;
        if(Physics.Raycast(transform.position, Vector3.down, out hit, 1.3f))
        {
            return true;
        }
        return false;
    }

    public void MouseLock()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}   
