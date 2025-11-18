using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbySceneLoader : MonoBehaviour
{
    // 🔹 OnClick()에 이 함수를 연결하면 로비씬으로 이동합니다.
    public void LoadLobbyScene()
    {
        SceneManager.LoadScene("Lobby"); // 씬 이름 그대로 입력
    }
}