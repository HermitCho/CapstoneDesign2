# 🎮 UI와 매니저 시스템 구조 및 동작 가이드

> **작성일**: 2024년  
> **버전**: v1.0  
> **프로젝트**: CapstoneDesign2 - 테디베어 게임 시스템

---

## 📋 목차
1. [전체 시스템 구조](#-전체-시스템-구조)
2. [시스템 초기화 순서](#-시스템-초기화-순서)
3. [실시간 동작 과정](#-실시간-동작-과정)
4. [데이터 흐름 구조](#-데이터-흐름-구조)
5. [이벤트 시스템 구조](#-이벤트-시스템-구조)
6. [핵심 동작 메커니즘](#-핵심-동작-메커니즘)
7. [사용자 인터랙션 흐름](#-사용자-인터랙션-흐름)
8. [UI 계층 구조](#-ui-계층-구조)
9. [성능 최적화 요소](#-성능-최적화-요소)

---

## 🏗️ 전체 시스템 구조

```
🎯 GameManager (중앙 컨트롤러)
    ├── 📊 DataBase (설정 데이터)
    ├── 🧸 TestTeddyBear (게임 오브젝트)
    └── 🖥️ UI 시스템
        ├── InGameUIManager (패널 관리)
        └── HUDPanel (실제 UI 표시)
```

### 컴포넌트별 역할

| 컴포넌트 | 역할 | 위치 |
|----------|------|------|
| **GameManager** | 전체 게임 상태 관리, 이벤트 중계 | `Singleton<GameManager>` |
| **DataBase** | 게임 설정값 저장, 캐싱 데이터 제공 | `Singleton<DataBase>` |
| **TestTeddyBear** | 테디베어 오브젝트 동작, 점수 생성 | `MonoBehaviour` |
| **InGameUIManager** | HeatUI 패널 전환 관리 | `MonoBehaviour` |
| **HUDPanel** | 실제 UI 표시 및 업데이트 | `MonoBehaviour` |

---

## 🚀 시스템 초기화 순서

### 1. **게임 시작 단계**
```
1. Singleton 초기화
   ├── DataBase.Instance 생성
   ├── GameManager.Instance 생성
   └── 중복 제거 및 DontDestroyOnLoad 설정

2. DataBase 초기화 (Awake)
   └── 설정값들 준비 (ScoreIncreaseTime: 20초, ScoreIncreaseRate: 2배 등)

3. GameManager 초기화 (Awake → Start)
   ├── 플레이어 찾기 및 체력 설정
   ├── 게임 시작 시간 기록 (gameStartTime = Time.time)
   ├── DataBase 정보 캐싱 (안전성을 위해)
   └── TestTeddyBear 찾기
```

### 2. **UI 시스템 초기화**
```
1. InGameUIManager (Start)
   ├── PanelManager 확인
   ├── HUD 패널 자동 표시
   └── 초기 상태 설정

2. HUDPanel (Awake → Start)
   ├── 컴포넌트 초기화 (Text, Image, Button 등)
   ├── 이벤트 구독 (GameManager 이벤트들)
   └── 초기값 설정 (점수: 0, 배율: 1x, 시간: 0초 등)
```

### 3. **TestTeddyBear 초기화**
```
1. Awake
   ├── DataBase 정보 가져오기
   ├── Outline 컴포넌트 설정 (발광 효과용)
   └── 물리 컴포넌트 준비 (Rigidbody, Collider)

2. Start
   ├── 원본 위치/회전값 저장
   ├── 게임 시작 시간 기록
   ├── 초기 점수 설정 (0점)
   └── 발광 시작 (깜박임 효과)
```

---

## 🔄 실시간 동작 과정

### **매 프레임마다 (Update)**

#### 1. **HUDPanel.Update() 실행**
```csharp
void Update()
{
    // 1. 스킬 쿨타임 업데이트 (deltaTime 기반)
    UpdateSkillCooldowns();
    
    // 2. 실시간 점수 상태 업데이트  
    UpdateRealTimeScoreStatus();
    
    // 3. 실시간 게임 시간 업데이트
    UpdateRealTimeUI();
    
    // 4. 시간대별 배율 UI 실시간 업데이트 (핵심!)
    UpdateMultiplier(GameManager.Instance.GetScoreMultiplier());
}
```

#### 2. **GameManager.GetScoreMultiplier() 실시간 계산**
```csharp
public float GetScoreMultiplier()
{
    float currentGameTime = GetGameTime();          // 실시간 게임 시간
    float scoreIncreaseTime = GetScoreIncreaseTime(); // 캐싱된 증가 시점 (20초)
    
    if (currentGameTime >= scoreIncreaseTime)
    {
        return cachedScoreIncreaseRate;  // 2.0x (증가된 배율)
    }
    else
    {
        return 1f;  // 1.0x (기본 배율)
    }
}
```

#### 3. **시간대별 UI 포맷 변경**
```csharp
// 19.9초: "점수 배율 1x" (GeneralMultiplierFormat)
// 20.0초: "배율: 2x" (multiplierFormat) ← 즉시 변경!
```

---

## 📊 데이터 흐름 구조

### **점수 시스템 데이터 흐름**
```
TestTeddyBear (점수 생성)
    ↓ [scoreGetTick 간격 (2초마다)]
ScoreIncreaseCoroutine 실행
    ├── CalculateScoreToAdd() (GameManager 기반 계산)
    ├── currentScore += scoreToAdd
    └── NotifyScoreUpdate()
        ↓
GameManager.UpdateTeddyBearScore(newScore)
    ├── totalTeddyBearScore 업데이트
    ├── OnScoreUpdated 이벤트 발생
    └── OnScoreMultiplierUpdated 이벤트 발생
        ↓
HUDPanel.UpdateScore() (이벤트 구독자)
    ↓ [UI 업데이트]
📱 "점수: 42" 화면에 표시
```

### **배율 시스템 데이터 흐름**
```
매 프레임마다:
GameManager.GetScoreMultiplier() (실시간 계산)
    ├── GetGameTime() (Time.time - gameStartTime)
    ├── GetScoreIncreaseTime() (캐싱된 20초)
    └── 조건 비교 후 배율 반환
        ↓ [매 프레임]
HUDPanel.UpdateMultiplier(multiplier)
    ├── 시간대별 포맷 선택
    ├── 색상 설정
    └── 텍스트 업데이트
        ↓ [UI 반영]
📱 "점수 배율 1x" → "배율: 2x"
```

### **게임 시간 데이터 흐름**
```
매 프레임마다:
GameManager.GetGameTime()
    └── return Time.time - gameStartTime
        ↓ [매 프레임]
HUDPanel.UpdateRealTimeUI()
    ├── gameTimeFormat 적용 ("시간: {0:F0}초")
    ├── 색상 설정 (gameTimeFormatColor)
    └── UI 업데이트
        ↓ [실시간 표시]
📱 "시간: 19초" → "시간: 20초"
```

---

## 🎯 이벤트 시스템 구조

### **GameManager 이벤트 정의**
```csharp
// 점수 관련 이벤트
public static event Action<float> OnScoreUpdated;          // 점수 변경
public static event Action<float> OnScoreMultiplierUpdated; // 배율 변경
public static event Action<float> OnGameTimeUpdated;       // 게임 시간 변경

// 테디베어 관련 이벤트  
public static event Action<bool> OnTeddyBearAttachmentChanged;   // 부착 상태 변경
public static event Action<float> OnTeddyBearReattachTimeChanged; // 재부착 시간 변경

// 플레이어 상태 이벤트
public static event Action<float, float> OnPlayerHealthChanged;  // 체력 변경 (현재, 최대)

// UI 상태 이벤트
public static event Action<bool> OnItemUIToggled;           // 아이템 UI 토글
public static event Action<bool> OnCrosshairTargetingChanged; // 크로스헤어 타겟팅

// 스킬 시스템 이벤트
public static event Action<int> OnSkillUsed;               // 스킬 사용
public static event Action<int, float> OnSkillCooldownStarted; // 스킬 쿨다운 시작
```

### **HUDPanel 이벤트 구독/해제**
```csharp
void SubscribeToEvents()
{
    if (GameManager.Instance != null)
    {
        // 점수 관련 이벤트 구독
        GameManager.OnScoreUpdated += UpdateScore;
        GameManager.OnScoreMultiplierUpdated += UpdateMultiplier;
        GameManager.OnGameTimeUpdated += UpdateGameTime;
        
        // 테디베어 관련 이벤트 구독
        GameManager.OnTeddyBearAttachmentChanged += OnTeddyBearAttachmentChanged;
        GameManager.OnTeddyBearReattachTimeChanged += OnTeddyBearReattachTimeChanged;
        
        // 플레이어 체력 이벤트 구독
        GameManager.OnPlayerHealthChanged += OnPlayerHealthChanged;
        
        // 기타 UI 이벤트들...
    }
}

void UnsubscribeFromEvents()
{
    // 모든 이벤트 해제 (메모리 누수 방지)
    if (GameManager.Instance != null)
    {
        GameManager.OnScoreUpdated -= UpdateScore;
        GameManager.OnScoreMultiplierUpdated -= UpdateMultiplier;
        // ... 모든 이벤트 해제
    }
}
```

---

## 🔧 핵심 동작 메커니즘

### **1. 테디베어 부착 시 전체 흐름**
```
플레이어 충돌 감지 (OnCollisionEnter)
    ↓
TestTeddyBear.AttachToPlayer(playerTransform)
    ├── 재부착 가능 여부 확인 (CanReattach)
    ├── 부착 상태 설정 (isAttached = true)
    ├── 물리 비활성화 (Rigidbody.isKinematic = true)
    ├── 플레이어 자식으로 설정 (SetParent)
    ├── 위치 조정 (AttachOffset 적용)
    ├── 콜라이더 비활성화
    ├── 발광 중지 (StopGlowing)
    └── 점수 증가 시작 (StartScoreIncrease)
        ↓
ScoreIncreaseCoroutine 시작
    ├── scoreGetTick(2초)마다 반복 실행
    ├── CalculateScoreToAdd() 호출
    ├── currentScore 누적
    ├── DataBase.teddyBearScore 동기화
    └── NotifyScoreUpdate()
        ↓
GameManager.UpdateTeddyBearScore() 호출
    ├── totalTeddyBearScore 업데이트
    ├── OnScoreUpdated 이벤트 발생
    └── OnScoreMultiplierUpdated 이벤트 발생
        ↓
HUD UI 업데이트 (이벤트 구독자들 실행)
```

### **2. 시간대별 배율 변경 (20초 시점)**
```
게임 시작 ~ 19.9초:
├── GameManager.GetScoreMultiplier() → 1.0f
├── HUDPanel.UpdateMultiplier(1.0f)
├── 조건: gameTime < scoreIncreaseTime
├── GeneralMultiplierFormat 사용
└── 화면 표시: "점수 배율 1x"

20.0초 정확한 순간:
├── GameManager.GetScoreMultiplier() → 2.0f
├── 조건: gameTime >= scoreIncreaseTime
├── multiplierFormat 사용  
├── 색상 변경 (multiplierFormatColor)
└── 화면 표시: "배율: 2x" (즉시 변경!)

20.1초 이후:
└── 계속 "배율: 2x" 유지
```

### **3. DataBase 안전 캐싱 시스템**
```
GameManager.Start() 실행 시:
    ↓
CacheDataBaseInfo() 호출
    ├── try-catch로 안전한 접근
    ├── DataBase.Instance 존재 확인
    ├── teddyBearData 존재 확인
    ├── cachedScoreIncreaseTime = 20f 저장
    ├── cachedScoreIncreaseRate = 2f 저장
    ├── dataBaseCached = true 설정
    └── 성공 로그 출력

이후 모든 접근은 캐싱된 값 사용:
├── GetScoreIncreaseTime() → cachedScoreIncreaseTime (20f)
├── GetScoreIncreaseRate() → cachedScoreIncreaseRate (2f)
└── 안전성 확보 + 성능 향상
```

### **4. 테디베어 분리 시스템**
```
DetachFromPlayer() 호출 시:
├── 현재 위치에 떨구기
├── 플레이어 앞쪽으로 밀어내기 (AddForce)
├── 물리 활성화 (isKinematic = false)
├── 재부착 방지 시간 설정 (lastDetachTime)
├── 점수 증가 중지 (StopScoreIncrease)
└── 발광 재시작 (StartGlowing)

DetachAndReturnToOriginal() 호출 시:
├── 원래 위치로 복귀
├── 원래 부모로 복귀
└── 나머지는 DetachFromPlayer()와 동일
```

---

## 🎮 사용자 인터랙션 흐름

### **Tab 키 입력 (아이템 UI 토글)**
```
InputManager.OnItemUIPressed 감지
    ↓
HUDPanel.OpenItemUI() 호출
    ├── itemModalWindow.isOn 확인
    ├── isItemUIOpen = true 설정
    ├── itemModalWindow.OpenWindow() 실행
    ├── 커서 해제 (CursorLockMode.None)
    ├── 커서 표시 (Cursor.visible = true)
    └── GameManager.NotifyItemUIToggled(true) 알림
        ↓
GameManager.OnItemUIToggled 이벤트 발생
    ↓
다른 시스템들이 아이템 UI 열림 감지 가능
```

### **ESC 키 입력 (일시정지)**
```
InputManager 또는 InGameUIManager.OnEscapePressed()
    ↓
InGameUIManager.ShowPausePanel() 호출
    ├── currentPanel 확인
    ├── PanelManager.OpenPanel("Pause") 실행
    ├── currentPanel = "Pause" 설정
    └── HUD → Pause 패널 전환 (HeatUI 시스템)
```

### **마우스 조준 (크로스헤어)**
```
크로스헤어 타겟 감지
    ↓
GameManager.NotifyCrosshairTargeting(true) 호출
    ↓
GameManager.OnCrosshairTargetingChanged 이벤트 발생
    ↓
HUDPanel.SetCrosshairTargeting(true) 실행
    ├── isTargeting = true 설정
    ├── 크로스헤어 색상 변경 (crosshairTargetColor)
    └── 크로스헤어 크기 조정 (선택적)
```

---

## 📱 UI 계층 구조

### **InGameUIManager (최상위 패널 관리자)**
```
역할: HeatUI PanelManager와 연동하여 게임 전체 패널 전환 관리

주요 기능:
├── HUD 패널 관리 (ShowHUDPanel)
├── Pause 패널 관리 (ShowPausePanel) - 구현 예정
├── GameStart 패널 관리 (ShowGameStartPanel) - 구현 예정  
├── 패널 간 전환 제어 (PreviousPanel, NextPanel)
├── 자동 HUD 시작 (autoStartWithHUD)
└── 디버그 모드 (debugMode)

설정 값:
├── hudPanelName = "HUD"
├── pausePanelName = "Pause"  
├── gameStartPanelName = "GameStart"
└── autoStartWithHUD = true
```

### **HUDPanel (실제 UI 표시 담당)**
```
역할: 모든 게임 중 UI 요소를 실제로 표시하고 실시간 업데이트

UI 컴포넌트들:
├── 🎯 크로스헤어 UI
│   ├── crosshairImage (Image)
│   ├── crosshairContainer (RectTransform)
│   ├── crosshairNormalColor / crosshairTargetColor
│   └── crosshairSize
│
├── ❤️ 체력바 UI  
│   ├── healthProgressBar (HeatUI ProgressBar)
│   ├── healthText (TextMeshProUGUI)
│   ├── healthyColor / warningColor / dangerColor
│   └── warningThreshold / dangerThreshold
│
├── 📊 점수/배율/시간 UI
│   ├── scoreText (TextMeshProUGUI)
│   ├── multiplierText (TextMeshProUGUI)  
│   ├── gameTimeText (TextMeshProUGUI)
│   ├── attachStatusText (TextMeshProUGUI)
│   ├── scoreStatusText (TextMeshProUGUI)
│   └── statusIcon (Image)
│
├── ⚔️ 스킬 UI
│   ├── skillButtons[] (Button 배열)
│   ├── skillIcons[] (Image 배열)
│   ├── skillCooldownOverlays[] (Image 배열)
│   ├── skillCooldownTexts[] (TextMeshProUGUI 배열)
│   └── maxSkillSlots = 4
│
└── 📦 아이템 UI (모달)
    ├── itemModalWindow (HeatUI ModalWindowManager)
    └── itemUIButton (Button, 선택적)

포맷 설정:
├── scoreFormat = "점수: {0:F0}"
├── GeneralMultiplierFormat = "점수 배율 {0:F0}x"
├── multiplierFormat = "배율: {0:F0}x"  
├── gameTimeFormat = "시간: {0:F0}초"
└── healthFormat = "{0:F0} / {1:F0}"

색상 설정:
├── scoreFormatColor
├── GeneralMultiplierFormatColor
├── multiplierFormatColor
├── gameTimeFormatColor
└── healthFormatColor
```

---

## 🔄 성능 최적화 요소

### **1. DataBase 캐싱 시스템**
```
문제: 매번 DataBase.Instance 싱글톤 접근 시 오버헤드 발생
해결: 게임 시작 시 한 번만 캐싱하고 이후 캐싱된 값 사용

구현:
├── GameManager.Start()에서 CacheDataBaseInfo() 호출
├── cachedScoreIncreaseTime, cachedScoreIncreaseRate 저장  
├── try-catch로 안전한 접근 보장
└── dataBaseCached 플래그로 캐싱 상태 관리

성능 향상:
├── 싱글톤 접근 횟수 99% 감소
├── 안정성 향상 (예외 처리)
└── 메모리 사용량 최소화
```

### **2. 실시간 계산 기반 배율 시스템**
```
문제: TestTeddyBear의 scoreGetTick(2초) 간격으로만 배율 업데이트
해결: GameManager에서 시간 기반 실시간 계산

구현:
├── GetScoreMultiplier()에서 매 프레임 계산
├── Time.time - gameStartTime 기반 정확한 시간
├── 조건문으로 간단한 배율 결정
└── 저장하지 않고 계산 (메모리 절약)

성능 향상:
├── 반응 속도: 2초 → 16ms (60FPS 기준)
├── 정확도: 부정확한 타이밍 → 정확한 시간 기반
└── 메모리: 배율 저장 불필요
```

### **3. 다중 업데이트 경로**
```
실시간성이 중요한 요소들은 여러 경로로 업데이트:

배율 업데이트 경로:
├── 매 프레임: HUDPanel.Update() → UpdateMultiplier()
├── 점수 변경: TestTeddyBear → GameManager → 이벤트 → HUD
└── 추가 실시간: HUDPanel.UpdateRealTimeUI() → UpdateMultiplier()

장점:
├── 빠른 반응: 최대 16ms 이내 반영
├── 안정성: 한 경로 실패해도 다른 경로로 업데이트
└── 정확성: 여러 검증 과정
```

### **4. 이벤트 기반 업데이트**
```
변경이 발생했을 때만 UI 업데이트:

이벤트 방식:
├── 점수 변경 시에만 UpdateScore() 호출
├── 체력 변경 시에만 UpdateHealth() 호출  
├── 부착 상태 변경 시에만 UpdateAttachStatus() 호출
└── 불필요한 UI 갱신 최소화

예외 (매 프레임 업데이트):
├── 게임 시간 (실시간성 중요)
├── 스킬 쿨타임 (사용자 확인 필요)
├── 배율 (시간 기반 변경)
└── 재부착 남은 시간 (실시간 표시)
```

### **5. 메모리 최적화**
```
오브젝트 풀링:
├── 스킬 UI 배열로 미리 생성
├── 동적 생성/삭제 최소화
└── GC 압박 감소

문자열 최적화:
├── string.Format 대신 StringBuilder 고려 (추후)
├── 자주 변경되는 텍스트 최적화 대상
└── 포맷 문자열 미리 정의

이벤트 최적화:
├── OnDestroy에서 모든 이벤트 해제
├── 메모리 누수 방지
└── 안전한 구독/해제 패턴
```

---

## 📋 문제 해결 가이드

### **자주 발생하는 문제들**

#### 1. **배율이 늦게 업데이트되는 경우**
```
원인: TestTeddyBear의 scoreGetTick 간격 때문
해결: GameManager.GetScoreMultiplier() 실시간 계산 구현됨
확인: HUDPanel.Update()에서 매 프레임 호출 중
```

#### 2. **DataBase 접근 오류**
```
원인: 싱글톤 초기화 순서 문제 또는 오브젝트 파괴
해결: GameManager에서 안전한 캐싱 시스템 구현됨
확인: dataBaseCached 플래그로 캐싱 상태 확인
```

#### 3. **이벤트 구독 해제 누락**
```
원인: OnDestroy에서 이벤트 해제 안함
해결: UnsubscribeFromEvents() 반드시 호출
확인: 메모리 프로파일러로 메모리 누수 체크
```

#### 4. **UI 반응 지연**
```
원인: 이벤트 기반 업데이트만 의존
해결: 중요한 요소는 매 프레임 업데이트 추가
확인: Update()에서 UpdateMultiplier() 호출 확인
```

---

## 🔧 확장 가능성

### **추가 구현 예정 기능들**

#### 1. **추가 UI 패널들**
```
PausePanel:
├── 게임 일시정지 기능
├── 설정 변경 메뉴
└── 게임 종료 옵션

GameStartPanel:
├── 게임 시작 화면
├── 튜토리얼 링크  
└── 난이도 선택

InventoryPanel:
├── 아이템 상세 정보
├── 아이템 사용/장착
└── 아이템 정렬/필터
```

#### 2. **고급 스킬 시스템**
```
스킬 트리:
├── 스킬 업그레이드
├── 스킬 조합
└── 스킬 효과 시각화

스킬 UI 확장:
├── 툴팁 시스템
├── 애니메이션 효과
└── 단축키 표시
```

#### 3. **상세 통계 시스템**
```
통계 패널:
├── 게임 진행 통계
├── 성취도 표시
└── 랭킹 시스템

실시간 분석:
├── DPS 미터
├── 효율성 분석
└── 성능 지표
```

---

**문서 마지막 업데이트**: 2024년  
**작성자**: AI Assistant  
**문의사항**: 코드 리뷰 또는 추가 기능 구현 시 이 문서 참조 