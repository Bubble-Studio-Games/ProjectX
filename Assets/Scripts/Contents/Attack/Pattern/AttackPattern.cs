using static Define;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;
using Unity.VisualScripting;

public abstract class AttackPattern
{
    public virtual void Init(AttackData data)
    {
        data.m_fLastCooltime = -data.CoolTime;
    }

    public virtual (E_AttackCondition condition, List<GridPosition> CanAttackablePos)
        CanExecute(GameEntity attacker, GameEntity target, AttackData checkData, AttackData thisTimeAttackData)
    {
        // 쿨타임
        if (!CheckCoolTime(checkData))
            return (E_AttackCondition.Fail_CoolTime, default);

        // 콤보
        if (!CheckCombo(attacker, checkData, thisTimeAttackData))
            return (E_AttackCondition.Fail_Combo, default);

        // 마나
        if (!CheckMana(attacker, checkData))
            return (E_AttackCondition.Fail_ManaCost, default);

        // 공격 가능 위치 가져오기
        var attackableGridPositions = GetAttackableGridPosition(attacker, target, checkData);

        // 그리드 타입
        var filtered = GetGridPositionByCheckType(attackableGridPositions, attacker, target, checkData).ToList();
        if (filtered == default || filtered.Count == 0)
            return (E_AttackCondition.Fail_ConditionGridType, default);

        if (attacker.m_AttributeSystem.m_CanMoveableGameEntity)
        {
            if (!CheckCanReach(attacker, ref filtered))
                return (E_AttackCondition.Fail_HasNotMovableGridPosition, filtered);

            if (!CheckDistance(filtered, attacker))
                return (E_AttackCondition.Fail_Distance, filtered);
        }

        return (E_AttackCondition.Success, filtered);
    }

    public virtual void StartAttack(GameEntity attacker, GameEntity target, AttackData checkAttackData, AttackData prevAttackData)
    {
        // 쿨타임 갱신 (SO가 아니라 per-unit state로)
        checkAttackData.m_fLastCooltime = Time.time;

        // 전 준비 단계 제거(기존 코드 그대로)
        if (prevAttackData != null && prevAttackData.NextAttacks.Select(p => p.Id).Contains(checkAttackData.Id))
            attacker.m_CombatManager.m_ReadyAttackPattern.Remove(prevAttackData as AttackData_Ready);

        // 클립 선택 결과도 state로
        checkAttackData.selectInfoClip = SelectClip(checkAttackData);
    }

    /// <summary>
    /// 애니메이션의 공격 이벤트가 실행이 될 때 발생하는 이벤트
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="target"></param>
    public virtual void Attack(GameEntity attacker, GameEntity target, AttackData checkAttackData)
    {
        // Reduce Mana
        attacker.m_AttributeSystem.ReduceMP((int)checkAttackData.ManaCost.Value);
    }

    /// <summary>
    /// 공격이 종료 되었을 때
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="target"></param>
    public virtual void EndAttack(GameEntity attacker, GameEntity target, AttackData checkAttackData) { }

    public virtual void StartAttackFail(GameEntity attacker, GameEntity target, AttackData checkAttackData) { }

    public virtual void EndAttackFail(GameEntity attacker, AttackData checkAttackData)
    {
        checkAttackData.m_fLastCooltime = Time.time;
    }

    #region CanExecute의 조건문들

    protected bool CheckCoolTime(AttackData checkAttackData)
        => Time.time - checkAttackData.m_fLastCooltime >= checkAttackData.CoolTime;

    protected bool CheckCombo(GameEntity attacker, AttackData checkData, AttackData thisTimeAttack)
    {
        // A 공격을 실행했으면 A 공격을 또 실행하면 안된다.
        if (thisTimeAttack != null && checkData == thisTimeAttack)
            return false;

        // 1) "이 공격은 특정 이전 공격 뒤에만 가능" 조건
        if (checkData.ConditionPrevAttack != null)
        {
            if (thisTimeAttack == null || thisTimeAttack.Id != checkData.ConditionPrevAttack.Id)
                return false;
        }

        // 2) "이전 공격의 NextAttacks가 지정돼 있다면, 그 안에 이번 공격이 있어야 함"
        if (thisTimeAttack != null && thisTimeAttack.NextAttacks != null && thisTimeAttack.NextAttacks.Length > 0)
        {
            if (!Managers.Game.NextIdSetCache.TryGetValue(thisTimeAttack.Id, out var nextSet))
                return false; // 캐시가 없다면 데이터/초기화 문제

            if (!nextSet.Contains(checkData.Id))
                return false;
        }

        return true;
    }


    protected bool CheckMana(GameEntity attacker, AttackData checkAttackData)
    {
        if (attacker.m_AttributeSystem.IsManaCharacter())
            return attacker.m_AttributeSystem.mp >= checkAttackData.ManaCost;
        return true;
    }

    protected bool IsValidTargetTendency(GridPosition grid, GameEntity attacker, AttackData checkAttackData)
    {
        var target = Managers.Grid.GetUnitAt(grid);
        if (target == null) return false;

        switch (checkAttackData.ApplyTargetTendency)
        {
            case E_TargetTendencyType.Ally: return IsValidAllyTarget(attacker, target);
            case E_TargetTendencyType.Enemy: return IsValidEnemyTarget(attacker, target);
            case E_TargetTendencyType.All: return true;
            default: return false;
        }
    }

    protected virtual bool IsValidAllyTarget(GameEntity attacker, GameEntity target)
    {
        return attacker.IsAlly(target); // 또는 팀 비교로 구현
    }

    protected virtual bool IsValidEnemyTarget(GameEntity attacker, GameEntity target)
    {
        return attacker.IsEnemy(target); // 또는 팀 비교 방식
    }

    protected bool CheckCanReach(GameEntity attacker, ref List<GridPosition> list)
    {
        var a = attacker.GetGridPosition();
        list = list.Where(pos => Managers.Path.HasPath(a, pos)).ToList();
        return list.Count > 0;
    }

    protected bool CheckDistance(IEnumerable<GridPosition> attackablePositions, GameEntity attacker)
        => attackablePositions.Contains(attacker.GetGridPosition());

    #endregion

    /// <summary>
    /// attacker가 target을 향해 AttackData의 사거리를 이용하여 8방향에서 공격 위치 구하기
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="target"></param>
    /// <param name="checkAttackData"></param>
    /// <returns></returns>
    protected HashSet<GridPosition> GetAttackableGridPosition(GameEntity attacker, GameEntity target, AttackData checkAttackData)
    {
        HashSet<GridPosition> attackablePosition = new();

        if (attacker.m_AttributeSystem.m_CanMoveableGameEntity)
        {
            var attackerGridPosition = attacker.GetGridPosition();
            var targetGridPosition = target.GetGridPosition();

            var offsets = Managers.Game.GetPatternOffsets(checkAttackData);

            foreach (var dir in Enum.GetValues(typeof(E_Dir)).Cast<E_Dir>())
            {
                foreach (var offset in offsets)
                {
                    GridPosition canAttackPos = Util.ToGridPosition(offset, targetGridPosition, dir);

                    if (!Managers.Grid.CanMoveTo(canAttackPos, attackerGridPosition) &&
                        canAttackPos != attackerGridPosition)
                        continue;

                    attackablePosition.Add(canAttackPos);
                }
            }
        }
        else
        {
            attackablePosition.Add(attacker.GetGridPosition());
        }

        return attackablePosition;
    }

    protected IEnumerable<GridPosition> GetGridPositionByCheckType(
        IEnumerable<GridPosition> attackableGridPositions, GameEntity attacker, GameEntity target, AttackData checkAttackData)
    {
        HashSet<GridPosition> result = new();
        var offsets = Managers.Game.GetPatternOffsets(checkAttackData);

        GridPosition targetGridPosition = target == null ? attacker.GetGridPosition() : target.GetGridPosition();

        foreach (var attackable in attackableGridPositions)
        {
            E_Dir dir = Util.GetDirGridPosition(attackable, targetGridPosition);

            bool isValid = offsets.Any(offset =>
            {
                GridPosition pos = Util.ToGridPosition(offset, attackable, dir);
                return GetGridListValidByCheckTypes(pos, attacker, checkAttackData);
            });

            if (isValid) result.Add(attackable);
        }

        return result;
    }

    protected bool GetGridListValidByCheckTypes(
        GridPosition checkGridPosition,
        GameEntity attacker,
        AttackData checkAttackData)
    {
        bool hasTerrainChecks = checkAttackData.m_TerrainGridCheckTypes != null &&
                                checkAttackData.m_TerrainGridCheckTypes.Count > 0;

        bool hasEntityChecks = checkAttackData.m_EntityGridCheckTypes != null &&
                                checkAttackData.m_EntityGridCheckTypes.Count > 0;

        // ✅ 아무 조건도 없으면: "대상이 존재하고 적이면 true" (기존 동작 유지)
        if (!hasTerrainChecks && !hasEntityChecks)
        {
            var t = Managers.Grid.GetUnitAt(checkGridPosition);
            if (t == null) return false;
            return !attacker.IsAlly(t);
        }

        // 1) Terrain 체크
        if (hasTerrainChecks)
        {
            var terrain = Managers.Grid.GetTerrainType(checkGridPosition);
            foreach (var type in checkAttackData.m_TerrainGridCheckTypes)
            {
                if (terrain != type)
                    return false;
            }
        }

        // 2) Entity 체크
        if (hasEntityChecks)
        {
            foreach (var type in checkAttackData.m_EntityGridCheckTypes)
            {
                switch (type)
                {
                    // ✅ "유닛(공격 가능한 타겟)" 체크는 단순 점유가 아니라 성향/팀 판정이 필요
                    case E_EntityCellType.GameEntity:
                        if (!IsValidTargetTendency(checkGridPosition, attacker, checkAttackData))
                            return false;
                        break;

                    // ✅ 예약 체크(플래그/단일값 여부에 따라 구현 달라짐)
                    case E_EntityCellType.Reserve:
                        if (Managers.Grid.GetEntityCellType(checkGridPosition) != E_EntityCellType.Reserve)
                            return false;
                        break;

                    // 필요 시 Blocker, Building 등 추가
                    default:
                        if (Managers.Grid.GetEntityCellType(checkGridPosition) != type)
                            return false;
                        break;
                }
            }
        }

        return true;
    }

    protected IEnumerator ObjectDestroy(GameObject go, float time)
    {
        yield return new WaitForSeconds(time);
        Managers.Resource.Destroy(go);
    }

    protected virtual AttackPatternInfoClip SelectClip(AttackData checkAttackData)
        => checkAttackData.m_AttackPatternInfoClips.RandomPick();

    /// <summary>
    /// 현재 공격자의 위치를 기준으로 공격 가능한 전체 사거리(범위)와,
    /// 특정 타겟을 지정한 경우 타겟 위치를 공격할 수 있는 실제 타겟팅 위치 목록을 반환한다.
    /// </summary>
    /// <param name="attacker">공격 범위를 계산할 공격자 유닛</param>
    /// <param name="target">선택적 타겟 유닛 (없으면 null)</param>
    /// <returns>
    /// attackRangeGridList : 공격 사거리에 포함되는 그리드 목록
    /// targetGridList : (타겟이 있을 경우) 타겟을 공격할 수 있는 유효 타겟 위치 그리드 목록
    /// </returns>
    public virtual (IEnumerable<GridPosition> attackRangeGridList, IEnumerable<GridPosition> targetGridList)
        GetAttackGridPositions(GameEntity attacker, GameEntity target, AttackData checkAttackData)
    {
        // 현재 위치에서 공격 사거리 그리드 구하기
        var rangeGridList = GetAttackRangeGridPositions(attacker.GetGridPosition(), target, checkAttackData);

        // 조건을 만족하는 그리드 가져오기
        var targetList = GetAttackSelectGridPositions(rangeGridList, attacker, target, checkAttackData).ToList();

        return (rangeGridList, targetList);
    }

    /// <summary>
    /// // 공격 사거리 안에 특정 조건을 만족하는 오브젝트 가져오기
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="target"></param>
    /// <param name="checkAttackData"></param>
    /// <returns></returns>
    public virtual IEnumerable<GameEntity>
    GetAttackTargetGridPositions(GameEntity attacker, GameEntity target, AttackData checkAttackData)
        => GetAttackGridPositions(attacker, target, checkAttackData).targetGridList.Select(p => Managers.Grid.GetUnitAt(p));

    /// <summary>
    /// 공격 사거리 범위 구하기
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public IEnumerable<GridPosition> GetAttackRangeGridPositions(GridPosition attackerGridPosition, GameEntity target, AttackData checkAttackData)
    {
        E_Dir dir;
        if (target == null)
        {
            dir = Util.GetDirGridPosition(attackerGridPosition, attackerGridPosition);
        }
        else
        {
            var targetGridPosition = target.GetGridPosition();
            dir = Util.GetDirGridPosition(attackerGridPosition, targetGridPosition);
        }

        var offsets = Managers.Game.GetPatternOffsets(checkAttackData);
        HashSet<GridPosition> candidates = new();

        foreach (var offset in offsets)
        {
            GridPosition canAttackPos = Util.ToGridPosition(offset, attackerGridPosition, dir);

            if (!Managers.Grid.IsValidGridPosition(canAttackPos)) // 유효한 위치만 추가
                continue;

            candidates.Add(canAttackPos);
        }

        // 공격자도 공격 범위에 포함되는가?
        if (checkAttackData.EnableSelfAttack == false)
            candidates.Remove(attackerGridPosition);

        return candidates;
    }

    protected virtual IEnumerable<GridPosition> GetAttackSelectGridPositions
        (IEnumerable<GridPosition> rangeGridList, GameEntity attacker, GameEntity target, AttackData checkAttackData)
    => rangeGridList.Where(pos => GetGridListValidByCheckTypes(pos, attacker, checkAttackData));
}

public abstract class AttackPattern<TData> : AttackPattern
    where TData : AttackData
{
    public virtual void Init(TData data)
        => base.Init(data);

    public sealed override void Init(AttackData data)
        => Init((TData)data);

    // ✅ 패턴 구현 클래스는 이 메서드만 구현하면 됨(강타입)
    // 기본 구현이 필요하면 여기 두고,
    // 대부분은 각 패턴이 override
    public virtual void Attack(GameEntity attacker, GameEntity target, TData checkAttackData)
        => base.Attack(attacker, target, checkAttackData);

    // ✅ 외부에서 들어오는 AttackData를 자동으로 TData로 변환해서 위로 넘김
    public sealed override void Attack(GameEntity attacker, GameEntity target, AttackData checkAttackData)
        => Attack(attacker, target, (TData)checkAttackData);

    // 같은 방식으로 CanExecute/StartAttack/EndAttack도 강타입 버전 제공 가능
    public virtual (E_AttackCondition condition, List<GridPosition> CanAttackablePos)
        CanExecute(GameEntity attacker, GameEntity target, TData checkAttackData, AttackData thisTimeAttackData)
        => base.CanExecute(attacker, target, checkAttackData, thisTimeAttackData);

    public sealed override (E_AttackCondition condition, List<GridPosition> CanAttackablePos)
        CanExecute(GameEntity attacker, GameEntity target, AttackData checkAttackData, AttackData thisTimeAttackData)
        => CanExecute(attacker, target, (TData)checkAttackData, thisTimeAttackData);

    public virtual void StartAttack(GameEntity attacker, GameEntity target, TData checkAttackData, AttackData prev)
        => base.StartAttack(attacker, target, checkAttackData, prev);

    public sealed override void StartAttack(GameEntity attacker, GameEntity target, AttackData checkAttackData, AttackData prev)
        => StartAttack(attacker, target, (TData)checkAttackData, prev);

    public virtual void EndAttack(GameEntity attacker, GameEntity target, TData checkAttackData)
        => base.EndAttack(attacker, target, checkAttackData);

    public sealed override void EndAttack(GameEntity attacker, GameEntity target, AttackData checkAttackData)
        => EndAttack(attacker, target, (TData)checkAttackData);

    public virtual void StartAttackFail(GameEntity attacker, GameEntity target, TData checkAttackData)
        => base.StartAttackFail(attacker, target, checkAttackData);

    public sealed override void StartAttackFail(GameEntity attacker, GameEntity target, AttackData checkAttackData)
        => StartAttackFail(attacker, target, (TData)checkAttackData);

    public virtual void EndAttackFail(GameEntity attacker, TData checkAttackData) 
        => base.EndAttackFail(attacker, checkAttackData);

    public sealed override void EndAttackFail(GameEntity attacker, AttackData checkAttackData)
        => EndAttackFail(attacker, (TData)checkAttackData);

    protected virtual AttackPatternInfoClip SelectClip(TData checkAttackData)
        => base.SelectClip(checkAttackData);

    protected sealed override AttackPatternInfoClip SelectClip(AttackData checkAttackData)
        => SelectClip((TData)checkAttackData);
}




