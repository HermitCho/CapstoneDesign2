using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.Heat;

public class ShopPanel : MonoBehaviour
{
    [Header("ShopPanel 버튼 컴포넌트들")]
    [SerializeField] private ButtonManager[] ShopButtons;

    [Header("ShopPanel 아이템 컴포넌트들")]
    [SerializeField] private GameObject[] Items;

    [Header("ShopPanel UI 컴포넌트들")]
    [SerializeField] private TextMeshProUGUI coinText;

    [Header("Item Description 모달 컴포넌트")]
    [SerializeField] private ModalWindowManager itemDescriptionModal;


    void Start()
    {
        InitializeShopPanel();
    }

    private void InitializeShopPanel()
    {
        try
        {
            AssignItemDataToButtons(Items, ShopButtons);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ShopPanel - 초기화 중 오류: {e.Message}");
        }
    }

    private void AssignItemDataToButtons(GameObject[] items, ButtonManager[] buttons)
    {
        if (items == null || buttons == null) return;

        int maxCount = Mathf.Min(items.Length, buttons.Length);
        
        for (int i = 0; i < maxCount; i++)
        {
            if (items[i] == null || buttons[i] == null) continue;

            try
            {
                Skill itemComponent = items[i].GetComponent<Skill>();
                if (itemComponent == null) continue;

                AssignButtonData(buttons[i], itemComponent, i);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ ShopPanel - 아이템 인덱스 {i} 데이터 할당 중 오류: {e.Message}");
            }
        }
    }

    public void OnHoverItemButton(int index)
    {
        itemDescriptionModal.OpenWindow();

        
        itemDescriptionModal.icon = Items[index].GetComponent<Skill>().SkillIcon;

        itemDescriptionModal.descriptionText = Items[index].GetComponent<Skill>().SkillDescription;
        itemDescriptionModal.UpdateUI();
    }

    public void OnLeaveItemButton()
    {
        //itemDescriptionModal.icon = null;
        //itemDescriptionModal.descriptionText = "";
        itemDescriptionModal.CloseWindow();
        itemDescriptionModal.UpdateUI();
    }

    private void AssignButtonData(ButtonManager button, Skill itemComponent, int itemIndex)
    {
        if (button == null || itemComponent == null) return;

        try
        {
            Sprite skillIcon = itemComponent.SkillIcon;
            string skillPrice = itemComponent.Price.ToString();

            button.SetIcon(skillIcon);
            button.SetText(skillPrice);


            button.UpdateUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ShopPanel - 버튼 데이터 할당 중 오류: {e.Message}");
        }
    }

    public void RefreshShopPanel()
    {
        InitializeShopPanel();
    }

    public void UpdateCoinText(int coinAmount)
    {
        if (coinText != null)
        {
            coinText.text = coinAmount.ToString();
        }
    }
}
