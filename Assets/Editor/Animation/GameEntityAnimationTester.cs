using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using static Define;
using static UnityEngine.Rendering.DebugUI;

/// <summary>
/// 🎬 GameEntity 애니메이션 및 사운드 테스트 전용 에디터 윈도우
/// - Sound_Test에서 활성화된 오브젝트를 자동 인식
/// - Animator와 AttackPattern 애니메이션/사운드를 테스트 가능
/// - AttackPattern 클릭 시 CombatAction.m_ThisTimeAttack 자동 설정
/// </summary>
public partial class CustomToolWindow : EditorWindow
{
    private Vector2 GameEntityAnimationTester_scrollPos;
    private GameEntity activeEntity;
    private List<GameEntityAnimator> animators = new();
    private List<AttackPattern> attackPatterns = new();
    private Dictionary<string, bool> listFoldout = new();

    private bool isEventRegistered = false;

    private void OnEnable_GameEntityAnimationTester()
    {
        // 최초 또는 다시 열릴 때 한 번만 이벤트 등록
        if (!isEventRegistered)
        {
            EditorApplication.update += EditorAutoRefresh;
            Sound_Test.OnActiveEntityChanged += RefreshActiveEntity;
            isEventRegistered = true;

            RefreshActiveEntity();
            //Debug.Log("[AnimationTester] 이벤트 등록 완료");
        }
    }

    private void OnDisable()
    {
        // 창이 닫힐 때 이벤트 해제
        if (isEventRegistered)
        {
            EditorApplication.update -= EditorAutoRefresh;
            Sound_Test.OnActiveEntityChanged -= RefreshActiveEntity;
            isEventRegistered = false;

            //Debug.Log("[AnimationTester] 이벤트 해제 완료");
        }
    }


    private void EditorAutoRefresh()
    {
        // Sound_Test 존재 여부 체크만
        var soundTest = FindObjectOfType<Sound_Test>();
        if (soundTest == null) return;
    }

    //────────────────────────────────────────────
    // 🔹 현재 활성 GameEntity 갱신
    //────────────────────────────────────────────
    private void RefreshActiveEntity()
    {
        var soundTest = FindObjectOfType<Sound_Test>();
        if (soundTest == null)
        {
            activeEntity = null;
            animators.Clear();
            Repaint();
            return;
        }

        // 활성화 GameEntity 긁어오기
        var field = typeof(Sound_Test).GetField("activeEntity", BindingFlags.NonPublic | BindingFlags.Instance);
        activeEntity = field?.GetValue(soundTest) as GameEntity;

        if (activeEntity != null)
        {
            animators = activeEntity.GetComponentsInChildren<GameEntityAnimator>(true).ToList();
            attackPatterns = activeEntity.GetComponent<AttributeSystem>().m_AttackPatterns.ToList();
        }
        else
        {
            animators.Clear();
            attackPatterns.Clear();
        }

        Repaint();
    }

    //────────────────────────────────────────────
    // 🔹 메인 GUI
    //────────────────────────────────────────────
    private void Handle_DrawGameEntityAnimation()
    {
        DrawLine();
        EditorGUILayout.LabelField("키보드 화살표 <- ->를 이용하여 오브젝트를 활성화 시킵니다.", EditorStyles.boldLabel);
        DrawLine();

        EditorGUILayout.Space(5);
        if (GUILayout.Button("🔄 Refresh", GUILayout.Height(25)))
            RefreshActiveEntity();

        EditorGUILayout.Space(5);

        if (activeEntity == null)
        {
            EditorGUILayout.HelpBox("현재 활성화된 GameEntity가 없습니다.", MessageType.Info);
            return;
        }

        GUIStyle header = new(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField($"🎯 활성 오브젝트: {activeEntity.name}", header);

        GameEntityAnimationTester_scrollPos = EditorGUILayout.BeginScrollView(GameEntityAnimationTester_scrollPos);

        // ───── 기본 애니메이션 ─────
        DrawSectionTitle("💡 기본 행동 애니메이션");
        if (animators.Count == 0)
            EditorGUILayout.HelpBox("GameEntityAnimator가 없습니다.", MessageType.Warning);
        else
            animators.ForEach(DrawAnimatorSection);

        // ───── 공격 패턴 애니메이션 ─────
        DrawSectionTitle("⚔ 공격 패턴 애니메이션");
        if (attackPatterns.Count == 0)
            EditorGUILayout.HelpBox("Attack Pattern이 없습니다.", MessageType.Warning);
        else
            attackPatterns.ForEach(a => DrawAttackPatternSection(a, animators));

        EditorGUILayout.EndScrollView();
    }

    private void DrawSectionTitle(string title)
    {
        EditorGUILayout.Space(15);
        DrawLine();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        DrawLine();
    }

    private void DrawLine(int thickness = 2, int padding = 4)
    {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        r.height = thickness;
        r.y += padding / 2;
        r.x -= 2;
        r.width += 4;
        EditorGUI.DrawRect(r, new Color(0.3f, 0.3f, 0.3f, 1));
    }

    private void DrawSubLine(Color color, int thickness = 1, int padding = 2)
    {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        r.height = thickness;
        r.y += padding / 2;
        r.x -= 2;
        r.width += 4;
        EditorGUI.DrawRect(r, color);
    }

    //────────────────────────────────────────────
    // ⚔ 공격 패턴 섹션
    //────────────────────────────────────────────
    private void DrawAttackPatternSection(AttackPattern attack, List<GameEntityAnimator> animators)
    {
        if (attack == null)
        {
            EditorGUILayout.HelpBox("⚠️ AttackPattern이 비활성화되어 표시할 수 없습니다.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField($"🎯 {attack.AttackName ?? "(이름 없음)"}", EditorStyles.boldLabel);

        // m_Clips 필드 탐색 (상속 포함)
        FieldInfo clipsField = null;
        var type = attack.GetType();
        while (type != null)
        {
            clipsField = type.GetField("m_Clips", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (clipsField != null) break;
            type = type.BaseType;
        }

        if (clipsField == null)
        {
            EditorGUILayout.HelpBox("⚠️ AttackPattern에 m_Clips 필드가 없습니다.", MessageType.None);
            return;
        }

        var clipsArray = clipsField.GetValue(attack) as Array;
        if (clipsArray == null || clipsArray.Length == 0)
        {
            EditorGUILayout.HelpBox("🎞️ 등록된 애니메이션 클립이 없습니다.", MessageType.Info);
            return;
        }

        int attackIndex = 1;

        foreach (var clipData in clipsArray)
        {
            if (clipData == null)
            {
                EditorGUILayout.HelpBox($"⚔ 어택 {attackIndex} 데이터가 null입니다.", MessageType.Warning);
                attackIndex++;
                continue;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"⚔ 어택 {attackIndex}", EditorStyles.boldLabel);

            // AnimationClip 필드 탐색
            var fields = clipData.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var attackAnimationClipField = fields.FirstOrDefault(f => f.Name.Contains("AttackAnimationClip"));
            var failAnimationClipField = fields.FirstOrDefault(f => f.Name.Contains("ReadyFailAnimationClip"));

            // 1) clipData 내부에 AudioClip 타입 필드가 있는지 찾아본다 (예: per-clip audio)
            var clipLevelAudioField = fields.FirstOrDefault(f => f.FieldType == typeof(AudioClip)
                || (f.FieldType.IsArray && f.FieldType.GetElementType() == typeof(AudioClip)));

            // 2) 패턴(attack) 자체에 선언된 대표 사운드 필드 찾아보기 (fallback)
            var patternAttackAudioField = attack.GetType().GetField("AttackAudioClip", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var patternMissAudioField = attack.GetType().GetField("AttackMissClipList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var patternFailAudioField = attack.GetType().GetField("m_AttackFailAudioClip", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            // (디버그) 어떤 필드를 찾았는지 로그: 필요하면 활성화
            //Debug.Log($"clipLevelAudioField={clipLevelAudioField?.Name}, patternAttackAudioField={patternAttackAudioField?.Name}");

            // ─ 공격 애니메이션
            if (attackAnimationClipField != null)
            {
                var clip = attackAnimationClipField.GetValue(clipData) as AnimationClip;
                string clipName = clip ? clip.name : "(null)";

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button($"┣ 공격 애니메이션 ▶ {clipName}", GUILayout.Height(22)))
                {
                    if (clip != null && animators.Count > 0 && animators[0] != null)
                    {
                        SetCombatActionAttack(attack);
                        PlayClipAttackAnimation(animators[0], clip);
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ {attack.AttackName}: 애니메이터나 클립이 null이라 재생할 수 없습니다.");
                    }
                }

                // 사운드 라벨 및 ▶ 단독 재생
                //if (attack.AttackAudioClip != null && animators.Count > 0 && animators[0] != null)
                //{
                //    DrawSoundLine(animators[0], attack.AttackAudioClip, E_GameEntityClipType.Attack);
                //}

                EditorGUILayout.EndHorizontal();
            }

            // ─ 공격 실패 애니메이션
            if (failAnimationClipField != null)
            {
                var failClip = failAnimationClipField.GetValue(clipData) as AnimationClip;
                string failName = failClip ? failClip.name : "(null)";

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button($"┗ 공격 실패 애니메이션 ▶ {failName}", GUILayout.Height(22)))
                {
                    if (failClip != null && animators.Count > 0 && animators[0] != null)
                    {
                        SetCombatActionAttack(attack);
                        PlayClipAttackAnimation(animators[0], failClip, true);
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ {attack.AttackName}: 실패 애니메이션을 재생할 수 없습니다.");
                    }
                }

                {
                    var failSoundField = attack.GetType().GetField("m_AttackFailAudioClip", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var failSound = failSoundField?.GetValue(attack) as AudioClip;

                    if (failSound != null && animators.Count > 0 && animators[0] != null)
                    {
                        DrawSoundLine(animators[0], failSound, E_GameEntityClipType.AttackReadyFail);
                    }

                    EditorGUILayout.EndHorizontal();

                    //────────────────────────────────────────────
                    // 🎧 공격 사운드 설정 ObjectField
                    //────────────────────────────────────────────
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("🎵 공격 패턴 사운드", EditorStyles.boldLabel);
                }
            }

            // ===== per-clip AudioClip 필드 UI 처리 (clipData 내부에 AudioClip 필드가 있을 때) =====
            if (clipLevelAudioField != null)
            {
                // 단일 AudioClip 또는 AudioClip[]인 경우 분기
                if (clipLevelAudioField.FieldType == typeof(AudioClip))
                {
                    var currentClip = clipLevelAudioField.GetValue(clipData) as AudioClip;
                    var newClip = (AudioClip)EditorGUILayout.ObjectField("어택별 사운드(clip)", currentClip, typeof(AudioClip), false);
                    if (newClip != currentClip)
                    {
                        // Undo/Dirty는 최상위 ScriptableObject(attack)에 적용
                        Undo.RecordObject(attack, "Change ClipData Audio");
                        clipLevelAudioField.SetValue(clipData, newClip);
                        EditorUtility.SetDirty(attack);
                    }
                }
                else if (clipLevelAudioField.FieldType.IsArray && clipLevelAudioField.FieldType.GetElementType() == typeof(AudioClip))
                {
                    var arr = clipLevelAudioField.GetValue(clipData) as AudioClip[];
                    var list = arr?.ToList() ?? new List<AudioClip>() { null };
                    // 간단하게 첫 슬롯만 노출 (원하면 전체 리스트 UI로 확장)
                    var current = list.FirstOrDefault();
                    var newClip = (AudioClip)EditorGUILayout.ObjectField("어택별 사운드(clip[])", current, typeof(AudioClip), false);
                    if (newClip != current)
                    {
                        Undo.RecordObject(attack, "Change ClipData AudioArray");
                        if (list.Count == 0) list.Add(newClip); else list[0] = newClip;
                        clipLevelAudioField.SetValue(clipData, list.ToArray());
                        EditorUtility.SetDirty(attack);
                    }
                }
            }
            else
            {
                // ===== fallback: AttackPattern(attack) 필드 사용 =====
                // 공격 사운드 (단일)
                if (patternAttackAudioField != null)
                {
                    var currentClip = patternAttackAudioField.GetValue(attack) as AudioClip;
                    var newClip = (AudioClip)EditorGUILayout.ObjectField("공격 사운드 (패턴)", currentClip, typeof(AudioClip), false);
                    if (newClip != currentClip)
                    {
                        Undo.RecordObject(attack, "Change Pattern AttackAudioClip");
                        patternAttackAudioField.SetValue(attack, newClip);
                        EditorUtility.SetDirty(attack);
                    }

                    // 그리고 플레이 버튼/라벨도 같이 보여주고 싶다면
                    if (currentClip != null && animators.Count > 0 && animators[0] != null)
                        DrawSoundLine(animators[0], currentClip, E_GameEntityClipType.Attack);
                }

                // 공격 미스 리스트 (배열)
                if (patternMissAudioField != null)
                {
                    var arr = patternMissAudioField.GetValue(attack) as AudioClip[];
                    var first = arr?.FirstOrDefault();
                    var newClip = (AudioClip)EditorGUILayout.ObjectField("공격 미스 사운드(첫 슬롯)", first, typeof(AudioClip), false);
                    if (newClip != first)
                    {
                        Undo.RecordObject(attack, "Change Pattern AttackMissClipList[0]");
                        // 안전하게 복사해서 교체
                        var list = (arr?.ToList()) ?? new List<AudioClip>() { null };
                        if (list.Count == 0) list.Add(newClip); else list[0] = newClip;
                        patternMissAudioField.SetValue(attack, list.ToArray());
                        EditorUtility.SetDirty(attack);
                    }
                }

                // 준비 실패(ReadyFail) 같은 필드가 패턴에 있다면 처리
                if (patternFailAudioField != null)
                {
                    var currentFail = patternFailAudioField.GetValue(attack) as AudioClip;
                    var newFail = (AudioClip)EditorGUILayout.ObjectField("공격 준비 실패 사운드 (패턴)", currentFail, typeof(AudioClip), false);
                    if (newFail != currentFail)
                    {
                        Undo.RecordObject(attack, "Change Pattern FailAudio");
                        patternFailAudioField.SetValue(attack, newFail);
                        EditorUtility.SetDirty(attack);
                    }
                }
            }

            attackIndex++;
            DrawSubLine(new Color(0.3f, 0.3f, 0.3f, 1f));
        }
    }


    //────────────────────────────────────────────
    // 🎧 사운드 라벨 + ▶ 단독 재생 버튼
    //────────────────────────────────────────────
    private void DrawSoundLine(GameEntityAnimator animator, AudioClip clip, E_GameEntityClipType clipType)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"🎧 {clipType}: {clip.name}", EditorStyles.miniLabel);
        if (GUILayout.Button("▶", GUILayout.Width(30)))
        {
            PlaySoundOnly(animator, clip, clipType);
        }
        EditorGUILayout.EndHorizontal();
    }


    /// <summary>
    /// GameEntityAnimator의 모든 애니메이션 필드 표시 및 테스트 재생
    /// </summary>
    private void DrawAnimatorSection(GameEntityAnimator animator)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField($"🎬 {animator.GetType().Name}", EditorStyles.boldLabel);

        // 🔹 GameEntityAnimator 및 자식 클래스의 AnimationClip / AnimationClip[] 필드 Reflection
        var fields = new List<FieldInfo>();
        var type = animator.GetType();

        while (type != null && type != typeof(MonoBehaviour))
        {
            fields.AddRange(
                type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(f =>
                        f.FieldType == typeof(AnimationClip) ||
                        (f.FieldType.IsArray && f.FieldType.GetElementType() == typeof(AnimationClip)))
            );
            type = type.BaseType;
        }

        foreach (var field in fields)
        {
            var value = field.GetValue(animator);
            string label = ObjectNames.NicifyVariableName(field.Name);

            // 단일 AnimationClip
            if (value is AnimationClip singleClip && singleClip != null)
            {
                if (GUILayout.Button($"▶ {label}: {singleClip.name}", GUILayout.Height(22)))
                    PlayClip(animator, singleClip, label);

                // 🎧 사운드 이름 표시
                var sounder = animator.GetComponentInParent<GameEntitySounder>();
                if (sounder != null)
                {
                    string soundName = GetSoundNameForLabel(sounder, label);
                    if (!string.IsNullOrEmpty(soundName))
                        EditorGUILayout.LabelField($"🎧 {soundName}", EditorStyles.miniLabel);
                }
            }

            // AnimationClip[] 배열
            else if (value is AnimationClip[] clipArray && clipArray.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button($"▶ {label}", GUILayout.Height(22)))
                    PlayClip(animator, clipArray[0], label);

                if (!listFoldout.ContainsKey(label))
                    listFoldout[label] = false;

                if (GUILayout.Button(listFoldout[label] ? "▼" : "▶", GUILayout.Width(35)))
                    listFoldout[label] = !listFoldout[label];

                EditorGUILayout.EndHorizontal();

                // 배열 펼침 UI
                if (listFoldout[label])
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < clipArray.Length; i++)
                    {
                        var clip = clipArray[i];
                        if (clip == null) continue;

                        if (GUILayout.Button($"• {clip.name}", GUILayout.Height(20)))
                            PlayClip(animator, clip, label);
                    }
                    EditorGUI.indentLevel--;
                }

                // 🎧 사운드 이름 표시
                var sounder = animator.GetComponentInParent<GameEntitySounder>();
                if (sounder != null)
                {
                    string soundName = GetSoundNameForLabel(sounder, label);
                    if (!string.IsNullOrEmpty(soundName))
                        EditorGUILayout.LabelField($"🎧 {soundName}", EditorStyles.miniLabel);
                }
            }
        }

        DrawSoundControlSection(animator);

    }

    private void DrawSoundControlSection(GameEntityAnimator animator)
    {
        var sounder = animator.GetComponentInParent<GameEntitySounder>();
        if (sounder == null) return;

        EditorGUILayout.Space(10);
        DrawSectionTitle("🎧 기본 사운드 제어");

        // 🔹 모든 AudioClip[] 필드 자동 탐색
        var soundFields = sounder.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(AudioClip[]))
            .ToList();

        foreach (var field in soundFields)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(field.Name), GUILayout.Width(180));

            var clips = field.GetValue(sounder) as AudioClip[];
            var list = clips?.ToList() ?? new List<AudioClip>();

            // ✅ 리스트가 비어 있으면 null 슬롯 하나 추가
            if (list.Count == 0)
                list.Add(null);

            EditorGUILayout.BeginVertical();

            // 🎵 각 슬롯 표시 (ObjectField + 삭제 버튼)
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                bool removeThis = false;
                var newClip = (AudioClip)EditorGUILayout.ObjectField(list[i], typeof(AudioClip), false);
                if (newClip != list[i])
                {
                    list[i] = newClip;
                    field.SetValue(sounder, list.ToArray());
                    EditorUtility.SetDirty(sounder);
                }

                // 🗑 삭제 버튼
                GUI.enabled = list.Count > 1;
                if (GUILayout.Button("−", GUILayout.Width(22)))
                    removeThis = true;
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();

                // 🔹 삭제를 루프 밖에서 수행 (Begin/End 균형 보장)
                if (removeThis)
                {
                    list.RemoveAt(i);
                    field.SetValue(sounder, list.ToArray());
                    EditorUtility.SetDirty(sounder);
                    break; // break는 여기선 안전함
                }
            }


            // ➕ 슬롯 추가 버튼
            if (GUILayout.Button("+ 추가", GUILayout.Width(80)))
            {
                list.Add(null);
                field.SetValue(sounder, list.ToArray());
                EditorUtility.SetDirty(sounder);
            }

            EditorGUILayout.EndVertical();

            // ▶ 단독 재생 버튼
            if (GUILayout.Button("▶", GUILayout.Width(30)))
            {
                var enumType = Enum.TryParse<E_GameEntityClipType>(
                    field.Name.Replace("ClipList", ""), true, out var clipType)
                    ? clipType
                    : E_GameEntityClipType.Idle;

                var first = list.FirstOrDefault();
                if (first != null)
                    sounder.SoundPlay(first, clipType.ToString());
                else
                    Debug.Log($"⚠️ {field.Name}에 재생할 오디오 클립이 없습니다.");
            }

            EditorGUILayout.EndHorizontal();
        }
    }




    //────────────────────────────────────────────
    // 🎧 일반 사운드 이름 매핑
    //────────────────────────────────────────────
    private string GetSoundNameForLabel(GameEntitySounder sounder, string label)
    {
        label = label.ToLower();
        if (label.Contains("spawn")) return sounder.SpawnClipList?.FirstOrDefault()?.name;
        if (label.Contains("despawn")) return sounder.DeSpawnClipList?.FirstOrDefault()?.name;
        if (label.Contains("death")) return sounder.DestroyClipList?.FirstOrDefault()?.name;
        if (label.Contains("walk") && sounder is GameEntitySounder co1) return co1.WalkClipList?.FirstOrDefault()?.name;
        if (label.Contains("run") && sounder is GameEntitySounder co2) return co2.RunClipList?.FirstOrDefault()?.name;
        return null;
    }

    //────────────────────────────────────────────
    // ▶ 애니메이션 재생 (일반)
    //────────────────────────────────────────────
    private void PlayClip(GameEntityAnimator animator, AnimationClip clip, string label = "")
    {
        if (animator == null || clip == null) return;
        string stateName = label.Replace("Animation Clip", "").Trim();

        // 🔹 이동 타입 변경
        var controllable = animator.GetComponentInParent<ControllableObject>();
        if (controllable != null)
        {
            if (stateName.Contains("Idle"))
                SetMoveType(controllable, E_MoveType.Idle);
            else if (stateName.Contains("Walk"))
                SetMoveType(controllable, E_MoveType.Walk);
            else if (stateName.Contains("Run"))
                SetMoveType(controllable, E_MoveType.Run);


            var sounder = controllable.GetSounderManager();
            if (sounder != null)
            {
                // 애니메이션 이름 기반으로 자동 사운드 연동
                if (stateName.Contains("Spawn"))
                    sounder.SoundPlay(sounder.SpawnClipList, E_GameEntityClipType.Spawn.ToString());
                else if (stateName.Contains("DeSpawn"))
                    sounder.SoundPlay(sounder.DeSpawnClipList, E_GameEntityClipType.DeSpawn.ToString());
                else if (stateName.Contains("Death"))
                    sounder.SoundPlay(sounder.DestroyClipList, E_GameEntityClipType.Death.ToString());
                else if (stateName.Contains("Revive"))
                    sounder.SoundPlay(sounder.ReviveClipList, E_GameEntityClipType.Revive.ToString());
                
                if (stateName.Contains("Critical Damaged"))
                    sounder.SoundPlay(sounder.CriticalDamagedClipList, E_GameEntityClipType.Damaged.ToString());
                else if (stateName.Contains("Damaged"))
                    sounder.SoundPlay(sounder.DamagedClipList, E_GameEntityClipType.Damaged.ToString());
            }
        }

        if (stateName.Contains("Critical"))
            stateName = "Damaged";



        animator.ChangeAnimationAtStart(stateName, clip, true);
        Debug.Log($"[AnimationTester] {animator.name} → {clip.name} 재생됨");
    }


    private void SetMoveType(ControllableObject obj, E_MoveType type)
    {
        var moveTypeField = typeof(ControllableObject)
            .GetField("m_EMoveType", BindingFlags.NonPublic | BindingFlags.Instance);
        moveTypeField?.SetValue(obj, type);
    }

    private void PlayClipAttackAnimation(GameEntityAnimator animator, AnimationClip clip, bool isFailAnimation = false)
    {
        if (animator == null || clip == null) return;
        string stateName = "";

        if(isFailAnimation)
        {
            stateName = "AttackReadyFail";

        }
        else
        {
            stateName = "Attack";
        }

        animator.ChangeAnimationAtStart(stateName, clip, true);
        Debug.Log($"[AnimationTester] {animator.name} → {clip.name} 재생됨");
    }

    //────────────────────────────────────────────
    // ▶ 애니메이션 없이 사운드만 재생
    //────────────────────────────────────────────
    private void PlaySoundOnly(GameEntityAnimator animator, AudioClip clip, E_GameEntityClipType clipType)
    {
        if (animator == null || clip == null) return;

        var sounder = animator.GetComponentInParent<GameEntitySounder>();
        if (sounder == null)
        {
            Debug.LogWarning($"⚠️ {animator.name} 에서 GameEntitySounder를 찾을 수 없습니다.");
            return;
        }

        sounder.SoundPlay(clip, clipType.ToString());
        Debug.Log($"🔊 [{clipType}] 사운드 재생: {clip.name}");
    }


    //────────────────────────────────────────────
    // ⚔ CombatAction의 m_ThisTimeAttack 설정
    //────────────────────────────────────────────
    private void SetCombatActionAttack(AttackPattern pattern)
    {
        if (activeEntity == null) return;

        var combat = activeEntity.GetComponentInChildren<CombatAction>();
        if (combat != null)
        {
            combat.m_ThisTimeAttack = pattern;
            Debug.Log($"[AnimationTester] CombatAction.m_ThisTimeAttack = {pattern.AttackName}");
        }
    }
}
