using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class HealSmoke : MonoBehaviourPun
{
    private int OnwerId;
    private float healAmount = 5f;
    private Dictionary<int, float> lastHealTimes = new Dictionary<int, float>(); // 캐릭터별 힐 시간 관리
    private float healCoolTime = 1f;

    public void GetOnwerPhotonviewID(int id)
    {
        OnwerId = id;
    }

    void OnTriggerStay(Collider character)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var pv = character.GetComponentInParent<PhotonView>();
        if (pv == null) return;

        // 오너가 아니면 무시
        if (pv.ViewID != OnwerId) 
        {
            Debug.Log($"[HealSmoke] 오너가 아님: {pv.ViewID} (오너: {OnwerId})");
            return;
        }

        // 해당 캐릭터의 마지막 힐 시간 확인
        if (!lastHealTimes.ContainsKey(pv.ViewID))
        {
            lastHealTimes[pv.ViewID] = 0f;
        }

        if (Time.time - lastHealTimes[pv.ViewID] > healCoolTime)
        {
            Debug.Log($"[HealSmoke] 오너 발견! ViewID: {pv.ViewID}");
            
            lastHealTimes[pv.ViewID] = Time.time; // 힐 시간 업데이트
            photonView.RPC("HealingSmokeActive", RpcTarget.All, pv.ViewID, OnwerId);
        }
    }

    void OnTriggerExit(Collider character)
    {
        // 캐릭터가 연막을 벗어나면 해당 기록 삭제 (선택사항)
        var pv = character.GetComponentInParent<PhotonView>();
        if (pv != null && lastHealTimes.ContainsKey(pv.ViewID))
        {
            lastHealTimes.Remove(pv.ViewID);
        }
    }

    [PunRPC]
    void HealingSmokeActive(int targetViewID, int smokeOwnerID)
    {
        PhotonView targetPV = PhotonView.Find(targetViewID);
        if (targetPV != null)
        {
            LivingEntity living = targetPV.GetComponent<LivingEntity>();
            if (living != null)
            {
                targetPV.RPC("RestoreHealth", RpcTarget.All, healAmount);
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