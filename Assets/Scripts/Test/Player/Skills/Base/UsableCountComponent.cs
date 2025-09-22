// UsableCountComponent.cs (no Photon)
using System;
using UnityEngine;

public class UsableCountComponent : MonoBehaviour, IUsableCount
{
    [Tooltip("최대 사용 횟수. -1이면 무한.")]
    [SerializeField] private int maxUses = -1;

    [Tooltip("시작 시 남은 횟수. -1이면 maxUses 또는 무한을 따른다.")]
    [SerializeField] private int startUses = -1;
    private int remaining;

    public int Remaining => remaining;

    private void Awake()
    {
        if (maxUses == -1) remaining = -1;
        else remaining = (startUses >= 0) ? Mathf.Clamp(startUses, 0, maxUses) : maxUses;
    }

    public bool Use()
    {
        if (remaining == -1) return true;
        if (remaining <= 0) return false;

        remaining--;
        return true;
    }

    public void Restore(int amount)
    {
        if (remaining == -1) return;
        remaining = Mathf.Clamp(remaining + amount, 0, maxUses);
    }

    public void SetRemaining(int newRemaining)
    {
        if (maxUses == -1) remaining = -1;
        else remaining = Mathf.Clamp(newRemaining, 0, maxUses);
    }

    public void SetMaxUses(int newMaxUses, int? newStartUses = null)
    {
        maxUses = newMaxUses;
        if (newMaxUses == -1) remaining = -1;
        else remaining = newStartUses ?? newMaxUses;
    }
}
