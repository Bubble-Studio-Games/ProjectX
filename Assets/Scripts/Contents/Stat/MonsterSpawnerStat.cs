using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;



[CreateAssetMenu(menuName = "Stat/Monster Spawner Stat")]
public class MonsterSpawnerStat : BuildingStat
{
    // 소환 관련
    [Header("Spawn")]
    public int m_iSpawnPosX; // 중심 기준 x * z * floor 정사각형 범위로 소환.
    public int m_iSpawnPosZ;
    public int m_iSpawnPosFloor;
    public List<GridPosition> m_SpawnGridPos; // 추가적으로 소환하고 싶은 특정 그리드

    public int m_iMinSpawnCount; // 한 번에 소환 가능한 최소한의 숫자
    public int m_iMaxSpawnCount; // 한 번에 소환 가능한 최대한의 숫자

    public float m_fSpawnCoolTime;
    public List<ControllableObject> m_fSpawnObject; // 나중에 ID로 대체 

    // 체력이 깎일 때 단게별 스텟
    [Header("Enhance Monster Step Health")]
    public BaseStat[] m_AddStatToSpawnObjectStepHealth = new BaseStat[3]; // 체력이 75% 50% 25% 일때 단계별로 강화 +? %?
}
