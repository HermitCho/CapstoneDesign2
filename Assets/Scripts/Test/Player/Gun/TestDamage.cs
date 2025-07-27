using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDamage : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        IDamageable target = collision.collider.GetComponent<IDamageable>();
        if (target != null)
        {
            Vector3 hitPoint = collision.contacts[0].point;
            Vector3 hitNormal = collision.contacts[0].normal;
            target.OnDamage(10f, hitPoint, hitNormal);
        }
    }
}
