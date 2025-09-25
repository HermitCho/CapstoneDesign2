using UnityEngine;

public class DummyHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    private Animator animator;

    // 이벤트 알림용 델리게이트
    public delegate void OnDeath(DummyHealth dummy);
    public static event OnDeath onDeath;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // 애니메이터 트리거 발동
        animator.SetTrigger("Die");

        // 매니저에 알림
        if (onDeath != null)
            onDeath(this);

        // 애니메이션 끝난 후 파괴하고 싶다면 → Animation Event 사용
        // 또는 코루틴으로 딜레이 후 Destroy(gameObject);
    }
}
