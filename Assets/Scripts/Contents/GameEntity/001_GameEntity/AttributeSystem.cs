using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Define;
using static AttributeSystem;
using Random = UnityEngine.Random;
using Data;

public partial class AttributeSystem : MonoBehaviour
{
    public event EventHandler OnRevived; // 	HP 회복 등으로 다시 살아날 때
    public event EventHandler<OnAttackInfoEventArgs> OnDead; // HP 0일 때 죽는 순간
    public event EventHandler<OnAttackInfoEventArgs> OnDamaged; // 데미지를 받았을 때
    public event EventHandler<OnHealEventArgs> OnHealed; // 회복을 받았을 때 (흡혈, 스킬 등)
    public event EventHandler OnUpdateStat;

    public class OnHealEventArgs : EventArgs
    {
        public int HealAmount;
        public E_HealType HealType;
        public GameEntity Healer; // 흡혈의 경우 자기 자신
    }

    private GameEntity m_GameEntity;

    [Header("Reward")]
    public Reward m_Reward;

    public bool Validate()
    {
        if (m_originalStat == null)
        {
            Debug.LogWarning($"{this.gameObject.name}: 스텟이 존재하지 않습니다.- AttributeSystem - Stat");
            //return false;
        }

        if (m_originalAttackPatterns.Count == 0)
        {
            Debug.LogWarning($"{this.gameObject.name}: 공격 패턴이 존재하지 않습니다.- AttributeSystem - AttackPatterns");
            //return false;
        }

        return true;
    }

    private void Awake()
    {
        Validate();
        
        m_GameEntity = GetComponent<GameEntity>();

        // Event
        OnDead += (s, e) => Reward();
        m_GameEntity.OnChangeBaseActionEvent += (s, e) => UpdateMoveState();

        // Stat을 Instantiate 한 후에 해야함.
        if (m_GameEntity is ControllableObject cobj)
            cobj.OnChangeGrade += UpdateStatOfGrade;

        StatInitInstantiate();
        AttackPatternInitInstantiate();
    }

    private void Start()
    {

        UnitActionSystem.Instance.OnUpdateActionTick += UpdateTickStat;
    }

    private void OnDestroy()
    {
        if (UnitActionSystem.Instance != null)
            UnitActionSystem.Instance.OnUpdateActionTick -= UpdateTickStat;
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
        if(m_isInitWithFullHealth)
            health = healthMax;

        // MP
        if(m_isInitWithFullMana)
            mp = mpMax;

        OnUpdateStat?.Invoke(this, EventArgs.Empty);
    }

    // 되살아남
    public void Revive()
    {
        OnRevived?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateStatOfGrade(object sender, ControllableObject.OnChangeGradeEventArgs args)
    {
        // 값 원상복구
        if(args.isSuccessGrade == false)
        {
            ReStoreStat();
            m_GameEntity.GetAnimationsManager().ForEach(a => a.AnimatonSpeedRestoreOriginalSpeed());
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

                attackPatterns.ForEach(attack =>
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
                attackPatterns.ForEach(attack =>
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
                attackPatterns.ForEach(attack => 
                {
                    attack.m_fAttackSpeed *= enhanveValue * 2;
                    attack.m_iCoolTime /= (enhanveValue * 2); // 쿨타임도 줄임
                });

                break;
            case E_ObjectEnhanceType.Critical:
                attackPatterns = m_AttackPatterns
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
        if (m_Reward == null)
            return;

        // --- 카드 보상 ---
        if (Random.value <= m_Reward.CardProb) // 0~1 범위 확률 체크
        {
            if (m_Reward.rewardCards.Count > 0)
            {
                RewardCard selected = WeightedRandomSelect(m_Reward.rewardCards);
                //Debug.Log($"카드 획득: {selected.m_GameEntity.m_StatSystem.m_Stat.Name}");

                BuildingTypeSelectUI.Instance.AddCard(selected.m_GameEntity, transform.position);
                // TODO: 카드 인벤토리 추가 처리
            }
        }

        // --- 잼 보상 ---
        if (Random.value <= m_Reward.downJamProb)
        {
            int jam = Random.Range(m_Reward.downJamMin, m_Reward.downJamMax + 1);
            Inventory.Instance.AddDownJam(jam);
            //Debug.Log($"다운잼 획득: {jam}");
        }

        // --- 버프 보상 ---
        if (Random.value <= m_Reward.BuffProb)
        {
            if (m_Reward.rewardBuffs.Count > 0)
            {
                RewardBuff selected = WeightedRandomSelect(m_Reward.rewardBuffs);
                Debug.Log($"버프 획득: {selected.buffId}");
                // TODO: 대상 오브젝트에 버프 적용
            }
        }

        //// --- 이펙트 보상 ---
        if (Random.value <= m_Reward.EffectProb)
        {
            if (m_Reward.rewardEffects.Count > 0)
            {
                RewardEffect selected = WeightedRandomSelect(m_Reward.rewardEffects);
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

    #region Data

    public AttributeSystemData CaptureSaveData()
    {
        return new AttributeSystemData
        {
            stat = m_Stat,
            attackPatterns = m_AttackPatterns.Select(attack => attack.CaptureSaveData()).ToList(),
            rewardData = m_Reward?.CaptureSaveData(),
        };
    }

    public void RestoreSaveData(AttributeSystemData data)
    {
        StatInitInstantiate();

        if (m_Stat != null && data.stat != null)
            m_Stat = data.stat;

        AttackPatternInitInstantiate();

        foreach (var attackData in data.attackPatterns)
        {
            var attack = m_AttackPatterns.ToList()
                .Find(a => a.ID == attackData.id);

            // 이미 가지고 있는 스킬
            if (attack != null)
            {
                attack.RestoreSaveData(attackData);
                continue;
            }

            // TODO 새로 얻은 스킬 → 생성
            //var newAttack = CreateAttackPatternFromData(attackData);
            //if (newAttack != null)
            //{
            //    m_AttackPatterns.Add(newAttack);
            //}
        }

        m_Reward.RestoreSaveData(data.rewardData);

        // 3. 복원 후 이벤트 발생 (UI 갱신 등)
        OnUpdateStat?.Invoke(this, EventArgs.Empty);

        Debug.Log("스탯 복원");
    }
    #endregion


}