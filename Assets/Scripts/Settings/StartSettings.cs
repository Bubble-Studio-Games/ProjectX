using UnityEngine;

[CreateAssetMenu(fileName = "StartSettings", menuName = "Settings/Start Settings")]
public class StartSettings : ScriptableObject
{
	[field: SerializeField] public Define.Scene CampSceneType {get; private set;} = Define.Scene.Camp;
	[field: SerializeField] public Define.Scene GameSceneType {get; private set;} = Define.Scene.Dungeon;

#if UNITY_EDITOR
	[field: SerializeField] public Define.Scene TestGameSceneType {get; private set;} = Define.Scene.LMGameScene;
	[field: SerializeField] public Define.Scene TestCampSceneType {get; private set;} = Define.Scene.LMCampScene;
#endif

	public static Define.Scene GetGameSceneType()
	{
#if UNITY_EDITOR
		return GlobalSettings.Instance.Start.TestGameSceneType;
#else
		return GlobalSettings.Instance.Start.GameSceneType;
#endif
	}

	public static Define.Scene GetCampSceneType()
	{
#if UNITY_EDITOR
		return GlobalSettings.Instance.Start.TestCampSceneType;
#else
		return GlobalSettings.Instance.Start.CampSceneType;
#endif
	}
}
