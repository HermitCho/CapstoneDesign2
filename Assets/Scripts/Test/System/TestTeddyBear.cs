using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTeddyBear : MonoBehaviour
{

    private DataBase.TeddyBearData teddyBearData;
    private Collider colliderTeddyBear;
    private Rigidbody teddyRigidbody;
    
    //테디베어 부착 관련 변수
    private Transform playerTransform;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
   

    //테디베어 발광 상태 확인 변수
    private bool isGlowing = false;
    //테디베어 부착 상태 확인 변수
    private bool isAttached = false;
    
    void Awake()
    {
        teddyBearData = DataBase.Instance.teddyBearData;
        colliderTeddyBear = GetComponent<Collider>();
        teddyRigidbody = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // 원본 위치와 회전값 저장
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isAttached)
        {
            AttachToPlayer(collision.transform);
        }
    }
    
    void AttachToPlayer(Transform player)
    {
        if (isAttached) return;
        
        isAttached = true;
        playerTransform = player;
        
        // 플레이어의 자식으로 설정
        transform.SetParent(player);
        
        // 플레이어 앞에 즉시 부착
        Vector3 targetPosition = player.position + player.forward * teddyBearData.AttachOffset.z + player.up * teddyBearData.AttachOffset.y + player.right * teddyBearData.AttachOffset.x;
        Quaternion targetRotation = player.rotation * Quaternion.Euler(teddyBearData.AttachRotation);
        
        // 즉시 위치와 회전 설정
        transform.position = targetPosition;
        transform.rotation = targetRotation;
        
        // 물리적 상호작용 비활성화 (떨어져 나가는 것 방지)
        if (teddyRigidbody != null)
        {
            teddyRigidbody.isKinematic = true;
            teddyRigidbody.useGravity = false;
        }
        
        // 콜라이더 비활성화 (추가 접촉 방지)
        if (colliderTeddyBear != null)
        {
            colliderTeddyBear.enabled = false;
        }
        
    }



    //테디베어 자체 발광 메서드
    void TeddyBearGlowing()
    {
        if(isGlowing)
        {

        }
    }






    #region 외부 호출용 메서드 모음
    
    // 외부에서 부착 해제 기능 
    public void DetachFromPlayer()
    {
        if (!isAttached) return;
        
        isAttached = false;
        
        // 원본 부모로 복원
        transform.SetParent(originalParent);
        
        // 원본 위치로 복원
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        
        // 물리적 상호작용 다시 활성화
        if (teddyRigidbody != null)
        {
            teddyRigidbody.isKinematic = false;
            teddyRigidbody.useGravity = true;
        }
        
        // 콜라이더 다시 활성화
        if (colliderTeddyBear != null)
        {
            colliderTeddyBear.enabled = true;
        }
        
        playerTransform = null;
        
    }


    //아이템 사용 효과 전용 메서드
    public void TeddyBearGlowingOff()
    {
        
    }

   
    // 현재 부착 상태 확인
    public bool IsAttached()
    {
        return isAttached;
    }

    #endregion
}
