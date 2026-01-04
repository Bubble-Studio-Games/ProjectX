using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점 관리 - 상점 데이터, 구매/판매, 재고 관리
/// </summary>
public class ShopManager
{
    private Dictionary<string, List<Shop.Data>> _shopData;
    private string _currentShopId;
    private ShopUI _shopUI;
    private InventoryUI _inventoryUI;

    public event Action<string, int> OnItemPurchased;
    public event Action<string, int> OnItemSold;

    public string CurrentShopId => _currentShopId;
    public bool IsShopOpen => _shopUI != null;

    public void Init()
    {
        _shopData = Managers.Data.ShopData;
    }

    public void Clear()
    {
        _currentShopId = null;
        _shopUI = null;
        _inventoryUI = null;
        OnItemPurchased = null;
        OnItemSold = null;
    }

    /// <summary>
    /// 상점 열기 - ShopUI와 InventoryUI 표시 후 IAsyncCloseable 반환
    /// </summary>
    public (ShopUI shopUI, InventoryUI inventoryUI) OpenShop(string shopId)
    {
        if (_shopData.TryGetValue(shopId, out var shopItems) == false)
        {
            Debug.LogError($"상점을 찾을 수 없습니다: {shopId}");
            return (null, null);
        }

        _currentShopId = shopId;

        // ShopUI는 Barrier 활성화 (뒤쪽 클릭 차단)
        _shopUI = Managers.UI.ShowPopupUI<ShopUI>();
        _shopUI.SetShopData(shopId, shopItems);

        // InventoryUI는 Barrier 비활성화 (ShopUI의 Barrier가 이미 차단)
        _inventoryUI = Managers.UI.ShowPopupUI<InventoryUI>();
        _inventoryUI.SetBarrierActive(false);

        return (_shopUI, _inventoryUI);
    }

    /// <summary>
    /// 상점 닫힘 처리 - DialogueManager에서 UI 닫힘 후 호출
    /// </summary>
    public void OnShopClosed()
    {
        _currentShopId = null;
        _shopUI = null;
        _inventoryUI = null;
    }

    /// <summary>
    /// 특정 상점의 아이템 목록 반환
    /// </summary>
    public List<Shop.Data> GetShopItems(string shopId)
    {
        if (_shopData.TryGetValue(shopId, out var items))
            return items;

        return null;
    }

    /// <summary>
    /// 상점 아이템의 실제 가격 반환 - Price_Override가 -1이면 기본가 사용
    /// </summary>
    public int GetItemPrice(Shop.Data shopData)
    {
        if (shopData.Price_Override >= 0)
            return shopData.Price_Override;

        var itemData = Managers.Data.ItemData;
        if (itemData.TryGetValue(shopData.Item_ID, out var item))
            return item.Price_Buy;

        return 0;
    }

    /// <summary>
    /// 아이템 판매 가격 반환
    /// </summary>
    public int GetSellPrice(string itemId)
    {
        var itemData = Managers.Data.ItemData;
        if (itemData.TryGetValue(itemId, out var item))
            return item.Price_Sell;

        return 0;
    }

    /// <summary>
    /// 해당 아이템이 구매 가능한지 확인 - 재고 및 해금 조건 체크
    /// </summary>
    public bool CanPurchase(Shop.Data shopData, int quantity = 1)
    {
        // 재고 확인 (-1이면 무한)
        if (shopData.Stock >= 0 && shopData.Stock < quantity)
            return false;

        // 해금 조건 확인
        if (string.IsNullOrEmpty(shopData.Unlock_Condition) == false)
        {
            if (CheckUnlockCondition(shopData.Unlock_Condition) == false)
                return false;
        }

        int totalPrice = GetItemPrice(shopData) * quantity;
        if (Managers.Inventory.Gold.CanSpend(totalPrice) == false)
            return false;

        return true;
    }

    /// <summary>
    /// 해금 조건 체크 - 퀘스트 완료 등
    /// </summary>
    private bool CheckUnlockCondition(string condition)
    {
        if (string.IsNullOrEmpty(condition))
            return true;

        // 퀘스트 완료 조건 체크 (예: QST_Main_001_Complete)
        if (condition.EndsWith("_Complete"))
        {
            string questId = condition.Replace("_Complete", "");
            var status = Managers.Quest.GetQuestStatus(questId);
            bool ret = status == QuestData.E_QuestStatus.Completed || status == QuestData.E_QuestStatus.Finished;
            return ret;
        }

        return true;
    }

    /// <summary>
    /// 아이템 구매 시도
    /// </summary>
    public bool TryPurchase(Shop.Data shopData, int quantity = 1)
    {
        if (CanPurchase(shopData, quantity) == false)
            return false;

        int totalPrice = GetItemPrice(shopData) * quantity;

        if (Managers.Inventory.Gold.TrySpend(totalPrice) == false)
            return false;

        // 인벤토리에 아이템 추가
        var itemData = Managers.Data.ItemData;
        if (itemData.TryGetValue(shopData.Item_ID, out var item))
        {
            for (int i = 0; i < quantity; i++)
                Managers.Inventory.AddItem(item);
        }

        // 재고 감소 (무한 재고가 아닌 경우)
        if (shopData.Stock > 0)
            shopData.Stock -= quantity;

        OnItemPurchased?.Invoke(shopData.Item_ID, quantity);
        return true;
    }

    /// <summary>
    /// 아이템 판매 시도
    /// </summary>
    public bool TrySell(Item.Data itemData, int quantity = 1)
    {
        // 판매 가능 여부 확인
        if (itemData.Is_Sellable == 0)
            return false;

        int totalPrice = itemData.Price_Sell * quantity;

        // 인벤토리에서 아이템 제거
        for (int i = 0; i < quantity; i++)
            Managers.Inventory.RemoveItem(itemData);

        Managers.Inventory.Gold.Add(totalPrice);

        OnItemSold?.Invoke(itemData.Item_ID, quantity);
        return true;
    }
}
