using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class HealSmoke : MonoBehaviourPun
{
    private int OnwerId;
    private float healAmount = 5f;
    private float lastHealTime = 0f;
    private float healCoolTime = 1f;

    public void GetOnwerPhotonviewID(int id)
    {
        OnwerId = id;
    }

    void OnTriggerStay(Collider character)
    {
        var pv = character.GetComponentInParent<PhotonView>();

        if (pv != null && photonView.IsMine)
            if (Time.time - lastHealTime > healCoolTime)
                photonView.RPC("HealingSmokeActive", RpcTarget.All, pv.ViewID);
    }

    [PunRPC]
    void HealingSmokeActive(int targetViewID)
    {
        lastHealTime = Time.time;
        PhotonView targetPV = PhotonView.Find(targetViewID);
        if (targetPV != null)
        {
            LivingEntity living = targetPV.GetComponent<LivingEntity>();
            if (living != null)
            {
                living.RestoreHealth(healAmount);
                Debug.Log($"[HealSmoke] {targetPV.gameObject.name} 체력 {healAmount} 회복");
            }
            else
            {
                Debug.LogWarning("[HealSmoke] LivingEntity가 없음");
            }
        }
        else
        {
            Debug.LogWarning("[HealSmoke] PhotonView를 찾을 수 없음");
        }
    }
}
