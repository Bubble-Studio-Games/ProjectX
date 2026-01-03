using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 글로벌 설정 관리 싱글톤
/// </summary>
[DefaultExecutionOrder(-100000)]
public class GlobalSettings : MonoBehaviour
{
	public static GlobalSettings Instance { get; private set; }
	
	[SerializeField] private GlobalSettingsData _settingsData;
	[SerializeField] private EventSystem _eventSystem;
	public EventSystem EventSystem => _eventSystem;

	public GlobalSettingsData SettingsData 
	{
		get 
		{
			if (Validate())
				return _settingsData;
			else
				return null;
		}
	}
	public MouseSettings Mouse => SettingsData?.MouseSettings;
	public NPCSettings NPC => SettingsData?.NPCSettings;
	public DialogueSettings Dialogue => SettingsData?.DialogueSettings;
	public StartSettings Start => SettingsData?.StartSettings;
	public InventorySettings Inventory => SettingsData?.InventorySettings;

	private void Awake()
	{
		Init();
	}	
	
	private void Init()
	{
		Instance = this;
		DontDestroyOnLoad(this.gameObject);
		_eventSystem = this.GetComponentInChildren<EventSystem>();
	}

	

	private bool Validate()
	{
		if (_settingsData == null)
		{
			Debug.LogWarning("[GlobalSettings] GlobalSettingsData이 연동되지 않았습니다. 인스펙터에서 설정해주세요.");
			return false;
		}
		return true;
	}
}
