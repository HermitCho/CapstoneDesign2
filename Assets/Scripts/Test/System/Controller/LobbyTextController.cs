using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class LobbyTextController : MonoBehaviour
{
    [Header("텍스트 할당")]
    public TextMeshProUGUI Text;

    [Space(10)]
    [Header("Money UI")]
    [SerializeField] private Image _moneyImage;
    [Tooltip("Money 텍스트")]
    [SerializeField] private TextMeshProUGUI _moneyText;
    
    // Money 애니메이션 Tween
    private Tween moneyImageBounceTween;
    private Tween moneyTextCountTween;
    
    // 마지막으로 표시된 재화량
    private int lastDisplayedMoney = 0;
    private bool isFirstDisplay = true;

    void Start()
    {
        // 구글 시트에서 최신 데이터 로드 후 Money UI 초기화
        StartCoroutine(LoadMoneyFromGoogleSheets());
    }
    
    void OnDestroy()
    {
        // 애니메이션 정리
        CleanupMoneyAnimations();
    }

    public void SetText(string text)
    {
        Text.text = text;
    }
    
    /// <summary>
    /// 구글 시트에서 최신 Money 데이터 로드
    /// </summary>
    private IEnumerator LoadMoneyFromGoogleSheets()
    {
        if (_moneyImage == null || _moneyText == null) yield break;
        
        // 초기 상태 설정
        _moneyImage.transform.localScale = Vector3.zero;
        _moneyText.text = "0";
        
        // 로그인 확인
        if (CurrentUser.Instance == null || !CurrentUser.Instance.IsLoggedIn())
        {
            Debug.LogWarning("LobbyTextController: 로그인되지 않은 상태");
            yield break;
        }
        
        // GoogleSheetsManager가 준비될 때까지 대기
        if (GoogleSheetsManager.Instance == null)
        {
            Debug.LogError("LobbyTextController: GoogleSheetsManager가 없습니다");
            yield break;
        }
        
        // 구글 시트 연결 확인
        if (!GoogleSheetsManager.Instance.IsConnected())
        {
            Debug.LogWarning("LobbyTextController: GoogleSheetsManager가 연결되지 않았습니다");
            yield break;
        }
        
        // 데이터 로드 대기 (최대 5초)
        float timeout = 5f;
        float timer = 0f;
        
        while (!GoogleSheetsManager.Instance.IsDataLoaded() && timer < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }
        
        if (!GoogleSheetsManager.Instance.IsDataLoaded())
        {
            Debug.LogWarning("LobbyTextController: 구글 시트 데이터 로드 타임아웃");
            yield break;
        }
        
        // 사용자 ID로 구글 시트에서 데이터 찾기
        string userId = CurrentUser.Instance.GetUserId();
        var allUserData = GoogleSheetsManager.Instance.GetAllUserData();
        var userData = allUserData.FirstOrDefault(u => u.userId == userId);
        
        if (userData == null)
        {
            Debug.LogWarning($"LobbyTextController: 구글 시트에서 사용자 '{userId}' 데이터를 찾을 수 없습니다");
            yield break;
        }
        
        // 구글 시트에서 재화 가져오기 (동기화는 하지 않음!)
        int sheetMoney = userData.money;
        
        // CurrentUser의 로컬 데이터와 비교 (디버그용)
        var currentGameData = CurrentUser.Instance.GetUserGameData();
        int localMoney = currentGameData != null ? currentGameData.money : 0;
        
        Debug.Log($"[LobbyTextController] 재화 확인 - 로컬: {localMoney}, 시트: {sheetMoney}");
        
        // UI에 시트 데이터를 표시 (CurrentUser는 건드리지 않음!)
        // GameResultManager가 이미 CurrentUser를 업데이트했으므로 여기서는 표시만 함
        yield return StartCoroutine(AnimateMoneyUICoroutine(sheetMoney));
    }
    
    /// <summary>
    /// Money UI 초기화 및 애니메이션 실행
    /// </summary>
    private void InitializeMoneyUI()
    {
        if (_moneyImage == null || _moneyText == null) return;
        
        // CurrentUser에서 재화 가져오기
        int currentMoney = 0;
        if (CurrentUser.Instance != null && CurrentUser.Instance.IsLoggedIn())
        {
            currentMoney = CurrentUser.Instance.GetMoney();
        }
        
        // 초기 상태 설정
        _moneyImage.transform.localScale = Vector3.zero;
        _moneyText.text = "0";
        
        // 애니메이션 실행
        StartCoroutine(AnimateMoneyUICoroutine(currentMoney));
    }
    
    /// <summary>
    /// Money UI 업데이트 (외부 호출용)
    /// </summary>
    public void UpdateMoneyUI()
    {
        if (_moneyImage == null || _moneyText == null) return;
        
        // CurrentUser에서 재화 가져오기
        int currentMoney = 0;
        if (CurrentUser.Instance != null && CurrentUser.Instance.IsLoggedIn())
        {
            currentMoney = CurrentUser.Instance.GetMoney();
        }
        
        // 재화가 변경되었을 때만 애니메이션 실행
        if (currentMoney != lastDisplayedMoney)
        {
            StartCoroutine(AnimateMoneyUICoroutine(currentMoney));
        }
    }
    
    /// <summary>
    /// Money UI 애니메이션 코루틴
    /// </summary>
    private IEnumerator AnimateMoneyUICoroutine(int targetMoney)
    {
        // 기존 애니메이션 정리
        CleanupMoneyAnimations();
        
        yield return new WaitForSeconds(0.2f); // 잠시 대기
        
        // Money 이미지 바운스 애니메이션 (위로 통통 튀는 효과)
        moneyImageBounceTween = DOTween.Sequence()
            .Append(_moneyImage.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack))
            .Append(_moneyImage.transform.DOScale(0.9f, 0.15f).SetEase(Ease.InQuad))
            .Append(_moneyImage.transform.DOScale(1.1f, 0.15f).SetEase(Ease.OutQuad))
            .Append(_moneyImage.transform.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad));
        
        // 사운드 재생
        if (AudioManager.Inst != null)
        {
            AudioManager.Inst.PlayOneShot("SFX_UI_GameOver_PopScore");
        }
        
        yield return moneyImageBounceTween.WaitForCompletion();
        
        // Money 텍스트 카운트업 애니메이션 (0 → 목표 재화)
        yield return StartCoroutine(AnimateMoneyCountUp(targetMoney));
        
        // 마지막으로 표시된 재화량 업데이트
        lastDisplayedMoney = targetMoney;
        isFirstDisplay = false;
    }
    
    /// <summary>
    /// Money 텍스트 카운트업 애니메이션
    /// </summary>
    private IEnumerator AnimateMoneyCountUp(int targetMoney)
    {
        if (_moneyText == null) yield break;
        
        float currentMoney = isFirstDisplay ? 0f : lastDisplayedMoney;
        float duration = 1.0f; // 1초 동안 카운트업
        float elapsedTime = 0f;
        int lastDisplayed = (int)currentMoney;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            currentMoney = Mathf.Lerp(isFirstDisplay ? 0f : lastDisplayedMoney, targetMoney, t);
            int displayMoney = Mathf.RoundToInt(currentMoney);
            
            // 재화가 변경될 때마다 UI 업데이트 및 사운드 재생
            if (displayMoney > lastDisplayed)
            {
                lastDisplayed = displayMoney;
                _moneyText.text = $"{displayMoney}";
                
                // 매 단위마다 사운드 재생 (너무 많으면 5단위마다)
                int moneyDiff = targetMoney - (isFirstDisplay ? 0 : lastDisplayedMoney);
                if (moneyDiff > 50)
                {
                    if (displayMoney % 5 == 0 && AudioManager.Inst != null)
                    {
                        AudioManager.Inst.PlayOneShot("SFX_UI_LeaderboardRatingText");
                    }
                }
                else
                {
                    if (AudioManager.Inst != null)
                    {
                        AudioManager.Inst.PlayOneShot("SFX_UI_LeaderboardRatingText");
                    }
                }
            }
            
            yield return null;
        }
        
        // 최종 값 설정
        _moneyText.text = $"{targetMoney}";
    }
    
    /// <summary>
    /// Money 애니메이션 정리
    /// </summary>
    private void CleanupMoneyAnimations()
    {
        moneyImageBounceTween?.Kill();
        moneyImageBounceTween = null;
        
        moneyTextCountTween?.Kill();
        moneyTextCountTween = null;
    }
}
