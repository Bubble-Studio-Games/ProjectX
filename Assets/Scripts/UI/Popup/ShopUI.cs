using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

public class ShopUI : UI_Popup, IAsyncCloseable
{
    public enum Buttons { BuyButton, }
    public Action OnClose { get; set; }
    private string _shopId;
    private List<Shop.Data> _shopItems;
    private List<ShopBoxUI> _shopBoxList = new();
    private ShopBoxUI _selectedBox;

    public string ShopId => _shopId;
    public List<Shop.Data> ShopItems => _shopItems;
    public ShopBoxUI SelectedBox => _selectedBox;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindButton(typeof(Buttons));
        GetButton((int)Buttons.BuyButton).onClick.AddListener(OnBuyButtonClicked);

        _shopBoxList = GetComponentsInChildren<ShopBoxUI>().ToList();

        return true;
    }

    public void SetShopData(string shopId, List<Shop.Data> shopItems)
    {
        _shopId = shopId;
        _shopItems = shopItems;
        ClearSelection();
        RefreshUI();
    }

    public override void RefreshUI()
    {
        base.RefreshUI();

        if (_shopItems == null)
            return;

        // 각 ShopBoxUI에 데이터 연동
        for (int i = 0; i < _shopBoxList.Count; i++)
        {
            var box = _shopBoxList[i];
            if (i < _shopItems.Count)
                box.SetUp(_shopItems[i], OnShopBoxSelected);
            else
                box.SetUp(null, null);
        }
    }

    /// <summary>
    /// 상점 박스 선택 처리 - 단일 선택만 허용
    /// </summary>
    private void OnShopBoxSelected(ShopBoxUI selectedBox)
    {
        // 동일 박스 재클릭 시 무시
        if (_selectedBox == selectedBox)
            return;

        // 이전 선택 해제
        if (_selectedBox != null)
            _selectedBox.SetSelected(false);

        // 새 선택 설정
        _selectedBox = selectedBox;
        _selectedBox.SetSelected(true);
    }

    public void ClearSelection()
    {
        if (_selectedBox != null)
        {
            _selectedBox.SetSelected(false);
            _selectedBox = null;
        }
    }

    /// <summary>
    /// 구매 버튼 클릭 처리
    /// </summary>
    private void OnBuyButtonClicked()
    {
        // 선택된 아이템 없음
        if (_selectedBox == null || _selectedBox.ShopData == null)
        {
            Debug.LogWarning("구매할 아이템을 선택하세요");
            return;
        }

        var shopData = _selectedBox.ShopData;

        // 구매 시도
        bool success = Managers.Shop.TryPurchase(shopData, 1);
        if (success)
        {
            Debug.Log($"구매 완료: {_selectedBox.ItemData?.Item_Name}");

            // UI 갱신
            RefreshUI();

            // 재고 소진 시 선택 해제
            if (shopData.Stock == 0)
                ClearSelection();
        }
        else
        {
            Debug.LogWarning("구매 실패: 조건 불충족");
        }
    }

    public override void ClosePopupUI()
    {
        ClearSelection();
        OnClose?.Invoke();
        OnClose = null;
        base.ClosePopupUI();
    }

    private void OnDestroy()
    {
        _shopBoxList.Clear();
        _selectedBox = null;
    }
}
