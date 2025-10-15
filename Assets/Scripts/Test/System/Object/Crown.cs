using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Photon.Pun;
using DG.Tweening;

public class Crown : MonoBehaviourPun
{

    private DataBase.TeddyBearData teddyBearData;
    private Collider crownCollider;
    private Rigidbody crownRigidbody;

    
    private PhotonView photonView;
    private PhotonView currentPlayerPhotonView;
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
    private float currentScore;
    
    //테디베어 재부착 방지 관련 변수
    private float lastDetachTime = -999f;
    
    //테디베어 발광 상태 확인 변수
    private bool isGlowing = false;
    //테디베어 부착 상태 확인 변수
    private bool isAttached = false;

    //테디베어 분리 이벤트
    public static event Action OnTeddyBearDetached;
    
    // 왕관 회전 애니메이션 관련
    private Tween crownRotationTween;

    //TeddyBear 캐시 변수
    private Vector3 cachedAttachOffset;
    private Vector3 cachedAttachRotation;
    private float cachedDetachReattachTime;
    private float cachedTeddyBearScore;
    private float cachedInitialScore;
    private float cachedScoreIncreaseRate;
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

        crownCollider = GetComponent<Collider>();
        crownRigidbody = GetComponent<Rigidbody>();
        photonView = GetComponent<PhotonView>();
        
        // Outline 컴포넌트 초기화
        InitializeOutline();
        
        // PhotonTransformView 회전 동기화 비활성화 (DOTween 회전과 충돌 방지)
        DisablePhotonRotationSync();
    }
    
    /// <summary>
    /// PhotonTransformView의 회전 동기화 비활성화
    /// DOTween 로컬 회전과 충돌 방지
    /// </summary>
    private void DisablePhotonRotationSync()
    {
        PhotonTransformView photonTransformView = GetComponent<PhotonTransformView>();
        if (photonTransformView != null)
        {
            photonTransformView.m_SynchronizeRotation = false;
        }
    }

    void OnEnable()
    {
        // ✅ 플레이어 사망 이벤트 구독
        LivingEntity.OnPlayerDied += OnPlayerDied;
        InputManager.OnDetachPressed += DetachFromPlayer;
    }

    void OnDisable()
    {
        // ✅ 플레이어 사망 이벤트 구독 해제
        LivingEntity.OnPlayerDied -= OnPlayerDied;
        InputManager.OnDetachPressed -= DetachFromPlayer;
        
        // 왕관 회전 애니메이션 정리
        StopCrownRotation();
    }

    // Start is called before the first frame update
    void Start()
    {
        // 원본 위치와 회전값 저장
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;
        
        // 초기 점수 설정
        currentScore = cachedTeddyBearScore;
        
        // 게임 시작 시 발광 시작
        StartGlowing();
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnCollisionEnter(Collision collision)
    {
       // if(!collision.gameObject.CompareTag("Player")) return;

        if (collision.gameObject.CompareTag("Player") && !isAttached)
        {   
            PhotonView playerPhotonView = collision.transform.GetComponent<PhotonView>();
            if (!playerPhotonView.IsMine) return;
            // 재부착 방지 시간 확인
            float timeSinceDetach = Time.time - lastDetachTime;
            if (timeSinceDetach >= cachedDetachReattachTime)
            {
                AttachToPlayer(playerPhotonView);
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



    void AttachToPlayer(PhotonView playerView )
    {
        if (isAttached) return;
        
        // 1) 소유권을 Crown 집은 플레이어에게 넘김
        photonView.TransferOwnership(playerView.Owner);
        currentPlayerPhotonView = playerView;

        // 2) 모든 클라이언트에게 Attach 사실 알림
        photonView.RPC("RpcAttachToPlayer", RpcTarget.AllBuffered, playerView.ViewID);

        #warning Static으로 선언되어 있음. 최적화를 위해 수정 필요
        if(playerView.IsMine)
        {
            TestShoot.SetIsShooting(false);
            isAttached = true;
        }
    }

    [PunRPC]
    private void RpcAttachToPlayer(int playerViewId)
    {
        PhotonView playerPV = PhotonView.Find(playerViewId);
        if (playerPV == null) return;

        playerTransform = playerPV.transform;

        // 플레이어 머리 위에 부착
        Vector3 targetPosition = playerPV.transform.position + playerPV.transform.forward * cachedAttachOffset.z + playerPV.transform.up * cachedAttachOffset.y + playerPV.transform.right * cachedAttachOffset.x;
        Quaternion targetRotation = playerPV.transform.rotation * Quaternion.Euler(cachedAttachRotation);

        
        transform.localPosition = targetPosition;
        transform.localRotation = targetRotation;
        transform.SetParent(playerTransform);

        if (crownRigidbody != null)
        {
            crownRigidbody.isKinematic = true;
            crownRigidbody.useGravity = false;
        }
        if (crownCollider != null)
        {
            crownCollider.enabled = false;
        }
        
        // 왕관 회전 애니메이션 시작
        StartCrownRotation();

        Debug.Log($"👑 Crown attached to {playerPV.Owner.NickName}");
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
    
    // 점수 업데이트 알림 (게임매니저나 UI 시스템에 전달)
    void NotifyScoreUpdate()
    {
        // GameManager가 있다면 점수 업데이트 알림
        if (GameManager.Instance != null)
        {
            // currentScore를 GameManager의 totalTeddyBearScore와 동기화
            GameManager.Instance.UpdateTeddyBearScore(currentScore);
        }
    }

    //테디베어 점수 초기화 메서드 - 개발자 테스트 용 메서드
    void TeddyBearScoreReset()
    {
        currentScore = 0f;
        cachedTeddyBearScore = 0f;
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
        if (currentPlayerPhotonView == null || !currentPlayerPhotonView.IsMine) return;
        if (!isAttached) return;
        
        // 현재 위치 저장 (떨굴 위치)
        Vector3 dropPosition = transform.position;
        

        // 모든 클라이언트에서 떨구기 상태 적용
        photonView.RPC("RpcDetachFromPlayer", RpcTarget.AllBuffered, transform.position);  

        // 재부착 방지 시간 기록
        lastDetachTime = Time.time;
        
        playerTransform = null;

        if(currentPlayerPhotonView.IsMine)
        {
            TestShoot.SetIsShooting(true);
            isAttached = false;
        }
        currentPlayerPhotonView = null;
    }

    [PunRPC]
    private void RpcDetachFromPlayer(Vector3 dropPosition)
    {
        playerTransform = null;
        
        // 회전 애니메이션 중지
        StopCrownRotation();

        // 부모 복원
        transform.SetParent(originalParent);

        // 위치, 회전 적용
        transform.position = dropPosition;
        transform.rotation = Quaternion.identity;

        // 물리 활성화
        if (crownRigidbody != null)
        {
            crownRigidbody.isKinematic = false;
            crownRigidbody.useGravity = true;

            // optional: 약간 힘을 가해 밀어내기
            if (playerTransform != null)
            {
             Vector3 pushDirection = playerTransform.forward + Vector3.up * 0.5f;
            float pushForce = 5f;
            crownRigidbody.AddForce(pushDirection * pushForce, ForceMode.Impulse);
            }
        }
        // 콜라이더 활성화
        if (crownCollider != null)
            crownCollider.enabled = true;

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
        if (crownRigidbody != null)
        {
            crownRigidbody.isKinematic = false;
            crownRigidbody.useGravity = true;
        }
        
        // 콜라이더 다시 활성화
        if (crownCollider != null)
        {
            crownCollider.enabled = true;
        }
        
        // 재부착 방지 시간 기록
        lastDetachTime = Time.time;
        
        playerTransform = null;
        
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
    

    
    // 점수 수동 추가/차감 (아이템 사용, 사망 시 손실 등)
    public void AddScore(float additionalScore)
    {
        // 점수 차감인 경우 (음수)
        if (additionalScore < 0f)
        {
            float scoreToSubtract = Mathf.Abs(additionalScore);
            
            // 현재 점수보다 많이 차감하려는 경우 방지
            if (scoreToSubtract > currentScore)
            {
                Debug.LogWarning($"⚠️ TestTeddyBear: 현재 점수({currentScore:F0})보다 많이 차감하려 함 - {scoreToSubtract:F0}, 0으로 설정");
                currentScore = 0f;
            }
            else
            {
                currentScore -= scoreToSubtract;
            }
            
            Debug.Log($"💯 TestTeddyBear - 점수 차감: -{scoreToSubtract:F0}, 남은 점수: {currentScore:F0}");
        }
        else
        {
            // 점수 증가인 경우 (양수)
            currentScore += additionalScore;
            Debug.Log($"✅ TestTeddyBear - 점수 증가: +{additionalScore:F0}, 총 점수: {currentScore:F0}");
        }
        
        cachedTeddyBearScore = currentScore;
        
        // 동기화가 아닌 실제 점수 변경인 경우에만 NotifyScoreUpdate 호출
        if (additionalScore != 0f)
        {
            NotifyScoreUpdate();
        }
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
    
    #region 왕관 회전 애니메이션
    
    /// <summary>
    /// 왕관 회전 애니메이션 시작 (플레이어 머리 위에서 빙글빙글)
    /// </summary>
    private void StartCrownRotation()
    {
        // 기존 애니메이션이 있다면 중지
        StopCrownRotation();
        
        // Y축 기준으로 360도 무한 회전
        // RotateMode.FastBeyond360: 360도를 넘어서도 계속 회전
        // SetLoops(-1, LoopType.Restart): 무한 반복, 0도로 돌아가서 다시 시작
        crownRotationTween = transform.DOLocalRotate(
            new Vector3(0f, 360f, 0f), // 목표 회전값 (Y축으로 360도)
            3f, // 3초에 한 바퀴 회전
            RotateMode.FastBeyond360
        )
        .SetEase(Ease.Linear) // 일정한 속도로 회전
        .SetLoops(-1, LoopType.Restart); // 무한 반복
    }
    
    /// <summary>
    /// 왕관 회전 애니메이션 중지
    /// </summary>
    private void StopCrownRotation()
    {
        if (crownRotationTween != null)
        {
            crownRotationTween.Kill();
            crownRotationTween = null;
        }
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
