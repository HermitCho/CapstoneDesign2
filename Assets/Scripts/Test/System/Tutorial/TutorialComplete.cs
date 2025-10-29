using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialComplete : MonoBehaviour
{
    [Header("문 이동 설정")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField] private float openDistance = 2f; // 문이 이동할 거리
    [SerializeField] private float openDuration = 0.8f; // 열리는 시간

    private enum OpenType
    {
        Slide,
        RotateY
    }

    [Header("열림 방식")]
    [SerializeField] private OpenType openType = OpenType.Slide;

    private enum SlideMode
    {
        ParentXAxis,     // 부모 로컬 X축 기준(기존 Vector3.left/right)
        DoorLocalRight,  // 각 문의 로컬 right 방향(좌문 -right, 우문 +right)
    }

    [Header("이동 방향 모드")]
    [SerializeField] private SlideMode slideMode = SlideMode.DoorLocalRight;

    private bool isOpened = false;
    private Vector3 leftInitialPos;
    private Vector3 rightInitialPos;
    private Quaternion leftInitialRot;
    private Quaternion rightInitialRot;

    void Awake()
    {
        if (leftDoor != null)
        {
            leftInitialPos = leftDoor.localPosition;
            leftInitialRot = leftDoor.rotation;
        }
        if (rightDoor != null)
        {
            rightInitialPos = rightDoor.localPosition;
            rightInitialRot = rightDoor.rotation;
        }
    }

    public void OpenDoor()
    {
        if (isOpened) return;
        isOpened = true;

        StopAllCoroutines();

        if (openType == OpenType.Slide)
        {
            // 왼쪽 문은 왼쪽으로, 오른쪽 문은 오른쪽으로 열기 (모드에 따라 기준 변경)
            if (leftDoor != null)
            {
                Vector3 leftDirLocal = GetSlideDirectionLocal(leftDoor, isLeftDoor: true);
                Vector3 leftTarget = leftInitialPos + leftDirLocal * openDistance;
                StartCoroutine(MoveDoorCoroutine(leftDoor, leftInitialPos, leftTarget, openDuration));
            }

            if (rightDoor != null)
            {
                Vector3 rightDirLocal = GetSlideDirectionLocal(rightDoor, isLeftDoor: false);
                Vector3 rightTarget = rightInitialPos + rightDirLocal * openDistance;
                StartCoroutine(MoveDoorCoroutine(rightDoor, rightInitialPos, rightTarget, openDuration));
            }
        }
        else // RotateY
        {
            if (leftDoor != null)
            {
                Vector3 leftHinge = GetHingePointWorld(leftDoor, isLeftDoor: true);
                Vector3 axis = GetWorldUpAxis(leftDoor);
                float angle = (invertLeftRotation ? 1f : -1f) * rotateAngle;
                StartCoroutine(RotateDoorCoroutine(leftDoor, leftHinge, axis, angle, openDuration));
            }

            if (rightDoor != null)
            {
                Vector3 rightHinge = GetHingePointWorld(rightDoor, isLeftDoor: false);
                Vector3 axis = GetWorldUpAxis(rightDoor);
                float angle = (invertRightRotation ? -1f : 1f) * rotateAngle;
                StartCoroutine(RotateDoorCoroutine(rightDoor, rightHinge, axis, angle, openDuration));
            }
        }
    }

    private Vector3 GetSlideDirectionLocal(Transform door, bool isLeftDoor)
    {
        switch (slideMode)
        {
            case SlideMode.ParentXAxis:
            {
                // 부모 로컬 좌표계에서 X축으로 이동
                return (isLeftDoor ? Vector3.left : Vector3.right);
            }
            case SlideMode.DoorLocalRight:
            default:
            {
                // 문 자신의 right(월드)를 부모 로컬 좌표로 변환 후 사용
                Transform parent = door.parent;
                Vector3 worldRight = door.right; // 월드 기준
                Vector3 parentLocalRight = parent != null
                    ? parent.InverseTransformDirection(worldRight)
                    : worldRight; // 부모가 없으면 월드=로컬로 가정
                return isLeftDoor ? -parentLocalRight : parentLocalRight;
            }
        }
    }

    [Header("회전 열림 설정 (Y축)")]
    [SerializeField] private float rotateAngle = 90f;
    [SerializeField] private Transform leftHingeOverride;   // 지정 시 해당 위치를 힌지로 사용
    [SerializeField] private Transform rightHingeOverride;
    [SerializeField] private bool useParentUpAsAxis = true; // 부모 Up 기준 회전
    [SerializeField] private bool invertLeftRotation = false; // 방향 반전 토글(필요 시 체크)
    [SerializeField] private bool invertRightRotation = false;

    private Vector3 GetWorldUpAxis(Transform door)
    {
        if (useParentUpAsAxis && door.parent != null)
            return door.parent.up; // 월드 기준 축 반환
        return Vector3.up;
    }

    private Vector3 GetHingePointWorld(Transform door, bool isLeftDoor)
    {
        Transform overrideTr = isLeftDoor ? leftHingeOverride : rightHingeOverride;
        if (overrideTr != null)
            return overrideTr.position;

        // 렌더러 바운드로 가장자리 힌지 계산 (월드 좌표)
        var renderers = door.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return door.position;

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            worldBounds.Encapsulate(renderers[i].bounds);

        float edgeX = isLeftDoor ? worldBounds.min.x : worldBounds.max.x;
        return new Vector3(edgeX, worldBounds.center.y, worldBounds.center.z);
    }

    private IEnumerator RotateDoorCoroutine(Transform door, Vector3 hingeWorld, Vector3 worldAxis, float angle, float duration)
    {
        float t = 0f;
        float applied = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;
            float eased = 1f - (1f - lerp) * (1f - lerp); // easeOutQuad
            float target = Mathf.LerpUnclamped(0f, angle, eased);
            float delta = target - applied;
            door.RotateAround(hingeWorld, worldAxis, delta);
            applied = target;
            yield return null;
        }
        // 잔차 보정
        float remain = angle - applied;
        if (Mathf.Abs(remain) > 0.001f)
            door.RotateAround(hingeWorld, worldAxis, remain);
    }

    [ContextMenu("Open Door (Test)")]
    private void ContextOpenDoor()
    {
        OpenDoor();
    }

    private System.Collections.IEnumerator MoveDoorCoroutine(Transform tr, Vector3 startPos, Vector3 target, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;
            // Ease.OutQuad
            float eased = 1f - (1f - lerp) * (1f - lerp);
            tr.localPosition = Vector3.LerpUnclamped(startPos, target, eased);
            yield return null;
        }
        tr.localPosition = target;
    }
}
