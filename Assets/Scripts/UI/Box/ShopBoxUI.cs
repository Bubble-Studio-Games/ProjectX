using System;
using UnityEngine;
using static Define;

public class ShopBoxUI : UI_Base
{
    public enum Images { Icon, BG, }
    public enum Texts { ItemNameText, ItemPriceText, }

    [Header("색상 설정")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _hoverColor = new Color(1f, 0.906f, 0.620f); // #FFE79E
    [SerializeField] private Color _selectedColor = new Color32(0xC1, 0xFF, 0x28, 0xFF); // #C1FF28

    private bool _isSelected;
    private bool _isHovered;
    private Action<ShopBoxUI> _onSelected;

    public Shop.Data ShopData { get; private set; }
    public Item.Data ItemData { get; private set; }
    public bool IsSelected => _isSelected;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindImage(typeof(Images));
        BindText(typeof(Texts));

        BindEvent(gameObject, OnBoxClicked, UIEvent.Click);
        BindEvent(gameObject, OnHoverEnter, UIEvent.Hover);
        BindEvent(gameObject, OnHoverExit, UIEvent.HoverExit);

        SetHover(false);
        SetSelected(false);

        return true;
    }

    protected override void OnDestroy()
    {
        _onSelected = null;
        ShopData = null;
        ItemData = null;
        base.OnDestroy();
    }

    public void SetUp(Shop.Data shopData, Action<ShopBoxUI> onSelected)
    {
        Init();
        _onSelected = onSelected;
        UpdateData(shopData);
    }

    public void UpdateData(Shop.Data shopData)
    {
        ShopData = shopData;

        if (shopData == null)
        {
            ItemData = null;
            ClearDisplay();
            return;
        }

        var itemData = Managers.Data.ItemData;
        if (itemData.TryGetValue(shopData.Item_ID, out var item) == false)
        {
            ItemData = null;
            ClearDisplay();
            return;
        }

        ItemData = item;
        UpdateIcon();
        UpdateItemName();
        UpdatePrice();
    }

    private void UpdateIcon()
    {
        var iconImage = GetImage((int)Images.Icon);
        if (ItemData == null)
        {
            iconImage.gameObject.SetActive(false);
            return;
        }

        iconImage.gameObject.SetActive(true);
        iconImage.sprite = Managers.Resource.Load<Sprite>(ResourceKeys.ITEM_ICON_PATH + ItemData.Item_ID);
    }

    private void UpdateItemName()
    {
        var nameText = GetText((int)Texts.ItemNameText);
        if (ItemData == null)
        {
            nameText.gameObject.SetActive(false);
            return;
        }

        nameText.gameObject.SetActive(true);
        nameText.text = ItemData.Item_Name;
    }

    private void UpdatePrice()
    {
        var priceText = GetText((int)Texts.ItemPriceText);
        if (ShopData == null)
        {
            priceText.gameObject.SetActive(false);
            return;
        }

        priceText.gameObject.SetActive(true);
        int price = Managers.Shop.GetItemPrice(ShopData);
        priceText.text = price.ToString();
    }

    private void ClearDisplay()
    {
        GetImage((int)Images.Icon).gameObject.SetActive(false);
        GetText((int)Texts.ItemNameText).gameObject.SetActive(false);
        GetText((int)Texts.ItemPriceText).gameObject.SetActive(false);
    }

    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;

        ApplyBGColor();
    }

    private void SetHover(bool isHovered)
    {
        _isHovered = isHovered;
        ApplyBGColor();
    }

    private void ApplyBGColor()
    {
        var bgImage = GetImage((int)Images.BG);
        if (bgImage == null)
            return;

        if (_isSelected)
        {
            bgImage.color = _selectedColor;
            return;
        }

        bgImage.color = _isHovered ? _hoverColor : _normalColor;
    }

    private void OnHoverEnter()
    {
        SetHover(true);
    }

    private void OnHoverExit()
    {
        SetHover(false);
    }

    private void OnBoxClicked()
    {
        if (ShopData == null)
            return;

        _onSelected?.Invoke(this);
    }
}
