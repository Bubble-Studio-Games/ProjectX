using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

public partial class CustomToolWindow 
{
    #region FBX 파일 안에 있는 애니메이션의 이름을 FBX 파일로 변경 + Rig/Avatar/Loop 확장

    private bool renameClipsToFileName = true;

    // Root Transform Rotation
    private bool applyRootRotation = true;
    private bool rootRotBake = true;
    private bool rootRotBasedOriginal = true;
    private float rootRotOffset = 0f;

    // Root Transform Position Y
    private bool applyRootPosY = true;
    private bool rootPosYBake = true;
    private bool rootPosYBasedOriginal = true;
    private float rootPosYOffset = 0f;

    // Root Transform Position XZ
    private bool applyRootPosXZ = true;
    private bool rootPosXZBake = true;
    private bool rootPosXZBasedOriginal = true;
    private float rootPosXZOffset = 0f;

    // Loop
    private bool FBXapplyLoopOptions = true;
    private bool loop = false;

    // ✅ 추가: Rig/Avatar 옵션
    private bool applyRigSettings = false;
    private ModelImporterAnimationType selectedAnimationType = ModelImporterAnimationType.Human;
    private Avatar avatarOverride;

    // ✅ 추가: 특정 이름 Loop 설정
    private bool applyLoopByName = true;
    private string loopKeywords = "idle,walk,run,hover,fly"; // 쉼표 구분 입력

    private void Handle_FBXAnimationBatchTool()
    {
        GUILayout.Label("🎬 FBX Animation Batch Processor", EditorStyles.boldLabel);
        GUILayout.Space(5);

        EditorGUILayout.LabelField("✔ 적용 범위: 현재 선택된 FBX 또는 폴더 내부 전체", EditorStyles.helpBox);

        renameClipsToFileName = EditorGUILayout.ToggleLeft("🎯 애니메이션 이름을 FBX 이름으로 변경", renameClipsToFileName);

        // ✅ Rig 설정 UI
        applyRigSettings = EditorGUILayout.BeginToggleGroup("🦴 Rig 설정 적용", applyRigSettings);
        selectedAnimationType = (ModelImporterAnimationType)EditorGUILayout.EnumPopup("Animation Type", selectedAnimationType);
        avatarOverride = (Avatar)EditorGUILayout.ObjectField("Avatar Override (Humanoid)", avatarOverride, typeof(Avatar), false);
        EditorGUILayout.EndToggleGroup();

        GUILayout.Space(10);
        GUILayout.Label("⚙ Root Transform 설정 (세부 옵션)", EditorStyles.boldLabel);

        applyRootRotation = EditorGUILayout.BeginToggleGroup("Root Transform Rotation", applyRootRotation);
        rootRotBake = EditorGUILayout.Toggle("Bake Into Pose", rootRotBake);
        rootRotBasedOriginal = EditorGUILayout.Toggle("Based Upon Original", rootRotBasedOriginal);
        rootRotOffset = EditorGUILayout.FloatField("Offset", rootRotOffset);
        EditorGUILayout.EndToggleGroup();

        applyRootPosY = EditorGUILayout.BeginToggleGroup("Root Transform Position (Y)", applyRootPosY);
        rootPosYBake = EditorGUILayout.Toggle("Bake Into Pose", rootPosYBake);
        rootPosYBasedOriginal = EditorGUILayout.Toggle("Based Upon Original", rootPosYBasedOriginal);
        rootPosYOffset = EditorGUILayout.FloatField("Offset", rootPosYOffset);
        EditorGUILayout.EndToggleGroup();

        applyRootPosXZ = EditorGUILayout.BeginToggleGroup("Root Transform Position (XZ)", applyRootPosXZ);
        rootPosXZBake = EditorGUILayout.Toggle("Bake Into Pose", rootPosXZBake);
        rootPosXZBasedOriginal = EditorGUILayout.Toggle("Based Upon Original", rootPosXZBasedOriginal);
        rootPosXZOffset = EditorGUILayout.FloatField("Offset", rootPosXZOffset);
        EditorGUILayout.EndToggleGroup();


        // Loop Setting
        FBXapplyLoopOptions = EditorGUILayout.BeginToggleGroup("🔁 Loop 설정 일괄 적용", FBXapplyLoopOptions);
        loop = EditorGUILayout.Toggle("기본 Loop 적용", loop);
        EditorGUILayout.EndToggleGroup();

        // ✅ 이름 기반 Loop 자동 적용
        applyLoopByName = EditorGUILayout.BeginToggleGroup("📝 특정 이름 포함 시 Loop 자동 적용", applyLoopByName);
        loopKeywords = EditorGUILayout.TextField("Loop 키워드 (쉼표 구분)", loopKeywords);
        EditorGUILayout.EndToggleGroup();

        GUILayout.Space(15);

        if (GUILayout.Button("🚀 변경 적용", GUILayout.Height(35)))
            ApplyChangesToSelectedFBXs();
    }

    private void ApplyChangesToSelectedFBXs()
    {
        var selectedObjects = Selection.objects;

        foreach (var selected in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(selected);

            // 폴더라면 내부 FBX 포함 재귀 처리
            if (AssetDatabase.IsValidFolder(path))
            {
                string[] fbxFiles = AssetDatabase.FindAssets("t:Model", new[] { path })
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray();

                foreach (var fbx in fbxFiles)
                    ProcessFBX(fbx);

                continue;
            }

            if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                ProcessFBX(path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ 모든 FBX 적용 완료!");
    }

    private void ProcessFBX(string assetPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null) return;

        // ✅ Rig 설정 적용
        if (applyRigSettings)
        {
            importer.animationType = selectedAnimationType;
            if (selectedAnimationType == ModelImporterAnimationType.Human && avatarOverride != null)
                importer.sourceAvatar = avatarOverride;
        }

        // 기존 Clip 불러오기
        var clips = importer.defaultClipAnimations;   // ← ★ 커브 보존되는 원본
        var newClips = new ModelImporterClipAnimation[clips.Length];

        string[] keywords = loopKeywords.ToLower().Split(',').Select(k => k.Trim()).ToArray();

        for (int i = 0; i < clips.Length; i++)
        {
            var src = clips[i];

            // 🔥 기존 src clip의 모든 설정 + 이벤트 + 마스크 + 커브 그대로 복사
            var clip = JsonUtility.FromJson<ModelImporterClipAnimation>(
                           JsonUtility.ToJson(src));

            clip.firstFrame = src.firstFrame;
            clip.lastFrame = src.lastFrame;

            // 필요 부분만 덮어쓰기
            clip.name = renameClipsToFileName ? Path.GetFileNameWithoutExtension(assetPath) : src.name;

            if (applyRootRotation)
            {
                clip.lockRootRotation = rootRotBake;
                clip.keepOriginalOrientation = rootRotBasedOriginal;
            }

            // ✅ 특정 키워드 포함 시 Loop 자동 활성화
            if (applyLoopByName)
                if (keywords.Any(k => clip.name.ToLower().Contains(k)))
                    clip.loopTime = true;

            // Root Transform Rotation
            if (applyRootRotation)
            {
                clip.lockRootRotation = rootRotBake;

                // Based Upon Original
                clip.keepOriginalOrientation = rootRotBasedOriginal;
            }

            // Root Transform Position (Y)
            if (applyRootPosY)
            {
                clip.lockRootHeightY = rootPosYBake;

                // Based Upon Original
                clip.keepOriginalPositionY = rootPosYBasedOriginal;
            }

            // Root Transform Position (XZ)
            if (applyRootPosXZ)
            {
                clip.lockRootPositionXZ = rootPosXZBake;

                // Based Upon Original
                clip.keepOriginalPositionXZ = rootPosXZBasedOriginal;
            }


            newClips[i] = clip;
        }

        importer.clipAnimations = newClips;
        AssetDatabase.ImportAsset(assetPath);
        Debug.Log($"🎯 적용됨 → {assetPath}");
    }

    #endregion


    #region AddColliderToSelectedAndChildren

    private enum ColliderType
    {
        BoxCollider,
        CapsuleCollider,
        SphereCollider,
        CircleCollider2D,
        BoxCollider2D,
        CapsuleCollider2D,
        MeshCollider,
        Custom
    }

    private ColliderType selectedColliderType = ColliderType.BoxCollider;
    private bool applyPrefabOverrides = false;
    private bool includeSelectedObject = true;

    private void HandleAddColliderToSelectedAndChildren()
    {
        // 선택된 오브젝트를 가져옵니다.
        GameObject selectedObject = Selection.activeGameObject;

        // 선택된 오브젝트가 없으면 메시지를 출력합니다.
        if (selectedObject == null)
        {
            EditorGUILayout.HelpBox("No object selected. Please select an object in the Hierarchy.", MessageType.Warning);
            return;
        }

        // 콜라이더 타입 선택 필드를 표시합니다.
        selectedColliderType = (ColliderType)EditorGUILayout.EnumPopup("Collider Type", selectedColliderType);

        // 선택한 오브젝트를 포함할지 여부를 결정짓는 체크박스를 추가합니다.
        includeSelectedObject = EditorGUILayout.Toggle("Include Selected Object", includeSelectedObject);

        // 프리팹 오버라이드 적용 여부를 결정짓는 체크박스를 추가합니다.
        applyPrefabOverrides = EditorGUILayout.Toggle("Apply Prefab Overrides", applyPrefabOverrides);

        // Apply 버튼을 표시합니다.
        if (GUILayout.Button("Apply"))
        {
            // 선택된 오브젝트를 포함할지 여부에 따라 콜라이더 추가
            if (includeSelectedObject)
            {
                AddColliderRecursively(selectedObject, selectedColliderType);
            }
            else
            {
                foreach (Transform child in selectedObject.transform)
                {
                    AddColliderRecursively(child.gameObject, selectedColliderType);
                }
            }

            Debug.Log("Added " + selectedColliderType.ToString() + " to " + selectedObject.name + " and its children.");

            if (applyPrefabOverrides)
            {
                ApplyPrefabOverrides(selectedObject);
                Debug.Log("Applied prefab overrides for " + selectedObject.name + " and its children.");
            }
        }
    }

    private static void AddColliderRecursively(GameObject obj, ColliderType colliderType)
    {
        // 선택된 콜라이더 타입에 따라 콜라이더 추가
        switch (colliderType)
        {
            case ColliderType.BoxCollider:
                if (obj.GetComponent<BoxCollider>() == null)
                    obj.AddComponent<BoxCollider>();
                break;
            case ColliderType.CapsuleCollider:
                if (obj.GetComponent<CapsuleCollider>() == null)
                    obj.AddComponent<CapsuleCollider>();
                break;
            case ColliderType.SphereCollider:
                if (obj.GetComponent<SphereCollider>() == null)
                    obj.AddComponent<SphereCollider>();
                break;
            case ColliderType.CircleCollider2D:
                if (obj.GetComponent<CircleCollider2D>() == null)
                    obj.AddComponent<CircleCollider2D>();
                break;
            case ColliderType.BoxCollider2D:
                if (obj.GetComponent<BoxCollider2D>() == null)
                    obj.AddComponent<BoxCollider2D>();
                break;
            case ColliderType.CapsuleCollider2D:
                if (obj.GetComponent<CapsuleCollider2D>() == null)
                    obj.AddComponent<CapsuleCollider2D>();
                break;
            case ColliderType.MeshCollider:
                if (obj.GetComponent<MeshCollider>() == null)
                    obj.AddComponent<MeshCollider>();
                break;
            case ColliderType.Custom:
                // 사용자 정의 콜라이더 추가 로직 (필요한 경우 추가 가능)
                break;
        }

        // 모든 자식 오브젝트에 대해서도 동일하게 적용합니다.
        foreach (Transform child in obj.transform)
        {
            AddColliderRecursively(child.gameObject, colliderType);
        }
    }

    private static void ApplyPrefabOverrides(GameObject obj)
    {
        // 현재 오브젝트가 프리팹의 일부인 경우 변경사항을 적용합니다.
        if (PrefabUtility.IsPartOfPrefabInstance(obj))
        {
            PrefabUtility.ApplyPrefabInstance(obj, InteractionMode.UserAction);
        }

        // 모든 자식 오브젝트에 대해서도 동일하게 적용합니다.
        foreach (Transform child in obj.transform)
        {
            ApplyPrefabOverrides(child.gameObject);
        }
    }

    #endregion

    #region ProPixelizer Material Converter

    private void Handle_ConvertMaterialsToProPixelizer()
    {
        if (GUILayout.Button("🚀 변경 적용", GUILayout.Height(35)))
        {
            ConvertMaterialsToProPixelizer();
        }
    }

    private void ConvertMaterialsToProPixelizer()
    {
        Object[] selectedObjects = Selection.objects;
        HashSet<Material> uniqueMaterials = new HashSet<Material>();
        int convertedCount = 0;
        int skippedCount = 0;

        // 1. 선택된 GameObject 및 Material에서 머티리얼 수집
        foreach (var obj in selectedObjects)
        {
            if (obj is GameObject go)
            {
                Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    foreach (var mat in renderer.sharedMaterials)
                    {
                        if (mat != null) uniqueMaterials.Add(mat);
                    }
                }
            }
            else if (obj is Material mat)
            {
                uniqueMaterials.Add(mat);
            }
        }

        if (uniqueMaterials.Count == 0)
        {
            EditorUtility.DisplayDialog("변환 대상 없음", "선택된 오브젝트에서 변환할 수 있는 머티리얼이 없습니다.", "확인");
            return;
        }

        // 2. 머티리얼 변환
        foreach (var mat in uniqueMaterials)
        {
            if (mat.shader.name != "Universal Render Pipeline/Lit")
            {
                Debug.Log($"[무시됨] {mat.name} — URP/Lit 쉐이더가 아님");
                continue;
            }

            // 추가 텍스처가 존재하는 경우 → 변환하지 않음
            string[] extraProps = {
                "_MaskMap", "_DetailMap", "_ParallaxMap", "_SpecGlossMap", "_EmissionMap"
            };

            bool hasExtraTextures = false;
            foreach (var prop in extraProps)
            {
                if (mat.HasProperty(prop) && mat.GetTexture(prop) != null)
                {
                    Debug.LogWarning($"[스킵됨] {mat.name} — '{prop}' 텍스처가 할당되어 있어 변환하지 않습니다.");
                    hasExtraTextures = true;
                    break;
                }
            }

            if (hasExtraTextures)
            {
                skippedCount++;
                continue;
            }

            // 필요한 텍스처만 가져와서 설정
            Texture baseMap = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
            Texture normalMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;

            mat.shader = Shader.Find("ProPixelizer/SRP/PixelizedWithOutline");

            // ProPixelizer 쉐이더의 실제 프로퍼티 이름에 할당
            if (baseMap != null && mat.HasProperty("_Albedo"))
                mat.SetTexture("_Albedo", baseMap);

            if (normalMap != null && mat.HasProperty("_NormalMap"))
                mat.SetTexture("_NormalMap", normalMap);


            Debug.Log($"[변환 완료] {mat.name} → ProPixelizer");
            convertedCount++;
        }

        // 결과 출력
        EditorUtility.DisplayDialog(
            "ProPixelizer 변환 완료",
            $"✅ 변환 완료: {convertedCount}개\n⚠️ 스킵됨(추가 텍스처 있음): {skippedCount}개",
            "확인"
        );
    }

    #endregion

    #region Ragoll Auto Wizard

    private GameObject selectedObject;
    private float defaultMass = 20f;
    private Vector3 defaultForce = new Vector3(0, -10f, 0);

    void Handle_RagollAutoWizard()
    {
        GUILayout.Label("💀 Ragdoll 자동 할당기", EditorStyles.boldLabel);

        selectedObject = (GameObject)EditorGUILayout.ObjectField("🎯 대상 오브젝트", selectedObject, typeof(GameObject), true);
        defaultMass = EditorGUILayout.FloatField("🧱 질량 설정 (Rigidbody)", defaultMass);
        defaultForce = EditorGUILayout.Vector3Field("💨 힘 적용 (테스트용)", defaultForce);

        if (GUILayout.Button("🧱 Ragdoll Builder 열기 + 본 자동 할당"))
        {
            HandleAutoAssign();
        }
    }
    private void HandleAutoAssign()
    {
        GameObject[] selection = Selection.gameObjects;

        if (selection.Length == 0 && selectedObject == null)
        {
            Debug.LogWarning("⚠️ 적용할 오브젝트가 없습니다. 씬에서 하나 선택하거나 드래그해주세요.");
            return;
        }

        if (selection.Length > 1)
        {
            Debug.LogWarning("⚠️ 두 개 이상의 오브젝트가 선택되었습니다. 자동 할당은 하지 않고 RagdollBuilder 창만 열겠습니다.");
            EditorApplication.ExecuteMenuItem("GameObject/3D Object/Ragdoll...");
            return;
        }

        GameObject target = selectedObject != null ? selectedObject : selection[0];

        // 1. RagdollBuilder 열기
        EditorApplication.ExecuteMenuItem("GameObject/3D Object/Ragdoll...");

        // 2. 내부 타입 획득
        var ragdollType = typeof(Editor).Assembly.GetType("UnityEditor.RagdollBuilder");
        if (ragdollType == null)
        {
            Debug.LogError("❌ UnityEditor.RagdollBuilder 타입을 찾을 수 없습니다.");
            return;
        }

        // 3. 열린 창에서 찾기
        EditorApplication.delayCall += () =>
        {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            foreach (var win in windows)
            {
                if (win.GetType() == ragdollType)
                {
                    bool assigned = AssignBonesViaReflection(win, target);
                    win.Repaint();

                    if (!assigned)
                    {
                        Debug.LogError("❌ pelvis 본이 비어 있습니다. Ragdoll 생성이 중단됩니다.");
                        return;
                    }

                    // 4. Rigidbody 질량 자동 조절 및 Force 테스트
                    EditorApplication.delayCall += () => ConfigureRigidbodies(target);

                    // ✅ 자동으로 생성까지 수행
                    MethodInfo createMethod = ragdollType.GetMethod("OnWizardCreate", BindingFlags.NonPublic | BindingFlags.Instance);
                    createMethod?.Invoke(win, null);
                    Debug.Log("🎉 Ragdoll 자동 생성 완료!");
                    return;
                }
            }
        };

    }

    private bool AssignBonesViaReflection(EditorWindow ragdollWindow, GameObject target)
    {
        Animator animator = target.GetComponent<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
        {
            Debug.LogError("❌ Humanoid 타입의 Animator가 필요합니다.");
            return false;
        }

        string[] boneFields = {
            "pelvis", "leftHips", "leftKnee", "leftFoot",
            "rightHips", "rightKnee", "rightFoot",
            "leftArm", "leftElbow", "rightArm", "rightElbow",
            "middleSpine", "head"
        };

        HumanBodyBones[] humanBones = {
            HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm,
            HumanBodyBones.Spine, HumanBodyBones.Head
        };

        bool hasPelvis = false;

        var type = ragdollWindow.GetType();
        for (int i = 0; i < boneFields.Length; i++)
        {
            var field = type.GetField(boneFields[i], BindingFlags.Public | BindingFlags.Instance);
            if (field == null) continue;

            var bone = animator.GetBoneTransform(humanBones[i]);
            if (bone != null)
            {
                field.SetValue(ragdollWindow, bone);
                if (boneFields[i] == "pelvis") hasPelvis = true;
            }
        }

        MethodInfo updateMethod = type.GetMethod("OnWizardUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
        updateMethod?.Invoke(ragdollWindow, null);

        Debug.Log("✅ Ragdoll 본 자동 할당 완료!");
        return hasPelvis;
    }


    private void ConfigureRigidbodies(GameObject root)
    {
        Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rigidbodies)
        {
            rb.mass = defaultMass;

            // 테스트용 Force 추가
            rb.AddForce(defaultForce, ForceMode.Impulse);
        }

        Debug.Log($"🧱 Rigidbody 질량 설정 완료 ({rigidbodies.Length}개): {defaultMass}");
    }

    #endregion

    #region Delete File

    private string selectedFolder = "Assets";
    private bool deleteSubFolders = true;
    private bool deleteOnlyTopLevelFiles = false;

    private List<string> favorites = new List<string>();
    private List<string> recentFolders = new List<string>();
    private int recentSelectedIndex = -1;
    private int favoriteSelectedIndex = -1;

    void Handle_DeleteFile()
    {
        GUILayout.Label("폴더 선택", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        selectedFolder = EditorGUILayout.TextField("Target Folder", selectedFolder);
        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    selectedFolder = "Assets" + path.Substring(Application.dataPath.Length);
                }
                else
                {
                    EditorUtility.DisplayDialog("경고", "Assets 폴더 내부만 선택 가능합니다.", "확인");
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        deleteSubFolders = EditorGUILayout.Toggle("하위 폴더 포함 삭제", deleteSubFolders);
        deleteOnlyTopLevelFiles = EditorGUILayout.Toggle("현재 폴더 파일만 삭제", deleteOnlyTopLevelFiles);

        GUILayout.Space(10);

        // 즐겨찾기
        GUILayout.Label("즐겨찾기", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("즐겨찾기 추가"))
        {
            if (!string.IsNullOrEmpty(selectedFolder) && !favorites.Contains(selectedFolder))
                favorites.Add(selectedFolder);
        }
        if (GUILayout.Button("즐겨찾기 삭제"))
        {
            favorites.Remove(selectedFolder);
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < favorites.Count; i++)
        {
            if (GUILayout.Button(favorites[i]))
                selectedFolder = favorites[i];
        }

        GUILayout.Space(10);

        // 최근 삭제
        GUILayout.Label("최근 삭제한 폴더", EditorStyles.boldLabel);
        if (recentFolders.Count > 0)
        {
            recentSelectedIndex = EditorGUILayout.Popup("Recent", recentSelectedIndex, recentFolders.ToArray());
            if (recentSelectedIndex >= 0 && recentSelectedIndex < recentFolders.Count)
            {
                selectedFolder = recentFolders[recentSelectedIndex];
            }
        }

        GUILayout.Space(20);

        if (GUILayout.Button("삭제 실행"))
        {
            DeleteFilesInFolder();
        }
    }

    private void DeleteFilesInFolder()
    {
        string absolutePath = Application.dataPath + selectedFolder.Substring("Assets".Length);

        if (!Directory.Exists(absolutePath))
        {
            EditorUtility.DisplayDialog("오류", "경로가 존재하지 않습니다:\n" + absolutePath, "확인");
            return;
        }

        string[] files = Directory.GetFiles(absolutePath, "*", deleteSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        int deletedCount = 0;
        foreach (string file in files)
        {
            //if (deleteOnlyTopLevelFiles && Path.GetDirectoryName(file) != absolutePath)
           //     continue;

            if (Path.GetExtension(file) == ".meta") continue; // Unity meta 파일은 유지

            try
            {
                File.Delete(file);
                deletedCount++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"삭제 실패: {file}\n{ex}");
            }
        }

        AssetDatabase.Refresh();

        if (!recentFolders.Contains(selectedFolder))
            recentFolders.Insert(0, selectedFolder);
        if (recentFolders.Count > 10)
            recentFolders.RemoveAt(recentFolders.Count - 1);

        EditorUtility.DisplayDialog("완료", $"{deletedCount} 개 파일을 삭제했습니다.", "확인");
    }

    #endregion
}
