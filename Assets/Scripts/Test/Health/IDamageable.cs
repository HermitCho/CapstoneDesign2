using UnityEngine;
using Photon.Pun;
public interface IDamageable 
{
    [PunRPC]
    void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal, LivingEntity attacker);
}
