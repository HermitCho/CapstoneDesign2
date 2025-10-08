using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SlowField : MonoBehaviourPun
{
    private List<MoveController> slowedPlayers = new List<MoveController>();
    private float slowAmountMultiplier = 0.9f;
    private float fieldLifeTime = 5f;
    private AudioSource aS;
    [SerializeField] private AudioClip impactSound; // 충돌 소리

    void Awake()
    {
        aS = GetComponent<AudioSource>();
        if (photonView != null)
        {
            photonView.RPC("PlayImpactSound", RpcTarget.All);
            photonView.RPC("RPCSlowFieldLifetime", RpcTarget.All);
        }
        else
        {
            Debug.LogError("SlowField 스크립트에 PhotonView가 없습니다. GameObject에 PhotonView 컴포넌트를 추가해주세요.");
            // PhotonView가 없으면 네트워크 관련 동작을 할 수 없으므로 
            // 네트워크와 무관한 로직만 실행하거나, 이 오브젝트를 파괴하는 것을 고려해야 합니다.
        }
    }

    [PunRPC]
    private void RPCSlowFieldLifetime()
    {
        StartCoroutine(SlowFieldLifetime());
    }

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

    [PunRPC]
    private void OnDestroy()
    {
        // 오브젝트가 파괴될 때 모든 플레이어의 슬로우 효과를 해제
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (MoveController player in slowedPlayers)
        {
            if (player != null)
            {
                player.photonView.RPC("RemoveSlowEffect", RpcTarget.All);
            }
        }
        slowedPlayers.Clear(); // 리스트 초기화
    }

    [PunRPC]
    private void PlayImpactSound()
    {
        Debug.Log("[Slowball] 임팩트 사운드 확인!");
        if (aS != null && impactSound != null)
        {
            Debug.Log("[Slowball] 임팩트 사운드 출력!");
            aS.PlayOneShot(impactSound);
        }
    }
}
