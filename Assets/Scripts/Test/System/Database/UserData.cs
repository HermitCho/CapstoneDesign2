using System;

/// <summary>
/// 기본 사용자 데이터 모델 클래스 (기존 호환성 유지)
/// </summary>
[Serializable]
public class UserData
{
    public int id;              // 기본키
    public string userId;       // 사용자 아이디 (로그인용)
    public string nickname;     // 게임 내 닉네임

    public UserData()
    {
        id = 0;
        userId = "";
        nickname = "";
    }

    public UserData(int id, string userId, string nickname)
    {
        this.id = id;
        this.userId = userId;
        this.nickname = nickname;
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
/// 게임 통계를 포함한 확장 사용자 데이터 모델 클래스
/// 구글 스프레드시트의 모든 컬럼 데이터를 포함
/// </summary>
[Serializable]
public class UserGameData : UserData
{
    public string password;     // 비밀번호 (실제로는 해시화 권장)
    public int win;            // 승리 횟수 (1등)
    public int lose;           // 패배 횟수 (2,3,4등)
    public int rate;           // 레이팅 점수
    public int money;          // 게임 재화

    public UserGameData() : base()
    {
        password = "";
        win = 0;
        lose = 0;
        rate = 1000; // 시작 레이팅
        money = 0;   // 시작 재화
    }

    public UserGameData(int id, string userId, string nickname, string password, int win, int lose, int rate, int money) 
        : base(id, userId, nickname)
    {
        this.password = password;
        this.win = win;
        this.lose = lose;
        this.rate = rate;
        this.money = money;
    }

    /// <summary>
    /// 총 게임 수 반환
    /// </summary>
    public int GetTotalGames()
    {
        return win + lose;
    }

    /// <summary>
    /// 승률 계산 (0.0 ~ 1.0)
    /// </summary>
    public float GetWinRate()
    {
        int totalGames = GetTotalGames();
        if (totalGames == 0) return 0f;
        return (float)win / totalGames;
    }

    /// <summary>
    /// 승률 퍼센트 반환 (0 ~ 100)
    /// </summary>
    public float GetWinRatePercent()
    {
        return GetWinRate() * 100f;
    }

    /// <summary>
    /// 기본 UserData로 변환
    /// </summary>
    public UserData ToUserData()
    {
        return new UserData(id, userId, nickname);
    }

    /// <summary>
    /// 재화 추가
    /// </summary>
    public void AddMoney(int amount)
    {
        money += amount;
        if (money < 0) money = 0; // 음수 방지
    }

    /// <summary>
    /// 디버그용 문자열 반환 (확장 정보 포함)
    /// </summary>
    public override string ToString()
    {
        return $"UserGameData[ID: {id}, UserId: {userId}, Nickname: {nickname}, Win: {win}, Lose: {lose}, Rate: {rate}, Money: {money}, WinRate: {GetWinRatePercent():F1}%]";
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

    private UserGameData _userGameData;
    private bool _isLoggedIn = false;

    private CurrentUser() { }

    /// <summary>
    /// 사용자 로그인 정보 설정 (확장 데이터 포함)
    /// </summary>
    public void SetUserGameData(UserGameData userData)
    {
        _userGameData = userData;
        _isLoggedIn = userData != null && userData.IsValid();
        
        if (_isLoggedIn)
        {
            // 로컬에 정보 저장 (기존 시스템과의 호환성을 위해)
            UnityEngine.PlayerPrefs.SetString("NickName", userData.nickname);
            UnityEngine.PlayerPrefs.SetString("UserId", userData.userId);
            UnityEngine.PlayerPrefs.SetInt("UserRate", userData.rate);
            UnityEngine.PlayerPrefs.SetInt("UserWin", userData.win);
            UnityEngine.PlayerPrefs.SetInt("UserLose", userData.lose);
            UnityEngine.PlayerPrefs.SetInt("UserMoney", userData.money);
            UnityEngine.PlayerPrefs.Save();
            
            UnityEngine.Debug.Log($"CurrentUser: 사용자 로그인 - {userData}");
        }
    }

    /// <summary>
    /// 기존 호환성을 위한 UserData 설정 메서드
    /// </summary>
    public void SetUserData(UserData userData)
    {
        if (userData is UserGameData gameData)
        {
            SetUserGameData(gameData);
        }
        else
        {
            // 기본 UserData를 UserGameData로 변환
            var gameUserData = new UserGameData
            {
                id = userData.id,
                userId = userData.userId,
                nickname = userData.nickname,
                win = 0,
                lose = 0,
                rate = 1000,
                money = 0
            };
            SetUserGameData(gameUserData);
        }
    }

    /// <summary>
    /// 사용자 로그아웃
    /// </summary>
    public void Logout()
    {
        _userGameData = null;
        _isLoggedIn = false;
        
        // 로컬 저장된 정보 삭제
        UnityEngine.PlayerPrefs.DeleteKey("NickName");
        UnityEngine.PlayerPrefs.DeleteKey("UserId");
        UnityEngine.PlayerPrefs.DeleteKey("UserRate");
        UnityEngine.PlayerPrefs.DeleteKey("UserWin");
        UnityEngine.PlayerPrefs.DeleteKey("UserLose");
        UnityEngine.PlayerPrefs.DeleteKey("UserMoney");
        UnityEngine.PlayerPrefs.Save();
        
        UnityEngine.Debug.Log("CurrentUser: 사용자 로그아웃 완료");
    }

    /// <summary>
    /// 현재 로그인 상태 확인
    /// </summary>
    public bool IsLoggedIn()
    {
        return _isLoggedIn && _userGameData != null && _userGameData.IsValid();
    }

    /// <summary>
    /// 현재 사용자 게임 데이터 가져오기
    /// </summary>
    public UserGameData GetUserGameData()
    {
        return _userGameData;
    }

    /// <summary>
    /// 기존 호환성을 위한 UserData 반환
    /// </summary>
    public UserData GetUserData()
    {
        return _userGameData?.ToUserData();
    }

    /// <summary>
    /// 현재 사용자 닉네임 가져오기
    /// </summary>
    public string GetNickname()
    {
        return _userGameData?.nickname ?? "Player";
    }

    /// <summary>
    /// 현재 사용자 아이디 가져오기
    /// </summary>
    public string GetUserId()
    {
        return _userGameData?.userId ?? "";
    }

    /// <summary>
    /// 현재 사용자 ID 가져오기
    /// </summary>
    public int GetId()
    {
        return _userGameData?.id ?? 0;
    }

    /// <summary>
    /// 현재 사용자 레이팅 가져오기
    /// </summary>
    public int GetRate()
    {
        return _userGameData?.rate ?? 1000;
    }

    /// <summary>
    /// 현재 사용자 승리 횟수 가져오기
    /// </summary>
    public int GetWin()
    {
        return _userGameData?.win ?? 0;
    }

    /// <summary>
    /// 현재 사용자 패배 횟수 가져오기
    /// </summary>
    public int GetLose()
    {
        return _userGameData?.lose ?? 0;
    }

    /// <summary>
    /// 현재 사용자 승률 가져오기 (퍼센트)
    /// </summary>
    public float GetWinRatePercent()
    {
        return _userGameData?.GetWinRatePercent() ?? 0f;
    }

    /// <summary>
    /// 현재 사용자 재화 가져오기
    /// </summary>
    public int GetMoney()
    {
        return _userGameData?.money ?? 0;
    }

    /// <summary>
    /// 게임 결과 업데이트 후 로컬 데이터 갱신
    /// </summary>
    public void UpdateGameStats(int win, int lose, int rate, int money)
    {
        if (_userGameData != null)
        {
            int oldMoney = _userGameData.money;
            
            _userGameData.win = win;
            _userGameData.lose = lose;
            _userGameData.rate = rate;
            _userGameData.money = money;
            
            UnityEngine.Debug.Log($"[CurrentUser] UpdateGameStats 호출 - Money: {oldMoney} -> {money}, Win: {win}, Lose: {lose}, Rate: {rate}");
            
            // PlayerPrefs 업데이트
            UnityEngine.PlayerPrefs.SetInt("UserRate", rate);
            UnityEngine.PlayerPrefs.SetInt("UserWin", win);
            UnityEngine.PlayerPrefs.SetInt("UserLose", lose);
            UnityEngine.PlayerPrefs.SetInt("UserMoney", money);
            UnityEngine.PlayerPrefs.Save();
            
            UnityEngine.Debug.Log($"CurrentUser: 게임 통계 업데이트 - Win: {win}, Lose: {lose}, Rate: {rate}, Money: {money}");
        }
    }

    /// <summary>
    /// 재화 추가 (로컬 데이터만)
    /// </summary>
    public void AddMoney(int amount)
    {
        if (_userGameData != null)
        {
            _userGameData.AddMoney(amount);
            UnityEngine.PlayerPrefs.SetInt("UserMoney", _userGameData.money);
            UnityEngine.PlayerPrefs.Save();
            
            UnityEngine.Debug.Log($"CurrentUser: 재화 추가 - {amount}, 현재 재화: {_userGameData.money}");
        }
    }
}
