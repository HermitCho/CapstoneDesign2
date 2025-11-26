using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.Heat;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class TutorialClear : MonoBehaviour
{
    [Header("모달 창 참조")]
    [SerializeField] private ModalWindowManager modalWindowManager;
    private bool hasTriggered = false;
    private MoveController playerMoveController;
    private CameraController playerCameraController;
    private TestMoveAnimationController playerAnimationController;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;
        
        PhotonView playerPhotonView = other.GetComponentInParent<PhotonView>();
        if (playerPhotonView == null || !playerPhotonView.IsMine) return;

        hasTriggered = true;
        FindPlayerComponents(other);
        ShowCompletionModal();
    }

    private void FindPlayerComponents(Collider playerCollider)
    {
        Transform playerRoot = playerCollider.transform.root;

        playerMoveController = playerRoot.GetComponent<MoveController>();
        if (playerMoveController == null)
        {
            playerMoveController = playerRoot.GetComponentInChildren<MoveController>();
        }

        playerCameraController = playerRoot.GetComponent<CameraController>();
        if (playerCameraController == null)
        {
            playerCameraController = playerRoot.GetComponentInChildren<CameraController>();
        }

        playerAnimationController = playerRoot.GetComponent<TestMoveAnimationController>();
        if (playerAnimationController == null)
        {
            playerAnimationController = playerRoot.GetComponentInChildren<TestMoveAnimationController>();
        }
    }

    private void ShowCompletionModal()
    {
        if (modalWindowManager == null) return;

        // 플레이어 조작 비활성화
        DisablePlayerControls();

        AudioManager.Inst.PlayOneShot("SFX_Game_Tutorial_Clear");

        // 커서 표시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 모달 창 열기
        modalWindowManager.OpenWindow();
    }

    private void DisablePlayerControls()
    {
        if (playerMoveController != null)
        {
            playerMoveController.DisableMoveControls();
        }

        if (playerCameraController != null)
        {
            playerCameraController.DisableCameraControl();
        }

        TestShoot.SetIsShooting(false);

        // 애니메이션 정지
        if (playerAnimationController != null)
        {
            playerAnimationController.StopAllAnimations();
        }
    }

    public void OnClickLobbyButton()
    {
        StartCoroutine(ReturnToLobby());
    }

    private IEnumerator ReturnToLobby()
    {
        TutorialStateManager.ResetAll();

        // 모달 창 닫기
        if (modalWindowManager != null)
        {
            modalWindowManager.CloseWindow();
        }

        yield return new WaitForSeconds(0.5f);

        // 방 나가기
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            
            float timeout = 3f;
            float timer = 0f;
            while (PhotonNetwork.InRoom && timer < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
        }

        // 로비 나가기
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
            
            float timeout = 3f;
            float timer = 0f;
            while (PhotonNetwork.InLobby && timer < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
        }

        // Photon 연결 해제
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            
            float timeout = 3f;
            float timer = 0f;
            while (PhotonNetwork.IsConnected && timer < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
        }

        // Lobby 씬 로드
        SceneManager.LoadScene("Lobby");
    }
}
