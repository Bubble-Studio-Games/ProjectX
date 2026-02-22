using CodeMonkey.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static Define;

[EditorShowInfo(
@"
New Input System을 이용한 키 입력과 이벤트 매칭
"
)]
public partial class InputController : MonoBehaviour
{
    //public static MouseWorld Instance { get; private set; }
    public event Action<GridPosition, GridPosition> OnMousePositionChanged;

    public event Action<ISelectable> OnInteractableClicked;
    public event Action<List<ISelectable>> OnDragSelection;
    public event Action OnGroundClicked;

    private GridPosition m_GridPosition;

    [Header("Selection")]
    [SerializeField] private RectTransform SelectionBox;
    private Vector2 startPosition;
    [SerializeField] private float DragDelay = 0.1f;
    private bool m_isDragwing;
    private float MouseDownTime;

    [Header("Cursor")]
    [SerializeField] Texture2D DefaultCursor;
    [SerializeField] Texture2D AttackCursor;
    [SerializeField] Texture2D InteractCursor;
    [SerializeField] Vector2 hotspot = Vector2.zero;
    private GameObject lastHoveredObject;

    // InputAction 콜백에서 IsPointerOverGameObject() 사용 시 이전 프레임 상태 반환 문제 해결용
    private bool _isPointerOverUI = false;
    public bool IsPointerOverUI => _isPointerOverUI;

    private void Update()
    {
        // InputAction 콜백에서 사용할 수 있도록 UI 상태 캐싱
        _isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        MouseDrag();
        UpdateCursor();
    }

    private void OnEnable()
    {
        OnInteractableClicked += HandleUnitClicked;
        OnDragSelection += HandleDragSelection;
        OnGroundClicked += HandleGroundClicked;
    }

    private void OnDisable()
    {
        OnInteractableClicked -= HandleUnitClicked;
        OnDragSelection -= HandleDragSelection;
        OnGroundClicked -= HandleGroundClicked;
    }

    /// <summary>
    /// 마우스 커서가 활성화되어 있는지 확인
    /// </summary>
    private bool IsCursorEnabled() => GameConfig.Mouse.IsMouseCursorEnabled;

    public void MouseUp(E_MouseClickType type)
    {
        m_isDragwing = false;
        if (SelectionBox == null)
        {
            Debug.LogWarning("SelectionBox is null!");
            return;
        }
        SelectionBox.gameObject.SetActive(false);
    }

    public void MouseDrag()
    {
        if (m_isDragwing == false)
            return;

        if ((MouseDownTime + DragDelay < Time.time))
        {
            //Debug.Log("마우스 클릭 왼쪽 드래그 중");
            ResizeSelectionBox();
        }
    }

    public void MouseDown(E_MouseClickType type)
    {
        // 다른 UI에 손을 못대게 (캐싱된 값 사용 - InputAction 콜백 호환)
        if (_isPointerOverUI)
            return;

        // Drag Box
        m_isDragwing = true;
        startPosition = Input.mousePosition;
        SelectionBox.gameObject.SetActive(true);
        SelectionBox.sizeDelta = Vector3.zero;
        MouseDownTime = Time.time;

        // 클릭 이벤트
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition),
            out RaycastHit hit, GameConfig.Layer.HitColLayerMask)
            && hit.transform.parent.TryGetComponent(out ISelectable unit))
        {
            OnInteractableClicked?.Invoke(unit);
            return;
        }

        // 빈 땅 클릭
        OnGroundClicked?.Invoke();
    }

    private void HandleUnitClicked(ISelectable obj)
    {
        if (_isLeftShiftHold)
            Managers.Selection.Toggle(obj);
        else
        {
            Managers.Selection.DeselectAll();
            Managers.Selection.Select(obj);
        }
    }

    private void HandleDragSelection(List<ISelectable> units)
    {
        Managers.Selection.DeselectAll();
        foreach (var u in units)
            Managers.Selection.Add(u);
    }

    private void HandleGroundClicked()
    {
        Managers.Selection.DeselectAll();
    }

    private void ResizeSelectionBox()
    {
        float width = Input.mousePosition.x - startPosition.x;
        float height = Input.mousePosition.y - startPosition.y;

        SelectionBox.anchoredPosition = startPosition + new Vector2(width / 2, height / 2);
        SelectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));

        List<ISelectable> selected = new();

        Bounds bounds = new Bounds(SelectionBox.anchoredPosition, SelectionBox.sizeDelta);

        var list = Managers.Object.GetObjectList()
                    .Where(obj => obj.GetComponent<ISelectable>() != null);

        foreach (var obj in list)
            if (ObjectIsInSelectionBox(Camera.main.WorldToScreenPoint(obj.transform.position), bounds))
                selected.Add(obj.GetComponent<ISelectable>());

        OnDragSelection?.Invoke(selected);

        bool ObjectIsInSelectionBox(Vector2 position, Bounds bounds)
        {
            return position.x > bounds.min.x && position.x < bounds.max.x
                && position.y > bounds.min.y && position.y < bounds.max.y;
        }
    }

    private bool UpdateGridPosition()
    {
        var oldPos = Util.Mouse.GetMouseWorldGridPosition();

        if (oldPos != m_GridPosition)
        {
            m_GridPosition = oldPos;
            return true;
        }

        return false;
    }

    #region Cursor

    private void UpdateCursor()
    {
        if (!Application.isFocused) return;

        // 커서 활성화 시 기본 커서 설정
        if (IsCursorEnabled())
            Cursor.SetCursor(DefaultCursor, hotspot, CursorMode.Auto);

        lastHoveredObject = null;

        // Select Object
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, GameConfig.Layer.HitColLayerMask)
            && hit.collider.TryGetComponent<GameEntity>(out GameEntity result))
        {
            if (lastHoveredObject != result)
            {
                if (result.m_TeamId == E_TeamId.Monster)
                {
                    if (Managers.Selection.SelectedUnits.Count == 0)
                        return;

                    if (IsCursorEnabled())
                        Cursor.SetCursor(AttackCursor, hotspot, CursorMode.Auto);
                }
                else if (result.m_EObjectType == E_ObjectType.Interact)
                {
                    if (IsCursorEnabled())
                        Cursor.SetCursor(InteractCursor, hotspot, CursorMode.Auto);
                }

                lastHoveredObject = result.gameObject;
            }
        }

        if (IsCursorEnabled() == false)
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    #endregion


}