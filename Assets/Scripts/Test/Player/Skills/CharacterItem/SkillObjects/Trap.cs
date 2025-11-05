using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using ExitGames.Client.Photon;
using System.Collections;

public class Trap : MonoBehaviourPun
{
    private int ownerActorNumber;
    private float lifetime;
    private bool isActivated = false;

    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private float damage = 20f;

    private const byte EXPLOSION_EVENT = 9;


    [PunRPC]
    public void InitializeTrap(int ownerId, float life)
    {
        ownerActorNumber = ownerId;
        lifetime = life;

        if (explosionEffect == null)
        {
            if (transform.childCount > 0)
            {
                explosionEffect = transform.GetChild(0).gameObject;
            }
        }

        if (explosionEffect != null)
        {
            explosionEffect.SetActive(false);
            var systems = explosionEffect.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var s in systems)
            {
                s.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        Destroy(gameObject, lifetime);
    }

    private void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += OnEventReceived;
    }

    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnEventReceived;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActivated) return;
        if (!PhotonNetwork.IsMasterClient) return;

        MoveController enemy = other.GetComponent<MoveController>();
        if (enemy == null) return;
        if (enemy.photonView.OwnerActorNr == ownerActorNumber) return;

        isActivated = true;

        // Prevent re-triggering
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // ✅ 폭발 이벤트 전송
        object[] content = new object[]
        {
            ownerActorNumber,
            transform.position,
            photonView.ViewID
        };

        PhotonNetwork.RaiseEvent(
            EXPLOSION_EVENT,
            content,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            SendOptions.SendReliable
        );

        // 데미지 처리
        enemy.photonView.RPC("OnDamage", RpcTarget.All, damage, enemy.transform.position, Vector3.down, ownerActorNumber);

        // 소유자 파괴 처리
        if (photonView.Owner != null)
            photonView.RPC(nameof(RequestDestroyByOwner), photonView.Owner);
        else if (PhotonNetwork.IsMasterClient)
        {
            photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
            PhotonNetwork.Destroy(photonView.gameObject);
        }
    }

    private void OnEventReceived(EventData photonEvent)
    {
        if (photonEvent.Code != EXPLOSION_EVENT) return;

        object[] data = (object[])photonEvent.CustomData;
        // int senderId = (int)data[0]; // 설치자 ID
        // Vector3 trapPosition = (Vector3)data[1]; // 트랩 위치
        int trapViewID = (int)data[2]; // ⭐ ViewID 추출

        Debug.Log($"[Trap] 💥 Event 수신됨 - ViewID: {trapViewID}");

        // ✅ PhotonView.Find(ViewID)를 사용하여 트랩 인스턴스 직접 찾기
        PhotonView targetPV = PhotonView.Find(trapViewID);

        if (targetPV != null)
        {
            Trap targetTrap = targetPV.GetComponent<Trap>();
            if (targetTrap != null)
            {
                // 해당 트랩 인스턴스에 이펙트 실행
                targetPV.RPC("ShowExplosionEffect", RpcTarget.All);
            }
            else
            {
                Debug.LogError($"[Trap] ViewID {trapViewID}에서 Trap 컴포넌트를 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogError($"[Trap] ViewID {trapViewID}를 가진 PhotonView를 찾을 수 없습니다.");
        }
    }

    [PunRPC]
    private void RequestDestroyByOwner()
    {
        if (photonView.IsMine)
        {
            StartCoroutine(DestroyAfterDelay());
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    public void ShowExplosionEffect()
    {
        Debug.Log($"[Trap] ShowExplosionEffect 호출됨 - {gameObject.name}, activeSelf={gameObject.activeSelf}, explosionEffect={(explosionEffect != null ? explosionEffect.name : "null")}" );

        if (explosionEffect == null)
        {
            if (transform.childCount > 0)
            {
                explosionEffect = transform.GetChild(0).gameObject;
            }
        }
        if (explosionEffect == null) return;

        // 트랩 파괴와 무관하게 이펙트가 보이도록 분리
        explosionEffect.transform.SetParent(null, true);
        explosionEffect.transform.position = transform.position;
        explosionEffect.SetActive(true);

        var systems = explosionEffect.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var s in systems)
        {
            s.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            s.Clear(true);
            s.Play(true);
        }

        // 파티클 총 수명 후 정리
        float maxLifetime = 0f;
        foreach (var s in systems)
        {
            var m = s.main;
            float duration = m.duration + m.startLifetime.constantMax;
            if (duration > maxLifetime) maxLifetime = duration;
        }
        if (maxLifetime <= 0f) maxLifetime = 2f;
    }
}
