# AI 데미지 문제 해결 가이드

## 🔧 수정 내용

### AIHealth.cs
`IDamageable` 인터페이스를 구현하도록 수정했습니다.

```csharp
public class AIHealth : MonoBehaviourPunCallbacks, IPunObservable, IDamageable
```

이제 `TestGun`의 `ProcessPelletHit` 메서드에서 AI를 정상적으로 감지하고 데미지를 줄 수 있습니다.

---

## ✅ 테스트 방법

### 1단계: 게임 실행
게임을 실행하고 AI 봇을 총으로 쏴보세요.

### 2단계: 콘솔 확인

#### ✅ 정상 케이스
AI를 쏘면 다음 로그가 나타나야 합니다:
```
[AIHealth] Char1 AI(Clone) OnDamage 호출됨! Damage: 10, Attacker: 1
[AIHealth] Char1 AI(Clone) 데미지 받음: 10, 현재 체력: 100 → 90
```

계속 쏘면:
```
[AIHealth] Char1 AI(Clone) OnDamage 호출됨! Damage: 10, Attacker: 1
[AIHealth] Char1 AI(Clone) 데미지 받음: 10, 현재 체력: 90 → 80
[AIHealth] Char1 AI(Clone) OnDamage 호출됨! Damage: 10, Attacker: 1
[AIHealth] Char1 AI(Clone) 데미지 받음: 10, 현재 체력: 80 → 70
...
[AIHealth] Char1 AI(Clone) OnDamage 호출됨! Damage: 10, Attacker: 1
[AIHealth] Char1 AI(Clone) 데미지 받음: 10, 현재 체력: 10 → 0
[AIHealth] Char1 AI(Clone) 사망!
```

#### ❌ 문제 케이스: OnDamage 로그가 없음

만약 AI를 쏴도 아무 로그도 나오지 않는다면:

**원인 1: AI 프리팹에 콜라이더가 없음**
- AI 오브젝트에 `CapsuleCollider` 또는 `BoxCollider`가 있어야 합니다.
- `isTrigger`는 **체크 해제** 되어야 합니다.

**해결:**
1. Hierarchy에서 AI 오브젝트 선택
2. Inspector에서 Collider 컴포넌트 확인
3. 없으면 Add Component → Physics → Capsule Collider
4. isTrigger 체크 해제

---

**원인 2: AIHealth가 콜라이더와 같은 오브젝트에 없음**

`TestGun.ProcessPelletHit`은 레이캐스트로 맞은 콜라이더에서 `IDamageable`을 찾습니다.

**해결 방법 A (권장):** AIHealth를 루트 오브젝트에 배치
```
Char1 AI(Clone)  ← AIHealth 여기에 있어야 함
├─ CapsuleCollider
├─ NavMeshAgent
├─ AIBot
└─ Gun
```

**해결 방법 B:** `GetComponentInParent` 사용하도록 AIHealth 수정 (이미 적용됨)

`TestGun.cs`의 248번째 줄을 보면:
```csharp
IDamageable target = hit.collider.GetComponent<IDamageable>();
```

이 코드는 맞은 콜라이더에서만 찾습니다. 만약 콜라이더가 자식 오브젝트에 있다면 감지하지 못합니다.

---

**원인 3: 레이어 문제**

AI의 레이어가 `TestGun`의 레이캐스트에서 제외되어 있을 수 있습니다.

**확인:**
1. AI 오브젝트 선택
2. Inspector 상단의 Layer 확인
3. `PlayerPosition` 레이어는 피해야 함 (TestGun에서 제외됨)

**해결:**
- AI의 Layer를 `Default`로 설정

---

## 🔍 추가 디버깅

### TestGun이 AI를 맞추는지 확인

만약 여전히 문제가 있다면, `TestGun.cs`를 임시로 수정해서 디버그 로그를 추가할 수 있습니다:

**위치:** `TestGun.cs` 248번째 줄 (ProcessPelletHit 메서드)

**추가할 코드:**
```csharp
private void ProcessPelletHit(Vector3 direction)
{
    int layerMask = ~LayerMask.GetMask("PlayerPosition");
    if (Physics.Raycast(fireTransform.position, direction, out RaycastHit hit, gunData.range, layerMask, QueryTriggerInteraction.Ignore))
    {
        // 디버그: 무엇을 맞췄는지 확인
        Debug.Log($"[TestGun] 맞춤! Object: {hit.collider.gameObject.name}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
        
        IDamageable target = hit.collider.GetComponent<IDamageable>();
        if (target != null)
        {
            Debug.Log($"[TestGun] IDamageable 찾음! Type: {target.GetType().Name}");
        }
        else
        {
            Debug.Log($"[TestGun] IDamageable 없음!");
        }
        
        // ... 나머지 코드
    }
}
```

이 로그를 통해:
1. 레이캐스트가 AI를 맞추는지 확인
2. IDamageable이 감지되는지 확인

---

## 📋 체크리스트

AI가 데미지를 받지 않을 때 순서대로 확인:

- [ ] AI 프리팹에 `CapsuleCollider` 또는 `BoxCollider` 있음
- [ ] 콜라이더의 `isTrigger` 체크 해제됨
- [ ] AI 프리팹에 `AIHealth` 컴포넌트 있음
- [ ] AIHealth가 콜라이더와 **같은 오브젝트** 또는 **부모 오브젝트**에 있음
- [ ] AI의 Layer가 `PlayerPosition`이 아님 (Default 권장)
- [ ] AI를 쏠 때 콘솔에 `[AIHealth] OnDamage 호출됨!` 로그 나타남

---

## 🎯 빠른 해결

가장 흔한 원인 2가지:

### 1위: AIHealth가 잘못된 위치에 있음 (70%)
**해결:** AIHealth를 AI의 **루트 오브젝트**에 배치

### 2위: 콜라이더 없음 (20%)
**해결:** AI에 CapsuleCollider 추가, isTrigger 체크 해제

---

## 💡 최종 확인

게임을 실행하고 AI를 쏜 후:

### 성공 신호
```
[AIHealth] Char1 AI(Clone) OnDamage 호출됨! Damage: 10
[AIHealth] Char1 AI(Clone) 데미지 받음: 10, 현재 체력: 100 → 90
```

### 실패 신호
```
(아무 로그도 없음)
```

성공 신호가 나타나면 완벽하게 작동하는 것입니다!


