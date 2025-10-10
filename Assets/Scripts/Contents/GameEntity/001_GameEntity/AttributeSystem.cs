using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Define;
using static AttributeSystem;
using Random = UnityEngine.Random;

public class AttributeSystem : MonoBehaviour
{
    public event EventHandler OnRevived; // 	HP 회복 등으로 다시 살아날 때
    public event EventHandler<OnAttackInfoEventArgs> OnDead; // HP 0일 때 죽는 순간
    public event EventHandler<OnAttackInfoEventArgs> OnDamaged; // 데미지를 받았을 때
    public event EventHandler OnUpdateStat;

    public class OnAttackInfoEventArgs : EventArgs
    {
        public AttackPattern AttackPattern;
        public E_HitDecisionType EHitDeCisionType;
        public GameEntity Attacker;
        public int FinalDamage;
    }

    [Header("Stat")]
    [SerializeField] private BaseStat m_originalStat;
    public BaseStat m_Stat
    {
        get 
        { 
            if(m_originalStat == null)
                m_originalStat = GetComponent<BaseStat>();

            return m_originalStat;
        }
        set { m_originalStat = value; }
    }

    [Header("Attack Pattern")]
    public List<AttackPattern> m_AttackPatterns = new List<AttackPattern>();

    private GameEntity m_GameEntity;

    [Header("Reward")]
    public RewardData m_RewardData;

    #region Stat 약칭

    [SerializeField] private StatValue health { get => m_Stat.m_iCurrentHp;  set { m_Stat.m_iCurrentHp = value; } }
    public StatValue healthMax { get => m_Stat.m_iMaxHP; set { m_Stat.m_iMaxHP = value; } }

    [SerializeField] private StatValue mp { get => m_Stat.m_iCurrentMP;  set { m_Stat.m_iCurrentMP = value; } }
    public StatValue mpMax { get => m_Stat.m_iMaxMP; set { m_Stat.m_iMaxMP = value; } }

    public bool m_IsDead => health == 0;

    #endregion

    private void Awake()
    {
        m_GameEntity = GetComponent<GameEntity>();

        // Event

        OnDead += (s, e) => Reward();


        // 스텟을 개별적으로 갖게 하기
        // TODO 나중에 DB로 가져오기
        m_Stat = Instantiate(m_Stat);

        // 공격
        m_AttackPatterns = m_AttackPatterns
        .Select(pattern => {
                var instance = Instantiate(pattern);
                return instance; })
        .ToList();

        m_originalStat = m_Stat;

        // Stat을 Instantiate 한 후에 해야함.
        if (m_GameEntity is ControllableObject cobj)
        {
            cobj.OnChangeGrade += UpdateStatOfGrade;
        }

    }

    private void Start()
    {

        Init();

        UnitActionSystem.Instance.OnUpdateActionTick += (s, e) => UpdateTickStat();
    }

    protected virtual void OnEnable()
    {
        // 풀로 다시 소환할 때 체력 및 마나 리셋
        // TODO 공격 쿨타임 등도 다 리셋 예정
        Init();
    }

    public void Init()
    {
        // HP
        health = healthMax;

        // MP
        mp = mpMax;

        OnUpdateStat?.Invoke(this, EventArgs.Empty);
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
        ReduceHP(finalDamage);

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

    public void AddHP(int addHp)
    {
        health = Math.Clamp(health + addHp, 0, healthMax);

        OnUpdateStat?.Invoke(this, EventArgs.Empty);
    }

    public void ReduceHP(int addHp)
    {
        health = Math.Clamp(health - addHp, 0, healthMax);

        OnUpdateStat?.Invoke(this, EventArgs.Empty);
    }

    public void AddMP(int addMP)
    {
        mp = Math.Clamp(mp + addMP, 0, mpMax);

        OnUpdateStat?.Invoke(this, EventArgs.Empty);
    }

    public void ReduceMP(int addMP)
    {
        mp = Math.Clamp(mp - addMP, 0, mpMax);

        OnUpdateStat?.Invoke(this, EventArgs.Empty);
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

    public void ReStoreStat()
    {
        m_Stat = m_originalStat;

        Init();
    }

    // Tick 당 이뤄지는 함수
    private void UpdateTickStat()
    {
        if(m_IsDead) 
            return;

        if(m_Stat.m_fHPRegenrate > 0)
        {
            AddHP(Mathf.RoundToInt( m_Stat.m_fHPRegenrate));
        }

        if (m_Stat.m_fMPRegenrate > 0)
        {
            AddMP(Mathf.RoundToInt(m_Stat.m_fMPRegenrate));
        }
    }

    private void UpdateStatOfGrade(object sender, ControllableObject.OnChangeGradeEventArgs args)
    {
        // 값 원상복구
        if(args.isSuccessGrade == false)
        {
            m_Stat = m_originalStat;
            m_GameEntity.GetAnimationManager().AnimatonSpeedRestoreOriginalSpeed();
            return;
        }

        // 스텟 강화

        var enhanveValue = args.enhanceValue;

        switch (args.gradeEnhanceType)
        {
            case E_ObjectEnhanceType.Health:
                // 최대 HP 상승
                m_Stat.m_iMaxHP *= enhanveValue;

                // 체력 재생률 상승
                // TODO 개편 바람
                m_Stat.m_fHPRegenrate += enhanveValue;
                break;
            case E_ObjectEnhanceType.Magic:
                // - MP 상승
                m_Stat.m_iMaxMP *= enhanveValue;

                m_AttackPatterns.ForEach(attack =>
                {
                    if (attack.m_EAttackType == E_AttackType.Magic)
                    {
                        attack.m_iMagicAttackDamage *= enhanveValue;
                        attack.m_fMagicalArmorPenetraion *= enhanveValue;
                        attack.m_iMagicFixedDamage *= enhanveValue;
                        attack.m_iManaCost /= enhanveValue;
                        attack.m_iCoolTime /= enhanveValue;
                    }
                });
                break;
            case E_ObjectEnhanceType.Physical:
                m_AttackPatterns.ForEach(attack =>
                {
                    if (attack.m_EAttackType == E_AttackType.Physical)
                    {
                        // 물리 공격력 상승
                        attack.m_iPhysicalAttackDamage *= enhanveValue;
                        // 고정 물리 데미지 상승
                        attack.m_fPhysicalArmorPenetraion *= enhanveValue;
                        // 물리 방어구 관통력 상승
                        attack.m_iPhysicalFixedDamage *= enhanveValue;
                        // 공격 속도 증가
                        attack.m_fAttackSpeed *= enhanveValue;
                    }
                });
                break;

            // 방어 강화형
            case E_ObjectEnhanceType.Defense:
                m_Stat.m_iPhysicalDefence *= enhanveValue;       // 물리 방어력 상승
                m_Stat.m_iMagicalDefence *= enhanveValue;       // 마법 방어력 상승
                m_Stat.m_fKnockbackRegist *= enhanveValue;       // 넉백 저항률 상승
                m_Stat.m_fCounterAttackChance *= enhanveValue;   // 반격율 상승
                break;
            case E_ObjectEnhanceType.Speed:
                m_Stat.m_fWalkSpeed *= enhanveValue; // 이동 속도 대폭 증가
                m_Stat.m_fChaseSpeed *= enhanveValue; // 이동 속도 대폭 증가

                // 공격 속도 대폭 증가
                m_AttackPatterns.ForEach(attack => 
                {
                    attack.m_fAttackSpeed *= enhanveValue * 2;
                    attack.m_iCoolTime /= (enhanveValue * 2); // 쿨타임도 줄임
                });

                break;
            case E_ObjectEnhanceType.Critical:
                m_AttackPatterns = m_AttackPatterns
                    .Select(attack =>
                    {
                        attack.m_iCriticalChance *= enhanveValue;  // 치명타율 상승
                        attack.m_fCriticalDamageUp *= enhanveValue;  // 치명타 데미지 증가율 상승
                        attack.m_fAccuracy *= enhanveValue;  // 명중률 상승
                        return attack;
                    })
                    .ToList();
                break;
            case E_ObjectEnhanceType.Range:
                // 공격 사거리 대폭 증가 TODO
                break;
            case E_ObjectEnhanceType.Skill:
                // 스킬 추가 TODO
                break;
            default:
                break;
        }
        Init();
    }


    #region Reward

    // 보물 상자, 몬스터 처치 등으로 보상 수령 가능
    public void Reward()
    {
        if (m_RewardData == null)
            return;

        // --- 카드 보상 ---
        if (Random.value <= m_RewardData.CardProb) // 0~1 범위 확률 체크
        {
            if (m_RewardData.rewardCards.Count > 0)
            {
                RewardCard selected = WeightedRandomSelect(m_RewardData.rewardCards);
                //Debug.Log($"카드 획득: {selected.m_GameEntity.m_StatSystem.m_Stat.Name}");

                BuildingTypeSelectUI.Instance.AddCard(selected.m_GameEntity, transform.position);
                // TODO: 카드 인벤토리 추가 처리
            }
        }

        // --- 잼 보상 ---
        if (Random.value <= m_RewardData.downJamProb)
        {
            int jam = Random.Range(m_RewardData.downJamMin, m_RewardData.downJamMax + 1);
            Inventory.Instance.AddDownJam(jam);
            //Debug.Log($"다운잼 획득: {jam}");
        }

        // --- 버프 보상 ---
        if (Random.value <= m_RewardData.BuffProb)
        {
            if (m_RewardData.rewardBuffs.Count > 0)
            {
                RewardBuff selected = WeightedRandomSelect(m_RewardData.rewardBuffs);
                Debug.Log($"버프 획득: {selected.buffId}");
                // TODO: 대상 오브젝트에 버프 적용
            }
        }

        //// --- 이펙트 보상 ---
        if (Random.value <= m_RewardData.EffectProb)
        {
            if (m_RewardData.rewardEffects.Count > 0)
            {
                RewardEffect selected = WeightedRandomSelect(m_RewardData.rewardEffects);
                Debug.Log($"이펙트 발동: {selected.effectId}");
                // TODO: 이펙트 실행
            }
        }
    }



    /// <summary>
    /// 리스트 내부 확률이 합=1인 것을 전제하고 랜덤 선택
    /// </summary>
    private T WeightedRandomSelect<T>(List<T> list) where T : class
    {
        float roll = UnityEngine.Random.value;
        float cumulative = 0f;

        foreach (var item in list)
        {
            float prob = 0f;
            if (item is RewardCard rc) prob = rc.Probability;
            else if (item is RewardBuff rb) prob = rb.Probability;
            else if (item is RewardEffect re) prob = re.Probability;

            cumulative += prob;
            if (roll <= cumulative)
                return item;
        }

        return list[list.Count - 1]; // 안전장치
    }

    #endregion
}
