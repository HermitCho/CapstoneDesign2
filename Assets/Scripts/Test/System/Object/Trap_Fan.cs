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
        // ⭐ Photon 환경: 마스터 클라이언트만 주기적인 켜짐/꺼짐 로직을 실행
        if (PhotonNetwork.IsMasterClient)
        {
            cts = new CancellationTokenSource();
            StartFanCycleAsync(cts.Token);
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

    // ⭐ Coroutine 대신 async/await를 사용한 주기 제어 로직
    private async void StartFanCycleAsync(CancellationToken token)
    {
        // 유니티 환경에서는 Task.Delay를 사용할 때 프레임 드롭을 방지하기 위해 
        // 외부 라이브러리(UniTask)를 사용하는 것이 권장되나, 
        // 표준 라이브러리만 사용하여 구현했습니다.
        while (!token.IsCancellationRequested)
        {
            // 1. 선풍기 켜기 (ON)
            // 모든 클라이언트에서 SetFanState RPC를 호출하여 상태 동기화
            photonView.RPC("SetFanState", RpcTarget.All, true);
            aS.PlayOneShot(fanSound);

            // Task.Delay(밀리초)로 대기
            await Task.Delay((int)(fanOnTime * 1000), token);

            if (token.IsCancellationRequested) break; // 취소 확인

            // 2. 선풍기 끄기 (OFF)
            // 모든 클라이언트에서 SetFanState RPC를 호출하여 상태 동기화
            photonView.RPC("SetFanState", RpcTarget.All, false);

            await Task.Delay((int)(fanOffTime * 1000), token);
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
}