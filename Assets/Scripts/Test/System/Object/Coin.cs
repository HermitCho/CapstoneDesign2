using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private SpawnCoin spawnCoin;
    
    // 각 Renderer의 원본 머티리얼 배열 저장
    private List<Material[]> originalMaterials = new List<Material[]>();

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
        spawnCoin = GetComponentInParent<SpawnCoin>();
        
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
            CollectCoin();
            
            // 플레이어의 CoinController에 직접 코인 추가
            CoinController playerCoinController = other.GetComponent<CoinController>();
            
            if (playerCoinController == null)
            {
                // 플레이어에 CoinController가 없으면 자식에서 찾기
                playerCoinController = other.GetComponentInChildren<CoinController>();
            }
            
            if (playerCoinController != null)
            {
                playerCoinController.AddCoin(coinValue);
            }


            else
            {
                Debug.LogWarning("⚠️ Coin - 플레이어에 CoinController를 찾을 수 없습니다.");
            }

            if(spawnCoin != null)
            {
                spawnCoin.StartCoroutine(spawnCoin.RespawnAfterDelay(spawnTime));
            }
            // 코인 즉시 파괴
            Destroy(gameObject);
        }
    }

    private void CollectCoin()
    {
        isCollected = true;
        
        // 파티클 효과 재생 (선택사항)
        if (coinEffect != null)
        {
            coinEffect.Play();
        }
    }
}
