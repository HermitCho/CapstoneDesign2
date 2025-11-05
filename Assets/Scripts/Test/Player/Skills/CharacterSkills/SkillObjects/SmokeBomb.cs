using Photon.Pun;
using UnityEngine;
using System.Collections;

public class SmokeBomb : MonoBehaviourPun
{
    [SerializeField] GameObject SmokeEffect;
    [SerializeField] AudioClip smokeSound;
    [SerializeField] private float smokeDuration = 7f;  // 연막 지속 시간
    Rigidbody rb;
    AudioSource aS;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        aS = GetComponent<AudioSource>();
    }

    [PunRPC]
    public void InitializeAndLaunch(int ownerId, Vector3 direction, float launchSpeed) // launchSpeed 파라미터 추가
    {
        rb.velocity = direction.normalized * launchSpeed;
        Debug.Log("[SmokeBomb] + InitializeAndLaunch 오너 아이디" + ownerId);
        SmokeEffect.GetComponent<HealSmoke>().GetOnwerPhotonviewID(ownerId);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (PhotonNetwork.IsMasterClient)
            photonView.RPC("RPC_PlaySmoke", RpcTarget.All);
    }

    [PunRPC]
    void RPC_PlaySmoke()
    {
        transform.localRotation = Quaternion.identity;
        StartCoroutine(WaitSmokeEffect());
    }

    IEnumerator WaitSmokeEffect()
    {
        Debug.Log("[SmokeBomb] 기다리는 중...");
        yield return new WaitForSeconds(3f);
        StartCoroutine(StartSmokeEffect());
    }

    IEnumerator StartSmokeEffect()
    {
        Debug.Log("[SmokeBomb] 연막 효과 시작!");
        SmokeEffect.SetActive(true);
        SmokeEffect.GetComponent<ParticleSystem>().Play();

        AudioManager.Inst?.PlayClipAtPoint(smokeSound, transform.position, 1f, 1f, null, transform);

        yield return new WaitForSeconds(smokeDuration);

        SmokeEffect.SetActive(false);

        if (photonView.IsMine)
            PhotonNetwork.Destroy(gameObject); // 오브젝트 전체 제거
    }
}