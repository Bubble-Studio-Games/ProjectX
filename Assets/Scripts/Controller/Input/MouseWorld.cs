using CodeMonkey.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static Define;

public class MouseWorld : MonoBehaviour
{
    public static MouseWorld Instance { get; private set; }

    public event EventHandler OnMouseDownChanged;
    public event EventHandler OnMouseUpChanged;
    public event EventHandler<(GridPosition oldgp, GridPosition newgp)> OnMousePositionChanged;

    [Header("Selection")]
    [SerializeField] private RectTransform SelectionBox;
    private Vector2 startPosition;
    private GridPosition m_GridPosition;
    [SerializeField]
    private float DragDelay = 0.1f;

    private float MouseDownTime;
    public bool m_IsDraging;

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
        if(UnitActionSystem.Instance != null)
            UnitActionSystem.Instance.OnCommandAction += InstantiateMouseEffect;
    }

    public Vector3 GetPosition()
    {
        Ray ray =  Camera.main.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition());
        Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, LayerManager.Instance.mousePlaneLayerMask);
        return raycastHit.point;
    }

    public GridPosition GetGridPosition()
    {
        Vector3 mousePlanePos = UtilsClass.GetMouseWorldPositionByRaycast(LayerManager.Instance.mousePlaneLayerMask);
        return LevelGrid.Instance.GetGridPosition(mousePlanePos);
    }

    public Vector3 GetPositionOnlyHitVisible()
    {
        // 1. 마우스 위치 → 카메라에서 쏘는 Ray 생성
        Ray ray = Camera.main.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition());

        // 2. RaycastAll: 모든 충돌체(콜라이더)를 다 맞춤
        RaycastHit[] raycastHitArray = Physics.RaycastAll(ray, float.MaxValue, LayerManager.Instance.mousePlaneLayerMask);

        // 3. 거리 기준으로 정렬 (가까운 게 먼저)
        System.Array.Sort(raycastHitArray, (a, b) =>
        {
            return Mathf.RoundToInt(a.distance - b.distance);
        });

        // 4. 맞은 것들 중에서 **Renderer.enabled == true** 인 애만 선택
        foreach (RaycastHit raycastHit in raycastHitArray)
        {
            if (raycastHit.transform.TryGetComponent(out Renderer renderer))
            {
                if (renderer.enabled)
                {
                    // → 카메라에 실제 보이는 오브젝트라면 그 좌표 리턴
                    return raycastHit.point;
                }
            }
        }

        // 5. 아무것도 없으면 (혹은 전부 invisible이면) (0,0,0) 리턴
        return Vector3.zero;
    }


    private void Update()
    {
        HandleSelectionInputs();
        UpdateCursor();

        // TODO 키보드 조작
        CancleSelectAll();

        UpdateGridPosition();
    }

    protected void UpdateGridPosition()
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

    private void UpdateCursor()
    {
        if (!Application.isFocused) return;

        Cursor.SetCursor(DefaultCursor, hotspot, CursorMode.Auto);
        lastHoveredObject = null;

        // Select Object
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, LayerManager.Instance.ControllableObjectLayerMask)
            && hit.collider.TryGetComponent<GameEntity>(out GameEntity result))
        {
            if (lastHoveredObject != result)
            {
                if (result.m_TeamId == E_TeamId.Monster)
                {
                    if (UnitActionSystem.Instance.m_SelectedObjects.Count == 0)
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

    private void HandleSelectionInputs()
    {
        MouseUp();



        MouseDown();
        MouseDrag();
    }

    private static void CancleSelectAll()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            UnitActionSystem.Instance.DeselectAll();
        }
    }

    private void MouseUp()
    {
        // 카드 드로우 중일 때 선택하지 못 하게
        if (BuildingTypeSelectUI.Instance.m_IsDrawing)
            return;

        if (Input.GetMouseButtonUp(0))
        {
            m_IsDraging = false;
            SelectionBox.sizeDelta = Vector3.zero;
            SelectionBox.gameObject.SetActive(false);

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, LayerManager.Instance.HitColLayerMask)
                && hit.transform.parent.TryGetComponent<ControllableObject>(out ControllableObject unit))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    if (UnitActionSystem.Instance.IsSelectedObject(unit))
                    {
                        UnitActionSystem.Instance.Deselect(unit);
                    }
                    else
                    {
                        UnitActionSystem.Instance.SetSelectedObject(unit);
                    }
                }
                else
                {
                    UnitActionSystem.Instance.DeselectAll();
                    UnitActionSystem.Instance.SetSelectedObject(unit);
                }
            }
            // Deselect all if it's a short click, not a drag
            else if (MouseDownTime + DragDelay > Time.time)
            {
                //UnitActionSystem.Instance.DeselectAll();
            }

            MouseDownTime = 0;
            OnMouseUpChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void MouseDrag()
    {


        if (Input.GetMouseButton(0) && MouseDownTime + DragDelay < Time.time && m_IsDraging)
        {
            ResizeSelectionBox();
        }
    }

    private void MouseDown()
    {
        // 다른 UI에 손을 못대게
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        // 카드 드로우 중일 때 선택하지 못 하게
        if (BuildingTypeSelectUI.Instance.m_IsDrawing)
            return;

        if (Managers.Scene.CurrentScene.SceneType != Define.Scene.Game)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            // Left Mosue Button Pressed
            startPosition = Input.mousePosition;
            SelectionBox.gameObject.SetActive(true);
            SelectionBox.sizeDelta = Vector3.zero;
            MouseDownTime = Time.time;

            m_IsDraging = true;

            OnMouseDownChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ResizeSelectionBox()
    {
        float width = Input.mousePosition.x - startPosition.x;
        float height = Input.mousePosition.y - startPosition.y;

        SelectionBox.anchoredPosition = startPosition + new Vector2(width / 2, height / 2);
        SelectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));

        Bounds bounds = new Bounds(SelectionBox.anchoredPosition, SelectionBox.sizeDelta);

        var list = Managers.Object.GetObjectList<ControllableObject>().Where(obj => obj.m_TeamId == E_TeamId.Player).ToList();
        for (int i = 0; i < list.Count; i++)
        {
            if (UnitIsInSelectionBox(Camera.main.WorldToScreenPoint(list[i].transform.position), bounds))
            {
                UnitActionSystem.Instance.SetSelectedObject(list[i]);
            }
            else
            {
                UnitActionSystem.Instance.Deselect(list[i]);
            }
        }
    }

    private bool UnitIsInSelectionBox(Vector2 position, Bounds bounds)
    {
        return position.x > bounds.min.x && position.x < bounds.max.x
            && position.y > bounds.min.y && position.y < bounds.max.y;
    }

    private void InstantiateMouseEffect(object sender, UnitActionSystem.OnCommandActionEventArgs e)
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
}