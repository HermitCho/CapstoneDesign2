using System.Collections;
using UnityEngine;
using Photon.Pun;

public class Dash : Skill
{
    [Header("대시 파워 설정")]
    public float dashForce = 10f;

    [Header("착지 감지 설정")]
    public float groundCheckDistance = 0.1f;
    public float maxDashTime = 2f;
    public float minDashDuration = 0.5f;

    [Header("레이어 및 안전 설정")]
    [SerializeField] private string dashLayerName = "PlayerDash";
    [SerializeField] private LayerMask groundLayerMask;

    // ✅ 복잡한 Dictionary 대신, 단 하나의 변수로 원래 레이어 저장
    private int savedOriginLayer;
    
    // 상태 관리 변수
    private bool isDashing = false;
    private Rigidbody currentRb;
    private SkillController currentExecutor;

    protected override void Awake()
    {
        base.Awake();
        if (usableCountComponent != null)
            Destroy(usableCountComponent as Component);

        if (groundLayerMask == 0)
            groundLayerMask = LayerMask.GetMask("Default", "Terrain", "Ground");
    }

    protected void OnDisable()
    {
        if (isDashing)
        {
            StopAllCoroutines();
            EndDashLogic();
        }
    }

    public override void Execute(SkillController executor, Vector3 pos, Vector3 dir)
    {
        base.Execute(executor, pos, dir);
        PlayFollowEffectAtRemote(executor);

        if (executor.photonView.IsMine)
        {
            var rb = executor.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (isDashing) return;

                currentExecutor = executor;
                currentRb = rb;

                StartDashLogic(executor.transform, rb);
                
                rb.velocity = Vector3.zero; 
                rb.AddForce(dir * dashForce, ForceMode.VelocityChange);

                StartCoroutine(DashStopRoutine(executor, rb));
            }
        }
    }

    private void StartDashLogic(Transform root, Rigidbody rb)
    {
        isDashing = true;

        // ✅ 1. 자식들 순회 없이, 현재 오브젝트(Root)의 레이어만 저장하고 변경
        savedOriginLayer = root.gameObject.layer;
        
        int dashLayer = LayerMask.NameToLayer(dashLayerName);
        if (dashLayer != -1)
        {
            root.gameObject.layer = dashLayer;
        }
        else
        {
            Debug.LogWarning($"Layer '{dashLayerName}'가 존재하지 않습니다.");
        }

        // 물리 안정화 (터널링 방지, 중력 끄기)
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; 
        //rb.useGravity = false; 
    }

    private void EndDashLogic()
    {
        if (currentRb != null)
        {
            currentRb.velocity = new Vector3(0f, currentRb.velocity.y, 0f);
            //currentRb.useGravity = true;
            currentRb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        // ✅ 2. 저장해뒀던 원래 레이어로 Root만 복구
        if (currentExecutor != null)
        {
            currentExecutor.gameObject.layer = savedOriginLayer;
        }
        
        isDashing = false;
        currentRb = null;
        currentExecutor = null;
    }

    private IEnumerator DashStopRoutine(SkillController executor, Rigidbody rb)
    {
        if (!executor.photonView.IsMine) yield break;

        float startTime = Time.time;
        yield return new WaitForSeconds(minDashDuration);

        while (Time.time - startTime < maxDashTime)
        {
            if (Physics.Raycast(executor.transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayerMask))
            {
                if (rb.velocity.y <= 0.1f)
                {
                    break;
                }
            }
            yield return null;
        }

        EndDashLogic();
    }
}