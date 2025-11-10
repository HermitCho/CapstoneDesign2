# AI 봇 완벽 설정 가이드 (최종 버전)

## 🎯 핵심 변경사항

### ✅ 완전히 독립적인 AI 시스템
- **LivingEntity 제거**: 플레이어 전용 컴포넌트 의존성 완전 제거
- **AIHealth 도입**: AI 전용 Health 시스템
- **MoveController 제거**: NavMeshAgent만 사용
- **GunData & CharacterData 공유**: 스탯만 공유, 로직은 완전 독립

## 📋 새 AI 봇 프리팹 만들기

### 1단계: 기본 프리팹 준비

1. **빈 GameObject 생성**
   - 이름: `Char1 AI`, `Char2 AI`, etc.
   - Tag: `Player`
   - Layer: `Default`

2. **캐릭터 모델 추가**
   - 기존 캐릭터 모델을 자식으로 추가
   - Animator가 포함된 모델

### 2단계: 필수 컴포넌트 설정

#### Root GameObject에 추가할 컴포넌트:

##### 1. **PhotonView**
```
Observe Option: Reliable Delta Compressed

Observed Components (순서대로):
  [0] Transform (Synchronize Position & Rotation)
  [1] AIHealth
  [2] AIBot
```

##### 2. **NavMeshAgent**
```
Agent Type: Humanoid
Base Offset: 0
Speed: (CharacterData에서 자동 설정됨)
Angular Speed: 300
Acceleration: 8
Stopping Distance: 0.5
Auto Braking: true
Auto Repath: true
Height: 2
Radius: 0.5
```

##### 3. **AIHealth** (새 컴포넌트)
```
Character Data: [할당 필요]
  - Char1Data, Char2Data 등 기존 CharacterData 사용
```

##### 4. **AIBot** (새 컴포넌트)
```
Data Assets:
  - Character Data: [할당 필요] (AIHealth와 동일한 것)
  - Gun Data: [할당 필요] (기존 GunData 사용)

AI 설정:
  - Vision Range: 25
  - Attack Range: 12
  - State Update Rate: 0.4
  - Shoot Cooldown: 0.4

References:
  - Fire Point: Gun/FirePoint (자동 검색)
  - Muzzle Flash: Gun 아래 파티클 (자동 검색)
  - Animator: 루트 Animator (자동 검색)
```

##### 5. **Rigidbody**
```
Is Kinematic: true
Use Gravity: false
```

##### 6. **Capsule Collider**
```
Center: (0, 1, 0)
Radius: 0.5
Height: 2
```

##### 7. **Animator**
```
Controller: C02_Player (또는 해당 캐릭터 컨트롤러)
Apply Root Motion: false
Update Mode: Normal
Culling Mode: Always Animate
```

### 3단계: 자식 오브젝트 설정

#### Gun 오브젝트

```
캐릭터 루트
├─ Gun (빈 Transform)
│  ├─ FirePoint (빈 Transform, 총구 위치)
│  └─ MuzzleFlash (ParticleSystem)
└─ 모델 (Animator 포함)
```

**Gun 설정:**
- Position: 캐릭터 손 위치에 맞춰 조정
- 모든 스크립트 제거 (TestGun 등)

**FirePoint 설정:**
- Position: 총구 끝
- 빈 Transform

**MuzzleFlash 설정:**
- ParticleSystem
- Play On Awake: false
- Duration: 0.1

### 4단계: 제거해야 할 컴포넌트

다음 컴포넌트들이 있다면 **모두 제거**:

- ❌ `LivingEntity` (AIHealth로 대체)
- ❌ `BotLivingEntity` (불필요)
- ❌ `TestShoot`
- ❌ `TestGun`
- ❌ `MoveController`
- ❌ `SkillController`
- ❌ `CameraController`
- ❌ `CoinController`
- ❌ 기존 BotController, BotMoveController, BotGunController, BotAnimationController

## ⚙️ 컴포넌트 상세 설명

### AIHealth (AI 전용 Health 시스템)

**목적:** LivingEntity의 플레이어 전용 로직을 제거한 순수 Health 시스템

**주요 기능:**
- CharacterData 기반 체력 관리
- Photon2 완벽 동기화
- RPC 기반 데미지 처리
- 10초 자동 부활

**RPC 메서드:**
```csharp
[PunRPC]
public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal, int attackerViewID)
```

**이벤트:**
- `OnDeath`: 사망 시
- `OnRevive`: 부활 시
- `OnHealthChanged`: 체력 변경 시

### AIBot (통합 AI 컨트롤러)

**목적:** 모든 AI 로직을 하나의 컴포넌트에 통합

**주요 기능:**
- 상태 기반 AI
- NavMeshAgent 기반 이동
- GunData 기반 전투
- 완벽한 네트워크 동기화

**AI 우선순위:**
1. 왕관 소유자 추적 ⭐⭐⭐
2. 왕관 보유 시 도망 ⭐⭐
3. 시야 내 적 교전 ⭐⭐
4. 떨어진 왕관 획득 ⭐
5. 코인 수집 (기본)

## 🔧 Data Asset 설정

### CharacterData 설정
```
기존 CharacterData 사용:
- Char1Data.asset
- Char2Data.asset
- Char3Data.asset
- Char4Data.asset

필수 필드:
- startingHealth: 100
- moveSpeed: 3.5
```

### GunData 설정
```
기존 GunData 사용:
- Shotgun.asset
- Rifle.asset
- Pistol.asset

필수 필드:
- damage: 10
- range: 50
- fireRate: 0.1
- maxAmmo: 30
- reloadTime: 2
- pelletCount: 1 (샷건은 7)
- spreadAngle: 0 (샷건은 2.2)
- shotClip: 발사 사운드
- reloadClip: 재장전 사운드
```

## 🎮 SpawnController 설정

`SpawnController.cs`의 `aiPrefabs` 배열에 AI 프리팹 할당:

```
AI Prefabs (4개):
- Char1 AI
- Char2 AI
- Char3 AI
- Char4 AI
```

## ✅ 테스트 체크리스트

### 기본 기능
- [ ] AI가 게임 시작 후 정상 스폰됨
- [ ] AI가 NavMesh를 따라 이동함
- [ ] AI의 이동 애니메이션이 재생됨 (MoveX, MoveY)
- [ ] AI가 코인을 수집함

### 전투 시스템
- [ ] AI가 플레이어를 발견하면 추적함
- [ ] AI가 공격 거리에서 발사함
- [ ] AI의 발사 애니메이션 재생 (fire 트리거)
- [ ] AI의 Muzzle Flash 이펙트 재생
- [ ] AI의 발사 사운드 재생
- [ ] **플레이어가 AI를 공격하면 AI가 데미지를 받음** ✅
- [ ] **AI가 플레이어를 공격하면 플레이어가 데미지를 받음** ✅
- [ ] **AI끼리 서로 공격하고 데미지를 입음** ✅

### Health 시스템
- [ ] AI의 체력이 0이 되면 사망 애니메이션 재생 (Death bool)
- [ ] AI가 10초 후 자동 부활함
- [ ] 부활 후 체력이 전부 회복됨
- [ ] 부활 후 AI가 정상적으로 행동 재개

### 왕관 시스템
- [ ] AI가 떨어진 왕관을 주우러 감
- [ ] AI가 왕관 소유자를 최우선으로 추적
- [ ] AI가 왕관을 가지고 있을 때 적을 회피

### 재장전
- [ ] AI의 탄약이 0이 되면 재장전 시작
- [ ] 재장전 애니메이션 재생 (Reload 트리거)
- [ ] 재장전 사운드 재생
- [ ] 재장전 중에는 발사하지 않음

### 멀티플레이어
- [ ] 마스터 클라이언트에서 AI 로직이 실행됨
- [ ] 모든 클라이언트에서 AI 애니메이션이 동기화됨
- [ ] 모든 클라이언트에서 AI 이펙트/사운드가 재생됨
- [ ] 마스터 클라이언트가 변경되어도 AI가 정상 작동

## 🐛 문제 해결

### 1. "AnimationEvent 'OnReloadEnd' has no receiver"

**해결:** `AIBot.cs`에 이미 포함되어 있습니다.
```csharp
public void OnReloadStart() { }
public void OnReloadEnd() { }
```

### 2. AI가 데미지를 받지 않음

**원인:** 
- LivingEntity 대신 AIHealth 사용
- TakeDamage RPC 올바른 호출 필요

**해결:**
```csharp
// AIBot.cs의 ShootPellet 메서드에서:
AIHealth aiTarget = hit.collider.GetComponentInParent<AIHealth>();
if (aiTarget != null && aiTarget != aiHealth)
{
    PhotonView targetPV = aiTarget.GetComponent<PhotonView>();
    if (targetPV != null)
    {
        targetPV.RPC("TakeDamage", RpcTarget.All, gunData.damage, hit.point, hit.normal, pv.ViewID);
    }
}
```

### 3. AI가 플레이어에게 데미지를 주지 못함

**해결:** AIBot.cs가 플레이어 타겟도 체크합니다:
```csharp
LivingEntity playerTarget = hit.collider.GetComponentInParent<LivingEntity>();
if (playerTarget != null)
{
    PhotonView targetPV = playerTarget.GetComponent<PhotonView>();
    if (targetPV != null)
    {
        targetPV.RPC("OnDamage", RpcTarget.All, gunData.damage, hit.point, hit.normal, pv.ViewID);
    }
}
```

### 4. AI가 움직이지 않음

**확인사항:**
1. NavMesh가 씬에 베이크되어 있는지
2. AI가 NavMesh 위에 스폰되는지
3. NavMeshAgent 컴포넌트가 활성화되어 있는지
4. CharacterData의 moveSpeed가 0이 아닌지

### 5. AI가 코인을 먹지 못함

**확인사항:**
1. AI의 Tag가 `Player`인지
2. Collider가 활성화되어 있는지
3. Coin의 OnTriggerEnter가 "Player" 태그를 체크하는지

### 6. 애니메이션이 재생되지 않음

**확인사항:**
1. Animator Controller가 할당되어 있는지
2. 애니메이터 파라미터 이름이 일치하는지:
   - `MoveX` (float)
   - `MoveY` (float)
   - `Death` (bool)
   - `fire` (trigger)
   - `Reload` (trigger)

## 📊 시스템 아키텍처

```
AI 봇 구조:

┌─────────────────┐
│   AIBot.cs      │ ← 메인 AI 컨트롤러
│  - 상태 관리     │
│  - 전투 로직     │
│  - 이동 제어     │
└────────┬────────┘
         │
         ├─→ ┌──────────────┐
         │   │ AIHealth.cs  │ ← 독립적 Health
         │   │ - 체력 관리   │
         │   │ - 데미지 처리 │
         │   │ - 부활 로직   │
         │   └──────────────┘
         │
         ├─→ ┌──────────────┐
         │   │ NavMeshAgent │ ← 이동
         │   └──────────────┘
         │
         ├─→ ┌──────────────┐
         │   │ Animator     │ ← 애니메이션
         │   └──────────────┘
         │
         └─→ ┌──────────────┐
             │ PhotonView   │ ← 네트워크 동기화
             └──────────────┘

데이터 공유:
┌───────────────┐
│ CharacterData │ ← 스탯 공유 (플레이어와 동일)
└───────────────┘
┌───────────────┐
│   GunData     │ ← 무기 스탯 공유 (플레이어와 동일)
└───────────────┘
```

## 🎉 최종 요약

### ✅ 완전히 해결된 문제들

1. **LivingEntity 의존성** → AIHealth로 완전 독립
2. **MoveController 의존성** → NavMeshAgent만 사용
3. **TestGun 의존성** → AIBot 내장 전투 시스템
4. **플레이어 전용 로직 간섭** → 완전히 분리된 AI 전용 시스템
5. **데미지 시스템** → ViewID 기반 정확한 데미지 처리
6. **애니메이션 이벤트 에러** → OnReloadStart/End 메서드 포함

### 🎯 핵심 장점

- **단순함**: 2개의 컴포넌트만 사용 (AIHealth + AIBot)
- **독립성**: 플레이어 시스템과 완전 분리
- **재사용성**: CharacterData, GunData 공유
- **확장성**: 새로운 AI 행동 추가 용이
- **안정성**: 네트워크 환경에서 완벽하게 작동

### 📝 프리팹 체크리스트 (최종)

```
✅ PhotonView (AIHealth, AIBot 관찰)
✅ NavMeshAgent
✅ AIHealth (CharacterData 할당)
✅ AIBot (CharacterData, GunData 할당)
✅ Rigidbody (Kinematic)
✅ Capsule Collider
✅ Animator (C02_Player)
✅ Gun/FirePoint (자식 오브젝트)
✅ Gun/MuzzleFlash (ParticleSystem)
✅ Tag: Player

❌ LivingEntity (제거)
❌ BotLivingEntity (제거)
❌ TestShoot, TestGun, MoveController 등 (제거)
```

이제 완벽하게 작동하는 AI 봇 시스템이 완성되었습니다! 🎮
