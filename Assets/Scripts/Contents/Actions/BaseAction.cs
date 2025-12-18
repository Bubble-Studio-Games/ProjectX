using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// 1. AttributeSystem의 "상태"만 담는 순수 데이터 클래스 (MonoBehaviour 상속 금지)
[Serializable]
public class BaseActionData
{
    public bool isActive;
}

public abstract class BaseAction : MonoBehaviour
{
    public static event EventHandler OnAnyActionStarted;
    public static event EventHandler OnAnyActionCompleted;

    public class OnChangeMoveGridEventArgs : EventArgs
    {
        public ControllableObject obj;
    }

    [Header("Ref")]
    protected bool m_bIsActive;
    protected GameEntity m_GameEntity;
    protected Action onActionComplete;

    public string m_actionName { get; protected set; }
    protected int m_iGetActionValidRange;

    [Header("Grid Position")]
    public GridPosition DestGirdPosition;

    protected virtual void Awake()
    {
        m_GameEntity = GetComponentInParent<GameEntity>();
    }

    protected virtual void Start()
    {

    }

    protected virtual void Update()
    {
    }

    public abstract BaseAction TakeAction(GridPosition gridPosition = default, Action onActionComplete = null);

    public virtual bool IsValidActionGridPosition(GridPosition gridPosition)
    {
        List<GridPosition> validGridPositionList = GetValidActionGridPositionList();
        return validGridPositionList.Contains(gridPosition);
    }

    public abstract List<GridPosition> GetValidActionGridPositionList();


    public virtual void ActionStart(Action onActionComplete)
    {
        m_bIsActive = true;
        OnAnyActionStarted?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void ActionComplete()
    {
        m_bIsActive = false;
        onActionComplete?.Invoke();

        OnAnyActionCompleted?.Invoke(this, EventArgs.Empty);
    }

    public void SetActionComlete(Action onActionComplete)
    {
        this.onActionComplete = onActionComplete;

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

    public abstract EnemyAIAction GetEnemyAIAction(GridPosition gridPosition);

    public virtual void ClearAction()
    {

    }
}