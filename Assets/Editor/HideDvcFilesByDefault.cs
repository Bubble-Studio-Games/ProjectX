using UnityEditor;
using System;
using System.Reflection;

[InitializeOnLoad]
public static class HideDvcFilesByDefault
{
    static HideDvcFilesByDefault()
    {
        try
        {
            // UnityEditor.ProjectBrowser 타입 가져오기
            var type = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
            if (type == null) return;

            // 숨김 확장자 배열 필드 가져오기
            var field = type.GetField("s_HiddenExtensions", BindingFlags.Static | BindingFlags.NonPublic);
            if (field == null) return;

            string[] hidden = (string[])field.GetValue(null);

            // 이미 등록돼있지 않으면 추가
            if (Array.IndexOf(hidden, ".dvc") < 0)
            {
                Array.Resize(ref hidden, hidden.Length + 1);
                hidden[hidden.Length - 1] = ".dvc";
                field.SetValue(null, hidden);

                UnityEngine.Debug.Log("✅ .dvc 파일이 Project 창에서 숨김 처리됨");
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("HideDvcFilesByDefault error: " + e);
        }
    }
}
