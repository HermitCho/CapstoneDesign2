using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TutorialShoot : MonoBehaviour
{
    [Header("튜토리얼 UI 참조")]
    [SerializeField] private TutorialUI tutorialUI;
    [Space(10)]

    [Header("튜토리얼 완료 참조")]
    [SerializeField] private TutorialComplete tutorialComplete;
    [Space(10)]

    [Header("총기 튜토리얼 설정")]
    [SerializeField] private int requiredShots = 3; // 필요한 발사 횟수
    [SerializeField] private int requiredReloads = 1; // 필요한 재장전 횟수

    private bool isCounting = false;
    private Transform playerTransform;
    private TestGun playerGun;
    private TestShoot playerTestShoot;
    private CameraController playerCameraController;

    private int shotsFired = 0;
    private int reloadsCompleted = 0;
    private int lastMagAmmo = 0;
    private bool hasZoomed = false;

    void OnEnable()
    {
        if (tutorialUI != null)
        {
            tutorialUI.OnTutorialClosed += BeginCounting;
        }
    }

    void OnDisable()
    {
        if (tutorialUI != null)
        {
            tutorialUI.OnTutorialClosed -= BeginCounting;
        }
        TestGun.OnLocalReloadStarted -= OnReloadStarted;
        isCounting = false;
    }

    void Update()
    {
        if (!isCounting || playerGun == null) return;

        // 줌 상태 확인
        if (playerCameraController != null && playerCameraController.IsZoom())
        {
            hasZoomed = true;
        }

        // 발사 횟수 확인 (탄약이 줄어들면 발사한 것으로 간주)
        int currentMagAmmo = playerGun.CurrentMagAmmo;
        if (currentMagAmmo < lastMagAmmo)
        {
            shotsFired++;
        }
        lastMagAmmo = currentMagAmmo;

        // 모든 조건 만족 시 완료
        if (hasZoomed && shotsFired >= requiredShots && reloadsCompleted >= requiredReloads)
        {
            CompleteTutorial();
        }
    }
    
    private void OnReloadStarted()
    {
        if (!isCounting) return;
        reloadsCompleted++;
    }

    private void BeginCounting()
    {
        // 튜토리얼 패널 닫힘 이후 시작
        LocateLocalPlayer();
        if (playerGun == null) return;

        // 초기화
        shotsFired = 0;
        reloadsCompleted = 0;
        hasZoomed = false;
        lastMagAmmo = playerGun.CurrentMagAmmo;
        isCounting = true;
        
        // 재장전 이벤트 구독
        TestGun.OnLocalReloadStarted += OnReloadStarted;
    }

    private void LocateLocalPlayer()
    {
        playerTransform = null;
        playerGun = null;
        playerTestShoot = null;
        playerCameraController = null;

        // MoveController를 통해 로컬 플레이어 찾기
        MoveController[] movers = FindObjectsOfType<MoveController>();
        for (int i = 0; i < movers.Length; i++)
        {
            var view = movers[i].GetComponent<PhotonView>();
            if (view == null || view.IsMine)
            {
                playerTransform = movers[i].transform;
                playerGun = playerTransform.GetComponentInChildren<TestGun>();
                playerTestShoot = playerTransform.GetComponentInChildren<TestShoot>();
                playerCameraController = playerTransform.GetComponentInChildren<CameraController>();
                break;
            }
        }

        // 폴백: CameraController로 찾기
        if (playerTransform == null)
        {
            var cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null)
            {
                playerTransform = cameraController.transform.root;
                playerGun = playerTransform.GetComponentInChildren<TestGun>();
                playerTestShoot = playerTransform.GetComponentInChildren<TestShoot>();
                playerCameraController = cameraController;
            }
        }
    }

    private void CompleteTutorial()
    {
        isCounting = false;
        TestGun.OnLocalReloadStarted -= OnReloadStarted;

        if (tutorialUI != null)
        {
            tutorialUI.ShowCompleteSticker();
        }
        if (tutorialComplete != null)
        {
            tutorialComplete.OpenDoor();
        }
    }
}
