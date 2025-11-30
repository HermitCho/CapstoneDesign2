using UnityEngine;

public class Trap_Fan_Wind : MonoBehaviour
{
    // 캐릭터를 밀어내는 힘의 세기 (인스펙터에서 조절 가능)
    [SerializeField] private float pushForce = 15f;

    // 이 오브젝트에 부착된 Capsule Collider는 Is Trigger가 체크되어 있어야 합니다.
    // 또한, 캐릭터는 Rigidbody와 Collider를 가지고 있어야 충돌이 감지됩니다.
    private void OnTriggerStay(Collider other)
    {
        // ✅ Player 태그 확인
        if (!other.gameObject.CompareTag("Player"))
        {
            return;
        }
        
        // ✅ Rigidbody 확인
        Rigidbody playerRb = other.GetComponent<Rigidbody>();
        if (playerRb == null)
        {
            Debug.LogWarning($"[Trap_Fan_Wind] {other.gameObject.name}에 Rigidbody가 없습니다.");
            return;
        }
        
        // ✅ 바람 방향 계산 (up 방향으로 밀어냄)
        Vector3 pushDirection = transform.up;
        
        // ✅ 힘 적용 (ForceMode.Force는 프레임 독립적이므로 Time.deltaTime 불필요)
        // pushForce 기본값이 15f인데, 더 강한 효과를 위해 배율 적용
        float effectiveForce = pushForce * 3f; // 기본값 15f * 30 = 450f의 힘
        playerRb.AddForce(pushDirection * effectiveForce, ForceMode.Force);
    }
    
  

}