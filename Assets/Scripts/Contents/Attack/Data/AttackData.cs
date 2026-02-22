using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

[Serializable]
public class AttackPatternInfoClip
{
    [Header("Audio")]
    public AudioClip AttackSuccessAudioClip;
    public AudioClip AttackMissAudioClip;
    public AudioClip AttackFailAudioClip;

    [Header("Animation")]
    public AnimationClip AttackAnimationClip;
    public AnimationClip ReadyFailAnimationClip;
}

[Serializable]
public  partial class AttackData : ScriptableObject
{
    #region 공격 데이터

    [Header("Base Info")]
    public int Id;
    public string AttackName;                    // 예: "전방3칸", "부채꼴" 등
    public E_AttackType AttackType;
    public bool EnableSelfAttack; // 공격자가 대상자에 포함되는가?, 나도 공격/버프 당할 수 있는가?
    [Range(1, 100)] public int m_iPriority = 1; // 공격 우선순위 (공격 발생 확률이 높다.)

    [Header("Range & Shape")]
    public E_RangeFillType m_ERangeFillType;
    public E_RangeShapeType m_ERangeShapeType;
    public List<GridPosition> m_RangeOffset = new();
    public (int MinX, int MaxX, int MinZ, int MaxZ, int MinFloor, int MaxFloor) m_RangeOffsetMinMax;
    public float m_ArcAngle = 90f;

    [Header("Condition")]
    public List<E_TerrainCellType> m_TerrainGridCheckTypes = new();
    public List<E_EntityCellType> m_EntityGridCheckTypes = new();
    public E_TargetTendencyType ApplyTargetTendency;

    [Header("Combo / Chain Links")]
    public AttackData[] NextAttacks;
    public AttackData ConditionPrevAttack;

    [Header("Condition")]
    [HideInInspector] public StatValue m_fLastCooltime = new StatValue(1, false);
    public bool m_bCoolTimeIsFinishied => Time.time - m_fLastCooltime >= CoolTime;


    [Header("Cost & Flags")]
    public StatValue CoolTime = new StatValue(1, false);
    public StatValue ManaCost = new StatValue(0, false);
    public bool IsTwoHandAttack;

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

    [Header("Clips")]
    public AttackPatternInfoClip[] m_AttackPatternInfoClips;
    [HideInInspector] public AttackPatternInfoClip selectInfoClip;

    #endregion


}

