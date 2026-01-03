using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드래그 전용 이미지 패널
/// </summary>
public class GhostImagePanel : UI_Base
{
    private Image _image;
    private RectTransform _rectTransform;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        _image = GetComponent<Image>();
        _rectTransform = transform as RectTransform;

        if (_image != null)
            _image.raycastTarget = false;

        Hide();
        return true;
    }

    public void Show(Sprite sprite, Vector2 screenPos)
    {
        Init();

        if (_image == null)
            return;

        gameObject.SetActive(true);
        _image.enabled = true;
        _image.sprite = sprite;
        _image.color = Color.white;

        SetPosition(screenPos);
        transform.SetAsLastSibling();
    }

    public void SetPosition(Vector2 screenPos)
    {
        if (_rectTransform == null)
            _rectTransform = transform as RectTransform;

        if (_rectTransform != null)
            _rectTransform.position = screenPos;
    }

    public void Hide()
    {
        Init();

        if (_image != null)
            _image.enabled = false;

        gameObject.SetActive(false);
    }
}