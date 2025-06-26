using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ScriptableObjects", menuName = "ScriptableObjects/Test/Gun")]
public class Gun : ScriptableObject
{   
    [Header("총 이름")]
    public string gunName;
    
    [Header("총 데미지")]
    public int damage;

    [Header("총 사거리")]
    public float range;
    
    [Header("총 발사 속도")]
    public float fireRate;
    
    [Header("총 최대 탄약")]
    public int maxAmmo;
    
    [Header("총 현재 탄약")]
    public int currentAmmo;

    [Header("총 재장전 시간")]
    public float reloadTime;

}
