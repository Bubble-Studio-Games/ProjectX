using UnityEngine;

[CreateAssetMenu(fileName = "NPCSettings", menuName = "Settings/NPC Settings")]
public class NPCSettings : ScriptableObject
{
	[field: SerializeField] public bool IsNPCEnabled { get; private set; } = true;

	[field: SerializeField, Range(1f, 60f)] public float SpawnInterval { get; private set; } = 5f;

	[field: SerializeField, Range(1, 100)] public int NpcCountPerSpawn { get; private set; } = 1;
}
