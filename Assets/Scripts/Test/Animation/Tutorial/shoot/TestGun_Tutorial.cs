using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGun_Tutorial : MonoBehaviour
{
    [Header("튜토리얼 총기 설정")]
    [SerializeField] private Camera rayCamera;
    [SerializeField] private float range = 100f;
    [SerializeField] private LayerMask targetMask; // 과녁 전용 레이어

    [Header("이펙트")]
    [SerializeField] private GameObject hitEffectPrefab;

    private void Awake()
    {
        if (rayCamera == null)
            rayCamera = Camera.main;
    }

    private void Update()
    {
        // 마우스 왼쪽 클릭 시 레이 발사
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = rayCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, range, targetMask))
            {
                Debug.DrawRay(ray.origin, ray.direction * range, Color.yellow, 0.2f);

                // ✅ 태그가 "Target"인 오브젝트만 반응
                if (hit.collider.CompareTag("Target"))
                {
                    // TargetMove 컴포넌트 찾기
                    TargetMove target = hit.collider.GetComponent<TargetMove>();
                    if (target != null)
                    {
                        // 과녁 제거
                        Destroy(target.gameObject);
                        Debug.Log($"[TestGun_Tutorial] 과녁 '{hit.collider.name}' 명중 → 파괴됨!");

                        // 피격 이펙트 생성
                        if (hitEffectPrefab != null)
                        {
                            GameObject fx = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                            Destroy(fx, 2f);
                        }
                    }
                }
            }
        }
    }
}