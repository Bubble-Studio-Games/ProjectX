using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 데이터 정의
/// </summary>
namespace Data
{
    public partial class Boss
    {
        [System.Serializable]
        public class Data
        {
            public string Id;
            public string Name;
            public int MaxHealth;
            public int Attack;
            public int Defense;
            public string PrefabPath; // Addressable/Resource 경로
        }

        public static Data Stage1Boss => new()
        {
            Id = "stage_1_boss",
            Name = "Stage 1 Boss",
            MaxHealth = 100,
            Attack = 10,
            Defense = 5,
            PrefabPath = "Prefabs/Boss/Ork",
        };

        public static Data TestBoss => Stage1Boss;

        public static Data GetBossData(Define.E_BossType bossType)
        {
            var ret = bossType switch
            {
                Define.E_BossType.Ork => Stage1Boss,
                _ => TestBoss
            };

            return ret;
        }
    }
}
