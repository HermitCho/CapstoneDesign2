# AI 움직이지 않음 - 빠른 진단 가이드

## 🚨 즉시 확인할 것

### 1단계: 게임 실행 후 콘솔 확인

게임을 실행하면 다음 로그가 나와야 합니다:

#### ✅ 정상 로그
```
[AIBot] Char1 AI(Clone) Awake 시작
[AIBot] Char1 AI(Clone) 컴포넌트 체크 - PV:True, Agent:True, Health:True
[AIBot] Char1 AI(Clone) CharacterData 적용됨
[AIBot] Char1 AI(Clone) NavMeshAgent 설정됨 - Speed:3.5
[AIBot] Char1 AI(Clone) 상태: CollectCoin
[AIBot] Char1 AI(Clone) 새 코인 타겟: Coin_01
```

#### ❌ 문제 로그 패턴

**패턴 1: 컴포넌트 없음**
```
[AIBot] Char1 AI(Clone) 컴포넌트 체크 - PV:True, Agent:False, Health:True
```
→ **해결:** AI 프리팹에 NavMeshAgent 컴포넌트 추가

**패턴 2: CharacterData 없음**
```
[AIBot] Char1 AI(Clone) CharacterData 없음! Data:False, Health:True
```
→ **해결:** AIBot 컴포넌트의 Character Data 필드에 CharacterData 할당

**패턴 3: AI 실행 안함**
```
[AIBot] Char1 AI(Clone) AI 실행 안함 - IsMaster:False, HasPV:True, IsMine:True
```
→ **해결:** 마스터 클라이언트가 아님. 혼자 테스트 중이면 방을 새로 만들기

**패턴 4: NavMesh 없음**
```
[AIBot] Char1 AI(Clone) NavMesh 위에 없음! Position: (0, 0, 0)
```
→ **해결:** NavMesh 베이크 필요

### 2단계: 문제별 해결 방법

## 문제 1: "컴포넌트 체크 - Agent:False"

### 원인
NavMeshAgent 컴포넌트가 없음

### 해결
1. AI 프리팹 선택
2. Add Component → Navigation → NavMesh Agent
3. 설정:
   ```
   Speed: 3.5
   Angular Speed: 300
   Acceleration: 8
   Stopping Distance: 0.5
   Auto Braking: true
   ```

## 문제 2: "CharacterData 없음!"

### 원인
CharacterData가 할당되지 않음

### 해결
1. AI 프리팹 선택
2. AIBot 컴포넌트 찾기
3. Character Data 필드에 다음 중 하나 드래그:
   - `Char1Data`
   - `Char2Data`
   - `Char3Data`
   - `Char4Data`
4. Gun Data 필드에도 할당:
   - `Shotgun`
   - `Rifle`
   - `Pistol`

## 문제 3: "AI 실행 안함 - IsMaster:False"

### 원인
마스터 클라이언트가 아님

### 해결 (단독 테스트)
1. 게임 종료
2. 새 게임 시작
3. 방 생성하면 자동으로 마스터 클라이언트가 됨

### 해결 (멀티플레이)
- 먼저 방에 들어온 사람이 마스터 클라이언트
- 마스터가 나가면 다른 사람에게 자동 전환

## 문제 4: "NavMesh 위에 없음!"

### 원인
NavMesh가 베이크되지 않음

### 해결
1. Unity 상단 메뉴: `Window → AI → Navigation`
2. `Bake` 탭 선택
3. 설정 확인:
   ```
   Agent Radius: 0.5
   Agent Height: 2
   Max Slope: 45
   ```
4. **"Bake" 버튼 클릭** ← 가장 중요!
5. Scene 뷰에서 파란색 영역 확인

## 문제 5: "아무 로그도 안 나옴"

### 원인
AIBot 컴포넌트가 없거나 비활성화됨

### 해결
1. Hierarchy에서 AI 오브젝트 선택
2. Inspector에서 AIBot 컴포넌트 찾기
3. 체크박스가 활성화되어 있는지 확인
4. 없으면 Add Component → AIBot

## 3단계: 완전 체크리스트

AI 프리팹을 선택하고 다음을 확인:

```
✅ PhotonView 컴포넌트 있음
   - Observed Components: Transform, AIHealth, AIBot

✅ NavMeshAgent 컴포넌트 있음
   - Speed > 0

✅ AIHealth 컴포넌트 있음
   - Character Data 할당됨

✅ AIBot 컴포넌트 있음
   - Character Data 할당됨
   - Gun Data 할당됨
   - 컴포넌트 활성화됨 (체크박스)

✅ Animator 컴포넌트 있음
   - Controller: C02_Player

✅ Tag: Player

✅ 씬에 NavMesh 베이크됨 (파란색 영역)
```

## 4단계: 최종 확인

게임 실행 후 콘솔에서:

### 이 로그가 나오면 정상:
```
[AIBot] Awake 시작
[AIBot] 컴포넌트 체크 - PV:True, Agent:True, Health:True
[AIBot] CharacterData 적용됨
[AIBot] NavMeshAgent 설정됨 - Speed:3.5
[AIBot] 상태: CollectCoin
```

### AI가 움직이면:
```
[AIBot] 새 코인 타겟: Coin_01
```

## 🎯 가장 흔한 원인 TOP 3

### 1위: NavMesh 베이크 안 함 (80%)
**해결:** Window → AI → Navigation → Bake 버튼 클릭

### 2위: CharacterData 미할당 (15%)
**해결:** AIBot 컴포넌트에 CharacterData 드래그

### 3위: 마스터 클라이언트 아님 (5%)
**해결:** 방을 새로 만들거나 마스터 클라이언트 기다림

## 📞 여전히 안 되면?

1. **콘솔 로그 전체 복사**
2. **AI 프리팹 Inspector 스크린샷**
3. **Scene 뷰 스크린샷 (NavMesh 확인용)**

위 3가지를 준비하고 확인 요청

