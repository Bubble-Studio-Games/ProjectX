using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Reflection;

/// <summary>
/// GlobalSettings Custom Inspector - 탭 기반 설정 편집
/// </summary>
[CustomEditor(typeof(GlobalSettings))]
public class GlobalSettingsEditor : Editor
{
	private const string TAB_INDEX_KEY = "GlobalSettingsEditor_SelectedTab";
	private const int MAX_TABS_PER_ROW = 4;

	private SerializedProperty _settingsDataProp;
	private SerializedObject _settingsDataObj;
	private int _selectedTab;
	private (string tabName, string propertyName)[] _tabs;
	private string[] _tabNames;

	private void OnEnable()
	{
		_settingsDataProp = serializedObject.FindProperty("_settingsData");
		_selectedTab = EditorPrefs.GetInt(TAB_INDEX_KEY, 0);

		InitTabsFromReflection();
		CacheSettingsDataObject();
		
		// 플레이 모드 중 인스펙터 자동 업데이트
		EditorApplication.update += OnRepaint;
	}
	
	private void OnDisable()
	{
		EditorApplication.update -= OnRepaint;
	}
	
	private void OnRepaint()
	{
		if (EditorApplication.isPlaying)
			Repaint();
	}

	private void InitTabsFromReflection()
	{
		var fields = typeof(GlobalSettingsData)
			.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
			.Where(f => f.FieldType.IsSubclassOf(typeof(ScriptableObject)))
			.OrderBy(f => f.MetadataToken)
			.ToArray();

		_tabs = fields.Select(f =>
		{
			var tabName = f.Name.Replace("_", "").Replace("Settings", "").Replace("settings", "");
			if (tabName == "mouse") tabName = "마우스";
			else if (tabName == "npc") tabName = "NPC";
			else if (tabName == "dialogue") tabName = "다이얼로그";
			else if (tabName == "scene") tabName = "씬";
			else if (tabName == "inventory") tabName = "인벤토리";

			var ret = (tabName, f.Name);
			return ret;
		}).ToArray();

		_tabNames = _tabs.Select(t => t.tabName).ToArray();
	}

	/// <summary>
	/// SettingsData SerializedObject 캐싱
	/// </summary>
	private void CacheSettingsDataObject()
	{
		if (_settingsDataProp.objectReferenceValue != null)
			_settingsDataObj = new SerializedObject(_settingsDataProp.objectReferenceValue);
		else
			_settingsDataObj = null;
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUI.BeginChangeCheck();
		DrawDefaultInspector();
		if (EditorGUI.EndChangeCheck())
			CacheSettingsDataObject();

		EditorGUILayout.Space();

		// 탭 UI
		if (_settingsDataObj != null)
		{
			_settingsDataObj.Update();

			EditorGUI.BeginChangeCheck();
			_selectedTab = GUILayout.SelectionGrid(_selectedTab, _tabNames, MAX_TABS_PER_ROW);
			if (EditorGUI.EndChangeCheck())
				EditorPrefs.SetInt(TAB_INDEX_KEY, _selectedTab);

			if (_selectedTab >= 0 && _selectedTab < _tabs.Length)
			{
				var (tabName, propertyName) = _tabs[_selectedTab];
				DrawCategorySettings(propertyName, tabName + " 설정");
			}

			_settingsDataObj.ApplyModifiedProperties();
		}
		else
		{
			EditorGUILayout.HelpBox("Settings Data를 인스펙터에 할당해주세요.", MessageType.Warning);
		}

		serializedObject.ApplyModifiedProperties();
	}

	/// <summary>
	/// 카테고리 설정 SO의 필드를 인라인으로 표시
	/// </summary>
	private void DrawCategorySettings(string propertyName, string label)
	{
		SerializedProperty categoryProp = _settingsDataObj.FindProperty(propertyName);

		if (categoryProp == null)
		{
			EditorGUILayout.HelpBox(
				$"[에러] '{propertyName}' 필드를 GlobalSettingsData에서 찾을 수 없습니다.\n" +
				$"GlobalSettingsData.cs의 필드명과 에디터 스크립트의 매핑을 확인하세요.",
				MessageType.Error
			);
			return;
		}

		EditorGUILayout.BeginVertical(EditorStyles.helpBox);

		EditorGUILayout.PropertyField(categoryProp, new GUIContent("에셋"));

		if (categoryProp.objectReferenceValue != null)
		{
			EditorGUILayout.Space(8);
			EditorGUI.indentLevel++;

			SerializedObject categoryObj = new SerializedObject(categoryProp.objectReferenceValue);
			SerializedProperty prop = categoryObj.GetIterator();

			// 첫 번째 프로퍼티는 스크립트 자체이므로 스킵
			prop.NextVisible(true);

			while (prop.NextVisible(false))
			{
				EditorGUILayout.PropertyField(prop, true);
			}

			categoryObj.ApplyModifiedProperties();
			EditorGUI.indentLevel--;
		}
		else
		{
			EditorGUILayout.HelpBox($"{label} 에셋을 할당해주세요.", MessageType.Info);
		}

		EditorGUILayout.EndVertical();
	}
}
