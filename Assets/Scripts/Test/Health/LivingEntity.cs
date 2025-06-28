using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LivingEntity : MonoBehaviour, IDamageable
{
    // Start is called before the first frame update
    public CharacterData characterData;
    public float startingHealth{ get; protected set; } // 시작 체력
    public float startingShield { get; protected set; } // 시작 체력
    public float health { get; protected set; } // 현재 체력
    public float shield { get; protected set; } // 현재 추가 방어막
    //public float frontBackMoveSpeed { get; protected set; } // 앞뒤 이동 속도
    //public float leftRIghtMoveSpeed { get; protected set; } // 좌우 이동 속도
    //public float sprintPlusSpeed { get; protected set; } // 달리기 추가 속도
    public bool dead { get; protected set; } // 사망 상태

    public event Action onDeath; // 사망시 발동할 이벤트

    // 생명체가 활성화될때 상태를 리셋
    protected virtual void OnEnable()
    {
        // 사망하지 않은 상태로 시작
        dead = false;
        startingHealth = characterData.startingHealth;
        health = startingHealth;
        startingShield = characterData.startingShield;
        shield = startingShield;
    }

    // 데미지를 입는 기능
    public virtual void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        Debug.Log("shield: " + shield);
        Debug.Log("health: " + health);
        Debug.Log(damage);

        // 데미지만큼 체력 감소
        if (shield >= damage)
        {
            shield -= damage;
        }
        else if (shield < damage && health > 0)
        {
            shield -= damage;
            health += shield;
            if (shield < 0)
            {
                shield = 0;
            }
        }
        else
        {
            health = 0;
        }

        // 체력이 0 이하 && 아직 죽지 않았다면 사망 처리 실행
        if (health <= 0 && !dead)
        {
            Die();
        }
    }

    // 체력을 회복하는 기능
    public virtual void RestoreHealth(float newHealth)
    {
        if (dead)
            // 이미 사망한 경우 체력을 회복할 수 없음
            return;

        // 체력 추가
        if (health + newHealth <= startingHealth)
        {
            health += newHealth;
        }
        else if (health + newHealth > startingHealth)
            health = startingHealth;
    }

    // 사망 처리
    public virtual bool Die()
    {
        // onDeath 이벤트에 등록된 메서드가 있다면 실행
        if (onDeath != null)
        {
            onDeath();
        }

        // 사망 상태를 참으로 변경
        dead = true;
        return true;
    }
}
