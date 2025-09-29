#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TVender.ArrayPlace
{
    [CustomEditor(typeof(ArrayPlaceHelper)), CanEditMultipleObjects]
    public class Editor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var script = (ArrayPlaceHelper)target;

            if (GUILayout.Button("Place"))
            {
                script.Place();
            }

            if (GUILayout.Button("Clear"))
            {
                script.Clear();
            }
        }
    }
}

#endif