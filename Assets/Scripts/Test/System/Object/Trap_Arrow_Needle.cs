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

        if (Trap_Arrow != null && Trap_Arrow.photonView != null && Trap_Arrow.photonView.ViewID != 0)
        {
            Trap_Arrow.photonView.RPC("RPC_arrowOff", RpcTarget.All);
        }
        else
        {
            Debug.LogError($"RPC_arrowOff 실패: 부모 Trap_Arrow 또는 PhotonView가 유효하지 않습니다. (Trap_Arrow null: {Trap_Arrow == null})");
        }
    }
}
