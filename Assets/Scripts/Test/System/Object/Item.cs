using UnityEngine;

/// <summary>
/// 아이템 데이터 컨테이너 - UI 처리는 ShopStand.cs에서 담당
/// </summary>
public class Item : MonoBehaviour
{
    [Header("아이템 기본 설정")]
    [SerializeField] private float renewTime = 30f;
    [SerializeField] [Range(0f, 1f)] private float probability = 0.5f;
    
    // 아이템 스킬 컴포넌트 (자동으로 찾음)
    private Skill itemSkill;
    [SerializeField] private GameObject itemObject;

    public float RenewTime => renewTime;
    public float Probability => probability;
    public Skill ItemSkill => itemSkill;
    public GameObject ItemObject => itemObject;

    void Start()
    {
        InitializeItem();
    }

    /// <summary>
    /// 아이템 초기화
    /// </summary>
    void InitializeItem()
    {
        // Skill 컴포넌트 자동 찾기
        if (itemSkill == null)
        {
            itemSkill = itemObject.GetComponent<Skill>();
        }
        
        if (itemSkill == null)
        {
            Debug.LogError($"Item: {itemObject.name}에 Skill 컴포넌트가 없습니다.");
            return;
        }
        
        Debug.Log($"Item: {itemObject.name} 초기화 완료 - Skill: {itemSkill.SkillName}");
    }


    /// <summary>
    /// 아이템 구매 시도 (ShopStand을 통해 호출)
    /// </summary>
    /// <param name="buyer">구매자</param>
    /// <returns>구매 성공 여부</returns>
    public bool TryPurchase(ShopController buyer)
    {
        if (buyer == null || itemSkill == null) return false;

        // 상점 오브젝트 찾기
        Shop shop = FindObjectOfType<Shop>();
        if (shop == null)
        {
            Debug.LogError("Item: Shop 오브젝트를 찾을 수 없습니다.");
            return false;
        }

        // 상점을 통해 구매 처리
        shop.PurchaseItem(gameObject, buyer);
        return true;
    }

    /// <summary>
    /// 아이템이 구매 가능한지 확인
    /// </summary>
    /// <param name="buyer">구매자</param>
    /// <returns>구매 가능 여부</returns>
    public bool CanPurchase(ShopController buyer)
    {
        if (buyer == null || itemSkill == null) return false;

        CoinController coinController = buyer.GetComponent<CoinController>();
        ItemController itemController = buyer.GetComponent<ItemController>();

        if (coinController == null || itemController == null) return false;

        // 구매 조건 확인
        if (coinController.GetCoin() < itemSkill.Price) return false;
        if (itemController.HasItemByIndex(itemSkill.Index)) return false;
        if (itemController.GetItemSlotIndex() >= itemController.GetMaxItemSlot()) return false;

        return true;
    }
}
