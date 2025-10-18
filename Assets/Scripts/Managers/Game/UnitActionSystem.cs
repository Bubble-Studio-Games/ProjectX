using CodeMonkey.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using static Define;

public class UnitActionSystem : MonoBehaviour
{
    public static UnitActionSystem Instance { get; private set; }

    public event EventHandler OnSelectedUnitChanged;
    public event EventHandler<OnCommandActionEventArgs> OnSelectedActionChanged;
    public event EventHandler<OnCommandActionEventArgs> OnCommandAction;
    public class OnCommandActionEventArgs : EventArgs
    {
        public GridPosition GridPosition;
        public Type action;
    }

    public event EventHandler<GridPosition> OnUpdateActionTick;

    public HashSet<ControllableObject> m_SelectedObjects = new HashSet<ControllableObject>();

    [SerializeField]  private BaseAction m_SelectedAction;


    [Header("Check Timer")]
    public float checkInterval = 0.5f;
    private float timer = 0f;

    private GridPosition m_CommandGridPosition;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one UnitActionSystem! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        m_SelectedObjects = null;
        m_SelectedAction = null;
        OnSelectedUnitChanged = null;
        OnSelectedActionChanged = null;
        OnCommandAction = null;
        OnUpdateActionTick = null;
    }

    private void Update()
    {
        HandleTick();

        if (m_SelectedObjects.Count <= 0)
            return;

        HandleSelectedAction();
    }

    private void HandleTick()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = checkInterval;

            // 지정 위치 전달
            OnUpdateActionTick?.Invoke(this, m_CommandGridPosition);

            // 전달 후 초기화
            if (m_CommandGridPosition != default)
                m_CommandGridPosition = default;
        }
    }

    private void HandleSelectedAction()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (InputManager.Instance.IsMouseButtonDownThisFrame())
        {
            GridPosition mouseGridPosition = LevelGrid.Instance.GetGridPosition(UtilsClass.GetMouseWorldPositionByRaycast(LayerManager.Instance.mousePlaneLayerMask));

            // Select Object
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, LayerManager.Instance.PlayerInteractableLayerMask))
            {
                var result = hit.collider.GetComponentInParent<GameEntity>();
                if(result != null)
                {
                    // Interasct
                    if (result.m_ObjectType == E_ObjectType.Interact)
                    {
                        // Command Attack
                        if (m_SelectedAction == null)
                        {
                            ExecuteActionForUnits<CommandAttackAction>(mouseGridPosition);
                        }
                    }

                    // Command Attack
                    else if (result.m_ObjectType == E_ObjectType.Unit || result.m_ObjectType == E_ObjectType.Building)
                    {
                        if (result.m_TeamId == E_TeamId.Monster)
                        {
                            // Command Attack
                            if (m_SelectedAction == null)
                            {
                                ExecuteActionForUnits<CommandAttackAction>(result.m_GridPosition);
                            }
                        }
                    }
                }
                else
                {
                    CommandMove(mouseGridPosition);
                }
            }

            // Select Grid
            else
            {
                CommandMove(mouseGridPosition);
            }
        }

        void CommandMove(GridPosition mouseGridPosition)
        {
            // Command Move
            if (m_SelectedAction == null)
            {
                ExecuteActionForUnits<CommandMoveAction>(mouseGridPosition, (u, a) => { u.SwitchToNextStateAction(u.GetAction<IdleAction>()); });
            }
        }
    }

    // 선택한 명령에 해당하는 유닛들, 그 유닛들에서 조건에 적합한 유닛들을 선별한 후 명령을 내린다.
    private void ExecuteActionForUnits<TAction>(GridPosition gridPosition, Action<ControllableObject, TAction> onActionComplete = null) where TAction : BaseAction
    {
        var units = m_SelectedObjects.Where(x => x.m_ObjectType == E_ObjectType.Unit).ToList();
        var filterObjects = FilterUnitsWithAction<TAction>(units);

        foreach (var (unit, action) in filterObjects)
        {
            if (!action.IsValidActionGridPosition(gridPosition))
                return;

            m_CommandGridPosition = gridPosition;
            unit.DirectCommand(action, onActionComplete);
        }

        OnCommandAction?.Invoke(this, new OnCommandActionEventArgs
        {
            action = typeof(TAction),
            GridPosition = gridPosition,
        });
    }

    #region Select Object

    public void SetSelectedObject(GameEntity entity)
    {
        if (entity is not ControllableObject unit) return;

        m_SelectedObjects.Add(unit);
        unit.OnSelected();

        OnSelectedUnitChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Deselect(GameEntity entity)
    {
        if (entity is not ControllableObject unit) return;

        unit.OnDeselected();
        m_SelectedObjects.Remove(unit);
        OnSelectedUnitChanged?.Invoke(this, EventArgs.Empty);
    }

    public void DeselectAll()
    {
        foreach (GameEntity unit in m_SelectedObjects)
            unit.OnDeselected();
        m_SelectedObjects.Clear();
        OnSelectedUnitChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool IsSelectedObject(GameEntity unit)
    {
        return m_SelectedObjects.Contains(unit);
    }


    #endregion

    public void SetSelectedAction(BaseAction baseAction)
    {
        m_SelectedAction = baseAction;

        OnSelectedActionChanged?.Invoke(this, new OnCommandActionEventArgs
        {
            action = m_SelectedAction.GetType()
        });

        //Debug.Log($"({m_SelectedAction.GetActionName()}) Action 이 선택됨");
    }

    public BaseAction GetSelectedAction()
    {
        return m_SelectedAction;
    }


    #region Command

    public void HandleCommand()
    {
        if (m_SelectedObjects.Count == 0)
            return;
    }

    public List<BaseAction> GetCommonActionTypes(List<ControllableObject> selectedUnits)
    {
        // 선택된 유닛 리스트가 비어있거나 null이면 빈 HashSet 반환
        if (selectedUnits == null || selectedUnits.Count == 0)
            return new List<BaseAction>();

        // 1. 유닛별 액션 타입 집합 만들기
        var commonTypes = selectedUnits
            .Select(unit =>
                System.Linq.Enumerable.ToHashSet(
                    unit.GetActions().Select(action => action.GetType())))
            .Aggregate((set1, set2) =>
            {
                set1.IntersectWith(set2); // 타입의 교집합
                return set1;
            });

        // 2. 첫 번째 유닛에서 공통 타입에 해당하는 액션 인스턴스 가져오기
        var firstUnitActions = selectedUnits[0].GetActions();

        return firstUnitActions
            .Where(action => commonTypes.Contains(action.GetType()))
            .ToList();
    }

    public List<(ControllableObject unit, TAction action)> FilterUnitsWithAction<TAction>(List<ControllableObject> selectedUnits) where TAction : BaseAction
    {
        return selectedUnits
            .Select(unit => (unit, action: unit.GetActions().OfType<TAction>().FirstOrDefault()))
            .Where(pair => pair.action != null)
            .ToList();
    }
    #endregion
}