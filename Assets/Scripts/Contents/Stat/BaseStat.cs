using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Stat/GameEntityStat")]
public class BaseStat : ScriptableObject
{
    [Header("Base")]
    public int ID;
    public string Name ;
    public Sprite sprite; // 카드에 넣을 대표 이미지
    public string m_sDescription; // 카드에 넣을 대표 이미지
    public int m_iMaxHP ;        // 최대 체력
    public int m_iCurrentHp ;    // 현재 체력
    public bool m_iIsStepReduceHP; // 체력이 단계적으로 깎이는가?

    public int m_iMaxMP ;     // 최대 마나
    public int m_iCurrentMP ; // 현재 마나

    //private float m_fMoveSpeed ; //  기본 걷기 이동 속도
    public float m_fChaseSpeed; //  추격 이동 속도
    public float m_fWalkSpeed; //   기본 걷기 속도 & 정찰 이동 속도

    public int m_iCommandMoveDistance; // 기본 이동 거리
    public int m_iDetectRange; // 감지 거리
    public int m_iChaseRange; // 추격 거리

    [Header("Battle")]
    public int m_iPhysicalDefence; // 물리 방어력
    public int m_iMagicalDefence; // 마법 방어력
    public int m_fCounterAttackChance; // 반격 확률
    public int m_fEvasionChance; //  회피 확률
    public float m_fKnockbackRegist;    // 넉백 저확률

    public List<AttackPattern> m_AttackPatterns = new List<AttackPattern>();
}




