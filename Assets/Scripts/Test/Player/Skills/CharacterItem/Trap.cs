using Photon.Pun;
using UnityEngine;

public class Trap : MonoBehaviourPun
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
        if (isActivated) return; // 중복 발동 방지
        if (!PhotonNetwork.IsMasterClient) return; // 마스터만 판정

        MoveController enemy = other.GetComponent<MoveController>();
        if (enemy == null) return;

        // 설치자 본인은 무시
        if (enemy.photonView.OwnerActorNr == ownerActorNumber) return;

        isActivated = true;
        Debug.Log($"함정 발동! 피해자: {enemy.name}");

        // TODO: 데미지 주기
        // victim.photonView.RPC("OnDamage", RpcTarget.All, 20f, victim.transform.position, Vector3.up, ownerActorNumber);

        // 발동 후 제거
        PhotonNetwork.Destroy(gameObject);
    }
}
