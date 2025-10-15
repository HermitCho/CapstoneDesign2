using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class infraredPerspectiveSkill : Skill
{
    private float revealDuration = 5f; // 빨갛게 표시되는 시간
    [SerializeField] private Color outlineColor = Color.red;
    [SerializeField] private float outlineWidth = 6f;


    void Awake()
    {
        duration = revealDuration;
    }

    public override void Execute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        base.Execute(executor, pos, dir);

        // ✅ 로컬 플레이어만 RPC 호출 (자기 시야에서만 보여야 하므로)
        if (executor.photonView.IsMine)
        {
            photonView.RPC(nameof(RevealEnemiesForLocalPlayer), RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    [PunRPC]
    private void RevealEnemiesForLocalPlayer(int ownerActorNumber)
    {
        // 🔹 오직 자기 자신(로컬)만 효과 적용
        if (PhotonNetwork.LocalPlayer.ActorNumber != ownerActorNumber) return;

        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in allPlayers)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv == null) continue;

            // 자기 자신은 제외
            if (pv.IsMine) continue;

            // Outline 컴포넌트 가져오기 (없으면 추가)
            Outline outline = player.GetComponent<Outline>();
            if (outline == null)
            {
                // Body, Model, 또는 Renderer가 있는 오브젝트 탐색
                Transform body = player.transform.Find("Bodies");
                if (body != null)
                {
                    outline = body.gameObject.AddComponent<Outline>();
                }
                else
                {
                    outline = player.AddComponent<Outline>();
                }
            }

            // Outline 속성 설정
            outline.OutlineMode = Outline.Mode.SilhouetteOnly;
            outline.OutlineColor = outlineColor;
            outline.OutlineWidth = outlineWidth;
            outline.enabled = true;

            // 일정 시간 후 자동 해제
            StartCoroutine(DisableOutlineAfterDelay(outline, duration));
        }
    }

    private IEnumerator DisableOutlineAfterDelay(Outline outline, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (outline != null)
            outline.enabled = false;
    }
}

