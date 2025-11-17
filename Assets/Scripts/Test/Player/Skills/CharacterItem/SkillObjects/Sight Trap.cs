using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class SightTrap : MonoBehaviourPun
{
    private int ownerActorNumber;
    private float lifetime;
    private bool isActivated = false;
    private AudioSource aS;
    [SerializeField] AudioClip TrapActivateSound;

    [Header("Wallhack Material")]
    [SerializeField] private Material wallhackMaterial;

    [Header("Reveal Settings")]
    [SerializeField] private float revealDuration = 5f;
    [SerializeField] private Color revealColor = Color.red;
    [SerializeField] private float emissionIntensity = 5f;

    private Dictionary<Renderer, Coroutine> activeCoroutines = new Dictionary<Renderer, Coroutine>();
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();


    [PunRPC]
    public void InitializeTrap(int ownerId, float life)
    {
        ownerActorNumber = ownerId;
        lifetime = life;

        Destroy(gameObject, lifetime);
        aS = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActivated) return;

        if (other.CompareTag("Player"))
        {
            MoveController enemy = other.GetComponent<MoveController>();
            if (enemy == null)
                enemy = other.GetComponentInParent<MoveController>();

            if (enemy == null) return;

            // 설치자 본인은 무시
            if (enemy.photonView.OwnerActorNr == ownerActorNumber) return;

            isActivated = true;

            photonView.RPC(nameof(ProvidesVisibility), RpcTarget.All, enemy.photonView.ViewID);

            if (aS != null && TrapActivateSound != null)
                aS.PlayOneShot(TrapActivateSound);

            if (photonView.Owner != null)
                photonView.RPC(nameof(RequestDestroyByOwner), photonView.Owner);
            else if (PhotonNetwork.IsMasterClient)
            {
                photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
                PhotonNetwork.Destroy(photonView.gameObject);
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

    [PunRPC]
    void ProvidesVisibility(int enemyViewId)
    {
        // 설치자만
        if (PhotonNetwork.LocalPlayer.ActorNumber != ownerActorNumber) return;

        PhotonView targetView = PhotonView.Find(enemyViewId);
        if (targetView == null) return;

        GameObject target = targetView.gameObject;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;

            if (activeCoroutines.TryGetValue(renderer, out Coroutine running))
                StopCoroutine(running);

            if (!originalMaterials.ContainsKey(renderer))
                originalMaterials[renderer] = renderer.materials;

            Coroutine newCoroutine = StartCoroutine(RevealRenderer(renderer, revealDuration));
            activeCoroutines[renderer] = newCoroutine;
        }
    }


    private IEnumerator RevealRenderer(Renderer renderer, float delay)
    {
        if (renderer == null) yield break;

        // Wallhack 적용
        Material[] newMaterials = new Material[renderer.sharedMaterials.Length];

        for (int i = 0; i < newMaterials.Length; i++)
        {
            Material mat = new Material(wallhackMaterial);

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", revealColor * emissionIntensity);
            }

            newMaterials[i] = mat;
        }

        renderer.materials = newMaterials;

        yield return new WaitForSeconds(delay);

        // 복원
        if (originalMaterials.TryGetValue(renderer, out Material[] stored))
        {
            renderer.materials = stored;

            foreach (Material temp in newMaterials)
                if (temp != null) Destroy(temp);

            originalMaterials.Remove(renderer);
        }

        if (activeCoroutines.ContainsKey(renderer))
            activeCoroutines.Remove(renderer);
    }
}
