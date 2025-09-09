using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SlowField : MonoBehaviour
{
    private List<MoveController> slowedPlayers = new List<MoveController>();
    private float slowAmountMultiplier = 0.5f;
    private float fieldLifeTime = 5f;

    void Awake()
    {
        StartCoroutine(SlowFieldLifetime());
    }

    [PunRPC]
    private IEnumerator SlowFieldLifetime()
    {
        yield return new WaitForSeconds(fieldLifeTime);
        Destroy(gameObject);
    }

    [PunRPC]
    private void OnTriggerEnter(Collider other)
    {
        // 마스터 클라이언트에서만 처리
        if (!PhotonNetwork.IsMasterClient) return;

        MoveController player = other.GetComponent<MoveController>();
        if (player != null && !slowedPlayers.Contains(player))
        {
            // 속도 감소 적용
            player.photonView.RPC("ApplySlowEffect", RpcTarget.All, slowAmountMultiplier);
            slowedPlayers.Add(player);
        }
    }

    [PunRPC]
    private void OnTriggerExit(Collider other)
    {
        // 마스터 클라이언트에서만 처리
        if (!PhotonNetwork.IsMasterClient) return;

        MoveController player = other.GetComponent<MoveController>();
        if (player != null && slowedPlayers.Contains(player))
        {
            // 원래 속도로 복구
            player.photonView.RPC("RemoveSlowEffect", RpcTarget.All);
            slowedPlayers.Remove(player);
        }
    }
}
