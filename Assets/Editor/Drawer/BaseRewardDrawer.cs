#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using static Define;

// 희귀도(enum) → 색상 매핑 전용 클래스
// Editor 전용 유틸리티
public static class RewardRarityColor
{
    public static Color Get(RewardRarity rarity)
    {
        // rarity 값에 따라 색상 반환
        return rarity switch
        {
            RewardRarity.Common => new Color(0.75f, 0.75f, 0.75f),
            RewardRarity.Rare => new Color(0.3f, 0.6f, 1f),
            RewardRarity.Epic => new Color(0.7f, 0.3f, 0.9f),
            RewardRarity.Legendary => new Color(1f, 0.6f, 0.1f),
            _ => Color.white
        };
    }
}


// BaseReward를 상속한 모든 "구체 클래스"를 자동 수집
public static class RewardTypeCache
{
    public static readonly Type[] Types;
    public static readonly string[] Names;

    static RewardTypeCache()
    {
        // 현재 AppDomain에 로드된 모든 어셈블리 탐색
        Types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                t.IsSubclassOf(typeof(BaseReward)) &&
                !t.IsAbstract)
            .ToArray();

        Names = Types.Select(t =>
        {
            var attr = t.GetCustomAttribute<RewardDisplayNameAttribute>();
            return attr != null ? attr.Name : t.Name;
        }).ToArray();
    }
}

// BaseReward 타입이면 무조건 이 Drawer 사용
// true → 상속 클래스도 포함
[CustomPropertyDrawer(typeof(BaseReward), true)]
public class BaseRewardDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
         // ✔ Undo / Prefab Override / Multi - edit 지원
        // ✔ 안 쓰면 버그 생김(무조건 써라)
        EditorGUI.BeginProperty(position, label, property);


        // line: 한 줄 높이

        // y : 지금 그릴 세로 위치

        // 레이아웃 시스템 없음 → 직접 계산
        var current = property.managedReferenceValue;
        Type currentType = current?.GetType();

        float line = EditorGUIUtility.singleLineHeight;
        float y = position.y;


        // ---------- 희귀도 컬러 바 ----------
        SerializedProperty rarityProp =
            property.FindPropertyRelative("Rarity");

        Color rarityColor = Color.white;

        if (rarityProp != null)
        {
            rarityColor = RewardRarityColor.Get(
                (RewardRarity)rarityProp.enumValueIndex
            );
        }

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = rarityColor * 1.2f;

        // =========================
        // 1️ 타입 드롭다운 (항상 활성)
        // =========================
        int index = Array.FindIndex(
            RewardTypeCache.Types,
            t => t == currentType
        );

        // 현재 Reward의 타입이 드롭다운에서 몇 번째인지 계산
        int nextIndex = EditorGUI.Popup(
            new Rect(position.x, y, position.width, line),
            index < 0 ? 0 : index,
            RewardTypeCache.Names
        );

        // ✔ 타입 바뀌면:

        // 기존 데이터 완전 삭제
           
        // 새 클래스 인스턴스 생성
           
        // 👉 네가 원하던 “카드 → 잼 바꾸면 데이터 날아감” 이 부분

        if (currentType != RewardTypeCache.Types[nextIndex])
        {
            property.managedReferenceValue =
                Activator.CreateInstance(RewardTypeCache.Types[nextIndex]);
            EditorGUI.EndProperty();
            return;
        }

        if (property.managedReferenceValue == null)
        {
            EditorGUI.EndProperty();
            return;
        }

        y += line + 2;

        // =========================
        // 2 Weight (항상 활성)
        // =========================
        SerializedProperty probProp =
            property.FindPropertyRelative("Weight");

        EditorGUI.PropertyField(
            new Rect(position.x, y, position.width, line),
            probProp
        );

        bool isDisabled = probProp.floatValue <= 0f;
        y += line + 4;

        // =========================
        // 3️ 나머지 필드 (조건부 비활성)
        // =========================
        EditorGUI.BeginDisabledGroup(isDisabled);
        EditorGUI.indentLevel++;

        var iterator = property.Copy();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (iterator.name == "Weight")
                continue;

            if (iterator.depth != property.depth + 1)
                continue;

            float h = EditorGUI.GetPropertyHeight(iterator, true);
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, h),
                iterator,
                true
            );
            y += h + 2;
        }

        // 컬러 바
        Rect barRect = new Rect(position.x, position.y, position.width, 4);
        EditorGUI.DrawRect(barRect, rarityColor);

        GUI.backgroundColor = prevBg;

        // y 오프셋
        position.y += 6;


        EditorGUI.indentLevel--;
        EditorGUI.EndDisabledGroup();

        // =========================
        // 4️ 안내 메시지
        // =========================
        if (isDisabled)
        {
            EditorGUI.HelpBox(
                new Rect(position.x, y, position.width, line),
                "Weight가 0이므로 이 보상은 드롭되지 않습니다.",
                MessageType.Info
            );
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.managedReferenceValue == null)
            return EditorGUIUtility.singleLineHeight;

        float height = 0f;
        float line = EditorGUIUtility.singleLineHeight;

        height += line * 2 + 6; // 타입 + Weight

        var iterator = property.Copy();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (iterator.name == "Weight")
                continue;

            if (iterator.depth != property.depth + 1)
                continue;

            height += EditorGUI.GetPropertyHeight(iterator, true) + 2;
        }

        var probProp = property.FindPropertyRelative("Weight");
        if (probProp.floatValue <= 0f)
        {
            height += line + 2;
        }

        return height;
    }
}
#endif
