using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using static Define;

public class InventoryUI : UI_Popup, IAsyncCloseable, IItemHoverPolicy
{
    public enum Buttons { CloseButton, }
    public enum Texts { GoldText,}

    [SerializeField] private ItemHoverPanel _hoverPanel;
    [SerializeField] private GhostImagePanel _ghostImage;

    private List<ItemBoxUI> _itemList = new();
    private ItemBoxUI _dragSource;
    private bool _isDragging;
    private readonly List<RaycastResult> _raycastResults = new();
    public Action OnClose { get; set; }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindButton(typeof(Buttons));
        BindText(typeof(Texts));
        
        GetButton((int)Buttons.CloseButton).onClick.AddListener(OnCloseButtonClicked);


        return true;
    }

    private void OnEnable()
    {
        _itemList = GetComponentsInChildren<ItemBoxUI>().ToList();

        Managers.Inventory.Gold.OnChanged -= OnGoldChanged;
        Managers.Inventory.Gold.OnChanged += OnGoldChanged;

        Managers.Inventory.OnInventoryChanged -= OnInventoryChanged;
        Managers.Inventory.OnInventoryChanged += OnInventoryChanged;

        foreach (var itemBox in _itemList)
            itemBox.SetOwner(this);

        if (_ghostImage != null)
            _ghostImage.Hide();

        RefreshUI();
    }

    private void OnDisable()
    {
        if (Managers.Inventory != null && Managers.Inventory.Gold != null)
            Managers.Inventory.Gold.OnChanged -= OnGoldChanged;

        if (Managers.Inventory != null)
            Managers.Inventory.OnInventoryChanged -= OnInventoryChanged;
    }

    public override void RefreshUI()
    {
        base.RefreshUI();

        var inventoryItems = Managers.Inventory.GetInventoryItems();

        uint itemPos = 0;
        foreach (var itemBox in _itemList)
        {
            if (inventoryItems.TryGetValue(itemPos, out var itemData))
                itemBox.UpdateData(itemData, itemPos);
            else
                itemBox.UpdateData(null, itemPos);

            itemPos++;
        }

        RefreshGoldUI();
    }

    protected override void OnDestroy()
    {
        if (Managers.Inventory != null && Managers.Inventory.Gold != null)
            Managers.Inventory.Gold.OnChanged -= OnGoldChanged;

        if (Managers.Inventory != null)
            Managers.Inventory.OnInventoryChanged -= OnInventoryChanged;

        _itemList = null;
        base.OnDestroy();
    }

    private void OnInventoryChanged()
    {
        RefreshUI();
    }

    private void OnGoldChanged(object sender, int delta)
    {
        RefreshGoldUI();
    }

    private void RefreshGoldUI()
    {
        var goldText = GetText((int)Texts.GoldText);
        if (goldText == null)
            return;

        goldText.text = $"골드 : {Managers.Inventory.Gold.Amount}";
    }

    private void OnCloseButtonClicked()
    {
        ClosePopupUI();
    }

    public override void ClosePopupUI()
    {
        OnClose?.Invoke();
        OnClose = null;
        base.ClosePopupUI();
    }

    public void ShowItemHover(Item.Data data, Vector2 screenPos)
    {
        if (_isDragging)
            return;

        if (_hoverPanel == null)
            return;

        _hoverPanel.Show(data);
    }

    public void HideItemHover()
    {
        if (_hoverPanel == null)
            return;

        _hoverPanel.Hide();
    }

    public void BeginItemDrag(ItemBoxUI source, Sprite sprite, Vector2 screenPos)
    {
        if (source == null)
            return;

        _isDragging = true;
        _dragSource = source;

        // 드래그 중에는 Hover 패널이 절대 뜨지 않도록 즉시 숨김
        HideItemHover();

        if (_ghostImage != null)
            _ghostImage.Show(sprite, screenPos);
    }

    public void UpdateItemDrag(Vector2 screenPos)
    {
        if (_ghostImage != null)
            _ghostImage.SetPosition(screenPos);
    }

    public void EndItemDrag(ItemBoxUI source, PointerEventData eventData)
    {
        _isDragging = false;

        if (_ghostImage != null)
            _ghostImage.Hide();

        _dragSource?.RestoreDragVisual();

        if (source == null || eventData == null)
        {
            _dragSource = null;
            return;
        }

        var target = FindItemBoxUnderPointer(eventData);
        if (target != null && target != source)
        {
            Managers.Inventory.SwapSlots(source.ItemPosition, target.ItemPosition);
            RefreshUI();
        }

        // 드래그 종료 시점에는 포인터가 이미 슬롯 위에 있어도 PointerEnter가 다시 안 올 수 있음.
        // 현재 포인터 아래 슬롯을 다시 판정해서 Hover 패널을 즉시 갱신한다.
        var hovered = FindItemBoxUnderPointer(eventData);
        if (hovered != null && hovered.ItemData != null)
            ShowItemHover(hovered.ItemData, eventData.position);
        else
            HideItemHover();

        _dragSource = null;
    }

    private ItemBoxUI FindItemBoxUnderPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null)
            return null;

        _raycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, _raycastResults);

        foreach (var rr in _raycastResults)
        {
            if (rr.gameObject == null)
                continue;

            var box = rr.gameObject.GetComponentInParent<ItemBoxUI>();
            if (box == null)
                continue;

            if (_itemList != null && _itemList.Contains(box))
                return box;
        }

        return null;
    }
}
