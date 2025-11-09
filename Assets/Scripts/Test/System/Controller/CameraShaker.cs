using System.Collections;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    // 싱글톤 패턴을 사용하여 쉽게 접근할 수 있게 합니다.
    public static CameraShaker Instance;

    private Vector3 originalPosition;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 카메라의 원래 위치를 저장
        originalPosition = transform.localPosition;
    }

    // 외부에서 호출하여 카메라를 흔드는 메서드
    public void TriggerShake(float duration, float intensity)
    {
        // 이미 흔들림 코루틴이 실행 중일 수도 있으므로, 기존 코루틴을 멈추고 새로 시작합니다.
        StopAllCoroutines();
        StartCoroutine(ShakeRoutine(duration, intensity));
    }

    IEnumerator ShakeRoutine(float duration, float intensity)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // X, Y, Z 축에 대해 intensity 범위 내에서 무작위 위치를 계산
            // Vector2 또는 Vector3의 Random.insideUnitSphere를 사용할 수도 있습니다.
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            // 카메라의 로컬 위치를 변경합니다.
            transform.localPosition = originalPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime; // 시간 경과 업데이트

            yield return null; // 다음 프레임까지 대기
        }

        // 흔들림이 끝나면 원래 위치로 정확히 복구합니다.
        transform.localPosition = originalPosition;
    }
}