using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Cysharp.Threading.Tasks;

public class Trap_Arrow_Needle : MonoBehaviourPun
{
    private float niddleOnTime = 2f;
    private Trap_Arrow Trap_Arrow;

    void Awake()
    {
        Trap_Arrow = GetComponentInParent<Trap_Arrow>();
    }

    [PunRPC]
    void RPC_niddleOn()
    {
        niddleOn().Forget();
    }

    public async UniTask niddleOn()
    {
        transform.Translate(Vector3.up * 0.5f);
        await UniTask.Delay((int)(niddleOnTime * 1000));
        transform.Translate(Vector3.up * -0.5f);

        // ✅ PhotonView 유효성 확인 후 RPC 호출 또는 로컬 호출
        if (Trap_Arrow != null)
        {
            if (Trap_Arrow.photonView != null && Trap_Arrow.photonView.ViewID != 0)
            {
                // 네트워크 모드: RPC 사용
                Trap_Arrow.photonView.RPC("RPC_arrowOff", RpcTarget.All);
            }
            else
            {
                // 로컬 모드: 직접 호출
                Trap_Arrow.RPC_arrowOff();
            }
        }
        else
        {
            Debug.LogError($"[Trap_Arrow_Needle] {gameObject.name} 부모 Trap_Arrow를 찾을 수 없습니다.");
        }
    }
}
