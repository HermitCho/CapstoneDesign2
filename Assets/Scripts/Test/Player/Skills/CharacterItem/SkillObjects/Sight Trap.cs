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

    [Header("Reveal Settings")]
    [SerializeField] private float revealDuration = 5f;
    [SerializeField] private Color revealColor = Color.red;
    [SerializeField] private float outlineWidth = 4f; // 외곽선 두께
    [SerializeField] private float flashSpeed = 0.5f; // 깜빡이는 속도

    // 생성된 Outline 컴포넌트들을 추적하여 나중에 제거
    private List<Outline> activeOutlines = new List<Outline>();

    [PunRPC]
    public void InitializeTrap(int ownerId, float life)
    {
        ownerActorNumber = ownerId;
        lifetime = life;
        
        // 자연 파괴 (밟지 않았을 때)
        StartCoroutine(DestroyRoutine(lifetime));
        
        aS = GetComponent<AudioSource>();
    }

    private IEnumerator DestroyRoutine(float time)
    {
        yield return new WaitForSeconds(time);
        // 활성화되지 않은 상태에서만 시간 다 되면 파괴
        if (!isActivated && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActivated) return;

        if (other.CompareTag("Player"))
        {
            MoveController enemy = other.GetComponent<MoveController>();
            if (enemy == null) enemy = other.GetComponentInParent<MoveController>();
            if (enemy == null) return;

            // 설치자 본인은 무시
            if (enemy.photonView.OwnerActorNr == ownerActorNumber) return;

            isActivated = true;

            // 1. 함정 시각적 숨김 (오브젝트 파괴 X)
            photonView.RPC(nameof(HideTrapVisuals), RpcTarget.All);

            // 2. 설치자에게만 적 표시 (Wallhack - Outline 적용)
            photonView.RPC(nameof(ApplyOutlineEffect), RpcTarget.All, enemy.photonView.ViewID);

            if (aS != null && TrapActivateSound != null)
                aS.PlayOneShot(TrapActivateSound);

            // 3. 효과 종료 후 진짜 파괴 요청
            if (photonView.Owner != null)
            {
                photonView.RPC(nameof(RequestDestroyByOwnerDelayed), photonView.Owner);
            }
        }
    }

    [PunRPC]
    private void HideTrapVisuals()
    {
        GetComponent<Collider>().enabled = false;
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = false;
    }

    [PunRPC]
    private void RequestDestroyByOwnerDelayed()
    {
        if (photonView.IsMine)
        {
            StartCoroutine(DestroyAfterEffectEnds());
        }
    }

    private IEnumerator DestroyAfterEffectEnds()
    {
        // 효과 지속 시간 + 0.5초 여유를 둠
        yield return new WaitForSeconds(revealDuration + 0.5f);
        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    void ApplyOutlineEffect(int enemyViewId)
    {
        // 설치자 본인 화면에서만 보이게 처리 (Wallhack은 정보전이므로 아군/적군 로직에 따라 변경 가능)
        // 만약 모든 사람이 보게 하려면 아래 줄 주석 처리
        if (PhotonNetwork.LocalPlayer.ActorNumber != ownerActorNumber) return;

        PhotonView targetView = PhotonView.Find(enemyViewId);
        if (targetView == null) return;

        GameObject target = targetView.gameObject;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        // 이미 다른 함정에 걸려있을 수도 있으니 리스트 초기화
        activeOutlines.Clear();

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            
            // 파티클 시스템 등은 제외
            if (r is ParticleSystemRenderer) continue;

            // Outline 컴포넌트 동적 추가
            // 이미 Outline이 있다면 가져오고, 없으면 추가
            Outline outline = r.gameObject.GetComponent<Outline>();
            if (outline == null)
            {
                outline = r.gameObject.AddComponent<Outline>();
            }
            else
            {
                // 원래 있던 Outline이라면 기존 설정 저장 로직이 필요할 수 있음.
                // 여기서는 함정 효과가 우선이라 가정하고 덮어씀.
                outline.enabled = true;
            }

            // Outline 설정 (Crown 스크립트와 동일한 로직)
            outline.OutlineMode = Outline.Mode.SilhouetteOnly; // 벽 뒤에서도 보이게
            outline.OutlineColor = revealColor;
            outline.OutlineWidth = outlineWidth;

            activeOutlines.Add(outline);
        }

        // 깜빡이는 효과 시작
        StartCoroutine(OutlineGlowingRoutine());
    }

    private IEnumerator OutlineGlowingRoutine()
    {
        float elapsedTime = 0f;
        
        // revealDuration 동안 깜빡거림 반복
        while (elapsedTime < revealDuration)
        {
            // Fade In (안 보임 -> 보임)
            yield return StartCoroutine(FadeOutlineAlpha(0f, 1f, flashSpeed));
            elapsedTime += flashSpeed;

            if (elapsedTime >= revealDuration) break;

            // Fade Out (보임 -> 안 보임)
            yield return StartCoroutine(FadeOutlineAlpha(1f, 0.3f, flashSpeed));
            elapsedTime += flashSpeed;
        }

        // 효과 종료: 정리 작업
        CleanupOutlines();
    }

    // Crown 스크립트의 Fade 로직 차용
    IEnumerator FadeOutlineAlpha(float fromAlpha, float toAlpha, float duration)
    {
        float t = 0f;
        
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t / duration);
            
            Color newColor = revealColor;
            newColor.a = alpha;
            
            // 추적 중인 모든 Outline 색상 업데이트
            foreach (var outline in activeOutlines)
            {
                if (outline != null)
                {
                    outline.OutlineColor = newColor;
                }
            }
            
            yield return null;
        }

        // 최종값 적용
        Color finalColor = revealColor;
        finalColor.a = toAlpha;
        foreach (var outline in activeOutlines)
        {
            if (outline != null) outline.OutlineColor = finalColor;
        }
    }

    private void CleanupOutlines()
    {
        foreach (var outline in activeOutlines)
        {
            if (outline != null)
            {
                // 동적으로 추가한 컴포넌트이므로 제거
                Destroy(outline);
            }
        }
        activeOutlines.Clear();
    }
}