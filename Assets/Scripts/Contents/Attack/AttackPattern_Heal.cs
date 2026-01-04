using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Attack Pattern/Heal")]
public class AttackPattern_Heal : AttackPattern<AttackPatternInfoClip>
{
    public StatValue m_fHealAmount = new StatValue(0, false);  // 힐 총량
    public Effect m_HealEffectPrefab;

    public override void Attack(GameEntity attacker, GameEntity target) // 종료
    {
        // 공격하려는 그리드에 오브젝트 정보 가져오기
        var targets = GetAttackGridPositions(attacker, target)
            .Select(p => LevelGrid.Instance.GetObjectAtGridPosition(p))
            .ToList();

        if(targets.Count > 0 )
        {
            foreach (var t in targets)
            {
                t.m_AttributeSystem.Heal(m_fHealAmount, E_HealType.None, attacker);
                if(m_HealEffectPrefab != null)
                    Managers.Resource.Instantiate(m_HealEffectPrefab.gameObject, t.transform.position, t.transform.rotation);
            }
        }
    }

    public override List<GridPosition> GetAttackGridPositions(GameEntity attacker, GameEntity target)
    {
        // 기본적으로 범위 내의 모든 적들을 공격함.
        GridPosition selfPos = attacker.GetGridPosition();

        if (target == null || target.IsDead)
            return default;

        GridPosition targetPos = target.GetGridPosition();
        E_Dir dir = LevelGrid.Instance.GetDirGridPosition(selfPos, targetPos);

        HashSet<GridPosition> attackGridOffsets = Managers.Game.GetPatternOffsets(this);
        
        // 실제 공격 범위 내 좌표 변환 및 필터링
        HashSet <GridPosition> attackedGridPositions = attackGridOffsets
            .Select(offset => LevelGrid.Instance.ToGridPosition(offset, selfPos, dir))
            .Where(pos => LevelGrid.Instance.IsGridPositionCheckType(pos, m_GridCheckTypes))
            .ToHashSet();

        // 만약 그리드 체크 타입이 유닛이라면 적인지 아군인지 구분
        if (m_GridCheckTypes.Contains(E_GridCheckType.GameEntity))
        {
            // 공격 대상이 플레이어 진영인지 / 적 진영인지에 따라 필터링
            switch (m_ApplyTargetE_Tendency)
            {
                case E_TargetTendencyType.All:
                    break;
                case E_TargetTendencyType.Ally: // 플레이어 아군만 적용
                    attackedGridPositions = attackedGridPositions
                        .Where(pos =>
                        {
                            var obj = LevelGrid.Instance.GetObjectAtGridPosition(pos);
                            return obj != null && !attacker.IsEnemy(obj);
                        })
                        .ToHashSet();
                    break;

                case E_TargetTendencyType.Enemy: // 적 유닛만 적용
                    attackedGridPositions = attackedGridPositions
                        .Where(pos =>
                        {
                            var obj = LevelGrid.Instance.GetObjectAtGridPosition(pos);
                            return obj != null && attacker.IsEnemy(obj);
                        })
                        .ToHashSet();
                    break;

                case E_TargetTendencyType.Neutral: // 중립 유닛은 양쪽 다 무시
                    attackedGridPositions.Clear();
                    break;
            }
        }

        //Debug.Log($"{attacker.name}가 {target}을 향해 공격함. 타겟 그리드 {string.Join(" ", attackedGridPositions)}");

        return attackedGridPositions.ToList();
    }
}
