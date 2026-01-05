using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

[CreateAssetMenu(fileName = "SceneSettings", menuName = "Settings/Scenes Settings")]
public class SceneSettings : ScriptableObject
{
	[field: SerializeField] public Scene StartScene {get; private set;} = Scene.Start;
	[field: SerializeField] public Scene CampScene {get; private set;} = Scene.Camp;
	[field: SerializeField] public Scene DungeonScene {get; private set;} = Scene.Dungeon;
	[field: SerializeField] public Scene LoadingScene {get; private set;} = Scene.Loading;

#if UNITY_EDITOR
	[field: Header("테스트용 씬 설정")]
	[field: SerializeField] public Scene TestStartScene {get; private set;} = Scene.Start;
	[field: SerializeField] public Scene TestCampScene {get; private set;} = Scene.Camp;
	[field: SerializeField] public Scene TestDungeonScene {get; private set;} = Scene.Dungeon;
	[field: SerializeField] public Scene TestLoadingScene {get; private set;} = Scene.Loading;

	[Header("입력 스택 디버그")]
	[SerializeField] private List<string> _inputStackDebug = new();

    /// <summary>
    /// 입력 스택 동기화 - InputManager에서 호출
    /// </summary>
    public void SyncInputStack(Stack<E_InputActionMap> stack)
	{
		_inputStackDebug.Clear();
		_inputStackDebug.AddRange(stack.Reverse().Select(x => x.ToString()));
		string ret =  string.Join(" > ", _inputStackDebug);
		if (string.IsNullOrEmpty(ret))
			ret = "Empty";

		Debug.Log("[SceneSettings] 입력스택: " + ret);
		UnityEditor.EditorUtility.SetDirty(this);
	}
#endif

    public void Clear()
    {
#if UNITY_EDITOR
        _inputStackDebug.Clear();
		UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

	public Scene GetStartScene()
	{
#if UNITY_EDITOR
		return TestStartScene;
#else
		return StartScene;
#endif
	}

	public Scene GetCampScene()
	{
#if UNITY_EDITOR
		return TestCampScene;
#else
		return CampScene;
#endif
	}

	public Scene GetDungeonScene()
	{
#if UNITY_EDITOR
		return TestDungeonScene;
#else
		return DungeonScene;
#endif
	}

	public Scene GetLoadingScene()
	{
#if UNITY_EDITOR
		return TestLoadingScene;
#else
		return LoadingScene;
#endif
	}
}
