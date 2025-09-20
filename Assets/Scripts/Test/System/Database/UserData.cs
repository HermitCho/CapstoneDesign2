using System;

/// <summary>
/// 사용자 데이터 모델 클래스
/// 데이터베이스에서 가져온 사용자 정보를 저장하는 구조체
/// </summary>
[Serializable]
public class UserData
{
    public int id;              // 데이터베이스 기본키
    public string userId;       // 사용자 아이디 (로그인용)
    public string nickname;     // 게임 내 닉네임
    public DateTime createdAt;  // 계정 생성일
    public DateTime lastLogin;  // 마지막 로그인 시간

    public UserData()
    {
        id = 0;
        userId = "";
        nickname = "";
        createdAt = DateTime.Now;
        lastLogin = DateTime.Now;
    }

    public UserData(int id, string userId, string nickname)
    {
        this.id = id;
        this.userId = userId;
        this.nickname = nickname;
        this.createdAt = DateTime.Now;
        this.lastLogin = DateTime.Now;
    }

    /// <summary>
    /// 사용자 데이터가 유효한지 확인
    /// </summary>
    public bool IsValid()
    {
        return id > 0 && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(nickname);
    }

    /// <summary>
    /// 디버그용 문자열 반환
    /// </summary>
    public override string ToString()
    {
        return $"UserData[ID: {id}, UserId: {userId}, Nickname: {nickname}]";
    }
}

/// <summary>
/// 현재 로그인된 사용자 정보를 관리하는 싱글톤 클래스
/// </summary>
public class CurrentUser
{
    private static CurrentUser _instance;
    public static CurrentUser Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new CurrentUser();
            }
            return _instance;
        }
    }

    private UserData _userData;
    private bool _isLoggedIn = false;

    private CurrentUser() { }

    /// <summary>
    /// 사용자 로그인 정보 설정
    /// </summary>
    public void SetUserData(UserData userData)
    {
        _userData = userData;
        _isLoggedIn = userData != null && userData.IsValid();
        
        if (_isLoggedIn)
        {
            // 로컬에 닉네임 저장 (기존 시스템과의 호환성을 위해)
            UnityEngine.PlayerPrefs.SetString("NickName", userData.nickname);
            UnityEngine.PlayerPrefs.SetString("UserId", userData.userId);
            UnityEngine.PlayerPrefs.Save();
            
            UnityEngine.Debug.Log($"CurrentUser: 사용자 로그인 - {userData}");
        }
    }

    /// <summary>
    /// 사용자 로그아웃
    /// </summary>
    public void Logout()
    {
        _userData = null;
        _isLoggedIn = false;
        
        // 로컬 저장된 정보 삭제
        UnityEngine.PlayerPrefs.DeleteKey("NickName");
        UnityEngine.PlayerPrefs.DeleteKey("UserId");
        UnityEngine.PlayerPrefs.Save();
        
        UnityEngine.Debug.Log("CurrentUser: 사용자 로그아웃 완료");
    }

    /// <summary>
    /// 현재 로그인 상태 확인
    /// </summary>
    public bool IsLoggedIn()
    {
        return _isLoggedIn && _userData != null && _userData.IsValid();
    }

    /// <summary>
    /// 현재 사용자 데이터 가져오기
    /// </summary>
    public UserData GetUserData()
    {
        return _userData;
    }

    /// <summary>
    /// 현재 사용자 닉네임 가져오기
    /// </summary>
    public string GetNickname()
    {
        return _userData?.nickname ?? "Player";
    }

    /// <summary>
    /// 현재 사용자 아이디 가져오기
    /// </summary>
    public string GetUserId()
    {
        return _userData?.userId ?? "";
    }

    /// <summary>
    /// 현재 사용자 ID 가져오기
    /// </summary>
    public int GetId()
    {
        return _userData?.id ?? 0;
    }
}
