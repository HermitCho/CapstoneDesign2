using Photon.Pun;
using UnityEngine;
using System.Collections;

public class SmokeBomb : MonoBehaviourPun, IPunObservable // 👈 IPunObservable 추가
{
    [SerializeField] GameObject SmokeEffect;
    [SerializeField] AudioClip smokeSound;
    [SerializeField] private float smokeDuration = 7f;  // 연막 지속 시간

    private Rigidbody rb; // private으로 변경
    private AudioSource aS; // private으로 변경

    // 네트워크 보간용
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private bool firstSync = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        aS = GetComponent<AudioSource>();
    }

    private void Start() // 👈 Start 함수 추가
    {
        // 자신이 생성하지 않은 Fireball은 물리 계산 끔
        rb.isKinematic = !photonView.IsMine;
    }

    [PunRPC]
    public void InitializeAndLaunch(int ownerId, Vector3 direction, float launchSpeed) // launchSpeed 파라미터 유지
    {
        // 자신이 생성한 SmokeBomb만 실제 물리 이동 처리
        if (photonView.IsMine)
        {
            rb.velocity = direction.normalized * launchSpeed;
        }

        Debug.Log("[SmokeBomb] + InitializeAndLaunch 오너 아이디" + ownerId);
        // Note: HealSmoke 컴포넌트가 모든 클라이언트에서 잘 작동하도록 필요 시 RPC 등을 고려해야 합니다.
        SmokeEffect.GetComponent<HealSmoke>()?.GetOnwerPhotonviewID(ownerId);
    }

    private void FixedUpdate() // 👈 FixedUpdate 추가 (Fireball과 동일한 보간 로직)
    {
        // 자신이 생성하지 않은 SmokeBomb은 보간만 수행
        if (!photonView.IsMine && !firstSync) // firstSync 체크 추가
        {
            // Time.fixedDeltaTime 대신 Time.deltaTime을 사용하여 보간할 수도 있지만, Fireball과 동일하게 적용
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.fixedDeltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.fixedDeltaTime * 10f);
        }
    }

    void OnCollisionEnter(Collision collision) // 👈 OnCollisionEnter 수정
    {
        // 자신이 생성한 SmokeBomb만 폭발/연막 생성 처리
        if (photonView.IsMine)
        {
            // 충돌 시 모든 클라이언트에게 연막 생성 RPC 전송
            photonView.RPC("RPC_PlaySmoke", RpcTarget.All, transform.position);
            // Note: Fireball은 충돌 후 ExplodeAndApplyAreaDamageRPC에서 Destroy를 하지만, 
            // SmokeBomb은 연막 지속 시간이 있으므로 충돌 후 바로 Destroy하지 않습니다.
        }
    }

    [PunRPC]
    void RPC_PlaySmoke(Vector3 hitPosition) // 👈 RPC_PlaySmoke 수정 (위치 파라미터 추가)
    {
        // 연막이 터진 위치로 이동 (네트워크 지연으로 인해 약간 다를 수 있음)
        // 보간 중 충돌하여 위치가 다를 경우를 대비해 위치를 동기화
        if (!photonView.IsMine)
        {
            transform.position = hitPosition;
            rb.isKinematic = true; // 터진 후에는 물리 이동을 멈춤
        }

        transform.localRotation = Quaternion.identity;
        StartCoroutine(WaitSmokeEffect());
    }

    IEnumerator WaitSmokeEffect()
    {
        Debug.Log("[SmokeBomb] 기다리는 중...");
        // Fireball에는 폭발/데미지 처리가 있었으나, SmokeBomb은 연막 효과 시작
        yield return new WaitForSeconds(3f);
        StartCoroutine(StartSmokeEffect());
    }

    IEnumerator StartSmokeEffect()
    {
        Debug.Log("[SmokeBomb] 연막 효과 시작!");
        SmokeEffect.SetActive(true);
        // 이미 재생 중일 수도 있으므로 Play()는 한번만 호출하도록 확인
        ParticleSystem ps = SmokeEffect.GetComponent<ParticleSystem>();
        if (!ps.isPlaying)
        {
            ps.Play();
        }

        // AudioManager를 사용한 사운드 재생은 모든 클라이언트에서 독립적으로 수행
        // AudioManager.Inst가 null일 수 있으므로 null 체크 추가
        if (AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayClipAtPoint(smokeSound, transform.position, 1f, 1f, null, transform);
        }
        else
        {
            // AudioManager가 없을 경우 대체 사운드 처리
            if (aS != null && smokeSound != null)
            {
                aS.PlayOneShot(smokeSound);
            }
        }

        yield return new WaitForSeconds(smokeDuration);

        // 연막 효과 종료 및 오브젝트 파괴
        SmokeEffect.SetActive(false);

        // 오브젝트 제거는 소유주 클라이언트만 호출해야 함 (Fireball의 ExplodeAndApplyAreaDamageRPC 마지막과 동일)
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject); // 오브젝트 전체 제거
        }
    }

    // 👈 IPunObservable 인터페이스 구현
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 소유주는 자신의 위치와 회전을 보냄
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // 다른 클라이언트는 위치와 회전을 받음
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();

            if (firstSync)
            {
                // 첫 동기화 시 즉시 위치 동기화
                transform.position = networkPosition;
                transform.rotation = networkRotation;
                firstSync = false;
            }
        }
    }
}