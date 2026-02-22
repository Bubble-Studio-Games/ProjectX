using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class MoveAction : BaseAction
{
    public event Action OnStartMoving;
    public event Action OnStopMoving;
    public event Action OnUpdateGrid;

    public event Action<OnChangeFloorsStartedEventArgs> OnChangedFloorsStarted;

    protected Vector3 forwardPosition;

    [Header("Change Floor")]
    protected bool isChangingFloors;
    protected float differentFloorsTeleportTimer;
    protected float differentFloorsTeleportTimerMax = .5f;

    [Header("Path Finding")]
    protected int m_iPathMaxCount = 2;
    protected int m_iPathCurrentCount = 0;

    private void OnEnable()
    {
        if (m_GameEntity.m_TeamId == E_TeamId.Player)
            OnUpdateGrid += DrawGridVisual;

        OnUpdateGrid += UpdateGridEntity;
    }

    private void OnDisable()
    {
        if (m_GameEntity.m_TeamId == E_TeamId.Player)
            OnUpdateGrid -= DrawGridVisual;

        OnUpdateGrid -= UpdateGridEntity;
    }

    private void UpdateGridEntity()
    {
        m_GameEntity.UpdateGridPosition();
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

        if (m_GameEntity.m_AttributeSystem.m_IsDead)
            return;

        if (!m_GameEntity.m_AttributeSystem.m_CanMoveableGameEntity)
            return;

        Vector3 targetPosition = forwardPosition;

        if (isChangingFloors)
        {
            // Stop and Teleport Logic
            Vector3 targetSameFloorPosition = targetPosition;
            targetSameFloorPosition.y = m_GameEntity.transform.position.y;

            Vector3 rotateDirection = (targetSameFloorPosition - m_GameEntity.transform.position).normalized;

            float rotateSpeed = 10f;
            m_GameEntity.transform.forward = Vector3.Slerp(m_GameEntity.transform.forward, rotateDirection, Time.deltaTime * rotateSpeed);

            differentFloorsTeleportTimer -= Time.deltaTime;
            if (differentFloorsTeleportTimer < 0f)
            {
                isChangingFloors = false;
                m_GameEntity.transform.position = targetPosition;
            }
        }
        else
        {
            // Regular move logic
            Vector3 moveDirection = (targetPosition - m_GameEntity.transform.position).normalized;

            float rotateSpeed = 10f;
            m_GameEntity.transform.forward = Vector3.Slerp(m_GameEntity.transform.forward, moveDirection, Time.deltaTime * rotateSpeed);

            m_GameEntity.transform.position += moveDirection * m_GameEntity.m_AttributeSystem.GetMoveSpeed() * Time.deltaTime;
        }

        // 다음 그리드 도착
        float stoppingDistance = .1f;
        if (Vector3.Distance(m_GameEntity.transform.position, targetPosition) < stoppingDistance)
        {
            forwardPosition = default;
            OnUpdateGrid?.Invoke();
            m_bIsActive = false;

            // 최종 목적지에 도착했는지 여부 따지기
            if (DestGirdPosition == m_GameEntity.GetGridPosition())
            {
                ActionComplete();
            }
        }
    }

    public override BaseAction TakeAction(GridPosition gridPosition)
    {
        return this;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        throw new NotImplementedException();
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        return default;
    }

    public override void ActionStart()
    {
        base.ActionStart();
        OnStartMoving?.Invoke();
    }

    protected override void ActionComplete()
    {
        base.ActionComplete();

        OnStopMoving?.Invoke();
        m_iPathCurrentCount = 0;
    }

    public override void ClearAction()
    {
        base.ClearAction();

        if (forwardPosition != default)
        {
            Managers.Grid.ReleaseCell(
                Managers.Grid.GetGridPosition(forwardPosition), m_GameEntity);
            forwardPosition = default;
            m_iPathCurrentCount = 0;
        }
    }

}
