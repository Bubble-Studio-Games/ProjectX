using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 임시
/// </summary>
public class Gold
{
    private int _amount;

    public int Amount => _amount;

    public event EventHandler<int> OnChanged;

    public void Set(int amount)
    {
        int clamped = amount < 0 ? 0 : amount;
        int delta = clamped - _amount;
        _amount = clamped;
        OnChanged?.Invoke(this, delta);
    }

    public void Add(int amount)
    {
        if (amount <= 0)
            return;

        Set(_amount + amount);
    }

    public bool CanSpend(int amount)
    {
        if (amount <= 0)
            return true;

        return _amount >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0)
            return true;

        if (_amount < amount)
            return false;

        Set(_amount - amount);
        return true;
    }
}
public class InventoryManager : IManager
{

    public void Clear()
    {
    }

    public Gold Gold { get; } = new Gold();

    public event Action OnInventoryChanged;

    private Dictionary<string, Item.Data> _itemData = new();
    private Dictionary<uint, Item.Data> _inventoryItems = new();
    private uint _nextSlotIndex;

    private void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    private uint FindFirstEmptySlot()
    {
        uint slot = 0;
        while (_inventoryItems.ContainsKey(slot))
            slot++;

        return slot;
    }

    public void Init()
    {
        _itemData = Managers.Data.ItemData;
        _inventoryItems.Clear();
        _nextSlotIndex = 0;

        Gold.Set(100000);

        LoadInventorySetting();
    }

    /// <summary>
    /// GlobalSettings에서 시작 아이템 로드
    /// </summary>
    private void LoadInventorySetting()
    {
        var inventorySettings = GameConfig.Inventory;
        if (inventorySettings == null)
        {
            Debug.LogError("[InventoryManager] InventorySettings가 GlobalSettings에 설정되지 않음");
            return;
        }

        inventorySettings.LoadItems(_itemData, (data) => AddItem(data));
    }

    public void AddItem(Item.Data itemData)
    {
        if (itemData == null)
            return;

        uint slot = FindFirstEmptySlot();
        _inventoryItems[slot] = itemData;

        if (slot >= _nextSlotIndex)
            _nextSlotIndex = slot + 1;

        NotifyInventoryChanged();
    }

    public void RemoveItem(Item.Data itemData)
    {
        if (itemData == null)
            return;

        uint? keyToRemove = null;
        foreach (var kvp in _inventoryItems)
        {
            if (kvp.Value != null && kvp.Value.Item_ID == itemData.Item_ID)
            {
                keyToRemove = kvp.Key;
                break;
            }
        }

        if (keyToRemove.HasValue == false)
            return;

        _inventoryItems.Remove(keyToRemove.Value);
        NotifyInventoryChanged();
    }

    public Dictionary<uint, Item.Data> GetInventoryItems()
    {
        return _inventoryItems;
    }

    public void SwapSlots(uint a, uint b)
    {
        if (a == b)
            return;

        _inventoryItems.TryGetValue(a, out var itemA);
        _inventoryItems.TryGetValue(b, out var itemB);

        bool hasA = itemA != null;
        bool hasB = itemB != null;

        if (hasA == false && hasB == false)
            return;

        if (hasA)
            _inventoryItems.Remove(a);
        if (hasB)
            _inventoryItems.Remove(b);

        if (hasA)
            _inventoryItems[b] = itemA;
        if (hasB)
            _inventoryItems[a] = itemB;

        NotifyInventoryChanged();
    }

    /// <summary>
    /// 슬롯 인덱스로 아이템 조회
    /// </summary>
    public Item.Data GetItem(uint slotIndex)
    {
        if (_inventoryItems.TryGetValue(slotIndex, out var itemData))
            return itemData;

        return null;
    }

    /// <summary>
    /// 슬롯 인덱스로 아이템 조회 시도
    /// </summary>
    public bool TryGetItem(uint slotIndex, out Item.Data itemData)
    {
        return _inventoryItems.TryGetValue(slotIndex, out itemData);
    }

    /// <summary>
    /// 아이템 ID로 아이템 조회
    /// </summary>
    public Item.Data GetItemById(string itemId)
    {
        foreach (var kvp in _inventoryItems)
        {
            if (kvp.Value.Item_ID == itemId)
                return kvp.Value;
        }

        return null;
    }

    /// <summary>
    /// 아이템 ID로 아이템 조회 시도
    /// </summary>
    public bool TryGetItemById(string itemId, out Item.Data itemData)
    {
        itemData = GetItemById(itemId);
        var ret = itemData != null;
        return ret;
    }

    /// <summary>
    /// 아이템 ID로 모든 아이템 조회
    /// </summary>
    public List<Item.Data> GetAllItemsById(string itemId)
    {
        var ret = new List<Item.Data>();
        foreach (var kvp in _inventoryItems)
        {
            if (kvp.Value.Item_ID == itemId)
                ret.Add(kvp.Value);
        }

        return ret;
    }

    /// <summary>
    /// 아이템 ID의 개수 조회
    /// </summary>
    public int GetItemCount(string itemId)
    {
        int count = 0;
        foreach (var kvp in _inventoryItems)
        {
            if (kvp.Value.Item_ID == itemId)
                count++;
        }

        return count;
    }

    /// <summary>
    /// 특정 아이템 보유 여부
    /// </summary>
    public bool HasItem(string itemId)
    {
        foreach (var kvp in _inventoryItems)
        {
            if (kvp.Value.Item_ID == itemId)
                return true;
        }

        return false;
    }

}
