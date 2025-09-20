using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Michsky.UI.Heat;

public class LoginButtonController : MonoBehaviour
{

    [Header("회원가입 화면 설정")]
    [SerializeField] private TMP_InputField createNicknameInputField; //닉네임 입력 필드
    [SerializeField] private TMP_InputField createIdInputField; //아이디 입력 필드
    [SerializeField] private TMP_InputField createpPasswordInputField; //비밀번호 입력 필드
    [SerializeField] private TMP_InputField createPasswordConfirmInputField; //비밀번호 확인 필드
    [SerializeField] private ButtonManager createButton; //회원가입 시도 버튼
    [SerializeField] private ButtonManager createCancelButton; //로그인 화면으로 돌아가기 버튼
    [Space(10)]
    [Header("로그인 화면 설정")]
    [SerializeField] private TMP_InputField loginIdInputField; //아이디 입력 필드
    [SerializeField] private TMP_InputField loginPasswordInputField; //비밀번호 입력 필드
    [SerializeField] private ButtonManager loginButton; //로그인 버튼
    [SerializeField] private ButtonManager signUpButton; //회원가입 버튼

    [Header("모달창 설정")]
    [SerializeField] private ModalWindowManager signUpSuccessModalWindowManager; //회원가입 성공 모달창
    [SerializeField] private ModalWindowManager signUpFailModalWindowManager; //회원가입 실패 모달창
    [SerializeField] private ModalWindowManager loginFailModalWindowManager; //로그인 실패 모달창

    [Header("패널 관리")]
    [SerializeField] private PanelManager panelManager; //패널 매니저
    [SerializeField] private string loginPanelName = "Login"; //로그인 패널 이름
    [SerializeField] private string signUpPanelName = "SignUp"; //회원가입 패널 이름

    [Header("로딩 UI")]
    [SerializeField] private GameObject loadingIndicator; //로딩 인디케이터

    // 내부 상태 관리
    private bool isProcessing = false;

    void Start()
    {
        // 데이터베이스 매니저 초기화 확인
        if (DatabaseManager.Instance == null)
        {
            Debug.LogError("LoginButtonController: DatabaseManager를 찾을 수 없습니다!");
        }

        // 로딩 인디케이터 초기 상태 설정
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(false);
        }

        // ButtonManager 이벤트 리스너 설정
        SetupButtonEvents();

        // 버튼 상태 초기화
        UpdateButtonStates();
    }

    /// <summary>
    /// 로그인 버튼 클릭 시 호출되는 메서드
    /// 로그인 시도
    /// </summary>
    public void OnClickLoginButton()
    {
        if (isProcessing) return;

        string userId = loginIdInputField.text.Trim();
        string password = loginPasswordInputField.text.Trim();

        // 입력 검증
        if (string.IsNullOrEmpty(userId))
        {
            ShowLoginFailModal("아이디를 입력해주세요.");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowLoginFailModal("비밀번호를 입력해주세요.");
            return;
        }

        // 로그인 시도
        StartCoroutine(LoginProcess(userId, password));
    }

    /// <summary>
    /// 회원가입 버튼 클릭 시 호출되는 메서드
    /// 회원가입 화면으로 이동
    /// </summary>
    public void OnClickSignUpButton()
    {
        if (isProcessing) return;

        // 회원가입 패널로 전환
        if (panelManager != null)
        {
            panelManager.OpenPanel(signUpPanelName);
            ClearAllInputFields();
        }
        else
        {
            Debug.LogError("LoginButtonController: PanelManager가 설정되지 않았습니다!");
        }
    }

    /// <summary>
    /// 로그인 화면으로 돌아가기 버튼 클릭 시 호출되는 메서드
    /// 로그인 화면으로 이동
    /// </summary>
    public void OnClickLoginCancelButton()
    {
        if (isProcessing) return;

        // 로그인 패널로 전환
        if (panelManager != null)
        {
            panelManager.OpenPanel(loginPanelName);
            ClearAllInputFields();
        }
        else
        {
            Debug.LogError("LoginButtonController: PanelManager가 설정되지 않았습니다!");
        }
    }

    /// <summary>
    /// 회원가입 시도 버튼 클릭 시 호출되는 메서드
    /// 회원가입 시도
    /// </summary>
    public void OnClickSignUpTryButton()
    {
        if (isProcessing) return;

        string userId = createIdInputField.text.Trim();
        string nickname = createNicknameInputField.text.Trim();
        string password = createpPasswordInputField.text.Trim();
        string confirmPassword = createPasswordConfirmInputField.text.Trim();

        // 입력 검증
        if (string.IsNullOrEmpty(userId))
        {
            ShowSignUpFailModal("아이디를 입력해주세요.");
            return;
        }

        if (string.IsNullOrEmpty(nickname))
        {
            ShowSignUpFailModal("닉네임을 입력해주세요.");
            return;
        }

        if (nickname.Length > 5)
        {
            ShowSignUpFailModal("닉네임은 5글자 이하로 입력해주세요.");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowSignUpFailModal("비밀번호를 입력해주세요.");
            return;
        }

        if (password != confirmPassword)
        {
            ShowSignUpFailModal("비밀번호가 일치하지 않습니다.");
            return;
        }

        if (password.Length < 4)
        {
            ShowSignUpFailModal("비밀번호는 4자 이상 입력해주세요.");
            return;
        }

        // 회원가입 시도
        StartCoroutine(SignUpProcess(userId, nickname, password));
    }

    #region 데이터베이스 처리 코루틴

    /// <summary>
    /// 로그인 처리 코루틴
    /// </summary>
    private IEnumerator LoginProcess(string userId, string password)
    {
        SetProcessingState(true);
        ShowLoadingIndicator(true);

        Debug.Log($"로그인 시도: {userId}");

        // 데이터베이스 연결 확인
        if (!DatabaseManager.Instance.IsConnected())
        {
            ShowLoginFailModal("데이터베이스 연결에 실패했습니다.");
            SetProcessingState(false);
            ShowLoadingIndicator(false);
            yield break;
        }

        bool loginCompleted = false;
        bool loginSuccess = false;
        string loginMessage = "";
        UserData userData = null;

        // 로그인 시도
        DatabaseManager.Instance.LoginUser(userId, password, (success, message, user) =>
        {
            loginSuccess = success;
            loginMessage = message;
            userData = user;
            loginCompleted = true;
        });

        // 로그인 완료까지 대기
        yield return new WaitUntil(() => loginCompleted);

        SetProcessingState(false);
        ShowLoadingIndicator(false);

        if (loginSuccess)
        {
            // 로그인 성공
            Debug.Log($"로그인 성공: {userData}");
            
            // 현재 사용자 정보 설정
            CurrentUser.Instance.SetUserData(userData);
            
            // Intro 씬으로 전환
            StartCoroutine(LoadIntroScene());
        }
        else
        {
            // 로그인 실패
            Debug.Log($"로그인 실패: {loginMessage}");
            ShowLoginFailModal(loginMessage);
        }
    }

    /// <summary>
    /// 회원가입 처리 코루틴
    /// </summary>
    private IEnumerator SignUpProcess(string userId, string nickname, string password)
    {
        SetProcessingState(true);
        ShowLoadingIndicator(true);

        Debug.Log($"회원가입 시도: {userId} - {nickname}");

        // 데이터베이스 연결 확인
        if (!DatabaseManager.Instance.IsConnected())
        {
            ShowSignUpFailModal("데이터베이스 연결에 실패했습니다.");
            SetProcessingState(false);
            ShowLoadingIndicator(false);
            yield break;
        }

        bool signUpCompleted = false;
        bool signUpSuccess = false;
        string signUpMessage = "";

        // 회원가입 시도
        DatabaseManager.Instance.RegisterUser(userId, nickname, password, (success, message) =>
        {
            signUpSuccess = success;
            signUpMessage = message;
            signUpCompleted = true;
        });

        // 회원가입 완료까지 대기
        yield return new WaitUntil(() => signUpCompleted);

        SetProcessingState(false);
        ShowLoadingIndicator(false);

        if (signUpSuccess)
        {
            // 회원가입 성공
            Debug.Log($"회원가입 성공: {userId} - {nickname}");
            ShowSignUpSuccessModal(signUpMessage);
            ClearSignUpInputFields();
        }
        else
        {
            // 회원가입 실패
            Debug.Log($"회원가입 실패: {signUpMessage}");
            ShowSignUpFailModal(signUpMessage);
        }
    }

    /// <summary>
    /// Intro 씬 로드 코루틴
    /// </summary>
    private IEnumerator LoadIntroScene()
    {
        Debug.Log("Intro 씬으로 전환 중...");
        
        // 페이드 아웃 효과 등을 추가할 수 있음
        yield return new WaitForSeconds(0.5f);
        
        // Intro 씬 로드
        SceneManager.LoadScene("Intro");
    }

    #endregion

    #region UI 관리 메서드

    /// <summary>
    /// ButtonManager 이벤트 리스너 설정
    /// </summary>
    private void SetupButtonEvents()
    {
        // 로그인 버튼 이벤트 연결
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnClickLoginButton);
        }

        // 회원가입 버튼 이벤트 연결
        if (signUpButton != null)
        {
            signUpButton.onClick.AddListener(OnClickSignUpButton);
        }

        // 회원가입 시도 버튼 이벤트 연결
        if (createButton != null)
        {
            createButton.onClick.AddListener(OnClickSignUpTryButton);
        }

        // 취소 버튼 이벤트 연결
        if (createCancelButton != null)
        {
            createCancelButton.onClick.AddListener(OnClickLoginCancelButton);
        }

        Debug.Log("LoginButtonController: ButtonManager 이벤트 리스너 설정 완료");
    }

    /// <summary>
    /// 처리 상태 설정 및 UI 업데이트
    /// </summary>
    private void SetProcessingState(bool processing)
    {
        isProcessing = processing;
        UpdateButtonStates();
    }

    /// <summary>
    /// 버튼 상태 업데이트
    /// </summary>
    private void UpdateButtonStates()
    {
        bool interactable = !isProcessing;

        if (loginButton != null) loginButton.isInteractable = interactable;
        if (signUpButton != null) signUpButton.isInteractable = interactable;
        if (createButton != null) createButton.isInteractable = interactable;
        if (createCancelButton != null) createCancelButton.isInteractable = interactable;
    }

    /// <summary>
    /// 로딩 인디케이터 표시/숨김
    /// </summary>
    private void ShowLoadingIndicator(bool show)
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(show);
        }
    }

    /// <summary>
    /// 모든 입력 필드 클리어
    /// </summary>
    private void ClearAllInputFields()
    {
        ClearLoginInputFields();
        ClearSignUpInputFields();
    }

    /// <summary>
    /// 로그인 입력 필드 클리어
    /// </summary>
    private void ClearLoginInputFields()
    {
        if (loginIdInputField != null) loginIdInputField.text = "";
        if (loginPasswordInputField != null) loginPasswordInputField.text = "";
    }

    /// <summary>
    /// 회원가입 입력 필드 클리어
    /// </summary>
    private void ClearSignUpInputFields()
    {
        if (createIdInputField != null) createIdInputField.text = "";
        if (createNicknameInputField != null) createNicknameInputField.text = "";
        if (createpPasswordInputField != null) createpPasswordInputField.text = "";
        if (createPasswordConfirmInputField != null) createPasswordConfirmInputField.text = "";
    }

    #endregion

    #region 모달창 표시 메서드

    /// <summary>
    /// 로그인 실패 모달창 표시
    /// </summary>
    private void ShowLoginFailModal(string message)
    {
        if (loginFailModalWindowManager != null)
        {
            loginFailModalWindowManager.descriptionText = message;
            loginFailModalWindowManager.UpdateUI();
            loginFailModalWindowManager.OpenWindow();
        }
        else
        {
            Debug.LogWarning($"로그인 실패 모달창이 설정되지 않았습니다: {message}");
        }
    }

    /// <summary>
    /// 회원가입 성공 모달창 표시
    /// </summary>
    private void ShowSignUpSuccessModal(string message)
    {
        if (signUpSuccessModalWindowManager != null)
        {
            signUpSuccessModalWindowManager.descriptionText = message;
            signUpSuccessModalWindowManager.UpdateUI();
            signUpSuccessModalWindowManager.OpenWindow();
        }
        else
        {
            Debug.LogWarning($"회원가입 성공 모달창이 설정되지 않았습니다: {message}");
        }
    }

    /// <summary>
    /// 회원가입 실패 모달창 표시
    /// </summary>
    private void ShowSignUpFailModal(string message)
    {
        if (signUpFailModalWindowManager != null)
        {
            signUpFailModalWindowManager.descriptionText = message;
            signUpFailModalWindowManager.UpdateUI();
            signUpFailModalWindowManager.OpenWindow();
        }
        else
        {
            Debug.LogWarning($" 회원가입 실패 모달창이 설정되지 않았습니다: {message}");
        }
    }

    #endregion

    #region 디버그 및 유틸리티

    /// <summary>
    /// 현재 연결 상태 확인 (디버깅용)
    /// </summary>
    [ContextMenu("데이터베이스 연결 테스트")]
    private void TestDatabaseConnection()
    {
        if (DatabaseManager.Instance != null)
        {
            bool isConnected = DatabaseManager.Instance.IsConnected();
            Debug.Log($"데이터베이스 연결 상태: {(isConnected ? "연결됨" : "연결 안됨")}");
            Debug.Log($"연결 문자열: {DatabaseManager.Instance.GetConnectionString()}");
        }
        else
        {
            Debug.LogError("DatabaseManager 인스턴스를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 현재 로그인 사용자 정보 확인 (디버깅용)
    /// </summary>
    [ContextMenu("현재 사용자 정보 확인")]
    private void CheckCurrentUser()
    {
        if (CurrentUser.Instance.IsLoggedIn())
        {
            var userData = CurrentUser.Instance.GetUserData();
            Debug.Log($"현재 로그인 사용자: {userData}");
        }
        else
        {
            Debug.Log("현재 로그인된 사용자가 없습니다.");
        }
    }

    #endregion
}
