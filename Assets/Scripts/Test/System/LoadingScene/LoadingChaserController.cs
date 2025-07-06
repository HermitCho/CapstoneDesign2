using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingChaserController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float jumpPower = 2f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Rigidbody rb;
    
    private bool canMove = true;
    
    // Start is called before the first frame update
    void Start()
    {
        // Rigidbody 컴포넌트 자동 할당
        if (rb == null)
            rb = GetComponent<Rigidbody>();
            
        // 2.5초 후 점프 실행
        StartCoroutine(JumpAfterDelay());
    }

    // Update is called once per frame
    void Update()
    {
        // 이동 가능한 상태일 때만 앞으로 이동
        if (canMove)
        {
            // 캐릭터의 앞 방향으로 이동
            Vector3 moveDirection = transform.forward;
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }                
    }
    
    private IEnumerator JumpAfterDelay()
    {
        yield return new WaitForSeconds(5f);
        
        // 이동 정지
        canMove = false;
        
        // Jump 애니메이션 트리거 발생
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
        
        // 물리적 점프 실행
        if (rb != null)
        {
            rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
        }
    }


 
}
