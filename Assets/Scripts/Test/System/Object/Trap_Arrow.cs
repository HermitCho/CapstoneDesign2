using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Cysharp.Threading.Tasks;

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
        // 플레이어와의 충돌이 아니면 무시
        if (!collision.gameObject.CompareTag("Player")) return;

        bool gameOverCheck = GameManager.Instance.GetIsGameOver();
        if (needleOnAlready || gameOverCheck) return;

        // 각 클라이언트에서 한 번만 발동되도록 플래그 설정
        needleOnAlready = true;

        // 1) 데미지 처리 (마스터 클라이언트만 담당)
        if (PhotonNetwork.IsMasterClient)
        {
            var targetPV = collision.gameObject.GetComponent<MoveController>()?.photonView;
            if (targetPV != null)
            {
                targetPV.RPC("OnDamage", RpcTarget.AllViaServer, damage, collision.transform.position, Vector3.down,
                    (photonView != null && photonView.ViewID != 0) ? photonView.OwnerActorNr : 0);
            }
        }

        // 2) 사운드 재생 (모든 클라이언트에서 로컬로 재생)
        AudioManager.Inst?.PlayClipAtPoint(arrowOnSound, transform.position, 1f, 1f, null, transform);

        // 3) 바늘 애니메이션 실행
        bool hasValidPhotonView = (photonView != null && photonView.ViewID != 0);
        bool isMaster = PhotonNetwork.IsMasterClient;
        
        for (int i = 0; i < niddles.Length; i++)
        {
            if (niddles[i] == null) continue;
            
            var needle = niddles[i].GetComponent<Trap_Arrow_Needle>();
            if (needle == null) continue;
            
            if (hasValidPhotonView && isMaster)
            {
                // 네트워크 모드: 마스터만 RPC 전송 (중복 방지)
                var needlePV = needle.photonView;
                if (needlePV != null && needlePV.ViewID != 0)
                {
                    needlePV.RPC("RPC_niddleOn", RpcTarget.AllViaServer);
                }
                else
                {
                    // PhotonView가 없으면 로컬로 실행
                    needle.niddleOn().Forget();
                }
            }
            else
            {
                // 로컬 모드 또는 비마스터 클라이언트: 직접 실행
                needle.niddleOn().Forget();
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
