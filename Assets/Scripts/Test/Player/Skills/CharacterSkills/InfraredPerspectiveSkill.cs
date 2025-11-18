using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class infraredPerspectiveSkill : Skill
{
    private float revealDuration = 5f;
    [SerializeField] private Color revealColor = Color.red;
    [SerializeField] private float emissionIntensity = 5f;

    [Header("Wallhack Material")]
    [SerializeField] private Material wallhackMaterial; // ✅ 벽 통과 전용 머티리얼 (Inspector에서 설정)

    // 이미 발광 이펙트 코루틴이 실행 중인 Renderer를 추적하기 위한 Dictionary
    private Dictionary<Renderer, Coroutine> activeCoroutines = new Dictionary<Renderer, Coroutine>();

    // Renderer별로 원래의 머티리얼 배열을 저장할 Dictionary
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>(); // ✅ 원본 머티리얼 배열 저장용

    /// <summary>
    /// 머티리얼의 원본 렌더링 설정을 저장하는 구조체
    /// </summary>
    private struct MaterialSettings
    {
        public int renderQueue;
        // ZTest 및 ZWrite는 쉐이더 프로퍼티 이름이므로, 여기에 저장하지 않고 원본값만 저장
        public Color originalEmission;
        public bool wasEmissionEnabled;
    }


    void Awake()
    {
        duration = revealDuration;
    }

    public override void Execute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        base.Execute(executor, pos, dir);

        if (executor.photonView.IsMine)
        {
            photonView.RPC(nameof(RevealEnemiesForLocalPlayer), RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        PlayFollowEffectAtRemote(executor);
    }

    [PunRPC]
    private void RevealEnemiesForLocalPlayer(int ownerActorNumber)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber != ownerActorNumber) return;
        if (wallhackMaterial == null)
        {
            Debug.LogError("[Infrared] Wallhack Material이 할당되지 않았습니다.");
            return;
        }

        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in allPlayers)
        {
            Debug.Log("[RevealEnemiesForLocalPlayer] Player : " + player.name);
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine && player.layer != LayerMask.NameToLayer("AI"))
                continue;

            Renderer[] targetRenderers = player.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in targetRenderers)
            {
                if (renderer == null) continue;

                // 기존 코루틴이 있다면 중지
                if (activeCoroutines.ContainsKey(renderer) && activeCoroutines[renderer] != null)
                {
                    StopCoroutine(activeCoroutines[renderer]);
                }

                // 원본 머티리얼 저장 (새로 할당할 필요 없음)
                if (!originalMaterials.ContainsKey(renderer))
                {
                    // renderer.materials는 복사본을 반환합니다.
                    originalMaterials.Add(renderer, renderer.materials);
                }

                // 새로운 코루틴 시작
                Coroutine newCoroutine = StartCoroutine(RevealRenderer(renderer, duration));
                activeCoroutines[renderer] = newCoroutine;
            }
        }
    }

    /// <summary>
    /// 단일 Renderer에 Wallhack Material을 적용하고 시간 후 복원합니다.
    /// </summary>
    private IEnumerator RevealRenderer(Renderer renderer, float delay)
    {
        if (renderer == null) yield break;

        // 1. Wallhack Material 적용
        Material[] newMaterials = new Material[renderer.sharedMaterials.Length];

        // 모든 서브메시에 Wallhack 머티리얼을 할당합니다.
        for (int i = 0; i < newMaterials.Length; i++)
        {
            // ✅ 새로운 머티리얼 인스턴스를 생성하여 색상을 변경해도 다른 오브젝트에 영향이 없도록 합니다.
            Material instanceMat = new Material(wallhackMaterial);

            // ✅ Emission 색상 및 강도 설정
            if (instanceMat.HasProperty("_EmissionColor"))
            {
                instanceMat.EnableKeyword("_EMISSION");
                instanceMat.SetColor("_EmissionColor", revealColor * emissionIntensity);
            }

            newMaterials[i] = instanceMat;
        }

        // 머티리얼 배열을 덮어씌웁니다.
        renderer.materials = newMaterials;

        yield return new WaitForSeconds(delay);

        // ------------------ 복원 ------------------

        // 1. 원본 머티리얼 복원 및 임시 머티리얼 파괴
        if (originalMaterials.TryGetValue(renderer, out Material[] storedMaterials))
        {
            // 원래 머티리얼로 복원
            renderer.materials = storedMaterials;

            // 임시로 생성했던 머티리얼 인스턴스들을 파괴 (메모리 누수 방지)
            foreach (Material tempMat in newMaterials)
            {
                if (tempMat != null) Destroy(tempMat);
            }

            // Dictionary에서 제거
            originalMaterials.Remove(renderer);
        }

        if (activeCoroutines.ContainsKey(renderer))
        {
            activeCoroutines.Remove(renderer);
        }
    }

    [PunRPC]
    private void EffctOn()
    {

    }
}