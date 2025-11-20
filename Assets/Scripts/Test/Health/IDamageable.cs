using UnityEngine;
using Photon.Pun;
public interface IDamageable 
{
   public int photonViewID { get; set; }
    [PunRPC]
    void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal, int attackerViewId);
}
