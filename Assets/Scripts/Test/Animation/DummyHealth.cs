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

        animator.SetTrigger("Die");

        // 죽었을 때 이벤트 발동
        if (onDeath != null)
            onDeath(this);

        
    }
}
