using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialComplete : MonoBehaviour
{
    [Header("문 이동 설정")]
    [SerializeField] private Transform doorTransform; // 문 Transform (없으면 자신)
    [SerializeField] private float openDistance = 3f; // 위로 얼마나 올라갈지
    [SerializeField] private float openDuration = 0.8f; // 열리는 시간

    private bool isOpened = false;
    private Vector3 initialPosition;

    void Awake()
    {
        if (doorTransform == null)
            doorTransform = transform;
        initialPosition = doorTransform.localPosition;
    }

    public void OpenDoor()
    {
        if (isOpened) return;
        isOpened = true;
        // 문이 아래에서 위로 올라가며 열리게끔
        Vector3 target = initialPosition + Vector3.up * openDistance;
        // 부드러운 이동 (DOTween 미의존, GC적고 간단하게)
        StopAllCoroutines();
        StartCoroutine(MoveDoorCoroutine(doorTransform, target, openDuration));
    }

    private System.Collections.IEnumerator MoveDoorCoroutine(Transform tr, Vector3 target, float duration)
    {
        float t = 0f;
        Vector3 start = tr.localPosition;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;
            // Ease.OutQuad
            float eased = 1f - (1f - lerp) * (1f - lerp);
            tr.localPosition = Vector3.LerpUnclamped(start, target, eased);
            yield return null;
        }
        tr.localPosition = target;
    }
}
