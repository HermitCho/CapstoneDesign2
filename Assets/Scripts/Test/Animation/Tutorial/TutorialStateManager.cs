using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TutorialStateManager
{
    // ===============================
    // Move 튜토리얼 상태
    // ===============================
    public static bool MoveTriggered = false;
    public static bool MoveCompleted = false;


    // ===============================
    // Shoot 튜토리얼 상태
    // ===============================
    public static bool ShootTriggered = false;
    public static bool ShootCompleted = false;
    public static int DestroyedTargets = 0;


    // ===============================
    // Skill 튜토리얼 상태
    // ===============================
    public static bool SkillTriggered = false;
    public static bool SkillCompleted = false;


    // ===============================
    // Coin 튜토리얼 상태
    // ===============================
    public static bool CoinTriggered = false;
    public static bool CoinCompleted = false;
    public static int CoinGained = 0;


    // ===============================
    // Item 튜토리얼 상태 (Shop + Item 사용)
    // ===============================
    public static bool ItemTriggered = false;
    public static bool ItemCompleted = false;
    public static bool ItemPurchased = false;
    public static bool ItemUsed = false;


    // ===============================
    // Crown 튜토리얼 상태
    // ===============================
    public static bool CrownTriggered = false;
    public static bool CrownCompleted = false;
    public static bool CrownAttached = false;
    public static int CrownStartCoin = 0;


    // ===============================
    // Clear 튜토리얼 상태
    // ===============================
    public static bool ClearTriggered = false;


    // ======================================================
    // 🔥 전체 초기화 (튜토리얼 재시작 / 로비 이동 시 호출)
    // ======================================================
    public static void ResetAll()
    {
        // Move
        MoveTriggered = false;
        MoveCompleted = false;

        // Shoot
        ShootTriggered = false;
        ShootCompleted = false;
        DestroyedTargets = 0;

        // Skill
        SkillTriggered = false;
        SkillCompleted = false;

        // Coin
        CoinTriggered = false;
        CoinCompleted = false;
        CoinGained = 0;

        // Item
        ItemTriggered = false;
        ItemCompleted = false;
        ItemPurchased = false;
        ItemUsed = false;

        // Crown
        CrownTriggered = false;
        CrownCompleted = false;
        CrownAttached = false;
        CrownStartCoin = 0;

        // Clear
        ClearTriggered = false;
    }
}