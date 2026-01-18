using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// NPC가 던전 코어를 향해 이동하는 행동 클래스
/// MoveAction을 상속받아 그리드 기반 이동 처리
/// </summary>
public class NPCMoveAction : MoveAction
{
    //private Transform _dungeonCoreTrans => DungeonCore.instance.transform;

    //public override string GetActionName()
    //{
    //    return "NPC 이동";
    //}

    //public override BaseAction TakeAction(GridPosition gridPosition = default, Action onActionComplete = null)
    //{
    //    if (m_BaseObject.m_ObjectType != E_ObjectType.NPC)
    //    {
    //        Debug.LogError($"NPCMoveAction은 NPC 타입의 게임 엔티티에만 적용될 수 있습니다 - {m_BaseObject.name}");
    //        return null;
    //    }

    //    GridPosition dungeonCoreGridPos = LevelGrid.Instance.GetGridPosition(_dungeonCoreTrans.position);
    //    DestGirdPosition = dungeonCoreGridPos;

    //    GridPosition startPosition = m_BaseObject.GetGridPosition();
    //    List<GridPosition> path = Pathfinding.Instance.FindPath(startPosition, dungeonCoreGridPos, out int pathLength);

    //    if (path == null || path.Count == 0)
    //    {
    //        Debug.LogWarning($"[NPCMoveAction] {m_BaseObject.name}: 던전 코어로의 경로를 찾을 수 없습니다.");
    //        return null;
    //    }

    //    if (path.Count >= Remove_MOVE_GRID)
    //    {
    //        path.RemoveAt(0);  
    //    }

    //    if (path.Count == 0)
    //    {
    //        // npc.OnGoalReached();
    //        return null;
    //    }

    //    forwardPosition = LevelGrid.Instance.GetWorldPosition(path[0]);

    //    LevelGrid.Instance.SetGridPositionCellInfo(path[0], E_GridCheckType.Reserve, m_BaseObject);

    //    SetActionComlete(onActionComplete);
    //    ActionStart(onActionComplete);

    //    Debug.Log($"[NPCMoveAction] {m_BaseObject.name}: 던전 코어로의 경로 발견 (길이: {pathLength})");

    //    return this;
    //}

    //public override void ClearAction()
    //{
    //    base.ClearAction();
    //}
}
