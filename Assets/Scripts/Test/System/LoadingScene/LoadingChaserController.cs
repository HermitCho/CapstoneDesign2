using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingChaserController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform chaseTarget;
    [SerializeField] private float jumpPower = 2f;
    [SerializeField] private float jumpPhysicsDelay = 0.3f; // 점프 애니메이션 후 물리적 점프까지의 지연시간
    [SerializeField] private Rigidbody rb;
    
    private bool canMove = true;
    private bool useRootMotion = true;
    
    // Start is called before the first frame update
    void Start()
    {
        // Rigidbody 컴포넌트 자동 할당
        if (rb == null)
            rb = GetComponent<Rigidbody>();
            
        // Animator에서 Apply Root Motion 활성화
        if (animator != null)
        {
            animator.applyRootMotion = true;
        }
            
        // 5초 후 점프 실행
        StartCoroutine(JumpAfterDelay());
    }

    // Update is called once per frame
    void Update()
    {
        // 애니메이션 재생 제어
        if (canMove && animator != null)
        {
            // 달리기 애니메이션 활성화 (Bool 파라미터 사용)
            animator.SetBool("IsRunning", true);
        }
        else if (animator != null)
        {
            // 달리기 애니메이션 비활성화
            animator.SetBool("IsRunning", false);
        }
    }
    
    // Root Motion 처리
    void OnAnimatorMove()
    {
        if (canMove && useRootMotion && animator != null)
        {
            // Root Motion을 Rigidbody에 적용
            if (rb != null)
            {
                rb.MovePosition(rb.position + animator.deltaPosition);
                rb.MoveRotation(rb.rotation * animator.deltaRotation);
            }
            else
            {
                // Rigidbody가 없는 경우 Transform에 직접 적용
                transform.position += animator.deltaPosition;
                transform.rotation *= animator.deltaRotation;
            }
        }
    }
    
    private IEnumerator JumpAfterDelay()
    {
        yield return new WaitForSeconds(5f);
        
        // 이동 정지
        canMove = false;
        
        // Root Motion 비활성화 (점프 시에는 물리 기반으로 처리)
        useRootMotion = false;
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
        
        // Jump 애니메이션 트리거 발생
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
        
        // 점프 애니메이션이 어느 정도 재생된 후 물리적 점프 실행
        yield return new WaitForSeconds(jumpPhysicsDelay);
        
        // 물리적 점프 실행
        if (rb != null)
        {
            rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
        }
    }

    private IEnumerator TriggerFallAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
    }
}
