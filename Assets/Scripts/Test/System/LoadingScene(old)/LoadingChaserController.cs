using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingChaserController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    
    [Header("Animation Control")]
    [SerializeField] private float jumpDelay = 5f; // 점프하기까지의 지연시간
    
    private bool canMove = true;
    private bool isJumping = false;
    
    // Start is called before the first frame update
    void Start()
    {
        // Rigidbody가 있다면 제거 (Root Motion만 사용)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            DestroyImmediate(rb);
            Debug.Log($"{gameObject.name}: Rigidbody 제거됨 - Root Motion만 사용");
        }
            
        // 지정된 시간 후 점프 실행
        StartCoroutine(JumpAfterDelay());
    }

    // Update is called once per frame
    void Update()
    {
        // 애니메이션 재생 제어
        if (animator != null)
        {
            if (canMove && !isJumping)
            {
                // 달리기 애니메이션 활성화
                animator.SetBool("IsRunning", true);
            }
            else
            {
                // 달리기 애니메이션 비활성화
                animator.SetBool("IsRunning", false);
            }
        }
    }
    

    private IEnumerator JumpAfterDelay()
    {
        yield return new WaitForSeconds(jumpDelay);
        
        // 달리기 중지
        canMove = false;
        isJumping = true;
        
        // Jump 애니메이션 트리거 발생 (애니메이션에 Root Motion이 포함되어야 함)
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
        
        Debug.Log($"{gameObject.name}: 점프 애니메이션 시작 (Root Motion 기반)");
    }
    
    // 애니메이션 이벤트에서 호출될 수 있는 메서드들
    public void OnJumpStart()
    {
        Debug.Log($"{gameObject.name}: 점프 시작");
    }
    
    public void OnJumpEnd()
    {
        isJumping = false;
        Debug.Log($"{gameObject.name}: 점프 종료");
    }
    
    public void OnLanding()
    {
        Debug.Log($"{gameObject.name}: 착지");
    }
    
    // 외부에서 호출 가능한 메서드들
    public void SetCanMove(bool enable)
    {
        canMove = enable;
    }
    
    public void TriggerJump()
    {
        if (animator != null && !isJumping)
        {
            canMove = false;
            isJumping = true;
            animator.SetTrigger("Jump");
        }
    }
    
    public void StopMovement()
    {
        canMove = false;
        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
        }
    }
    
    public bool IsMoving()
    {
        return canMove && !isJumping;
    }
    
    public bool IsJumping()
    {
        return isJumping;
    }
}
