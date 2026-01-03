using System.Collections.Generic;
using UGS;
using UnityEngine;

public partial class DataManager
{
    private Dictionary<string, List<Dialogue.Data>> _dialogueData = new();
    public Dictionary<string, List<Dialogue.Data>> DialogueData => _dialogueData;
    
    private Dictionary<string, List<Dialogue.EntryData>> _entryData = new();
    public Dictionary<string, List<Dialogue.EntryData>> EntryData => _entryData;

    private Dictionary<string, Quest.Data> _questData = new();
    public Dictionary<string, Quest.Data> QuestData => _questData;
    
    private Dictionary<string, Item.Data> _itemData = new();
    public Dictionary<string, Item.Data> ItemData => _itemData;

    private Dictionary<string, List<Shop.Data>> _shopData = new();
    public Dictionary<string, List<Shop.Data>> ShopData => _shopData;

    public void Init()
    {
        LoadDialogueData();
        LoadEntryData();
        LoadQuestData();
        LoadItemData();
        LoadShopData();
    }

    private void LoadDialogueData()
    {
        _dialogueData.Clear();

        var dataList = Dialogue.Data.GetList();
        foreach (var data in dataList)
        {
            if (_dialogueData.TryGetValue(data.Dialogue_ID, out var list))
                list.Add(data);
            else
                _dialogueData[data.Dialogue_ID] = new List<Dialogue.Data> { data };
        }

        // 각 대화 그룹 내에서 Line_Index 기준 정렬
        foreach (var kvp in _dialogueData)
        {
            kvp.Value.Sort((a, b) => a.Line_Index.CompareTo(b.Line_Index));
        }
    }

    private void LoadEntryData()
    {
        _entryData.Clear();

        var dataList = Dialogue.EntryData.GetList();
        foreach (var data in dataList)
        {
            if (_entryData.TryGetValue(data.Dialogue_ID, out var list))
                list.Add(data);
            else
                _entryData[data.Dialogue_ID] = new List<Dialogue.EntryData> { data };
        }

        // 각 Dialogue별 Priority 내림차순 정렬
        foreach (var kvp in _entryData)
        {
            kvp.Value.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
    }

    private void LoadQuestData()
    {
        _questData.Clear();
        _questData = Quest.Data.GetDictionary();
    }

    private void LoadItemData()
    {
        _itemData.Clear();
        _itemData = Item.Data.GetDictionary();
    }

    private void LoadShopData()
    {
        _shopData.Clear();

        var dataList = Shop.Data.GetList();
        foreach (var data in dataList)
        {
            if (_shopData.TryGetValue(data.Shop_ID, out var list))
                list.Add(data);
            else
                _shopData[data.Shop_ID] = new List<Shop.Data> { data };
        }
    }

    public void Clear()
    {
        _dialogueData?.Clear();
        _entryData?.Clear();
        _questData?.Clear();
        _shopData?.Clear();
    }

}

