using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Attack Pattern/Melee")]
public class AttackPattern_Melee : AttackPattern<AttackPatternInfoClip>
{
    public override void StartAttack(ControllableObject attacker, GameEntity target, AttackPattern prevAttackpatern)
    {
        base.StartAttack(attacker, target, prevAttackpatern);
        
        // 전 준비 단계가 있다면 해시에서 제거
        if (prevAttackpatern != null && prevAttackpatern.m_iNextAttackPattern.Select(p => p.ID).ToArray().Contains(ID))
        {
            attacker.m_ControllableObjectCombatManager.m_ReadyAttackPattern.Remove(prevAttackpatern as AttackPattern_Ready);
        }
    }

    public override void Attack(ControllableObject attacker, GameEntity target) // 종료
    {
        // 기본적으로 범위 내의 모든 적들을 공격함.
        GridPosition selfPos = attacker.GetGridPosition();
        GridPosition targetPos = target.GetGridPosition();
        E_Dir dir = LevelGrid.Instance.GetDirGridPosition(selfPos, targetPos);

        // 범위 내의 공격할 수 있는 모든 그리드 체크
        HashSet<GridPosition> attackGridPostison =
            m_RangeOffset
            .Select(x => LevelGrid.Instance.ToGridPosition(x, selfPos, dir))
            .Where(pos => LevelGrid.Instance.IsValidGridPosition(pos))
            .ToHashSet();

        // 해당 오브젝트가 공격자 유닛과 적인지 체크
        var targets = attackGridPostison
            .Select(p => LevelGrid.Instance.GetObjectAtGridPosition(p))
            .Where(unit => unit != null && attacker.IsEnemy(unit))
            .ToList();

        //if (E_AttackEffectType == E_AttackEffectType.Damage)
        {
            foreach (var t in targets)
                t.m_AttributeSystem.Hit(this, attacker);
        }
    }
}
