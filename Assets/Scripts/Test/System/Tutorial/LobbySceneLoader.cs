using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class LobbySceneLoader : MonoBehaviour
{
    // 🔹 OnClick()에 이 함수를 연결하면 로비씬으로 이동합니다.
    public void LoadLobbyScene()
    {
        StartCoroutine(SafeDisconnectAndLoadLobby());
    }
    
    /// <summary>
    /// 포톤 서버 연결을 안전하게 해제한 후 로비씬으로 이동
    /// </summary>
    private IEnumerator SafeDisconnectAndLoadLobby()
    {
        Debug.Log("LobbySceneLoader: 포톤 서버 연결 해제 시작");
        
        // 로컬 플레이어 Properties 정리
        if (PhotonNetwork.LocalPlayer != null)
        {
            var props = new ExitGames.Client.Photon.Hashtable();
            props["playerReady"] = null;
            props["nickname"] = null;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
        
        // 방에서 나가기
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            
            // 방 나가기 완료 대기 (최대 3초)
            float timeout = 3f;
            float timer = 0f;
            
            while (PhotonNetwork.InRoom && timer < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            
            if (PhotonNetwork.InRoom)
            {
                Debug.LogWarning("LobbySceneLoader: 방 나가기 타임아웃");
            }
        }
        
        // 로비에서 나가기
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
            
            // 로비 나가기 완료 대기 (최대 2초)
            float timeout = 2f;
            float timer = 0f;
            
            while (PhotonNetwork.InLobby && timer < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            
            if (PhotonNetwork.InLobby)
            {
                Debug.LogWarning("LobbySceneLoader: 로비 나가기 타임아웃");
            }
        }
        
        // Photon 연결 완전 해제
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            
            // 연결 해제 완료 대기 (최대 3초)
            float timeout = 3f;
            float timer = 0f;
            
            while (PhotonNetwork.IsConnected && timer < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            
            if (PhotonNetwork.IsConnected)
            {
                Debug.LogWarning("LobbySceneLoader: 연결 해제 타임아웃");
            }
        }
        
        // 연결 해제 완료 후 로비씬으로 이동
        SceneManager.LoadScene("Lobby");
    }
}