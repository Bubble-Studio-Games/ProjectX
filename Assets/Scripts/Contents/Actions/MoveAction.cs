using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static Define;

public class MoveAction : BaseAction
{
    public event EventHandler OnStartMoving;
    public event EventHandler OnStopMoving;
    public event EventHandler OnUpdateGrid;

    public event EventHandler<OnChangeFloorsStartedEventArgs> OnChangedFloorsStarted;
    public class OnChangeFloorsStartedEventArgs : EventArgs
    {
        public GridPosition unitGridPosition;
        public GridPosition targetGridPosition;
    }

    protected int    m_iMaxMoveDistance;

    protected Vector3 forwardPosition;
    protected bool isChangingFloors;
    protected float differentFloorsTeleportTimer;
    protected float differentFloorsTeleportTimerMax = .5f;

    protected int m_iPathMaxCount = 2;
    protected int m_iPathCurrentCount = 0;

    protected override void Start()
    {
        base.Start();

        if (m_BaseObject.m_TeamId == E_TeamId.Player)
        {
            OnUpdateGrid += (s, e) => GridSystemVisual.Instance.UpdateGridVisual_Event(s, m_BaseObject);
        }
    }

    protected override void Update()
    {
        base.Update();

        if (!m_bIsActive)
        {
            return;
        }

        if (forwardPosition == default)
        {
            return;
        }

        if (m_BaseObject.m_AttributeSystem.m_IsDead)
            return;

        if (m_BaseObject is PassiveObject pobj)
        {

        }
        else if (m_BaseObject is ControllableObject cobj)
        {
            Vector3 targetPosition = forwardPosition;

            if (isChangingFloors)
            {
                // Stop and Teleport Logic
                Vector3 targetSameFloorPosition = targetPosition;
                targetSameFloorPosition.y = m_BaseObject.transform.position.y;

                Vector3 rotateDirection = (targetSameFloorPosition - m_BaseObject.transform.position).normalized;

                float rotateSpeed = 10f;
                m_BaseObject.transform.forward = Vector3.Slerp(m_BaseObject.transform.forward, rotateDirection, Time.deltaTime * rotateSpeed);

                differentFloorsTeleportTimer -= Time.deltaTime;
                if (differentFloorsTeleportTimer < 0f)
                {
                    isChangingFloors = false;
                    m_BaseObject.transform.position = targetPosition;
                }
            }
            else
            {
                // Regular move logic
                Vector3 moveDirection = (targetPosition - m_BaseObject.transform.position).normalized;

                float rotateSpeed = 10f;
                m_BaseObject.transform.forward = Vector3.Slerp(m_BaseObject.transform.forward, moveDirection, Time.deltaTime * rotateSpeed);

                m_BaseObject.transform.position += moveDirection * cobj.GetMoveSpeed() * Time.deltaTime;
            }

            // 다음 그리드 도착
            float stoppingDistance = .1f;
            if (Vector3.Distance(m_BaseObject.transform.position, targetPosition) < stoppingDistance)
            {
                LevelGrid.Instance.SetGridPositionCellInfo(
                    LevelGrid.Instance.GetGridPosition(forwardPosition), E_GridCheckType.GameEntity, m_BaseObject);
                forwardPosition = default;
                OnUpdateGrid?.Invoke(this, EventArgs.Empty);

                // 최종 목적지에 도착했는지 여부 따지기
                if (DestGirdPosition == m_BaseObject.GetGridPosition())
                {
                    ActionComplete();
                }
            }
        }
        else
        {

        }
    }

    public override BaseAction TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        return this;
    }

    public override string GetActionName()
    {
        return "Move";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        throw new NotImplementedException();
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        return default;
    }

    private List<GridPosition> GetValidEmptyGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        GridPosition unitGridPosition = m_BaseObject.GetGridPosition();

        for (int x = -m_iMaxMoveDistance; x <= m_iMaxMoveDistance; x++)
        {
            for (int z = -m_iMaxMoveDistance; z <= m_iMaxMoveDistance; z++)
            {
                for (int floor = -m_iMaxMoveDistance; floor <= m_iMaxMoveDistance; floor++)
                {
                    GridPosition offsetGridPosition = new GridPosition(x, z, floor);
                    GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                    if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
                    {
                        continue;
                    }

                    if (!LevelGrid.Instance.IsGridPositionCheckType(testGridPosition, E_GridCheckType.Walkable))
                        continue;

                    if (!Pathfinding.Instance.HasPath(unitGridPosition, testGridPosition))
                    {
                        continue;
                    }

                    int pathfindingDistanceMultiplier = 10;
                    if (Pathfinding.Instance.GetPathLength(unitGridPosition, testGridPosition) > m_iMaxMoveDistance * pathfindingDistanceMultiplier)
                    {
                        // Path length is too long
                        continue;
                    }

                    validGridPositionList.Add(testGridPosition);
                }
            }
        }

        return validGridPositionList;
    }

    public override void ActionStart(Action onActionComplete)
    {
        base.ActionStart(onActionComplete);
        OnStartMoving?.Invoke(this, EventArgs.Empty);
    }

    protected override void ActionComplete()
    {
        base.ActionComplete();

        OnStopMoving?.Invoke(this, EventArgs.Empty);
        m_iPathCurrentCount = 0;
    }

    public override void ClearAction()
    {
        base.ClearAction();

        if (forwardPosition != default)
        {
            LevelGrid.Instance.SetGridPositionCellInfo(LevelGrid.Instance.GetGridPosition(forwardPosition), E_GridCheckType.Walkable);
            forwardPosition = default;
            m_iPathCurrentCount = 0;
        }
    }

    protected BaseAction FailSerachTarget()
    {
        // 이동 중이면 대기
        if (!m_bIsActive)
            return this;

        if (m_BaseObject is PassiveObject pobj)
        {

        }
        else if (m_BaseObject is ControllableObject cobj)
        {

            // 타겟 Null
            cobj.SetTarget(null);

            // 현재 이동에도 가장 빈 곳 찾아서 이동후 IdleAction
            GridPosition pos = LevelGrid.Instance.GetClosestGridPositionSpecificCondition(m_BaseObject.GetGridPosition(), GetValidEmptyGridPositionList());

            // 움직일 곳이 없다면 그 자리에서 대기
            if (pos == default)
            {
                OnStopMoving?.Invoke(this, EventArgs.Empty);
                ActionComplete();

                return m_BaseObject.GetAction<IdleAction>();
            }

            DestGirdPosition = pos;

            // 빈곳 길찾기
            var list = Pathfinding.Instance.FindPath(m_BaseObject.GetGridPosition(), pos, out int len);
            if (list.Count >= Remove_MOVE_GRID)
            {
                list.RemoveAt(0);

                // 기존 꺼 제거
                LevelGrid.Instance.SetGridPositionCellInfo(LevelGrid.Instance.GetGridPosition(forwardPosition), E_GridCheckType.Walkable);

                // Reset
                forwardPosition = LevelGrid.Instance.GetWorldPosition(list[0]);
                LevelGrid.Instance.SetGridPositionCellInfo(LevelGrid.Instance.GetGridPosition(forwardPosition), E_GridCheckType.Reserve, m_BaseObject);
            }
        }
        else
        {

        }


        return this;
    }
}
