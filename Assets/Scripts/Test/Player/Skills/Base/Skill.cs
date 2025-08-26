using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public abstract class Skill : MonoBehaviour
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

    #endregion

    private float lastUseTime;

    public bool CanUse => Time.time - lastUseTime >= cooldown;
    public bool IsCasting => Time.time - lastUseTime < castTime;

    public void ActivateSkill(MoveController executor)
    {
        if (!CanUse) return;
        lastUseTime = Time.time;

        if(castTime > 0f)
        {
            executor.photonView.RPC(
                "CastExecuteSkill",
                RpcTarget.All,
                this.index,
                executor.transform.position,
                executor.transform.forward
            );
            StartCoroutine(DelaySkillExecute(executor, castTime));
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
        lastUseTime = Time.time;
        
        if(castTime > 0f)
        {
            executor.photonView.RPC(
                "CastExecuteItem",
                RpcTarget.All,
                this.index,
                executor.transform.position,
                executor.transform.forward
            );
            StartCoroutine(DelayItemExecute(executor, castTime));
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

    private IEnumerator DelaySkillExecute(MoveController executor, float delay)
    {
        yield return new WaitForSeconds(delay);
        executor.photonView.RPC(
            "ExecuteSkill",
            RpcTarget.All,
            this.index,
            executor.transform.position,
            executor.transform.forward
        );
    }

    private IEnumerator DelayItemExecute(MoveController executor, float delay)
    {
        yield return new WaitForSeconds(delay);
        executor.photonView.RPC(
            "ExecuteItem",
            RpcTarget.All,
            this.index,
            executor.transform.position,
            executor.transform.forward
        );
    }

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
            if(isFollowing)
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
            if(isCastingFollowing)
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

}
