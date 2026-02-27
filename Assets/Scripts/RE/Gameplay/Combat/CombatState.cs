using UnityEngine;

/// <summary>
/// 전투에 관련된 통합 로직 
/// </summary>
public sealed class CombatState
{
    public Transform Target { get; set; } // 일단 Transform로(나중에 UnitContext로 교체 가능)
    public float NextAttackTime { get; private set; }

    public bool CanAttack(float nowTime) => nowTime >= NextAttackTime;

    public void ConsumeAttackCooldown(float nowTime, float cooldown)
    {
        NextAttackTime = nowTime + Mathf.Max(0f, cooldown);
    }

    public void ClearTarget()
    {
        Target = null;
    }
}
