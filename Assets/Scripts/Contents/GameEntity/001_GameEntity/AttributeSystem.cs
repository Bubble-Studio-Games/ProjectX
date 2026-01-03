using System;
using System.Linq;
using UnityEngine;
using static Define;
using Data;

public partial class AttributeSystem : MonoBehaviour
{
    private IUnitActionTickService _unitActionTickService;

    /*
     * 
    OnUpdateStat 하나로 HP바/패널 갱신
    OnStatDelta 하나로 팝업 표시 (현재까지는 Damage Display UI 용도로만 사용중임)
    OnDamaged/OnDead는 전투 로직용으로만 유지

     */
    public event Action OnRevived; // 	HP 회복 등으로 다시 살아날 때
    public event Action<OnAttackInfoEventArgs> OnDead; // HP 0일 때 죽는 순간
    public event Action<OnAttackInfoEventArgs> OnDamaged; // 데미지를 받았을 때
    public event Action OnUpdateStat;
    public event Action<OnStatDeltaEventArgs> OnStatDelta; 

    private GameEntity m_GameEntity;

    [Header("RewardTable")]
    public RewardTable m_RewardTable;

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

    protected void Awake()
    {
        m_GameEntity = GetComponent<GameEntity>();
        _unitActionTickService = Managers.SceneServices.UnitActionTick;

        StatInitInstantiate();
        AttackPatternInitInstantiate();
    }

    private void Start()
    {
        Validate();
    }

    protected void OnEnable()
    {
        OnDead += Reward;
        m_GameEntity.OnActionChanged += UpdateMoveState;

        if (m_GameEntity is IUpgradeble cobj)
            cobj.OnChangeGrade += UpdateStatOfGrade;

        _unitActionTickService.OnUpdateActionTick += UpdateTickStat;

        // TODO
        // 쿨타임 초기화
        // 오브젝트가 죽고 살아날때마다 하는 거.
        // attackPatterns.ForEach(a => a.Init());
    }

    protected void OnDisable()
    {
        OnDead -= Reward;
        m_GameEntity.OnActionChanged -= UpdateMoveState;

        if (m_GameEntity is IUpgradeble cobj)
            cobj.OnChangeGrade -= UpdateStatOfGrade;

        _unitActionTickService.OnUpdateActionTick -= UpdateTickStat;
    }

    public void Init()
    {
        // HP
        if(m_isInitWithFullHealth)
            health = healthMax;

        // MP
        if(m_isInitWithFullMana)
            mp = mpMax;

        OnUpdateStat?.Invoke();
    }

    // 되살아남
    public void Revive()
    {
        OnRevived?.Invoke();
    }

    private void UpdateStatOfGrade(Define.OnChangeGradeEventArgs args)
    {
        // 값 원상복구
        if(args.isSuccessGrade == false)
        {
            ReStoreStat();
            m_GameEntity.m_GameEntityAnimator.AnimatonSpeedRestoreOriginalSpeed();
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
                    if (attack.AttackType == E_AttackType.Magic)
                    {
                        attack.m_iMagicAttackDamage *= enhanveValue;
                        attack.m_fMagicalArmorPenetraion *= enhanveValue;
                        attack.m_iMagicFixedDamage *= enhanveValue;
                        attack.ManaCost /= enhanveValue;
                        attack.CoolTime /= enhanveValue;
                    }
                });
                break;
            case E_ObjectEnhanceType.Physical:
                attackPatterns.ForEach(attack =>
                {
                    if (attack.AttackType == E_AttackType.Physical)
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
                    attack.CoolTime /= (enhanveValue * 2); // 쿨타임도 줄임
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

    // 보물 상자, 몬스터 처치 등으로 보상 수령 가능
    public void Reward(OnAttackInfoEventArgs e)
    {
        if (m_RewardTable == null)
            return;

        m_RewardTable.Execute(m_GameEntity);
    }

    #region Data

    public AttributeSystemData CaptureSaveData()
    {
        return new AttributeSystemData
        {
            //stat = m_Stat,
            //attackPatterns = m_AttackPatterns.Select(attack => attack.CaptureSaveData()).ToList(),
            //rewardData = m_Reward?.CaptureSaveData(),
        };
    }

    public void RestoreSaveData(AttributeSystemData data)
    {
        StatInitInstantiate();

        //if (m_Stat != null && data.stat != null)
            //m_Stat = data.stat;

        AttackPatternInitInstantiate();

        foreach (var attackData in data.attackPatterns)
        {
            var attack = m_AttackPatterns.ToList()
                .Find(a => a.Id == attackData.id);

            // 이미 가지고 있는 스킬
            if (attack != null)
            {
                //attack.RestoreSaveData(attackData);
                continue;
            }
        }

        //m_Reward.RestoreSaveData(data.rewardData);

        // 3. 복원 후 이벤트 발생 (UI 갱신 등)
        OnUpdateStat?.Invoke();

        Debug.Log("스탯 복원");
    }
    #endregion


}