using UnityEditor;
using UnityEngine;

/// <summary>
/// InventorySettings Custom Inspector - 아이템 ID 일괄 파싱 기능
/// </summary>
[CustomEditor(typeof(InventorySettings))]
public class InventorySettingsEditor : Editor
{
	private string _pasteInput = "";
	private bool _showParseSection = false;
	private Vector2 _scrollPosition;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		EditorGUILayout.Space(10);

		// 파싱 섹션 토글
		_showParseSection = EditorGUILayout.Foldout(_showParseSection, "아이템 ID 일괄 추가", true);

		if (_showParseSection)
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);

			EditorGUILayout.LabelField("아이템 ID 입력 (줄바꿈으로 구분)", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("시트에서 복사한 Item_ID를 붙여넣기 하세요.\n각 줄이 하나의 아이템으로 추가됩니다.", MessageType.Info);

			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(120));
			_pasteInput = EditorGUILayout.TextArea(_pasteInput, GUILayout.ExpandHeight(true));
			EditorGUILayout.EndScrollView();

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("파싱하기", GUILayout.Height(30)))
				ParseAndAddItems();

			if (GUILayout.Button("초기화", GUILayout.Height(30), GUILayout.Width(60)))
				_pasteInput = "";

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();
		}
	}

	/// <summary>
	/// 입력된 텍스트를 파싱하여 아이템 추가
	/// </summary>
	private void ParseAndAddItems()
	{
		if (string.IsNullOrWhiteSpace(_pasteInput))
		{
			EditorUtility.DisplayDialog("알림", "입력된 내용이 없습니다.", "확인");
			return;
		}

		var settings = (InventorySettings)target;
		var lines = _pasteInput.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

		int addedCount = 0;
		int skippedCount = 0;

		Undo.RecordObject(settings, "Add Items from Paste");

		foreach (var line in lines)
		{
			string itemId = line.Trim();

			if (string.IsNullOrEmpty(itemId))
				continue;

			// 중복 체크
			bool isDuplicate = settings.Items.Exists(e => e.ItemId == itemId);
			if (isDuplicate)
			{
				skippedCount++;
				continue;
			}

			var newEntry = new InventorySettings.ItemEntry
			{
				ItemId = itemId,
				Count = 1
			};

			settings.Items.Add(newEntry);
			addedCount++;
		}

		EditorUtility.SetDirty(settings);

		string message = $"추가됨: {addedCount}개";
		if (skippedCount > 0)
			message += $"\n중복 스킵: {skippedCount}개";

		EditorUtility.DisplayDialog("파싱 완료", message, "확인");

		_pasteInput = "";
	}
}
