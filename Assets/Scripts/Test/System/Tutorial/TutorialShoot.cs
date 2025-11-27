using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using DG.Tweening;

public class TutorialShoot : MonoBehaviour
{
    [Header("튜토리얼 UI 참조")]
    [SerializeField] private TutorialUI tutorialUI;
    [Space(10)]

    [Header("튜토리얼 완료 참조")]
    [SerializeField] private TutorialComplete tutorialComplete;
    [Space(10)]

    [Header("타겟 설정")]
    [SerializeField] private int totalTargets = 4; // 총 과녁 개수
    [SerializeField] private List<TargetMove> targetList = new List<TargetMove>(); // 과녁 목록
    [SerializeField] private float targetActivateDelay = 1.5f; // UI 닫힌 후 등장 전 대기 시간
    [SerializeField] private float fadeInDuration = 0.7f;      // 서서히 나타나는 시간


    void OnEnable()
    {
        if (tutorialUI != null)
            tutorialUI.OnTutorialClosed += BeginCounting;
    }

    void OnDisable()
    {
        if (tutorialUI != null)
            tutorialUI.OnTutorialClosed -= BeginCounting;
    }

    private void BeginCounting()
    {
        TutorialStateManager.ShootTriggered = true;
        TutorialStateManager.ShootCompleted = false;
        TutorialStateManager.DestroyedTargets = 0;

        Debug.Log("🎯 타겟 카운팅 시작됨 (튜토리얼 UI 종료)");

        StartCoroutine(ActivateTargetsAfterDelay());
    }

    private IEnumerator ActivateTargetsAfterDelay()
    {
        yield return new WaitForSeconds(targetActivateDelay);

        foreach (var target in targetList)
        {
            if (target == null) continue;

            target.gameObject.SetActive(true);
            Transform t = target.transform;

            // ✅ 0에서 시작해 원래 크기로 Tween
            t.localScale = Vector3.zero;
            t.DOScale(target.originalScale, fadeInDuration).SetEase(Ease.OutBack);

            Renderer rend = target.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                foreach (var mat in rend.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color start = mat.color;
                        Color end = new Color(start.r, start.g, start.b, 1f);
                        mat.DOColor(end, fadeInDuration).SetEase(Ease.InOutSine);
                    }
                    else if (mat.HasProperty("_TintColor"))
                    {
                        Color start = mat.GetColor("_TintColor");
                        Color end = new Color(start.r, start.g, start.b, 1f);
                        mat.DOColor(end, "_TintColor", fadeInDuration).SetEase(Ease.InOutSine);
                    }
                }
            }

            target.EnableMovementAfter(fadeInDuration);
        }

        Debug.Log("✨ 과녁 활성화 연출 완료!");
    }

    public void OnTargetDestroyed()
    {
        if (!TutorialStateManager.ShootTriggered ||
            TutorialStateManager.ShootCompleted)
            return;

        TutorialStateManager.DestroyedTargets++;

        if (TutorialStateManager.DestroyedTargets >= totalTargets)
            CompleteTutorial();
    }

    private void CompleteTutorial()
    {
        TutorialStateManager.ShootCompleted = true;
        Debug.Log("✅ 모든 타겟 파괴됨 — 튜토리얼 완료!");

        if (tutorialUI != null)
            tutorialUI.ShowCompleteSticker();

        if (tutorialComplete != null)
            tutorialComplete.OpenDoor();
    }
}