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

    public event Action<int> OnRemainingChanged;
    public int Remaining => remaining;

    private void Awake()
    {
        if (maxUses == -1)
        {
            // 무한
            remaining = -1;
        }
        else
        {
            if (startUses >= 0) remaining = Mathf.Clamp(startUses, 0, maxUses);
            else remaining = maxUses;
        }

        OnRemainingChanged?.Invoke(remaining);
    }

    public bool Use()
    {
        // 무한이면 항상 성공 (상태 변화 없음)
        if (remaining == -1) return true;

        if (remaining <= 0) return false;

        remaining--;
        OnRemainingChanged?.Invoke(remaining);
        return true;
    }

    public void Restore(int amount)
    {
        if (amount <= 0) return;
        if (remaining == -1) return; // 무한은 무시

        // 보정: 남은 횟수는 0..maxUses 범위
        remaining = Mathf.Clamp(remaining + amount, 0, maxUses);
        OnRemainingChanged?.Invoke(remaining);
    }

    // (선택) 외부에서 강제로 세트할 메서드도 있으면 편함
    public void SetRemaining(int newRemaining)
    {
        if (maxUses == -1)
        {
            remaining = -1;
        }
        else
        {
            remaining = Mathf.Clamp(newRemaining, 0, maxUses);
        }
        OnRemainingChanged?.Invoke(remaining);
    }
}
