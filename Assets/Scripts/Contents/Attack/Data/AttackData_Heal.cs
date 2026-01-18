using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Attack Pattern/Heal")]
public class AttackData_Heal : AttackData
{

    public StatValue HealAmount = new StatValue(0, false);  // 힐 총량
    public Effect HealEffectPrefab;
}
