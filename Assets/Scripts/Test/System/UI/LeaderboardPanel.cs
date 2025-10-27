using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Michsky.UI.Heat;
using UnityEngine.UI;
using System.Linq;
using DG.Tweening;

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
    [Tooltip("텍스트 애니메이션 딜레이")]
    [SerializeField] private float textAnimationDelay = 0.2f;

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
        // 구글 시트 업데이트 완료 이벤트 구독
        GoogleSheetsManager.OnSheetsWriteSuccess += OnSheetsWriteSuccess;
        
        // 패널이 활성화될 때만 데이터 로드 (최적화)
        if (!isUpdating)
        {
            Debug.Log("📋 LeaderboardPanel: 패널 활성화 - 최신 데이터 로드 시작");
            StartCoroutine(UpdateAllPanels());
        }
    }

    void OnDisable()
    {
        // 구글 시트 업데이트 완료 이벤트 구독 해제
        GoogleSheetsManager.OnSheetsWriteSuccess -= OnSheetsWriteSuccess;
        
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
    /// Google Sheets에서 최신 사용자 데이터 로드 (강제 새로고침)
    /// </summary>
    private IEnumerator LoadLatestUserData()
    {
        if (GoogleSheetsManager.Instance == null)
        {
            Debug.LogError("❌ GoogleSheetsManager를 찾을 수 없습니다!");
            yield break;
        }

        Debug.Log("📡 최신 사용자 데이터 강제 로드 중...");
        
        // 기존 캐시 데이터 클리어 (강제 새로고침을 위해)
        allUserData.Clear();
        
        // GoogleSheetsManager의 캐시를 무효화하고 새로 로드
        GoogleSheetsManager.Instance.LoadUserData();
        
        // 데이터 로드 완료까지 대기 (최대 10초로 증가)
        float timeout = 10f;
        float elapsed = 0f;
        int previousCount = -1;
        
        while (elapsed < timeout)
        {
            // GoogleSheetsManager에서 데이터를 가져옴
            var newData = GetAllUserDataFromManager();
            
            if (newData.Count > 0)
            {
                // 데이터가 실제로 변경되었는지 확인
                if (newData.Count != previousCount)
                {
                    allUserData = newData;
                    Debug.Log($"✅ 사용자 데이터 로드 완료 - {allUserData.Count}명 (이전: {previousCount}명)");
                    
                    // 현재 로그인된 사용자의 최신 정보 확인
                    if (CurrentUser.Instance.IsLoggedIn())
                    {
                        string currentUserId = CurrentUser.Instance.GetUserId();
                        var currentUserData = allUserData.FirstOrDefault(u => u.userId == currentUserId);
                        if (currentUserData != null)
                        {
                            Debug.Log($"🔍 현재 사용자 최신 정보 - {currentUserData.nickname}: Win={currentUserData.win}, Lose={currentUserData.lose}, Rate={currentUserData.rate}");
                        }
                    }
                    break;
                }
                previousCount = newData.Count;
            }
            
            yield return new WaitForSeconds(0.2f); // 더 긴 간격으로 체크
            elapsed += 0.2f;
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
            // 애니메이션과 함께 UI 업데이트
            StartCoroutine(AnimateProfileTexts(gameData));
            
            Debug.Log($"✅ 프로필 업데이트 완료: {gameData.nickname} (Rate: {gameData.rate})");
        }
        else
        {
            Debug.LogWarning("⚠️ 게임 데이터를 찾을 수 없습니다.");
            ClearProfileUI();
        }
    }
    
    /// <summary>
    /// 프로필 텍스트들을 순차적으로 팝 애니메이션과 함께 표시
    /// </summary>
    private IEnumerator AnimateProfileTexts(UserGameData gameData)
    {
        // 초기 상태 설정 (투명 + 축소)
        TextMeshProUGUI[] textComponents = { myNameText, myWinText, myLoseText, myRatingText };
        string[] textValues = { gameData.nickname, gameData.win.ToString(), gameData.lose.ToString(), gameData.rate.ToString() };
        
        for (int i = 0; i < textComponents.Length; i++)
        {
            if (textComponents[i] != null)
            {
                textComponents[i].alpha = 0f;
                textComponents[i].transform.localScale = Vector3.zero;
            }
        }
        
        // 티어 UI 초기화 (스포 방지)
        if (myTierImage != null)
        {
            myTierImage.color = new Color(1f, 1f, 1f, 0f); // 완전 투명
        }
        if (myTierRatingText != null)
        {
            myTierRatingText.alpha = 0f; // 완전 투명
        }
        
        // 순차적으로 팝 애니메이션
        for (int i = 0; i < textComponents.Length; i++)
        {
            if (textComponents[i] != null)
            {
                // 텍스트 설정
                textComponents[i].text = textValues[i];
                
                // 발랄한 팝 애니메이션 (OutBack 사용)
                Sequence seq = DOTween.Sequence();
                seq.Append(textComponents[i].transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutBack));
                seq.Join(textComponents[i].DOFade(1f, 0.15f).SetEase(Ease.OutQuad));
                seq.Append(textComponents[i].transform.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad));
                
                // 사운드 재생
                if (AudioManager.Inst != null)
                {
                    AudioManager.Inst.PlayOneShot("SFX_UI_PopLeaderboardText");
                }
                
                yield return new WaitForSeconds(textAnimationDelay);
            }
        }
        
        // 프로필 텍스트 완료 후 티어 레이팅 애니메이션 시작
        yield return StartCoroutine(AnimateTierRating(gameData.rate));
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
        
        Debug.Log($"✅ 티어 업데이트 준비 완료: Rate {gameData.rate} -> {GetTierName(gameData.rate)}");
        // 애니메이션은 AnimateTierRating에서 처리됨
    }
    
    /// <summary>
    /// 티어 레이팅 점수가 0부터 목표 점수까지 증가하는 애니메이션
    /// </summary>
    private IEnumerator AnimateTierRating(int targetRate)
    {
        if (myTierRatingText == null || myTierImage == null) yield break;
        
        // 초기 상태 설정
        myTierRatingText.text = "0";
        myTierImage.sprite = bronzeTierSprite;
        myTierImage.transform.localScale = Vector3.zero;
        
        // 첫 티어 스프라이트 등장 애니메이션
        yield return StartCoroutine(AnimateTierSpriteAppear());
        
        // 점수 증가 애니메이션 (0 → targetRate)
        float duration = 2f; // 2초 동안 증가
        float elapsed = 0f;
        int currentRate = 0;
        int lastTier = GetTierLevel(0);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Ease.OutQuad로 점수 증가
            currentRate = Mathf.RoundToInt(Mathf.Lerp(0, targetRate, progress));
            
            // 텍스트 업데이트
            myTierRatingText.text = currentRate.ToString();
            
            // 티어 레벨 확인
            int newTier = GetTierLevel(currentRate);
            if (newTier != lastTier)
            {
                // 티어가 변경되었을 때
                myTierImage.sprite = GetTierSprite(currentRate);
                yield return StartCoroutine(AnimateTierChange());
                lastTier = newTier;
            }
            
            // 점수 증가 사운드 (10점마다)
            if (currentRate % 10 == 0 && currentRate > 0)
            {
                if (AudioManager.Inst != null)
                {
                    AudioManager.Inst.PlayOneShot("SFX_UI_LeaderboardRatingText");
                }
            }
            
            yield return null;
        }
        
        // 최종 값 보장
        myTierRatingText.text = targetRate.ToString();
        myTierImage.sprite = GetTierSprite(targetRate);
    }
    
    /// <summary>
    /// 티어 스프라이트 첫 등장 애니메이션
    /// </summary>
    private IEnumerator AnimateTierSpriteAppear()
    {
        if (myTierImage == null) yield break;
        
        // 티어 이미지와 레이팅 텍스트 표시 (페이드 인)
        if (myTierImage != null)
        {
            myTierImage.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
        }
        if (myTierRatingText != null)
        {
            myTierRatingText.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
        }
        
        yield return new WaitForSeconds(0.15f);
        
        // 브론즈 티어 사운드
        if (AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayOneShot("SFX_UI_LeaderboardBronze");
        }
        
        // 스케일 애니메이션 + 회전
        Sequence seq = DOTween.Sequence();
        seq.Append(myTierImage.transform.DOScale(1.15f, 0.25f).SetEase(Ease.OutBack));
        seq.Join(myTierImage.transform.DORotate(new Vector3(0f, 0f, 5f), 0.15f).SetEase(Ease.OutQuad));
        seq.Append(myTierImage.transform.DORotate(Vector3.zero, 0.15f).SetEase(Ease.InOutQuad));
        seq.Join(myTierImage.transform.DOScale(1f, 0.15f).SetEase(Ease.InOutQuad));
        
        yield return seq.WaitForCompletion();
    }
    
    /// <summary>
    /// 티어 변경 시 애니메이션
    /// </summary>
    private IEnumerator AnimateTierChange()
    {
        if (myTierImage == null) yield break;
        
        // 현재 티어에 맞는 사운드 재생
        int currentRate = int.Parse(myTierRatingText.text);
        PlayTierSound(currentRate);
        
        // 스케일 증가 + 오른쪽 회전 → 원위치
        Sequence seq = DOTween.Sequence();
        seq.Append(myTierImage.transform.DOScale(1.15f, 0.2f).SetEase(Ease.OutBack));
        seq.Join(myTierImage.transform.DORotate(new Vector3(0f, 0f, 5f), 0.12f).SetEase(Ease.OutQuad));
        seq.Append(myTierImage.transform.DORotate(Vector3.zero, 0.12f).SetEase(Ease.InOutQuad));
        seq.Join(myTierImage.transform.DOScale(1f, 0.12f).SetEase(Ease.InOutQuad));
        
        yield return seq.WaitForCompletion();
    }
    
    /// <summary>
    /// 티어에 맞는 사운드 재생
    /// </summary>
    private void PlayTierSound(int rate)
    {
        if (AudioManager.Inst == null) return;
        
        string soundName = "";
        
        if (rate >= 2700 && IsTopTenPercent(rate))
        {
            soundName = "SFX_UI_LeaderboardMaster";
        }
        else if (rate >= 1901)
        {
            soundName = "SFX_UI_LeaderboardDiamond";
        }
        else if (rate >= 1301)
        {
            soundName = "SFX_UI_LeaderboardGold";
        }
        else if (rate >= 801)
        {
            soundName = "SFX_UI_LeaderboardSilver";
        }
        else
        {
            soundName = "SFX_UI_LeaderboardBronze";
        }
        
        AudioManager.Inst.PlayOneShot(soundName);
    }
    
    /// <summary>
    /// 점수 기반 티어 레벨 반환 (0=Bronze, 1=Silver, 2=Gold, 3=Diamond, 4=Master)
    /// </summary>
    private int GetTierLevel(int rate)
    {
        if (rate >= 2700 && IsTopTenPercent(rate)) return 4; // Master
        else if (rate >= 1901) return 3; // Diamond
        else if (rate >= 1301) return 2; // Gold
        else if (rate >= 801) return 1;  // Silver
        else return 0; // Bronze
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
    /// 구글 시트 업데이트 완료 시 자동 갱신
    /// </summary>
    private void OnSheetsWriteSuccess()
    {
        Debug.Log("📝 구글 시트 업데이트 완료 - 리더보드 강제 갱신 시작");
        
        if (!isUpdating)
        {
            // 구글 시트 반영 시간을 고려하여 지연 후 갱신
            StartCoroutine(DelayedRefreshAfterSheetsUpdate());
        }
        else
        {
            // 이미 업데이트 중이면 완료 후 다시 갱신
            StartCoroutine(WaitAndRefresh());
        }
    }
    
    /// <summary>
    /// 구글 시트 업데이트 후 지연된 리더보드 갱신
    /// </summary>
    private IEnumerator DelayedRefreshAfterSheetsUpdate()
    {
        // 구글 시트 서버 반영 시간 고려하여 3초 대기
        yield return new WaitForSeconds(3f);
        
        Debug.Log("🔄 구글 시트 업데이트 후 리더보드 강제 갱신 실행");
        
        if (!isUpdating)
        {
            // 캐시 무효화를 위해 allUserData 클리어
            allUserData.Clear();
            
            // 강제 업데이트 실행
            StartCoroutine(UpdateAllPanels());
        }
    }
    
    /// <summary>
    /// 업데이트 완료 후 재갱신
    /// </summary>
    private IEnumerator WaitAndRefresh()
    {
        // 현재 업데이트가 완료될 때까지 대기
        while (isUpdating)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // 완료 후 다시 갱신
        yield return new WaitForSeconds(1f);
        StartCoroutine(DelayedRefreshAfterSheetsUpdate());
    }
    
    /// <summary>
    /// 지연된 리더보드 갱신 (기존 메서드 호환성 유지)
    /// </summary>
    private IEnumerator DelayedRefresh()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (!isUpdating)
        {
            StartCoroutine(UpdateAllPanels());
        }
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
