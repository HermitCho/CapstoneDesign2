# AI 봇 문제 해결 가이드

## 🚨 주요 문제 및 해결 방법

### 1. AI가 움직이지 않음

#### 원인
- NavMesh가 씬에 베이크되지 않음
- AI가 NavMesh 밖에 스폰됨
- NavMeshAgent 컴포넌트 설정 오류

#### 해결 방법

**1단계: NavMesh 베이크 확인**
```
Window → AI → Navigation
- Bake 탭 선택
- Agent Radius: 0.5
- Agent Height: 2
- Max Slope: 45
- Step Height: 0.4
- "Bake" 버튼 클릭
```

**2단계: 스폰 위치 확인**
- AI가 스폰되는 위치가 NavMesh 위인지 Scene 뷰에서 확인
- NavMesh는 파란색으로 표시됨
- AI가 NavMesh 밖에 있으면 자동으로 가장 가까운 NavMesh로 이동하도록 수정됨

**3단계: NavMeshAgent 설정**
```
AIBot 프리팹 → NavMeshAgent 컴포넌트:
- Base Offset: 0
- Speed: 3.5 (또는 CharacterData.moveSpeed)
- Angular Speed: 300
- Acceleration: 8
- Stopping Distance: 0.5
- Auto Braking: true
- Auto Repath: true
- Height: 2
- Radius: 0.5
- Area Mask: Walkable (체크)
```

**4단계: 디버그 로그 확인**
콘솔에서 다음 로그 확인:
```
[AIBot] {이름} NavMesh 위에 없음! Position: ...
[AIBot] {이름} MoveTo 실패 - NavMesh 없음
[AIBot] {이름} 경로 무효! Target: ...
```

### 2. AI가 데미지를 받지 않음

#### 원인
- PhotonView 설정 오류
- RPC 메서드 이름 불일치
- Collider 설정 오류

#### 해결 방법

**1단계: PhotonView 설정 확인**
```
AIBot 프리팹 → PhotonView 컴포넌트:
✅ Observe Option: Reliable Delta Compressed

Observed Components:
  [0] Transform
  [1] AIHealth
  [2] AIBot
```

**2단계: AIHealth RPC 호환성**
`AIHealth.cs`는 이제 두 가지 RPC를 모두 지원합니다:
```csharp
// AI가 AI를 공격할 때 (ViewID 사용)
[PunRPC]
public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal, int attackerID)

// 플레이어가 AI를 공격할 때 (ActorNr 사용)
[PunRPC]
public void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal, int attackerActorNr)
```

**3단계: Collider 확인**
```
AIBot 프리팹 → Capsule Collider:
- Center: (0, 1, 0)
- Radius: 0.5
- Height: 2
- Is Trigger: false (체크 해제!)
```

**4단계: 레이어 설정**
- AI 프리팹의 Layer: `Default`
- 플레이어 총기의 Hit Mask에 `Default` 포함되어 있는지 확인

**5단계: 디버그 로그 확인**
플레이어가 AI를 공격하면 콘솔에 표시:
```
[AIHealth] {이름} 데미지 받음: 10, 현재 체력: 100 → 90
[AIHealth] {이름} 사망!
```

만약 로그가 안 나온다면:
- PhotonView가 올바르게 설정되지 않음
- RPC가 호출되지 않음
- 마스터 클라이언트가 아님

### 3. AI가 코인을 수집하지 않음

#### 원인
- AI의 Tag가 `Player`가 아님
- Coin의 Trigger Collider 설정 오류

#### 해결 방법

**1단계: Tag 확인**
```
AIBot 프리팹:
- Tag: Player (반드시!)
```

**2단계: Coin Collider 확인**
```
Coin 프리팹:
- Collider:
  - Is Trigger: true (체크!)
```

**3단계: 디버그 로그 확인**
```
[AIBot] {이름} 새 코인 타겟: Coin_01
[AIBot] {이름} 코인 없음, 랜덤 이동
```

### 4. AI가 가까이 가야만 공격함

#### 원인
- Vision Range 설정이 너무 작음
- Attack Range 설정이 너무 작음

#### 해결 방법

**AIBot 컴포넌트 설정 조정:**
```
Vision Range: 25 → 30 (더 넓은 시야)
Attack Range: 12 → 15 (더 긴 사거리)
```

### 5. 애니메이션이 재생되지 않음

#### 원인
- Animator Controller 미할당
- 애니메이터 파라미터 이름 불일치

#### 해결 방법

**1단계: Animator 확인**
```
AIBot 프리팹 → Animator:
- Controller: C02_Player
- Apply Root Motion: false
- Update Mode: Normal
```

**2단계: 애니메이터 파라미터 확인**
```
필수 파라미터:
- MoveX (float)
- MoveY (float)
- Death (bool)
- fire (trigger)
- Reload (trigger)
```

### 6. 애니메이션 이벤트 에러

```
'AI(Clone)' AnimationEvent 'OnReloadEnd' has no receiver!
```

#### 해결 방법
`AIBot.cs`에 이미 포함되어 있습니다:
```csharp
public void OnReloadStart() { }
public void OnReloadEnd() { }
```

### 7. AI가 벽을 통과함

#### 원인
- Rigidbody가 Kinematic이 아님
- Collider가 비활성화됨

#### 해결 방법
```
AIBot 프리팹:
- Rigidbody:
  - Is Kinematic: true (체크!)
  - Use Gravity: false
```

## 🔍 완전한 체크리스트

### 필수 컴포넌트 체크리스트

```
✅ PhotonView
   - Observed Components: Transform, AIHealth, AIBot
   
✅ NavMeshAgent
   - Speed: 3.5
   - Angular Speed: 300
   - Agent Type: Humanoid
   
✅ AIHealth
   - Character Data: 할당됨
   
✅ AIBot
   - Character Data: 할당됨
   - Gun Data: 할당됨
   - Vision Range: 25-30
   - Attack Range: 12-15
   
✅ Rigidbody
   - Is Kinematic: true
   - Use Gravity: false
   
✅ Capsule Collider
   - Is Trigger: false
   
✅ Animator
   - Controller: C02_Player
   
✅ Tag: Player
✅ Layer: Default
```

### 씬 설정 체크리스트

```
✅ NavMesh 베이크됨 (파란색 영역 표시)
✅ 스폰 포인트가 NavMesh 위에 있음
✅ SpawnController에 AI 프리팹 할당됨
```

### 테스트 체크리스트

```
✅ AI가 스폰됨
✅ AI가 움직임 (코인 수집)
✅ AI가 플레이어를 감지하면 공격함
✅ 플레이어가 AI를 공격하면 데미지 입음
✅ AI의 체력이 0이 되면 사망 애니메이션 재생
✅ AI가 10초 후 부활함
✅ AI끼리 서로 공격하고 데미지 입음
```

## 📝 디버그 모드 활성화

### 콘솔 로그 확인

AI의 모든 행동이 로그로 출력됩니다:

```
[AIBot] 움직임:
  - {이름} 새 코인 타겟: Coin_01
  - {이름} 코인 없음, 랜덤 이동
  - {이름} NavMesh 위에 없음! Position: ...
  - {이름} MoveTo 실패 - NavMesh 없음
  - {이름} 경로 무효! Target: ...

[AIHealth] 전투:
  - {이름} 데미지 받음: 10, 현재 체력: 100 → 90
  - {이름} 사망!
```

### 디버그 로그 끄기

로그가 너무 많으면 `AIBot.cs`와 `AIHealth.cs`에서 `Debug.Log` 라인을 주석 처리하세요.

## 🎯 빠른 문제 해결 플로우차트

```
AI가 작동하지 않음
    ↓
AI가 스폰됨?
    NO → SpawnController 확인
    YES ↓
        
AI가 움직임?
    NO → NavMesh 베이크 확인 + NavMeshAgent 설정
    YES ↓
        
AI가 플레이어를 공격?
    NO → Vision Range 확인 + 디버그 로그
    YES ↓
        
플레이어가 AI를 공격하면 데미지 입음?
    NO → PhotonView 설정 + Collider 확인 + 디버그 로그
    YES ↓
        
완벽! ✅
```

## 🚀 마스터 클라이언트 확인

AI는 **마스터 클라이언트에서만 로직이 실행**됩니다.

### 확인 방법

1. 게임 시작
2. 콘솔에서 다음 로그 확인:
   ```
   [마스터 클라이언트] 플레이어 이름
   ```

3. 마스터 클라이언트가 아닌 플레이어는 AI가 움직이는 것만 보임 (동기화)

### 마스터 클라이언트 변경

- 마스터 클라이언트가 방을 나가면 자동으로 다른 플레이어가 마스터가 됨
- AI 소유권도 자동으로 전환됨

## 📞 추가 도움이 필요한 경우

위의 모든 방법을 시도했는데도 문제가 해결되지 않으면:

1. **콘솔 로그 전체 복사**
2. **AI 프리팹 설정 스크린샷**
3. **NavMesh Scene 뷰 스크린샷**

를 준비하세요.

