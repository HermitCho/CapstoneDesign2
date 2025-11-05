using System.Collections;
using Photon.Pun;
using UnityEngine;

public class SightTrap : MonoBehaviourPun
{
    private int ownerActorNumber;
    private float lifetime;
    private bool isActivated = false;
    private AudioSource aS;
    [SerializeField] AudioClip TrapActivateSound;

    [PunRPC]
    public void InitializeTrap(int ownerId, float life)
    {
        ownerActorNumber = ownerId;
        lifetime = life;

        // 일정 시간 후 자동 제거
        Destroy(gameObject, lifetime);
        aS = GetComponent<AudioSource>();
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
            if (enemy.photonView.OwnerActorNr == ownerActorNumber) return;

            isActivated = true;
            Debug.Log($"[SightTrap] 함정 발동! 피해자: {enemy.name}");

            // 발동 효과 실행 (설치자에게만 보여주기)
            photonView.RPC(nameof(ProvidesVisibility), RpcTarget.All, enemy.photonView.ViewID);

            if (aS != null && TrapActivateSound != null)
                aS.PlayOneShot(TrapActivateSound);

            // 소유자 파괴 처리
            if (photonView.Owner != null)
                photonView.RPC(nameof(RequestDestroyByOwner), photonView.Owner);
            else if (PhotonNetwork.IsMasterClient)
            {
                photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
                PhotonNetwork.Destroy(photonView.gameObject);
            }
        }
    }
    
    [PunRPC]
    private void RequestDestroyByOwner()
    {
        if (photonView.IsMine)
        {
            StartCoroutine(DestroyAfterDelay());
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        PhotonNetwork.Destroy(gameObject);
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
        if (outline == null)
        {
            // Renderer 단위로 Outline 붙이지 말고,
            // "MainBody" 같은 특정 오브젝트만 찾아서 Outline 붙이는 것도 가능
            var body = enemy.transform.Find("Bodies");
            if (body != null)
                outline = body.gameObject.AddComponent<Outline>();
        }

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
