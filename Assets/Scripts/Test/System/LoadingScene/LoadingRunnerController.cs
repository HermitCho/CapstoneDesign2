using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingRunnerController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform handTransform; // 손 Transform (수동 할당 가능)
    
    [Header("TeddyBear Drop Settings")]
    [SerializeField] private float dropForce = 5f; // 떨어뜨릴 힘
    [SerializeField] private float upwardForce = 2f; // 위쪽 힘
    [SerializeField] private float forwardForce = 3f; // 앞쪽 힘
    [SerializeField] private float rotationForce = 100f; // 회전 힘
    [SerializeField] private float dropDelay = 0.2f; // 넘어지기 시작 후 떨어뜨리는 지연시간
    
    private GameObject teddyBear;
    private bool hasDroppedTeddyBear = false;
    
    // Start is called before the first frame update
    void Start()
    {
        FindTeddyBear();
        StartCoroutine(TriggerFallAfterDelay());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void FindTeddyBear()
    {
        // 자식 오브젝트 중에서 teddybear tag를 가진 오브젝트 찾기
        Transform[] children = GetComponentsInChildren<Transform>();
        
        foreach (Transform child in children)
        {
            if (child.CompareTag("TeddyBear"))
            {
                teddyBear = child.gameObject;
                Debug.Log("테디베어를 찾았습니다: " + teddyBear.name);
                break;
            }
        }
        
        if (teddyBear == null)
        {
            Debug.LogWarning("teddybear tag를 가진 자식 오브젝트를 찾을 수 없습니다.");
        }
    }

    private IEnumerator TriggerFallAfterDelay()
    {
        yield return new WaitForSeconds(2.5f);
        
        // 넘어지기 애니메이션 시작
        animator.SetTrigger("Fall");
        
        // 조금 더 지연 후 테디베어 떨어뜨리기
        yield return new WaitForSeconds(dropDelay);
        DropTeddyBear();
    }
    
    void DropTeddyBear()
    {
        if (teddyBear == null || hasDroppedTeddyBear) return;
        
        hasDroppedTeddyBear = true;
        
        // 부모 관계 해제
        teddyBear.transform.SetParent(null);
        
        // LoadingTeddyBearController 추가 (고급 물리 효과용)
        LoadingTeddyBearController teddyController = teddyBear.GetComponent<LoadingTeddyBearController>();
        if (teddyController == null)
        {
            teddyController = teddyBear.AddComponent<LoadingTeddyBearController>();
        }
        
        // Rigidbody 컴포넌트 추가 또는 가져오기
        Rigidbody rb = teddyBear.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = teddyBear.AddComponent<Rigidbody>();
        }
        
        // Rigidbody 설정
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = 0.5f; // 가벼운 질량
        rb.drag = 0.5f; // 공기 저항
        rb.angularDrag = 0.5f; // 회전 저항
        
        // Collider 확인 및 추가
        Collider col = teddyBear.GetComponent<Collider>();
        if (col == null)
        {
            // 기본 Box Collider 추가
            col = teddyBear.AddComponent<BoxCollider>();
        }
        
        // 테디베어가 떨어질 방향과 힘 계산
        Vector3 dropDirection = CalculateDropDirection();
        
        // 물리적 힘 적용
        rb.AddForce(dropDirection * dropForce, ForceMode.Impulse);
        
        // 회전 힘 적용 (자연스러운 회전을 위해)
        Vector3 randomTorque = new Vector3(
            Random.Range(-rotationForce, rotationForce),
            Random.Range(-rotationForce, rotationForce),
            Random.Range(-rotationForce, rotationForce)
        );
        rb.AddTorque(randomTorque, ForceMode.Impulse);
        
        Debug.Log("테디베어가 떨어집니다!");
    }
    
    Vector3 CalculateDropDirection()
    {
        // 캐릭터의 앞쪽 방향
        Vector3 forward = transform.forward;
        
        // 위쪽 방향
        Vector3 up = Vector3.up;
        
        // 약간 랜덤한 방향 추가
        Vector3 randomDirection = new Vector3(
            Random.Range(-0.5f, 0.5f),
            0,
            Random.Range(-0.3f, 0.3f)
        );
        
        // 최종 방향 계산
        Vector3 finalDirection = (forward * forwardForce + up * upwardForce).normalized + randomDirection;
        
        return finalDirection;
    }
    
    // 외부에서 호출 가능한 메서드들
    public void ForceDropTeddyBear()
    {
        DropTeddyBear();
    }
    
    public bool HasTeddyBear()
    {
        return teddyBear != null && !hasDroppedTeddyBear;
    }
    
    public GameObject GetTeddyBear()
    {
        return teddyBear;
    }
    
    // 테디베어 설정 메서드들
    public void SetDropForce(float force)
    {
        dropForce = force;
    }
    
    public void SetUpwardForce(float force)
    {
        upwardForce = force;
    }
    
    public void SetForwardForce(float force)
    {
        forwardForce = force;
    }
}
