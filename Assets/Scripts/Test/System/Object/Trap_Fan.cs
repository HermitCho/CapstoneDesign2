using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // Photon 기능을 사용하기 위해 추가
using System.Threading.Tasks; // async/await와 Task.Delay를 사용하기 위해 추가
using System.Threading; // CancellationToken을 사용하기 위해 추가

public class Trap_Fan : MonoBehaviourPun
{
    private float fanOnTime = 5f;
    private float fanOffTime = 5f;
    private AudioSource aS;
    [SerializeField] private GameObject fan;
    [SerializeField] private GameObject wind;
    [SerializeField] private AudioClip fanSound;

    // 선풍기 날개의 회전 속도 (인스펙터에서 조절 가능)
    [SerializeField] private float rotationSpeed = 500f;
    private bool isFanOn = false;
    private CancellationTokenSource cts; // 비동기 작업 취소를 위한 토큰

    void Start()
    {
        aS = GetComponent<AudioSource>();
        
        // ✅ PhotonView 유효성 확인
        if (photonView == null || photonView.ViewID == 0)
        {
            Debug.LogWarning($"[Trap_Fan] {gameObject.name} PhotonView가 유효하지 않습니다. 로컬 모드로 작동합니다.");
            // 로컬 모드: 모든 클라이언트에서 자체적으로 동작 (마스터/비마스터 동일한 애니메이션/사운드/바람 효과)
            cts = new CancellationTokenSource();
            StartFanCycleLocal(cts.Token);
        }
        else
        {
            // 네트워크 모드: 마스터 클라이언트만 주기적인 켜짐/꺼짐 로직을 실행
            if (PhotonNetwork.IsMasterClient)
            {
                cts = new CancellationTokenSource();
                StartFanCycleAsync(cts.Token);
            }
        }

        // 초기 설정: wind 오브젝트는 비활성화
        if (wind != null)
        {
            wind.SetActive(false);
        }
    }
    void Update()
    {
        // ⭐ 수정된 부분: 함정이 켜져 있을 때 fan 오브젝트를 '로컬 Y축' 기준으로 회전
        if (isFanOn && fan != null)
        {
            // Vector3.up (글로벌 Y축) 대신 fan 오브젝트의 '로컬 Y축' (transform.up)을 기준으로 회전
            // 또는 transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self); 사용 가능

            // 로컬 축을 기준으로 회전시키는 가장 확실한 방법 (Space.Self 사용)
            fan.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    // ⭐ Coroutine 대신 async/await를 사용한 주기 제어 로직 (네트워크 모드)
    private async void StartFanCycleAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                // 1. 선풍기 켜기 (ON)
                bool gameOverCheck = GameManager.Instance.GetIsGameOver();
                if (!gameOverCheck)
                {
                    // ✅ PhotonView 유효성 재확인
                    if (photonView != null && photonView.ViewID != 0)
                    {
                        photonView.RPC("SetFanState", RpcTarget.All, true);
                    }
                    else
                    {
                        // PhotonView가 유효하지 않으면 로컬로 전환
                        SetFanState(true);
                    }
                    AudioManager.Inst?.PlayClipAtPoint(fanSound, transform.position, 1f, 1f, null, transform);
                }

                await Task.Delay((int)(fanOnTime * 1000), token);
                if (token.IsCancellationRequested) break;

                // 2. 선풍기 끄기 (OFF)
                if (photonView != null && photonView.ViewID != 0)
                {
                    photonView.RPC("SetFanState", RpcTarget.All, false);
                }
                else
                {
                    SetFanState(false);
                }

                await Task.Delay((int)(fanOffTime * 1000), token);
            }
        }
        catch (System.Threading.Tasks.TaskCanceledException)
        {
            // 정상적인 취소 동작
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Trap_Fan: 예상치 못한 오류 발생 - {ex.Message}");
        }
    }
    
    // 로컬 모드: PhotonView가 없을 때 사용
    private async void StartFanCycleLocal(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                bool gameOverCheck = GameManager.Instance.GetIsGameOver();
                if (!gameOverCheck)
                {
                    SetFanState(true);
                    AudioManager.Inst?.PlayClipAtPoint(fanSound, transform.position, 1f, 1f, null, transform);
                }

                await Task.Delay((int)(fanOnTime * 1000), token);
                if (token.IsCancellationRequested) break;

                SetFanState(false);
                await Task.Delay((int)(fanOffTime * 1000), token);
            }
        }
        catch (System.Threading.Tasks.TaskCanceledException)
        {
            // 정상적인 취소 동작
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Trap_Fan: 예상치 못한 오류 발생 - {ex.Message}");
        }
    }

    // ⭐ [PunRPC]를 사용하여 마스터 클라이언트가 보낸 상태 변경 명령을 모든 클라이언트가 수신
    [PunRPC]
    private void SetFanState(bool state)
    {
        isFanOn = state;
        if (wind != null)
        {
            wind.SetActive(state);
            Debug.Log($"[Trap_Fan] {gameObject.name} Wind 상태 변경: {state}, 활성화: {wind.activeInHierarchy}");
            
            // ✅ Collider 확인
            Collider windCollider = wind.GetComponent<Collider>();
            if (windCollider != null)
            {
                Debug.Log($"[Trap_Fan] Wind Collider - IsTrigger: {windCollider.isTrigger}, Enabled: {windCollider.enabled}");
            }
            else
            {
                Debug.LogWarning($"[Trap_Fan] {gameObject.name} Wind 오브젝트에 Collider가 없습니다!");
            }
        }
        else
        {
            Debug.LogWarning($"[Trap_Fan] {gameObject.name} Wind 오브젝트가 null입니다!");
        }
    }

    // 오브젝트가 비활성화되거나 파괴될 때 비동기 작업을 안전하게 취소
    private void OnDisable()
    {
        if (cts != null)
        {
            cts.Cancel();
        }
    }
    
    // 오브젝트가 파괴될 때 리소스 정리
    private void OnDestroy()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
    }
}