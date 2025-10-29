using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HitEffectUI : MonoBehaviour
{
    public RawImage overlay;  // UI RawImage
    private Material mat;

    void Start()
    {
        // null 체크
        if (overlay == null)
        {
            Debug.LogError("HitEffectUI: overlay RawImage가 할당되지 않았습니다!");
            return;
        }

        if (overlay.material == null)
        {
            Debug.LogError("HitEffectUI: overlay에 Material이 없습니다!");
            return;
        }

        mat = Instantiate(overlay.material);
        overlay.material = mat;

        // 초기 상태에서는 투명하게
        mat.SetFloat("_Intensity", 0f);

        Debug.Log("HitEffectUI 초기화 완료");
    }

    void OnEnable()
    {
        // 이벤트 구독

        GameEvents.OnLocalPlayerHit += OnPlayerHit;
        Debug.Log("구독 완료");
    }

    void OnDisable()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        GameEvents.OnLocalPlayerHit -= OnPlayerHit;
    }

    // 이벤트 핸들러
    private void OnPlayerHit(Vector3 hitWorldDir)
    {
        ShowHit(hitWorldDir);
    }

    public void ShowHit(Vector3 hitWorldDir)
    {
        // null 체크들
        if (mat == null)
        {
            Debug.LogError("HitEffectUI: Material이 초기화되지 않았습니다!");
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogError("HitEffectUI: MainCamera를 찾을 수 없습니다!");
            return;
        }

        // 월드 → 화면 방향 변환
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        Vector3 camUp = Camera.main.transform.up;

        // 공격 방향을 카메라 좌표계 기준으로 변환
        float rightComponent = Vector3.Dot(hitWorldDir, camRight);
        float upComponent = Vector3.Dot(hitWorldDir, camUp);

        // UI 화면 좌표계로 변환 (X: 오른쪽이 양수, Y: 위쪽이 양수)
        Vector2 screenDir = new Vector2(rightComponent, upComponent);
        if (screenDir.sqrMagnitude > 0.0001f)
            screenDir.Normalize();
        else
            screenDir = Vector2.up; // 기본값

        Debug.Log($"HitEffectUI - hitWorldDir: {hitWorldDir}");
        Debug.Log($"Camera - Right: {camRight}, Up: {camUp}, Forward: {camForward}");
        Debug.Log($"Components - Right: {rightComponent:F2}, Up: {upComponent:F2}");
        Debug.Log($"Final screenDir: {screenDir}");

        // 셰이더 프로퍼티 설정
        mat.SetVector("_HitDir", new Vector4(screenDir.x, screenDir.y, 0, 0));
        mat.SetFloat("_Intensity", 1f);
        mat.SetColor("_Color", new Color(1f, 0f, 0f, 0.8f)); // 빨간색, 약간 투명

        // 디버깅: Inspector에서 Material 속성 확인용
        Debug.Log($"셰이더 속성 설정됨 - HitDir: {screenDir}, Intensity: 1.0, Spread: 0.3, EdgeFade: 0.5");

        StopAllCoroutines();
        StartCoroutine(FadeOutCoroutine());

        Debug.Log("HitEffectUI 이펙트 시작!");

    }

    IEnumerator FadeOutCoroutine()
    {
        float t = 0f;
        float duration = 1f; // 1초 동안 유지
        mat.SetFloat("_Intensity", 1f);

        while (t < duration)
        {
            t += Time.deltaTime;
            float intensity = Mathf.Lerp(1f, 0f, t / duration);
            mat.SetFloat("_Intensity", intensity);
            yield return null;
        }

        mat.SetFloat("_Intensity", 0f);
    }

    // 테스트용 메서드 (키보드 입력으로 테스트)
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("I키 눌림 - 왼쪽에서 공격 테스트");
            ShowHit(-Vector3.right); // 왼쪽에서 공격 (화면 오른쪽에 효과)
        }
    }

    // 기본 가시성 테스트
    void TestBasicVisibility()
    {
        if (overlay == null)
        {
            Debug.LogError("overlay가 null입니다!");
            return;
        }

        if (mat == null)
        {
            Debug.LogError("mat가 null입니다!");
            return;
        }

        Debug.Log($"RawImage 활성화 상태: {overlay.gameObject.activeInHierarchy}");
        Debug.Log($"RawImage enabled: {overlay.enabled}");
        Debug.Log($"RawImage color: {overlay.color}");
        Debug.Log($"Canvas 활성화 상태: {overlay.canvas?.gameObject.activeInHierarchy}");

        // 단순히 빨간색으로 채우기 테스트
        mat.SetFloat("_Intensity", 1f);
        mat.SetColor("_Color", Color.red);
        mat.SetVector("_HitDir", Vector4.one);

        Debug.Log("기본 가시성 테스트 완료 - 빨간색이 보여야 함");

        // 5초 후 원래대로
        Invoke(nameof(ClearEffect), 1f);
    }

    void ClearEffect()
    {
        if (mat != null)
        {
            mat.SetFloat("_Intensity", 0f);
            Debug.Log("이펙트 클리어 완료");
        }
    }
}