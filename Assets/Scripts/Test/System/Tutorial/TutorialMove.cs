using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TutorialMove : MonoBehaviour
{
    [Header("튜토리얼 UI 참조")]
    [SerializeField] private TutorialUI tutorialUI;
    [Space(10)]

    [Header("튜토리얼 완료 참조")]
    [SerializeField] private TutorialComplete tutorialComplete;
    [Space(10)]

    [Header("이동 요구 시간 (각 방향)")]
    [SerializeField] private float minMoveTime = 0.5f; // 각 방향 최소 입력 시간
    [SerializeField] private float inputThreshold = 0.2f; // 입력 임계값 (|x|/|y|)

    private bool isCounting = false;
    private Transform playerTransform;
    private Vector2 lastMoveInput;

    private float forwardTime;
    private float backTime;
    private float leftTime;
    private float rightTime;

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
        isCounting = false;
        InputManager.OnMoveInput -= OnMoveInput;
    }

    void Update()
    {
        if (!isCounting || playerTransform == null) return;

        float dt = Time.deltaTime;

        // 입력 기반 누적 (플레이어 기준 축)
        if (lastMoveInput.y > inputThreshold) forwardTime += dt;
        else if (lastMoveInput.y < -inputThreshold) backTime += dt;

        if (lastMoveInput.x > inputThreshold) rightTime += dt;
        else if (lastMoveInput.x < -inputThreshold) leftTime += dt;

        if (forwardTime >= minMoveTime && backTime >= minMoveTime &&
            leftTime >= minMoveTime && rightTime >= minMoveTime)
        {
            CompleteTutorial();
        }
    }

    private void BeginCounting()
    {
        // 튜토리얼 패널 닫힘 이후 시작
        LocateLocalPlayer();
        if (playerTransform == null) return;

        forwardTime = backTime = leftTime = rightTime = 0f;
        lastMoveInput = Vector2.zero;
        isCounting = true;
        InputManager.OnMoveInput += OnMoveInput;
    }

    private void LocateLocalPlayer()
    {
        playerTransform = null;
        MoveController[] movers = FindObjectsOfType<MoveController>();
        for (int i = 0; i < movers.Length; i++)
        {
            var view = movers[i].GetComponent<PhotonView>();
            if (view == null || view.IsMine)
            {
                playerTransform = movers[i].transform;
                break;
            }
        }
        if (playerTransform == null)
        {
            var any = FindObjectOfType<CameraController>();
            if (any != null)
            {
                playerTransform = any.transform.root;
            }
        }
    }

    private void OnMoveInput(Vector2 input)
    {
        if (!isCounting) return;
        lastMoveInput = input;
    }

    private void CompleteTutorial()
    {
        isCounting = false;
        InputManager.OnMoveInput -= OnMoveInput;

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
