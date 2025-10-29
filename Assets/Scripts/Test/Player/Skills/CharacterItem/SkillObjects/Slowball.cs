using Photon.Pun;
using UnityEngine;
using System.Collections;

/// <summary>
/// 슬로우볼의 물리적 동작, 충돌 처리 및 슬로우 장판 생성을 담당하는 스크립트.
/// </summary>
public class Slowball : MonoBehaviourPun
{
    [Header("슬로우볼 설정")]
    [SerializeField] private float speed = 30f;
    [SerializeField] private GameObject slowFieldPrefab; // 바닥에 생성할 슬로우 장판 프리팹

    private Rigidbody rb;
    private AudioSource aS;
    [SerializeField] private AudioClip throwingSound; // 투사체 비행 소리

    private int ownerActorNumber;
    private bool hasExploded = false; // 장판 생성 여부 확인

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        aS = GetComponent<AudioSource>();
        photonView.RPC("PlayThrowingSound", RpcTarget.All);
    }

    [PunRPC]
    public void InitializeAndLaunch(int ownerId, Vector3 direction, float launchSpeed)
    {
        ownerActorNumber = ownerId;
        rb.velocity = direction.normalized * launchSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 이미 장판을 생성했다면 추가 작업 방지
        if (hasExploded) return;
        hasExploded = true;

        // 마스터 클라이언트만 충돌 및 장판 생성 처리를 담당
        if (PhotonNetwork.IsMasterClient)
        {
            Vector3 impactPosition = transform.position;

            // 충돌 지점에서 아래로 레이캐스트를 쏴서 바닥을 찾음
            RaycastHit hit;
            if (Physics.Raycast(impactPosition, Vector3.down, out hit, 100f))
            {
                // 레이에 맞은 지점(바닥)에 슬로우 필드 생성 RPC 호출
                photonView.RPC("SpawnSlowFieldRPC", RpcTarget.All, hit.point);
            }
            else
            {
                // 바닥을 찾지 못했을 경우 (예: 허공에서 수명이 다했을 때)
                // 그냥 투사체 위치에서 생성
                photonView.RPC("SpawnSlowFieldRPC", RpcTarget.All, impactPosition);

            }
        }
    }

    [PunRPC]
    private void SpawnSlowFieldRPC(Vector3 position)
    {
        if (slowFieldPrefab != null)
        {
            string prefabPath = "Prefabs/ItemObject/" + slowFieldPrefab.name;

            PhotonNetwork.Instantiate(prefabPath, position + new Vector3(0, 0.02f, 0), Quaternion.identity);
        }
        // 투사체 오브젝트 파괴
        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    private void PlayThrowingSound()
    {
        if (aS != null && throwingSound != null)
        {
            aS.PlayOneShot(throwingSound);
        }
    }

    public float GetSlowballSpeed()
    {
        return speed;
    }
}