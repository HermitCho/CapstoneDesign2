using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class HiddenSmoke : MonoBehaviourPun
{
    [SerializeField] GameObject SmokeEffect;
    [SerializeField] AudioClip SmokeSound;
    [SerializeField] private float smokeDuration = 10f;  // 연막 지속 시간
    AudioSource aS;

    private void Awake()
    {
        aS = GetComponent<AudioSource>();
    }

    [PunRPC]
    public void InitializeHiddenSmoke()
    {
        
    }

    IEnumerator StartSmokeEffect()
    {
        Debug.Log("[SmokeBomb] 연막 효과 시작!");
        SmokeEffect.SetActive(true);
        SmokeEffect.GetComponent<ParticleSystem>().Play();

        if (aS != null && SmokeSound != null)
            aS.PlayOneShot(SmokeSound);

        yield return new WaitForSeconds(smokeDuration);

        SmokeEffect.SetActive(false);

        if (photonView.IsMine)
            PhotonNetwork.Destroy(gameObject); // 오브젝트 전체 제거
    }
}
