using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunLockController : MonoBehaviour
{
    public Transform gunPivot;

    [Header("Lock Settings")]
    public bool lockPosition = false;
    public Vector3 lockedLocalPosition;
    public Vector3 lockedLocalRotation;

    void LateUpdate()
    {
        if (lockPosition)
        {
            gunPivot.localPosition = lockedLocalPosition;
            gunPivot.localEulerAngles = lockedLocalRotation;
        }
    }
}