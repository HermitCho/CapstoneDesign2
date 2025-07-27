using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TestTeddyBear : MonoBehaviour
{

    private DataBase.TeddyBearData teddyBearData;
    private Collider colliderTeddyBear;
    private Rigidbody teddyRigidbody;
    
    //테디베어 부착 관련 변수
    private Transform playerTransform;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;

    //테디베어 발광 관련 변수
    private Outline outlineComponent;
    private Coroutine glowingCoroutine;
    private Color originalOutlineColor;
    private float originalOutlineAlpha;
    
    //테디베어 점수 관련 변수
    private Coroutine scoreIncreaseCoroutine;
    private float gameStartTime;
    private float currentScore;
    private float currentScoreMultiplier = 1f;
    
    //테디베어 재부착 방지 관련 변수
    private float lastDetachTime = -999f;
    
    //테디베어 발광 상태 확인 변수
    private bool isGlowing = false;
    //테디베어 부착 상태 확인 변수
    private bool isAttached = false;

    //테디베어 분리 이벤트
    public static event Action OnTeddyBearDetached;

    //TeddyBear 캐시 변수
    private Vector3 cachedAttachOffset;
    private Vector3 cachedAttachRotation;
    private float cachedDetachReattachTime;
    private float cachedTeddyBearScore;
    private float cachedInitialScore;
    private float cachedScoreIncreaseRate;
    private float cachedScoreIncreaseTime;
    private float cachedScoreGetTick;
    private float cachedGlowingIntensity;
    private Color cachedGlowingColor;
    private float cachedGlowingOutlineWidth;
    private float cachedGlowingColorChangeTime;
    private bool cachedCanUseItem;
    private bool cachedCanUseSkill;
    private bool cachedCanUseGun;

    private bool dataBaseCached = false;

    
    void Awake()
    {   
        CacheDataBaseInfo();

        colliderTeddyBear = GetComponent<Collider>();
        teddyRigidbody = GetComponent<Rigidbody>();
        
        // Outline 컴포넌트 초기화
        InitializeOutline();
    }

    void OnEnable()
    {
        // ✅ 플레이어 사망 이벤트 구독
        LivingEntity.OnPlayerDied += OnPlayerDied;
    }

    void OnDisable()
    {
        // ✅ 플레이어 사망 이벤트 구독 해제
        LivingEntity.OnPlayerDied -= OnPlayerDied;
    }

    // Start is called before the first frame update
    void Start()
    {
        // 원본 위치와 회전값 저장
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;
        
        // 게임 시작 시간 기록
        gameStartTime = Time.time;
        
        // 초기 점수 설정
        currentScore = cachedTeddyBearScore;
        
        // 게임 시작 시 발광 시작
        StartGlowing();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.LeftShift))
        {
            DetachFromPlayer();
        }  
        
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isAttached)
        {
            // 재부착 방지 시간 확인
            float timeSinceDetach = Time.time - lastDetachTime;
            if (timeSinceDetach >= cachedDetachReattachTime)
            {
                AttachToPlayer(collision.transform);
            }
            else
            {
                // 재부착 방지 시간 동안은 부착하지 않음
                float remainingTime = cachedDetachReattachTime - timeSinceDetach;
                Debug.Log($"재부착 방지 중... 남은 시간: {remainingTime:F1}초");
            }
        }
    }
    

    void CacheDataBaseInfo()
    {
        try
        {
            if(DataBase.Instance == null)
            {
                Debug.LogWarning("TestTeddyBear - DataBase 인스턴스가 없습니다.");
                return;
            }

            if(DataBase.Instance.teddyBearData != null)
            {
                teddyBearData = DataBase.Instance.teddyBearData;

                cachedAttachOffset = teddyBearData.AttachOffset;
                cachedAttachRotation = teddyBearData.AttachRotation;
                cachedDetachReattachTime = teddyBearData.DetachReattachTime;
                cachedTeddyBearScore = teddyBearData.TeddyBearScore;
                cachedInitialScore = teddyBearData.InitialScore;
                cachedScoreIncreaseRate = teddyBearData.ScoreIncreaseRate;
                cachedScoreIncreaseTime = teddyBearData.ScoreIncreaseTime;
                cachedScoreGetTick = teddyBearData.ScoreGetTick;
                cachedGlowingIntensity = teddyBearData.GlowingIntensity;
                cachedGlowingColor = teddyBearData.GlowingColor;
                cachedGlowingOutlineWidth = teddyBearData.GlowingOutlineWidth;
                cachedGlowingColorChangeTime = teddyBearData.GlowingColorChangeTime;
                cachedCanUseItem = teddyBearData.CanUseItem;
                cachedCanUseSkill = teddyBearData.CanUseSkill;
                cachedCanUseGun = teddyBearData.CanUseGun;

                dataBaseCached = true;
            }

            else
            {
                Debug.LogWarning("TestTeddyBear - DataBase 접근 실패, 기본값 사용");
                dataBaseCached = false;
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ TestTeddyBear - DataBase 캐싱 중 오류: {e.Message}");
            dataBaseCached = false;
        }

    }


    void AttachToPlayer(Transform player)
    {
        if (isAttached) return;
        
        isAttached = true;
        playerTransform = player;

        
        
        // 플레이어의 자식으로 설정
        transform.SetParent(player);
        
        // 플레이어 앞에 즉시 부착
        Vector3 targetPosition = player.position + player.forward * cachedAttachOffset.z + player.up * cachedAttachOffset.y + player.right * cachedAttachOffset.x;
        Quaternion targetRotation = player.rotation * Quaternion.Euler(cachedAttachRotation);
        
        // 즉시 위치와 회전 설정
        transform.position = targetPosition;
        transform.rotation = targetRotation;
        
        // 물리적 상호작용 비활성화 (떨어져 나가는 것 방지)
        if (teddyRigidbody != null)
        {
            teddyRigidbody.isKinematic = true;
            teddyRigidbody.useGravity = false;
        }
        
        // 콜라이더 비활성화 (추가 접촉 방지)
        if (colliderTeddyBear != null)
        {
            colliderTeddyBear.enabled = false;
        }
        

        #warning Static으로 선언되어 있음. 최적화를 위해 수정 필요
        TestShoot.SetIsShooting(false);
        // 부착되면 점수 증가 시작
        StartScoreIncrease();
    }

    // Outline 컴포넌트 초기화
    void InitializeOutline()
    {
        // Outline 컴포넌트가 없으면 추가
        outlineComponent = gameObject.GetComponent<Outline>();
        if (outlineComponent == null)
        {
            outlineComponent = gameObject.AddComponent<Outline>();
        }
        
        // 초기 설정
        outlineComponent.OutlineMode = Outline.Mode.SilhouetteOnly; // 벽을 통과해서 보이게 설정
        outlineComponent.OutlineColor = cachedGlowingColor;
        outlineComponent.OutlineWidth = cachedGlowingOutlineWidth; // 외곽선 두께
        
        // 원본 색상 저장
        originalOutlineColor = cachedGlowingColor;
        originalOutlineAlpha = originalOutlineColor.a;
        
        // 처음에는 비활성화
        outlineComponent.enabled = false;
    }

    // 발광 시작
    void StartGlowing()
    {
        if (isGlowing) return;
        
        isGlowing = true;
        
        if (outlineComponent != null)
        {
            outlineComponent.enabled = true;
            // 발광 깜박임 코루틴 시작
            glowingCoroutine = StartCoroutine(GlowingEffect());
        }
    }

    // 발광 중지 - 개발자용 메서드
    void StopGlowing()
    {
        if (!isGlowing) return;
        
        isGlowing = false;
        
        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
        }
        
        if (glowingCoroutine != null)
        {
            StopCoroutine(glowingCoroutine);
            glowingCoroutine = null;
        }
    }

    // 발광 깜박임 효과 코루틴
    IEnumerator GlowingEffect()
    {
        while (isGlowing)
        {
            // 발광 색상으로 페이드 인
            yield return StartCoroutine(FadeOutlineAlpha(0f, cachedGlowingIntensity, cachedGlowingColorChangeTime * 0.5f));
            
            // 발광 색상에서 페이드 아웃
            yield return StartCoroutine(FadeOutlineAlpha(cachedGlowingIntensity, 0.2f, cachedGlowingColorChangeTime * 0.5f));
            
            yield return null;
        }
    }

    // 외곽선 투명도 페이드 효과
    IEnumerator FadeOutlineAlpha(float fromAlpha, float toAlpha, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsedTime / duration);
            
            Color newColor = cachedGlowingColor;
            newColor.a = alpha;
            
            if (outlineComponent != null)
            {
                outlineComponent.OutlineColor = newColor;
            }
            
            yield return null;
        }
        
        // 최종 알파값 설정
        Color finalColor = cachedGlowingColor;
        finalColor.a = toAlpha;
        if (outlineComponent != null)
        {
            outlineComponent.OutlineColor = finalColor;
        }
    }
    
    // 점수 증가 시작
    void StartScoreIncrease()
    {
        if (scoreIncreaseCoroutine != null)
        {
            StopCoroutine(scoreIncreaseCoroutine);
        }
        scoreIncreaseCoroutine = StartCoroutine(ScoreIncreaseCoroutine());
    }
    
    // 점수 증가 중지
    void StopScoreIncrease()
    {
        if (scoreIncreaseCoroutine != null)
        {
            StopCoroutine(scoreIncreaseCoroutine);
            scoreIncreaseCoroutine = null;
        }
    }
    
    // 점수 증가 코루틴
    IEnumerator ScoreIncreaseCoroutine()
    {
        while (isAttached)
        {
            // 현재 게임 시간 계산
            float currentGameTime = Time.time - gameStartTime;
            
            // 점수 증가량 계산
            float scoreToAdd = CalculateScoreToAdd(currentGameTime);
            currentScore += scoreToAdd;
            
            // DataBase의 점수도 업데이트
            cachedTeddyBearScore = currentScore;
            
            // HeatUI에 점수 업데이트 알림 (게임매니저를 통해)
            NotifyScoreUpdate();
            
            // 설정된 틱만큼 대기
            yield return new WaitForSeconds(cachedScoreGetTick);
        }
    }
    
    // 게임 시간에 따른 점수 증가량 계산 (GameManager 기반)
    float CalculateScoreToAdd(float gameTime)
    {
        // GameManager에서 실시간 배율 가져오기
        float currentMultiplier = 1f;
        if (GameManager.Instance != null)
        {
            currentMultiplier = GameManager.Instance.GetScoreMultiplier();
        }
        
        // currentScoreMultiplier를 실시간으로 동기화
        currentScoreMultiplier = currentMultiplier;
        
        // 초기 점수에 배율 적용
        return cachedInitialScore * currentMultiplier;
    }
    
    // 점수 업데이트 알림 (게임매니저나 UI 시스템에 전달)
    void NotifyScoreUpdate()
    {
        // GameManager가 있다면 점수 업데이트 알림
        if (GameManager.Instance != null)
        {
            // GameManager의 실시간 배율로 currentScoreMultiplier 동기화
            currentScoreMultiplier = GameManager.Instance.GetScoreMultiplier();
            GameManager.Instance.UpdateTeddyBearScore(currentScore);
        }
    }

    //테디베어 점수 증가 메서드 - 외부 호출용
    void TeddyBearScoreIncrease()
    {
        if (isAttached)
        {
            float currentGameTime = Time.time - gameStartTime;
            float scoreToAdd = CalculateScoreToAdd(currentGameTime);
            currentScore += scoreToAdd;
            cachedTeddyBearScore = currentScore;
            NotifyScoreUpdate();
        }
    }

    //테디베어 점수 초기화 메서드 - 개발자 테스트 용 메서드
    void TeddyBearScoreReset()
    {
        currentScore = 0f;
        currentScoreMultiplier = 1f;
        cachedTeddyBearScore = 0f;
        gameStartTime = Time.time; // 게임 시작 시간도 리셋
        lastDetachTime = -999f; // 재부착 방지 시간도 리셋

    }

    

    void OnShootPressed()
    {
        Debug.Log("ShootPressed");
    }



    #region 외부 호출용 메서드 모음
    
    // 기본 부착 해제 기능 - 현재 위치에 떨구기
    public void DetachFromPlayer()
    {     
        if (!isAttached) 
        {
            return;
        }
        
        isAttached = false;
        
        // 현재 위치 저장 (떨굴 위치)
        Vector3 currentPos = transform.position;
        Quaternion currentRot = transform.rotation;
        
        // 원본 부모로 복원
        transform.SetParent(originalParent);
        
        // 현재 위치에 떨구기 (원래 위치가 아닌)
        transform.position = currentPos;
        transform.rotation = currentRot;
        
        // 물리적 상호작용 다시 활성화
        if (teddyRigidbody != null)
        {
            teddyRigidbody.isKinematic = false;
            teddyRigidbody.useGravity = true;
            
            // 플레이어 앞쪽 방향으로 힘 가하기 (밀어내기)
            if (playerTransform != null)
            {
                Vector3 pushDirection = playerTransform.forward + Vector3.up * 0.5f; // 약간 위쪽으로도 힘 가하기
                float pushForce = 5f; // 밀어내는 힘의 강도
                teddyRigidbody.AddForce(pushDirection * pushForce, ForceMode.Impulse);

            }
        }
        
        // 콜라이더 다시 활성화
        if (colliderTeddyBear != null)
        {
            colliderTeddyBear.enabled = true;
        }
        
        // 재부착 방지 시간 기록
        lastDetachTime = Time.time;
        
        playerTransform = null;

        #warning Static으로 선언되어 있음. 최적화를 위해 수정 필요
        TestShoot.SetIsShooting(true);

        
        // 점수 증가 중지
        StopScoreIncrease();
        
        // 분리되면 다시 발광 시작
        StartGlowing();
    }
    
    // 아이템 사용 시 원래 위치로 되돌아가는 부착 해제 기능
    public void DetachAndReturnToOriginal()
    {
        if (!isAttached) return;
        
        isAttached = false;
        
        // 원본 부모로 복원
        transform.SetParent(originalParent);
        
        // 원본 위치로 복원
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        
        // 물리적 상호작용 다시 활성화
        if (teddyRigidbody != null)
        {
            teddyRigidbody.isKinematic = false;
            teddyRigidbody.useGravity = true;
        }
        
        // 콜라이더 다시 활성화
        if (colliderTeddyBear != null)
        {
            colliderTeddyBear.enabled = true;
        }
        
        // 재부착 방지 시간 기록
        lastDetachTime = Time.time;
        
        playerTransform = null;
        
        // 점수 증가 중지
        StopScoreIncrease();
        
        // 분리되면 다시 발광 시작
        StartGlowing();
    }


    //아이템 사용 효과 전용 메서드
    public void TeddyBearGlowingOff()
    {
        StopGlowing();
    }
    
    // 외부에서 발광 시작
    public void TeddyBearGlowingOn()
    {
        StartGlowing();
    }

   
    // 현재 부착 상태 확인
    public bool IsAttached()
    {
        return isAttached;
    }
    
    // 현재 발광 상태 확인
    public bool IsGlowing()
    {
        return isGlowing;
    }
    
    // 현재 점수 가져오기 (HeatUI에서 사용)
    public float GetCurrentScore()
    {
        return currentScore;
    }
    
    // 현재 점수 배율 가져오기 (GameManager 실시간 값 사용)
    public float GetCurrentScoreMultiplier()
    {
        // GameManager에서 실시간 배율 가져오기 (더 정확함)
        if (GameManager.Instance != null)
        {
            float realtimeMultiplier = GameManager.Instance.GetScoreMultiplier();
            currentScoreMultiplier = realtimeMultiplier; // 내부 값도 동기화
            return realtimeMultiplier;
        }
        
        // GameManager가 없으면 내부 값 사용
        return currentScoreMultiplier;
    }
    
    // 게임 시간 가져오기
    public float GetGameTime()
    {
        return Time.time - gameStartTime;
    }
    
    // 점수 수동 추가 (아이템 사용 등)
    public void AddScore(float additionalScore)
    {
        currentScore += additionalScore;
        cachedTeddyBearScore = currentScore;
        NotifyScoreUpdate();
    }
    
    // 점수 초기화 (외부 호출용)
    public void ResetScore()
    {
        TeddyBearScoreReset();
    }
    
    // 재부착까지 남은 시간 가져오기
    public float GetTimeUntilReattach()
    {
        float timeSinceDetach = Time.time - lastDetachTime;
        float remainingTime = cachedDetachReattachTime - timeSinceDetach;
        return Mathf.Max(0f, remainingTime);
    }
    
    // 재부착 가능한 상태인지 확인
    public bool CanReattach()
    {
        float timeSinceDetach = Time.time - lastDetachTime;
        return timeSinceDetach >= cachedDetachReattachTime;
    }

    #endregion

    #region 이벤트 핸들러

    /// <summary>
    /// 플레이어 사망 시 호출되는 이벤트 핸들러
    /// </summary>
    /// <param name="deadPlayer">사망한 플레이어의 LivingEntity</param>
    private void OnPlayerDied(LivingEntity deadPlayer)
    {
        // 테디베어가 부착되지 않은 상태면 무시
        if (!isAttached)
        {
            return;
        }

        // 현재 테디베어를 들고 있는 플레이어와 사망한 플레이어가 같은지 확인
        if (playerTransform != null && deadPlayer != null)
        {
            // 사망한 플레이어의 Transform과 현재 부착된 플레이어의 Transform이 같은지 확인
            if (playerTransform == deadPlayer.transform)
            {
                Debug.Log($"✅ TestTeddyBear - 플레이어 사망으로 인한 테디베어 자동 분리: {deadPlayer.name}");
                DetachFromPlayer();
            }
        }
    }

    #endregion
}
