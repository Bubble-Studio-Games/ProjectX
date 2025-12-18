using CodeMonkey.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static Define;

public class MouseWorld : MonoBehaviour
{
    public static MouseWorld Instance { get; private set; }
    public event EventHandler<(GridPosition oldgp, GridPosition newgp)> OnMousePositionChanged;

    public event Action<IInteractable> OnInteractableClicked;
    public event Action<List<IInteractable>> OnDragSelection;
    public event Action OnGroundClicked;

    private GridPosition m_GridPosition;

    [Header("Selection")]
    [SerializeField] private RectTransform SelectionBox;
    private Vector2 startPosition;
    [SerializeField]  private float DragDelay = 0.1f;
    private bool m_isDragwing;
    private float MouseDownTime;

    [Header("Cursor")]
    [SerializeField] Texture2D DefaultCursor;
    [SerializeField] Texture2D AttackCursor;
    [SerializeField] Texture2D InteractCursor;
    [SerializeField] Vector2 hotspot = Vector2.zero;
    private GameObject lastHoveredObject;

    [Header("Click Effect")]
    [SerializeField] Transform m_WorldUITransform;
    [SerializeField] GameObject m_goCommandActionAtGridEffect;
    GameObject m_goPoolableEffect;
    [SerializeField] float m_Defaultheight = 2f;

    private void Awake()
    {
        Instance = this;
        SelectionBox.gameObject.SetActive(false);

        // Cursor
        Cursor.SetCursor(DefaultCursor, hotspot, CursorMode.Auto);

        // effect
        Managers.Command.OnCommandAction += InstantiateMouseEffect;

        OnInteractableClicked += HandleUnitClicked;
        OnDragSelection += HandleDragSelection;
        OnGroundClicked += HandleGroundClicked;
    }

    private void Update()
    {
        if (Managers.Scene.CurrentScene.SceneType == Define.Scene.Dungeon ||
           Managers.Scene.CurrentScene.SceneType == Define.Scene.Camp)
        {

            MouseDrag();
            UpdateCursor();

            UpdateGridPosition();
        }
    }

    void UpdateGridPosition()
    {
        GridPosition newGridPosition = GetGridPosition();

        if (!LevelGrid.Instance.IsValidGridPosition(newGridPosition))
            return;

        if (newGridPosition != m_GridPosition)
        {
            // Unit changed Grid Position
            var oldGridPosition = m_GridPosition;
            m_GridPosition = newGridPosition;

            OnMousePositionChanged?.Invoke(this, (oldGridPosition, newGridPosition));
        }
    }

    public GridPosition GetGridPosition()
    {
        Vector3 mousePlanePos = UtilsClass.GetMouseWorldPositionByRaycast(Managers.Layer.mousePlaneLayerMask);
        return LevelGrid.Instance.GetGridPosition(mousePlanePos);
    }



    public void MouseUp()
    {
        m_isDragwing = false;
        SelectionBox.gameObject.SetActive(false);
    }

    public void MouseDrag()
    {
        if (m_isDragwing == false)
            return;

        if ((MouseDownTime + DragDelay < Time.time))
        {
            Debug.Log("마우스 클릭 왼쪽 드래그 중");
            ResizeSelectionBox();
        }
    }

    public void MouseDown()
    {
        // 다른 UI에 손을 못대게
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        
        // Drag Box
        m_isDragwing = true;
        startPosition = Input.mousePosition;
        SelectionBox.gameObject.SetActive(true);
        SelectionBox.sizeDelta = Vector3.zero;
        MouseDownTime = Time.time;

        // 클릭 이벤트
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition),
            out RaycastHit hit, Managers.Layer.HitColLayerMask)
            && hit.transform.parent.TryGetComponent<IInteractable>(out IInteractable unit))
        {
            OnInteractableClicked?.Invoke(unit);
            return;
        }

        // 빈 땅 클릭
        OnGroundClicked?.Invoke();
    }

    private void HandleUnitClicked(IInteractable obj)
    {
        if (Keyboard.current.shiftKey.isPressed)
            Managers.Selection.Toggle(obj);
        else
        {
            Managers.Selection.DeselectAll();
            Managers.Selection.Select(obj);
        }
    }

    private void HandleDragSelection(List<IInteractable> units)
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

        List<IInteractable> selected = new();

        Bounds bounds = new Bounds(SelectionBox.anchoredPosition, SelectionBox.sizeDelta);

        var list = Managers.Object.GetObjectList()
                    .Where(obj => obj.GetComponent<IInteractable>() != null);

        foreach (var obj in list)
            if (ObjectIsInSelectionBox(Camera.main.WorldToScreenPoint(obj.transform.position), bounds))
                selected.Add(obj.GetComponent<IInteractable>());

        OnDragSelection?.Invoke(selected);

        bool ObjectIsInSelectionBox(Vector2 position, Bounds bounds)
        {
            return position.x > bounds.min.x && position.x < bounds.max.x
                && position.y > bounds.min.y && position.y < bounds.max.y;
        }
    }

    // 마우스 클릭 이벤트
    private void InstantiateMouseEffect(object sender, CommandManager.OnCommandActionEventArgs e)
    {
        float height = m_Defaultheight;

        if (e.action == typeof(CommandAttackAction))
        {
            GameEntity target = LevelGrid.Instance.GetObjectAtGridPosition(e.GridPosition);
            if (target == null)
                return;
            height += target.m_HitCollider.bounds.max.y;
            // 해당 위치의 오브젝트의 선택 색상을 빨갛게 변화시키고,
            // 화살표를 오브젝트의 콜라이더 위로 옮겨 버리기
            // 너무 높은 것도 그냥 올려버림.
        }

        if(m_goPoolableEffect != null)
            Managers.Resource.Destroy(m_goPoolableEffect);

        // 해당 위치에 이펙트 생성
        m_goPoolableEffect = Managers.Resource.Instantiate(m_goCommandActionAtGridEffect, m_WorldUITransform);
        m_goPoolableEffect.transform.position = LevelGrid.Instance.GetWorldPosition(e.GridPosition) + new Vector3(0, height, 0);

        FunctionTimer.Create(() =>
        {
            if (m_goPoolableEffect != null)
                Managers.Resource.Destroy(m_goPoolableEffect.gameObject);
        }, 5f);
    }

    #region Cursor

    private void UpdateCursor()
    {
        if (!Application.isFocused) return;

        Cursor.SetCursor(DefaultCursor, hotspot, CursorMode.Auto);
        lastHoveredObject = null;

        // Select Object
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Managers.Layer.ControllableObjectLayerMask)
            && hit.collider.TryGetComponent<GameEntity>(out GameEntity result))
        {
            if (lastHoveredObject != result)
            {
                if (result.m_TeamId == E_TeamId.Monster)
                {
                    if (Managers.Selection.SelectedUnits.Count == 0)
                        return;

                    Cursor.SetCursor(AttackCursor, hotspot, CursorMode.Auto);
                }
                else if (result.m_ObjectType == E_ObjectType.Interact)
                {
                    Cursor.SetCursor(InteractCursor, hotspot, CursorMode.Auto);

                }

                lastHoveredObject = result.gameObject;
            }
        }
    }

    #endregion
}