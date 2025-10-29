using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

public abstract class Skill : MonoBehaviourPun
{
    #region Serialized Fields

    [Header("스킬 기본 정보")]
    [Tooltip("스킬 이름")]
    [SerializeField] protected string skillName = "기본 스킬";
    [Tooltip("스킬 설명")]
    [SerializeField] protected string skillDescription = "스킬 설명";
    [Tooltip("재사용 대기시간")]
    [SerializeField] protected float cooldown;
    [Tooltip("스킬 지속시간")]
    [SerializeField] protected float duration = 0f; // 스킬 지속시간
    [Tooltip("이펙트 지속시간")]
    [SerializeField] protected float effectDuration = 0f;
    [Tooltip("캐스팅 이펙트 지속시간")]
    [SerializeField] protected float effectCastingDuration = 0f;
    [Tooltip("시전 시간 (0이면 즉시 발동)")]
    [SerializeField] protected float castTime = 0f;
    [Tooltip("아이템 사용 시 삭제 시간 (-1이면 삭제 안함)")]
    [SerializeField] protected float destroyTime = -1f; // 스킬 삭제 시간
    [Tooltip("스킬 이펙트 및 사운드가 플레이어를 따라가는지 여부")]
    [SerializeField] protected bool isFollowing = false;
    [Tooltip("캐스팅 중 스킬 이펙트 및 사운드가 플레이어를 따라가는지 여부")]
    [SerializeField] protected bool isCastingFollowing = false;

    [Tooltip("고유 인덱스 - 스킬, 아이템 인덱스 따로 동작함 (둘끼리는 인덱스 중복 가능)")]
    [SerializeField] protected int index = 0;

    [Space(10)]
    [Header("UI 요소")]
    [Tooltip("스킬 아이콘")]
    [SerializeField] protected Sprite skillIcon; // 스킬 아이콘
    [Tooltip("스킬 색상")]
    [SerializeField] protected Color skillColor = Color.white; // 스킬 색상

    [Space(10)]
    [Header("시각 효과")]
    [Tooltip("스킬 이펙트")]
    [SerializeField] protected ParticleSystem skillEffect; // 스킬 이펙트
    [Tooltip("캐스팅 이펙트")]
    [SerializeField] protected ParticleSystem castTimeSkillEffect; //스킬 시전 이펙트
    [Tooltip("스킬 사운드")]
    [SerializeField] protected AudioClip skillSound; // 스킬 사운드
    [Tooltip("캐스팅 이펙트 사운드")]
    [SerializeField] protected AudioClip castTimeSkillSound; // 스킬 시전 사운드

    [Header("아이템 가격 - 해당 프리팹이 아이템인 경우 사용")]
    [SerializeField] protected int price = 1;

    [Header("스킬 애니메이션 트리거 이름")]
    [SerializeField] protected string skillAnimationTriggerName = "None";
    #endregion

    #region Properties

    public string SkillName => skillName;
    public string SkillDescription => skillDescription;
    public float Cooldown => cooldown;
    public float Duration => duration;
    public float EffectDuration => effectDuration;
    public float EffectCastingDuration => effectCastingDuration;
    public float CastTime => castTime;
    public float DestroyTime => destroyTime;
    public Sprite SkillIcon => skillIcon;
    public Color SkillColor => skillColor;
    public ParticleSystem SkillEffect => skillEffect;
    public ParticleSystem CastTimeSkillEffect => castTimeSkillEffect;
    public AudioClip SkillSound => skillSound;
    public AudioClip CastTimeSkillSound => castTimeSkillSound;
    public int Price => price;
    public int Index => index;
    public string SkillAnimationTriggerName => skillAnimationTriggerName;
    public float RemainingCooldown => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));
    public int RemainingUses => usableCountComponent != null ? usableCountComponent.Remaining : int.MaxValue;


    protected IUsableCount _usableCount;
    protected IProjectilePreview _projPreview;
    protected IPlacementPreview _placementPreview;
    protected ProjectilePreviewComponent projectilePreviewComponent;
    protected PlacementPreviewComponent placementPreviewComponent;
    protected UsableCountComponent usableCountComponent;
    public bool HasPreview => projectilePreviewComponent != null || placementPreviewComponent != null;

    #endregion

    private float lastUseTime;
    public bool IsCasting => Time.time - lastUseTime < castTime;

    protected virtual void Awake()
    {
        Debug.Log($"[FlashItem] 생성됨 - {PhotonNetwork.LocalPlayer.NickName}, ViewID: {photonView.ViewID}");

        // 컴포넌트 캐싱 — 성능 개선
        if (TryGetComponent(out ProjectilePreviewComponent ppc))
        {
            projectilePreviewComponent = ppc;
            _projPreview = ppc;
        }
        else
            Debug.Log("[Skill] 못찾음 ");
        if (TryGetComponent(out PlacementPreviewComponent plp))
        {
            placementPreviewComponent = plp;
            _placementPreview = plp;
            Debug.Log("[Skill] 찾음 " + placementPreviewComponent);
            Debug.Log("[Skill] 프리뷰 그림 " + HasPreview);
        }
        else
            Debug.Log("[Skill] 못찾음 ");

        if (TryGetComponent(out UsableCountComponent ucc))
        {
            usableCountComponent = ucc;
            _usableCount = ucc;
            Debug.Log("[Skill] 찾음 " + usableCountComponent);
        }
        else
            Debug.Log("[Skill] 못찾음 ");
    }

    /// <summary>
    /// 현재 스킬 사용 가능한지 확인 (쿨타임 + 사용 횟수)
    /// </summary>
    public bool CanUse
    {
        get
        {
            if (Time.time - lastUseTime < cooldown)
            {
                Debug.Log($"[Skill] {skillName} CanUse 쿨타임 {Time.time - lastUseTime}");
                return false;
            }
            if (usableCountComponent != null)
            {
                Debug.Log(usableCountComponent.gameObject.name);
                if (usableCountComponent.Remaining <= 0)
                {
                    Debug.Log($"[Skill] {skillName} CanUse 호출됨, 횟수 {usableCountComponent.Remaining}");
                    return false;
                }
            }
            return true;
        }
    }

    public void ActivateSkill(SkillController executor)
    {
        Debug.Log("[Skill - ActiveSkill] 활성");
        if (!CanUse) return;

        // 사용 횟수 컴포넌트가 있으면 실제로 "Use()" 를 호출해서 감소시키자.
        // 횟수 제한이 있는 경우 -> Use() 실행
        if (_usableCount != null && !_usableCount.Use()) return;

        Debug.Log($"[Skill] {skillName} ActivateSkill 호출됨, 쿨다운 갱신");
        lastUseTime = Time.time;

        if (castTime > 0f)
        {
            executor.photonView.RPC(
                "CastExecuteSkill",
                RpcTarget.All,
                this.index,
                executor.transform.position,
                executor.transform.forward
            );
        }
        else
        {
            executor.photonView.RPC(
                "ExecuteSkill",
                RpcTarget.All,
                this.index,
                executor.transform.position,
                executor.transform.forward
            );
        }
    }

    public void ActivateItem(SkillController executor)
    {
        Debug.Log("[Skill - ActiveItem] 활성");
        if (!CanUse) return;

        // 횟수 제한이 있는 경우 -> Use() 실행
        if (_usableCount != null && !_usableCount.Use()) return;

        lastUseTime = Time.time;

        if (castTime > 0f)
        {
            Debug.Log("[Skill] CastExecuteItem 활성");
            executor.photonView.RPC(
                "CastExecuteItem",
                RpcTarget.All,
                this.index,
                executor.transform.position,
                executor.transform.forward
            );
        }
        else
        {
            Debug.Log("[Skill] ExecuteItem 활성");
            executor.photonView.RPC(
                "ExecuteItem",
                RpcTarget.All,
                this.index,
                executor.transform.position,
                executor.transform.forward
            );
        }
    }

    // 실제 동작: 자기 자신만 실행
    public virtual void Execute(SkillController executor, Vector3 pos, Vector3 dir) { lastUseTime = Time.time; }
    public virtual void Execute(SkillController executorSkill, MoveController executorSkillMove, Vector3 pos, Vector3 dir) { lastUseTime = Time.time; }
    public virtual void CastExecute(SkillController executor, Vector3 pos, Vector3 dir) { lastUseTime = Time.time; }
    public virtual void CastExecute(SkillController executorSkill, MoveController executorSkillMove, Vector3 pos, Vector3 dir) { lastUseTime = Time.time; }

    protected void SpawnEffectFollow(ParticleSystem effectPrefab, Transform followTarget, float destroyDelay)
    {
        if (effectPrefab == null || followTarget == null) return;

        // ⭐ 1. 루트 오브젝트 인스턴스화 및 따라가도록 부모 설정
        var fxRoot = GameObject.Instantiate(effectPrefab.gameObject, followTarget.position, followTarget.rotation, followTarget);

        // 2. 모든 파티클 시스템 찾기 (루트와 자식 모두)
        ParticleSystem[] particleSystems = fxRoot.GetComponentsInChildren<ParticleSystem>(true);

        // 3. 모든 파티클 시스템 재생
        float maxDuration = 0f;
        foreach (var ps in particleSystems)
        {
            // 파티클 시스템이 이미 Play On Awake 상태일 수 있으나, 명시적으로 Play 호출
            ps.Play();

            // ⭐ 가장 긴 파티클 지속 시간을 파괴 딜레이 계산에 활용
            maxDuration = Mathf.Max(maxDuration, ps.main.duration);
        }

        // 4. 이펙트 파괴 타이머 설정
        if (destroyDelay > 0f)
        {
            // Skill의 effectDuration 또는 파티클의 최대 지속시간 중 큰 값 사용
            float actualDestroyDelay = Mathf.Max(destroyDelay, maxDuration);

            // ⭐ 지연 파괴 코루틴 시작
            StartCoroutine(DestroyGameObjectDelayed(fxRoot, actualDestroyDelay));
        }
        else // destroyDelay가 0인 경우, 파티클이 재생만 되고 Destroy 되지 않을 수 있습니다.
        {
            // destroyDelay가 명시적으로 0이거나 -1(즉시 파괴 안 함)일 때,
            // 파티클 시스템의 Stop Action에 의존하거나,
            // 파티클이 루핑이 아닌 경우 가장 긴 지속시간 후 파괴합니다.
            if (maxDuration > 0.01f)
            {
                StartCoroutine(DestroyGameObjectDelayed(fxRoot, maxDuration));
            }
            else
            {
                // 만약 이펙트가 루핑이 아닌 1회성이고, duration도 짧다면 0.1초 후 파괴 (안전 장치)
                // 하지만 일반적으로 파티클의 duration을 destroyDelay로 사용하는 것이 맞습니다.
                Destroy(fxRoot, 0.1f);
            }
        }
    }

    protected void SpawnEffectAtPosition(ParticleSystem effectPrefab, Vector3 pos, Quaternion rot, float destroyDelay)
    {
        if (effectPrefab == null) return;
        Debug.Log("이펙트 고정 - 복합 파티클 처리");

        // 1. 루트 오브젝트 인스턴스화
        var fxRoot = GameObject.Instantiate(effectPrefab.gameObject, pos, rot, null);

        // 2. 모든 파티클 시스템 찾기 (루트와 자식 모두)
        ParticleSystem[] particleSystems = fxRoot.GetComponentsInChildren<ParticleSystem>(true);

        // 3. 모든 파티클 시스템을 재생 및 Stop Action 설정
        float maxDuration = 0f;
        foreach (var ps in particleSystems)
        {
            // ⭐ Looping을 해제하거나, 코드로 재생을 제어하지 않을 경우
            //    Stop Action을 Destroy로 설정하여 파티클 종료 후 자동으로 파괴되도록 할 수 있습니다.
            // var main = ps.main;
            // main.stopAction = ParticleSystemStopAction.Destroy;

            ps.Play();
            // 가장 긴 지속 시간을 계산합니다. (Destroy 시간에 사용)
            maxDuration = Mathf.Max(maxDuration, ps.main.duration);
        }

        // 4. 이펙트 파괴 타이머 설정
        if (destroyDelay > 0f)
        {
            // Skill의 effectDuration 또는 파티클의 최대 지속시간 중 큰 값 사용
            float actualDestroyDelay = Mathf.Max(destroyDelay, maxDuration);

            // ⭐ 지연 파괴: Destroy(fxRoot, actualDestroyDelay); 대신 코루틴 사용
            //    (Destroy 로직이 이미 Skill 클래스 내부에 있으므로, 그대로 사용하거나 아래와 같이 명시적으로 변경)
            StartCoroutine(DestroyGameObjectDelayed(fxRoot, actualDestroyDelay));
        }
        // destroyDelay가 0인 경우, 파티클이 재생만 되고 Destroy 되지 않을 수 있습니다.
        else if (fxRoot.GetComponent<ParticleSystem>() == null)
        {
            // 루트에 ParticleSystem이 없다면 수동으로 파괴합니다. (0.1초 후)
            Destroy(fxRoot, 0.1f);
        }
    }

    protected IEnumerator DestroyGameObjectDelayed(GameObject target, float delay)
    {
        Debug.Log("[Skill - SpawnEffectFollow] 타겟 확인 " + target);
        yield return new WaitForSeconds(delay);
        Debug.Log("[Skill - SpawnEffectFollow] 타겟 확인 2 및 루틴 뒤로 돌아가는거 확인 " + target);
        if (target != null)
        {
            Debug.Log("[Skill - SpawnEffectFollow] 이펙트 파괴 시간 " + delay);
            Destroy(target);
        }
    }

    public void PlayEffectAtRemote(SkillController executor, Vector3 pos, Vector3 dir)
    {
        if (skillEffect != null && !isFollowing)
        {
            SpawnEffectAtPosition(skillEffect, pos, Quaternion.identity, effectDuration);
        }

        if (skillSound != null && AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayClipAtPoint(skillSound, executor.transform.position, 1f, 1f, null, executor.transform);
        }
    }

    public void PlayFollowEffectAtRemote(SkillController executor)
    {
        if (skillEffect != null && isFollowing)
        {
            SpawnEffectFollow(skillEffect, executor.transform, effectDuration);
        }

        if (skillSound != null && AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayClipAtPoint(skillSound, executor.transform.position, 1f, 1f, null, executor.transform);
        }
    }

    public void PlayFollowEffectOnHeartAtRemote(SkillController executor, Transform heartPosition)
    {
        if (skillEffect != null && isFollowing)
        {
            SpawnEffectFollow(skillEffect, heartPosition, effectDuration);
        }

        if (skillSound != null && AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayClipAtPoint(skillSound, executor.transform.position, 1f, 1f, null, executor.transform);
        }
    }

    public void PlayCastEffectAtRemote(SkillController executor, Vector3 pos, Vector3 dir)
    {
        if (castTimeSkillEffect != null && !isCastingFollowing)
        {
            SpawnEffectAtPosition(castTimeSkillEffect, pos, Quaternion.identity, effectCastingDuration);
        }

        if (castTimeSkillSound != null && AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayClipAtPoint(castTimeSkillSound, executor.transform.position, 1f, 1f, null, executor.transform);
        }
    }

    public void PlayFollowCastEffectAtRemote(SkillController executor)
    {
        if (castTimeSkillEffect != null && isCastingFollowing)
        {
            SpawnEffectFollow(castTimeSkillEffect, executor.transform, effectCastingDuration);
        }

        if (castTimeSkillSound != null && AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayClipAtPoint(castTimeSkillSound, executor.transform.position, 1f, 1f, null, executor.transform);
        }
    }

    public void PlayFollowCastEffectOnHeartAtRemote(SkillController executor, Transform heartPosition)
    {
        if (castTimeSkillEffect != null && isCastingFollowing)
        {
            SpawnEffectFollow(skillEffect, heartPosition, effectDuration);
        }

        if (castTimeSkillSound != null && AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayClipAtPoint(castTimeSkillSound, executor.transform.position, 1f, 1f, null, executor.transform);
        }
    }

    public virtual float GetProjectileSpeed() { return 10f; } //기본 값
    public virtual GameObject GetPlacementPrefab() { return null; }

    #region UseInterface
    public virtual void StartPreview(SkillController owner)
    {
        if (!owner.photonView.IsMine) return;

        if (projectilePreviewComponent != null)
        {
            projectilePreviewComponent.StartPreview(owner);
        }
        else
        {
            placementPreviewComponent?.GetGhostPrefab(GetPlacementPrefab());
            placementPreviewComponent?.StartPreview(owner);
        }
    }

    public virtual void UpdatePreview(SkillController owner, Vector3 origin, Vector3 direction, float initialSpeed = 10f)
    {
        if (!owner.photonView.IsMine) return;

        if (projectilePreviewComponent != null)
        {
            projectilePreviewComponent.UpdatePreview(origin, direction, GetProjectileSpeed());
        }
        else
        {
            placementPreviewComponent?.UpdatePreview(origin, Quaternion.LookRotation(direction));
        }
    }

    public virtual void EndPreview(SkillController owner)
    {
        if (!owner.photonView.IsMine) return;

        if (projectilePreviewComponent != null)
        {
            projectilePreviewComponent.EndPreview();
        }
        else
        {
            placementPreviewComponent?.EndPreview();
        }
    }
    #endregion

}
