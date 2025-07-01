using UnityEngine;

/// <summary>
/// 🎮 통합 HUD 패널 설정 가이드
/// HeatUI PanelManager 기반 UI 시스템
/// </summary>
public class HUD_Setup_Guide : MonoBehaviour
{
    /*
     * ============================================================================
     * 🎮 통합 HUD 패널 UI 시스템 설정 가이드
     * ============================================================================
     * 
     * 📋 HeatUI PanelManager를 활용한 새로운 통합 HUD 시스템 설정 방법
     * 
     * ============================================================================
     * 📱 1. 하이어라키 구조
     * ============================================================================
     * 
     * PanelManager (+ PanelManager.cs) [HeatUI]
     * ├── HUDPanel (+ HUDPanel.cs) [Panel Name: "HUD"]
     * │   └── Contents
     * │       ├── CrosshairUI (Image)
     * │       ├── HealthPanel
     * │       │   ├── HealthBar (+ ProgressBar) [HeatUI]
     * │       │   └── HealthText (TextMeshPro)
     * │       ├── ScorePanel
     * │       │   ├── ScoreText (TextMeshPro)
     * │       │   ├── MultiplierText (TextMeshPro)
     * │       │   ├── GameTimeText (TextMeshPro)
     * │       │   ├── AttachStatusText (TextMeshPro)
     * │       │   └── StatusIcon (Image)
     * │       ├── SkillPanel
     * │       │   ├── Skill1-4 Buttons (Button + Image)
     * │       │   ├── Cooldown Overlays (Image, Fill: Radial)
     * │       │   └── Cooldown Texts (TextMeshPro)
     * │       └── ItemModalWindow (+ ModalWindowManager) [HeatUI]
     * ├── PausePanel [구현 예정]
     * └── GameStartPanel [구현 예정]
     * 
     * UIManager (+ InGameUIManager.cs)
     * 
     * ============================================================================
     * ⚙️ 2. 컴포넌트 설정
     * ============================================================================
     * 
     * 📍 PanelManager 설정:
     * - Panel Item: "HUD" -> HUDPanel 할당
     * - Default Panel: HUD
     * 
     * 📍 HUDPanel 설정:
     * - 모든 UI 컴포넌트를 Inspector에서 할당
     * - 크로스헤어, 체력바, 점수, 스킬, 아이템 UI 모두 포함
     * 
     * 📍 InGameUIManager 설정:
     * - Panel Manager: PanelManager 할당
     * - Hud Panel: HUDPanel 할당
     * - Panel Names: "HUD", "Pause", "GameStart"
     * 
     * ============================================================================
     * 🎯 3. 기능
     * ============================================================================
     * 
     * ✅ 통합된 HUD: 모든 UI가 하나의 패널에서 동작
     * ✅ 실시간 업데이트: 점수, 체력, 시간 자동 갱신
     * ✅ 모달 아이템 UI: HUD 위에 모달창으로 표시
     * ✅ 스킬 쿨다운: 시각적 쿨다운 표시
     * ✅ 크로스헤어 애니메이션: 줌 기능 지원
     * ✅ 패널 전환: ESC로 일시정지, Tab으로 아이템
     * 
     * ============================================================================
     * 🔧 4. 입력 연동
     * ============================================================================
     * 
     * - Tab: 아이템 UI 토글
     * - ESC: 일시정지 패널 토글
     * - 스킬 키: 스킬 사용 (1-4번 키)
     * 
     */
    
    [Header("🔍 모니터링")]
    [SerializeField] private InGameUIManager uiManager;
    [SerializeField] private HUDPanel hudPanel;
    
    void Start()
    {
        CheckSetup();
    }
    
    void CheckSetup()
    {
        if (uiManager == null)
            uiManager = FindObjectOfType<InGameUIManager>();
        
        if (hudPanel == null)
            hudPanel = FindObjectOfType<HUDPanel>();
        
        Debug.Log("🎮 통합 HUD 패널 가이드 로드!");
        
        if (uiManager != null)
            Debug.Log("✅ InGameUIManager 발견");
        else
            Debug.LogWarning("❌ InGameUIManager 없음");
        
        if (hudPanel != null)
            Debug.Log("✅ HUDPanel 발견");
        else
            Debug.LogWarning("❌ HUDPanel 없음");
    }
} 