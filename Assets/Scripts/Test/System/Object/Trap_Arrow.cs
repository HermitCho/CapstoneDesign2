using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Trap_Arrow : MonoBehaviourPun
{
    [SerializeField] private GameObject[] niddles;
    [SerializeField] private float damage = 10f;
    [SerializeField] private AudioClip arrowOnSound;
    [SerializeField] private AudioClip arrowOffSound;
    private PhotonView pv;
    private bool needleOnAlready = false;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!PhotonNetwork.IsMasterClient) return; // 단 한 곳에서만 판정
        if (!collision.gameObject.CompareTag("Player")) return;

        bool gameOverCheck = GameManager.Instance.GetIsGameOver();

        if (needleOnAlready || gameOverCheck) return;

        needleOnAlready = true;

        // 1) 데미지는 서버 한 번만 발생시키고
        var targetPV = collision.gameObject.GetComponent<MoveController>()?.photonView;
        if (targetPV != null)
        {
            // 서버 경유 단일 브로드캐스트 권장
            targetPV.RPC("OnDamage", RpcTarget.AllViaServer, damage, collision.transform.position, Vector3.down, photonView.OwnerActorNr);
            AudioManager.Inst?.PlayClipAtPoint(arrowOnSound, transform.position, 1f, 1f, null, transform);
        }

        // 2) 바늘 On도 서버에서 한 번만 호출
        for (int i = 0; i < niddles.Length; i++)
        {
            var needle = niddles[i].GetComponent<Trap_Arrow_Needle>();
            var needlePV = needle?.photonView;
            if (needlePV != null && needlePV.ViewID != 0)
            {
                needlePV.RPC("RPC_niddleOn", RpcTarget.AllViaServer);
            }
        }
    }

    [PunRPC]
    public void RPC_arrowOff()
    {
        needleOnAlready = false;
        AudioManager.Inst?.PlayClipAtPoint(arrowOffSound, transform.position, 1f, 1f, null, transform);
    }
}
