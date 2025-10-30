using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialComplete : MonoBehaviour
{
    [Header("문 오브젝트 설정")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;

    [Header("회전 열림 설정 (Y축 기준)")]
    [SerializeField] private float rotateAngle = 90f; // 열릴 각도
    [SerializeField] private float openDuration = 0.8f; // 열리는 시간
    [SerializeField] private Transform leftHingeOverride;   // 왼쪽 문 힌지 포인트
    [SerializeField] private Transform rightHingeOverride;  // 오른쪽 문 힌지 포인트
    [SerializeField] private bool useParentUpAsAxis = true; // 부모의 Up 기준 회전
    [SerializeField] private bool invertLeftRotation = false;
    [SerializeField] private bool invertRightRotation = false;

    private bool isOpened = false;

    private void Awake()
    {
        if (leftDoor == null || rightDoor == null)
        {
            Debug.LogError("[TutorialComplete] Door 오브젝트가 할당되지 않았습니다!");
        }
        if (leftHingeOverride == null || rightHingeOverride == null)
        {
            Debug.LogWarning("[TutorialComplete] 힌지 포인트가 지정되지 않았습니다. 회전 중심이 잘못될 수 있습니다.");
        }
    }

    [ContextMenu("Open Door (Test)")]
    public void OpenDoor()
    {
        if (isOpened) return;
        isOpened = true;

        StopAllCoroutines();

        // 왼쪽 문 회전
        if (leftDoor != null && leftHingeOverride != null)
        {
            Vector3 axis = GetWorldUpAxis(leftDoor);
            float angle = (invertLeftRotation ? 1f : -1f) * rotateAngle;
            StartCoroutine(RotateDoorCoroutine(leftDoor, leftHingeOverride.position, axis, angle, openDuration));
        }

        // 오른쪽 문 회전
        if (rightDoor != null && rightHingeOverride != null)
        {
            Vector3 axis = GetWorldUpAxis(rightDoor);
            float angle = (invertRightRotation ? -1f : 1f) * rotateAngle;
            StartCoroutine(RotateDoorCoroutine(rightDoor, rightHingeOverride.position, axis, angle, openDuration));
        }
    }

    private Vector3 GetWorldUpAxis(Transform door)
    {
        if (useParentUpAsAxis && door.parent != null)
            return door.parent.up;
        return Vector3.up;
    }

    private IEnumerator RotateDoorCoroutine(Transform door, Vector3 hingeWorld, Vector3 worldAxis, float angle, float duration)
    {
        float t = 0f;
        float applied = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            float eased = 1f - (1f - lerp) * (1f - lerp); // easeOutQuad
            float target = Mathf.LerpUnclamped(0f, angle, eased);
            float delta = target - applied;
            door.RotateAround(hingeWorld, worldAxis, delta);
            applied = target;
            yield return null;
        }

        // 회전 오차 보정
        float remain = angle - applied;
        if (Mathf.Abs(remain) > 0.001f)
            door.RotateAround(hingeWorld, worldAxis, remain);
    }
}