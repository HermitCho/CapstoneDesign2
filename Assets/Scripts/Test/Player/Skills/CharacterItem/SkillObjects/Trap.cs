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

    private const byte EXPLOSION_EVENT = 9;


    [PunRPC]
    public void InitializeTrap(int ownerId, float life)
    {
        ownerActorNumber = ownerId;
        lifetime = life;

        if (explosionEffect == null)
        {
            explosionEffect = transform.GetChild(0).gameObject;
            explosionEffect.SetActive(false);
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

        // ✅ 폭발 이벤트 전송 (ViewID 대신 Owner, 위치 사용)
        object[] content = new object[]
        {
            ownerActorNumber,
            transform.position
        };

        PhotonNetwork.RaiseEvent(
            EXPLOSION_EVENT,
            content,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            SendOptions.SendReliable
        );

        // 데미지 처리
        enemy.photonView.RPC("OnDamage", RpcTarget.All, 20f, enemy.transform.position, Vector3.down, ownerActorNumber);

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
        int senderId = (int)data[0];
        Vector3 trapPosition = (Vector3)data[1];

        Debug.Log($"[Trap] 💥 Event 수신됨 - 설치자: {senderId}, 위치: {trapPosition}");

        // ✅ 해당 위치 근처의 Trap을 찾아 이펙트 실행
        foreach (var trap in FindObjectsOfType<Trap>())
        {
            if (trap.ownerActorNumber == senderId &&
                Vector3.Distance(trap.transform.position, trapPosition) < 0.1f)
            {
                trap.ShowExplosionEffect();
                break;
            }
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

    public void ShowExplosionEffect()
    {
        Debug.Log($"[Trap] ShowExplosionEffect 호출됨 - {gameObject.name}, activeSelf={gameObject.activeSelf}, explosionEffect={(explosionEffect != null ? explosionEffect.name : "null")}");

        if (explosionEffect == null)
        {
            Debug.LogWarning($"[Trap] explosionEffect가 null입니다! 자식 오브젝트를 탐색합니다.");
            explosionEffect = transform.GetChild(0).gameObject;
        }

        explosionEffect.SetActive(true);
        var ps = explosionEffect.GetComponent<ParticleSystem>();
        ps?.Play();

        Debug.Log($"[Trap] 💥 폭발 이펙트 재생 완료 - 클라이언트 {PhotonNetwork.LocalPlayer.ActorNumber}");
    }

}
