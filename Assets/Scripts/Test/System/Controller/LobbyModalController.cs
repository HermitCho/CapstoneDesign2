using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Heat;

public class LobbyModalController : MonoBehaviour
{
    [Header("능력 Modal Window 할당")]
    [SerializeField] private ModalWindowManager abilityModalWindow;

    [Header("총 Modal Window 할당")]
    [SerializeField] private ModalWindowManager gunModalWindow;

    [Header("능력 설명")]
    [Tooltip("0 index : 1번째 캐릭터 능력, 1 index : 2번째 캐릭터 능력 . . . 총 4개 캐릭터 능력")]
    [SerializeField] private string[] abilityDescription;

    [Header("총 설명")]
    [Tooltip("0 index : 1번째 총, 1 index : 2번째 총 . . . 총 4개 총")]
    [SerializeField] private string[] gunDescription;

    private int currnetIndex = 0;

    void Awake()
    {
        // 저장된 모달 인덱스 복원
        currnetIndex = PlayerPrefs.GetInt("LobbyModal_CurrentIndex", 0);
    }

    public void OnHoverAbilityButton()
    {
        if (currnetIndex != -1)
        {
            abilityModalWindow.OpenWindow();
            abilityModalWindow.descriptionText = abilityDescription[currnetIndex];
            abilityModalWindow.UpdateUI();
        }
    }

    public void OnHoverGunButton()
    {
        if (currnetIndex != -1)
        {
            gunModalWindow.OpenWindow();
            gunModalWindow.descriptionText = gunDescription[currnetIndex];
            gunModalWindow.UpdateUI();
        }
    }

    public void OnLeaveAbilityButton()
    {
        if (currnetIndex != -1)
        {
            abilityModalWindow.descriptionText = "";
            abilityModalWindow.UpdateUI();
            abilityModalWindow.CloseWindow();
        }
    }

    public void OnLeaveGunButton()
    {
        if (currnetIndex != -1)
        {
            gunModalWindow.descriptionText = "";
            gunModalWindow.UpdateUI();
            gunModalWindow.CloseWindow();
        }
    }

    public void OnSetIndex(int index)
    {
        currnetIndex = index;
        // 선택한 인덱스 저장
        PlayerPrefs.SetInt("LobbyModal_CurrentIndex", currnetIndex);
        PlayerPrefs.Save();
    }

    public void OnClearIndex()
    {
        currnetIndex = -1;
        // 인덱스 초기화 시에도 저장
        PlayerPrefs.SetInt("LobbyModal_CurrentIndex", currnetIndex);
        PlayerPrefs.Save();
    }

}
