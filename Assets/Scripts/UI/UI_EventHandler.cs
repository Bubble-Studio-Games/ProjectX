using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_EventHandler : MonoBehaviour, 
    IPointerClickHandler, 
    IPointerDownHandler, 
    IPointerUpHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public Action OnClickHandler = null;
    public Action OnPressedHandler = null;
    public Action OnPointerDownHandler = null;
    public Action OnPointerUpHandler = null;
    public Action OnPointerEnterHandler = null;
    public Action OnPointerExitHandler = null;
    public Action<PointerEventData> OnBeginDragHandler = null;
    public Action<PointerEventData> OnDragHandler = null;
    public Action<PointerEventData> OnEndDragHandler = null;

    private bool _pressed = false;

    private void Update()
    {
        if (_pressed)
            OnPressedHandler?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClickHandler?.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressed = true;
        OnPointerDownHandler?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pressed = false;
        OnPointerUpHandler?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnPointerEnterHandler?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnPointerExitHandler?.Invoke();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        OnBeginDragHandler?.Invoke(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        OnDragHandler?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnEndDragHandler?.Invoke(eventData);
    }

    private void OnDestroy()
    {
        OnClickHandler = null;
        OnPressedHandler = null;
        OnPointerDownHandler = null;
        OnPointerUpHandler = null;
        OnPointerEnterHandler = null;
        OnPointerExitHandler = null;
        OnBeginDragHandler = null;
        OnDragHandler = null;
        OnEndDragHandler = null;
    }
}
