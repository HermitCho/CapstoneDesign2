using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// 승리 플레이어의 Victory 애니메이션을 관리하는 컨트롤러
/// 숫자키 1, 2, 3을 눌러 Win1, Win2, Win3 애니메이션을 재생
/// 포톤 네트워크를 통해 모든 플레이어에게 동기화
/// </summary>
public class VictoryAnimationController : MonoBehaviourPun
{
    [Header("애니메이터 설정")]
    [Tooltip("캐릭터의 Animator 컴포넌트")]
    [SerializeField] private Animator animator;
    
    [Header("Victory 애니메이션 트리거 이름")]
    [SerializeField] private string win1TriggerName = "Win1";
    [SerializeField] private string win2TriggerName = "Win2";
    [SerializeField] private string win3TriggerName = "Win3";
    
    [Header("Victory 애니메이션 상태 이름")]
    [Tooltip("Animator Controller에서 실제 State 이름 (트리거 이름과 다를 수 있음)")]
    [SerializeField] private string win1StateName = "Win1";
    [SerializeField] private string win2StateName = "Win2";
    [SerializeField] private string win3StateName = "Win3";
    
    [Header("애니메이션 지속 시간")]
    [Tooltip("Victory 애니메이션이 재생되는 동안 IK를 비활성화할 시간")]
    [SerializeField] private float animationDuration = 3f;
    
    // 승리 플레이어만 조작 가능 여부
    private bool canControlVictoryAnimation = false;
    
    // FinalIK 컴포넌트 캐싱
    private Component fullBodyBipedIK;
    
    void Start()
    {
        // Animator 자동 할당
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // ✅ FinalIK 컴포넌트 찾기 (리플렉션 사용하여 에셋 의존성 제거)
        FindFullBodyBipedIK();
    }
    
    /// <summary>
    /// Full Body Biped IK 컴포넌트 찾기 (리플렉션 사용)
    /// </summary>
    private void FindFullBodyBipedIK()
    {
        Component[] allComponents = GetComponentsInChildren<Component>();
        foreach (Component comp in allComponents)
        {
            if (comp.GetType().Name == "FullBodyBipedIK")
            {
                fullBodyBipedIK = comp;
                break;
            }
        }
    }
    
    void Update()
    {
        // 승리 플레이어만 입력 받기
        if (!canControlVictoryAnimation) return;
        
        // 숫자키 1, 2, 3 입력 감지
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            PlayVictoryAnimation(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            PlayVictoryAnimation(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            PlayVictoryAnimation(3);
        }
    }
    
    /// <summary>
    /// 승리 애니메이션 조작 활성화 (GameOverController에서 호출)
    /// </summary>
    public void EnableVictoryControl()
    {
        canControlVictoryAnimation = true;
    }
    
    /// <summary>
    /// 승리 애니메이션 조작 비활성화
    /// </summary>
    public void DisableVictoryControl()
    {
        canControlVictoryAnimation = false;
    }
    
    /// <summary>
    /// Victory 애니메이션 재생 (로컬에서 호출 → 네트워크 동기화)
    /// </summary>
    private void PlayVictoryAnimation(int animationIndex)
    {
        if (animator == null) return;
        
        // ✅ RPC를 통해 모든 클라이언트에서 애니메이션 재생
        photonView.RPC("RPC_PlayVictoryAnimation", RpcTarget.All, animationIndex);
    }
    
    /// <summary>
    /// [RPC] 모든 클라이언트에서 Victory 애니메이션 재생
    /// </summary>
    [PunRPC]
    private void RPC_PlayVictoryAnimation(int animationIndex)
    {
        if (animator == null) return;
        
        string triggerName = "";
        string stateName = "";
        
        switch (animationIndex)
        {
            case 1:
                triggerName = win1TriggerName;
                stateName = win1StateName;
                break;
            case 2:
                triggerName = win2TriggerName;
                stateName = win2StateName;
                break;
            case 3:
                triggerName = win3TriggerName;
                stateName = win3StateName;
                break;
            default:
                return;
        }
        
        if (string.IsNullOrEmpty(triggerName) || string.IsNullOrEmpty(stateName)) return;
        
        Debug.Log($"🎭 VictoryAnimationController: Victory 애니메이션 재생 시도 - Index: {animationIndex}, State: {stateName}, Trigger: {triggerName}");
        
        // ✅ FinalIK 일시적으로 비활성화 (애니메이션 재생을 위해)
        DisableIK();
        
        // ✅ 방법 1: CrossFade로 강제 전환 (0.1초 전환 시간)
        // 현재 상태와 무관하게 즉시 전환
        animator.CrossFadeInFixedTime(stateName, 0.1f, 0, 0f);
        
        // ✅ 방법 2: 트리거도 함께 설정 (보험용)
        animator.SetTrigger(triggerName);
        
        // ✅ 방법 3: 강제 업데이트로 트리거 즉시 처리
        animator.Update(0f);
        
        Debug.Log($"✅ VictoryAnimationController: CrossFade 실행 완료 - 현재 상태: {animator.GetCurrentAnimatorStateInfo(0).IsName(stateName)}");
        
        // ✅ 애니메이션 재생 후 IK 다시 활성화 (코루틴)
        StartCoroutine(ReEnableIKAfterAnimation());
    }
    
    /// <summary>
    /// FinalIK 비활성화 (리플렉션 사용)
    /// </summary>
    private void DisableIK()
    {
        if (fullBodyBipedIK == null) return;
        
        try
        {
            // 리플렉션으로 enabled 속성 비활성화
            var enabledProperty = fullBodyBipedIK.GetType().GetProperty("enabled");
            if (enabledProperty != null)
            {
                enabledProperty.SetValue(fullBodyBipedIK, false);
                Debug.Log("🔧 VictoryAnimationController: FinalIK 비활성화");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"VictoryAnimationController: IK 비활성화 실패 - {e.Message}");
        }
    }
    
    /// <summary>
    /// FinalIK 다시 활성화 (리플렉션 사용)
    /// </summary>
    private void EnableIK()
    {
        if (fullBodyBipedIK == null) return;
        
        try
        {
            // 리플렉션으로 enabled 속성 활성화
            var enabledProperty = fullBodyBipedIK.GetType().GetProperty("enabled");
            if (enabledProperty != null)
            {
                enabledProperty.SetValue(fullBodyBipedIK, true);
                Debug.Log("🔧 VictoryAnimationController: FinalIK 재활성화");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"VictoryAnimationController: IK 활성화 실패 - {e.Message}");
        }
    }
    
    /// <summary>
    /// 애니메이션 재생 후 IK 다시 활성화
    /// </summary>
    private IEnumerator ReEnableIKAfterAnimation()
    {
        yield return new WaitForSeconds(animationDuration);
        EnableIK();
    }
}
