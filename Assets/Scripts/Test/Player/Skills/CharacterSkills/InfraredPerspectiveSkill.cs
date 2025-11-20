using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class infraredPerspectiveSkill : Skill
{
    [Header("Reveal Settings")]
    [SerializeField] private float revealDuration = 5f;
    [SerializeField] private Color revealColor = Color.red; // 적에게 씌울 색상
    [SerializeField] private float outlineWidth = 4f;       // 외곽선 두께
    [SerializeField] private float flashSpeed = 0.5f;       // 깜빡이는 속도

    // 생성된 Outline 컴포넌트들을 추적하여 나중에 제거하기 위한 리스트
    private List<Outline> activeOutlines = new List<Outline>();

    // 이미 실행 중인 코루틴을 추적하여 중복 실행 방지
    private Coroutine revealCoroutine;

    void Awake()
    {
        // 부모 클래스(Skill)의 duration 설정
        duration = revealDuration;
    }

    public override void Execute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        base.Execute(executor, pos, dir);

        if (executor.photonView.IsMine)
        {
            // 나 자신의 화면에서만 적들이 보이게 처리
            photonView.RPC(nameof(RevealEnemiesForLocalPlayer), RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        // (선택 사항) 다른 사람들에게 스킬 썼다는 이펙트 보여주기
        PlayFollowEffectAtRemote(executor);
    }

    [PunRPC]
    private void RevealEnemiesForLocalPlayer(int ownerActorNumber)
    {
        // 스킬 시전자(나)만 이 로직을 수행함
        if (PhotonNetwork.LocalPlayer.ActorNumber != ownerActorNumber) return;

        // 기존에 실행 중인 효과가 있다면 정리하고 다시 시작
        CleanupOutlines();
        if (revealCoroutine != null) StopCoroutine(revealCoroutine);

        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in allPlayers)
        {
            PhotonView pv = player.GetComponent<PhotonView>();

            if (pv != null)
            {
                // 1. AI인지 확인 (AIHealth 컴포넌트 유무로 판단)
                bool isAI = player.GetComponent<AIBot>() != null;

                // 2. 필터링 로직 수정
                // "내 소유(pv.IsMine)이면서 AI가 아닌 경우(플레이어 자신)"에만 건너뜀.
                // 즉, 내 소유라도 AI라면(isAI == true) 아래 코드가 실행되어 실루엣이 보임.
                if (pv.IsMine && !isAI) continue;
            }

            // (필요시 AI 제외 로직 유지 - AIHealth가 없는 다른 AI가 있다면 여기서 처리)
            // if (player.layer == LayerMask.NameToLayer("AI")) continue;

            Renderer[] targetRenderers = player.GetComponentsInChildren<Renderer>();

            foreach (Renderer r in targetRenderers)
            {
                if (r == null) continue;

                // 파티클 시스템이나 트레일 등은 아웃라인 제외
                if (r is ParticleSystemRenderer || r is TrailRenderer) continue;

                // Outline 컴포넌트 동적 추가
                // 이미 Crown 등을 쓰고 있어서 Outline이 있을 수 있으므로 확인
                Outline outline = r.gameObject.GetComponent<Outline>();

                if (outline == null)
                {
                    // 없으면 새로 추가
                    outline = r.gameObject.AddComponent<Outline>();
                }
                else
                {
                    // 있으면 켜주기 (설정은 아래에서 덮어씀)
                    outline.enabled = true;
                }

                // Wallhack(실루엣) 설정 적용
                outline.OutlineMode = Outline.Mode.SilhouetteOnly; // 벽 투시 모드
                outline.OutlineColor = revealColor;
                outline.OutlineWidth = outlineWidth;

                // 추적 리스트에 추가
                activeOutlines.Add(outline);
            }
        }

        // 깜빡이는 효과 코루틴 시작
        revealCoroutine = StartCoroutine(OutlineGlowingRoutine());
    }
    /// <summary>
    /// 지속 시간 동안 아웃라인을 깜빡이게 하는 루틴
    /// </summary>
    private IEnumerator OutlineGlowingRoutine()
    {
        float elapsedTime = 0f;

        // revealDuration 동안 반복
        while (elapsedTime < revealDuration)
        {
            // Fade In (안 보임 -> 보임)
            yield return StartCoroutine(FadeOutlineAlpha(0f, 1f, flashSpeed));
            elapsedTime += flashSpeed;

            if (elapsedTime >= revealDuration) break;

            // Fade Out (보임 -> 희미해짐) - 완전히 끄지 않고 0.3 정도로 유지하다 다시 켜지게 함
            yield return StartCoroutine(FadeOutlineAlpha(1f, 0.3f, flashSpeed));
            elapsedTime += flashSpeed;
        }

        // 시간 종료 후 정리
        CleanupOutlines();
    }

    /// <summary>
    /// 아웃라인 알파값을 부드럽게 변경
    /// </summary>
    IEnumerator FadeOutlineAlpha(float fromAlpha, float toAlpha, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t / duration);

            Color newColor = revealColor;
            newColor.a = alpha;

            // 활성화된 모든 아웃라인 색상 일괄 변경
            foreach (var outline in activeOutlines)
            {
                // 적이 죽거나 파괴되어 null이 될 수 있음
                if (outline != null)
                {
                    outline.OutlineColor = newColor;
                }
            }
            yield return null;
        }

        // 최종값 적용
        Color finalColor = revealColor;
        finalColor.a = toAlpha;
        foreach (var outline in activeOutlines)
        {
            if (outline != null) outline.OutlineColor = finalColor;
        }
    }

    /// <summary>
    /// 효과 종료 시 추가했던 Outline 컴포넌트 제거 및 초기화
    /// </summary>
    private void CleanupOutlines()
    {
        foreach (var outline in activeOutlines)
        {
            if (outline != null)
            {
                // InfraredSkill이 추가한 컴포넌트라고 가정하고 제거
                // 만약 원래 Outline이 있었던 캐릭터라면 enabled = false 로 하는 등 로직 분리가 필요할 수 있으나,
                // 보통 이런 투시 스킬은 끝나면 안 보이는게 맞으므로 제거(Destroy)가 깔끔함.
                Destroy(outline);
            }
        }
        activeOutlines.Clear();
    }
}