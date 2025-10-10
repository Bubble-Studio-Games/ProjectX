using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using static Define;

[RequireComponent(typeof(ControllableObjectCombatManager))]
[RequireChildComponent(typeof(UnitAnimationManager), typeof(GameEntitySounder))]
public class Building : ControllableObject, IBuffReceiver, IBuffSender
{
    public E_BuildingType m_EBuildingType;

    #region Buff Fields
    [field: SerializeField] public int ObjectID { get; private set; }
    [field: SerializeField] public List<int> BuffID { get; private set; } = new();
    public List<IBuff> TakeBuffs { get; private set; }

    public E_GridShape Shape => throw new NotImplementedException();

    public int2 ShapeOffset => throw new NotImplementedException();
    #endregion

    protected override void Awake()
    {
        base.Awake();

        InitializedBuff();
    }

    protected override void Update()
    {
        base.Update();

        if (TakeBuffs.Count > 0)
        {
            foreach (var b in TakeBuffs)
            {
                b.Update(Time.deltaTime);
                if (b.IsExpired) RemoveBuff(b);
            }
        }
    }

    public Building()
    {
        m_ObjectType = E_ObjectType.Building;
    }

    #region Buff
    // ControllableObject 에 인터페이스 추가해도 될 것 같은데, 코드가 길어서 보기 힘듬다
    private void InitializedBuff()
    {
        TakeBuffs = new();

        // 버프 로드 요청
        if(BuffID.Count > 0)
        {
            foreach (var id in BuffID)
            {
                IssueRequest(RequestType.BuffLoad, default, id);
            }
        }
    }
    private void IssueRequest(RequestType requestType, in IBuffReceiver receiver, int id, in IBuff instance = null)
    {
        Request request = new Request(requestType, this, in receiver, id, null);
        Managers.Buff.SubmitRequest(in request);
    }
    private void RemoveBuff(IBuff buff)
    {
        if (buff == null) return;
        if (TakeBuffs?.Count == 0) return;
        
        TakeBuffs.Remove(buff);
        IssueRequest(RequestType.BuffRemove, null, -1, buff);
    }
    private IBuff SelectHigherStatValue(in IBuff a, in IBuff b, out IBuff lower)
    {
        lower = null;

        var dataA = a.Data;
        var dataB = b.Data;

        // 연산자 기준이 같다고 가정 (+,*,-,/)
        switch (dataA.Operator)
        {
            case E_BuffOperator.Add:
            case E_BuffOperator.Multiply:
                // 둘 다 클수록 강함
                if (dataA.Value >= dataB.Value)
                {
                    lower = b;
                    return a;
                }
                else
                {
                    lower = a;
                    return b;
                }

            case E_BuffOperator.Subtract:
            case E_BuffOperator.Divide:
                // 둘 다 작을수록 강함
                if (dataA.Value <= dataB.Value)
                {
                    lower = b;
                    return a;
                }
                else
                {
                    lower = a;
                    return b;
                }

            default:
                // 예외 처리
                UnityEngine.Debug.LogWarning($"예외: {dataA.Operator}");
                lower = b;
                return a;
        }
    }
    public void ApplyBuff(IBuff buff)
    {
        if (buff == null) return;
        IBuff buffToRemove = null;
        bool canAdd = true;

        foreach (var hasBuff in TakeBuffs)
        {
            // 동일한 건물/오브젝트인지 검사 
            bool isEqualSender = hasBuff.Sender.ObjectID == buff.Sender.ObjectID;
            // 같은 스텟인지 검사
            bool isEqualStat = hasBuff.Data.ControlableStat == buff.Data.ControlableStat;

            if (isEqualSender && isEqualStat)
            {
                var higher = SelectHigherStatValue(in hasBuff, in buff, out IBuff lower);
                buffToRemove = lower;
                if (higher == hasBuff)
                    canAdd = false; // 기존이 더 강하면 새 버프 무시
                else
                    canAdd = true;  // 새 버프가 더 강하면 교체
                break;
            }
        }

        if (buffToRemove != null)
            TakeBuffs?.Remove(buffToRemove);

        if (canAdd)
            TakeBuffs?.Add(buff);
    }
    #endregion
}

