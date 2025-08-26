using UnityEngine;
using Photon.Pun;

/// <summary>
/// 플레이어의 점수판 데이터를 관리하는 클래스
/// </summary>
[System.Serializable]
public class PlayerScoreData
{
    public int playerId;
    public string nickname;
    public float score;
    public bool isLocalPlayer;
    public PhotonView playerPhotonView;
    
    public PlayerScoreData(int id, string name, float currentScore, bool isLocal, PhotonView pv)
    {
        playerId = id;
        nickname = name;
        score = currentScore;
        isLocalPlayer = isLocal;
        playerPhotonView = pv;
    }
    
    public void UpdateScore(float newScore)
    {
        score = newScore;
    }
}
