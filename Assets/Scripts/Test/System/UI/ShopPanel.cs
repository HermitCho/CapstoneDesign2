using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.Heat;

public class ShopPanel : MonoBehaviour
{
    #region UI 컴포넌트들
    [Header("ShopPanel 버튼 컴포넌트들")]
    [SerializeField] private ShopButtonManager[] buffShopButtons;
    [SerializeField] private ShopButtonManager[] debuffShopButtons;

    [Header("ShopPanel 아이템 컴포넌트들")]
    [SerializeField] private GameObject[] buffItems;
    [SerializeField] private GameObject[] debuffItems;

    [Header("ShopPanel UI 컴포넌트들")]
    [SerializeField] private TextMeshProUGUI coinText;
    #endregion

    #region Unity 생명주기

    void Start()
    {
        InitializeShopPanel();
    }

    #endregion

    #region 초기화

    /// <summary>
    /// 상점 패널 초기화
    /// </summary>
    private void InitializeShopPanel()
    {
        try
        {
            // 버프 아이템 데이터 할당
            AssignItemDataToButtons(buffItems, buffShopButtons, "Buff");
            
            // 디버프 아이템 데이터 할당
            AssignItemDataToButtons(debuffItems, debuffShopButtons, "Debuff");
            
            Debug.Log("✅ ShopPanel - 상점 패널 초기화 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ShopPanel - 초기화 중 오류: {e.Message}");
        }
    }

    #endregion

    #region 아이템 데이터 할당

    /// <summary>
    /// 아이템 데이터를 ShopButtonManager에 할당
    /// </summary>
    /// <param name="items">아이템 프리팹 배열</param>
    /// <param name="buttons">ShopButtonManager 배열</param>
    /// <param name="itemType">아이템 타입 (Buff/Debuff)</param>
    private void AssignItemDataToButtons(GameObject[] items, ShopButtonManager[] buttons, string itemType)
    {
        if (items == null || buttons == null)
        {
            Debug.LogWarning($"⚠️ ShopPanel - {itemType} 아이템 또는 버튼 배열이 null입니다.");
            return;
        }

        int maxCount = Mathf.Min(items.Length, buttons.Length);
        
        for (int i = 0; i < maxCount; i++)
        {
            if (items[i] == null)
            {
                Debug.LogWarning($"⚠️ ShopPanel - {itemType} 아이템 인덱스 {i}가 null입니다.");
                continue;
            }

            if (buttons[i] == null)
            {
                Debug.LogWarning($"⚠️ ShopPanel - {itemType} 버튼 인덱스 {i}가 null입니다.");
                continue;
            }

            try
            {
                // CharacterItem 컴포넌트에서 데이터 가져오기
                CharacterItem itemComponent = items[i].GetComponent<CharacterItem>();
                if (itemComponent == null)
                {
                    Debug.LogError($"❌ ShopPanel - {itemType} 아이템 인덱스 {i}에 CharacterItem 컴포넌트가 없습니다.");
                    continue;
                }

                // ShopButtonManager에 데이터 할당
                AssignButtonData(buttons[i], itemComponent, i);
                
                Debug.Log($"✅ ShopPanel - {itemType} 아이템 인덱스 {i} 데이터 할당 완료: {itemComponent.SkillName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ ShopPanel - {itemType} 아이템 인덱스 {i} 데이터 할당 중 오류: {e.Message}");
            }
        }
    }

    /// <summary>
    /// ShopButtonManager에 아이템 데이터 할당
    /// </summary>
    /// <param name="button">ShopButtonManager</param>
    /// <param name="itemComponent">CharacterItem 컴포넌트</param>
    /// <param name="itemIndex">아이템 인덱스</param>
    private void AssignButtonData(ShopButtonManager button, CharacterItem itemComponent, int itemIndex)
    {
        if (button == null || itemComponent == null)
        {
            Debug.LogError("❌ ShopPanel - 버튼 또는 아이템 컴포넌트가 null입니다.");
            return;
        }

        try
        {
            // 아이템 데이터 가져오기 (Skill 클래스의 속성들 사용)
            Sprite skillIcon = itemComponent.SkillIcon;
            string skillName = itemComponent.SkillName;
            string skillDescription = itemComponent.SkillDescription;
            int price = itemComponent.GetPrice();

            // ShopButtonManager에 데이터 할당
            button.SetIcon(skillIcon);
            button.SetText(skillName);
            button.SetPrice(price.ToString());

            // 설명 텍스트 설정 (descriptionObj가 있는 경우)
            if (button.descriptionObj != null)
            {
                button.descriptionObj.text = skillDescription;
            }

            // UI 업데이트
            button.UpdateUI();
            
            Debug.Log($"✅ ShopPanel - 버튼 데이터 할당 완료: {skillName} (가격: {price})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ShopPanel - 버튼 데이터 할당 중 오류: {e.Message}");
        }
    }

    #endregion

    #region 공개 메서드

    /// <summary>
    /// 상점 패널 UI 새로고침
    /// </summary>
    public void RefreshShopPanel()
    {
        InitializeShopPanel();
    }

    /// <summary>
    /// 코인 텍스트 업데이트
    /// </summary>
    /// <param name="coinAmount">코인 수량</param>
    public void UpdateCoinText(int coinAmount)
    {
        if (coinText != null)
        {
            coinText.text = coinAmount.ToString();
        }
    }

    #endregion
}
