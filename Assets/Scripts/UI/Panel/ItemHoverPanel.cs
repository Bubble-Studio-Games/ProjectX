using UnityEngine;

/// <summary>
/// 아이템 호버 패널 - 마우스 위치에 아이템 정보 표시
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ItemHoverPanel : UI_Base
{
    [Header("설정")]
    [SerializeField] private Vector2 _offset = new Vector2(10, -10);
    [SerializeField] private float _showDelay = 0.3f;
    private Vector2 _pivot = new Vector2(0, 1);

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private RectTransform _targetRect;
    private CanvasGroup _canvasGroup;

    private bool _isShowing;
    private bool _isWaiting;
    private float _hoverTimer;
    private Item.Data _pendingData;

#if UNITY_EDITOR
    private void OnValidate()
    {
        var rectTransform = transform as RectTransform;
        if (rectTransform != null)
            rectTransform.pivot = _pivot;
    }
#endif

    public override bool Init()
    {
        if (_init)
            return false;

        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null)
            _canvasRect = _canvas.transform as RectTransform;

        _targetRect = transform as RectTransform;
        _canvasGroup = GetComponent<CanvasGroup>();

        SetVisible(false);

        return base.Init();
    }

    private void LateUpdate()
    {
        // 딜레이 대기 중
        if (_isWaiting)
        {
            _hoverTimer += Time.deltaTime;
            if (_hoverTimer >= _showDelay)
            {
                _isWaiting = false;
                ShowImmediate();
            }
        }

        // 표시 중 위치 업데이트
        if (_isShowing)
            UpdatePosition(Input.mousePosition);
    }

    public void Show(Item.Data data)
    {
        if (data == null)
            return;

        Init();

        _pendingData = data;

        // 딜레이가 0이면 즉시 표시
        if (_showDelay <= 0f)
        {
            ShowImmediate();
            return;
        }

        // 딜레이 대기 시작
        _isWaiting = true;
        _hoverTimer = 0f;
    }

    private void ShowImmediate()
    {
        if (_pendingData == null)
            return;

        SetVisible(true);
        _isShowing = true;

        UpdatePosition(Input.mousePosition);
    }

    public void Hide()
    {
        _isWaiting = false;
        _isShowing = false;
        _pendingData = null;
        _hoverTimer = 0f;

        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.blocksRaycasts = visible;
        _canvasGroup.interactable = visible;
    }

    private void UpdatePosition(Vector2 screenPos)
    {
        if (_canvasRect == null || _targetRect == null)
            return;

        Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            screenPos,
            cam,
            out Vector2 localPos);

        // Pivot (0, 1) 기준 - 왼쪽 상단이 마우스 위치
        _targetRect.anchoredPosition = localPos + _offset;
    }
}