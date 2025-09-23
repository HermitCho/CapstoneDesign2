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

    [Header("페이드 효과")]
    [SerializeField] private GameObject fadeOverlay; //페이드 오버레이 (검은색 이미지)
    [SerializeField] private float fadeOutDuration = 1.0f; //페이드 아웃 지속 시간

    // 내부 상태 관리
    private bool isProcessing = false;
    private int signUpResult = -1;

    void Start()
    {
        // 구글 스프레드시트 매니저 초기화 확인
        if (GoogleSheetsManager.Instance == null)
        {
            Debug.LogError("LoginButtonController: GoogleSheetsManager를 찾을 수 없습니다!");
        }

        // 로딩 인디케이터 초기 상태 설정
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(false);
        }

        // 페이드 오버레이 초기 상태 설정
        if (fadeOverlay != null)
        {
            fadeOverlay.SetActive(false);
        }

        // 비밀번호 입력 필드 마스킹 설정
        SetupPasswordFields();

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

        // 아이디 검증: 영문+숫자 조합, 최소 6글자 이상
        if (userId.Length < 6)
        {
            ShowSignUpFailModal("아이디는 6글자 이상 입력해주세요.");
            return;
        }

        if (!IsValidUserId(userId))
        {
            ShowSignUpFailModal("아이디는 영문과 숫자를 모두 포함하여 입력해주세요.");
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

        // 비밀번호 검증: 영문+숫자 조합
        if (!IsValidPassword(password))
        {
            ShowSignUpFailModal("비밀번호는 영문과 숫자를 조합하여 입력해주세요.");
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

        // 구글 스프레드시트 연결 확인
        if (!GoogleSheetsManager.Instance.IsConnected())
        {
            ShowLoginFailModal("구글 스프레드시트 연결에 실패했습니다.");
            SetProcessingState(false);
            ShowLoadingIndicator(false);
            yield break;
        }

        bool loginCompleted = false;
        bool loginSuccess = false;
        string loginMessage = "";
        UserGameData userData = null;

        // 로그인 시도
        GoogleSheetsManager.Instance.LoginUser(userId, password, (success, message, user) =>
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
            CurrentUser.Instance.SetUserGameData(userData);
            
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

        // 구글 스프레드시트 연결 확인
        if (!GoogleSheetsManager.Instance.IsConnected())
        {
            ShowSignUpFailModal("구글 스프레드시트 연결에 실패했습니다.");
            SetProcessingState(false);
            ShowLoadingIndicator(false);
            yield break;
        }

        bool signUpCompleted = false;
        bool signUpSuccess = false;
        string signUpMessage = "";

        // 회원가입 시도
        GoogleSheetsManager.Instance.RegisterUser(userId, nickname, password, (success, message, result) =>
        {
            signUpSuccess = success;
            signUpMessage = message;
            signUpResult = result;
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
            //signUpResult = -1;
        }
        else
        {
            if(signUpResult == 0)
            {
                ShowSignUpFailModal("데이터 로드 실패");
                createNicknameInputField.text = "";
                createIdInputField.text = "";
                createpPasswordInputField.text = "";
                createPasswordConfirmInputField.text = "";
                createNicknameInputField.Select();
            }
            else if(signUpResult == 1)
            {
                ShowSignUpFailModal("이미 존재하는 아이디입니다.");
                createIdInputField.text = "";
                createIdInputField.Select();
            }
            else if(signUpResult == 2)
            {
                ShowSignUpFailModal("이미 존재하는 닉네임입니다.");
                createNicknameInputField.text = "";
                createNicknameInputField.Select();
            }
             // 회원가입 실패
            Debug.Log($"회원가입 실패: {signUpMessage}");
            // ShowSignUpFailModal(signUpMessage);
        }          
    }

    /// <summary>
    /// Intro 씬 로드 코루틴
    /// </summary>
    private IEnumerator LoadIntroScene()
    {
        Debug.Log("Intro 씬으로 전환 중...");
        
        // 페이드 아웃 효과 시작
        yield return StartCoroutine(FadeOut());
        
        // Lobby 씬 로드
        SceneManager.LoadScene("Lobby");
    }

    #endregion

    #region UI 관리 메서드

    /// <summary>
    /// 비밀번호 입력 필드 마스킹 설정
    /// </summary>
    private void SetupPasswordFields()
    {
        // 회원가입 비밀번호 필드
        if (createpPasswordInputField != null)
        {
            createpPasswordInputField.contentType = TMP_InputField.ContentType.Password;
            createpPasswordInputField.asteriskChar = '*';
            createpPasswordInputField.characterLimit = 20; // 비밀번호 최대 길이 제한
        }

        // 회원가입 비밀번호 확인 필드
        if (createPasswordConfirmInputField != null)
        {
            createPasswordConfirmInputField.contentType = TMP_InputField.ContentType.Password;
            createPasswordConfirmInputField.asteriskChar = '*';
            createPasswordConfirmInputField.characterLimit = 20; // 비밀번호 최대 길이 제한
        }

        // 로그인 비밀번호 필드
        if (loginPasswordInputField != null)
        {
            loginPasswordInputField.contentType = TMP_InputField.ContentType.Password;
            loginPasswordInputField.asteriskChar = '*';
            loginPasswordInputField.characterLimit = 20; // 비밀번호 최대 길이 제한
        }

        Debug.Log("LoginButtonController: 비밀번호 필드 마스킹 설정 완료");
    }

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
    /// 페이드 아웃 효과 코루틴
    /// </summary>
    private IEnumerator FadeOut()
    {
        if (fadeOverlay == null)
        {
            Debug.LogWarning("LoginButtonController: 페이드 오버레이가 설정되지 않았습니다!");
            yield return new WaitForSeconds(0.5f); // 기본 대기 시간
            yield break;
        }

        // 페이드 오버레이 활성화
        fadeOverlay.SetActive(true);

        // Image 컴포넌트 가져오기
        Image fadeImage = fadeOverlay.GetComponent<Image>();
        if (fadeImage == null)
        {
            Debug.LogError("LoginButtonController: 페이드 오버레이에 Image 컴포넌트가 없습니다!");
            yield return new WaitForSeconds(0.5f); // 기본 대기 시간
            yield break;
        }

        // 초기 알파값 설정 (투명)
        Color startColor = fadeImage.color;
        startColor.a = 0f;
        fadeImage.color = startColor;

        // 페이드 아웃 애니메이션 (투명 -> 불투명)
        float elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeOutDuration);
            
            Color currentColor = fadeImage.color;
            currentColor.a = alpha;
            fadeImage.color = currentColor;
            
            yield return null;
        }

        // 최종 알파값 설정 (완전 불투명)
        Color finalColor = fadeImage.color;
        finalColor.a = 1f;
        fadeImage.color = finalColor;

        Debug.Log("페이드 아웃 효과 완료");
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
        if (loginFailModalWindowManager != null )
        {
            loginFailModalWindowManager.descriptionText = message;
            loginFailModalWindowManager.UpdateUI();
            loginFailModalWindowManager.OpenWindow();
            ClearLoginInputFields();
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
            ClearSignUpInputFields();
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
        if (signUpSuccessModalWindowManager.isOn)
        {
            return;
        }

        if (signUpFailModalWindowManager != null && !signUpSuccessModalWindowManager.isOn)
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

    /// <summary>
    /// 아이디 유효성 검증 (영문+숫자 조합, 6글자 이상)
    /// </summary>
    /// <param name="userId">검증할 아이디</param>
    /// <returns>유효한 아이디인지 여부</returns>
    private bool IsValidUserId(string userId)
    {
        if (string.IsNullOrEmpty(userId) || userId.Length < 6)
            return false;

        // 영문과 숫자만 허용
        foreach (char c in userId)
        {
            if (!char.IsLetterOrDigit(c))
                return false;
        }

        // 최소 하나의 영문과 하나의 숫자가 포함되어야 함
        bool hasLetter = false;
        bool hasDigit = false;

        foreach (char c in userId)
        {
            if (char.IsLetter(c))
                hasLetter = true;
            else if (char.IsDigit(c))
                hasDigit = true;

            if (hasLetter && hasDigit)
                return true;
        }

        return false; // 영문과 숫자가 모두 포함되지 않음
    }

    /// <summary>
    /// 비밀번호 유효성 검증 (영문+숫자 조합)
    /// </summary>
    /// <param name="password">검증할 비밀번호</param>
    /// <returns>유효한 비밀번호인지 여부</returns>
    private bool IsValidPassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 4)
            return false;

        // 영문과 숫자만 허용
        foreach (char c in password)
        {
            if (!char.IsLetterOrDigit(c))
                return false;
        }

        // 최소 하나의 영문과 하나의 숫자가 포함되어야 함
        bool hasLetter = false;
        bool hasDigit = false;

        foreach (char c in password)
        {
            if (char.IsLetter(c))
                hasLetter = true;
            else if (char.IsDigit(c))
                hasDigit = true;

            if (hasLetter && hasDigit)
                return true;
        }

        return false; // 영문과 숫자가 모두 포함되지 않음
    }

    #endregion

    #region 디버그 및 유틸리티

    /// <summary>
    /// 현재 연결 상태 확인 (디버깅용)
    /// </summary>
    [ContextMenu("구글 스프레드시트 연결 테스트")]
    private void TestGoogleSheetsConnection()
    {
        if (GoogleSheetsManager.Instance != null)
        {
            bool isConnected = GoogleSheetsManager.Instance.IsConnected();
            Debug.Log($"구글 스프레드시트 연결 상태: {(isConnected ? "연결됨" : "연결 안됨")}");
            GoogleSheetsManager.Instance.DiagnoseConnection();
        }
        else
        {
            Debug.LogError("GoogleSheetsManager 인스턴스를 찾을 수 없습니다!");
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
