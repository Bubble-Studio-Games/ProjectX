using Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Define;

[Serializable]
public class AttackPatternInfoClip
{
    public AnimationClip AttackAnimationClip;
}

[Serializable]
public class AttackPatternInfoClipWithReady : AttackPatternInfoClip
{
    public AnimationClip ReadyFailAnimationClip;
}

[Serializable]
public class AttackPattern<TClip> : AttackPattern
    where TClip : AttackPatternInfoClip
{

    public TClip[] m_Clips;
    public override AttackPatternInfoClip[] GetBaseClip() => m_Clips;

    public override bool Validate(bool log = false)
    {
        if (m_Clips == null || m_Clips.Length <= 0 ||
                m_Clips.Any(clip => clip == null) ||
                m_Clips.Any(clip => clip.AttackAnimationClip == null))
        {
            if (log)
                Debug.LogError($"{nameof(TClip)}: 공격 패턴 '{name}'에 클립 배열이 존재하지 않거나 Missing이 존재합니다", this);
            return false;
        }

        if (m_fAttackSpeed.Value <= 0f)
        {
            if (log)
                Debug.LogError($"{nameof(TClip)}: 공격 패턴 '{name}'의 m_fAttackSpeed가 {m_fAttackSpeed.Value}입니다! 1.0 이상으로 설정하세요.", this);
            return false;
        }

        return true;
    }
}

// 데이터
[Serializable]
public abstract class AttackPattern : ScriptableObject
{
    #region 공격 데이터

    [Header("Base Info")]
    public int ID;
    public string AttackName;                    // 예: "전방3칸", "부채꼴" 등
    public E_AttackType m_EAttackType;
    public bool m_IsEnableSelfAttack; // 공격자가 대상자에 포함되는가?, 나도 공격/버프 당할 수 있는가?

    [Header("Range & Shape")]
    public E_RangeFillType m_ERangeFillType;
    public E_RangeShapeType m_ERangeShapeType;
    public List<GridPosition> m_RangeOffset = new();
    private (int MinX, int MaxX, int MinZ, int MaxZ, int MinFloor, int MaxFloor) rangeOffsetMinMax;
    public float m_ArcAngle = 90f;

    [Header("Attack Start Pos")]
    // true이면 시전자 위치를 기준으로, false이면 타겟을 기준으로
    // 타겟이 없으면 해당 공격은 canexecute에서 제외함.
    public bool m_IsAttackStartPositionAtAttacker = true; 

    [Header("Condition")]
    public List<E_GridCheckType> m_GridCheckTypes = new List<E_GridCheckType>();
    public E_TargetTendencyType m_ApplyTargetE_Tendency; // 영향 받을 타겟 성향 ally의 경우 플레이어 유닛은 같은 플레이어 유닛만 스킬 범주에 넣는다.

    [Header("Combo / Chain Links")]
    List<int> m_iNextIds;
    public AttackPattern[] m_iNextAttackPattern;
    public AttackPattern m_iConditionPrevAttackPattern;

    [Header("Condition")]
    public StatValue m_iCoolTime = new StatValue(1, false);
    [HideInInspector] public StatValue m_fLastCooltime = new StatValue(1, false);
    public bool m_bCoolTimeIsFinishied => Time.time - m_fLastCooltime >= m_iCoolTime;
    public StatValue m_iManaCost = new StatValue(0, false);
    public bool m_IsTwoHandAttack; // 두 손 행동인가?
    public bool m_IsIncludeCasterInAttack; // 시전자도 공격 판정의 영향을 받는가?

    [Header("Damage Info")]
    public StatValue m_iPhysicalAttackDamage = new StatValue(0, false);     // 물리 공격 데미지
    public StatValue m_iMagicAttackDamage = new StatValue(0, false);        // 미밥 공격 데미지
    public StatValue m_iPhysicalFixedDamage = new StatValue(0, false);      // 물리 고정 데미지
    public StatValue m_iMagicFixedDamage = new StatValue(0, false);         // 마법 고정 데미지
    public StatValue m_fPhysicalArmorPenetraion = new StatValue(0, false);    // 물리 방어구 관통력
    public StatValue m_fMagicalArmorPenetraion = new StatValue(0, false);     // 마법 방어구 관통력

    [Header("Battle Attack Chance")]
    public StatValue m_iCriticalChance = new StatValue(0, false);     // 치명타율
    public StatValue m_fCriticalDamageUp = new StatValue(1, false);   // 치명타 데미지 증가율
    public StatValue m_fAccuracy = new StatValue(0, false);           // 명중률
    public StatValue m_fAttackSpeed = new StatValue(1, false);        // 공격 속도
    public StatValue m_iKnockbackChance = new StatValue(0, false);    // 넉백 확률
    public StatValue m_fLifeStealPercent = new StatValue(0, false);  // 흡혈 비율 - 피해량 대비

    [Header("Clip")]
    public AudioClip AttackAudioClip;
    public AudioClip[] AttackMissClipList;

    public virtual AttackPatternInfoClip[] GetBaseClip() { return null; }

    #endregion

    #region 공격 로직

    public virtual void Init()
    {
        m_fLastCooltime = -m_iCoolTime;              // 쿨타임 끝난 상태로 시작
        rangeOffsetMinMax = GetRangeMinMaxFromOffsets();
    }

    // 반환은 공격 시전 위치, 성공 여부 이렇게
    public virtual (E_AttackCondition condition, HashSet<GridPosition> CanAttackablePos)
        CanExecute(GameEntity attacker, GameEntity target)
    {
        if (!CheckCoolTime()) return (E_AttackCondition.Fail_CoolTime, default);
        if (!CheckCombo(attacker)) return (E_AttackCondition.Fail_Combo, default);
        if (!CheckMana(attacker)) return (E_AttackCondition.Fail_ManaCost, default);
        var filtered = CheckAttackableGridPositionbyGridType(attacker, target);

        if (filtered == default || filtered.Count == 0)
            return (E_AttackCondition.Fail_ConditionGridType, default);

        if (!CheckDistance(filtered, attacker)) return (E_AttackCondition.Fail_Distance, filtered);

        // 최종 판정
        if (filtered.Count > 0)
        {
            if (attacker.m_AttributeSystem.m_CanMoveableGameEntity)
                return (E_AttackCondition.Success, filtered);
            else
                return default;
        }
        else
            return (E_AttackCondition.Fail_ConditionGridType, default);

        // 그리드 체크 타입에 따른 공격 가능한 위치 반환


    }

    public virtual void StartAttack(GameEntity attacker, GameEntity target, AttackPattern prevAttackpatern) // 실행
    {
        // 쿨타임 갱신
        m_fLastCooltime = Time.time;

        if (attacker is PassiveObject pobj)
        {

        }
        else if (attacker is ControllableObject cobj)
        {
            // 전 준비 단계가 있다면 해시에서 제거
            if (prevAttackpatern != null && prevAttackpatern.m_iNextAttackPattern.Select(p => p.ID).ToArray().Contains(ID))
            {
                cobj.m_ControllableObjectCombatManager.m_ReadyAttackPattern.Remove(prevAttackpatern as AttackPattern_Ready);
            }
        }
        else
        {

        }
    }

    public virtual void Attack(GameEntity attacker, GameEntity target) { } // 종료

    public virtual void EndAttack(GameEntity attacker, GameEntity target) { } // 종료

    public virtual void StartAttackFail(GameEntity attacker, GameEntity target)
    {
        //Debug.Log($"{attacker.name}의 {AttackName} 공격 실패");
    }

    public void EndAttackFail()
    {
        // 쿨타임 갱신
        m_fLastCooltime = Time.time;
    }

    protected IEnumerator ObjectDestroy(GameObject go, float time)
    {
        yield return new WaitForSeconds(time);
        Managers.Resource.Destroy(go);
    }

    #endregion


    public abstract bool Validate(bool log = false);


    public (int MinX, int MaxX, int MinZ, int MaxZ, int MinFloor, int MaxFloor)
    GetRangeMinMaxFromOffsets()
    {
        if (m_RangeOffset == null || m_RangeOffset.Count == 0)
            return (0, 0, 0, 0, 0, 0);

        int minX = 0, maxX = 0;
        int minZ = 0, maxZ = 0;
        int minF = 0, maxF = 0;

        foreach (var o in m_RangeOffset)
        {
            minX = Mathf.Min(minX, o.x);
            maxX = Mathf.Max(maxX, o.x);
            minZ = Mathf.Min(minZ, o.z);
            maxZ = Mathf.Max(maxZ, o.z);
            minF = Mathf.Min(minF, o.floor);
            maxF = Mathf.Max(maxF, o.floor);
        }

        return (minX, maxX, minZ, maxZ, minF, maxF);
    }

    // 패턴에 의해 영향을 받을 그리드 리스트 가져오기
    public virtual List<GridPosition> GetAttackGridPositions(GameEntity attacker, GameEntity target = null) { return default; }

    public List<int> GetNextIds()
    {
        if(m_iNextIds == null)
        {
            m_iNextIds = m_iNextAttackPattern.Select(a => a.ID).ToList();
        }

        return m_iNextIds;
    }

    #region Data Save & Load

    public AttackPatternData CaptureSaveData()
    {
        return new AttackPatternData()
        {
            id = ID,
            coolTime = m_iCoolTime,
            lastCoolTime = m_fLastCooltime,
            manaCost = m_iManaCost,

            physicalAttackDamage = m_iPhysicalAttackDamage,
            magicAttackDamage = m_iMagicAttackDamage,

            physicalFixedDamage = m_iPhysicalFixedDamage,
            magicFixedDamage  = m_iMagicFixedDamage,

            physicalArmorPenetraion = m_fPhysicalArmorPenetraion,
            magicalArmorPenetraion = m_fMagicalArmorPenetraion,

            criticalChance = m_iCriticalChance,
            criticalDamageUp = m_fCriticalDamageUp,

            accuracy = m_fAccuracy,
            attackSpeed = m_fAttackSpeed,
            knockbackChance = m_iKnockbackChance,
            lifeStealPercent = m_fLifeStealPercent,
        };
    }

    public void RestoreSaveData(BaseData data)
    {
        var attackData = data as AttackPatternData;
        m_iCoolTime = attackData.coolTime;
        m_fLastCooltime = attackData.lastCoolTime;
        m_iManaCost = attackData.manaCost;

        m_iPhysicalAttackDamage = attackData.physicalAttackDamage;
        m_iMagicAttackDamage = attackData.magicAttackDamage;

        m_iPhysicalFixedDamage = attackData.physicalFixedDamage;
        m_iMagicFixedDamage = attackData.magicFixedDamage;

        m_fPhysicalArmorPenetraion = attackData.physicalArmorPenetraion;
        m_fMagicalArmorPenetraion = attackData.magicalArmorPenetraion;

        m_iCriticalChance = attackData.criticalChance;
        m_fCriticalDamageUp = attackData.criticalDamageUp;

        m_fAccuracy = attackData.accuracy;
        m_fAttackSpeed = attackData.attackSpeed;
        m_iKnockbackChance = attackData.knockbackChance;
        m_fLifeStealPercent = attackData.lifeStealPercent;
    }

    #endregion

    #region CheckCondition

    bool CheckCoolTime()
    {
        if (!m_bCoolTimeIsFinishied)
            return false;
        return true;
    }

    bool CheckCombo(GameEntity attacker)
    {
        AttackPattern attack = attacker.GetAction<CombatAction>().m_ThisTimeAttack;

        if (m_iConditionPrevAttackPattern != null)
        {
            if (attack == null || attack.ID != m_iConditionPrevAttackPattern.ID)
                return false;
        }

        // 관계 없는 콤보 필터링
        if (attack != null)
        {
            if (attack.m_iNextAttackPattern.Count() > 0 && !attack.GetNextIds().Contains(ID))
                return false;
        }

        return true;
    }

    bool CheckMana(GameEntity attacker)
    {
        if (attacker.m_AttributeSystem.IsManaCharacter())
        {
            if (attacker.m_AttributeSystem.mp < m_iManaCost)
                return false;
        }

        return true;
    }

    HashSet<GridPosition> CheckAttackableGridPositionbyGridType(GameEntity attacker, GameEntity target)
    {
        // 공격 가능한 위치 가져오기
        HashSet<GridPosition> candidates = new HashSet<GridPosition>();
        var attackerGridPosition = attacker.GetGridPosition();

        // false이면 적 타겟을 중심으로 이동 가능한 위치 탐색
        // true이면 공격자를 중심으로 탐색
        if (!m_IsAttackStartPositionAtAttacker)
        {
            // 적을 중심으로 8방향에서 공격할 수 있는 범위 구하기
            candidates = Enumerable.ToHashSet(GetCanAttackPosition(attacker, target));
        }
        else
        {
            candidates = Enumerable.ToHashSet(GetCanAttackPosition(attacker, attacker));
        }

        if(m_GridCheckTypes.Count > 0)
        {
            // 조건에 맞는 그리드를 필터링
            var filtered = m_GridCheckTypes
                                            .Select(t => GetAttackableGridPositionByGridCheckType(candidates, t))
                                            .SelectMany(a => a)  // 일렬로
                                            .ToHashSet<GridPosition>();

            if (filtered.Count == 0)
                return default;
            else
                return filtered;
        }
        else
        {
            return candidates;
        }


        // 타겟을 공격할 수 있는 모든 공격 위치 구하기
        HashSet<GridPosition> GetCanAttackPosition
        (GameEntity owner,
        GameEntity target)
        {
            HashSet<GridPosition> result = new();
            if (owner == null)
                return result;

            // 공격 사거리 오프셋을 이용해서 그리드에서 위치 구하기
            var offsets = Managers.Game.GetPatternOffsets(this);
            var attackerGridPosition = owner.GetGridPosition();
            var targetGridPosition = target.GetGridPosition();

            // 시작 위치(origin) 및 방향(8방향) 계산
            foreach (var dir in Enum.GetValues(typeof(E_Dir)).Cast<E_Dir>())
            {
                foreach (var offset in offsets)
                {
                    GridPosition canAttackPos = LevelGrid.Instance.ToGridPosition(offset, targetGridPosition, dir);

                    // 유효한 범위만 가져오기
                    if (!LevelGrid.Instance.IsValidGridPosition(canAttackPos)) // 유효한 위치만 추가
                        continue;

                    if (m_IsAttackStartPositionAtAttacker == false)
                    {
                        if (attackerGridPosition == canAttackPos)
                            return new HashSet<GridPosition> { canAttackPos };

                        if (!Pathfinding.Instance.HasPath(attackerGridPosition, targetGridPosition))
                            continue;
                    }
                    else
                    {

                    }

                    result.Add(canAttackPos);
                }
            }

            return result;
        }
    }

    bool CheckDistance(HashSet<GridPosition> attackablePositions, GameEntity attacker)
    {
        if(m_IsAttackStartPositionAtAttacker)
        {
            return true;
        }
        else
        {
            return attackablePositions.Contains(attacker.GetGridPosition());
        }
    }

    #endregion

    #region Caculate

    // 공격 가능한 위치에서 범위내 그리드 조건 탐색
    private HashSet<GridPosition> GetAttackableGridPositionByGridCheckType
            (IEnumerable<GridPosition> checkPositions, E_GridCheckType type)
    {
        HashSet<GridPosition> validAttackablePositions = new HashSet<GridPosition>();

        foreach (var grid in checkPositions)
        {
            switch (type)
            {
                // Walable => 빈 자리를 탐색
                // checkPositions에서 공격 범위 내에 공격 오프셋 만큼
                case E_GridCheckType.Walkable:
                    if (!LevelGrid.Instance.IsGridPositionCheckType(grid, E_GridCheckType.Walkable))
                        continue;
                    break;
                // GameEnitity => 공격 사거리 안에 닿는지 체크
                case E_GridCheckType.GameEntity:
                    // 타겟 성향 체크
                    if (m_ApplyTargetE_Tendency == E_TargetTendencyType.Ally)
                    {
                        // 버프나 힐 같은 경우
                        // 버프는 아직 버프가 남아 있다면 냅두고
                        // 힐 같은 경우 최대 체력이 아닌 경우에만 힐을 준다.
                        //CheckGridPositions
                        //    (grid => LevelGrid.Instance.GetObjectAtGridPosition(grid)?.m_AttributeSystem.FullHealth == false,
                        //    position);


                    }

                    else if (m_ApplyTargetE_Tendency == E_TargetTendencyType.Enemy)
                    {

                    }

                    else if (m_ApplyTargetE_Tendency == E_TargetTendencyType.All)
                    {

                    }
                    break;
                // TODO
                case E_GridCheckType.Reserve:
                    break;
                // TODO
                case E_GridCheckType.Obstacle:
                    break;
                // TODO
                case E_GridCheckType.Void:
                    break;
                default:
                    break;
            }

            validAttackablePositions.Add(grid);
        }


        return validAttackablePositions;
    }

    #endregion
}

