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
    
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private Animator animator;
    
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
    
    // 애니메이터 해시
    private int moveXHash;
    private int moveYHash;
    private int deathHash;
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
        fireHash = Animator.StringToHash("fire");
        reloadHash = Animator.StringToHash("Reload");
        
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
        
        // NavMeshAgent 설정
        if (agent != null && characterData != null)
        {
            agent.updateRotation = false;
            agent.speed = characterData.moveSpeed;
            agent.angularSpeed = 300f;
            agent.acceleration = 8f;
            Debug.Log($"[AIBot] {gameObject.name} NavMeshAgent 설정됨 - Speed:{agent.speed}");
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
            Debug.Log($"[AIBot] {gameObject.name} 상태: {currentState}");
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
        bool isMaster = PhotonNetwork.IsMasterClient;
        bool hasPV = pv != null;
        bool isMine = hasPV && pv.IsMine;
        
        if (!isMaster || !hasPV || !isMine)
        {
            // 첫 프레임에만 로그 출력 (스팸 방지)
            if (Time.frameCount % 300 == 0) // 5초마다 (60fps 기준)
            {
                Debug.Log($"[AIBot] {gameObject.name} AI 실행 안함 - IsMaster:{isMaster}, HasPV:{hasPV}, IsMine:{isMine}");
            }
            return false;
        }
        
        return true;
    }
    
    private void UpdateState()
    {
        // 우선순위 1: 왕관 소유자 추적
        Transform crownHolder = GetCrownHolder();
        if (crownHolder != null && crownHolder != transform)
        {
            currentTarget = crownHolder;
            currentState = AIState.ChaseCrownHolder;
            return;
        }
        
        // 우선순위 2: 내가 왕관을 가지고 있으면 도망
        if (HasCrown())
        {
            Transform threat = FindNearestEnemy();
            if (threat != null && Vector3.Distance(transform.position, threat.position) < visionRange)
            {
                currentTarget = threat;
                currentState = AIState.FleeWithCrown;
                return;
            }
        }
        
        // 우선순위 3: 시야 내 적 공격
        Transform enemy = FindNearestEnemy();
        if (enemy != null && Vector3.Distance(transform.position, enemy.position) <= visionRange)
        {
            currentTarget = enemy;
            currentState = AIState.AttackEnemy;
            return;
        }
        
        // 우선순위 4: 떨어진 왕관 획득
        if (crownObject != null && !IsCrownAttached())
        {
            float dist = Vector3.Distance(transform.position, crownObject.transform.position);
            if (dist <= visionRange)
            {
                currentState = AIState.SeekFreeCrown;
                return;
            }
        }
        
        // 우선순위 5: 코인 수집
        currentState = AIState.CollectCoin;
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
            
            if (targetCoin != null)
            {
                Debug.Log($"[AIBot] {gameObject.name} 새 코인 타겟: {targetCoin.name}");
            }
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
                Debug.Log($"[AIBot] {gameObject.name} 코인 없음, 랜덤 이동");
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
            // 공격 거리 내 - 멈추고 사격
            StopMovement();
            RotateTowards(currentTarget.position);
            TryShoot();
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
            StopMovement();
            RotateTowards(currentTarget.position);
            TryShoot();
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
        
        Vector3 fleeDirection = (transform.position - currentTarget.position).normalized;
        Vector3 fleeTarget = transform.position + fleeDirection * 20f;
        MoveTo(fleeTarget);
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
            
        if (isReloading)
            return;
            
        if (currentAmmo <= 0)
        {
            StartCoroutine(ReloadCoroutine());
            return;
        }
        
        if (Time.time < lastShootTime + shootCooldown)
            return;
            
        if (currentTarget == null)
            return;
        
        Vector3 targetPoint = currentTarget.position + Vector3.up * 1.4f;
        
        // RPC로 발사
        pv.RPC("RPC_Shoot", RpcTarget.All, targetPoint);
        
        currentAmmo--;
        lastShootTime = Time.time;
        
        if (currentAmmo <= 0)
        {
            StartCoroutine(ReloadCoroutine());
        }
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
        if (Physics.Raycast(firePoint.position, direction, out RaycastHit hit, gunData.range))
        {
            // 마스터 클라이언트만 데미지 처리
            if (PhotonNetwork.IsMasterClient)
            {
                // AI 타겟 체크
                AIHealth aiTarget = hit.collider.GetComponentInParent<AIHealth>();
                if (aiTarget != null && aiTarget != aiHealth)
                {
                    PhotonView targetPV = aiTarget.GetComponent<PhotonView>();
                    if (targetPV != null)
                    {
                        targetPV.RPC("TakeDamage", RpcTarget.All, gunData.damage, hit.point, hit.normal, pv.ViewID);
                        return;
                    }
                }
                
                // 플레이어 타겟 체크
                LivingEntity playerTarget = hit.collider.GetComponentInParent<LivingEntity>();
                if (playerTarget != null)
                {
                    PhotonView targetPV = playerTarget.GetComponent<PhotonView>();
                    if (targetPV != null)
                    {
                        targetPV.RPC("OnDamage", RpcTarget.All, gunData.damage, hit.point, hit.normal, pv.ViewID);
                    }
                }
            }
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
        
        // 애니메이션 트리거
        if (animator != null)
        {
            animator.SetTrigger(reloadHash);
        }
        
        // 사운드 재생
        if (gunData.reloadClip != null && firePoint != null)
        {
            AudioManager.Inst?.PlayClipAtPoint(gunData.reloadClip, firePoint.position, 0.8f, 1f, null, firePoint);
        }
        
        yield return new WaitForSeconds(gunData.reloadTime);
        
        currentAmmo = gunData.maxAmmo;
        isReloading = false;
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
    
    #endregion
    
    #region Animation
    
    private void UpdateAnimation()
    {
        if (animator == null)
            return;
            
        // 사망 애니메이션
        if (aiHealth != null)
        {
            animator.SetBool(deathHash, aiHealth.IsDead);
        }
        
        // 이동 애니메이션
        if (agent != null && agent.isOnNavMesh)
        {
            Vector3 velocity = agent.velocity;
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);
            
            animator.SetFloat(moveXHash, localVelocity.x, 0.1f, Time.deltaTime);
            animator.SetFloat(moveYHash, localVelocity.z, 0.1f, Time.deltaTime);
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
    
    #endregion
    
    #region Event Handlers
    
    private void HandleDeath()
    {
        currentState = AIState.Dead;
        StopMovement();
        currentTarget = null;
        targetCoin = null;
    }
    
    private void HandleRevive()
    {
        currentState = AIState.Idle;
        currentAmmo = gunData != null ? gunData.maxAmmo : 30;
        isReloading = false;
    }
    
    #endregion
    
    #region Photon Callbacks
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 마스터가 상태 전송
            stream.SendNext((int)currentState);
            stream.SendNext(currentAmmo);
            stream.SendNext(isReloading);
        }
        else
        {
            // 클라이언트가 상태 수신
            currentState = (AIState)stream.ReceiveNext();
            currentAmmo = (int)stream.ReceiveNext();
            isReloading = (bool)stream.ReceiveNext();
        }
    }
    
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (pv != null && PhotonNetwork.LocalPlayer == newMasterClient)
        {
            pv.TransferOwnership(newMasterClient);
        }
    }
    
    #endregion
}
