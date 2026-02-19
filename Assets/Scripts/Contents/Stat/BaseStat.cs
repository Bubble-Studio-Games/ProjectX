using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Define;

[System.Serializable]
public struct StatValue
{
    [SerializeField] private float _value;
    [HideInInspector] private readonly int _decimalPlaces;
    [HideInInspector] private readonly bool _autoRound;

    public float Value
    {
        get => _autoRound ? (float)System.Math.Round(_value, _decimalPlaces) : _value;
        set => _value = _autoRound ? (float)System.Math.Round(value, _decimalPlaces) : value;
    }

    public StatValue(int decimalPlaces = 1, bool autoRound = true)
    {
        _value = 0f; // 인스펙터에서 입력됨
        _decimalPlaces = decimalPlaces;
        _autoRound = autoRound;

        /*
        정수형(큰 수치) → 반올림 해도 무방, 오히려 깔끔

        세밀형(속도·재생률·확률) → 반올림 금지, float 그대로

        즉, “눈에 보이는 수치 = 반올림”, “체감형 수치 = 유지”
         */
    }

    // ✅ 인스펙터에서 값이 변경될 때 자동 반올림
    public void OnValidate()
    {
        if (_autoRound)
            _value = (float)System.Math.Round(_value, _decimalPlaces);
    }

    // 🔹 float → StatValue (기존 설정 유지)
    public static implicit operator StatValue(float value)
    {
        StatValue stat = new StatValue(1, true); // 기본 세팅
        stat.Value = value;
        return stat;
    }

    // 🔹 StatValue → float
    public static implicit operator float(StatValue stat) => stat.Value;


    // 🔹 StatValue + float
    public static StatValue operator +(StatValue stat, float delta)
    {
        stat.Value += delta;
        return stat;
    }

    // 🔹 StatValue - float
    public static StatValue operator -(StatValue stat, float delta)
    {
        stat.Value -= delta;
        return stat;
    }

    // 🔹 StatValue * float
    public static StatValue operator *(StatValue stat, float multiplier)
    {
        stat.Value *= multiplier;
        return stat;
    }

    // 🔹 StatValue / float
    public static StatValue operator /(StatValue stat, float divisor)
    {
        stat.Value /= divisor;
        return stat;
    }

}


[CreateAssetMenu(menuName = "Stat/GameEntityStat")]
[Serializable]
public class BaseStat : ScriptableObject
{
    [Header("Base")]
    public int ID;
    public string Name ;

    [JsonIgnore]
    public Sprite sprite; // 카드에 넣을 대표 이미지
    public string m_sDescription; // 설명


    public StatValue m_iMaxHP = new StatValue(0, true);        // 최대 체력
    public StatValue m_iCurrentHp = new StatValue(0, true);    // 현재 체력
    public bool m_iIsStepReduceHP; // 체력이 단계적으로 깎이는가?

    public StatValue m_fHPRegenrate = new StatValue(1, false); // 체력 재생률, Tick당 차오르는 수치
    public StatValue m_fMPRegenrate = new StatValue(1, false); // 마나 재생률, Tick당 차오르는 수치

    public StatValue m_iMaxMP = new StatValue(0, false);     // 최대 마나
    public StatValue m_iCurrentMP = new StatValue(0, false); // 현재 마나

    //private float m_fMoveSpeed ; //  기본 걷기 이동 속도
    public StatValue m_fChaseSpeed = new StatValue(1, false); //  추격 이동 속도
    public StatValue m_fWalkSpeed = new StatValue(1, false); //   기본 걷기 속도 & 정찰 이동 속도

    public int m_iCommandMoveRange; // 커맨드 이동 거리
    public int m_iDetectRange; // 감지 거리
    public int m_iChaseRange; // 추격 거리

    [Header("Battle")]
    public StatValue m_iPhysicalBaseDamamge = new StatValue(0, true); // 기본 물리 공격력
    public StatValue m_iMagicalBaseDamamge = new StatValue(0, true); // 기본 마법 공격력
    public StatValue m_iPhysicalDefence = new StatValue(0, true); // 물리 방어력
    public StatValue m_iMagicalDefence = new StatValue(0, true); // 마법 방어력
    public StatValue m_fCounterAttackChance = new StatValue(0, true); // 반격 확률
    public StatValue m_fEvasionChance = new StatValue(0, true); //  회피 확률
    public StatValue m_fKnockbackRegist = new StatValue(0, true);    // 넉백 저확률

    [Header("Spawn")]
    public int m_iSpawnCost; // 소환 비용 (아군 유닛일 때만)
}




