using System;
using UnityEngine;

/// <summary>
/// Helper wrapper that exposes LivingEntity data/events tailored for bots.
/// This keeps bot specific logic isolated from the generic LivingEntity implementation.
/// </summary>
[RequireComponent(typeof(LivingEntity))]
public class BotLivingEntity : MonoBehaviour
{
    [Header("Bot Stats")]
    [SerializeField] private LivingEntity livingEntity;

    public LivingEntity Source => livingEntity;
    public CharacterData CharacterData => livingEntity != null ? livingEntity.CharacterData : null;
    public bool IsDead => livingEntity != null && livingEntity.IsDead;

    public event Action Died;
    public event Action Revived;

    private void Reset()
    {
        livingEntity = GetComponent<LivingEntity>();
    }

    private void Awake()
    {
        if (livingEntity == null)
        {
            livingEntity = GetComponent<LivingEntity>();
        }
    }

    private void OnEnable()
    {
        if (livingEntity != null)
        {
            livingEntity.OnDeath += HandleDeath;
            livingEntity.OnRevive += HandleRevive;
        }
    }

    private void OnDisable()
    {
        if (livingEntity != null)
        {
            livingEntity.OnDeath -= HandleDeath;
            livingEntity.OnRevive -= HandleRevive;
        }
    }

    private void HandleDeath()
    {
        Died?.Invoke();
    }

    private void HandleRevive()
    {
        Revived?.Invoke();
    }
}