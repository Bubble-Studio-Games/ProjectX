using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public abstract class BaseAction : MonoBehaviour
{
    #region Field
    protected IGridVisualUpdateSource _gridUpdate;
    protected IGridQuery _grid;
    protected IDungeonCoreRegistry _dungeonCoreRegistry;

    public event Action OnActionStarted;
    public event Action OnActionCompleted;

    public class OnChangeMoveGridEventArgs : EventArgs
    {
        public ControllableObject obj;
    }

    [Header("Ref")]
    protected bool m_bIsActive;
    protected GameEntity m_GameEntity;

    public string m_actionName { get; protected set; }
    protected int m_iGetActionValidRange;

    public GridPosition DestGirdPosition { get; protected set; }

    #endregion

    #region Unity Life Cycle

    protected virtual void Awake()
    {
        m_GameEntity = GetComponentInParent<GameEntity>();
    }

    protected virtual void Start()
    {
        _gridUpdate = Managers.SceneServices.GridVisualUpdateSource;
        _grid = Managers.SceneServices.Grid;
        _dungeonCoreRegistry = Managers.SceneServices.DungeonCores;
    }

    protected virtual void Update() { }

    #endregion

    #region Method

    public abstract BaseAction TakeAction(GridPosition gridPosition = default);

    public virtual bool IsValidActionGridPosition(GridPosition gridPosition)
        => GetValidActionGridPositionList().Contains(gridPosition);

    public virtual List<GridPosition> GetValidActionGridPositionList() => default;

    public virtual void ActionStart()
    {
        m_bIsActive = true;
        OnActionStarted?.Invoke();
    }

    protected virtual void ActionComplete()
    {
        m_bIsActive = false;
        OnActionCompleted?.Invoke();
    }

    public EnemyAIAction GetBestEnemyAIAction()
    {
        List<EnemyAIAction> enemyAIActionList = new List<EnemyAIAction>();

        List<GridPosition> validActionGridPositionList = GetValidActionGridPositionList();

        foreach (GridPosition gridPosition in validActionGridPositionList)
        {
            EnemyAIAction enemyAIAction = GetEnemyAIAction(gridPosition);
            enemyAIActionList.Add(enemyAIAction);
        }

        if (enemyAIActionList.Count > 0)
        {
            enemyAIActionList.Sort((EnemyAIAction a, EnemyAIAction b) => b.actionValue - a.actionValue);
            return enemyAIActionList[0];
        } else
        {
            // No possible Enemy AI Actions
            return null;
        }

    }

    public virtual EnemyAIAction GetEnemyAIAction(GridPosition gridPosition) => default;

    public virtual void ClearAction() { }

    protected void DrawGridVisual() => _gridUpdate.DrawGridVisual();
    protected void DrawGridVisual(AttackData e = null) => _gridUpdate.DrawGridVisual();

    #endregion
}