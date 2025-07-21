using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class ClickToSkip : MonoBehaviour
{

    public string lobbySceneName = "Lobby";
    public PlayableDirector timelineDirector; // 타임라인 연결용
    // Start is called before the first frame update
    void Start()
    {
        if (timelineDirector != null)
        {
            timelineDirector.stopped += OnTimelineStopped;
        }
    }

    void OnDestroy()
    {
        if (timelineDirector != null)
        {
            timelineDirector.stopped -= OnTimelineStopped;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 마우스 좌클릭 시
        if (Input.GetMouseButtonDown(0))
        {
            // Loading 씬으로 이동 (씬 이름은 정확히 'Loading')
            LoadingController.LoadWithLoadingScene(lobbySceneName, false); // 싱글(인트로→로비)
        }
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        LoadingController.LoadWithLoadingScene(lobbySceneName, false); // 싱글(인트로→로비)
    }
}
