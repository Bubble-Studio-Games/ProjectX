using UnityEngine;
using static Define;

public class ItemHoverTrigger : UI_Base
{
    private IItemBoxUI _itemBox;
    private IItemHoverPolicy _hoverPolicy;

    public override bool Init()
    {
        if (_init)
            return false;

        _init = true;
        _itemBox = GetComponent<IItemBoxUI>();
        _hoverPolicy = GetComponentInParent<IItemHoverPolicy>();

        BindEvent(gameObject, OnHover, UIEvent.Hover);
        BindEvent(gameObject, OnHoverExit, UIEvent.HoverExit);

        return true;
    }

    private void OnHover()
    {
        Init();

        if (_hoverPolicy == null)
            return;

        if (_itemBox == null || _itemBox.Table == null || _itemBox.Table is not Item.Data data)
            return;

        _hoverPolicy.ShowItemHover(data, Input.mousePosition);
    }

    private void OnHoverExit()
    {
        Init();
        _hoverPolicy?.HideItemHover();
    }
}