using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyAnimationController : MonoBehaviour
{
    [Header("애니메이터 설정")]
    [SerializeField] private Animator animator;
    [Header("시간당 애니메이션 출력 확률")]
    [Range(0f, 1f)]
    [SerializeField] private float animationPlayRate = 0.2f;

    [Header("애니메이션 출력 대기 시간 - (초 단위)")]
    [SerializeField] private float secWaitTime = 10f;


    [Header("애니메이션 트리거 이름")]
    [SerializeField] private string[] triggerNames = { "LookingAround", "Waving", "Shrugging" };

    [Header("선택 애니메이션 설정")]
    [Tooltip("Select 애니메이션의 트리거 이름")]
    [SerializeField] private string selectedTriggerNames = "Select";
    [Tooltip("Select 애니메이션의 상태 이름 (트리거 이름과 같으면 동일하게 설정)")]
    [SerializeField] private string selectedStateName = "Select";
    
    //내부 상태 변수
    private float lastAnimationTime;
    private bool isAnimationPlaying = false;

    // Start is called before the first frame update
    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        lastAnimationTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        // secWaitTime이 지났고, 현재 애니메이션이 재생 중이 아닐 때
        if (Time.time - lastAnimationTime >= secWaitTime && !isAnimationPlaying)
        {
            // animationPlayRate 확률로 애니메이션 재생
            if (Random.Range(0f, 1f) <= animationPlayRate)
            {
                PlayRandomAnimation();
            }
            else
            {
                // 애니메이션을 재생하지 않았어도 시간을 리셋
                lastAnimationTime = Time.time;
            }
        }
    }

    private void PlayRandomAnimation()
    {
        if (animator == null || triggerNames.Length == 0) return;

        // 랜덤한 트리거 선택
        string randomTrigger = triggerNames[Random.Range(0, triggerNames.Length)];
        
        // 애니메이션 전환을 더 빠르게 하기 위해 SetTrigger 사용
        animator.SetTrigger(randomTrigger);
        
        // 애니메이션 재생 상태로 설정
        isAnimationPlaying = true;
        lastAnimationTime = Time.time;
        
        // 애니메이션 완료 후 상태를 리셋하기 위한 코루틴 시작
        StartCoroutine(ResetAnimationState(randomTrigger));
    }

    private IEnumerator ResetAnimationState(string triggerName)
    {
        // 애니메이션 완료까지 대기 (더 짧은 시간)
        yield return new WaitForSeconds(0.1f);
        
        // 애니메이션이 실제로 재생 중인지 확인
        while (animator.GetCurrentAnimatorStateInfo(0).IsName(triggerName))
        {
            yield return null; // 한 프레임 대기
        }
        
        // 애니메이션 재생 상태 리셋
        isAnimationPlaying = false;
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
        
        // 애니메이션 재생 상태로 설정
        isAnimationPlaying = true;
        lastAnimationTime = Time.time;
        
        // 애니메이션 완료 후 상태를 리셋하기 위한 코루틴 시작
        StartCoroutine(ResetAnimationState(selectedTriggerNames));
    }

    // 디버깅을 위한 메서드
    private void OnValidate()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }
}
