using System.Collections;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    private const string SPAWN_NOTIFICATION_SINGLE = "⚑ {0}이(가) 미궁 어딘가에서 나타났습니다.";
    private const string SPAWN_NOTIFICATION_MULTIPLE = "⚑ NPC {0}명이 미궁 어딘가에서 나타났습니다.";
    private const string DEATH_NOTIFICATION = "⚠ {0}이(가) 사망했습니다.";
    private const string RESPAWN_NOTIFICATION = "🔄 {0}이(가) 다시 출현했습니다.";
    private const string GOAL_REACHED_NOTIFICATION = "✅ {0}이(가) 던전 코어에 도달했습니다.";

    private Coroutine _spawnCoroutine;
    private GlobalSettings _settings;

    public void SetUp()
    {
        return;
        _settings = GlobalSettings.Instance;
        _spawnCoroutine = StartCoroutine(SpawnNPCPeriodically());
    }

    public void Clear()
    {
        StopAllCoroutines();
        _spawnCoroutine = null;
        _settings = null;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        _spawnCoroutine = null;
        _settings = null;
    }

    private IEnumerator SpawnNPCPeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(_settings.NPC.SpawnInterval);
            SpawnNPC();
        }
    }

    private void SpawnNPC()
    {
        int npcCount = _settings.NPC.NpcCountPerSpawn;

        for (int i = 0; i < npcCount; i++)
        {
        }

        string notification = npcCount == 1
            ? string.Format(SPAWN_NOTIFICATION_SINGLE, "NPC")
            : string.Format(SPAWN_NOTIFICATION_MULTIPLE, npcCount);

        Debug.Log(notification);
    }
}
