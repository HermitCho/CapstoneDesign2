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

    public void OnLobbyCam()
    {
        Cam.transform.position = LobbyCamPosition.position;
    }

    public void OnSelectCharCam(int index)
    {
        Cam.transform.position = SelectCharCamPositions[index].position;
    }

    public void OnSettingCam()
    {
        Cam.transform.position = SettingCamPosition.position;
    }
}
