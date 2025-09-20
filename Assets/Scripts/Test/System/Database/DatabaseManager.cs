using System;
using System.Collections;
using UnityEngine;
using MySql.Data.MySqlClient;

/// <summary>
/// MySQL/MariaDB 데이터베이스 연결 및 관리 클래스
/// 싱글톤 패턴으로 구현하여 전역에서 접근 가능
/// </summary>
public class DatabaseManager : MonoBehaviour
{
    private static DatabaseManager _instance;
    public static DatabaseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("DatabaseManager");
                _instance = go.AddComponent<DatabaseManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("데이터베이스 연결 설정")]
    [SerializeField] private string server = "127.0.0.1";
    [SerializeField] private string database = "gamedb";
    [SerializeField] private string uid = "root";
    [SerializeField] private string password = "root";
    [SerializeField] private int port = 3306;

    private string connectionString;
    private bool isConnected = false;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDatabase();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 데이터베이스 초기화 및 테이블 생성
    /// </summary>
    private void InitializeDatabase()
    {
        connectionString = $"Server={server};Database={database};Uid={uid};Pwd={password};Port={port};";
        
        // 데이터베이스 연결 테스트
        StartCoroutine(TestConnection());
        
        // 사용자 테이블이 존재하지 않으면 생성
        CreateUserTableIfNotExists();
    }

    /// <summary>
    /// 데이터베이스 연결 테스트
    /// </summary>
    private IEnumerator TestConnection()
    {
        yield return StartCoroutine(TestConnectionCoroutine((success) =>
        {
            if (success)
            {
                Debug.Log(" DatabaseManager: 데이터베이스 연결 성공");
                isConnected = true;
            }
            else
            {
                Debug.LogError(" DatabaseManager: 데이터베이스 연결 실패");
                isConnected = false;
            }
        }));
    }

    /// <summary>
    /// 연결 테스트 코루틴
    /// </summary>
    private IEnumerator TestConnectionCoroutine(System.Action<bool> callback)
    {
        bool connectionResult = false;
        
        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                connectionResult = true;
                conn.Close();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($" DatabaseManager: 연결 테스트 실패 - {e.Message}");
            connectionResult = false;
        }
        
        yield return null;
        callback(connectionResult);
    }

    /// <summary>
    /// 사용자 테이블 생성 (존재하지 않을 경우)
    /// </summary>
    private void CreateUserTableIfNotExists()
    {
        StartCoroutine(CreateUserTableCoroutine());
    }

    private IEnumerator CreateUserTableCoroutine()
    {
        string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS users (
                id INT AUTO_INCREMENT PRIMARY KEY,
                user_id VARCHAR(50) NOT NULL UNIQUE,
                nickname VARCHAR(20) NOT NULL,
                password VARCHAR(255) NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                last_login TIMESTAMP NULL DEFAULT NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

        yield return StartCoroutine(ExecuteNonQueryCoroutine(createTableQuery, (success) =>
        {
            if (success)
            {
                Debug.Log(" DatabaseManager: 사용자 테이블 확인/생성 완료");
            }
            else
            {
                Debug.LogError(" DatabaseManager: 사용자 테이블 생성 실패");
            }
        }));
    }

    /// <summary>
    /// 사용자 등록 (회원가입)
    /// </summary>
    public void RegisterUser(string userId, string nickname, string userPassword, System.Action<bool, string> callback)
    {
        StartCoroutine(RegisterUserCoroutine(userId, nickname, userPassword, callback));
    }

    private IEnumerator RegisterUserCoroutine(string userId, string nickname, string userPassword, System.Action<bool, string> callback)
    {
        // 입력 검증
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(nickname) || string.IsNullOrWhiteSpace(userPassword))
        {
            callback(false, "모든 필드를 입력해주세요.");
            yield break;
        }

        if (nickname.Length > 5)
        {
            callback(false, "닉네임은 5글자 이하로 입력해주세요.");
            yield break;
        }

        // 아이디 중복 검사
        yield return StartCoroutine(CheckUserIdExistsCoroutine(userId, (exists) =>
        {
            if (exists)
            {
                callback(false, "이미 사용중인 아이디입니다.");
                return;
            }

            // 사용자 등록 쿼리 실행
            StartCoroutine(InsertUserCoroutine(userId, nickname, userPassword, callback));
        }));
    }

    /// <summary>
    /// 사용자 삽입 코루틴
    /// </summary>
    private IEnumerator InsertUserCoroutine(string userId, string nickname, string userPassword, System.Action<bool, string> callback)
    {
        string insertQuery = "INSERT INTO users (user_id, nickname, password) VALUES (@userId, @nickname, @password)";
        
        bool insertSuccess = false;
        string errorMessage = "";

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@nickname", nickname);
                    cmd.Parameters.AddWithValue("@password", HashPassword(userPassword));
                    
                    int rowsAffected = cmd.ExecuteNonQuery();
                    insertSuccess = rowsAffected > 0;
                }
                conn.Close();
            }
        }
        catch (MySqlException e)
        {
            if (e.Number == 1062) // Duplicate entry error
            {
                errorMessage = "이미 사용중인 아이디입니다.";
            }
            else
            {
                errorMessage = "회원가입 중 오류가 발생했습니다.";
            }
            Debug.LogError($" DatabaseManager: 사용자 등록 실패 - {e.Message}");
        }
        catch (Exception e)
        {
            errorMessage = "회원가입 중 오류가 발생했습니다.";
            Debug.LogError($"DatabaseManager: 사용자 등록 실패 - {e.Message}");
        }

        yield return null;
        
        if (insertSuccess)
        {
            Debug.Log($" DatabaseManager: 사용자 등록 성공 - {userId}");
            callback(true, "회원가입이 완료되었습니다.");
        }
        else
        {
            callback(false, errorMessage);
        }
    }

    /// <summary>
    /// 사용자 로그인 검증
    /// </summary>
    public void LoginUser(string userId, string userPassword, System.Action<bool, string, UserData> callback)
    {
        StartCoroutine(LoginUserCoroutine(userId, userPassword, callback));
    }

    private IEnumerator LoginUserCoroutine(string userId, string userPassword, System.Action<bool, string, UserData> callback)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(userPassword))
        {
            callback(false, "아이디와 비밀번호를 입력해주세요.", null);
            yield break;
        }

        string selectQuery = "SELECT id, user_id, nickname, password FROM users WHERE user_id = @userId";
        UserData userData = null;
        bool loginSuccess = false;
        string message = "";

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(selectQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedPassword = reader.GetString("password");
                            
                            if (VerifyPassword(userPassword, storedPassword))
                            {
                                userData = new UserData
                                {
                                    id = reader.GetInt32("id"),
                                    userId = reader.GetString("user_id"),
                                    nickname = reader.GetString("nickname")
                                };
                                loginSuccess = true;
                                message = "로그인 성공";
                            }
                            else
                            {
                                message = "비밀번호가 올바르지 않습니다.";
                            }
                        }
                        else
                        {
                            message = "존재하지 않는 아이디입니다.";
                        }
                    }
                }
                conn.Close();
            }
        }
        catch (Exception e)
        {
            message = "로그인 중 오류가 발생했습니다.";
            Debug.LogError($"DatabaseManager: 로그인 실패 - {e.Message}");
        }

        yield return null;

        if (loginSuccess)
        {
            // 마지막 로그인 시간 업데이트
            StartCoroutine(UpdateLastLoginCoroutine(userId));
            Debug.Log($"DatabaseManager: 로그인 성공 - {userId}");
        }

        callback(loginSuccess, message, userData);
    }

    /// <summary>
    /// 아이디 중복 검사
    /// </summary>
    public void CheckUserIdExists(string userId, System.Action<bool> callback)
    {
        StartCoroutine(CheckUserIdExistsCoroutine(userId, callback));
    }

    private IEnumerator CheckUserIdExistsCoroutine(string userId, System.Action<bool> callback)
    {
        string selectQuery = "SELECT COUNT(*) FROM users WHERE user_id = @userId";
        bool exists = false;

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(selectQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    exists = count > 0;
                }
                conn.Close();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"DatabaseManager: 아이디 중복 검사 실패 - {e.Message}");
            exists = true; // 오류 시 안전하게 중복으로 처리
        }

        yield return null;
        callback(exists);
    }

    /// <summary>
    /// 마지막 로그인 시간 업데이트
    /// </summary>
    private IEnumerator UpdateLastLoginCoroutine(string userId)
    {
        string updateQuery = "UPDATE users SET last_login = NOW() WHERE user_id = @userId";
        
        yield return StartCoroutine(ExecuteNonQueryCoroutine(updateQuery, (success) =>
        {
            if (success)
            {
                Debug.Log($" DatabaseManager: 마지막 로그인 시간 업데이트 완료 - {userId}");
            }
            else
            {
                Debug.LogWarning($"DatabaseManager: 마지막 로그인 시간 업데이트 실패 - {userId}");
            }
        }, new MySqlParameter("@userId", userId)));
    }

    /// <summary>
    /// 비쿼리 실행 코루틴 (INSERT, UPDATE, DELETE)
    /// </summary>
    private IEnumerator ExecuteNonQueryCoroutine(string query, System.Action<bool> callback, params MySqlParameter[] parameters)
    {
        bool success = false;

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    
                    cmd.ExecuteNonQuery();
                    success = true;
                }
                conn.Close();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"DatabaseManager: 쿼리 실행 실패 - {e.Message}");
            success = false;
        }

        yield return null;
        callback(success);
    }

    /// <summary>
    /// 비밀번호 해시화 (간단한 해시 - 실제 프로덕션에서는 더 강력한 해시 사용 권장)
    /// </summary>
    private string HashPassword(string password)
    {
        // Unity에서 사용 가능한 간단한 해시 방법
        // 실제 프로덕션에서는 BCrypt 등 더 안전한 방법 사용 권장
        return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password + "salt_key_2024"));
    }

    /// <summary>
    /// 비밀번호 검증
    /// </summary>
    private bool VerifyPassword(string inputPassword, string storedPassword)
    {
        string hashedInput = HashPassword(inputPassword);
        return hashedInput == storedPassword;
    }

    /// <summary>
    /// 연결 상태 확인
    /// </summary>
    public bool IsConnected()
    {
        return isConnected;
    }

    /// <summary>
    /// 연결 문자열 가져오기 (디버깅용)
    /// </summary>
    public string GetConnectionString()
    {
        return connectionString;
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
