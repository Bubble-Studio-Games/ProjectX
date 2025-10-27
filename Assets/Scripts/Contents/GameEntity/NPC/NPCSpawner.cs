using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    private const string SPAWN_NOTIFICATION_SINGLE = "⚑ {0}이(가) 미궁 어딘가에서 나타났습니다.";
    private const string SPAWN_NOTIFICATION_MULTIPLE = "⚑ NPC {0}명이 미궁 어딘가에서 나타났습니다.";
    private const string DEATH_NOTIFICATION = "⚠ {0}이(가) 사망했습니다.";
    private const string RESPAWN_NOTIFICATION = "🔄 {0}이(가) 다시 출현했습니다.";
    private const string GOAL_REACHED_NOTIFICATION = "✅ {0}이(가) 던전 코어에 도달했습니다.";

    /// <summary>
    /// 재소환 대기 중인 NPC 정보
    /// </summary>
    [System.Serializable]
    private class RespawnInfo
    {
        public string NPCName;
        public float RespawnTime;
        [HideInInspector] public NPC OriginalPrefab;
        [HideInInspector] public Coroutine RespawnCoroutine;
    }

    [Header("Spawn Settings")]
    [SerializeField] private List<NPC> _npcPrefabs = new();
    [SerializeField] private int _minSpawnCount = 1;
    [SerializeField] private int _maxSpawnCount = 3;

    [Header("Spawn Area")]
    [SerializeField] private int _spawnRangeX = 15;
    [SerializeField] private int _spawnRangeZ = 15;
    [SerializeField] private int _spawnFloorRange = 0;

    [Header("Debug")]
    [SerializeField] private List<NPC> _npcInstances = new List<NPC>();
    [SerializeField] private List<RespawnInfo> _respawnNPCs = new();

    private Transform _dungeonCoreTarget => DungeonCore.instance.transform;
    private HashSet<GridPosition> _spawnGridPos = new HashSet<GridPosition>();
    private GridPosition _centerGridPos;

    private void Start()
    {
        InitSpawnArea();
        NPC.OnAnyNPCDeath += OnNPCDeath;
        SpawnNPCAtPosition();
    }

    private void InitSpawnArea()
    {
        var gridPos = LevelGrid.Instance.GetGridPosition(_dungeonCoreTarget.position);
        _centerGridPos = gridPos + GridPosition.To(_spawnRangeX, _spawnRangeZ, _spawnFloorRange);

        for (int x = -_spawnRangeX; x <= _spawnRangeX; x++)
        {
            for (int z = -_spawnRangeZ; z <= _spawnRangeZ; z++)
            {
                for (int floor = -_spawnFloorRange; floor <= _spawnFloorRange; floor++)
                {
                    var offsetPos = GridPosition.To(x, z, floor);
                    var testPos = _centerGridPos + offsetPos;

                    // 유효한 그리드이고, 이동 가능한 위치만 추가
                    if (LevelGrid.Instance.IsValidGridPosition(testPos) &&
                        Pathfinding.Instance.IsWalkableGridPosition(testPos))
                    {
                        _spawnGridPos.Add(testPos);
                    }
                }
            }
        }
    }

    [ContextMenu("NPC 랜덤 개수 스폰")]
    private void SpawnNPC()
    {
        SpawnNPCs(_minSpawnCount, _maxSpawnCount);
    }

    [ContextMenu("NPC 1명 스폰")]
    private void SpawnSingleNPC()
    {
        SpawnNPCs(1, 1);
    }

    [SerializeField] private Transform _testSpawnPos;

    [ContextMenu("NPC 지정된 위치에 스폰")]
    private void SpawnNPCAtPosition()
    {
        var gridPos = LevelGrid.Instance.GetGridPosition(_testSpawnPos.position);
        SpawnNPCs(1, 1, gridPos);
    }

    /// <summary>
    /// 지정된 개수만큼 NPC를 생성
    /// </summary>
    private void SpawnNPCs(int minCount, int maxCount, GridPosition? testPos = null)
    {
        List<GridPosition> enableSpawnPos;

        if (testPos.HasValue)
        {
            enableSpawnPos = new List<GridPosition> { testPos.Value };
        }
        else
        {
            if (TryGetSpawnAreaInfo(out enableSpawnPos) == false)
                return;
        }

        int rand = UnityEngine.Random.Range(minCount, maxCount + 1);
        int spawnCount = Mathf.Min(rand, enableSpawnPos.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            var spawnPos = enableSpawnPos.RandomPick();
            var npcPrefab = _npcPrefabs.RandomPick();

            var npc = Managers.Resource.Instantiate<NPC>(npcPrefab.gameObject);
            npc.transform.position = LevelGrid.Instance.GetWorldPosition(spawnPos);

            npc.SetTarget(_dungeonCoreTarget.position);
            npc.SpawnStart();
            _npcInstances.Add(npc);

            // 스폰 위치 제거 - 중복 스폰 방지
            enableSpawnPos.Remove(spawnPos);
        }

        ShowNPCSpawnNotification(spawnCount);
    }


    /// <summary>
    /// 스폰 가능한 위치 정보 조회
    /// </summary>
    private bool TryGetSpawnAreaInfo(out List<GridPosition> enableSpawnPos)
    {
        enableSpawnPos = default;
        if (_npcPrefabs == null || _npcPrefabs.Count <= 0)
        {
            Debug.LogWarning("스폰할 NPC 프리팹이 없습니다.");
            return false;
        }

        // 스폰 가능한 위치 필터링 (유닛이 없고, 예약되지 않은 위치)
        enableSpawnPos = _spawnGridPos
           .Where(pos => LevelGrid.Instance.HasAnyUnitOnGridPosition(pos) == false &&
                         LevelGrid.Instance.IsReservedGridPosition(pos) == false)
           .ToHashSet().ToList();

        if (enableSpawnPos == null || enableSpawnPos.Count <= 0)
        {
            Debug.LogWarning("NPC의 스폰 가능한 위치가 존재하지 않습니다.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// NPC 사망 이벤트 핸들러 - 즉시 파괴 + 재소환 타이머 시작
    /// </summary>
    private void OnNPCDeath(NPC owner)
    {
        _npcInstances.Remove(owner);

        // 재소환 정보 저장
        var respawnInfo = new RespawnInfo
        {
            OriginalPrefab = GetOriginalPrefab(owner),
            RespawnTime = owner.NPCStat.RespawnTime,
            NPCName = owner.name
        };

        // 재소환 타이머 시작
        if (respawnInfo.RespawnTime > 0)
        {
            respawnInfo.RespawnCoroutine = StartCoroutine(RespawnNPCAfterDelay(respawnInfo));
            _respawnNPCs.Add(respawnInfo);
        }

        // 즉시 파괴
        Managers.Resource.Destroy(owner.gameObject);

        ShowNPCDeathNotification(owner.name);
        Debug.Log($"NPCSpawner - {owner.name}이(가) 사망했습니다. {owner.NPCStat.RespawnTime}초 후 재소환 예정.");
    }

    /// <summary>
    /// 일정 시간 후 NPC 재소환 - 랜덤 위치에서
    /// </summary>
    private IEnumerator RespawnNPCAfterDelay(RespawnInfo info)
    {
        yield return new WaitForSeconds(info.RespawnTime);

        if (TryGetSpawnAreaInfo(out List<GridPosition> enableSpawnPos) == false)
        {
            Debug.LogWarning($"[NPCSpawner] {info.NPCName}의 재소환 위치를 찾을 수 없습니다.");
            _respawnNPCs.Remove(info);
            yield break;
        }

        // 랜덤 위치에서 재소환
        var spawnPos = enableSpawnPos.RandomPick();
        var npc = Managers.Resource.Instantiate<NPC>(info.OriginalPrefab.gameObject);
        npc.transform.position = LevelGrid.Instance.GetWorldPosition(spawnPos);
        npc.SetTarget(_dungeonCoreTarget.position);
        npc.SpawnStart();
        _npcInstances.Add(npc);

        ShowNPCRespawnNotification(npc.name);
        Debug.Log($"[NPCSpawner] {info.NPCName}이(가) 위치 {spawnPos}에서 재소환되었습니다.");

        _respawnNPCs.Remove(info);
    }

    /// <summary>
    /// NPC 인스턴스로부터 원본 프리팹 찾기
    /// </summary>
    private NPC GetOriginalPrefab(NPC instance)
    {
        if (instance.NPCStat != null)
        {
            var prefab = _npcPrefabs.Find(p => p.NPCStat.NPCType == instance.NPCStat.NPCType);
            if (prefab != null)
                return prefab;
        }

        Debug.LogWarning($"NPCSpawner - {instance.name}의 원본 프리팹을 찾을 수 없습니다. 첫 번째 프리팹을 사용합니다.");
        return _npcPrefabs.Count > 0 ? _npcPrefabs[0] : null;
    }

    private void ShowNPCSpawnNotification(int count, string npcName = "")
    {
        string message;

        if (count == 1)
        {
            message = string.IsNullOrEmpty(npcName)
                ? "⚑ NPC가 미궁 어딘가에서 나타났습니다."
                : string.Format(SPAWN_NOTIFICATION_SINGLE, npcName);
        }
        else
            message = string.Format(SPAWN_NOTIFICATION_MULTIPLE, count);

        ShowNotification(message);
    }

    public void ShowNPCDeathNotification(string npcName)
    {
        string message = string.Format(DEATH_NOTIFICATION, npcName);
        ShowNotification(message);
    }

    public void ShowNPCRespawnNotification(string npcName)
    {
        string message = string.Format(RESPAWN_NOTIFICATION, npcName);
        ShowNotification(message);
    }

    public void ShowNPCGoalReachedNotification(string npcName)
    {
        string message = string.Format(GOAL_REACHED_NOTIFICATION, npcName);
        ShowNotification(message);
    }

    private void ShowNotification(string message)
    {
        Managers.UI.ShowPopupUI<UI_NPCNotification>().SetMessage(message);
    }

    private void OnDestroy()
    {
        NPC.OnAnyNPCDeath -= OnNPCDeath; 

        _npcInstances.Clear();
        _spawnGridPos.Clear();

        foreach (var respawnInfo in _respawnNPCs)
        {
            if (respawnInfo.RespawnCoroutine != null)
                StopCoroutine(respawnInfo.RespawnCoroutine);
        }
        _respawnNPCs.Clear();
    }
}
