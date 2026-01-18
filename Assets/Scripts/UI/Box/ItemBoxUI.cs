using GoogleSheet;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static Define;


public class ItemBoxUI : UI_Base, IItemBoxUI
{
    public enum Images { ItemImage, BG, }
    public enum Texts { ItemName, ItemCount, }

    [Header("색상 설정")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _hoverColor = new Color(1f, 0.906f, 0.620f); // #FFE79E

    public uint ItemPosition { get; private set; }
    private bool _isHovered;

    private InventoryUI _owner;
    private Color _originalIconColor = Color.white;

    public Item.Data ItemData { get; private set; }
    public ITable Table => ItemData;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindImage(typeof(Images));
        BindText(typeof(Texts));

        BindEvent(gameObject, OnHoverEnter, UIEvent.Hover);
        BindEvent(gameObject, OnHoverExit, UIEvent.HoverExit);

        BindDragEvent(gameObject, OnBeginDrag, UIEvent.BeginDrag);
        BindDragEvent(gameObject, OnDrag, UIEvent.Drag);
        BindDragEvent(gameObject, OnEndDrag, UIEvent.EndDrag);

        SetHover(false);
        RefreshUI();
        return true;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ItemData = null;
    }

    public void UpdateData(Item.Data itemData, uint itemPosition = default)
    {
        Init();
        ItemData = itemData;
        ItemPosition = itemPosition;
        UpdateItemImage(itemData);
        UpdateItemName(itemData);
        UpdateItemCount(itemData, 99);
        RefreshUI();
    }

    public void SetOwner(InventoryUI owner)
    {
        _owner = owner;
    }

    private void UpdateItemImage(Item.Data itemData)
    {
        if (itemData == null)
        {
            GetImage((int)Images.ItemImage).gameObject.SetActive(false);
            return;
        }

        GetImage((int)Images.ItemImage).gameObject.SetActive(true);
        GetImage((int)Images.ItemImage).sprite = Managers.Resource.Load<Sprite>(Define.ITEM_ICON_PATH + itemData.Item_ID);
    }


    private string UpdateItemName(Item.Data itemData)
    {
        if (itemData == null)
        {
            GetText((int)Texts.ItemName).gameObject.SetActive(false);
            return "";
        }
        else
        {
            GetText((int)Texts.ItemName).gameObject.SetActive(true);
            GetText((int)Texts.ItemName).text = itemData.Item_Name;
            return itemData.Item_Name;
        }
    }
    private int UpdateItemCount(Item.Data itemData, int count)
    {
        if (itemData == null)
        {
            GetText((int)Texts.ItemCount).gameObject.SetActive(false);
            return 0;
        }
        else
        {
            GetText((int)Texts.ItemCount).gameObject.SetActive(true);
            GetText((int)Texts.ItemCount).text = count.ToString();
            return count;
        }
    }

    /// <summary>
    /// Hover 상태 설정 - BG 색상 변경
    /// </summary>
    private void SetHover(bool isHovered)
    {
        _isHovered = isHovered;

        var bgImage = GetImage((int)Images.BG);
        if (bgImage != null)
        {
            var targetColor = isHovered ? _hoverColor : _normalColor;
            bgImage.color = targetColor;
        }
    }

    private void OnHoverEnter()
    {
        SetHover(true);
    }

    private void OnHoverExit()
    {
        SetHover(false);
    }

    private void OnBeginDrag(PointerEventData eventData)
    {
        if (_owner == null)
            return;

        if (ItemData == null)
            return;

        var icon = GetImage((int)Images.ItemImage);
        if (icon == null || icon.sprite == null)
            return;

        _originalIconColor = icon.color;
        icon.color = new Color(_originalIconColor.r, _originalIconColor.g, _originalIconColor.b, 0.4f);

        _owner.BeginItemDrag(this, icon.sprite, eventData.position);
    }

    private void OnDrag(PointerEventData eventData)
    {
        if (_owner == null)
            return;

        _owner.UpdateItemDrag(eventData.position);
    }

    private void OnEndDrag(PointerEventData eventData)
    {
        if (_owner == null)
        {
            RestoreDragVisual();
            return;
        }

        _owner.EndItemDrag(this, eventData);
    }

    public void RestoreDragVisual()
    {
        var icon = GetImage((int)Images.ItemImage);
        if (icon == null)
            return;

        icon.color = new Color(_originalIconColor.r, _originalIconColor.g, _originalIconColor.b, 1f);
    }
}
