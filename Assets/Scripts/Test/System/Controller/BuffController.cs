using Photon.Pun;
using UnityEngine;
using System.Collections;

// 이 스크립트는 플레이어 루트 오브젝트에 붙어 있어야 하며, 
// 해당 PhotonView의 Observed Components에 등록되어야 합니다.
public class BuffController : MonoBehaviourPun, IPunObservable
{
    // 마스터/오너가 전송할 버프 상태 변수
    private float currentBuffMultiplier = 1.0f; 
    private double buffEndTime = 0.0;
    
    // 버프 효과 적용 대상 컴포넌트 (MoveController, LivingEntity 등)
    private MoveController mover;
    private LivingEntity living;

    void Awake()
    {
        mover = GetComponent<MoveController>();
        living = GetComponent<LivingEntity>();
        
        // 초기화 시 버프 코루틴이 이미 실행 중일 수 있으므로 중지
        StopAllCoroutines(); 
    }

    /// <summary>
    /// StrengthItem.cs의 Execute에서 이 함수를 호출합니다. (로컬에서만 실행)
    /// </summary>
    public void StartStrengthBuff(float multiplier, float duration)
    {
        // 로컬에서 버프 상태 업데이트 (이 값이 네트워크로 전송됨)
        currentBuffMultiplier = 1f + multiplier;
        buffEndTime = PhotonNetwork.Time + duration;
        
        // 즉시 로컬 효과 적용 시작
        StartCoroutine(ApplyBuffCoroutine(multiplier, duration));
    }

    // MoveController와 LivingEntity 등에 실제 효과를 적용하는 코루틴
    private IEnumerator ApplyBuffCoroutine(float multiplier, float duration)
    {
        // 1. 버프 적용 로직 (RPC 대신 로컬 컴포넌트 직접 호출)
        // 예: mover.ApplySpeedMultiplier(currentBuffMultiplier); // MoveController에 함수 추가 필요
        // 예: living.UpdateMaxHealthMultiplier(currentBuffMultiplier); // LivingEntity에 함수 추가 필요
        
        Debug.Log($"[BuffManager] 로컬 버프 적용 시작. 배율: {currentBuffMultiplier}");

        yield return new WaitForSeconds(duration);

        // 2. 버프 해제 로직 (버프 종료 시간이 남아 있다면 해제하지 않도록 안전 장치 필요)
        if (PhotonNetwork.Time >= buffEndTime)
        {
            currentBuffMultiplier = 1.0f;
            buffEndTime = 0.0;
            
            // 예: mover.ApplySpeedMultiplier(1.0f); 
            // 예: living.UpdateMaxHealthMultiplier(1.0f);
            
            Debug.Log("[BuffManager] 로컬 버프 해제.");
        }
    }

    // ⚡️ IPunObservable 구현: 버프 상태 동기화
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 💡 마스터 클라이언트/오너만 이 값을 전송합니다.
            stream.SendNext(currentBuffMultiplier);
            stream.SendNext(buffEndTime);
        }
        else
        {
            // 💡 원격 클라이언트가 이 값을 수신합니다.
            float receivedMultiplier = (float)stream.ReceiveNext();
            double receivedEndTime = (double)stream.ReceiveNext();

            // 데이터 수신 시 로컬 상태 업데이트
            if (currentBuffMultiplier != receivedMultiplier || buffEndTime != receivedEndTime)
            {
                currentBuffMultiplier = receivedMultiplier;
                buffEndTime = receivedEndTime;

                // 수신된 값에 따라 원격 클라이언트의 효과를 업데이트/적용하는 별도 함수 호출
                // 이 함수는 원격 클라이언트의 비주얼/속도 등을 업데이트합니다.
                UpdateRemoteBuffEffect();
            }
        }
    }
    
    // 원격 클라이언트에서 호출되어 시각적/속도 등을 업데이트하는 함수
    private void UpdateRemoteBuffEffect()
    {
        // 남은 시간 계산
        double remainingTime = buffEndTime - PhotonNetwork.Time;
        
        // 버프가 활성화되어 있다면
        if (remainingTime > 0.0)
        {
            // 예: mover.ApplySpeedMultiplier(currentBuffMultiplier);
            Debug.Log($"[BuffManager] 원격 버프 적용. 남은 시간: {remainingTime:F2}s");
            // 코루틴 시작하여 남은 시간 후에 해제되도록 할 수 있습니다. (코루틴 중복 실행 방지 로직 필요)
        }
        else
        {
            // 버프 해제
            // 예: mover.ApplySpeedMultiplier(1.0f);
            Debug.Log("[BuffManager] 원격 버프 해제.");
        }
    }
}