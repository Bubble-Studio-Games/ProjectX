using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;
using static StatSystem;

public class StatSystem : MonoBehaviour
{
    public event EventHandler OnRevived; // 	HP 회복 등으로 다시 살아날 때
    public event EventHandler<OnAttackInfoEventArgs> OnDead; // HP 0일 때 죽는 순간
    public event EventHandler<OnAttackInfoEventArgs> OnDamaged; // 데미지를 받았을 때
    public event EventHandler OnMPUsed;

    public class OnAttackInfoEventArgs : EventArgs
    {
        public AttackPattern AttackPattern;
        public E_HitDecisionType EHitDeCisionType;
        public GameEntity Attacker;
        public int FinalDamage;
    }

    public BaseStat m_Stat;

    [SerializeField] private int health { get => m_Stat.m_iCurrentHp;  set { m_Stat.m_iCurrentHp = value; } }
    private int healthMax { get => m_Stat.m_iMaxHP; set { m_Stat.m_iMaxHP = value; } }

    [SerializeField] private int mp { get => m_Stat.m_iCurrentMP;  set { m_Stat.m_iCurrentMP = value; } }
    private int mpMax { get => m_Stat.m_iMaxMP; set { m_Stat.m_iMaxMP = value; } }

    public bool m_IsDead => health == 0;

    private void Start()
    {
        // 스텟을 개별적으로 갖게 하기
        // TODO 나중에 DB로 가져오기
        m_Stat = Instantiate(m_Stat);

        // 공격
        m_Stat.m_AttackPatterns = m_Stat.m_AttackPatterns
        .Select(pattern =>
        {
            var instance = Instantiate(pattern);
            return instance;
        })
        .ToList();

        Init();
    }

    protected virtual void OnEnable()
    {
        // 풀로 다시 소환할 때 체력 및 마나 리셋
        // TODO 공격 쿨타임 등도 다 리셋 예정
        Init();
    }

    private void Init()
    {
        // HP
        health = healthMax;

        // MP
        mp = mpMax;

    }

    public void Hit(AttackPattern attack, ControllableObject attacker)
    {
        // 사망시 타격 판정 불가
        if (m_IsDead)
            return;

        E_HitDecisionType hitDecision = E_HitDecisionType.Hit;

        int rand = UnityEngine.Random.Range(0, 101); // 0 이상 100 이하의 정수

        // 0. 명중률 체크 (맞지 않으면 끝)
        if (rand > attack.m_fAccuracy)
        {
            EventOnDamaged(attack, E_HitDecisionType.AttackMiss, attacker);
            return;
        }

        // 1. 회피 체크 (우선적으로 처리)
        if (rand < m_Stat.m_fEvasionChance)
        {
            EventOnDamaged(attack, E_HitDecisionType.Evasion, attacker);
            return; // 공격 무효화
        }

        // 2. 치명타 체크
        if (rand < attack.m_iCriticalChance)
            hitDecision = E_HitDecisionType.CriticalHit;

        // 3. 반격 체크 (반격 여부만 판단, 실제 반격 수행은 CombatAction 등에서)
        if (rand < m_Stat.m_fCounterAttackChance)
            EventOnDamaged(attack, E_HitDecisionType.Counter, attacker);

        // 4. 최종 피해 적용
        ApplyDamage(attack, hitDecision, attacker);
    }

    public void ApplyDamage(AttackPattern attack, E_HitDecisionType hitDecision, ControllableObject attacker)
    {
        int finalDamage;

        if (m_Stat.m_iIsStepReduceHP)
        {
            finalDamage = 1;
        }
        else
        {
            // 유효 방어력 = 방어력 × (1 - 방어구 관통력)
            // 순수 데미지 = 공격력 + 고정 데미지 - 유효 방어력
            // 최종 데미지 = max(고정 데미지, 순수 데미지)

            float physicalAttack = attack.m_iPhysicalAttackDamage;
            float magicAttack = attack.m_iMagicAttackDamage;

            // 치명타 적용 (공격력에만 영향, 고정 데미지는 영향 없음)
            if (hitDecision == E_HitDecisionType.CriticalHit)
            {
                physicalAttack *= 1f + attack.m_fCriticalDamageUp;
                magicAttack *= 1f + attack.m_fCriticalDamageUp;
            }

            // 물리 유효 방어력 계산
            float effectivePhysicalDef = m_Stat.m_iPhysicalDefence * (1f - attack.m_fPhysicalArmorPenetraion);
            float physicalRawDamage = physicalAttack + attack.m_iPhysicalFixedDamage - effectivePhysicalDef;
            int physicalDamage = Mathf.RoundToInt(Mathf.Max(attack.m_iPhysicalFixedDamage, physicalRawDamage));

            // 마법 유효 방어력 계산
            float effectiveMagicalDef = m_Stat.m_iMagicalDefence * (1f - attack.m_fMagicalArmorPenetraion);
            float magicalRawDamage = magicAttack + attack.m_iMagicFixedDamage - effectiveMagicalDef;
            int magicalDamage = Mathf.RoundToInt(Mathf.Max(attack.m_iMagicFixedDamage, magicalRawDamage));

            // 최종 데미지 합산
            finalDamage = physicalDamage + magicalDamage;
        }


        // 체력 감소
        health = Mathf.Max(0, health - finalDamage);

        var info = new OnAttackInfoEventArgs()
        {
            AttackPattern = attack,
            EHitDeCisionType = hitDecision,
            Attacker = attacker,
            FinalDamage = finalDamage,

        };

        if (health == 0)
        {
            // 사망 처리
            OnDead?.Invoke(this, info);
        }
        else if (health > 0)
        {
            // 데미지 이벤트 호출
            OnDamaged?.Invoke(this, info);
        }
    }

    public void EventOnDamaged(AttackPattern pattern, E_HitDecisionType type, GameEntity attacker)
    {
        OnDamaged?.Invoke(this, new OnAttackInfoEventArgs { AttackPattern = pattern, EHitDeCisionType = type, Attacker=attacker}); 
    }

    public void ReduceMP(int count)
    {
        mp = Math.Max(0, mp - count);

        OnMPUsed?.Invoke(this, EventArgs.Empty);
    }

    public float GetHealthNormalized()
    {
        return (float)health / healthMax;
    }

    public float GetManaNormalized()
    {
        return (float)mp / mpMax;
    }

    public bool IsManaCharacter()
    {
        return m_Stat.m_iMaxMP > 0;
    }
}
