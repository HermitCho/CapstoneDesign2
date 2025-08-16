using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.Heat;

public class ShopPanel : MonoBehaviour
{
    [Header("ShopPanel 버튼 컴포넌트들")]
    [SerializeField] private ShopButtonManager[] buffShopButtons;
    [SerializeField] private ShopButtonManager[] debuffShopButtons;

    [Header("ShopPanel 아이템 컴포넌트들")]
    [SerializeField] private GameObject[] buffItems;
    [SerializeField] private GameObject[] debuffItems;

    [Header("ShopPanel UI 컴포넌트들")]
    [SerializeField] private TextMeshProUGUI coinText;

    void Start()
    {
        InitializeShopPanel();
    }

    private void InitializeShopPanel()
    {
        try
        {
            AssignItemDataToButtons(buffItems, buffShopButtons, "Buff");
            AssignItemDataToButtons(debuffItems, debuffShopButtons, "Debuff");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ShopPanel - 초기화 중 오류: {e.Message}");
        }
    }

    private void AssignItemDataToButtons(GameObject[] items, ShopButtonManager[] buttons, string itemType)
    {
        if (items == null || buttons == null) return;

        int maxCount = Mathf.Min(items.Length, buttons.Length);
        
        for (int i = 0; i < maxCount; i++)
        {
            if (items[i] == null || buttons[i] == null) continue;

            try
            {
                CharacterItem itemComponent = items[i].GetComponent<CharacterItem>();
                if (itemComponent == null) continue;

                AssignButtonData(buttons[i], itemComponent, i);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ ShopPanel - {itemType} 아이템 인덱스 {i} 데이터 할당 중 오류: {e.Message}");
            }
        }
    }

    private void AssignButtonData(ShopButtonManager button, CharacterItem itemComponent, int itemIndex)
    {
        if (button == null || itemComponent == null) return;

        try
        {
            Sprite skillIcon = itemComponent.SkillIcon;
            string skillName = itemComponent.SkillName;
            string skillDescription = itemComponent.SkillDescription;
            int price = itemComponent.GetPrice();

            button.SetIcon(skillIcon);
            button.SetText(skillName);
            button.SetPrice(price.ToString());

            if (button.descriptionObj != null)
            {
                button.descriptionObj.text = skillDescription;
            }

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
