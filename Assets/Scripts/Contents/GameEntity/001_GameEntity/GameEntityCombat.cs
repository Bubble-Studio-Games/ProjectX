using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

[EditorShowInfo("전투 전용 스크립트 \n 무기 장착 프로젝타일 소환 위치 등을 관리")]
public class GameEntityCombat : MonoBehaviour
{
    #region Field
    public List<(Item obj, Transform spawnTransform)> m_AttackReadyItemObject = new();
    public HashSet<AttackData_Ready> m_ReadyAttackPattern = new HashSet<AttackData_Ready>();

    private GameEntity m_GameEntity;
    private AttributeSystem m_AttributeSystem;

    public event Action<AttackData_Ready> OnAttackReadyFailed; // "표현" 요청 신호
    private CombatAction CombatAction;

    #endregion

    #region 기본 함수

    protected virtual void Awake()
    {
        m_GameEntity = GetComponent<GameEntity>();
        m_AttributeSystem = GetComponent<AttributeSystem>();

        CombatAction = m_GameEntity.GetComponentInChildren<CombatAction>();
    }



    private void OnEnable()
    {
        m_AttributeSystem.OnDamaged += CheckDamagedAttackReadyFail;

        if(CombatAction != null)
            CombatAction.OnStartAttack += CheckAttackReady;

        m_GameEntity.OnAttackPoint += Attack;
    }

    private void OnDisable()
    {
        m_AttributeSystem.OnDamaged -= CheckDamagedAttackReadyFail;

        if (CombatAction != null)
            CombatAction.OnStartAttack -= CheckAttackReady;

        m_GameEntity.OnAttackPoint -= Attack;
    }

    #endregion

    private void CheckAttackReady(AttackData e)
    {
        if (e is not AttackData_Ready ready)
            return;

        StartCoroutine(ICheckAttackReady(ready));
    }

    // 공격 준비 실패에는 2가지가 있다.
    // 2. 공격 준비 후 다음 단계를 진행하지 못하고 일정 시간이 지났을 때
    IEnumerator ICheckAttackReady(AttackData_Ready readyAttack)
    {
        yield return new WaitForSeconds(readyAttack.m_AttackReadyTime);

        if (!m_ReadyAttackPattern.Contains(readyAttack))
            yield break;

        // 준비가 완료되었는지 체크
        if (readyAttack.m_ISAttackReadyFinished)
        {
            AttackReadyFailStart(readyAttack);   // 여기만 남기기
        }
    }

    // 공격 준비 실패에는 2가지가 있다.
    // 1. 공격 준비 중인데 공격을 받았을 때 
    private void CheckDamagedAttackReadyFail(OnAttackInfoEventArgs e)
    {
        var currentReady = CombatAction != null ? CombatAction.m_ThisTimeAttack as AttackData_Ready : null;
        if (currentReady == null)
            return;

        AttackReadyFailStart(currentReady);
    }

    public void AttackReadyFailStart(AttackData_Ready attackData)
    {
        if (m_ReadyAttackPattern.Contains(attackData))
            m_ReadyAttackPattern.Remove(attackData);

        Managers.Game.AttackPattern(attackData).StartAttackFail(m_GameEntity, m_GameEntity.m_Target, attackData);

        foreach (var (obj, _) in m_AttackReadyItemObject)
            obj.Destroy();

        // 리스트 초기화
        m_AttackReadyItemObject.Clear();

        // 이번 타임 공격 제거
        CombatAction.ChangeAttack(null);

        OnAttackReadyFailed?.Invoke(attackData);   // 여기만 남기기

        m_GameEntity.RaiseAttackReadyFailed(attackData);
    }

    public void AttackReadyFailEnd()
    {
        var data = CombatAction.m_ThisTimeAttack;
        Managers.Game.AttackPattern(data).EndAttackFail(m_GameEntity, data);
        CombatAction.m_ThisTimeAttack = null;
        CombatAction.ActiveSet(false);
    }

    public void Attack(AttackData data)
    {
        Managers.Game.AttackPattern(data).Attack(m_GameEntity, m_GameEntity.m_Target, data);
    }
}
