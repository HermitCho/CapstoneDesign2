# AI 디버깅 로그 가이드

## 📊 로그 분석 방법

AI가 움직이지 않을 때, 콘솔에 출력되는 로그를 순서대로 확인하세요.

---

## 1단계: 초기화 로그

### ✅ 정상 케이스
```
[AIBot] Char1 AI(Clone) Awake 시작
[AIBot] Char1 AI(Clone) 컴포넌트 체크 - PV:True, Agent:True, Health:True
[AIBot] Char1 AI(Clone) CharacterData 적용됨
[AIBot] Char1 AI(Clone) NavMeshAgent 설정됨 - Speed:3.5
```

### ❌ 문제 케이스
```
[AIBot] Char1 AI(Clone) 컴포넌트 체크 - PV:True, Agent:False, Health:True
```
→ **NavMeshAgent 컴포넌트 없음** → AI 프리팹에 NavMeshAgent 추가

```
[AIBot] Char1 AI(Clone) CharacterData 없음! Data:False, Health:True
```
→ **CharacterData 미할당** → AIBot 컴포넌트에 CharacterData 할당

---

## 2단계: AI 실행 여부

### ✅ 정상 케이스
```
[AIBot] Char1 AI(Clone) 상태: CollectCoin
```
→ AI가 정상적으로 실행 중

### ❌ 문제 케이스
```
[AIBot] Char1 AI(Clone) AI 실행 안함 - IsMaster:False, HasPV:True, IsMine:True
```
→ **마스터 클라이언트 아님** → 방을 새로 만들거나 기존 방의 마스터 클라이언트 대기

---

## 3단계: CollectCoins 상태일 때

### ✅ 정상 케이스
```
[AIBot] Char1 AI(Clone) CollectCoins 실행
[AIBot] Char1 AI(Clone) 새 코인 찾기 (현재 타겟: null)
[AIBot] Char1 AI(Clone) FindNearestCoin - 전체 코인 수: 10
[AIBot] Char1 AI(Clone) 수집 가능한 코인: 8개, 가장 가까운 코인: Coin_01, 거리: 5.2
[AIBot] Char1 AI(Clone) 새 코인 타겟: Coin_01 at (10, 0, 5)
[AIBot] Char1 AI(Clone) 코인으로 이동 시작: Coin_01
[AIBot] Char1 AI(Clone) MoveTo 호출 - 목표: (10, 0, 5), 현재위치: (0, 0, 0)
[AIBot] Char1 AI(Clone) Agent 상태: enabled=True, isStopped=False, speed=3.5
[AIBot] Char1 AI(Clone) NavMesh 샘플링 성공: (10, 0, 5)
[AIBot] Char1 AI(Clone) SetDestination 결과: True, PathStatus: PathComplete
[AIBot] Char1 AI(Clone) Agent velocity: 3.5
```
→ **완벽하게 작동 중!**

---

## 4단계: 문제별 로그 패턴

### 문제 A: 코인을 찾지 못함
```
[AIBot] Char1 AI(Clone) CollectCoins 실행
[AIBot] Char1 AI(Clone) FindNearestCoin - 전체 코인 수: 0
[AIBot] Char1 AI(Clone) 수집 가능한 코인: 0개, 가장 가까운 코인: 없음
[AIBot] Char1 AI(Clone) 찾은 코인 없음!
```
**원인:** 씬에 코인이 없음
**해결:** 코인 오브젝트를 씬에 배치하고 `Coin.cs` 스크립트 할당

---

### 문제 B: NavMesh 위에 없음
```
[AIBot] Char1 AI(Clone) CollectCoins 실행
[AIBot] Char1 AI(Clone) 코인으로 이동 시작: Coin_01
[AIBot] Char1 AI(Clone) MoveTo 호출 - 목표: (10, 0, 5), 현재위치: (0, 0, 0)
[AIBot] Char1 AI(Clone) MoveTo 실패 - NavMesh 위에 없음! Position: (0, 0, 0)
```
**원인:** NavMesh가 베이크되지 않았거나 AI가 NavMesh 밖에 스폰됨
**해결:**
1. Window → AI → Navigation → Bake 버튼 클릭
2. AI 스폰 위치가 NavMesh 위에 있는지 확인 (Scene 뷰에서 파란색 영역)

---

### 문제 C: NavMesh 샘플링 실패
```
[AIBot] Char1 AI(Clone) MoveTo 호출 - 목표: (10, 0, 5), 현재위치: (0, 0, 0)
[AIBot] Char1 AI(Clone) Agent 상태: enabled=True, isStopped=False, speed=3.5
[AIBot] Char1 AI(Clone) NavMesh 샘플링 실패! 원본: (10, 0, 5)
```
**원인:** 목표 위치가 NavMesh 밖에 있음
**해결:** 코인 위치가 NavMesh 위에 있는지 확인 (Scene 뷰에서 파란색 영역)

---

### 문제 D: SetDestination 실패
```
[AIBot] Char1 AI(Clone) SetDestination 결과: False, PathStatus: PathInvalid
[AIBot] Char1 AI(Clone) 경로 무효! Target: (10, 0, 5)
```
**원인:** NavMesh 경로를 찾을 수 없음 (장애물로 막힘 또는 NavMesh 단절)
**해결:**
1. NavMesh가 연결되어 있는지 확인
2. AI와 코인 사이에 큰 장애물이 없는지 확인
3. NavMesh Obstacle 설정 확인

---

### 문제 E: Agent 속도가 0
```
[AIBot] Char1 AI(Clone) SetDestination 결과: True, PathStatus: PathComplete
[AIBot] Char1 AI(Clone) Agent velocity: 0
```
**원인 1:** NavMeshAgent가 비활성화됨
**해결:** Inspector에서 NavMeshAgent 컴포넌트 활성화 확인

**원인 2:** Speed가 0으로 설정됨
**해결:** `Agent 상태: speed=3.5` 로그 확인, 0이면 CharacterData의 moveSpeed 값 확인

**원인 3:** isStopped가 true
**해결:** `Agent 상태: isStopped=True` 로그 확인, True이면 다른 스크립트에서 멈추고 있는지 확인

---

## 5단계: 로그가 아예 없을 때

### 케이스 1: Awake 로그도 없음
**원인:** AIBot 스크립트가 없거나 비활성화됨
**해결:**
1. Hierarchy에서 AI 오브젝트 선택
2. Inspector에서 AIBot 컴포넌트 확인
3. 체크박스가 활성화되어 있는지 확인

### 케이스 2: Awake 로그는 있는데 Update 로그 없음
```
[AIBot] Char1 AI(Clone) Awake 시작
(이후 로그 없음)
```
**원인:** AI가 마스터 클라이언트가 아니거나 PhotonView.IsMine이 false
**해결:** 5초마다 출력되는 로그 확인
```
[AIBot] Char1 AI(Clone) AI 실행 안함 - IsMaster:False, HasPV:True, IsMine:True
```

---

## 💡 빠른 체크리스트

AI가 움직이지 않을 때 순서대로 확인:

1. **콘솔에 `Awake 시작` 로그가 있는가?**
   - 없으면: AIBot 컴포넌트 확인
   
2. **`컴포넌트 체크`에서 모두 True인가?**
   - False 있으면: 해당 컴포넌트 추가/할당
   
3. **`AI 실행 안함` 로그가 있는가?**
   - 있으면: 마스터 클라이언트 확인
   
4. **`CollectCoins 실행` 로그가 있는가?**
   - 없으면: AI 상태 확인
   
5. **`FindNearestCoin - 전체 코인 수: 0`인가?**
   - 그렇다면: 씬에 코인 배치
   
6. **`MoveTo 실패 - NavMesh 위에 없음`이 있는가?**
   - 그렇다면: NavMesh 베이크
   
7. **`SetDestination 결과: False`인가?**
   - 그렇다면: NavMesh 경로 확인
   
8. **`Agent velocity: 0`인가?**
   - 그렇다면: Agent 설정 확인 (enabled, speed, isStopped)

---

## 📞 문제 보고 시 필요한 정보

문제가 해결되지 않으면 다음 정보를 제공:

1. **콘솔 로그 전체 복사** (최소 30줄)
2. **AI 프리팹 Inspector 스크린샷**
   - AIBot 컴포넌트
   - NavMeshAgent 컴포넌트
   - PhotonView 컴포넌트
3. **Scene 뷰 스크린샷** (NavMesh 표시 ON)
4. **현재 상황 설명**
   - 혼자 테스트 중인지
   - 멀티플레이 중인지
   - 방을 생성했는지/참가했는지

