using System;
using System.Collections.Generic;

/*
📚 Sheet 데이터 접근 전용 모듈

🔹 추천 사용 패턴
  - 대사 라인 리스트:      Managers.Data.GetDialogueLines(dialogueId)
  - 대사 엔트리(선택지):   Managers.Data.GetDialogueEntries(dialogueId)
  - 퀘스트 한 개:         Managers.Data.GetQuest(questId)
  - 아이템 한 개:         Managers.Data.GetItem(itemId)
  - 상점 아이템 리스트:   Managers.Data.GetShopItems(shopId)
*/
public partial class DataManager
{
    /// <summary>대사 ID 기준으로, 한 대사에 속한 라인들을 순서대로 반환</summary>
    public IReadOnlyList<Dialogue.Data> GetDialogueLines(string dialogueId)
    {
        if (_dialogueData.TryGetValue(dialogueId, out var list))
            return list;
        return Array.Empty<Dialogue.Data>();
    }

    /// <summary>대사 ID 기준으로, 엔트리(선택지/조건문) 리스트를 우선순위 순으로 반환</summary>
    public IReadOnlyList<Dialogue.EntryData> GetDialogueEntries(string dialogueId)
    {
        if (_entryData.TryGetValue(dialogueId, out var list))
            return list;
        return Array.Empty<Dialogue.EntryData>();
    }

    /// <summary>퀘스트 ID로 퀘스트 데이터 1개 반환. 없으면 null</summary>
    public Quest.Data GetQuest(string questId)
    {
        _questData.TryGetValue(questId, out var data);
        return data;
    }

    /// <summary>아이템 ID로 아이템 데이터 1개 반환. 없으면 null</summary>
    public Item.Data GetItem(string itemId)
    {
        _itemData.TryGetValue(itemId, out var data);
        return data;
    }

    /// <summary>상점 ID 기준으로, 상점에 올라간 아이템 목록 반환</summary>
    public IReadOnlyList<Shop.Data> GetShopItems(string shopId)
    {
        if (_shopData.TryGetValue(shopId, out var list))
            return list;
        return Array.Empty<Shop.Data>();
    }

    /// <summary>
    /// 상점 아이템의 실제 가격 계산 유틸
    ///  - Price_Override >= 0 이면 그 값 사용
    ///  - 아니면 ItemData에서 Buy 가격 찾아서 사용
    /// </summary>
    public int GetShopItemPrice(Shop.Data shopData)
    {
        if (shopData.Price_Override >= 0)
            return shopData.Price_Override;

        var item = GetItem(shopData.Item_ID);
        return item != null ? item.Price_Buy : 0;
    }
}


// 시트에서 고정 데이터를 가져온다.
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

    public void Init_SheetData()
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

    public void ClearSheetData()
    {
        _dialogueData?.Clear();
        _entryData?.Clear();
        _questData?.Clear();
        _shopData?.Clear();
        _itemData?.Clear();
    }

}

