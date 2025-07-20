using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; // 추가

public class LoadingController : MonoBehaviour
{
    public Slider progressBar;
    public TMP_Text progressText; // TMP_Text로 변경
    public string nextSceneName; // 인게임씬 이름

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LoadSceneAsync());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator LoadSceneAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(nextSceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            if (progressBar != null)
                progressBar.value = progress;
            if (progressText != null)
                progressText.text = $"{(progress * 100):0}%";

            // 로딩이 끝나면 자동으로 씬 전환
            if (operation.progress >= 0.9f)
            {
                if (progressBar != null)
                    progressBar.value = 1f;
                if (progressText != null)
                    progressText.text = "100%";
                yield return new WaitForSeconds(0.5f); // 잠깐 대기
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
