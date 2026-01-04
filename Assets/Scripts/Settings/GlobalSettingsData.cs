using UnityEngine;

[CreateAssetMenu(fileName = "GlobalSettingsData", menuName = "Settings/Global Settings Data")]
public class GlobalSettingsData : ScriptableObject
{
	[Header("마우스 설정")]
	[SerializeField] private MouseSettings _mouseSettings;
	public MouseSettings MouseSettings => _mouseSettings;

	[Header("NPC 설정")]
	[SerializeField] private NPCSettings _npcSettings;
	public NPCSettings NPCSettings => _npcSettings;

	[Header("다이얼로그 설정")]
	[SerializeField] private DialogueSettings _dialogueSettings;
	public DialogueSettings DialogueSettings => _dialogueSettings;

	[Header("시작 설정")]
	[SerializeField] private StartSettings _startSettings;
	public StartSettings StartSettings => _startSettings;

	[Header("인벤토리 설정")]
	[SerializeField] private InventorySettings _inventorySettings;
	public InventorySettings InventorySettings => _inventorySettings;
}
