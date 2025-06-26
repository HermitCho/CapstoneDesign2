using UnityEngine;
using UnityEngine.InputSystem;
using Michsky.UI.Heat;

// 아이템 UI 관리 전용 스크립트
public class ItemUIManager : MonoBehaviour
{
    [Header("UI 설정")]
    [SerializeField] private bool isItemUIOpen = false;
    
    [Header("디버그")]
    [SerializeField] private bool showDebugInfo = true;




    
    void OnEnable()
    {
        // InputManager 이벤트 구독
        InputManager.OnItemUIPressed += OnItemUIPressed;
        InputManager.OnItemUICanceledPressed += OnItemUICanceled;
    }

    void OnDisable()
    {
        // InputManager 이벤트 구독 해제
        InputManager.OnItemUIPressed -= OnItemUIPressed;
        InputManager.OnItemUICanceledPressed -= OnItemUICanceled;
    }



    #region 아이템 UI 관련 메서드 모음
    // Tab키를 눌렀을 때 (UI 열기) - InputManager 이벤트
    void OnItemUIPressed()
    {      
        // 실제 UI 열기 기능 (현재는 로그로 대체)
        OpenItemUI();
    }
    
    // Tab키를 놓았을 때 (UI 닫기) - InputManager 이벤트
    void OnItemUICanceled()
    {     
        // 실제 UI 닫기 기능 (현재는 로그로 대체)
        CloseItemUI();
    }
    
    // 아이템 UI 열기 (로그로 대체)
    public void OpenItemUI()
    {
        if (!isItemUIOpen)
        {
            isItemUIOpen = true;

            if (showDebugInfo)
                Debug.Log("아이템 UI가 열렸습니다 (로그로 대체)");
        }
    }
    
    // 아이템 UI 닫기 (로그로 대체)
    public void CloseItemUI()
    {
        if (isItemUIOpen)
        {
            isItemUIOpen = false;

            if (showDebugInfo)
                Debug.Log("아이템 UI가 닫혔습니다 (로그로 대체)");
        }
    }
    
    // UI 상태 확인
    public bool IsItemUIOpen => isItemUIOpen;
    
    // 외부에서 UI 상태 변경 가능
    public void SetItemUIOpen(bool isOpen)
    {
        if (isOpen)
            OpenItemUI();
        else
            CloseItemUI();
    }
    #endregion
} 