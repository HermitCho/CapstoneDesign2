using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialComplete : MonoBehaviour
{
    [Header("문 이동 설정")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField] private float openDistance = 2f; // 위로 얼마나 올라갈지
    [SerializeField] private float openDuration = 0.8f; // 열리는 시간

    private bool isOpened = false;
    private Vector3 leftInitialPos;
    private Vector3 rightInitialPos;

    void Awake()
    {
        if (leftDoor != null)
            leftInitialPos = leftDoor.localPosition;
        if (rightDoor != null)
            rightInitialPos = rightDoor.localPosition;
    }

    public void OpenDoor()
    {
        if (isOpened) return;
        isOpened = true;

        StopAllCoroutines();

        // 왼쪽 문은 왼쪽(-x)으로, 오른쪽 문은 오른쪽(+x)으로 열기
        if (leftDoor != null)
        {
            Vector3 leftTarget = leftInitialPos + Vector3.left * openDistance;
            StartCoroutine(MoveDoorCoroutine(leftDoor, leftInitialPos, leftTarget, openDuration));
        }

        if (rightDoor != null)
        {
            Vector3 rightTarget = rightInitialPos + Vector3.right * openDistance;
            StartCoroutine(MoveDoorCoroutine(rightDoor, rightInitialPos, rightTarget, openDuration));
        }
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
