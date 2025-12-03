using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // Photon PUN 네임스페이스 추가

public class Coin : MonoBehaviour
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

    // ⭐ 수정: SpawnCoin의 PhotonView ID를 저장할 필드
    private int spawnCoinViewID = 0;

    // 각 Renderer의 원본 머티리얼 배열 저장
    private List<Material[]> originalMaterials = new List<Material[]>();

    public bool IsCollected => isCollected;

    // ⭐ 새로 추가: SpawnCoin의 ViewID를 설정하는 메서드
    public void SetSpawnCoinViewID(int viewID)
    {
        this.spawnCoinViewID = viewID;
    }

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

        foreach (Renderer renderer in coinRenderers)
        {
            Material[] materials = renderer.materials;
            originalMaterials.Add(materials);
        }
    }

    private void RotateCoin()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);

        float bobbingOffset = Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
        limitBobbingHeight = Mathf.Clamp(bobbingOffset, 0, bobbingHeight);
        transform.position = originalPosition + Vector3.up * limitBobbingHeight;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 플레이어 및 로컬 플레이어 확인
        if (other.CompareTag("Player") && !isCollected)
        {
            PhotonView playerPV = other.GetComponent<PhotonView>();
            if (playerPV == null || !playerPV.IsMine)
            {
                return; // 로컬 플레이어가 아니면 처리하지 않음
            }

            CollectCoin();

            // 2. 코인 획득 및 점수 처리 (로컬 처리)
            CoinController playerCoinController = other.GetComponent<CoinController>();
            if (playerCoinController == null)
            {
                playerCoinController = other.GetComponentInChildren<CoinController>();
            }

            if (playerCoinController != null)
            {
                playerCoinController.AddCoin(coinValue);
            }

            // ⭐ 3. RPC 호출: 마스터 클라이언트에게 파괴 및 재생성 요청
            // 이 코인의 PhotonView를 사용하여 마스터에게 RPC를 보냅니다.
            PhotonView coinPV = GetComponent<PhotonView>();
            if (coinPV != null)
            {
                // 코인 파괴와 SpawnCoin 재생성 요청을 동시에 수행하는 RPC를 마스터에게 보냅니다.
                coinPV.RPC("RPC_CollectAndRespawn", RpcTarget.MasterClient, spawnCoinViewID, spawnTime);
            }

            // ⭐ 4. 로컬 파괴 삭제: 직접 Destroy하지 않고 마스터의 RPC를 기다립니다.
            // 로컬에서는 IsCollected 플래그만 true로 설정하여 추가 충돌을 방지합니다.
            // PhotonNetwork.Destroy(gameObject); // ❌ 이 코드를 삭제합니다.
        }
    }

    private void CollectCoin()
    {
        isCollected = true;

        if (coinEffect != null)
        {
            coinEffect.Play();
        }
    }

    /// <summary>
    /// 마스터 클라이언트만 실행: 코인을 파괴하고 SpawnCoin에게 재생성을 요청합니다.
    /// </summary>
    /// <param name="targetSpawnCoinViewID">재생성을 요청할 SpawnCoin의 View ID</param>
    /// <param name="delay">재생성 딜레이 시간</param>
    [PunRPC]
    public void RPC_CollectAndRespawn(int targetSpawnCoinViewID, float delay)
    {
        if (!PhotonNetwork.IsMasterClient) return; // 마스터 클라이언트만 실행 보장

        // 1. SpawnCoin에게 재생성 RPC 요청 (기존 로직 재사용)
        if (targetSpawnCoinViewID != 0)
        {
            PhotonView spawnCoinPV = PhotonView.Find(targetSpawnCoinViewID);
            if (spawnCoinPV != null)
            {
                // SpawnCoin의 재생성 RPC 호출
                spawnCoinPV.RPC("RPC_RequestRespawn", RpcTarget.MasterClient, delay);
            }
        }

        // 2. 코인 파괴 (마스터 클라이언트가 소유자이므로 파괴 가능)
        PhotonNetwork.Destroy(gameObject);
    }
}