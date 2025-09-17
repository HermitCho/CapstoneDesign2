using UnityEngine;
using UnityEngine.EventSystems;
using Michsky.UI.Heat;

/// <summary>
/// ButtonManager와 함께 사용하여 클릭 시 Highlighted 상태를 유지하는 간단한 컴포넌트
/// </summary>
[RequireComponent(typeof(ButtonManager))]
public class ClickableButton : MonoBehaviour, IPointerExitHandler, IDeselectHandler
{
    private ButtonManager buttonManager;
    private bool isClicked = false;
    
    void Start()
    {
        buttonManager = GetComponent<ButtonManager>();
        
        // 버튼 클릭 이벤트에 핸들러 추가
        if (buttonManager != null)
        {
            buttonManager.onClick.AddListener(OnButtonClicked);
        }
    }
    
    /// <summary>
    /// 버튼이 클릭되었을 때 호출
    /// </summary>
    private void OnButtonClicked()
    {
        isClicked = true;
    }
    
    /// <summary>
    /// 클릭 상태를 해제
    /// </summary>
    public void SetUnclicked()
    {
        isClicked = false;
        
        // Normal 상태로 변경
        if (buttonManager != null && buttonManager.isInteractable)
        {
            buttonManager.StartCoroutine("SetNormal");
        }
    }
    
    /// <summary>
    /// 마우스가 버튼에서 나갈 때 - 클릭된 상태라면 Highlighted 유지
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isClicked && buttonManager != null && buttonManager.isInteractable)
        {
            // 잠시 후 Highlighted 상태로 복원
            Invoke(nameof(RestoreHighlight), 0f);
        }
    }
    
    /// <summary>
    /// 포커스가 해제될 때 - 클릭된 상태라면 Highlighted 유지
    /// </summary>
    public void OnDeselect(BaseEventData eventData)
    {
        if (isClicked && buttonManager != null && buttonManager.isInteractable)
        {
            // 잠시 후 Highlighted 상태로 복원
            Invoke(nameof(RestoreHighlight), 0f);
        }
    }
    
    /// <summary>
    /// Highlighted 상태로 복원
    /// </summary>
    private void RestoreHighlight()
    {
        if (isClicked && buttonManager != null && buttonManager.isInteractable)
        {
            buttonManager.StartCoroutine("SetHighlight");
        }
    }
    
    /// <summary>
    /// 현재 클릭 상태 확인
    /// </summary>
    public bool IsClicked()
    {
        return isClicked;
    }
    
    /// <summary>
    /// 외부에서 클릭 상태로 설정 (SelectCharController용)
    /// </summary>
    public void SetClicked()
    {
        isClicked = true;
        
        // Highlighted 상태로 설정
        if (buttonManager != null && buttonManager.isInteractable)
        {
            buttonManager.StartCoroutine("SetHighlight");
        }
    }
}
