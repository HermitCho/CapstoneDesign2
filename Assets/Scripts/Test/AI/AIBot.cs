using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using System.Collections;

/// <summary>
/// 완전히 재구축된 AI 봇 시스템
/// - LivingEntity 대신 AIHealth 사용
/// - CharacterData, GunData로 스탯 공유
/// - 완벽한 네트워크 동기화
/// - 모든 기능이 독립적으로 작동
/// </summary>
[RequireComponent(typeof(PhotonView), typeof(NavMeshAgent), typeof(AIHealth))]
public class AIBot : MonoBehaviourPunCallbacks, IPunObservable
{
    #region Enums
    
    private enum AIState
    {
        Idle,
        CollectCoin,
        AttackEnemy,
        ChaseCrownHolder,
        FleeWithCrown,
        FleeFromDanger, // 위험으로부터 도망 (부활 후 또는 체력 낮을 때)
        SeekFreeCrown,
        Dead
    }
    
    #endregion
    
    #region Inspector Fields
    
    [Header("Data Assets")]
    [SerializeField] private CharacterData characterData;
    [SerializeField] private GunData gunData;
    
    [Header("AI 설정")]
    [SerializeField] private float visionRange = 25f;
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float stateUpdateRate = 0.4f;
    [SerializeField] private float shootCooldown = 0.4f;
    [SerializeField] private LayerMask shopLayerMask;
    
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private Animator animator;
    
    [Header("Footstep Sound")]
    [SerializeField] private float footstepMinInterval = 0.15f;
    private float lastFootstepTime = -999f;
    private int lastStepPhase = int.MinValue;
    
    #endregion
    
    #region Private Fields
    
    // 컴포넌트 캐시
    private PhotonView pv;
    private NavMeshAgent agent;
    private AIHealth aiHealth;
    
    // AI 상태
    private AIState currentState = AIState.Idle;
    private Transform currentTarget;
    private Coin targetCoin;
    private Crown crownObject;
    
    // 전투 상태
    private int currentAmmo;
    private bool isReloading;
    private float lastShootTime;
    private float lastStateUpdate;
    private float nextActionTime;
    private bool isInShop;
    
    // 도망 상태 관리
    private float fleeUntilTime = 0f; // 도망 상태 종료 시간
    private const float FLEE_AFTER_REVIVE_DURATION = 2f; // 부활 후 도망 지속 시간 (2초로 단축)
    private const float LOW_HEALTH_THRESHOLD = 0.3f; // 체력 30% 이하일 때 위험 판단
    private const int DANGER_ENEMY_COUNT = 3; // 주변에 3명 이상의 적이 있으면 위험 (2명에서 증가)
    private const float FLEE_PROBABILITY_LOW_HEALTH = 0.5f; // 체력 낮을 때 도망 확률 (50%)
    private const float FLEE_PROBABILITY_MANY_ENEMIES = 0.4f; // 적 많을 때 도망 확률 (40%)
    
    // 애니메이터 해시
    private int moveXHash;
    private int moveYHash;
    private int deathHash;
    private int reviveHash;
    private int fireHash;
    private int reloadHash;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        Debug.Log($"[AIBot] {gameObject.name} Awake 시작");
        
        // 컴포넌트 초기화
        pv = GetComponent<PhotonView>();
        agent = GetComponent<NavMeshAgent>();
        aiHealth = GetComponent<AIHealth>();
        
        Debug.Log($"[AIBot] {gameObject.name} 컴포넌트 체크 - PV:{pv!=null}, Agent:{agent!=null}, Health:{aiHealth!=null}");
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        // firePoint 자동 검색
        if (firePoint == null)
        {
            Transform gunTransform = transform.Find("Gun");
            if (gunTransform != null)
            {
                Transform fireTransform = gunTransform.Find("FirePoint");
                if (fireTransform != null)
                    firePoint = fireTransform;
                else
                    firePoint = gunTransform;
            }
            else
            {
                firePoint = transform;
            }
        }
        
        // muzzleFlash 자동 검색
        if (muzzleFlash == null)
        {
            muzzleFlash = GetComponentInChildren<ParticleSystem>();
        }
        
        // 애니메이터 파라미터 해시
        moveXHash = Animator.StringToHash("MoveX");
        moveYHash = Animator.StringToHash("MoveY");
        deathHash = Animator.StringToHash("Death");
        reviveHash = Animator.StringToHash("Revive");
        fireHash = Animator.StringToHash("fire");
        reloadHash = Animator.StringToHash("Reload");
        
        // SpeedMultiplier 초기값 설정 (TestMoveAnimationController와 동일)
        if (animator != null)
        {
            animator.SetFloat("SpeedMultiplier", 1.2f);
        }
        
        // CharacterData 적용
        if (characterData != null && aiHealth != null)
        {
            aiHealth.SetCharacterData(characterData);
            Debug.Log($"[AIBot] {gameObject.name} CharacterData 적용됨");
        }
        else
        {
            Debug.LogError($"[AIBot] {gameObject.name} CharacterData 없음! Data:{characterData!=null}, Health:{aiHealth!=null}");
        }
        
        // ✅ CRITICAL DEBUG: Collider 상태 확인
        Collider[] colliders = GetComponentsInChildren<Collider>();
        Debug.Log($"[AIBot - Awake] {gameObject.name} Collider 개수: {colliders.Length}");
        foreach (Collider col in colliders)
        {
            Debug.Log($"  - Collider: {col.gameObject.name}, Enabled: {col.enabled}, Layer: {LayerMask.LayerToName(col.gameObject.layer)} (ID: {col.gameObject.layer}), isTrigger: {col.isTrigger}");
        }
        
        // NavMeshAgent 설정
        if (agent != null && characterData != null)
        {
            agent.updateRotation = false;
            agent.speed = characterData.moveSpeed;
            agent.angularSpeed = 300f;
            agent.acceleration = 8f;
            
            // ✅ CRITICAL FIX: NavMeshAgent가 Physics Collider와 충돌하지 않도록 설정
            // obstacleAvoidanceType을 NoObstacleAvoidance로 설정하여 NavMeshAgent가
            // Physics Collider를 무시하고 NavMesh만 사용하도록 함
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            
            // ✅ CRITICAL: 클라이언트에서는 NavMeshAgent 비활성화 (위치 동기화와 충돌 방지)
            // MasterClient만 NavMesh로 AI 경로 계산
            if (!PhotonNetwork.IsMasterClient)
            {
                agent.enabled = false;
                Debug.Log($"[AIBot] {gameObject.name} NavMeshAgent 비활성화 (클라이언트)");
            }
            else
            {
                Debug.Log($"[AIBot] {gameObject.name} NavMeshAgent 설정됨 - Speed:{agent.speed}, ObstacleAvoidance: NoObstacleAvoidance (마스터)");
            }
        }
        else
        {
            Debug.LogError($"[AIBot] {gameObject.name} NavMeshAgent 설정 실패! Agent:{agent!=null}, Data:{characterData!=null}");
        }
        
        // GunData 초기화
        if (gunData != null)
        {
            currentAmmo = gunData.maxAmmo;
        }
    }
    
    private void OnEnable()
    {
        if (aiHealth != null)
        {
            aiHealth.OnDeath += HandleDeath;
            aiHealth.OnRevive += HandleRevive;
        }
        
        StartCoroutine(FindCrownCoroutine());
    }
    
    private void OnDisable()
    {
        if (aiHealth != null)
        {
            aiHealth.OnDeath -= HandleDeath;
            aiHealth.OnRevive -= HandleRevive;
        }
    }
    
    private void Update()
    {
        // 마스터 클라이언트만 AI 실행
        if (!ShouldRunAI())
            return;
        
        // NavMesh 체크
        if (agent == null)
        {
            Debug.LogError($"[AIBot] {gameObject.name} NavMeshAgent가 null!");
            return;
        }
        
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"[AIBot] {gameObject.name} NavMesh 위에 없음! Position: {transform.position}");
            return;
        }
            
        // 사망 상태면 정지
        if (aiHealth != null && aiHealth.IsDead)
        {
            currentState = AIState.Dead;
            StopMovement();
            UpdateAnimation();
            return;
        }
        
        // 주기적으로 상태 업데이트
        if (Time.time >= lastStateUpdate + stateUpdateRate)
        {
            UpdateState();
            lastStateUpdate = Time.time;
        }
        
        // 현재 상태 실행
        ExecuteState();
        
        // 애니메이션 업데이트
        UpdateAnimation();
    }
    
    #endregion
    
    #region AI Logic
    
    private bool ShouldRunAI()
    {
        // AI는 항상 MasterClient가 제어
        return PhotonNetwork.IsMasterClient && pv != null;
    }
    
    private void UpdateState()
    {
        // 상점 안에 있는지 확인
        CheckIfInShop();
        
        // ✅ 우선순위 0: 도망 상태가 활성화되어 있으면 도망 (부활 후 짧은 시간만)
        if (Time.time < fleeUntilTime)
        {
            Transform nearestThreat = FindNearestEnemy();
            if (nearestThreat != null)
            {
                currentTarget = nearestThreat;
                currentState = AIState.FleeFromDanger;
                return;
            }
            // 위협이 없으면 도망 상태 해제
            fleeUntilTime = 0f;
        }
        
        // ✅ 우선순위 1: 왕관 소유자 추적 (최우선, 확률 100%)
        Transform crownHolder = GetCrownHolder();
        if (crownHolder != null && crownHolder != transform)
        {
            // 왕관 주변에 적이 많은지 확인
            int nearbyEnemyCount = CountNearbyEnemies(crownHolder.position, visionRange);
            
            // 왕관 주변에 적이 많으면 전략적으로 행동 (도망 또는 공격)
            if (nearbyEnemyCount >= DANGER_ENEMY_COUNT)
            {
                // 확률적으로 도망 (40%) 또는 공격 (60%)
                if (Random.value < FLEE_PROBABILITY_MANY_ENEMIES)
                {
                    // 도망: 가장 가까운 적으로부터 도망
                    Transform nearestThreat = FindNearestEnemy();
                    if (nearestThreat != null)
                    {
                        currentTarget = nearestThreat;
                        currentState = AIState.FleeFromDanger;
                        fleeUntilTime = Time.time + 2f; // 2초 동안 도망
                        return;
                    }
                }
                // 공격: 왕관 소유자를 공격 (60% 확률)
                currentTarget = crownHolder;
                currentState = AIState.ChaseCrownHolder;
                return;
            }
            else
            {
                // 주변에 적이 적으면 왕관 소유자 추적
                currentTarget = crownHolder;
                currentState = AIState.ChaseCrownHolder;
                return;
            }
        }
        
        // ✅ 우선순위 1.5: 떨어진 왕관 획득 (왕관이 최우선이므로 상위로 이동)
        if (crownObject != null && !IsCrownAttached())
        {
            float dist = Vector3.Distance(transform.position, crownObject.transform.position);
            if (dist <= visionRange)
            {
                // 왕관 주변에 적이 많은지 확인
                int nearbyEnemyCount = CountNearbyEnemies(crownObject.transform.position, visionRange);
                
                // 왕관 주변에 적이 많으면 전략적으로 행동
                if (nearbyEnemyCount >= DANGER_ENEMY_COUNT)
                {
                    // 확률적으로 도망 (40%) 또는 왕관 획득 시도 (60%)
                    if (Random.value < FLEE_PROBABILITY_MANY_ENEMIES)
                    {
                        Transform nearestThreat = FindNearestEnemy();
                        if (nearestThreat != null)
                        {
                            currentTarget = nearestThreat;
                            currentState = AIState.FleeFromDanger;
                            fleeUntilTime = Time.time + 2f;
                            return;
                        }
                    }
                }
                // 왕관 획득 시도
                currentState = AIState.SeekFreeCrown;
                return;
            }
        }
        
        // ✅ 우선순위 2: 체력이 낮거나 주변에 적이 많으면 전략적으로 행동
        if (aiHealth != null && !aiHealth.IsDead)
        {
            float healthRatio = aiHealth.CurrentHealth / aiHealth.MaxHealth;
            int nearbyEnemyCount = CountNearbyEnemies(visionRange);
            
            // 체력이 30% 이하이거나 주변에 3명 이상의 적이 있으면 전략 판단
            if (healthRatio <= LOW_HEALTH_THRESHOLD || nearbyEnemyCount >= DANGER_ENEMY_COUNT)
            {
                Transform nearestThreat = FindNearestEnemy();
                if (nearestThreat != null && Vector3.Distance(transform.position, nearestThreat.position) < visionRange)
                {
                    // 체력이 낮을 때는 50% 확률로 도망, 적이 많을 때는 40% 확률로 도망
                    float fleeChance = healthRatio <= LOW_HEALTH_THRESHOLD ? FLEE_PROBABILITY_LOW_HEALTH : FLEE_PROBABILITY_MANY_ENEMIES;
                    
                    if (Random.value < fleeChance)
                    {
                        // 도망
                        currentTarget = nearestThreat;
                        currentState = AIState.FleeFromDanger;
                        fleeUntilTime = Time.time + 2f; // 2초 동안 도망
                        return;
                    }
                    else
                    {
                        // 맞서 싸우기
                        currentTarget = nearestThreat;
                        currentState = AIState.AttackEnemy;
                        return;
                    }
                }
            }
        }
        
        // 우선순위 3: 내가 왕관을 가지고 있으면 도망
        if (HasCrown())
        {
            Transform threat = FindNearestEnemy();
            if (threat != null && Vector3.Distance(transform.position, threat.position) < visionRange)
            {
                currentTarget = threat;
                currentState = AIState.FleeWithCrown;
                return;
            }
            // 왕관 있어도 가끔 코인 수집 (20% 확률)
            if (Random.value < 0.2f)
            {
                currentState = AIState.CollectCoin;
                return;
            }
        }
        
        // 우선순위 4: 시야 내 적 공격 (랜덤 확률 60%, 가끔 무시)
        Transform enemy = FindNearestEnemy();
        if (enemy != null && Vector3.Distance(transform.position, enemy.position) <= visionRange && Random.value > 0.4f)
        {
            currentTarget = enemy;
            currentState = AIState.AttackEnemy;
            return;
        }
        
        // 우선순위 5: 코인 수집 또는 랜덤 배회
        if (Random.value > 0.1f)
        {
            currentState = AIState.CollectCoin;
        }
        else
        {
            currentState = AIState.Idle; // 가끔 잠깐 멈춤
        }
    }
    
    private void ExecuteState()
    {
        switch (currentState)
        {
            case AIState.Idle:
                StopMovement();
                break;
                
            case AIState.CollectCoin:
                CollectCoins();
                break;
                
            case AIState.AttackEnemy:
                AttackTarget();
                break;
                
            case AIState.ChaseCrownHolder:
                ChaseAndAttack();
                break;
                
            case AIState.FleeWithCrown:
                FleeFromThreat();
                break;
                
            case AIState.FleeFromDanger:
                FleeFromThreat(); // 위험으로부터 도망 (FleeWithCrown과 동일한 로직 사용)
                break;
                
            case AIState.SeekFreeCrown:
                SeekCrown();
                break;
                
            case AIState.Dead:
                StopMovement();
                break;
        }
    }
    
    #endregion
    
    #region State Behaviors
    
    private void CollectCoins()
    {
        if (targetCoin == null || targetCoin.IsCollected)
        {
            targetCoin = FindNearestCoin();
            
        }
        
        if (targetCoin != null)
        {
            MoveTo(targetCoin.transform.position);
        }
        else
        {
            // 코인이 없으면 랜덤 위치로 이동
            if (Time.time % 5f < Time.deltaTime) // 5초마다
            {
                Vector3 randomPos = transform.position + Random.insideUnitSphere * 10f;
                randomPos.y = transform.position.y;
                MoveTo(randomPos);
            }
        }
    }
    
    private void AttackTarget()
    {
        if (currentTarget == null)
        {
            currentState = AIState.Idle;
            return;
        }
        
        // AI인지 플레이어인지 확인하여 사망 체크
        bool targetDead = IsTargetDead(currentTarget);
        if (targetDead)
        {
            currentTarget = null;
            currentState = AIState.Idle;
            return;
        }
        
        float distance = Vector3.Distance(transform.position, currentTarget.position);
        
        if (distance <= attackRange)
        {
            // 시야선 체크 (벽에 막혀있는지)
            if (HasLineOfSight(currentTarget))
            {
                // 공격 거리 내이고 시야 확보 - 멈추고 사격
                StopMovement();
                RotateTowards(currentTarget.position);
                TryShoot();
            }
            else
            {
                // 벽에 막혀있으면 계속 추적
                MoveTo(currentTarget.position);
            }
        }
        else if (distance <= visionRange)
        {
            // 추적
            MoveTo(currentTarget.position);
        }
        else
        {
            currentTarget = null;
            currentState = AIState.Idle;
        }
    }
    
    private void ChaseAndAttack()
    {
        if (currentTarget == null)
        {
            currentState = AIState.Idle;
            return;
        }
        
        float distance = Vector3.Distance(transform.position, currentTarget.position);
        
        if (distance <= attackRange)
        {
            // 시야선 체크
            if (HasLineOfSight(currentTarget))
            {
                StopMovement();
                RotateTowards(currentTarget.position);
                TryShoot();
            }
            else
            {
                // 벽에 막혀있으면 계속 추적
                MoveTo(currentTarget.position);
            }
        }
        else
        {
            MoveTo(currentTarget.position);
        }
    }
    
    private void FleeFromThreat()
    {
        if (currentTarget == null)
        {
            currentState = AIState.CollectCoin;
            return;
        }
        
        // 위협으로부터 반대 방향으로 도망
        Vector3 fleeDirection = (transform.position - currentTarget.position).normalized;
        
        // 여러 각도로 도망칠 위치 시도
        Vector3 fleeTarget = Vector3.zero;
        bool foundValidPosition = false;
        
        // 시도 각도: 정면, 좌측 45도, 우측 45도
        float[] angles = { 0f, 45f, -45f, 90f, -90f };
        
        foreach (float angle in angles)
        {
            Vector3 direction = Quaternion.Euler(0, angle, 0) * fleeDirection;
            Vector3 testTarget = transform.position + direction * 15f;
            
            // NavMesh에서 유효한 위치인지 확인
            NavMeshHit hit;
            if (NavMesh.SamplePosition(testTarget, out hit, 10f, NavMesh.AllAreas))
            {
                // 경로가 존재하는지 확인
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    fleeTarget = hit.position;
                    foundValidPosition = true;
                    break;
                }
            }
        }
        
        if (foundValidPosition)
        {
            MoveTo(fleeTarget);
        }
        else
        {
            // 도망칠 곳이 없으면 그냥 멀리 이동 시도
            Vector3 randomFlee = transform.position + fleeDirection * 10f + Random.insideUnitSphere * 5f;
            randomFlee.y = transform.position.y;
            MoveTo(randomFlee);
        }
    }
    
    private void SeekCrown()
    {
        if (crownObject == null || IsCrownAttached())
        {
            currentState = AIState.Idle;
            return;
        }
        
        MoveTo(crownObject.transform.position);
    }
    
    #endregion
    
    #region Movement
    
    private void MoveTo(Vector3 targetPosition)
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogWarning($"[AIBot] {gameObject.name} MoveTo 실패 - NavMesh 없음");
            return;
        }
        
        // NavMesh 위의 가장 가까운 점 찾기
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, 10f, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
        }
            
        agent.isStopped = false;
        agent.SetDestination(targetPosition);
        
        // 디버그: 경로 상태 확인
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Debug.LogWarning($"[AIBot] {gameObject.name} 경로 무효! Target: {targetPosition}");
        }
        
        // 이동 방향으로 회전
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 direction = agent.velocity.normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 6f);
            }
        }
    }
    
    private void StopMovement()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }
    
    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }
    
    #endregion
    
    #region Combat
    
    private void TryShoot()
    {
        if (gunData == null)
            return;
        
        // 상점 안에서는 총 못 쏨
        if (isInShop)
            return;
            
        if (isReloading)
            return;
            
        if (currentAmmo <= 0)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(ReloadCoroutine());
            }
            return;
        }
        
        // 랜덤 쿨다운 (0.3 ~ 0.6초)
        float randomCooldown = shootCooldown + Random.Range(-0.1f, 0.2f);
        if (Time.time < lastShootTime + randomCooldown)
            return;
            
        if (currentTarget == null)
            return;
        
        // 가끔 조준 실수 (30% 확률)
        Vector3 targetPoint = currentTarget.position + Vector3.up * 1.4f;
        if (Random.value < 0.3f)
        {
            targetPoint += Random.insideUnitSphere * 0.5f;
        }
        
        // RPC로 발사
        pv.RPC("RPC_Shoot", RpcTarget.All, targetPoint);
        
        currentAmmo--;
        lastShootTime = Time.time;
    }
    
    [PunRPC]
    private void RPC_Shoot(Vector3 targetPoint)
    {
        if (firePoint == null || gunData == null)
            return;
            
        Vector3 direction = (targetPoint - firePoint.position).normalized;
        
        // 샷건 펠릿 처리
        int pelletCount = Mathf.Max(1, gunData.pelletCount);
        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 pelletDirection = ApplySpread(direction);
            ShootPellet(pelletDirection);
        }
        
        // 이펙트 재생 (모든 클라이언트)
        PlayShootEffects();
        
        // 애니메이션 트리거
        if (animator != null)
        {
            animator.SetTrigger(fireHash);
        }
    }
    
    private Vector3 ApplySpread(Vector3 baseDirection)
    {
        if (gunData == null || gunData.spreadAngle <= 0.001f)
            return baseDirection;
            
        float spread = gunData.spreadAngle;
        float randomX = Random.Range(-spread, spread);
        float randomY = Random.Range(-spread, spread);
        
        Quaternion spreadRotation = Quaternion.Euler(randomX, randomY, 0f);
        return spreadRotation * baseDirection;
    }
    
    private void ShootPellet(Vector3 direction)
    {
        if (gunData == null)
            return;
            
        // 레이캐스트로 히트 감지
        int layerMask = ~LayerMask.GetMask("PlayerPosition");
        
        // ✅ CRITICAL FIX: QueryTriggerInteraction.Ignore 추가
        // 트리거 콜라이더(Shop 등)가 레이캐스트를 방해하지 않도록 함
        
        // 디버그: 레이캐스트 시각화
        Debug.DrawRay(firePoint.position, direction * gunData.range, Color.cyan, 1f);
        
        if (Physics.Raycast(firePoint.position, direction, out RaycastHit hit, gunData.range, layerMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log($"[AIBot - ShootPellet] 레이캐스트 히트! Object: {hit.collider.gameObject.name}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}, isTrigger: {hit.collider.isTrigger}, Distance: {hit.distance:F2}m");
            
            // 마스터 클라이언트만 데미지 처리
            if (PhotonNetwork.IsMasterClient)
            {
                // AI 타겟 체크
                AIHealth aiTarget = hit.collider.GetComponentInParent<AIHealth>();
                if (aiTarget != null && aiTarget != aiHealth && !aiTarget.IsDead)
                {
                    PhotonView targetPV = aiTarget.GetComponent<PhotonView>();
                    if (targetPV != null)
                    {
                        Debug.Log($"[AIBot - ShootPellet] AI 타겟 발견! {aiTarget.name}, TakeDamage RPC 호출");
                        // TakeDamage RPC 호출
                        targetPV.RPC("TakeDamage", RpcTarget.All, gunData.damage, hit.point, hit.normal, pv.ViewID);
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"[AIBot - ShootPellet]  AI 타겟의 PhotonView를 찾을 수 없음: {aiTarget.name}");
                    }
                }
                
                // 플레이어 타겟 체크
                LivingEntity playerTarget = hit.collider.GetComponentInParent<LivingEntity>();
                if (playerTarget != null && !playerTarget.IsDead)
                {
                    PhotonView targetPV = playerTarget.GetComponent<PhotonView>();
                    if (targetPV != null)
                    {
                        Debug.Log($"[AIBot - ShootPellet]  플레이어 타겟 발견! {playerTarget.name}, OnDamage RPC 호출");
                        targetPV.RPC("OnDamage", RpcTarget.All, gunData.damage, hit.point, hit.normal, pv.ViewID);
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"[AIBot - ShootPellet]  플레이어 타겟의 PhotonView를 찾을 수 없음: {playerTarget.name}");
                    }
                }
                else
                {
                    if (playerTarget == null)
                        Debug.Log($"[AIBot - ShootPellet] 플레이어 타겟 없음 (GetComponentInParent 실패)");
                    else if (playerTarget.IsDead)
                        Debug.Log($"[AIBot - ShootPellet] 이미 죽은 플레이어 타겟");
                }
            }
            else
            {
                Debug.Log($"[AIBot - ShootPellet] 마스터 클라이언트가 아님 (IsMasterClient: {PhotonNetwork.IsMasterClient})");
            }
        }
        else
        {
            Debug.Log($"[AIBot - ShootPellet] 레이캐스트 빗나감 - 사거리: {gunData.range}m");
        }
    }
    
    private void PlayShootEffects()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        
        if (gunData != null && gunData.shotClip != null && firePoint != null)
        {
            AudioManager.Inst?.PlayClipAtPoint(gunData.shotClip, firePoint.position, 1f, 1f, null, firePoint);
        }
    }
    
    private IEnumerator ReloadCoroutine()
    {
        if (gunData == null)
            yield break;
            
        isReloading = true;
        
        // 재장전 시작을 모든 클라이언트에 동기화
        pv.RPC("RPC_StartReload", RpcTarget.All);
        
        yield return new WaitForSeconds(gunData.reloadTime);
        
        // 재장전 완료를 모든 클라이언트에 동기화
        pv.RPC("RPC_CompleteReload", RpcTarget.All);
    }
    
    [PunRPC]
    private void RPC_StartReload()
    {
        isReloading = true;
        
        // 애니메이션 트리거
        if (animator != null)
        {
            animator.SetBool(reloadHash, true);
        }
        
        // 사운드 재생
        if (gunData != null && gunData.reloadClip != null && firePoint != null)
        {
            AudioManager.Inst?.PlayClipAtPoint(gunData.reloadClip, firePoint.position, 0.8f, 1f, null, firePoint);
        }
    }
    
    [PunRPC]
    private void RPC_CompleteReload()
    {
        if (gunData == null)
            return;
            
        currentAmmo = gunData.maxAmmo;
        isReloading = false;
        
        // 재장전 애니메이션 종료
        if (animator != null)
        {
            animator.SetBool(reloadHash, false);
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    private Transform FindNearestEnemy()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform nearest = null;
        float nearestDist = visionRange;
        
        foreach (GameObject obj in players)
        {
            if (obj == gameObject)
                continue;
            
            // AI 체크
            AIHealth aiTarget = obj.GetComponent<AIHealth>();
            if (aiTarget != null)
            {
                if (!aiTarget.IsDead)
                {
                    float dist = Vector3.Distance(transform.position, obj.transform.position);
                    if (dist < nearestDist)
                    {
                        nearest = obj.transform;
                        nearestDist = dist;
                    }
                }
                continue;
            }
            
            // 플레이어 체크
            LivingEntity playerTarget = obj.GetComponent<LivingEntity>();
            if (playerTarget != null && !playerTarget.IsDead)
            {
                float dist = Vector3.Distance(transform.position, obj.transform.position);
                if (dist < nearestDist)
                {
                    nearest = obj.transform;
                    nearestDist = dist;
                }
            }
        }
        
        return nearest;
    }
    
    private bool IsTargetDead(Transform target)
    {
        if (target == null)
            return true;
            
        // AI 체크
        AIHealth aiTarget = target.GetComponent<AIHealth>();
        if (aiTarget != null)
            return aiTarget.IsDead;
            
        // 플레이어 체크
        LivingEntity playerTarget = target.GetComponent<LivingEntity>();
        if (playerTarget != null)
            return playerTarget.IsDead;
            
        return true;
    }
    
    /// <summary>
    /// 주변에 있는 적의 수를 카운트 (현재 위치 기준)
    /// </summary>
    private int CountNearbyEnemies(float range)
    {
        return CountNearbyEnemies(transform.position, range);
    }
    
    /// <summary>
    /// 특정 위치 주변에 있는 적의 수를 카운트
    /// </summary>
    private int CountNearbyEnemies(Vector3 position, float range)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        int count = 0;
        
        foreach (GameObject obj in players)
        {
            if (obj == gameObject)
                continue;
            
            float dist = Vector3.Distance(position, obj.transform.position);
            if (dist > range)
                continue;
            
            // AI 체크
            AIHealth aiTarget = obj.GetComponent<AIHealth>();
            if (aiTarget != null && !aiTarget.IsDead)
            {
                count++;
                continue;
            }
            
            // 플레이어 체크
            LivingEntity playerTarget = obj.GetComponent<LivingEntity>();
            if (playerTarget != null && !playerTarget.IsDead)
            {
                count++;
            }
        }
        
        return count;
    }
    
    /// <summary>
    /// 타겟까지 시야선이 확보되어 있는지 체크 (벽 등의 장애물 확인)
    /// </summary>
    private bool HasLineOfSight(Transform target)
    {
        if (target == null || firePoint == null)
            return false;
            
        Vector3 direction = (target.position + Vector3.up * 1.4f) - firePoint.position;
        float distance = direction.magnitude;
        
        // 레이캐스트로 시야선 확인
        RaycastHit hit;
        int layerMask = ~LayerMask.GetMask("PlayerPosition"); // PlayerPosition 레이어 제외
        
        // ✅ CRITICAL FIX: QueryTriggerInteraction.Ignore 추가
        // 트리거 콜라이더(Shop 등)가 레이캐스트를 방해하지 않도록 함
        if (Physics.Raycast(firePoint.position, direction.normalized, out hit, distance, layerMask, QueryTriggerInteraction.Ignore))
        {
            // 맞은 대상이 타겟인지 확인
            Transform hitTransform = hit.collider.transform;
            
            // 타겟 자신이거나 타겟의 부모인지 확인
            while (hitTransform != null)
            {
                if (hitTransform == target)
                    return true; // 시야 확보
                hitTransform = hitTransform.parent;
            }
            
            // 타겟이 아닌 다른 것(벽 등)에 막혔음
            return false;
        }
        
        // 아무것도 안 맞았으면 시야 확보
        return true;
    }
    
    private Coin FindNearestCoin()
    {
        Coin[] coins = FindObjectsOfType<Coin>();
        Coin nearest = null;
        float nearestDist = Mathf.Infinity;
        
        foreach (Coin coin in coins)
        {
            if (coin.IsCollected)
                continue;
                
            float dist = Vector3.Distance(transform.position, coin.transform.position);
            if (dist < nearestDist)
            {
                nearest = coin;
                nearestDist = dist;
            }
        }
        
        return nearest;
    }
    
    private bool HasCrown()
    {
        return crownObject != null && crownObject.transform.IsChildOf(transform);
    }
    
    private bool IsCrownAttached()
    {
        return crownObject != null && crownObject.transform.parent != null;
    }
    
    private Transform GetCrownHolder()
    {
        if (crownObject == null || !IsCrownAttached())
            return null;
            
        return crownObject.transform.parent;
    }
    
    private IEnumerator FindCrownCoroutine()
    {
        while (crownObject == null)
        {
            crownObject = FindObjectOfType<Crown>();
            yield return new WaitForSeconds(1f);
        }
    }
    
    private void CheckIfInShop()
    {
        // Shop 레이어에 충돌 체크
        Collider[] hits = Physics.OverlapSphere(transform.position, 1f, shopLayerMask);
        isInShop = hits.Length > 0;
        
        // 또는 Shop 태그 체크
        if (!isInShop)
        {
            hits = Physics.OverlapSphere(transform.position, 1f);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Shop"))
                {
                    isInShop = true;
                    break;
                }
            }
        }
    }
    
    #endregion
    
    #region Animation
    
    private void UpdateAnimation()
    {
        if (animator == null)
            return;
        
        // 이동 애니메이션 - TestMoveAnimationController와 동일한 방식 사용
        if (agent != null && agent.isOnNavMesh && aiHealth != null && !aiHealth.IsDead)
        {
            Vector3 velocity = agent.velocity;
            float speed = velocity.magnitude;
            
            if (speed > 0.1f)
            {
                // 로컬 좌표계로 변환
                Vector3 localVelocity = transform.InverseTransformDirection(velocity);
                
                // 속도 정규화 (-1 ~ 1 범위)
                float moveSpeed = characterData != null ? characterData.moveSpeed : 5f;
                float normalizedX = Mathf.Clamp(localVelocity.x / moveSpeed, -1f, 1f);
                float normalizedZ = Mathf.Clamp(localVelocity.z / moveSpeed, -1f, 1f);
                
                // TestMoveAnimationController처럼 dampTime 0.1f 사용하여 부드럽게 보간
                animator.SetFloat(moveXHash, normalizedX, 0.1f, Time.deltaTime);
                animator.SetFloat(moveYHash, normalizedZ, 0.1f, Time.deltaTime);
            }
            else
            {
                // 정지 상태
                animator.SetFloat(moveXHash, 0f, 0.1f, Time.deltaTime);
                animator.SetFloat(moveYHash, 0f, 0.1f, Time.deltaTime);
            }
        }
        else
        {
            animator.SetFloat(moveXHash, 0f, 0.1f, Time.deltaTime);
            animator.SetFloat(moveYHash, 0f, 0.1f, Time.deltaTime);
        }
    }
    
    // 애니메이션 이벤트 수신자
    public void OnReloadStart()
    {
        // 재장전 시작 이벤트
    }
    
    public void OnReloadEnd()
    {
        // 재장전 완료 이벤트
    }
    
    /// <summary>
    /// 발자국 소리 애니메이션 이벤트
    /// </summary>
    public void FootStepSound()
    {
        // 마스터 클라이언트만 발자국 소리 재생 (중복 방지)
        if (!PhotonNetwork.IsMasterClient)
            return;
            
        // 이동 중인지 체크
        if (agent == null || !agent.isOnNavMesh || agent.velocity.magnitude < 0.1f)
            return;
        
        // Animator 위상 체크 (같은 반 주기 내 중복 이벤트 차단)
        if (animator != null)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            float norm = state.normalizedTime % 1f;
            int currentPhase = Mathf.FloorToInt(norm * 2f + 0.01f);
            if (currentPhase == lastStepPhase)
            {
                return;
            }
            lastStepPhase = currentPhase;
        }
        
        // 최소 간격 보호
        if (Time.time - lastFootstepTime < footstepMinInterval)
        {
            return;
        }
        lastFootstepTime = Time.time;
        
        // RPC로 모든 클라이언트에 발자국 소리 재생
        pv.RPC("RPC_AIFootStep", RpcTarget.All);
    }
    
    [PunRPC]
    private void RPC_AIFootStep()
    {
        AudioManager.Inst?.PlayClipAtPoint("SFX_Game_FootStep", transform.position, null, transform);
    }
    
    #endregion
    
    #region Event Handlers
    
    private void HandleDeath()
    {
        currentState = AIState.Dead;
        StopMovement();
        currentTarget = null;
        targetCoin = null;
        
        if (animator != null)
        {
            animator.SetTrigger(deathHash);
        }
    }
    
    private void HandleRevive()
    {
        currentState = AIState.Idle;
        currentAmmo = gunData != null ? gunData.maxAmmo : 30;
        isReloading = false;
        currentTarget = null;
        
        // 부활 후 일정 시간 동안 도망 상태로 설정 (뭉치는 현상 방지)
        fleeUntilTime = Time.time + FLEE_AFTER_REVIVE_DURATION;
        Debug.Log($"[AIBot] {gameObject.name} 부활 - {FLEE_AFTER_REVIVE_DURATION}초 동안 도망 상태 활성화");
        
        // CRITICAL: NavMeshAgent는 MasterClient에서만 활성화
        if (agent != null && PhotonNetwork.IsMasterClient)
        {
            agent.enabled = true;
            agent.isStopped = false;
        }
        
        if (animator != null)
        {
            animator.SetTrigger(reviveHash);
            // 애니메이션 파라미터 초기화
            animator.SetFloat(moveXHash, 0f);
            animator.SetFloat(moveYHash, 0f);
        }
    }
    
    #endregion
    
    #region Photon Callbacks
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // ✅ 마스터가 상태 + 위치/회전 전송
            stream.SendNext((int)currentState);
            stream.SendNext(currentAmmo);
            stream.SendNext(isReloading);
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // ✅ 클라이언트가 상태 + 위치/회전 수신
            currentState = (AIState)stream.ReceiveNext();
            currentAmmo = (int)stream.ReceiveNext();
            isReloading = (bool)stream.ReceiveNext();
            Vector3 networkPosition = (Vector3)stream.ReceiveNext();
            Quaternion networkRotation = (Quaternion)stream.ReceiveNext();
            
            // ✅ CRITICAL: 클라이언트에서는 NavMeshAgent를 사용하지 않고 직접 위치 동기화
            if (!PhotonNetwork.IsMasterClient)
            {
                // 사망 상태가 아닐 때만 동기화
                if (!aiHealth.IsDead)
                {
                    // 거리 차이가 크면 즉시 이동 (워프), 작으면 보간
                    float distance = Vector3.Distance(transform.position, networkPosition);
                    
                    if (distance > 5f) // 5m 이상 차이나면 즉시 이동 (텔레포트 방지)
                    {
                        transform.position = networkPosition;
                        transform.rotation = networkRotation;
                    }
                    else if (distance > 0.1f) // 작은 차이는 부드럽게 보간
                    {
                        transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
                        transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 10f);
                    }
                }
            }
        }
    }
    
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        // 새로운 MasterClient가 AI 소유권을 가져감
        if (pv != null && PhotonNetwork.IsMasterClient)
        {
            pv.TransferOwnership(PhotonNetwork.MasterClient);
        }
    }
    
    #endregion
}

