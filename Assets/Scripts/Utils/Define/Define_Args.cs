using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Define
{
    public class OnChangeFloorsStartedEventArgs : EventArgs
    {
        public GridPosition unitGridPosition;
        public GridPosition targetGridPosition;
    }


    public sealed class OnStatDeltaEventArgs : EventArgs
    {
        public E_StatDeltaKind Kind;
        public int Amount;               // "실제 적용된 값" (heal은 actualHeal, damage는 finalDamage)
        public E_StatDeltaSign Sign;     // +/-
        public E_StatDeltaCause Cause;   // Heal/LifeSteal/Damage 등

        public GameEntity Source;        // 공격자/힐러
        public GameEntity Target;        // 피해자/대상(= this GameEntity)

        public E_HitDecisionType HitDecision; // 필요할 때만(데미지)
        public E_HealType HealType;           // 필요할 때만(힐)
        public AttackData AttackPattern;      // 필요할 때만(데미지)
    }

    public class OnHealEventArgs : EventArgs
    {
        public int HealAmount;
        public E_HealType HealType;
        public GameEntity Healer; // 흡혈의 경우 자기 자신
    }

    public class OnAttackInfoEventArgs : EventArgs
    {
        public AttackData AttackPattern;
        public E_HitDecisionType EHitDeCisionType;
        public GameEntity Attacker;
        public GameEntity Victim;
        public int FinalDamage;
    }
}
