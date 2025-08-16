using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyCamController : MonoBehaviour
{
    [Header("카메라 할당")]
    public Camera Cam;
    [Space(10)]

    [Header("카메라 위치 오브젝트 할당")]
    [Tooltip("로비 카메라 위치")]
    public Transform LobbyCamPosition;
    [Space(10)]

    [Tooltip("캐릭터 선택 카메라 위치")]
    public Transform[] SelectCharCamPositions;
    [Space(10)]

    [Tooltip("설정 카메라 위치")]
    public Transform SettingCamPosition;

    private int currentIndex = 0;

    void Awake()
    {
        // 저장된 카메라 인덱스 복원
        currentIndex = PlayerPrefs.GetInt("LobbyCam_CurrentIndex", 0);
    }

    public void OnLobbyCam()
    {
        Cam.transform.position = LobbyCamPosition.position;
    }

    public void OnSelectCharCam(int index)
    {
        if (index >= 0 && index < SelectCharCamPositions.Length)
        {
            Cam.transform.position = SelectCharCamPositions[index].position;
            currentIndex = index;
            // 선택한 캐릭터 인덱스 저장
            PlayerPrefs.SetInt("LobbyCam_CurrentIndex", currentIndex);
            PlayerPrefs.Save();
        }
    }

    public void OnClickSelectChar()
    {
        // 저장된 인덱스를 먼저 복원
        currentIndex = PlayerPrefs.GetInt("LobbyCam_CurrentIndex", currentIndex);
        
        if (currentIndex >= 0 && currentIndex < SelectCharCamPositions.Length)
        {
            Cam.transform.position = SelectCharCamPositions[currentIndex].position;
        }
    }

    public void OnSettingCam()
    {
        Cam.transform.position = SettingCamPosition.position;
    }
}
