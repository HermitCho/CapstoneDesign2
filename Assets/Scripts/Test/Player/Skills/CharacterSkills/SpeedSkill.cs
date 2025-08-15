using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpeedSkill : CharacterSkill
{
    [Header("스킬 효과 설정")]
    [SerializeField] private float speedMultiplier = 1.5f;

    private MoveController moveController;
    private bool isBuffActive = false;
    private float originalSpeed = -1f;

    protected override void Start()
    {
        base.Start();
        moveController = GetComponent<MoveController>();
        if (moveController == null)
        {
            Debug.LogError("SpeedSkill: MoveController를 찾을 수 없습니다.");
        }
    }

    protected override void OnSkillExecuted()
    {
        base.OnSkillExecuted();
        if (!PhotonView.Get(this).IsMine || moveController == null || isBuffActive) return;
        PhotonView.Get(this).RPC("RPC_ApplySpeedBuff", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_ApplySpeedBuff()
    {
        var moveControllerType = typeof(MoveController);
        var speedField = moveControllerType.GetField("cachedSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (speedField != null)
        {
            originalSpeed = (float)speedField.GetValue(moveController);
            float buffedSpeed = originalSpeed * speedMultiplier;
            speedField.SetValue(moveController, buffedSpeed);
            isBuffActive = true;
        }
        else
        {
            Debug.LogError("SpeedSkill: MoveController의 cachedSpeed 필드를 찾을 수 없습니다.");
        }
    }

    protected override void OnSkillFinished()
    {
        base.OnSkillFinished();
        if (!PhotonView.Get(this).IsMine || moveController == null || !isBuffActive) return;
        PhotonView.Get(this).RPC("RPC_RemoveSpeedBuff", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_RemoveSpeedBuff()
    {
        var moveControllerType = typeof(MoveController);
        var speedField = moveControllerType.GetField("cachedSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (speedField != null && originalSpeed > 0f)
        {
            speedField.SetValue(moveController, originalSpeed);
        }
        isBuffActive = false;
    }
}
