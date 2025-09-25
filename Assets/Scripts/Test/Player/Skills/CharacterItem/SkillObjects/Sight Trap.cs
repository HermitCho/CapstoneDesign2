using Photon.Pun;
using UnityEngine;

public class SightTrap : MonoBehaviourPun
{
    private int ownerActorNumber;
    private float lifetime;
    private bool isActivated = false;

    [PunRPC]
    public void InitializeTrap(int ownerId, float life)
    {
        ownerActorNumber = ownerId;
        lifetime = life;

        // 일정 시간 후 자동 제거
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[SightTrap] OnTriggerEnter 호출됨 - other={other.name}");
        if (isActivated) return; // 중복 발동 방지
                                 //if (!PhotonNetwork.IsMasterClient) return; // 마스터만 판정

        if (other.CompareTag("Player"))
        {
            MoveController enemy = other.GetComponent<MoveController>();
            if (enemy == null)
                enemy = other.GetComponentInParent<MoveController>();

            if (enemy == null)
            {
                Debug.Log("[SightTrap] MoveController를 찾을 수 없음");
                return;
            }

            // 설치자 본인은 무시
            //if (enemy.photonView.OwnerActorNr == ownerActorNumber) return;

            isActivated = true;
            Debug.Log($"[SightTrap] 함정 발동! 피해자: {enemy.name}");

            // 발동 효과 실행 (설치자에게만 보여주기)
            photonView.RPC(nameof(ProvidesVisibility), RpcTarget.All, enemy.photonView.ViewID);

            // 발동 후 제거
            PhotonNetwork.Destroy(gameObject);
        }
    }

    [PunRPC]
    void ProvidesVisibility(int enemyViewId)
    {
        // 설치자만 실행
        if (PhotonNetwork.LocalPlayer.ActorNumber != ownerActorNumber) return;

        PhotonView targetView = PhotonView.Find(enemyViewId);
        if (targetView == null) return;

        MoveController enemy = targetView.GetComponent<MoveController>();
        if (enemy == null) return;

        // Outline 효과 붙이기
        var outline = enemy.GetComponent<Outline>();
        if (outline == null) outline = enemy.gameObject.AddComponent<Outline>();

        outline.OutlineMode = Outline.Mode.SilhouetteOnly;
        outline.OutlineColor = Color.red;
        outline.OutlineWidth = 6f;
        outline.enabled = true;

        // 5초 후 자동 해제
        enemy.StartCoroutine(DisableOutlineAfterDelay(outline, 5f));
    }

    private System.Collections.IEnumerator DisableOutlineAfterDelay(Outline outline, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (outline != null) outline.enabled = false;
    }
}
