using System;
using UnityEngine;

public interface IUsableCount
{
    /// <summary>남은 사용 횟수. -1이면 '무한'(infinite)로 해석.</summary>
    int Remaining { get; }

    /// <summary>사용 시도: 성공하면 true(내부 상태 감소), 실패하면 false.</summary>
    bool Use();

    /// <summary>회복(주울 때 등) — 음수 허용하지 않음.</summary>
    void Restore(int amount);

    /// <summary>남은 횟수 변화 알림 (인자: 현재 남은 횟수, -1이면 무한)</summary>
    void SetRemaining(int newRemaining);
    ///
    void SetMaxUses(int newMaxUses, int? newStartUses);
}

public interface IProjectilePreview
{
    void StartPreview(SkillController owner);

    void UpdatePreview(Vector3 origin, Vector3 direction, float initialSpeed);

    void EndPreview();
}


public interface IPlacementPreview
{
    void StartPreview(SkillController owner);
    void UpdatePreview(Vector3 worldPos, Quaternion rot);
    void EndPreview();
}
