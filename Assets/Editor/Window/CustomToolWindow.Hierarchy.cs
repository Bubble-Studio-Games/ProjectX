using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro; // TextMeshPro
#endif

public partial class CustomToolWindow 
{
    private void Handle_FindAudioListeners()
    {
        if (GUILayout.Button("Find Audio Listeners"))
        {
            // 씬에 있는 모든 오디오 리스너를 찾습니다.
#if UNITY_2023_1_OR_NEWER
            AudioListener[] audioListeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
#else
        AudioListener[] audioListeners = Object.FindObjectsOfType<AudioListener>();
#endif

            Camera mainCamera = Camera.main;
            GameObject mainCamGO = mainCamera != null ? mainCamera.gameObject : null;

            int disabledCount = 0;

            foreach (var listener in audioListeners)
            {
                GameObject go = listener.gameObject;

                // 메인 카메라의 AudioListener는 제외
                if (mainCamGO != null && go == mainCamGO)
                {
                    Debug.Log($"✔ 유지: Main Camera의 AudioListener ({go.name})");
                    continue;
                }

                // 그 외 AudioListener는 비활성화
                if (listener.enabled)
                {
                    listener.enabled = false;
                    Debug.Log($"❌ Disabled AudioListener on GameObject: {go.name}");
                    disabledCount++;
                }
            }

            if (audioListeners.Length == 0)
            {
                Debug.LogWarning("No AudioListeners found in the scene.");
            }
            else if (disabledCount == 0)
            {
                Debug.Log("모든 AudioListener는 정상입니다. 중복된 항목이 없습니다.");
            }
            else
            {
                Debug.LogWarning($"총 {disabledCount}개의 AudioListener가 비활성화되었습니다. 메인 카메라만 유지됨.");
            }
        }
    }

    private Vector3 customCenter = Vector3.zero;
    private Vector3 customExtents = Vector3.one;

    private void Handle_AdjustSkinnedMeshBounds()
    {
        GUILayout.Label("Custom Bounds Settings", EditorStyles.boldLabel);

        customCenter = EditorGUILayout.Vector3Field("Center", customCenter);
        customExtents = EditorGUILayout.Vector3Field("Extents", customExtents);

        if (GUILayout.Button("Apply To Selected"))
        {
            ApplyBoundsToSelected();
        }
    }

    private void ApplyBoundsToSelected()
    {
        foreach (var obj in Selection.gameObjects)
        {
            var renderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in renderers)
            {
                Undo.RecordObject(smr, "Adjust SkinnedMesh Bounds");
                var bounds = smr.localBounds;
                bounds.center = customCenter;
                bounds.extents = customExtents;
                smr.localBounds = bounds;
                EditorUtility.SetDirty(smr);
            }
        }

        Debug.Log("Applied new bounds to selected objects.");
    }

#if TMP_PRESENT
    private TMP_FontAsset newTMPFont; // 바꿀 폰트 (TMP)
#else
    private Font newFont; // 바꿀 폰트 (UI Text)
#endif

    private void Handle_FontReplace()
    {
        GUILayout.Label("일괄 폰트 변경", EditorStyles.boldLabel);

#if TMP_PRESENT
        newTMPFont = (TMP_FontAsset)EditorGUILayout.ObjectField("TMP Font", newTMPFont, typeof(TMP_FontAsset), false);
#else
        newFont = (Font)EditorGUILayout.ObjectField("UI Font", newFont, typeof(Font), false);
#endif

        if (GUILayout.Button("Change All in Scene"))
        {
            ReplaceFontsInScene();
        }
    }

    private void ReplaceFontsInScene()
    {
        int count = 0;

#if TMP_PRESENT
        if (newTMPFont == null)
        {
            Debug.LogWarning("폰트를 먼저 지정하세요!");
            return;
        }

        // TMP Text
        foreach (var tmpText in FindObjectsOfType<TextMeshProUGUI>(true))
        {
            Undo.RecordObject(tmpText, "Replace TMP Font");
            if (newTMPFont != null)
            {
                tmpText.font = newTMPFont;
                EditorUtility.SetDirty(tmpText);
                count++;
            }
        }
#else
        if (newFont == null)
        
        {
            Debug.LogWarning("폰트를 먼저 지정하세요!");
            return;
        }


        // UI Text
        foreach (var text in FindObjectsOfType<Text>(true))
        {
            Undo.RecordObject(text, "Replace Font");
            if (newFont != null)
            {
                text.font = newFont;
                EditorUtility.SetDirty(text);
                count++;
            }
        }
#endif

        Debug.Log($"총 {count} 개의 Text 컴포넌트의 폰트가 변경되었습니다.");
    }
}
