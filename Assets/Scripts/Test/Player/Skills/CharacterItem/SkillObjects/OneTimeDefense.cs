using UnityEngine;
using Photon.Pun;

public class OneTimeDefense : MonoBehaviourPun
{
    private LivingEntity livingEntity;

    [PunRPC]
    public void InitializeShield(int livingViewId)
    {
        PhotonView targetView = PhotonView.Find(livingViewId);
        if (targetView == null)
        {
            Debug.LogWarning($"[OneTimeDefense] LivingEntity ViewID {livingViewId}를 찾을 수 없음");
            return;
        }

        livingEntity = targetView.GetComponent<LivingEntity>();
        if (livingEntity == null)
        {
            Debug.LogWarning("[OneTimeDefense] LivingEntity 컴포넌트를 찾을 수 없음");
            return;
        }

        if (livingEntity.photonView != null)
        {
            livingEntity.photonView.RPC("Set_Count_invincibility", RpcTarget.All, 1);
        }
    }

    private void Update()
    {
        // 🛡️ 오직 이 방어막 오브젝트의 소유자만 파괴 로직을 실행합니다.
        if (!photonView.IsMine) return;

        // LivingEntity가 초기화되지 않았거나 아직 유효하다면 종료
        if (livingEntity == null || livingEntity.HasInvincibilityCount()) return;

        // 파괴 조건 만족 (IsDead이거나 무적 카운트가 0일 때)
        if (livingEntity.IsDead || !livingEntity.HasInvincibilityCount())
        {
            // 📢 소유자만이 PhotonNetwork.Destroy를 호출하여 모든 클라이언트에서 제거합니다.
            // RPC를 사용하지 않고 바로 PhotonNetwork.Destroy를 호출하면 됩니다.
            // 왜냐하면 이 오브젝트는 이미 네트워크 오브젝트이며, 소유자가 파괴하면 자동으로 동기화됩니다.
            PhotonNetwork.Destroy(gameObject);
        }
    }

    [PunRPC]
    void RPC_SetParent(int parentViewId)
    {
        PhotonView parentView = PhotonView.Find(parentViewId);
        if (parentView != null)
        {
            transform.SetParent(parentView.transform, false);
        }
    }
}
