using Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class AttributeSystem : MonoBehaviour
{

    [Header("Attack Pattern")]
    // 원본 공격 패턴
    [SerializeField] private List<AttackData> m_originalAttackPatterns = new List<AttackData>();
    private List<AttackData> attackPatterns = new List<AttackData>();
    public IReadOnlyList<AttackData> m_AttackPatterns => attackPatterns;
    
    // 데이터 로드 후 원본 복사를 2번 하는 것을 방지하기 위해서
    bool m_isAttackPatternInstantiate;


    private void AttackPatternInitInstantiate()
    {
        if (m_isAttackPatternInstantiate)
            return;

        //Debug.Log("공격 패턴 원본 복사");

        if (m_originalAttackPatterns.Count > 0)
        {
            m_originalAttackPatterns = m_originalAttackPatterns
            .Select(pattern => {
                var instance = Instantiate(pattern);

                Managers.Game.AttackPattern(instance).Init(instance);

                // 애니메이션을 스탭 애니메이션 변경
                Managers.Setting.ReplaceAnimationClipsInAttackPattern(m_Stat.Name, instance);
                return instance;
            })
            .ToList();

        }

        ReStoreAttackPattern();
        m_isAttackPatternInstantiate = true;
    }

    private void ReStoreAttackPattern()
    {
        attackPatterns = m_originalAttackPatterns;
    }

    // TODO 개별 공격 패턴 데이터에 관한 저장과 세이트
    //public AttackPatternData CaptureSaveData()
    //{
    //    return new AttackPatternData()
    //    {
    //        id = Id,
    //        coolTime = CoolTime,
    //        lastCoolTime = m_fLastCooltime,
    //        manaCost = ManaCost,

    //        physicalAttackDamage = m_iPhysicalAttackDamage,
    //        magicAttackDamage = m_iMagicAttackDamage,

    //        physicalFixedDamage = m_iPhysicalFixedDamage,
    //        magicFixedDamage = m_iMagicFixedDamage,

    //        physicalArmorPenetraion = m_fPhysicalArmorPenetraion,
    //        magicalArmorPenetraion = m_fMagicalArmorPenetraion,

    //        criticalChance = m_iCriticalChance,
    //        criticalDamageUp = m_fCriticalDamageUp,

    //        accuracy = m_fAccuracy,
    //        attackSpeed = m_fAttackSpeed,
    //        knockbackChance = m_iKnockbackChance,
    //        lifeStealPercent = m_fLifeStealPercent,
    //    };
    //}

    //public void RestoreSaveData(BaseData data)
    //{
    //    var attackData = data as AttackPatternData;
    //    CoolTime = attackData.coolTime;
    //    m_fLastCooltime = attackData.lastCoolTime;
    //    ManaCost = attackData.manaCost;

    //    m_iPhysicalAttackDamage = attackData.physicalAttackDamage;
    //    m_iMagicAttackDamage = attackData.magicAttackDamage;

    //    m_iPhysicalFixedDamage = attackData.physicalFixedDamage;
    //    m_iMagicFixedDamage = attackData.magicFixedDamage;

    //    m_fPhysicalArmorPenetraion = attackData.physicalArmorPenetraion;
    //    m_fMagicalArmorPenetraion = attackData.magicalArmorPenetraion;

    //    m_iCriticalChance = attackData.criticalChance;
    //    m_fCriticalDamageUp = attackData.criticalDamageUp;

    //    m_fAccuracy = attackData.accuracy;
    //    m_fAttackSpeed = attackData.attackSpeed;
    //    m_iKnockbackChance = attackData.knockbackChance;
    //    m_fLifeStealPercent = attackData.lifeStealPercent;
    //}
}