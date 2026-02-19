using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 시작 시 기본 인벤토리 구성 설정
/// </summary>
[CreateAssetMenu(fileName = "InventorySettings", menuName = "Game/Config/Inventory")]
public class InventorySettings : ScriptableObject
{
    [System.Serializable]
    public class ItemEntry
    {
        [Header("아이템 설정")]
        [Tooltip("Item.Data의 Item_ID")]
        public string ItemId;

        [Range(1, 99)]
        public int Count = 1;
    }

    [Header("시작 아이템 목록")]
    public List<ItemEntry> Items = new();

    /// <summary>
    /// 아이템 로드 - Action 콜백으로 아이템 추가
    /// </summary>
    public void LoadItems(Dictionary<string, Item.Data> itemData, Action<Item.Data> onAddItem)
    {
        if (itemData == null || onAddItem == null)
            return;

        foreach (var entry in Items)
        {
            if (itemData.TryGetValue(entry.ItemId, out var data))
            {
                for (int i = 0; i < entry.Count; i++)
                {
                    onAddItem.Invoke(data);
                }
            }
            else
            {
                Debug.LogWarning($"[InventorySettings] 아이템을 찾을 수 없음: {entry.ItemId}");
            }
        }
    }
}
