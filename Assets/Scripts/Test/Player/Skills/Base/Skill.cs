using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

public abstract class Skill : MonoBehaviour
{
    #region Serialized Fields

    [Header("스킬 기본 정보")]
    [Tooltip("스킬 이름")]
    [SerializeField] protected string skillName = "기본 스킬";
    [Tooltip("스킬 설명")]
    [SerializeField] protected string skillDescription = "스킬 설명";
    [Tooltip("재사용 대기시간")]
    [SerializeField] protected int count;
    [Tooltip("스킬 사용 가능 횟수(아이템 스킬)")]
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
    public int Count => count;
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


    private IUsableCount _usableCount;
    private IProjectilePreview _projPreview;
    private IPlacementPreview _placementPreview;
    private ProjectilePreviewComponent projectilePreviewComponent;
    private PlacementPreviewComponent placementPreviewComponent;
    private UsableCountComponent usableCountComponent;
    #endregion

    private float lastUseTime;
    public bool IsCasting => Time.time - lastUseTime < castTime;

    protected void Awake()
    {
        // 컴포넌트 캐싱 — 성능 개선
        var comps = GetComponents<MonoBehaviour>();
        foreach (var c in comps)
        {
            if (c is IProjectilePreview pp) _projPreview = pp;
            if (c is IPlacementPreview pl) _placementPreview = pl;
            if (c is IUsableCount uc) _usableCount = uc;

            // 컴포넌트 캐싱 — 성능 개선
            if (TryGetComponent(out ProjectilePreviewComponent ppc))
            {
                projectilePreviewComponent = ppc;
                Debug.Log("[Skill] 찾음 " + projectilePreviewComponent);
            }

            if (TryGetComponent(out PlacementPreviewComponent plc))
            {
                placementPreviewComponent = plc;
                Debug.Log("[Skill] 찾음 " + projectilePreviewComponent);
            }

            if (TryGetComponent(out UsableCountComponent ucc))
            {
                usableCountComponent = ucc;
                Debug.Log("[Skill] 못찾음 ");
            }
            Debug.Log("[Skill] 찾음 " + projectilePreviewComponent);
        }
    }
    // 안전하게 찾는 헬퍼 (Awake에서 안찾혔을 경우를 대비)
    private IUsableCount GetUsableCountComponent()
    {
        if (_usableCount != null) return _usableCount;

        var comps = GetComponents<MonoBehaviour>();
        foreach (var c in comps)
        {
            if (c is IUsableCount uc)
            {
                _usableCount = uc;
                break;
            }
        }
        return _usableCount;
    }

    // (선택) CanUse에 사용 횟수 반영하려면 아래처럼.
    // 주의: 이 코드는 GetUsableCountComponent()를 호출하므로 성능 상
    // Awake에서 미리 캐싱하는 것을 권장합니다.
    public bool CanUse
    {
        get
        {
            // 쿨다운 체크
            if (Time.time - lastUseTime < cooldown) return false;

            // 사용 가능 횟수 체크 (있다면)
            var usable = _usableCount ?? GetUsableCountComponent();
            if (usable != null && usable.Remaining <= 0) return false;

            return true;
        }
    }

    public void ActivateSkill(MoveController executor)
    {
        if (!CanUse) return;

        // 사용 횟수 컴포넌트가 있으면 실제로 "Use()" 를 호출해서 감소시키자.
        var usable = _usableCount ?? GetUsableCountComponent();
        if (usable != null)
        {
            // Use()는 내부에서 Remaining 검사 후 성공 시 -1 하고 true 반환하도록 구현되어야 합니다.
            if (!usable.Use())
            {
                // 사용 불가(남은 횟수 없음) => UI 토스트 등
                return;
            }
        }
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
            //StartCoroutine(DelaySkillExecute(executor, castTime));
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

    public void ActivateItem(MoveController executor)
    {
        if (!CanUse) return;

        var usable = _usableCount ?? GetUsableCountComponent();
        if (usable != null)
        {
            if (!usable.Use())
            {
                // 사용 불가
                return;
            }
        }

        lastUseTime = Time.time;

        if (castTime > 0f)
        {
            executor.photonView.RPC(
                "CastExecuteItem",
                RpcTarget.All,
                this.index,
                executor.transform.position,
                executor.transform.forward
            );
            //StartCoroutine(DelayItemExecute(executor, castTime));
        }
        else
        {

            executor.photonView.RPC(
            "ExecuteItem",
            RpcTarget.All,
            this.index,
            executor.transform.position,
            executor.transform.forward
        );
        }
    }

    // private IEnumerator DelaySkillExecute(MoveController executor, float delay)
    // {
    //     yield return new WaitForSeconds(delay);
    //     executor.photonView.RPC(
    //         "ExecuteSkill",
    //         RpcTarget.All,
    //         this.index,
    //         executor.transform.position,
    //         executor.transform.forward
    //     );
    // }

    // private IEnumerator DelayItemExecute(MoveController executor, float delay)
    // {
    //     yield return new WaitForSeconds(delay);
    //     executor.photonView.RPC(
    //         "ExecuteItem",
    //         RpcTarget.All,
    //         this.index,
    //         executor.transform.position,
    //         executor.transform.forward
    //     );
    // }

    // 실제 동작: 자기 자신만 실행
    public virtual void Execute(MoveController executor, Vector3 pos, Vector3 dir) { }
    public virtual void CastExecute(MoveController executor, Vector3 pos, Vector3 dir) { }

    protected void SpawnEffectFollow(ParticleSystem effectPrefab, Transform followTarget, float destroyDelay)
    {
        if (effectPrefab == null) return;
        Debug.Log("이펙트 따라감");
        var fx = GameObject.Instantiate(effectPrefab, followTarget.position, followTarget.rotation, followTarget);
        fx.Play();
        Destroy(fx.gameObject, destroyDelay > 0f ? destroyDelay : 0f);
    }

    protected void SpawnEffectAtPosition(ParticleSystem effectPrefab, Vector3 pos, Quaternion rot, float destroyDelay)
    {
        if (effectPrefab == null) return;
        Debug.Log("이펙트 고정");
        var fx = GameObject.Instantiate(effectPrefab, pos, rot, null);
        fx.Play();
        Destroy(fx.gameObject, destroyDelay > 0f ? destroyDelay : 0f);
    }


    // 원격 클라이언트에서도 실행되는 이펙트/사운드
    public void PlayEffectAtRemote(MoveController executor, Vector3 pos, Vector3 dir)
    {
        if (skillEffect != null)
        {
            if (isFollowing)
            {
                SpawnEffectFollow(skillEffect, executor.transform, effectDuration);
                if (skillSound != null && AudioManager.Inst != null)
                    AudioManager.Inst.PlayClipAtPoint(skillSound, executor.transform.position, 1f, 1f, null, executor.transform);
            }
            else
            {
                SpawnEffectAtPosition(skillEffect, pos, Quaternion.identity, effectDuration);
                if (skillSound != null && AudioManager.Inst != null)
                    AudioManager.Inst.PlayClipAtPoint(skillSound, pos, 1f, 1f, null, executor.transform);
            }
        }
    }

    public void PlayCastEffectAtRemote(MoveController executor, Vector3 pos, Vector3 dir)
    {
        if (castTimeSkillEffect != null)
        {
            if (isCastingFollowing)
            {
                SpawnEffectFollow(castTimeSkillEffect, executor.transform, effectCastingDuration);
                if (castTimeSkillSound != null && AudioManager.Inst != null)
                    AudioManager.Inst.PlayClipAtPoint(castTimeSkillSound, executor.transform.position, 1f, 1f, null, executor.transform);
            }
            else
            {
                SpawnEffectAtPosition(castTimeSkillEffect, pos, Quaternion.identity, effectCastingDuration);
                if (castTimeSkillSound != null && AudioManager.Inst != null)
                    AudioManager.Inst.PlayClipAtPoint(castTimeSkillSound, pos, 1f, 1f, null, executor.transform);
            }
        }
    }

    public float RemainingCooldown
    {
        get
        {
            float elapsed = Time.time - lastUseTime;
            return Mathf.Max(0f, cooldown - elapsed);
        }
    }

    public virtual float GetProjectileSpeed() { return 10f; } //기본 값

    #region UseInterface
    public virtual void StartPreview(MoveController owner)
    {
        if (!owner.photonView.IsMine) return;

        if (projectilePreviewComponent != null)
        {
            Debug.Log("Skill _projPreview를 찾음!!!" + projectilePreviewComponent);
            projectilePreviewComponent.StartPreview(owner);
        }
        else
        {
            //placementPreviewComponent?.GetGhostPrefab()
            placementPreviewComponent?.StartPreview(owner);
        }
    }

    //placementPreviewComponent가 설치할 프리팹 전달용
    public virtual void StartPreview(MoveController owner, GameObject? placementPrefab)
    {
        if (!owner.photonView.IsMine) return;

        if (projectilePreviewComponent != null)
        {
            Debug.Log("Skill _projPreview를 찾음!!!" + projectilePreviewComponent);
            projectilePreviewComponent.StartPreview(owner);
        }
        else
        {
            placementPreviewComponent?.GetGhostPrefab(placementPrefab);
            placementPreviewComponent?.StartPreview(owner);
        }
    }

    public virtual void UpdatePreview(MoveController owner, Vector3 origin, Vector3 direction, float initialSpeed = 10f)
    {
        if (!owner.photonView.IsMine) return;

        if (projectilePreviewComponent != null)
        {
            Debug.Log("Skill _projPreview를 찾음!!!" + projectilePreviewComponent);
            projectilePreviewComponent.UpdatePreview(origin, direction, GetProjectileSpeed());
        }
        else
        {
            placementPreviewComponent?.UpdatePreview(origin, Quaternion.LookRotation(direction));
        }
    }

    public virtual void EndPreview(MoveController owner)
    {
        if (!owner.photonView.IsMine) return;

        if (projectilePreviewComponent != null)
        {
            Debug.Log("Skill _projPreview를 찾음!!!" + projectilePreviewComponent);
            projectilePreviewComponent.EndPreview();
        }
        else
        {
            placementPreviewComponent?.EndPreview();
        }
    }
    #endregion

}
