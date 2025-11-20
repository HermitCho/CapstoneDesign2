using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyAnimationController : MonoBehaviour
{
    [Header("애니메이터 설정")]
    [SerializeField] private Animator animator;

    [Header("애니메이션 트리거 이름")]
    [SerializeField] private string[] triggerNames = {"LookingAround", "Waving", "Shrugging" };

    [Header("선택 애니메이션 설정")]
    [Tooltip("Select 애니메이션의 트리거 이름")]
    [SerializeField] private string selectedTriggerNames = "Select";
    [Tooltip("Select 애니메이션의 상태 이름 (트리거 이름과 같으면 동일하게 설정)")]
    [SerializeField] private string selectedStateName = "Select";
    
    //내부 상태 변수
    private int currentDanceIndex = 0;
    private Coroutine danceCoroutine;
    private bool isDancing = false;

    // Start is called before the first frame update
    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        if (triggerNames.Length > 0)
            StartCoroutine(DanceLoop());
    }

    private IEnumerator DanceLoop()
    {
        isDancing = true;

        while (true)
        {
            // 🔹 Animator가 파괴되거나 비활성화되면 루프 종료
            if (animator == null || !animator.isActiveAndEnabled)
                yield break;

            string trigger = triggerNames[currentDanceIndex];

            // 🔹 즉시 전환 (CrossFade 사용)
            animator.CrossFadeInFixedTime(trigger, 0f, 0, 0f);

            // 🔹 Animator 상태가 전환될 수 있도록 한 프레임 대기
            yield return null;

            // 🔹 상태 정보 가져오기 (안전하게)
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            float duration = info.length > 0 ? info.length : 1f;
            
            // 🔹 애니메이션 대기
            yield return new WaitForSeconds(duration - 0.05f);

            // 🔹 다음 애니메이션 인덱스 순환
            currentDanceIndex = (currentDanceIndex + 1) % triggerNames.Length;

        }
    }

    /// <summary>
    /// Select 애니메이션을 재생합니다 (캐릭터 선택 시 호출)
    /// 이 스크립트가 붙어있는 GameObject의 Animator를 사용합니다.
    /// 기존 애니메이션을 중단하고 즉시 Select 애니메이션으로 전환합니다.
    /// </summary>
    public void PlaySelectAnimation()
    {
        if (animator == null || string.IsNullOrEmpty(selectedTriggerNames) || string.IsNullOrEmpty(selectedStateName))
        {
            return;
        }

        // 🔹 댄스 루프 정지
        StopAllCoroutines();
        isDancing = false;
        
        // ✅ 기존 트리거들을 리셋하여 다른 애니메이션 중단
        if (triggerNames != null && triggerNames.Length > 0)
        {
            foreach (string triggerName in triggerNames)
            {
                animator.ResetTrigger(triggerName);
            }
        }
        
        // ✅ CrossFade를 사용하여 즉시 전환 (0초 = 즉시)
        // 현재 애니메이션 상태에 상관없이 바로 Select 애니메이션으로 전환
        animator.CrossFadeInFixedTime(selectedStateName, 0f, 0, 0f);
        
        // ✅ 트리거도 함께 설정 (애니메이션 컨트롤러가 트리거를 사용한다면)
        animator.SetTrigger(selectedTriggerNames);
    }

    // 디버깅을 위한 메서드
    private void OnValidate()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void OnDisable()
    {
        // 🔹 씬 종료나 Play 중단 시 안전하게 코루틴 정리
        if (danceCoroutine != null)
        {
            StopCoroutine(danceCoroutine);
            danceCoroutine = null;
        }
        isDancing = false;
    }

    private void OnDestroy()
    {
        // 🔹 혹시라도 남아 있을 코루틴 완전 정리
        if (danceCoroutine != null)
        {
            StopCoroutine(danceCoroutine);
            danceCoroutine = null;
        }
        isDancing = false;
    }
}

