using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialComplete : MonoBehaviour
{
    [Header("문 오브젝트 설정 (Pivot이 힌지에 맞춰져 있어야 함)")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;

    [Header("회전 열림 설정")]
    [SerializeField] private float rotateAngle = 90f;
    [SerializeField] private float openDuration = 15f; // 🔹 천천히 열리도록 시간 늘림 (기존 0.8 → 1.5)
    [SerializeField] private bool invertLeftRotation = false;
    [SerializeField] private bool invertRightRotation = false;

    

    private bool isOpened = false;

    [ContextMenu("Open Door (Test)")]
    public void OpenDoor()
    {
        if (isOpened) return;
        isOpened = true;

        // 🔹 사운드 재생
        AudioManager.Inst.PlayOneShot("SFX_Game_Tutorial_Door");

        if (leftDoor != null)
        {
            float angle = (invertLeftRotation ? -1 : 1) * rotateAngle;
            StartCoroutine(RotateDoor(leftDoor, angle, openDuration));
        }

        if (rightDoor != null)
        {
            float angle = (invertRightRotation ? -1 : 1) * rotateAngle;
            StartCoroutine(RotateDoor(rightDoor, angle, openDuration));
        }
    }

    private IEnumerator RotateDoor(Transform door, float angle, float duration)
    {
        Quaternion startRot = door.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, angle, 0f);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            float eased = 1f - (1f - lerp) * (1f - lerp); // easeOutQuad
            door.localRotation = Quaternion.Slerp(startRot, endRot, eased);
            yield return null;
        }

        door.localRotation = endRot;
    }
}