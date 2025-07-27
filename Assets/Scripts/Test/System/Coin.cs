using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
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

    [Header("코인 획득 효과 소리")]
    [SerializeField] private AudioClip coinCollectSound;

    private Vector3 originalPosition;
    private Renderer coinRenderer;
    private bool isCollected = false;
    private float limitBobbingHeight;

    private CoinController coinController;

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
        coinRenderer = GetComponent<Renderer>();
        coinController = FindObjectOfType<CoinController>();
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
            AudioSource audioSource = other.GetComponent<AudioSource>();
            
            if (playerCoinController == null)
            {
                // 플레이어에 CoinController가 없으면 자식에서 찾기
                playerCoinController = other.GetComponentInChildren<CoinController>();
            }
            
            if (playerCoinController != null)
            {
                playerCoinController.AddCoin(1);
            }

            if (audioSource != null && coinCollectSound != null)
            {
                audioSource.PlayOneShot(coinCollectSound);
            }

            else
            {
                Debug.LogWarning("⚠️ Coin - 플레이어에 CoinController를 찾을 수 없습니다.");
            }
        }
    }

    private void CollectCoin()
    {
        isCollected = true;
        
        // 코인 투명도 0으로 설정
        if (coinRenderer != null)
        {
            Color color = coinRenderer.material.color;
            color.a = 0f;
            coinRenderer.material.color = color;
        }
        
        // 파티클 효과 재생 (선택사항)
        if (coinEffect != null)
        {
            coinEffect.Play();
        }
        
        // spawnTime 후에 코인 다시 나타나기
        StartCoroutine(RespawnCoin());
    }

    private IEnumerator RespawnCoin()
    {
        yield return new WaitForSeconds(spawnTime);
        
        // 코인 투명도를 원래대로 복원
        if (coinRenderer != null)
        {
            Color color = coinRenderer.material.color;
            color.a = 1f;
            coinRenderer.material.color = color;
        }
        
        isCollected = false;
    }
}
