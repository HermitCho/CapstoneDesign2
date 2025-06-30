using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : Singleton<GameManager>
{
    [Header("테디베어 점수 관리")]
    [SerializeField] private float totalTeddyBearScore = 0f;
    private TestTeddyBear currentTeddyBear;
    
    // 점수 업데이트 이벤트 (HeatUI에서 구독 가능)
    public static event Action<float> OnScoreUpdated;
    public static event Action<float> OnScoreMultiplierUpdated;
    
    // Start is called before the first frame update
    void Start()
    {
        // 테디베어 찾기
        FindTeddyBear();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // 테디베어 찾기
    void FindTeddyBear()
    {
        if (currentTeddyBear == null)
        {
            currentTeddyBear = FindObjectOfType<TestTeddyBear>();
            if (currentTeddyBear != null)
            {
                Debug.Log("테디베어를 찾았습니다!");
            }
        }
    }
    
    // 테디베어 점수 업데이트 (TestTeddyBear에서 호출)
    public void UpdateTeddyBearScore(float newScore)
    {
        totalTeddyBearScore = newScore;
        
        // HeatUI에 점수 업데이트 이벤트 발생
        OnScoreUpdated?.Invoke(totalTeddyBearScore);
        
        // 점수 배율도 함께 업데이트
        if (currentTeddyBear != null)
        {
            OnScoreMultiplierUpdated?.Invoke(currentTeddyBear.GetCurrentScoreMultiplier());
        }
    }
    
    // 현재 테디베어 점수 가져오기
    public float GetTeddyBearScore()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.GetCurrentScore();
        }
        return totalTeddyBearScore;
    }
    
    // 현재 점수 배율 가져오기
    public float GetScoreMultiplier()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.GetCurrentScoreMultiplier();
        }
        return 1f;
    }
    
    // 게임 시간 가져오기
    public float GetGameTime()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.GetGameTime();
        }
        return Time.time;
    }
    
    // 테디베어가 부착되어 있는지 확인
    public bool IsTeddyBearAttached()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.IsAttached();
        }
        return false;
    }
    
    // 테디베어 재부착까지 남은 시간 가져오기
    public float GetTimeUntilReattach()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.GetTimeUntilReattach();
        }
        return 0f;
    }
    
    // 테디베어 재부착 가능 여부 확인
    public bool CanTeddyBearReattach()
    {
        if (currentTeddyBear != null)
        {
            return currentTeddyBear.CanReattach();
        }
        return true;
    }
    
    // 점수 초기화 (개발자용)
    public void ResetAllScores()
    {
        totalTeddyBearScore = 0f;
        if (currentTeddyBear != null)
        {
            currentTeddyBear.ResetScore();
        }
        OnScoreUpdated?.Invoke(0f);
        OnScoreMultiplierUpdated?.Invoke(1f);
    }
}
