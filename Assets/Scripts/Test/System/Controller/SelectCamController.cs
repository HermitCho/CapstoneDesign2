using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectCamController : MonoBehaviour
{

    [Header("카메라 설정")]
    [Tooltip("메인 카메라 할당")]
    [SerializeField] private Camera mainCam;
    [Space(10)]

    [Tooltip("초기 카메라 위치 설정")]
    [SerializeField] private Transform initialCamPosition;
    [Tooltip("초기 카메라 회전 설정")]
    [SerializeField] private Vector3 initialCamRotation;
    [Space(10)]
    [Tooltip("카메라 부드러움 설정")]
    [SerializeField] private float camSmoothTime = 2f;

    [Header("캐릭터 오브젝트 설정")]
    [Tooltip("캐릭터 1 할당")]
    [SerializeField] private GameObject Character1;
    [Tooltip("캐릭터 1 애니메이션 컴포넌트 할당")]
    [SerializeField] private Animator character1Animator;
    [Tooltip("캐릭터 1 애니메이션 트리거 할당")]
    [SerializeField] private string character1AnimationTrigger;
    [Space(10)]
    [Tooltip("캐릭터 1 카메라 위치 할당")]
    [SerializeField] private Transform character1CamPosition;
    [Tooltip("캐릭터 1 카메라 회전 할당")]
    [SerializeField] private Vector3 character1CamRotation;
 
    [Space(10)]

    [Tooltip("캐릭터 2 할당")]
    [SerializeField] private GameObject Character2;
    [Tooltip("캐릭터 2 애니메이션 컴포넌트 할당")]
    [SerializeField] private Animator character2Animator;
    [Tooltip("캐릭터 2 애니메이션 트리거 할당")]
    [SerializeField] private string character2AnimationTrigger;
    [Space(10)]
    [Tooltip("캐릭터 2 카메라 위치 할당")]
    [SerializeField] private Transform character2CamPosition;
    [Tooltip("캐릭터 2 카메라 회전 할당")]
    [SerializeField] private Vector3 character2CamRotation;
    [Space(10)]
    
    [Tooltip("캐릭터 3 할당")]
    [SerializeField] private GameObject Character3;
    [Tooltip("캐릭터 3 애니메이션 컴포넌트 할당")]
    [SerializeField] private Animator character3Animator;
    [Tooltip("캐릭터 3 애니메이션 트리거 할당")]
    [SerializeField] private string character3AnimationTrigger;
    [Space(10)]
    [Tooltip("캐릭터 3 카메라 위치 할당")]
    [SerializeField] private Transform character3CamPosition;
    [Tooltip("캐릭터 3 카메라 회전 할당")]
    [SerializeField] private Vector3 character3CamRotation;
    [Space(10)]

    [Tooltip("캐릭터 4 할당")]
    [SerializeField] private GameObject Character4;
    [Tooltip("캐릭터 4 애니메이션 컴포넌트 할당")]
    [SerializeField] private Animator character4Animator;
    [Tooltip("캐릭터 4 애니메이션 트리거 할당")]
    [SerializeField] private string character4AnimationTrigger;
    [Space(10)]
    [Tooltip("캐릭터 4 카메라 위치 할당")]
    [SerializeField] private Transform character4CamPosition;
    [Tooltip("캐릭터 4 카메라 회전 할당")]
    [SerializeField] private Vector3 character4CamRotation;

    // Private variables
    private Vector3 velocity = Vector3.zero;

    // Start is called before the first frame update
    void Awake()
    {
        // 메인 카메라가 할당되지 않았으면 자동으로 찾기
        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = FindObjectOfType<Camera>();
            }
        }

        // 초기 카메라 위치와 회전 설정
        if (mainCam != null && initialCamPosition != null)
        {
            mainCam.transform.position = initialCamPosition.position;
            mainCam.transform.rotation = Quaternion.Euler(initialCamRotation);
        }

        // 게임 시작 시 이 스크립트를 비활성화하는 이벤트 구독
        // GameManager나 다른 매니저에서 게임 시작 이벤트를 발생시키면 여기서 구독
        // 예시: GameManager.OnGameStarted += DisableSelectCamController;
    }


    /// <summary>
    /// 캐릭터 1 선택 시 호출
    /// </summary>
    public void OnClickCharacter11()
    {
        if (character1CamPosition != null)
        {
            StopAllCoroutines();
            StartCoroutine(MoveCameraToCharacter(character1CamPosition, character1CamRotation, character1Animator, character1AnimationTrigger));
        }
    }

    /// <summary>
    /// 캐릭터 1 호버 시 호출
    /// </summary>
    public void OnHoverCharacter1()
    {
        if (character1CamPosition != null)
        {
            StopAllCoroutines();
            StartCoroutine(MoveCameraToCharacter(character1CamPosition, character1CamRotation, character1Animator, character1AnimationTrigger));
        }
    }

    /// <summary>
    /// 캐릭터 2 선택 시 호출
    /// </summary>
    public void OnClickCharacter2()
    {
        if (character2CamPosition != null)
        {
            StopAllCoroutines();
            StartCoroutine(MoveCameraToCharacter(character2CamPosition, character2CamRotation, character2Animator, character2AnimationTrigger));
        }
    }

    /// <summary>
    /// 캐릭터 2 호버 시 호출
    /// </summary>
    public void OnHoverCharacter2()
    {
        if (character2CamPosition != null)
        {
            StopAllCoroutines();
            StartCoroutine(MoveCameraToCharacter(character2CamPosition, character2CamRotation, character2Animator, character2AnimationTrigger));
        }
    }

    /// <summary>
    /// 캐릭터 3 선택 시 호출
    /// </summary>
    public void OnClickCharacter3() 
    {
        if (character3CamPosition != null)
        {
            StopAllCoroutines();
            StartCoroutine(MoveCameraToCharacter(character3CamPosition, character3CamRotation, character3Animator, character3AnimationTrigger));
        }
    }

    /// <summary>
    /// 캐릭터 3 호버 시 호출
    /// </summary>
    public void OnHoverCharacter3()
    {
        if (character3CamPosition != null)
        {
            StopAllCoroutines();
            StartCoroutine(MoveCameraToCharacter(character3CamPosition, character3CamRotation, character3Animator, character3AnimationTrigger));
        }
    }

    /// <summary>
    /// 캐릭터 4 선택 시 호출
    /// </summary>
    public void OnClickCharacter4()
    {
        if (character4CamPosition != null)
        {
            StopAllCoroutines();
            StartCoroutine(MoveCameraToCharacter(character4CamPosition, character4CamRotation, character4Animator, character4AnimationTrigger));
        }
    }

    /// <summary>
    /// 캐릭터 4 호버 시 호출
    /// </summary>
    public void OnHoverCharacter4()
    {
        if (character4CamPosition != null)
        {
            StopAllCoroutines();
            StartCoroutine(MoveCameraToCharacter(character4CamPosition, character4CamRotation, character4Animator, character4AnimationTrigger));
        }
    }

    /// <summary>
    /// 카메라를 특정 캐릭터 위치로 부드럽게 이동시키는 코루틴
    /// </summary>
    /// <param name="targetPosition">목표 카메라 위치</param>
    /// <param name="targetRotation">목표 카메라 회전</param>
    /// <param name="animator">애니메이터</param>
    /// <param name="animationTrigger">애니메이션 트리거</param>
    /// <returns></returns>
    private IEnumerator MoveCameraToCharacter(Transform targetPosition, Vector3 targetRotation, Animator animator, string animationTrigger)
    {
        if (mainCam == null) yield break;

        Vector3 startPosition = mainCam.transform.position;
        Quaternion startRotation = mainCam.transform.rotation;
        Quaternion targetQuaternion = Quaternion.Euler(targetRotation);

        // 카메라 이동 시작과 동시에 애니메이션 트리거
        if (animator != null && !string.IsNullOrEmpty(animationTrigger))
        {
            animator.SetTrigger(animationTrigger);
        }

        float elapsedTime = 0f;

        // 카메라 부드러운 이동
        while (elapsedTime < camSmoothTime)
        {
            float t = elapsedTime / camSmoothTime;
            
            // 부드러운 보간
            mainCam.transform.position = Vector3.SmoothDamp(mainCam.transform.position, targetPosition.position, ref velocity, camSmoothTime * 0.1f);
            mainCam.transform.rotation = Quaternion.Slerp(startRotation, targetQuaternion, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 정확한 목표 위치로 설정
        mainCam.transform.position = targetPosition.position;
        mainCam.transform.rotation = targetQuaternion;
    }

    /// <summary>
    /// 게임 시작 시 이 스크립트를 비활성화
    /// </summary>
    public void DisableSelectCamController()
    {
        // 모든 코루틴 중지
        StopAllCoroutines();
        
        // 이벤트 구독 해제
        // GameManager.OnGameStarted -= DisableSelectCamController;
        
        // 스크립트 비활성화
        this.enabled = false;
        
        Debug.Log("캐릭터 선택 카메라 컨트롤러가 비활성화되었습니다.");
    }

    /// <summary>
    /// 게임 시작 시 호출할 수 있는 공개 메서드
    /// </summary>
    public void OnGameStarted()
    {
        DisableSelectCamController();
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        // GameManager.OnGameStarted -= DisableSelectCamController;
    }
}
