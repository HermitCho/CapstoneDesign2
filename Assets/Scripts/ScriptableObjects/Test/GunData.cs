using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ScriptableObjects", menuName = "ScriptableObjects/Test/Gun")]
public class GunData : ScriptableObject
{
    [Header("총 기본 스탯탯")]
    public string gunName;
    public int damage;
    public float range;
    public float fireRate;
    public int maxAmmo;
    public int currentAmmo;
    public float reloadTime;

    [Header("Shotgun Specific (or Multi-pellet Gun)")]
    [Tooltip("총알 당 발사되는 펠릿(pellet)의 수. 1이면 일반 총처럼 작동합니다.")]
    public int pelletCount = 1; // 샷건 탄알 수 (기본값 1)
    [Tooltip("펠릿이 퍼지는 각도. 0이면 퍼지지 않습니다.")]
    public float spreadAngle = 0f; // 퍼짐 각도 (기본값 0)

    [Header("오디오 클립")]
    public AudioClip shotClip;
    public AudioClip reloadClip;

    [Header("총알 궤적")]
    public Material bulletTrailMaterial; // 총알 궤적 라인 렌더러의 Material
    public float bulletTrailStartWidth = 0.1f; // 시작 두께
    public float bulletTrailEndWidth = 0.1f; // 끝 두께
    public float bulletTrailDuration = 0.1f; // 궤적 유지 시간
}
