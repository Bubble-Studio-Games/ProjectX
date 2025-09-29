using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using static Define;

public class MonsterSpawner : Building
{
    [Header("Core")]
    MonsterSpawnerCore Core;
    HashSet<GridPosition> m_SpawnGridPosition = new HashSet<GridPosition>();

    float m_currentSpawnTimer;

    MonsterSpawnerStat m_MSS;

    // 오디오랑 애니메이터를 따로 파기 뭐해서 그냥 한 곳에 몰아 넣음
    [Header("Animation")]
    PassiveObjectAnimator m_CoreAnimatorManager;
    [SerializeField] PassiveObjectAnimator m_BackgroundAnimatorManager;

    public MonsterSpawner()
    {
        m_EBuildingType = E_BuildingType.Spawner;
    }

    protected override void Start()
    {
        base.Start();

        Core = GetComponentInChildren<MonsterSpawnerCore>();

        // 애니메이션 교체
        m_CoreAnimatorManager = Core.GetComponentInChildren<PassiveObjectAnimator>();
        m_CoreAnimatorManager.ChangeAnimationAtStart
            ("Spawn Object", m_CoreAnimatorManager.m_OrderAnimationClip.Where(ani => ani.name.Contains("Core")).FirstOrDefault());

        SetSpawnGridPosition();

    }

    protected override void Update()
    {
        base.Update();

        CheckCoolTimeSpawnObject();
    }

    private void SetSpawnGridPosition()
    {
        // 스탯 긁어오기 소환 범위
        m_MSS = m_StatSystem.m_Stat as MonsterSpawnerStat;

        GridPosition coreGridPosition = LevelGrid.Instance.GetGridPosition(Core.transform.position);

        // 내위치 + x*z*floor - 코어 위치 - 갈 수 없거나 막힌 곳 제외.
        for (int x = -m_MSS.m_iSpawnPosX; x <= m_MSS.m_iSpawnPosX; x++)
        {
            for (int z = -m_MSS.m_iSpawnPosZ; z <= m_MSS.m_iSpawnPosZ; z++)
            {
                for (int floor = -m_MSS.m_iSpawnPosFloor; floor <= m_MSS.m_iSpawnPosFloor; floor++)
                {
                    GridPosition offsetGridPosition = new GridPosition(x, z, floor);
                    GridPosition testGridPosition = m_GridPosition + offsetGridPosition;

                    m_SpawnGridPosition.Add(testGridPosition);
                }
            }
        }

        // 추가적인 특정 그리드
        m_SpawnGridPosition.AddRange(m_MSS.m_SpawnGridPos.Select(pos => pos + m_GridPosition));

        m_SpawnGridPosition = Enumerable.ToHashSet( m_SpawnGridPosition
            .Where(grid =>
            LevelGrid.Instance.IsValidGridPosition(grid) &&
            coreGridPosition != grid &&
            Pathfinding.Instance.IsWalkableGridPosition(grid)
            ));
    }

    private void CheckCoolTimeSpawnObject()
    {
        if(m_StatSystem.m_IsDead) 
            return;

        m_currentSpawnTimer += Time.deltaTime;
        if (m_currentSpawnTimer >= Mathf.Max(m_MSS.m_fSpawnCoolTime, 4))
        {
            m_currentSpawnTimer = 0;
            SpawnObject();
        }
    }

    private void SpawnObject()
    {
        // 소환 가능한 포지션을 랜덤으로 뽑아서 검사
        var validSpawnPosList = Enumerable.ToHashSet
            (m_SpawnGridPosition.Where(pos => !LevelGrid.Instance.HasAnyUnitOnGridPosition(pos) && !LevelGrid.Instance.IsReservedGridPosition(pos)));

        // 소환 가능한 수 계산
        int spawnCount = Mathf.Min(UnityEngine.Random.Range(m_MSS.m_iMinSpawnCount, m_MSS.m_iMaxSpawnCount), validSpawnPosList.Count);

        if (spawnCount == 0)
            return;

        // Animation
        m_CoreAnimatorManager.PlayTargetAnimation("Spawn Object", false);

        // 소환 가능한 위치이면 랜덤 소환
        for (int i = 0; i < spawnCount; i++)
        {
            var spawnPos =  validSpawnPosList.RandomPick();

            // 해당 위치로 소환
            var pickObj = m_MSS.m_fSpawnObject.RandomPick();
            var spawnObj = Managers.Resource.Instantiate<GameEntity>(pickObj.gameObject);
            spawnObj.transform.position = LevelGrid.Instance.GetWorldPosition(spawnPos);
            spawnObj.SpawnStart();

            // valid에서 제외
            validSpawnPosList.Remove(spawnPos);
        }
    }
}
