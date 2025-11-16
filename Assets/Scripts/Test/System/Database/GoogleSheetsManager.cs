#define BOUNCY_CASTLE_AVAILABLE
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.IO;

// BouncyCastle imports (DLL이 있는 경우에만 활성화)
#if BOUNCY_CASTLE_AVAILABLE
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
#endif

/// <summary>
/// 구글 시트 API를 서비스 계정으로 사용하는 사용자 데이터 관리 클래스
/// OAuth2 JWT 토큰 기반 인증 사용
/// </summary>
public class GoogleSheetsManager : MonoBehaviour
{
    // 구글 시트 업데이트 완료 이벤트
    public static event System.Action OnSheetsWriteSuccess;
    private static GoogleSheetsManager _instance;
    public static GoogleSheetsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GoogleSheetsManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GoogleSheetsManager");
                    _instance = go.AddComponent<GoogleSheetsManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("구글 시트 설정")]
    [SerializeField] private string spreadsheetId = ""; // 스프레드시트 ID
    [SerializeField] private string sheetName = "Sheet1"; // 시트 이름
    
    [Header("서비스 계정 인증")]
    [SerializeField] private string serviceAccountEmail = ""; // 서비스 계정 이메일
    [SerializeField] private string privateKeyId = ""; // 개인 키 ID
    [SerializeField] [TextArea(5, 10)] private string privateKey = ""; // 개인 키 (PEM 형식)
    
    // OAuth2 관련
    private string accessToken = "";
    private DateTime tokenExpiry = DateTime.MinValue;
    
    // 사용자 데이터 캐시
    private List<UserGameData> userDataCache = new List<UserGameData>();
    private bool isDataLoaded = false;
    private bool isConnected = false;
    
    // 구글 시트 API URLs (Money 컬럼 추가: A1:G1000)
    private string ReadURL => $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{sheetName}!A1:G1000";
    private string WriteURL => $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{sheetName}:append?valueInputOption=RAW&insertDataOption=INSERT_ROWS";

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGoogleSheets();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 구글 시트 초기화
    /// </summary>
    private void InitializeGoogleSheets()
    {
        Debug.Log("GoogleSheetsManager: 서비스 계정 연결 초기화");
        CheckConnection();
        
        if (isConnected)
        {
            LoadUserData();
        }
    }

    /// <summary>
    /// 연결 상태 확인
    /// </summary>
    private void CheckConnection()
    {
        if (string.IsNullOrEmpty(spreadsheetId))
        {
            Debug.LogError("GoogleSheetsManager: 스프레드시트 ID가 설정되지 않았습니다!");
            isConnected = false;
            return;
        }
        
        if (string.IsNullOrEmpty(serviceAccountEmail))
        {
            Debug.LogError("GoogleSheetsManager: 서비스 계정 이메일이 설정되지 않았습니다!");
            isConnected = false;
            return;
        }
        
        if (string.IsNullOrEmpty(privateKey))
        {
            Debug.LogError("GoogleSheetsManager: 개인 키가 설정되지 않았습니다!");
            isConnected = false;
            return;
        }
        
        isConnected = true;
        Debug.Log("GoogleSheetsManager: 서비스 계정 연결 준비 완료");
    }

    /// <summary>
    /// OAuth2 액세스 토큰 획득
    /// </summary>
    private IEnumerator GetAccessToken(System.Action<bool> callback)
    {
        // 토큰이 유효하면 재사용
        if (!string.IsNullOrEmpty(accessToken) && DateTime.UtcNow < tokenExpiry)
        {
            callback?.Invoke(true);
            yield break;
        }

        // JWT 토큰 생성
        string jwt = CreateJWT();
        if (string.IsNullOrEmpty(jwt))
        {
            Debug.LogError("GoogleSheetsManager: JWT 토큰 생성 실패");
            callback?.Invoke(false);
            yield break;
        }

        // Google OAuth2 토큰 요청
        WWWForm form = new WWWForm();
        form.AddField("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer");
        form.AddField("assertion", jwt);

        using (UnityWebRequest request = UnityWebRequest.Post("https://oauth2.googleapis.com/token", form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var tokenResponse = JsonUtility.FromJson<OAuth2TokenResponse>(request.downloadHandler.text);
                    accessToken = tokenResponse.access_token;
                    tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in - 60); // 1분 여유
                    
                    Debug.Log("GoogleSheetsManager: OAuth2 토큰 획득 성공");
                    callback?.Invoke(true);
                }
                catch (Exception e)
                {
                    Debug.LogError($"GoogleSheetsManager: 토큰 응답 파싱 실패 - {e.Message}");
                    callback?.Invoke(false);
                }
            }
            else
            {
                Debug.LogError($"GoogleSheetsManager: OAuth2 토큰 요청 실패 - {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                callback?.Invoke(false);
            }
        }
    }

    /// <summary>
    /// JWT 토큰 생성 (BouncyCastle 사용)
    /// </summary>
    private string CreateJWT()
    {
        try
        {
            #if BOUNCY_CASTLE_AVAILABLE
            return CreateJWTWithBouncyCastle();
            #else
            Debug.LogError("BouncyCastle 라이브러리가 없습니다. Assets/Plugins에 BouncyCastle.Crypto.dll을 추가해주세요.");
            return CreateJWTFallback();
            #endif
        }
        catch (Exception e)
        {
            Debug.LogError($"GoogleSheetsManager: JWT 생성 실패 - {e.Message}");
            return null;
        }
    }

    #if BOUNCY_CASTLE_AVAILABLE
    /// <summary>
    /// BouncyCastle을 사용한 JWT 생성
    /// </summary>
    private string CreateJWTWithBouncyCastle()
    {
        // JWT Header
        var header = new
        {
            alg = "RS256",
            typ = "JWT",
            kid = privateKeyId
        };

        // JWT Payload
        var payload = new
        {
            iss = serviceAccountEmail,
            scope = "https://www.googleapis.com/auth/spreadsheets https://www.googleapis.com/auth/drive.file",
            aud = "https://oauth2.googleapis.com/token",
            exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        string encodedHeader = Base64UrlEncode(JsonConvert.SerializeObject(header));
        string encodedPayload = Base64UrlEncode(JsonConvert.SerializeObject(payload));
        string message = $"{encodedHeader}.{encodedPayload}";

        // BouncyCastle로 RSA 서명
        string signature = SignWithBouncyCastle(message, privateKey);
        return $"{message}.{signature}";
    }

    /// <summary>
    /// BouncyCastle을 사용한 RSA 서명
    /// </summary>
    private string SignWithBouncyCastle(string message, string privateKeyPem)
    {
        try
        {
            // PEM 키 파싱
            AsymmetricKeyParameter privateKey;
            using (var reader = new StringReader(privateKeyPem.Replace("\\n", "\n")))
            {
                var pemReader = new PemReader(reader);
                privateKey = pemReader.ReadObject() as AsymmetricKeyParameter;
            }

            if (privateKey == null)
            {
                throw new Exception("PEM 키 파싱 실패");
            }

            // SHA256withRSA 서명
            var signer = SignerUtilities.GetSigner("SHA256withRSA");
            signer.Init(true, privateKey);

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            signer.BlockUpdate(messageBytes, 0, messageBytes.Length);
            byte[] signature = signer.GenerateSignature();

            return Base64UrlEncodeBytes(signature);
        }
        catch (Exception e)
        {
            Debug.LogError($"BouncyCastle RSA 서명 실패: {e.Message}");
            return null;
        }
    }
    #endif

    /// <summary>
    /// BouncyCastle 없이 사용할 수 있는 대안 (제한적)
    /// </summary>
    private string CreateJWTFallback()
    {
        Debug.LogError("JWT 생성을 위해 BouncyCastle.Crypto.dll이 필요합니다.");
        Debug.LogError("다음 단계를 따라주세요:");
        Debug.LogError("1. NuGet에서 BouncyCastle 패키지 다운로드");
        Debug.LogError("2. BouncyCastle.Crypto.dll을 Assets/Plugins 폴더에 복사");
        Debug.LogError("3. GoogleSheetsManager.cs 파일 상단에 #define BOUNCY_CASTLE_AVAILABLE 추가");
        return null;
    }

    /// <summary>
    /// Base64 URL 인코딩 (문자열용)
    /// </summary>
    private string Base64UrlEncode(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        return Base64UrlEncodeBytes(bytes);
    }

    /// <summary>
    /// Base64 URL 인코딩 (바이트 배열용)
    /// </summary>
    private string Base64UrlEncodeBytes(byte[] bytes)
    {
        string base64 = Convert.ToBase64String(bytes);
        return base64.Replace('+', '-').Replace('/', '_').Replace("=", "");
    }



    /// <summary>
    /// JWT 토큰 생성 테스트 (디버깅용)
    /// </summary>
    [ContextMenu("JWT 토큰 테스트")]
    public void TestJWTToken()
    {
        Debug.Log("=== JWT 토큰 생성 테스트 시작 ===");
        
        if (string.IsNullOrEmpty(serviceAccountEmail))
        {
            Debug.LogError("❌ 서비스 계정 이메일이 설정되지 않았습니다!");
            return;
        }
        
        if (string.IsNullOrEmpty(privateKey))
        {
            Debug.LogError("❌ 개인 키가 설정되지 않았습니다!");
            return;
        }
        
        Debug.Log($"🔍 JWT 토큰 생성 상세 분석:");
        Debug.Log($"- 서비스 계정 이메일: {serviceAccountEmail}");
        Debug.Log($"- 개인 키 ID: {privateKeyId}");
        Debug.Log($"- 개인 키 길이: {privateKey.Length} 문자");
        Debug.Log($"- 개인 키 시작: {privateKey.Substring(0, Math.Min(50, privateKey.Length))}...");
        
        string jwt = CreateJWT();
        
        if (!string.IsNullOrEmpty(jwt))
        {
            Debug.Log($"✅ JWT 토큰 생성 성공!");
            Debug.Log($"JWT 길이: {jwt.Length}");
            Debug.Log($"JWT 전체: {jwt}");
            
            // JWT 구조 분석
            string[] jwtParts = jwt.Split('.');
            if (jwtParts.Length == 3)
            {
                Debug.Log($"JWT 헤더: {jwtParts[0]}");
                Debug.Log($"JWT 페이로드: {jwtParts[1]}");
                Debug.Log($"JWT 서명: {jwtParts[2]}");
                
                try
                {
                    // 헤더 디코딩
                    string headerJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(AddBase64Padding(jwtParts[0].Replace('-', '+').Replace('_', '/'))));
                    Debug.Log($"디코딩된 헤더: {headerJson}");
                    
                    // 페이로드 디코딩
                    string payloadJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(AddBase64Padding(jwtParts[1].Replace('-', '+').Replace('_', '/'))));
                    Debug.Log($"디코딩된 페이로드: {payloadJson}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"JWT 디코딩 실패: {e.Message}");
                }
            }
            
            // OAuth2 토큰 요청 테스트
            StartCoroutine(TestOAuth2TokenRequest(jwt));
        }
        else
        {
            Debug.LogError("❌ JWT 토큰 생성 실패!");
        }
    }

    /// <summary>
    /// Base64 패딩 추가
    /// </summary>
    private string AddBase64Padding(string base64)
    {
        int padding = 4 - (base64.Length % 4);
        if (padding != 4)
        {
            base64 += new string('=', padding);
        }
        return base64;
    }

    /// <summary>
    /// 서비스 계정 키 검증 (디버깅용)
    /// </summary>
    [ContextMenu("서비스 계정 키 검증")]
    public void ValidateServiceAccountKey()
    {
        Debug.Log("=== 서비스 계정 키 검증 시작 ===");
        
        Debug.Log($"🔍 현재 설정 검증:");
        Debug.Log($"- 서비스 계정 이메일: '{serviceAccountEmail}'");
        Debug.Log($"- 서비스 계정 이메일 길이: {serviceAccountEmail?.Length ?? 0}");
        Debug.Log($"- 서비스 계정 이메일 형식: {(serviceAccountEmail?.Contains("@") == true ? "올바름" : "잘못됨")}");
        
        Debug.Log($"- 개인 키 ID: '{privateKeyId}'");
        Debug.Log($"- 개인 키 ID 길이: {privateKeyId?.Length ?? 0}");
        
        Debug.Log($"- 개인 키 길이: {privateKey?.Length ?? 0} 문자");
        
        if (!string.IsNullOrEmpty(privateKey))
        {
            Debug.Log($"- 개인 키 시작: '{privateKey.Substring(0, Math.Min(30, privateKey.Length))}'");
            Debug.Log($"- 개인 키 끝: '{privateKey.Substring(Math.Max(0, privateKey.Length - 30))}'");
            
            // PEM 헤더/푸터 확인
            bool hasBeginHeader = privateKey.Contains("-----BEGIN PRIVATE KEY-----");
            bool hasEndFooter = privateKey.Contains("-----END PRIVATE KEY-----");
            Debug.Log($"- PEM 시작 헤더 존재: {hasBeginHeader}");
            Debug.Log($"- PEM 끝 푸터 존재: {hasEndFooter}");
            
            // 이스케이프된 개행 문자 확인
            bool hasEscapedNewlines = privateKey.Contains("\\n");
            Debug.Log($"- 이스케이프된 개행 문자(\\n) 존재: {hasEscapedNewlines}");
            
            if (hasEscapedNewlines)
            {
                Debug.LogWarning("⚠️ 개인 키에 이스케이프된 개행 문자가 있습니다. 이는 정상입니다.");
            }
            
            // Base64 부분 추출 시도
            try
            {
                string keyText = privateKey
                    .Replace("\\n", "\n")
                    .Replace("-----BEGIN PRIVATE KEY-----", "")
                    .Replace("-----END PRIVATE KEY-----", "")
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace(" ", "");
                
                Debug.Log($"- 정리된 Base64 키 길이: {keyText.Length}");
                Debug.Log($"- Base64 키 시작: {keyText.Substring(0, Math.Min(50, keyText.Length))}...");
                
                // Base64 디코딩 테스트
                byte[] keyBytes = Convert.FromBase64String(keyText);
                Debug.Log($"✅ Base64 디코딩 성공! 키 바이트 길이: {keyBytes.Length}");
                
                // PKCS#8 헤더 확인 (간단한 검증)
                if (keyBytes.Length > 10)
                {
                    Debug.Log($"- 키 바이트 시작: [{string.Join(", ", keyBytes.Take(10).Select(b => b.ToString("X2")))}]");
                    
                    // PKCS#8 private key는 0x30으로 시작해야 함
                    if (keyBytes[0] == 0x30)
                    {
                        Debug.Log("✅ PKCS#8 형식으로 보입니다.");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ PKCS#8 형식이 아닐 수 있습니다. 첫 바이트: 0x{keyBytes[0]:X2}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Base64 디코딩 실패: {e.Message}");
                Debug.LogError("개인 키가 올바른 Base64 형식이 아닙니다.");
            }
        }
        
        // 시간 검증
        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Debug.Log($"- 현재 Unix 시간: {currentTime}");
        Debug.Log($"- 현재 UTC 시간: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        
        Debug.Log("=== 서비스 계정 키 검증 완료 ===");
    }

    /// <summary>
    /// 전체 인증 플로우 테스트 (디버깅용)
    /// </summary>
    [ContextMenu("전체 인증 플로우 테스트")]
    public void TestFullAuthFlow()
    {
        Debug.Log("=== 전체 인증 플로우 테스트 시작 ===");
        StartCoroutine(FullAuthFlowTest());
    }

    /// <summary>
    /// 전체 인증 플로우 테스트 코루틴
    /// </summary>
    private IEnumerator FullAuthFlowTest()
    {
        Debug.Log("1️⃣ JWT 토큰 생성 중...");
        
        string jwt = CreateJWT();
        if (string.IsNullOrEmpty(jwt))
        {
            Debug.LogError("❌ JWT 토큰 생성 실패!");
            yield break;
        }
        
        Debug.Log($"✅ JWT 토큰 생성 성공! 길이: {jwt.Length}");
        Debug.Log($"JWT: {jwt}");
        
        Debug.Log("2️⃣ OAuth2 토큰 요청 중...");
        
        WWWForm form = new WWWForm();
        form.AddField("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer");
        form.AddField("assertion", jwt);

        using (UnityWebRequest tokenRequest = UnityWebRequest.Post("https://oauth2.googleapis.com/token", form))
        {
            yield return tokenRequest.SendWebRequest();

            if (tokenRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ OAuth2 토큰 요청 실패!");
                Debug.LogError($"Response Code: {tokenRequest.responseCode}");
                Debug.LogError($"Error: {tokenRequest.downloadHandler.text}");
                yield break;
            }

            Debug.Log("✅ OAuth2 토큰 요청 성공!");
            
            var tokenResponse = JsonUtility.FromJson<OAuth2TokenResponse>(tokenRequest.downloadHandler.text);
            string accessToken = tokenResponse.access_token;
            
            Debug.Log($"Access Token 길이: {accessToken.Length}");
            Debug.Log($"Access Token: {accessToken}");
            
            Debug.Log("3️⃣ Google Sheets 읽기 테스트 중...");
            
            string readUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{sheetName}!A1:F10";
            Debug.Log($"Read URL: {readUrl}");
            
            using (UnityWebRequest readRequest = UnityWebRequest.Get(readUrl))
            {
                readRequest.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                
                yield return readRequest.SendWebRequest();
                
                Debug.Log($"읽기 응답 코드: {readRequest.responseCode}");
                Debug.Log($"읽기 응답: {readRequest.downloadHandler.text}");
                
                if (readRequest.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("✅ Google Sheets 읽기 성공!");
                }
                else
                {
                    Debug.LogError("❌ Google Sheets 읽기 실패!");
                    
                    if (readRequest.responseCode == 403)
                    {
                        Debug.LogError("🔐 권한 문제 분석:");
                        Debug.LogError("1. Google Sheets에서 서비스 계정 공유 확인");
                        Debug.LogError($"   서비스 계정: {serviceAccountEmail}");
                        Debug.LogError("   권한: 편집자 또는 뷰어");
                        Debug.LogError("2. Google Cloud Console에서 Sheets API 활성화 확인");
                        Debug.LogError("3. 스프레드시트가 올바른지 확인");
                        Debug.LogError($"   스프레드시트 ID: {spreadsheetId}");
                    }
                }
            }
            
            Debug.Log("4️⃣ Google Sheets 쓰기 테스트 중...");
            
            string writeUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{sheetName}:append?valueInputOption=RAW&insertDataOption=INSERT_ROWS";
            Debug.Log($"Write URL: {writeUrl}");
            
            // JsonUtility 대신 수동으로 JSON 생성
            string testId = "test_" + System.DateTime.Now.Ticks;
            string jsonData = $@"{{
                ""values"": [
                    [""{testId}"", ""testpass"", ""테스트"", ""0"", ""0"", ""1000"", ""0""]
                ]
            }}";
            
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            
            Debug.Log($"쓰기 데이터: {jsonData}");
            
            using (UnityWebRequest writeRequest = new UnityWebRequest(writeUrl, "POST"))
            {
                writeRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                writeRequest.downloadHandler = new DownloadHandlerBuffer();
                writeRequest.SetRequestHeader("Content-Type", "application/json");
                writeRequest.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                
                yield return writeRequest.SendWebRequest();
                
                Debug.Log($"쓰기 응답 코드: {writeRequest.responseCode}");
                Debug.Log($"쓰기 응답: {writeRequest.downloadHandler.text}");
                
                if (writeRequest.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("✅ Google Sheets 쓰기 성공!");
                    Debug.Log("🎉 전체 인증 플로우 테스트 완료! 모든 기능이 정상 작동합니다.");
                }
                else
                {
                    Debug.LogError("❌ Google Sheets 쓰기 실패!");
                    
                    if (writeRequest.responseCode == 403)
                    {
                        Debug.LogError("🔐 쓰기 권한 문제:");
                        Debug.LogError("Google Sheets에서 서비스 계정에 '편집자' 권한이 필요합니다.");
                    }
                }
            }
        }
        
        Debug.Log("=== 전체 인증 플로우 테스트 완료 ===");
    }

    /// <summary>
    /// OAuth2 토큰 요청 테스트
    /// </summary>
    private IEnumerator TestOAuth2TokenRequest(string jwt)
    {
        Debug.Log("OAuth2 토큰 요청 테스트 시작...");
        
        WWWForm form = new WWWForm();
        form.AddField("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer");
        form.AddField("assertion", jwt);

        using (UnityWebRequest request = UnityWebRequest.Post("https://oauth2.googleapis.com/token", form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ OAuth2 토큰 요청 성공!");
                Debug.Log($"Response: {request.downloadHandler.text}");
                
                try
                {
                    var tokenResponse = JsonUtility.FromJson<OAuth2TokenResponse>(request.downloadHandler.text);
                    Debug.Log($"Access Token 길이: {tokenResponse.access_token?.Length}");
                    Debug.Log($"Token Type: {tokenResponse.token_type}");
                    Debug.Log($"Expires In: {tokenResponse.expires_in}");
                    
                    // Google Sheets API 요청 테스트
                    if (!string.IsNullOrEmpty(tokenResponse.access_token))
                    {
                        StartCoroutine(TestGoogleSheetsAPI(tokenResponse.access_token));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"토큰 응답 파싱 실패: {e.Message}");
                }
            }
            else
            {
                Debug.LogError("❌ OAuth2 토큰 요청 실패!");
                Debug.LogError($"HTTP Error: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }
        }
        
        Debug.Log("=== JWT 토큰 테스트 완료 ===");
    }

    /// <summary>
    /// Google Sheets API 요청 테스트
    /// </summary>
    private IEnumerator TestGoogleSheetsAPI(string accessToken)
    {
        Debug.Log("=== Google Sheets API 테스트 시작 ===");
        Debug.Log($"스프레드시트 ID: {spreadsheetId}");
        Debug.Log($"시트 이름: {sheetName}");
        
        string testUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{sheetName}!A1:F1000";
        Debug.Log($"요청 URL: {testUrl}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(testUrl))
        {
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Google Sheets API 요청 성공!");
                Debug.Log($"Response: {request.downloadHandler.text}");
                
                try
                {
                    var sheetsResponse = JsonUtility.FromJson<GoogleSheetsResponse>(request.downloadHandler.text);
                    if (sheetsResponse != null && sheetsResponse.values != null)
                    {
                        Debug.Log($"데이터 행 수: {sheetsResponse.values.Length}");
                        if (sheetsResponse.values.Length > 0)
                        {
                            Debug.Log($"첫 번째 행: {string.Join(", ", sheetsResponse.values[0])}");
                        }
                    }
                    else
                    {
                        Debug.Log("시트가 비어있거나 데이터가 없습니다.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Sheets 응답 파싱 실패: {e.Message}");
                    Debug.LogError($"Raw Response: {request.downloadHandler.text}");
                }
            }
            else
            {
                Debug.LogError("❌ Google Sheets API 요청 실패!");
                Debug.LogError($"HTTP Error: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                Debug.LogError($"Request URL: {testUrl}");
                
                // 400 오류 상세 분석
                if (request.responseCode == 400)
                {
                    Debug.LogError("🔍 400 Bad Request 분석:");
                    Debug.LogError("- 시트 이름이 잘못되었을 가능성이 높습니다.");
                    Debug.LogError($"- 현재 시트 이름: '{sheetName}'");
                    Debug.LogError("- Google Sheets에서 실제 시트 이름을 확인하세요.");
                    Debug.LogError("- 시트 이름에 특수문자나 공백이 있다면 정확히 입력해야 합니다.");
                }
                
                // 일반적인 오류 원인 분석
                if (request.responseCode == 403)
                {
                    Debug.LogError("🔐 권한 오류: 서비스 계정이 스프레드시트에 접근할 권한이 없습니다.");
                    Debug.LogError($"해결 방법: Google Sheets에서 '{serviceAccountEmail}' 이메일에 편집자 권한을 부여하세요.");
                }
                else if (request.responseCode == 404)
                {
                    Debug.LogError("📄 리소스 오류: 스프레드시트 ID 또는 시트 이름이 잘못되었습니다.");
                    Debug.LogError($"스프레드시트 ID 확인: {spreadsheetId}");
                    Debug.LogError($"시트 이름 확인: {sheetName}");
                }
            }
        }
        
        Debug.Log("=== Google Sheets API 테스트 완료 ===");
    }

    /// <summary>
    /// 기본 시트 이름으로 테스트 (디버깅용)
    /// </summary>
    [ContextMenu("기본 시트 테스트")]
    public void TestWithDefaultSheet()
    {
        Debug.Log("=== 기본 시트 이름 테스트 시작 ===");
        
        if (string.IsNullOrEmpty(spreadsheetId))
        {
            Debug.LogError("❌ 스프레드시트 ID가 설정되지 않았습니다!");
            return;
        }
        
        // 일반적인 기본 시트 이름들로 테스트
        string[] commonSheetNames = { "Sheet1", "시트1", "LoginData", "UserData", "Data" };
        
        Debug.Log($"스프레드시트 ID: {spreadsheetId}");
        Debug.Log($"현재 설정된 시트 이름: '{sheetName}'");
        Debug.Log("다음 기본 시트 이름들로 테스트를 시도합니다:");
        
        foreach (string testSheetName in commonSheetNames)
        {
            Debug.Log($"- {testSheetName}");
        }
        
        StartCoroutine(TestMultipleSheetNames(commonSheetNames));
    }

    /// <summary>
    /// 여러 시트 이름으로 순차 테스트
    /// </summary>
    private IEnumerator TestMultipleSheetNames(string[] sheetNames)
    {
        // 먼저 JWT 토큰 얻기
        string accessToken = null;
        bool tokenCompleted = false;
        
        StartCoroutine(GetAccessTokenForTest((token) =>
        {
            accessToken = token;
            tokenCompleted = true;
        }));
        
        yield return new WaitUntil(() => tokenCompleted);
        
        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.LogError("❌ 액세스 토큰 획득 실패!");
            yield break;
        }
        
        Debug.Log("✅ 액세스 토큰 획득 성공! 시트 이름 테스트 시작...");
        
        foreach (string testSheetName in sheetNames)
        {
            Debug.Log($"🔍 '{testSheetName}' 시트 테스트 중...");
            
            string testUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{testSheetName}!A1:F10";
            
            using (UnityWebRequest request = UnityWebRequest.Get(testUrl))
            {
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"✅ '{testSheetName}' 시트 발견!");
                    Debug.Log($"Response: {request.downloadHandler.text}");
                    
                    // 성공한 시트 이름을 현재 설정으로 제안
                    Debug.Log($"💡 제안: Unity Inspector에서 Sheet Name을 '{testSheetName}'로 변경하세요.");
                    break;
                }
                else
                {
                    Debug.Log($"❌ '{testSheetName}' 시트 없음 (Code: {request.responseCode})");
                }
            }
            
            yield return new WaitForSeconds(0.5f); // API 요청 간격
        }
        
        Debug.Log("=== 기본 시트 이름 테스트 완료 ===");
    }

    /// <summary>
    /// 데이터 쓰기 테스트 (디버깅용)
    /// </summary>
    [ContextMenu("데이터 쓰기 테스트")]
    public void TestDataWrite()
    {
        Debug.Log("=== 데이터 쓰기 테스트 시작 ===");
        
        if (string.IsNullOrEmpty(spreadsheetId))
        {
            Debug.LogError("❌ 스프레드시트 ID가 설정되지 않았습니다!");
            return;
        }
        
        if (string.IsNullOrEmpty(sheetName))
        {
            Debug.LogError("❌ 시트 이름이 설정되지 않았습니다!");
            return;
        }
        
        // 테스트 데이터로 사용자 등록 시도
        string testUserId = "testuser" + System.DateTime.Now.Ticks.ToString().Substring(10);
        string testNickname = "테스트유저";
        string testPassword = "1234";
        
        Debug.Log($"테스트 사용자 정보:");
        Debug.Log($"- ID: {testUserId}");
        Debug.Log($"- Nickname: {testNickname}");
        Debug.Log($"- Password: {testPassword}");
        
        RegisterUser(testUserId, testNickname, testPassword, (success, message, result) =>
        {
            if (success)
            {
                Debug.Log($"✅ 데이터 쓰기 테스트 성공: {message}");
                Debug.Log("📖 데이터 읽기 테스트를 시작합니다...");
                
                // 잠시 후 데이터 읽기 테스트
                StartCoroutine(DelayedReadTest());
            }
            else
            {
                Debug.LogError($"❌ 데이터 쓰기 테스트 실패: {message}");
            }
        });
    }

    /// <summary>
    /// 지연된 데이터 읽기 테스트
    /// </summary>
    private IEnumerator DelayedReadTest()
    {
        yield return new WaitForSeconds(2f); // 2초 대기
        
        Debug.Log("📖 데이터 읽기 테스트 실행 중...");
        
        // 캐시 초기화
        userDataCache.Clear();
        isDataLoaded = false;
        
        // 데이터 다시 로드
        LoadUserData();
    }

    /// <summary>
    /// 데이터 읽기 테스트 (디버깅용)
    /// </summary>
    [ContextMenu("데이터 읽기 테스트")]
    public void TestDataRead()
    {
        Debug.Log("=== 데이터 읽기 테스트 시작 ===");
        
        if (string.IsNullOrEmpty(spreadsheetId))
        {
            Debug.LogError("❌ 스프레드시트 ID가 설정되지 않았습니다!");
            return;
        }
        
        if (string.IsNullOrEmpty(sheetName))
        {
            Debug.LogError("❌ 시트 이름이 설정되지 않았습니다!");
            return;
        }
        
        // 캐시 초기화
        userDataCache.Clear();
        isDataLoaded = false;
        
        Debug.Log($"스프레드시트 ID: {spreadsheetId}");
        Debug.Log($"시트 이름: {sheetName}");
        Debug.Log($"Read URL: {ReadURL}");
        
        // 데이터 로드
        LoadUserData();
    }

    /// <summary>
    /// 테스트용 액세스 토큰 획득
    /// </summary>
    private IEnumerator GetAccessTokenForTest(System.Action<string> callback)
    {
        string jwt = CreateJWT();
        if (string.IsNullOrEmpty(jwt))
        {
            callback?.Invoke(null);
            yield break;
        }
        
        WWWForm form = new WWWForm();
        form.AddField("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer");
        form.AddField("assertion", jwt);

        using (UnityWebRequest request = UnityWebRequest.Post("https://oauth2.googleapis.com/token", form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var tokenResponse = JsonUtility.FromJson<OAuth2TokenResponse>(request.downloadHandler.text);
                    callback?.Invoke(tokenResponse.access_token);
                }
                catch
                {
                    callback?.Invoke(null);
                }
            }
            else
            {
                callback?.Invoke(null);
            }
        }
    }

    /// <summary>
    /// 사용자 데이터 로드
    /// </summary>
    public void LoadUserData()
    {
        if (!isConnected) return;
        StartCoroutine(LoadUserDataCoroutine());
    }

    /// <summary>
    /// 사용자 데이터 로드 코루틴
    /// </summary>
    private IEnumerator LoadUserDataCoroutine()
    {
        // 먼저 액세스 토큰 획득
        bool tokenSuccess = false;
        yield return StartCoroutine(GetAccessToken(success => tokenSuccess = success));
        
        if (!tokenSuccess)
        {
            Debug.LogError("GoogleSheetsManager: 액세스 토큰 획득 실패");
            yield break;
        }

        // 데이터 읽기 요청
        Debug.Log($"GoogleSheetsManager: 데이터 읽기 요청 시작");
        Debug.Log($"Read URL: {ReadURL}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(ReadURL))
        {
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            yield return request.SendWebRequest();

            Debug.Log($"GoogleSheetsManager: 데이터 읽기 응답 수신");
            Debug.Log($"Response Code: {request.responseCode}");
            Debug.Log($"Response Length: {request.downloadHandler.text?.Length ?? 0}");
            Debug.Log($"Raw Response: {request.downloadHandler.text}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    ParseUserData(request.downloadHandler.text);
                    isDataLoaded = true;
                    Debug.Log($"GoogleSheetsManager: 사용자 데이터 로드 성공 - {userDataCache.Count}명");
                }
                catch (Exception e)
                {
                    Debug.LogError($"GoogleSheetsManager: 데이터 파싱 실패 - {e.Message}");
                    Debug.LogError($"Stack Trace: {e.StackTrace}");
                    isDataLoaded = false;
                }
            }
            else
            {
                Debug.LogError($"GoogleSheetsManager: 데이터 로드 실패");
                Debug.LogError($"HTTP Error: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response Text: {request.downloadHandler.text}");
                isDataLoaded = false;
            }
        }
    }

    /// <summary>
    /// JSON 응답에서 사용자 데이터 파싱
    /// </summary>
    private void ParseUserData(string jsonResponse)
    {
        Debug.Log($"GoogleSheetsManager: 데이터 파싱 시작");
        Debug.Log($"JSON Response 길이: {jsonResponse?.Length ?? 0}");
        
        var response = JsonConvert.DeserializeObject<GoogleSheetsResponse>(jsonResponse);
        userDataCache.Clear();

        if (response == null)
        {
            Debug.LogError("GoogleSheetsManager: JSON 응답을 파싱할 수 없습니다.");
            return;
        }

        if (response.values == null || response.values.Length == 0)
        {
            Debug.Log("GoogleSheetsManager: 시트가 비어있습니다 (values가 null이거나 길이가 0).");
            return;
        }

        Debug.Log($"GoogleSheetsManager: 총 {response.values.Length}개 행 발견");
        
        // 첫 번째 행 헤더 확인
        if (response.values.Length > 0)
        {
            Debug.Log($"첫 번째 행 (헤더): {string.Join(", ", response.values[0])}");
        }

        // 첫 번째 행은 헤더이므로 건너뛰기
        for (int i = 1; i < response.values.Length; i++)
        {
            var row = response.values[i];
            Debug.Log($"행 {i}: [{string.Join(", ", row)}] (길이: {row.Length})");
            
            if (row.Length >= 6) // ID, Password, Nickname, Win, Lose, Rate (Money는 선택적)
            {
                try
                {
                    UserGameData userData = new UserGameData
                    {
                        id = i, // 행 번호를 ID로 사용
                        userId = row[0],
                        password = row[1],
                        nickname = row[2],
                        win = int.Parse(row[3]),
                        lose = int.Parse(row[4]),
                        rate = int.Parse(row[5]),
                        money = row.Length >= 7 ? int.Parse(row[6]) : 0 // Money 컬럼 (없으면 0)
                    };
                    userDataCache.Add(userData);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"GoogleSheetsManager: {i+1}행 데이터 파싱 실패 - {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 사용자 로그인
    /// </summary>
    public void LoginUser(string userId, string password, System.Action<bool, string, UserGameData> callback)
    {
        StartCoroutine(LoginUserCoroutine(userId, password, callback));
    }

    /// <summary>
    /// 로그인 처리 코루틴
    /// </summary>
    private IEnumerator LoginUserCoroutine(string userId, string password, System.Action<bool, string, UserGameData> callback)
    {
        if (!isDataLoaded)
        {
            yield return StartCoroutine(LoadUserDataCoroutine());
        }

        if (!isDataLoaded)
        {
            callback?.Invoke(false, "데이터 로드 실패", null);
            yield break;
        }

        var user = userDataCache.FirstOrDefault(u => u.userId == userId && u.password == password);
        if (user != null)
        {
            callback?.Invoke(true, "로그인 성공", user);
        }
        else
        {
            callback?.Invoke(false, "아이디 또는 비밀번호가 잘못되었습니다.", null);
        }
    }

    /// <summary>
    /// 사용자 등록
    /// </summary>
    public void RegisterUser(string userId, string nickname, string password, System.Action<bool, string, int> callback)
    {
        StartCoroutine(RegisterUserCoroutine(userId, nickname, password, callback));
    }

    /// <summary>
    /// 회원가입 처리 코루틴
    /// </summary>
    private IEnumerator RegisterUserCoroutine(string userId, string nickname, string password, System.Action<bool, string, int> callback)
    {
        if (!isDataLoaded)
        {
            yield return StartCoroutine(LoadUserDataCoroutine());
        }

        if (!isDataLoaded)
        {
            callback?.Invoke(false, "데이터 로드 실패", 0);
            yield break;
        }

        // 아이디 중복 확인
        if (userDataCache.Any(u => u.userId == userId))
        {
            callback?.Invoke(false, "이미 존재하는 아이디입니다.", 1);
            yield break;
        }

        // 닉네임 중복 확인
        if (userDataCache.Any(u => u.nickname == nickname))
        {
            callback?.Invoke(false, "이미 존재하는 닉네임입니다.", 2);
            yield break;
        }

        // 새 사용자 데이터를 시트에 추가
        yield return StartCoroutine(AddUserToSheet(userId, nickname, password, callback));
    }

    /// <summary>
    /// 시트에 새 사용자 추가
    /// </summary>
    private IEnumerator AddUserToSheet(string userId, string nickname, string password, System.Action<bool, string, int> callback)
    {
        // 먼저 액세스 토큰 확인
        bool tokenSuccess = false;
        yield return StartCoroutine(GetAccessToken(success => tokenSuccess = success));
        
        if (!tokenSuccess)
        {
            callback?.Invoke(false, "인증 실패", 3);
            yield break;
        }

        // 새 행 데이터 준비 - JsonUtility 대신 수동으로 JSON 생성 (Money 컬럼 추가)
        string jsonData = $@"{{
            ""values"": [
                [""{userId}"", ""{password}"", ""{nickname}"", ""0"", ""0"", ""1000"", ""0""]
            ]
        }}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        
        using (UnityWebRequest request = new UnityWebRequest(WriteURL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                // 캐시에 새 사용자 추가
                UserGameData newUser = new UserGameData
                {
                    id = userDataCache.Count + 1,
                    userId = userId,
                    password = password,
                    nickname = nickname,
                    win = 0,
                    lose = 0,
                    rate = 1000,
                    money = 0
                };
                userDataCache.Add(newUser);
                
                Debug.Log($"GoogleSheetsManager: 사용자 등록 성공 - {userId}");
                
                // 구글 시트 업데이트 완료 이벤트 발생
                OnSheetsWriteSuccess?.Invoke();
                
                callback?.Invoke(true, "회원가입이 완료되었습니다.",-1);
            }
            else
            {
                Debug.LogError($"❌ GoogleSheetsManager: 사용자 등록 실패");
                Debug.LogError($"HTTP Error: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                Debug.LogError($"Request URL: {WriteURL}");
                Debug.LogError($"Request Data: {jsonData}");
                
                // 일반적인 오류 원인 분석
                if (request.responseCode == 403)
                {
                    Debug.LogError("🔐 권한 오류: 서비스 계정이 스프레드시트 쓰기 권한이 없습니다.");
                    Debug.LogError($"해결 방법: Google Sheets에서 '{serviceAccountEmail}' 이메일에 편집자 권한을 부여하세요.");
                }
                else if (request.responseCode == 400)
                {
                    Debug.LogError("📝 요청 오류: 데이터 형식이나 시트 구조에 문제가 있습니다.");
                    Debug.LogError("해결 방법: 시트의 첫 번째 행에 'ID, Password, Nickname, Win, Lose, Rate' 헤더가 있는지 확인하세요.");
                }
                
                callback?.Invoke(false, "회원가입 중 오류가 발생했습니다: " + request.error, 4);
            }
        }
    }

    /// <summary>
    /// 게임 결과 업데이트 (재화 포함)
    /// </summary>
    public void UpdateGameResult(string userId, int rank, int moneyReward, System.Action<bool, string> callback)
    {
        StartCoroutine(UpdateGameResultCoroutine(userId, rank, moneyReward, callback));
    }

    /// <summary>
    /// 게임 결과 업데이트 코루틴 (재화 포함)
    /// </summary>
    private IEnumerator UpdateGameResultCoroutine(string userId, int rank, int moneyReward, System.Action<bool, string> callback)
    {
        var user = userDataCache.FirstOrDefault(u => u.userId == userId);
        if (user == null)
        {
            callback?.Invoke(false, "사용자를 찾을 수 없습니다.");
            yield break;
        }

        Debug.Log($"[GoogleSheetsManager] UpdateGameResultCoroutine 시작 - User: {userId}, 기존 Money: {user.money}, 보상: {moneyReward}");

        // 랭크에 따른 스탯 업데이트
        if (rank == 1)
        {
            user.win++;
            user.rate += 14;
        }
        else
        {
            user.lose++;
            switch (rank)
            {
                case 2: user.rate += 6; break;
                case 3: user.rate += 0; break;
                case 4: user.rate -= 9; break;
            }
        }

        // Rate가 0 아래로 내려가지 않도록
        if (user.rate < 0) user.rate = 0;

        // 재화 추가 (구글 시트 캐시용 - 직접 할당)
        int oldMoney = user.money;
        user.money += moneyReward;
        Debug.Log($"[GoogleSheetsManager] 재화 업데이트 - {oldMoney} + {moneyReward} = {user.money}");

        // 시트 업데이트
        yield return StartCoroutine(UpdateUserInSheet(user, callback));
    }

    /// <summary>
    /// 시트에서 사용자 데이터 업데이트
    /// </summary>
    private IEnumerator UpdateUserInSheet(UserGameData user, System.Action<bool, string> callback)
    {
        // 먼저 액세스 토큰 확인
        bool tokenSuccess = false;
        yield return StartCoroutine(GetAccessToken(success => tokenSuccess = success));
        
        if (!tokenSuccess)
        {
            callback?.Invoke(false, "인증 실패");
            yield break;
        }

        // 업데이트할 행 범위 설정 (A{row}:G{row}) - Money 컬럼 추가
        string updateRange = $"{sheetName}!A{user.id + 1}:G{user.id + 1}";
        string updateURL = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{updateRange}?valueInputOption=RAW";

        // 업데이트 데이터 준비 - JsonUtility 대신 수동으로 JSON 생성 (Money 컬럼 추가)
        string jsonData = $@"{{
            ""values"": [
                [""{user.userId}"", ""{user.password}"", ""{user.nickname}"", ""{user.win}"", ""{user.lose}"", ""{user.rate}"", ""{user.money}""]
            ]
        }}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(updateURL, "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"GoogleSheetsManager: 사용자 데이터 업데이트 성공 - {user.userId}");
                
                // 구글 시트 업데이트 완료 이벤트 발생
                OnSheetsWriteSuccess?.Invoke();
                
                callback?.Invoke(true, "게임 결과가 업데이트되었습니다.");
            }
            else
            {
                Debug.LogError($"GoogleSheetsManager: 사용자 데이터 업데이트 실패 - {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                callback?.Invoke(false, "데이터 업데이트 중 오류가 발생했습니다.");
            }
        }
    }

    /// <summary>
    /// 연결 상태 확인
    /// </summary>
    public bool IsConnected()
    {
        return isConnected;
    }

    /// <summary>
    /// 모든 사용자 데이터 반환 (리더보드용)
    /// </summary>
    public List<UserGameData> GetAllUserData()
    {
        return new List<UserGameData>(userDataCache);
    }

    /// <summary>
    /// 데이터 로드 상태 확인
    /// </summary>
    public bool IsDataLoaded()
    {
        return isDataLoaded;
    }

    /// <summary>
    /// 사용자 수 반환
    /// </summary>
    public int GetUserCount()
    {
        return userDataCache.Count;
    }

    /// <summary>
    /// 아이디 중복 확인
    /// </summary>
    /// <param name="userId">확인할 아이디</param>
    /// <returns>중복 여부 (true: 중복됨, false: 사용 가능)</returns>
    public bool IsUserIdDuplicate(string userId)
    {
        if (string.IsNullOrEmpty(userId) || !isDataLoaded)
            return false;

        return userDataCache.Any(u => u.userId == userId);
    }

    /// <summary>
    /// 닉네임 중복 확인
    /// </summary>
    /// <param name="nickname">확인할 닉네임</param>
    /// <returns>중복 여부 (true: 중복됨, false: 사용 가능)</returns>
    public bool IsNicknameDuplicate(string nickname)
    {
        if (string.IsNullOrEmpty(nickname) || !isDataLoaded)
            return false;

        return userDataCache.Any(u => u.nickname == nickname);
    }

    /// <summary>
    /// 연결 상태 진단
    /// </summary>
    [ContextMenu("연결 진단")]
    public void DiagnoseConnection()
    {
        Debug.Log("=== 구글 시트 서비스 계정 연결 진단 ===");
        Debug.Log($"스프레드시트 ID: {(string.IsNullOrEmpty(spreadsheetId) ? "❌ 설정되지 않음" : "✅ 설정됨")}");
        Debug.Log($"서비스 계정 이메일: {(string.IsNullOrEmpty(serviceAccountEmail) ? "❌ 설정되지 않음" : "✅ 설정됨")}");
        Debug.Log($"개인 키 ID: {(string.IsNullOrEmpty(privateKeyId) ? "❌ 설정되지 않음" : "✅ 설정됨")}");
        Debug.Log($"개인 키: {(string.IsNullOrEmpty(privateKey) ? "❌ 설정되지 않음" : $"✅ 설정됨 (길이: {privateKey.Length})")}");
        Debug.Log($"시트 이름: {sheetName}");
        Debug.Log($"연결 상태: {(isConnected ? "✅ 연결됨" : "❌ 연결 안됨")}");
        Debug.Log($"데이터 로드 상태: {(isDataLoaded ? "✅ 로드됨" : "❌ 로드 안됨")}");
        Debug.Log($"캐시된 사용자 수: {userDataCache.Count}");
        Debug.Log($"액세스 토큰: {(string.IsNullOrEmpty(accessToken) ? "❌ 없음" : "✅ 있음")}");
        Debug.Log($"토큰 만료: {tokenExpiry}");
        
        // Unity 환경 설정 확인
        Debug.Log("=== Unity 환경 설정 확인 ===");
        #if UNITY_EDITOR
        Debug.Log($"Unity 버전: {UnityEngine.Application.unityVersion}");
        Debug.Log($"플랫폼: {UnityEditor.EditorUserBuildSettings.activeBuildTarget}");
        Debug.Log($"API 호환성 레벨: {UnityEditor.PlayerSettings.GetApiCompatibilityLevel(UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup)}");
        #endif
        
        // 개인 키 형식 간단 검사
        if (!string.IsNullOrEmpty(privateKey))
        {
            bool hasBeginHeader = privateKey.Contains("-----BEGIN PRIVATE KEY-----");
            bool hasEndHeader = privateKey.Contains("-----END PRIVATE KEY-----");
            bool hasNewlines = privateKey.Contains("\\n") || privateKey.Contains("\n");
            
            Debug.Log($"개인 키 형식 검사:");
            Debug.Log($"  - BEGIN 헤더 존재: {(hasBeginHeader ? "✅" : "❌")}");
            Debug.Log($"  - END 헤더 존재: {(hasEndHeader ? "✅" : "❌")}");
            Debug.Log($"  - 줄바꿈 존재: {(hasNewlines ? "✅" : "❌")}");
        }
    }
    
    /// <summary>
    /// RSA 서명 테스트 (디버깅용)
    /// </summary>


    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}

/// <summary>
/// OAuth2 토큰 응답 구조체
/// </summary>
[System.Serializable]
public class OAuth2TokenResponse
{
    public string access_token;
    public string token_type;
    public int expires_in;
}

/// <summary>
/// 구글 시트 응답 구조체
/// </summary>
[System.Serializable]
public class GoogleSheetsResponse
{
    public string range;
    public string majorDimension;
    public string[][] values;
}

/// <summary>
/// 구글 시트 쓰기 요청 구조체
/// </summary>
[System.Serializable]
public class GoogleSheetsWriteRequest
{
    public string[][] values;
}