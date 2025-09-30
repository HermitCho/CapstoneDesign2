using System;
using Unity.VisualScripting;
using UnityEngine;

public static class GameEvents
{
    // 플레이어가 피격당했을 때 발생하는 이벤트
    public static System.Action<Vector3> OnLocalPlayerHit;
    public static Action OnLocalPlayerHeal;
}
