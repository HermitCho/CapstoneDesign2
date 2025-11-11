# AI 프리팹 완벽 설정 가이드

## 📦 필수 컴포넌트 체크리스트

AI 프리팹에 다음 컴포넌트들이 **모두** 있어야 합니다:

### 루트 오브젝트 (Char1 AI, Char2 AI 등)
```
✅ Transform
✅ PhotonView
   - Observed Components: [Transform, AIHealth, AIBot]
   
✅ Rigidbody
   - Use Gravity: true
   - Is Kinematic: false
   - Constraints: Freeze Rotation X, Y, Z (체크)
   
✅ CapsuleCollider (⚠️ 매우 중요!)
   - Center: (0, 1, 0)
   - Radius: 0.5
   - Height: 2
   - isTrigger: ❌ 체크 해제! (반드시!)
   
✅ NavMeshAgent
   - Speed: 3.5
   - Angular Speed: 300
   - Acceleration: 8
   - Stopping Distance: 0.5
   - Auto Braking: true
   
✅ Animator
   - Controller: C02_Player
   
✅ AIHealth (⚠️ 데미지 받기 위해 필수!)
   - Character Data: (할당 필요)
   
✅ AIBot
   - Character Data: (할당 필요)
   - Gun Data: (할당 필요)
   - Vision Range: 25
   - Attack Range: 12
   - Fire Point: (Gun/FirePoint 할당)
   - Muzzle Flash: (Gun의 ParticleSystem 할당)
   - Animator: (자동 할당됨)

✅ AudioSource (발소리용)

✅ Tag: Player
✅ Layer: Default (⚠️ PlayerPosition 아님!)
```

---

## 🎯 가장 중요한 3가지

### 1. CapsuleCollider 설정 (데미지 받기 위해 필수!)

```
isTrigger: ❌ 체크 해제
```

**이유:** 
- `TestGun`의 레이캐스트는 Trigger를 무시합니다
- isTrigger가 체크되어 있으면 총알이 관통하고 데미지를 받지 않습니다

### 2. AIHealth 컴포넌트 (IDamageable 구현)

```
AIHealth는 루트 오브젝트에 있어야 함
```

**이유:**
- `TestGun.ProcessPelletHit`은 맞은 콜라이더에서 `IDamageable`을 찾습니다
- CapsuleCollider와 AIHealth가 같은 오브젝트에 있어야 합니다

### 3. Layer 설정

```
Layer: Default
❌ PlayerPosition 레이어 사용 금지
```

**이유:**
- `TestGun`은 `PlayerPosition` 레이어를 레이캐스트에서 제외합니다
- AI가 이 레이어에 있으면 총알이 무시됩니다

---

## 🔍 설정 확인 방법

### 단계 1: Inspector 확인

1. **Hierarchy**에서 AI 프리팹 선택
2. **Inspector** 상단 확인:
   ```
   Tag: Player
   Layer: Default
   ```
3. **CapsuleCollider** 찾기:
   ```
   isTrigger: ❌ (체크 해제됨)
   ```
4. **AIHealth** 찾기:
   ```
   Character Data: Char1Data (할당됨)
   ```
5. **AIBot** 찾기:
   ```
   Character Data: Char1Data (할당됨)
   Gun Data: Shotgun (할당됨)
   ```

### 단계 2: PhotonView 확인

**PhotonView** 컴포넌트의 **Observed Components**에 3개가 있어야 합니다:
```
1. Transform (Position/Rotation 동기화)
2. AIHealth (체력 동기화)
3. AIBot (애니메이션 동기화)
```

### 단계 3: NavMeshAgent 확인

```
Speed: 3.5 이상
enabled: ✅ 체크
```

---

## 🎮 테스트 방법

### 1. 움직임 테스트

**실행 후 콘솔 확인:**
```
[AIBot] Char1 AI(Clone) Awake 시작
[AIBot] Char1 AI(Clone) 컴포넌트 체크 - PV:True, Agent:True, Health:True
[AIBot] Char1 AI(Clone) CharacterData 적용됨
[AIBot] Char1 AI(Clone) NavMeshAgent 설정됨 - Speed:3.5
[AIBot] Char1 AI(Clone) 상태: CollectCoin
```

→ AI가 코인을 향해 움직이면 ✅ 성공

### 2. 데미지 테스트

**AI를 총으로 쏜 후 콘솔 확인:**
```
[AIHealth] Char1 AI(Clone) OnDamage 호출됨! Damage: 10, Attacker: 1
[AIHealth] Char1 AI(Clone) 데미지 받음: 10, 현재 체력: 100 → 90
```

→ 데미지 로그가 나타나면 ✅ 성공

### 3. 사망 테스트

**AI를 계속 쏴서 죽이기:**
```
[AIHealth] Char1 AI(Clone) 데미지 받음: 10, 현재 체력: 10 → 0
[AIHealth] Char1 AI(Clone) 사망!
```

→ 10초 후 부활하면 ✅ 성공

---

## ⚠️ 흔한 실수

### 실수 1: isTrigger 체크됨
```
❌ CapsuleCollider - isTrigger: ✅
✅ CapsuleCollider - isTrigger: ❌
```
**증상:** AI를 쏴도 데미지가 안 들어감
**해결:** isTrigger 체크 해제

### 실수 2: Layer가 PlayerPosition
```
❌ Layer: PlayerPosition
✅ Layer: Default
```
**증상:** AI를 쏴도 데미지가 안 들어감
**해결:** Layer를 Default로 변경

### 실수 3: AIHealth 없음
```
❌ AIHealth 컴포넌트 없음
✅ AIHealth 컴포넌트 있음
```
**증상:** AI를 쏴도 데미지가 안 들어감
**해결:** AIHealth 컴포넌트 추가

### 실수 4: CharacterData 미할당
```
❌ Character Data: None
✅ Character Data: Char1Data
```
**증상:** AI가 움직이지 않거나 체력이 0
**해결:** CharacterData 할당

### 실수 5: NavMesh 베이크 안 함
```
❌ Scene에 NavMesh 없음 (파란색 영역 없음)
✅ Scene에 NavMesh 있음 (파란색 영역 있음)
```
**증상:** AI가 움직이지 않음
**해결:** Window → AI → Navigation → Bake

---

## 📝 빠른 설정 순서

1. **AI 프리팹 복제** (기존 Player 프리팹 복제)
2. **이름 변경** (예: Char1 AI)
3. **컴포넌트 제거**
   - CameraController
   - InputManager (있다면)
   - 플레이어 전용 UI 스크립트들
4. **컴포넌트 추가**
   - NavMeshAgent
   - AIHealth
   - AIBot
5. **컴포넌트 설정**
   - CapsuleCollider: isTrigger 체크 해제
   - Tag: Player
   - Layer: Default
   - AIHealth: Character Data 할당
   - AIBot: Character Data, Gun Data 할당
6. **PhotonView 설정**
   - Observed Components에 Transform, AIHealth, AIBot 추가
7. **NavMesh 베이크**
   - Window → AI → Navigation → Bake

---

## ✅ 최종 확인

모든 설정이 완료되면:

**게임 실행 → AI 생성 → 총으로 쏘기**

### 성공 신호
```
[AIBot] Char1 AI(Clone) 상태: CollectCoin
(AI가 움직임)

[AIHealth] Char1 AI(Clone) OnDamage 호출됨! Damage: 10
[AIHealth] Char1 AI(Clone) 데미지 받음: 10, 현재 체력: 100 → 90
(AI 체력이 줄어듦)

[AIHealth] Char1 AI(Clone) 사망!
(AI가 죽음)
```

이 3가지가 모두 작동하면 ✅ **완벽하게 설정됨!**


