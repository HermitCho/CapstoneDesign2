using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // Photon PUN 네임스페이스 추가 (이 코인이 네트워크 객체라면 필수)

public class Coin : MonoBehaviourPun
{
    [Header("코인 가치 (1, 10, 50)")]
    [SerializeField] private int coinValue = 1;
    
    [Header("코인 회전 속도")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("코인 위아래 떨림 속도")]
    [SerializeField] private float bobbingSpeed = 0.2f;

    [Header("코인 위아래 떨림 높이")]
    [SerializeField] private float bobbingHeight = 0.3f;

    [Header("코인 리스폰 시간")]
    [SerializeField] private float spawnTime = 5f;

    [Header("코인 획득 효과 파티클")]
    [SerializeField] private ParticleSystem coinEffect;

    private Vector3 originalPosition;
    private Renderer[] coinRenderers;
    private bool isCollected = false;
    private float limitBobbingHeight;

    private CoinController coinController;
    private SpawnCoin spawnCoin;
    
    // 각 Renderer의 원본 머티리얼 배열 저장
    private List<Material[]> originalMaterials = new List<Material[]>();
    
    // AI가 코인 상태를 확인할 수 있도록 public 프로퍼티 제공
    public bool IsCollected => isCollected;

    void Start()
    {
        Init();
    }


    void Update()
    {
        if (!isCollected)
        {
            RotateCoin();
        }
    }

    private void Init()
    {
        originalPosition = transform.position;
        coinRenderers = GetComponentsInChildren<Renderer>();
        coinController = FindObjectOfType<CoinController>();
        
        // ✅ SpawnCoin을 부모에서 찾거나, 위치 기반으로 찾기
        spawnCoin = GetComponentInParent<SpawnCoin>();
        if (spawnCoin == null)
        {
            // 부모에 없으면 가장 가까운 SpawnCoin 찾기
            SpawnCoin[] allSpawnCoins = FindObjectsOfType<SpawnCoin>();
            float closestDistance = float.MaxValue;
            foreach (SpawnCoin sc in allSpawnCoins)
            {
                float dist = Vector3.Distance(transform.position, sc.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    spawnCoin = sc;
                }
            }
        }
        
        // 각 Renderer의 모든 머티리얼을 저장
        foreach (Renderer renderer in coinRenderers)
        {
            // materials를 사용하면 모든 머티리얼을 가져옴
            Material[] materials = renderer.materials;
            originalMaterials.Add(materials);
        }
    }

    private void RotateCoin()
    {
        // Y축 회전
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        
        // 위아래 떨림 효과
        float bobbingOffset = Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
        limitBobbingHeight = Mathf.Clamp(bobbingOffset, 0, bobbingHeight);
        transform.position = originalPosition + Vector3.up * limitBobbingHeight;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어와 닿았을 때만 처리
        if (other.CompareTag("Player") && !isCollected)
        {
            // ✅ 중복 수집 방지
            if (isCollected) return;
            
            // ✅ 마스터 클라이언트에서만 수집 처리 (동기화)
            if (PhotonNetwork.IsMasterClient)
            {
                // RPC로 모든 클라이언트에 수집 알림
                if (photonView != null && photonView.ViewID != 0)
                {
                    photonView.RPC("RPC_CollectCoin", RpcTarget.AllViaServer, other.GetComponent<PhotonView>()?.ViewID ?? 0);
                }
                else
                {
                    // PhotonView가 없으면 로컬로만 처리
                    CollectCoinLocal(other.GetComponent<PhotonView>()?.ViewID ?? 0);
                }
            }
        }
    }
    
    /// <summary>
    /// 코인 수집 RPC (마스터 클라이언트가 호출)
    /// </summary>
    [PunRPC]
    private void RPC_CollectCoin(int playerViewID)
    {
        if (isCollected) return;
        
        CollectCoinLocal(playerViewID);
    }
    
    /// <summary>
    /// 코인 수집 로컬 처리 (RPC 또는 로컬 호출)
    /// </summary>
    private void CollectCoinLocal(int playerViewID)
    {
        if (isCollected) return;
        
        isCollected = true;
        
        // ✅ 플레이어 찾기
        PhotonView playerPV = null;
        if (playerViewID > 0)
        {
            playerPV = PhotonView.Find(playerViewID);
        }
        
        // ✅ 로컬 플레이어인 경우에만 코인 추가
        if (playerPV != null && playerPV.IsMine)
        {
            CoinController playerCoinController = playerPV.GetComponent<CoinController>();
            
            if (playerCoinController == null)
            {
                playerCoinController = playerPV.GetComponentInChildren<CoinController>();
            }
            
            if (playerCoinController != null)
            {
                playerCoinController.AddCoin(coinValue);
            }
            else
            {
                Debug.LogWarning("⚠️ Coin - 플레이어에 CoinController를 찾을 수 없습니다.");
            }
        }
        
        // ✅ 파티클 효과 재생 (모든 클라이언트)
        if (coinEffect != null)
        {
            coinEffect.Play();
        }
        
        // ✅ 마스터 클라이언트에서만 코인 파괴 및 재생성 스케줄링
        if (PhotonNetwork.IsMasterClient)
        {
            // 재생성 스케줄링
            if (spawnCoin != null)
            {
                spawnCoin.ScheduleRespawn(spawnTime);
            }
            
            // ✅ 코루틴을 먼저 시작 (게임 오브젝트가 활성화된 상태에서)
            // 코루틴 내에서 비활성화 및 파괴 처리
            StartCoroutine(DestroyCoinAfterDelay(0.1f));
        }
        else
        {
            // ✅ 비마스터 클라이언트는 즉시 비활성화만 처리
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 코인 파괴 지연 코루틴 (마스터 클라이언트에서만 실행)
    /// </summary>
    private IEnumerator DestroyCoinAfterDelay(float delay)
    {
        // ✅ 먼저 코인 비활성화 (모든 클라이언트에서 보이지 않게)
        gameObject.SetActive(false);
        
        yield return new WaitForSeconds(delay);
        
        // ✅ 마스터 클라이언트에서만 파괴 처리
        if (PhotonNetwork.IsMasterClient)
        {
            if (photonView != null && photonView.ViewID != 0)
            {
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}