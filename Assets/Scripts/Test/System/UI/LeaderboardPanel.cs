using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Michsky.UI.Heat;
using UnityEngine.UI;
using System.Linq;

public class LeaderboardPanel : MonoBehaviour
{
    [Header("프로필")]
    [Tooltip("이름 텍스트")]
    [SerializeField] private TextMeshProUGUI myNameText; [Space(5)]
    [Tooltip("승리횟수 텍스트")]
    [SerializeField] private TextMeshProUGUI myWinText; [Space(5)]
    [Tooltip("패배횟수 텍스트")]
    [SerializeField] private TextMeshProUGUI myLoseText; [Space(5)]
    [Tooltip("레이팅 텍스트")]
    [SerializeField] private TextMeshProUGUI myRatingText; [Space(10)]

    [Header("티어")]

    [Header("전체 티어 스프라이트")]
    [Tooltip("브론즈 티어 스프라이트")]
    [SerializeField] private Sprite bronzeTierSprite; [Space(5)]
    [Tooltip("실버 티어 스프라이트")]
    [SerializeField] private Sprite silverTierSprite; [Space(5)]
    [Tooltip("골드 티어 스프라이트")]
    [SerializeField] private Sprite goldTierSprite; [Space(5)]
    [Tooltip("다이아 티어 스프라이트")]
    [SerializeField] private Sprite diamondTierSprite; [Space(5)]
    [Tooltip("마스터 티어 스프라이트")]
    [SerializeField] private Sprite masterTierSprite; [Space(5)]

    [Header("내 티어 정보")]
    [Tooltip("내 티어 이미지")]
    [SerializeField] private Image myTierImage; [Space(5)]
    [Tooltip("내 티어 레이팅 텍스트")]
    [SerializeField] private TextMeshProUGUI myTierRatingText; [Space(10)]

    [Header("리더보드")]
    [Tooltip("랭킹 오브젝트 프리팹")]
    [SerializeField] private GameObject rankingObjectPrefab; [Space(5)]

    [Tooltip("프리팹 할당 부모 오브젝트")]
    [SerializeField] private Transform rankingObjectParent;

    // 캐시된 데이터
    private List<UserGameData> allUserData = new List<UserGameData>();
    private List<GameObject> spawnedRankingObjects = new List<GameObject>();
    private bool isUpdating = false;

    void OnEnable()
    {
        // 패널이 활성화될 때마다 업데이트
        if (!isUpdating)
        {
            StartCoroutine(UpdateAllPanels());
        }
    }

    void OnDisable()
    {
        // 패널이 비활성화될 때 업데이트 중단
        isUpdating = false;
    }

    /// <summary>
    /// 모든 패널 업데이트 (메인 진입점)
    /// </summary>
    private IEnumerator UpdateAllPanels()
    {
        isUpdating = true;
        
        Debug.Log("🏆 LeaderboardPanel: 업데이트 시작");
        
        // 1. Google Sheets에서 최신 데이터 로드
        yield return StartCoroutine(LoadLatestUserData());
        
        // 2. 프로필 업데이트
        ProfileUIUpdate();
        
        // 3. 티어 업데이트
        TierUIUpdate();
        
        // 4. 리더보드 업데이트
        LeaderboardUIUpdate();
        
        isUpdating = false;
        Debug.Log("✅ LeaderboardPanel: 모든 업데이트 완료");
    }

    /// <summary>
    /// Google Sheets에서 최신 사용자 데이터 로드
    /// </summary>
    private IEnumerator LoadLatestUserData()
    {
        if (GoogleSheetsManager.Instance == null)
        {
            Debug.LogError("❌ GoogleSheetsManager를 찾을 수 없습니다!");
            yield break;
        }

        Debug.Log("📡 최신 사용자 데이터 로드 중...");
        
        // GoogleSheetsManager의 캐시된 데이터를 새로 로드
        GoogleSheetsManager.Instance.LoadUserData();
        
        // 데이터 로드 완료까지 대기 (최대 5초)
        float timeout = 5f;
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            // GoogleSheetsManager에서 데이터를 가져옴 (private이므로 public 메서드 필요)
            allUserData = GetAllUserDataFromManager();
            
            if (allUserData.Count > 0)
            {
                Debug.Log($"✅ 사용자 데이터 로드 완료 - {allUserData.Count}명");
                break;
            }
            
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (allUserData.Count == 0)
        {
            Debug.LogWarning("⚠️ 사용자 데이터를 로드할 수 없습니다.");
        }
    }

    /// <summary>
    /// GoogleSheetsManager에서 사용자 데이터 가져오기
    /// </summary>
    private List<UserGameData> GetAllUserDataFromManager()
    {
        if (GoogleSheetsManager.Instance == null)
        {
            Debug.LogWarning("⚠️ GoogleSheetsManager 인스턴스가 없습니다.");
            return new List<UserGameData>();
        }

        if (!GoogleSheetsManager.Instance.IsDataLoaded())
        {
            Debug.LogWarning("⚠️ 데이터가 아직 로드되지 않았습니다.");
            return new List<UserGameData>();
        }

        var userData = GoogleSheetsManager.Instance.GetAllUserData();
        Debug.Log($"📊 GoogleSheetsManager에서 {userData.Count}명의 사용자 데이터를 가져왔습니다.");
        
        return userData;
    }

    /// <summary>
    /// 프로필 UI 업데이트 (로그인된 사용자 정보)
    /// </summary>
    private void ProfileUIUpdate()
    {
        Debug.Log("👤 프로필 업데이트 시작");
        
        if (!CurrentUser.Instance.IsLoggedIn())
        {
            Debug.LogWarning("⚠️ 로그인된 사용자가 없습니다.");
            ClearProfileUI();
            return;
        }
        
        var currentUserData = CurrentUser.Instance.GetUserData();
        if (currentUserData == null)
        {
            Debug.LogWarning("⚠️ 현재 사용자 데이터를 가져올 수 없습니다.");
            ClearProfileUI();
            return;
        }
        
        // UserData를 UserGameData로 변환하여 최신 정보 가져오기
        var gameData = currentUserData as UserGameData;
        if (gameData == null)
        {
            // 시트에서 최신 정보 찾기
            gameData = allUserData.FirstOrDefault(u => u.userId == currentUserData.userId);
        }
        
        if (gameData != null)
        {
            // UI 업데이트
            if (myNameText != null)
                myNameText.text = gameData.nickname;
            
            if (myWinText != null)
                myWinText.text = gameData.win.ToString();
            
            if (myLoseText != null)
                myLoseText.text = gameData.lose.ToString();
            
            if (myRatingText != null)
                myRatingText.text = gameData.rate.ToString();
            
            Debug.Log($"✅ 프로필 업데이트 완료: {gameData.nickname} (Rate: {gameData.rate})");
        }
        else
        {
            Debug.LogWarning("⚠️ 게임 데이터를 찾을 수 없습니다.");
            ClearProfileUI();
        }
    }

    /// <summary>
    /// 프로필 UI 초기화
    /// </summary>
    private void ClearProfileUI()
    {
        if (myNameText != null) myNameText.text = "-";
        if (myWinText != null) myWinText.text = "0";
        if (myLoseText != null) myLoseText.text = "0";
        if (myRatingText != null) myRatingText.text = "0";
    }

    /// <summary>
    /// 티어 UI 업데이트 (Rate 기반 티어 시스템)
    /// </summary>
    private void TierUIUpdate()
    {
        Debug.Log("🏅 티어 업데이트 시작");
        
        if (!CurrentUser.Instance.IsLoggedIn())
        {
            Debug.LogWarning("⚠️ 로그인된 사용자가 없습니다.");
            ClearTierUI();
            return;
        }
        
        var currentUserData = CurrentUser.Instance.GetUserData();
        var gameData = currentUserData as UserGameData;
        if (gameData == null)
        {
            gameData = allUserData.FirstOrDefault(u => u.userId == currentUserData.userId);
        }
        
        if (gameData == null)
        {
            Debug.LogWarning("⚠️ 게임 데이터를 찾을 수 없습니다.");
            ClearTierUI();
            return;
        }
        
        int userRate = gameData.rate;
        Sprite tierSprite = GetTierSprite(userRate);
        
        // 티어 이미지 설정
        if (myTierImage != null)
        {
            myTierImage.sprite = tierSprite;
        }
        
        // 티어 레이팅 텍스트 설정
        if (myTierRatingText != null)
        {
            myTierRatingText.text = userRate.ToString();
        }
        
        Debug.Log($"✅ 티어 업데이트 완료: Rate {userRate} -> {GetTierName(userRate)}");
    }

    /// <summary>
    /// Rate에 따른 티어 스프라이트 반환
    /// </summary>
    private Sprite GetTierSprite(int rate)
    {
        // 마스터 티어 조건: 2700 이상 + 상위 10%
        if (rate >= 2700 && IsTopTenPercent(rate))
        {
            return masterTierSprite;
        }
        // 다이아 티어: 1901~2700
        else if (rate >= 1901)
        {
            return diamondTierSprite;
        }
        // 골드 티어: 1301~1900
        else if (rate >= 1301)
        {
            return goldTierSprite;
        }
        // 실버 티어: 801~1300
        else if (rate >= 801)
        {
            return silverTierSprite;
        }
        // 브론즈 티어: 0~800
        else
        {
            return bronzeTierSprite;
        }
    }

    /// <summary>
    /// Rate에 따른 티어 이름 반환 (디버깅용)
    /// </summary>
    private string GetTierName(int rate)
    {
        if (rate >= 2700 && IsTopTenPercent(rate)) return "Master";
        else if (rate >= 1901) return "Diamond";
        else if (rate >= 1301) return "Gold";
        else if (rate >= 801) return "Silver";
        else return "Bronze";
    }

    /// <summary>
    /// 상위 10% 여부 확인
    /// </summary>
    private bool IsTopTenPercent(int userRate)
    {
        if (allUserData.Count == 0) return false;
        
        // 모든 플레이어의 Rate를 내림차순으로 정렬
        var sortedRates = allUserData.Select(u => u.rate).OrderByDescending(r => r).ToList();
        
        // 상위 10% 계산
        int topTenPercentCount = Mathf.Max(1, Mathf.FloorToInt(sortedRates.Count * 0.1f));
        
        // 상위 10%에 포함되는지 확인
        if (topTenPercentCount < sortedRates.Count)
        {
            int topTenPercentThreshold = sortedRates[topTenPercentCount - 1];
            return userRate >= topTenPercentThreshold;
        }
        
        return false;
    }

    /// <summary>
    /// 티어 UI 초기화
    /// </summary>
    private void ClearTierUI()
    {
        if (myTierImage != null) myTierImage.sprite = bronzeTierSprite;
        if (myTierRatingText != null) myTierRatingText.text = "0";
    }

    /// <summary>
    /// 리더보드 UI 업데이트 (상위 30명 랭킹 표시)
    /// </summary>
    private void LeaderboardUIUpdate()
    {
        Debug.Log("📊 리더보드 업데이트 시작");
        
        // 기존 랭킹 오브젝트 제거
        ClearRankingObjects();
        
        if (allUserData.Count == 0)
        {
            Debug.LogWarning("⚠️ 표시할 사용자 데이터가 없습니다.");
            return;
        }
        
        // 병합 정렬을 사용하여 Rate 기준 내림차순 정렬
        var sortedUsers = MergeSort(allUserData.ToList(), (a, b) => b.rate.CompareTo(a.rate));
        
        // 상위 30명 또는 전체 인원 (둘 중 작은 값)
        int displayCount = Mathf.Min(30, sortedUsers.Count);
        
        Debug.Log($"🏆 상위 {displayCount}명 리더보드 생성 중...");
        
        // 랭킹 오브젝트 생성
        for (int i = 0; i < displayCount; i++)
        {
            CreateRankingObject(i + 1, sortedUsers[i]);
        }
        
        Debug.Log($"✅ 리더보드 업데이트 완료 - {displayCount}명 표시");
    }

    /// <summary>
    /// 랭킹 오브젝트 생성
    /// </summary>
    private void CreateRankingObject(int rank, UserGameData userData)
    {
        if (rankingObjectPrefab == null || rankingObjectParent == null)
        {
            Debug.LogError("❌ 랭킹 오브젝트 프리팹 또는 부모 오브젝트가 설정되지 않았습니다!");
            return;
        }
        
        // 프리팹 인스턴스 생성
        GameObject rankingObj = Instantiate(rankingObjectPrefab, rankingObjectParent);
        spawnedRankingObjects.Add(rankingObj);
        
        // Info 태그가 달린 자식 오브젝트 찾기
        Transform infoTransform = FindChildWithTag(rankingObj.transform, "Info");
        if (infoTransform == null)
        {
            Debug.LogError($"❌ 랭킹 오브젝트에서 'Info' 태그를 찾을 수 없습니다! (Rank: {rank})");
            return;
        }
        
        // Tier, Rate, Name 자식 오브젝트 찾기
        Transform tierTransform = infoTransform.Find("Tier");
        Transform rateTransform = infoTransform.Find("Rate");
        Transform nameTransform = infoTransform.Find("Name");
        
        // Tier 이미지 설정
        if (tierTransform != null)
        {
            Image tierImage = tierTransform.GetComponent<Image>();
            if (tierImage != null)
            {
                tierImage.sprite = GetTierSprite(userData.rate);
            }
            else
            {
                Debug.LogWarning($"⚠️ Tier 오브젝트에 Image 컴포넌트가 없습니다! (Rank: {rank})");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ 'Tier' 자식 오브젝트를 찾을 수 없습니다! (Rank: {rank})");
        }
        
        // Rate 텍스트 설정
        if (rateTransform != null)
        {
            TextMeshProUGUI rateText = rateTransform.GetComponent<TextMeshProUGUI>();
            if (rateText != null)
            {
                rateText.text = userData.rate.ToString();
            }
            else
            {
                Debug.LogWarning($"⚠️ Rate 오브젝트에 TextMeshProUGUI 컴포넌트가 없습니다! (Rank: {rank})");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ 'Rate' 자식 오브젝트를 찾을 수 없습니다! (Rank: {rank})");
        }
        
        // Name 텍스트 설정
        if (nameTransform != null)
        {
            TextMeshProUGUI nameText = nameTransform.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = userData.nickname;
            }
            else
            {
                Debug.LogWarning($"⚠️ Name 오브젝트에 TextMeshProUGUI 컴포넌트가 없습니다! (Rank: {rank})");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ 'Name' 자식 오브젝트를 찾을 수 없습니다! (Rank: {rank})");
        }
        
        Debug.Log($"🏅 {rank}등: {userData.nickname} (Rate: {userData.rate}) - 랭킹 오브젝트 생성 완료");
    }

    /// <summary>
    /// 특정 태그를 가진 자식 오브젝트 찾기
    /// </summary>
    private Transform FindChildWithTag(Transform parent, string tag)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.CompareTag(tag))
            {
                return child;
            }
            
            // 재귀적으로 하위 자식들도 검색
            Transform found = FindChildWithTag(child, tag);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    /// <summary>
    /// 기존 랭킹 오브젝트들 제거
    /// </summary>
    private void ClearRankingObjects()
    {
        foreach (GameObject obj in spawnedRankingObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        spawnedRankingObjects.Clear();
        
        Debug.Log("🗑️ 기존 랭킹 오브젝트 제거 완료");
    }

    /// <summary>
    /// 병합 정렬 (최적화된 정렬 알고리즘)
    /// </summary>
    private List<T> MergeSort<T>(List<T> list, System.Comparison<T> comparison)
    {
        if (list.Count <= 1)
            return list;
        
        // 분할
        int middle = list.Count / 2;
        List<T> left = list.GetRange(0, middle);
        List<T> right = list.GetRange(middle, list.Count - middle);
        
        // 재귀적으로 정렬
        left = MergeSort(left, comparison);
        right = MergeSort(right, comparison);
        
        // 병합
        return Merge(left, right, comparison);
    }

    /// <summary>
    /// 병합 과정
    /// </summary>
    private List<T> Merge<T>(List<T> left, List<T> right, System.Comparison<T> comparison)
    {
        List<T> result = new List<T>();
        int leftIndex = 0, rightIndex = 0;
        
        // 두 리스트를 비교하며 병합
        while (leftIndex < left.Count && rightIndex < right.Count)
        {
            if (comparison(left[leftIndex], right[rightIndex]) <= 0)
            {
                result.Add(left[leftIndex]);
                leftIndex++;
            }
            else
            {
                result.Add(right[rightIndex]);
                rightIndex++;
            }
        }
        
        // 남은 요소들 추가
        while (leftIndex < left.Count)
        {
            result.Add(left[leftIndex]);
            leftIndex++;
        }
        
        while (rightIndex < right.Count)
        {
            result.Add(right[rightIndex]);
            rightIndex++;
        }
        
        return result;
    }

    /// <summary>
    /// 수동 업데이트 (디버깅용)
    /// </summary>
    [ContextMenu("수동 업데이트")]
    public void ManualUpdate()
    {
        if (!isUpdating)
        {
            StartCoroutine(UpdateAllPanels());
        }
    }
}
