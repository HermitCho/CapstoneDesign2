using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingController : MonoBehaviour
{
    public Slider progressBar;
    public TMP_Text progressText;
    public static string nextSceneName;
    public static bool usePhotonSync = false;

    private static bool isLoadingNextScene = false;

    void Start()
    {
        isLoadingNextScene = false; // 씬 진입 시 항상 리셋
        Debug.Log("LoadingController Start() 진입");
        Debug.Log("nextSceneName: " + nextSceneName);
        Debug.Log("usePhotonSync: " + usePhotonSync);
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        // 로딩 UI 표시(가짜)
        float timer = 0f;
        float fakeLoadTime = 2f;

        while (timer < fakeLoadTime)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / fakeLoadTime);
            if (progressBar != null)
                progressBar.value = progress;
            if (progressText != null)
                progressText.text = $"{(progress * 100):0}%";
            yield return null;
        }

        if (progressBar != null)
            progressBar.value = 1f;
        if (progressText != null)
            progressText.text = "100%";
        yield return new WaitForSeconds(0.5f);

        if (isLoadingNextScene) yield break;
        isLoadingNextScene = true;

        if (usePhotonSync)
        {
            Debug.Log("PhotonNetwork.LoadLevel 호출: " + nextSceneName);
            Photon.Pun.PhotonNetwork.LoadLevel(nextSceneName);
        }
        else
        {
            Debug.Log("SceneManager.LoadScene 호출: " + nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
    }

    public static void LoadWithLoadingScene(string nextScene, bool usePhotonSync)
    {
        LoadingController.nextSceneName = nextScene;
        LoadingController.usePhotonSync = usePhotonSync;
        SceneManager.LoadScene("Loading");
    }
}
