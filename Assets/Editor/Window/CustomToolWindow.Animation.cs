using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public partial class CustomToolWindow 
{
    #region 오브젝트 태그의 이름을 바꾸기

    private string newTag = "";
    private bool applyPrefabOverridesTag = false;

    public void Handle_SelectObjectChangeTag()
    {
        // 선택된 오브젝트를 가져옵니다.
        GameObject selectedObject = Selection.activeGameObject;

        // 선택된 오브젝트가 없으면 메시지를 출력합니다.
        if (selectedObject == null)
        {
            EditorGUILayout.HelpBox("No object selected. Please select an object in the Hierarchy.", MessageType.Warning);
            return;
        }

        // 현재 태그를 기본값으로 태그 선택 필드를 표시합니다.
        newTag = EditorGUILayout.TagField("New Tag", newTag);

        // 프리팹 오버라이드 적용 여부를 결정짓는 체크박스를 추가합니다.
        applyPrefabOverridesTag = EditorGUILayout.Toggle("Apply Prefab Overrides", applyPrefabOverridesTag);

        // Apply 버튼을 표시합니다.
        if (GUILayout.Button("Apply"))
        {
            // 태그가 비어 있지 않은지 확인합니다.
            if (!string.IsNullOrEmpty(newTag))
            {
                ChangeTagRecursively(selectedObject, newTag);
                Debug.Log("Changed tag for " + selectedObject.name + " and its children to " + newTag);

                if (applyPrefabOverridesTag)
                {
                    ApplyPrefabOverridesTag(selectedObject);
                    Debug.Log("Applied prefab overrides for " + selectedObject.name + " and its children.");
                }
            }
            else
            {
                Debug.LogWarning("Tag cannot be empty.");
            }
        }
    }

    private  void ChangeTagRecursively(GameObject obj, string newTag)
    {
        // 현재 오브젝트의 태그를 변경합니다.
        obj.tag = newTag;

        // 모든 자식 오브젝트의 태그를 재귀적으로 변경합니다.
        foreach (Transform child in obj.transform)
        {
            ChangeTagRecursively(child.gameObject, newTag);
        }
    }

    private  void ApplyPrefabOverridesTag(GameObject obj)
    {
        // 현재 오브젝트가 프리팹의 일부인 경우 변경사항을 적용합니다.
        if (PrefabUtility.IsPartOfPrefabInstance(obj))
        {
            PrefabUtility.ApplyPrefabInstance(obj, InteractionMode.UserAction);
        }

        // 모든 자식 오브젝트에 대해서도 동일하게 적용합니다.
        foreach (Transform child in obj.transform)
        {
            ApplyPrefabOverridesTag(child.gameObject);
        }
    }

    #endregion


}
