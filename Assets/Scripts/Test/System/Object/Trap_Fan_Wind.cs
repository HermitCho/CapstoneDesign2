using UnityEngine;

public class Trap_Fan_Wind : MonoBehaviour
{
    // 캐릭터를 밀어내는 힘의 세기 (인스펙터에서 조절 가능)
    [SerializeField] private float pushForce = 15f;

    // 이 오브젝트에 부착된 Capsule Collider는 Is Trigger가 체크되어 있어야 합니다.
    // 또한, 캐릭터는 Rigidbody와 Collider를 가지고 있어야 충돌이 감지됩니다.
    private void OnTriggerStay(Collider other)
    {
        // 충돌한 오브젝트가 "Player" 태그를 가지고 있는지 확인 (원하는 태그로 변경 가능)
        // 캐릭터의 Rigidbody를 가져옵니다.
        if (other.gameObject.tag == "Player")
        {
            Rigidbody playerRb = other.GetComponent<Rigidbody>();

            // Rigidbody가 존재할 경우에만 처리
            if (playerRb != null)
            {
                // 현재 오브젝트(바람)의 Z축 +방향 (forward 방향)을 가져옵니다.
                Vector3 pushDirection = transform.up;

                // 힘을 가하여 캐릭터를 밀어냅니다.
                // ForceMode.Force를 사용하여 지속적으로 힘을 가합니다.
                playerRb.AddForce(pushDirection * pushForce, ForceMode.Force);
            }
        }
    }

    // 주석: 캐릭터가 닿았을 때 한 번만 밀어내는 것이 아니라
    // 닿아 있는 동안 계속 밀어내기 위해 OnTriggerStay를 사용했습니다.
    // 만약 한 번만 밀어내고 싶다면 OnTriggerEnter를 사용하고 ForceMode.Impulse를 사용할 수 있습니다.
}