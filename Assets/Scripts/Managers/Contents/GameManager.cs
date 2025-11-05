using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Define;
using static Unity.VisualScripting.Member;
using static UnityEngine.Splines.SplineInstantiate;
using static UnityEngine.UI.Image;
using Scene = Define.Scene;

public class GameManager
{
    public EventHandler OnDungeonExplosionStart; // 미궁 탐험 시작
    public EventHandler OnDungeonExplosionFail; // 미궁 탐험 실패
    public EventHandler OnDungeonExplosionFinish; // 미궁 탐험 종료

    public bool m_IsGamePauseing { get; private set; } = false;

    [Header("Data")]
    public int m_PlaySlotId;
    public float sessionStartTime;

    // 패턴별로, (owner, field, originalClip) 목록을 저장
    private static readonly Dictionary<AttackPattern, List<ClipBackup>> _attackPatternAnimationOriginals = new();

    private sealed class ClipBackup
    {
        public object Owner;          // 필드의 실제 소유자 (패턴이 아닐 수 있음)
        public FieldInfo Field;       // AnimationClip 필드 자체
        public AnimationClip Original; // 원본 클립
    }

    // 클래스 상단에 캐시 추가
    private readonly Dictionary<(E_RangeShapeType, (int, int, int, int, int, int), E_RangeFillType), HashSet<GridPosition>> _patternOffsetCache
        = new();


    #region Init

    public void Init()
    {
        sessionStartTime = Time.realtimeSinceStartup;

        m_PlaySlotId = Managers.Data.playStatistics?.lastSlotID ?? 0;
    }

    // 보상품 리스트의 뽑힐 확률의 총합을 1.0으로 맞춤.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitAllRewards()
    {
        if (CheckRunMethodThisScene() == false)
            return;

        Reward[] rewards = Resources.LoadAll<Reward>("Data/Reward");
        foreach (var reward in rewards)
        {
            reward.Init();
            //Debug.Log($"[RewardInitializer] {reward.name} initialized.");
        }
    }

    /// <summary>
    /// AttackPattern 내부의 모든 AnimationClip을 스텝 애니메이션으로 변환
    /// 중복 변환 방지 및 캐시 기반 로드
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitAttackAnimationStepAnimation()
    {
        if (!CheckRunMethodThisScene()) return;

        var patterns = Resources.LoadAll<AttackPattern>("Data/AttackPattern");
        int convertedCount = 0;

        foreach (var pattern in patterns)
        {
            if (pattern == null) continue;

            pattern.Init();

            // 백업용 사전 초기화
            if (!_attackPatternAnimationOriginals.ContainsKey(pattern))
                _attackPatternAnimationOriginals[pattern] = new();

            // 모든 AnimationClip 필드 검색 (배열/리스트/Serializable 내부 포함)
            var clipFields = Util.FindAllFieldsOfType<AnimationClip>(pattern);

            foreach (var (field, owner, clip) in clipFields)
            {
                if (clip == null) continue;
                if (clip.name.Contains("_stepped_")) continue;

                // 🔸 (owner, field) 단위로 한 번만 백업
                if (!_attackPatternAnimationOriginals[pattern].Exists(b => ReferenceEquals(b.Owner, owner) && b.Field == field))
                {
                    _attackPatternAnimationOriginals[pattern].Add(new ClipBackup
                    {
                        Owner = owner,
                        Field = field,
                        Original = clip
                    });
                }

                // 스텝 애니메이션 로드 or 변환
                var stepped = SettingManager.Instance.ReplaceOrLoadSteppedClip(clip);
                if (stepped != null && stepped != clip)
                {
                    // 실제 필드 값 교체
                    Util.ReplaceFieldValue(owner, field, stepped);
                    convertedCount++;
                }
            }
        }

        //Debug.Log($"✅ AttackPattern 스텝 애니메이션 변환 완료: {convertedCount}개 변환됨 ({patterns.Length}개 패턴)");
    }

    // 🔹 게임 종료 시 원본 복원
    public static void RestoreOriginalClips()
    {
        int restoreCount = 0;

        foreach (var kvp in _attackPatternAnimationOriginals)
        {
            var pattern = kvp.Key;
            var backups = kvp.Value;

            foreach (var b in backups)
            {
                try
                {
                    // 🔸 원래의 owner와 field를 사용해 되돌린다
                    b.Field.SetValue(b.Owner, b.Original);
                    restoreCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"⚠️ 복원 실패: {pattern.name} :: {b.Owner?.GetType().Name}.{b.Field?.Name} - {e.Message}");
                }
            }
        }

        //Debug.Log($"♻️ AttackPattern 원본 복원 완료: {restoreCount}개 필드 복원 (패턴 {_attackPatternOriginals.Count}개)");
    }

    static bool CheckRunMethodThisScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (!sceneName.Contains(Scene.Dungeon.ToString()) && !sceneName.Contains(Scene.Test.ToString()))
            return false;

        return true;
    }


    // 🔹 게임 종료 시 Restore 호출
    public void OnApplicationQuit()
    {
        RestoreOriginalClips();
    }

    #endregion


    public Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    // 선택한 오브젝트의 가장 긴 y축(월드 상) 가져오기
    public float GetObjectColliderLongLength(GameObject obj)
    {
        Collider col = obj.GetComponentInChildren<Collider>();
        if (col == null)
            return 1f; // 기본값

        Vector3 scaledSize = Vector3.zero;

        switch (col)
        {
            case BoxCollider box:
                scaledSize = Vector3.Scale(box.size, obj.transform.lossyScale);
                return Mathf.Max(scaledSize.x, scaledSize.y, scaledSize.z); // 또는 방향 기준
            case SphereCollider sphere:
                return sphere.radius * 2f * obj.transform.lossyScale.x; // 지름
            case CapsuleCollider capsule:
                return capsule.height * obj.transform.lossyScale.y;
            case MeshCollider mesh:
                scaledSize = Vector3.Scale(mesh.sharedMesh.bounds.size, obj.transform.lossyScale);
                return Mathf.Max(scaledSize.x, scaledSize.y, scaledSize.z);
            default:
                return 1f;
        }
    }

    #region Dungeon Start & End

    public void DungeonExplosionStart()
    {

    }

    public void DungeonExplosionFail()
    {
        Debug.Log("Dungeon Core destroyed! Game Over.");
        OnDungeonExplosionFail?.Invoke(this, EventArgs.Empty);

        // 팝업 띄우기
        Managers.UI.ShowPopupUI<GameOverUI>();

        DungeonExplosionFinish();
    }

    public void DungeonExplosionFinish()
    {
        PauseGame();
    }

    // 일시 정지
    public void PauseGame()
    {
        // 게임 진행 멈춤
        Time.timeScale = 0f;
        AudioListener.pause = false; // 음악은 유지
        m_IsGamePauseing = true;


        //Debug.Log("게임 일시 정지");
    }

    // 일시 정지 해제
    public void ResumeGame()
    {
        Time.timeScale = 1f;
        m_IsGamePauseing = false;

        //Debug.Log("게임 일시 정지 해제");
    }

    public async Task GameSave(Action action = null)
    {
        Debug.Log("Data Save...");

        // 게임 일시 정지
        PauseGame();

        // 저장 팝업 표시하기

        await Managers.Save.SaveAllData();

        action?.Invoke();
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(); // 어플리케이션 종료
#endif
    }

    #endregion


    public float GetStateClipLength(Animator animator, string stateName, int layerIndex = 0)
    {
        if (animator == null) return 0f;

        var controller = animator.runtimeAnimatorController as AnimatorController;
        if (controller == null) return 0f;

        foreach (var childState in controller.layers[layerIndex].stateMachine.states)
        {
            if (childState.state.name == stateName && childState.state.motion is AnimationClip clip)
            {
                return clip.length;
            }
        }

        return 0f; // 못 찾았을 때
    }

    #region Screen Shot

    // 1. 스크린 샷 찍기
    // UI를 다 띄운 것도 보여줌
    private Texture2D CaptureScreenshot()
    {
        int width = Screen.width;
        int height = Screen.height;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        return tex;
    }

    public Texture2D CaptureCamera()
    {
        int width = Screen.width;
        int height = Screen.height;
        var cam = Camera.main;

        // 1. RenderTexture 생성
        RenderTexture rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;

        // 2. 카메라 렌더링
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        cam.Render();

        // 3. 픽셀 읽기
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        // 4. 리소스 정리
        cam.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.Destroy(rt);

        return tex;
    }


    public void CaptureAndSave()
    {
        CoroutineRunner.Instance.StartCoroutine(ICaptureAndSave());
    }

    // 2. 파일로 저장하기 (PNG)
    private IEnumerator ICaptureAndSave()
    {
        yield return new WaitForEndOfFrame(); // 화면 렌더 끝난 후 캡처

        Texture2D tex = CaptureCamera();
        byte[] bytes = tex.EncodeToPNG();


#if UNITY_EDITOR
        // 에디터 환경에서만 직접 Assets 접근
        string slotPath = $"{Managers.Data.GetFilePath()}/slot_{m_PlaySlotId}.png";
#else
    // 빌드 시에는 StreamingAssets에서 불러오기
    return Application.streamingAssetsPath + "/";
#endif

        File.WriteAllBytes(slotPath, bytes);

        //Debug.Log($"스크린샷 저장 완료: {slotPath}");

        GameObject.Destroy(tex); // 메모리 해제

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    public Sprite LoadScreenShot(int slotID)
    {
#if UNITY_EDITOR
        // 에디터 환경에서만 직접 Assets 접근
        string slotPath = $"{Managers.Data.GetFilePath()}/slot_{slotID}.png";
#else
    // 빌드 시에는 StreamingAssets에서 불러오기
    return Application.streamingAssetsPath + "/";
#endif

        if (!File.Exists(slotPath))
            return null;

        byte[] bytes = File.ReadAllBytes(slotPath);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    #endregion

    public void FilCopyAndRename(string directory, string originalName, string newName)
    {
        string srcPath = Path.Combine(directory, originalName).Replace("\\", "/");
        string dstPath = Path.Combine(directory, newName).Replace("\\", "/");

        if (!File.Exists(srcPath))
        {
            Debug.LogError($"파일을 찾을 수 없습니다: {srcPath}");
            return;
        }

        File.Copy(srcPath, dstPath, overwrite: true);
        Debug.Log($"파일 복사 완료: {originalName} → {newName}");
    }

    public void GameEntityModelsSetLayer(GameEntity gameEntity, int layerID)
    {
        if (gameEntity == null)
            return;

        foreach (var (mat, obj) in gameEntity.GetModelsMaterial())
        {
            if (obj != null)
                obj.layer = layerID;
        }
    }

    public void GameEntityModelsSetColor(GameEntity gameEntity, Color color)
    {
        foreach (var (mat, obj) in gameEntity.GetModelsMaterial())
        {
            if (mat != null)
                mat.color = color;
        }
    }


    #region Grid Range System With Attack Pattern


    // 1. 공격 오프셋 가져오기
    public HashSet<GridPosition> GetAllPatternOffsets(IEnumerable<AttackPattern> attackPatterns)
    {
        HashSet<GridPosition> unique = new();

        foreach (var pattern in attackPatterns)
        {
            unique.AddRange(GetPatternOffsets(pattern));
        }

        return unique;
    }

    public HashSet<GridPosition> GetPatternOffsets(AttackPattern pattern)
    {
        if (pattern == null)
            return new();

        // 키 생성
        var key = (
            pattern.m_ERangeShapeType,
            pattern.GetRangeMinMaxFromOffsets(),
            pattern.m_ERangeFillType
        );

        // 이미 계산된 캐시가 있으면 그대로 반환
        if (_patternOffsetCache.TryGetValue(key, out var cached))
            return cached;

        // 없으면 새로 계산
        var computed = CalculatePatternOffsets(pattern);

        // 캐싱
        _patternOffsetCache[key] = computed;

        return computed;
    }

    public HashSet<GridPosition> CalculatePatternOffsets(AttackPattern pattern)
    {
        var unique = new HashSet<GridPosition>();
        if (pattern == null)
            return unique;

        // 1) bounding box 결정: custom offsets가 있으면 그것의 박스, 없으면 radius 기반 박스
        var range = pattern.GetRangeMinMaxFromOffsets();

        switch (pattern.m_ERangeShapeType)
        {
            // 최소 값, 최대 값을 구해서 중심을 반경으로 사각형 형태의 범위를 구한다.
            case E_RangeShapeType.Square:

                for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                {
                    for (int x = range.MinX; x <= range.MaxX; x++)
                    {
                        for (int z = range.MinZ; z <= range.MaxZ; z++)
                        {
                            var offset = new GridPosition(x, z, f);

                            if (pattern.m_ERangeFillType == E_RangeFillType.FullRange)
                                unique.Add(offset);

                            // 경계선 위치한 셀만 true
                            else if (pattern.m_ERangeFillType == E_RangeFillType.OuterRing)
                            {
                                if (x == range.MinX || x == range.MaxX ||
                                    z == range.MinZ || z == range.MaxZ)
                                    unique.Add(offset);
                            }
                            // 경계선 안쪽 위치한 셀만 true
                            else
                            {
                                if (x != range.MinX && x != range.MaxX && z != range.MinZ && z != range.MaxZ)
                                    unique.Add(offset);
                            }

                        }
                    }
                }

                break;
            case E_RangeShapeType.Checker: 
                // TODO
                // 대각선의 경우에도 대각 선 방향일 때에도 격자가 진행되도록 해야 됨
                // 또한 매번 격자가 항상 바뀔지, 아니면 그대로 할지도 정해야 됨. 
                for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                {
                    for (int x = range.MinX; x <= range.MaxX; x++)
                    {
                        for (int z = range.MinZ; z <= range.MaxZ; z++)
                        {
                            var offset = new GridPosition(x, z, f);

                            if (pattern.m_ERangeFillType == E_RangeFillType.FullRange)
                            {
                                if((x + z) % 2 == 0)
                                    unique.Add(offset);
                            }

                            // 경계선 위치한 셀만 true
                            else if (pattern.m_ERangeFillType == E_RangeFillType.OuterRing)
                            {
                                if (x == range.MinX || x == range.MaxX ||
                                    z == range.MinZ || z == range.MaxZ)
                                    if((x + z) % 2 == 0)
                                        unique.Add(offset);
                            }
                            // 경계선 안쪽 위치한 셀만 true
                            else
                            {
                                if (x != range.MinX && x != range.MaxX && z != range.MinZ && z != range.MaxZ)
                                    if((x + z) % 2 == 0)
                                        unique.Add(offset);
                            }

                        }
                    }
                }

                break;
            case E_RangeShapeType.Diamond:
                {
                    int maxX = range.MaxX;
                    int maxZ = range.MaxZ;

                    for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                    {
                        for (int x = -maxX; x <= maxX; x++)
                        {
                            for (int z = -maxZ; z <= maxZ; z++)
                            {
                                // 다이아몬드 형태 기본 조건 (비율 계산 없이)
                                // |x| + |z| <= radius
                                int radius = Mathf.Max(maxX, maxZ);
                                if (Mathf.Abs(x) + Mathf.Abs(z) > radius)
                                    continue;

                                // 🔹 경계선 판정
                                // 이 셀에서 한 칸이라도 나가면 범위를 벗어나는가? → 경계
                                bool isEdge = false;
                                int[][] dirs = new int[][] {
                                                new int[] { 1, 0 },
                                                new int[] { -1, 0 },
                                                new int[] { 0, 1 },
                                                new int[] { 0, -1 }
                                            };


                                foreach (var dir in dirs)
                                {
                                    int nx = x + dir[0];
                                    int nz = z + dir[1];
                                    if (Mathf.Abs(nx) + Mathf.Abs(nz) > radius)
                                    {
                                        isEdge = true;
                                        break;
                                    }
                                }

                                var offset = new GridPosition(x, z, f);

                                switch (pattern.m_ERangeFillType)
                                {
                                    case E_RangeFillType.FullRange:
                                        unique.Add(offset);
                                        break;

                                    case E_RangeFillType.OuterRing:
                                        if (isEdge)
                                            unique.Add(offset);
                                        break;

                                    case E_RangeFillType.Inner:
                                        if (!isEdge)
                                            unique.Add(offset);
                                        break;
                                }
                            }
                        }
                    }
                    break;
                }

            case E_RangeShapeType.Arc: // 수정 필요
                {
                    float halfAngle = pattern.m_ArcAngle * 0.5f;

                    // 반경 계산
                    int radius = Mathf.Max(Mathf.Abs(range.MaxX), Mathf.Abs(range.MaxZ));

                    for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                    {
                        for (int x = range.MinX; x <= range.MaxX; x++)
                        {
                            for (int z = range.MinZ; z <= range.MaxZ; z++)
                            {
                                var offset = new GridPosition(x, z, f);

                                // 거리 계산
                                float dist = Mathf.Sqrt(x * x + z * z);
                                if (dist == 0f || dist > radius)
                                    continue;

                                // 각도 계산 (Z축이 forward)
                                float angle = Mathf.Atan2(z, x) * Mathf.Rad2Deg;
                                float diff = Mathf.Abs(Mathf.DeltaAngle(0f, angle)); // 0° 기준 전방

                                if (diff > halfAngle)
                                    continue; // 부채꼴 영역 밖

                                // FillType 처리
                                switch (pattern.m_ERangeFillType)
                                {
                                    case E_RangeFillType.FullRange:
                                        unique.Add(offset);
                                        break;

                                    case E_RangeFillType.OuterRing:
                                        // 외곽(거리 거의 radius인 셀)
                                        if (Mathf.RoundToInt(dist) == radius)
                                            unique.Add(offset);
                                        break;

                                    case E_RangeFillType.Inner:
                                        // 내부 (OuterRing 제외)
                                        if (dist < radius)
                                            unique.Add(offset);
                                        break;
                                }
                            }
                        }
                    }
                    break;
                }

            case E_RangeShapeType.Triangle:
                {
                    int zMax = range.MaxZ;

                    for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                    {
                        for (int z = 0; z < zMax; z++)
                        {
                            int halfWidth = (zMax - 1) - z; // 위로 갈수록 폭이 줄어듦

                            for (int x = -halfWidth; x <= halfWidth; x++)
                            {
                                bool isEdge = (z == 0) || (x == -halfWidth) || (x == halfWidth) || (z == zMax - 1);
                                var offset = new GridPosition(x, z, f);

                                switch (pattern.m_ERangeFillType)
                                {
                                    case E_RangeFillType.FullRange:
                                        unique.Add(offset);
                                        break;

                                    case E_RangeFillType.OuterRing:
                                        if (isEdge)
                                            unique.Add(offset);
                                        break;

                                    case E_RangeFillType.Inner:
                                        if (!isEdge)
                                            unique.Add(offset);
                                        break;
                                }
                            }
                        }
                    }
                    break;
                }

            case E_RangeShapeType.ReverseTriangle:
                {
                    int zMax = range.MaxZ;

                    for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                    {
                        for (int z = 0; z < zMax; z++)
                        {
                            int halfWidth = z; // 아래로 갈수록 폭이 넓어짐

                            for (int x = -halfWidth; x <= halfWidth; x++)
                            {
                                bool isEdge = (z == 0) || (x == -halfWidth) || (x == halfWidth) || (z == zMax - 1);

                                var offset = new GridPosition(x, z, f);

                                switch (pattern.m_ERangeFillType)
                                {
                                    case E_RangeFillType.FullRange:
                                        unique.Add(offset);
                                        break;

                                    case E_RangeFillType.OuterRing:
                                        if (isEdge)
                                            unique.Add(offset);
                                        break;

                                    case E_RangeFillType.Inner:
                                        if (!isEdge)
                                            unique.Add(offset);
                                        break;
                                }
                            }
                        }
                    }
                    break;
                }

            case E_RangeShapeType.Plus:
                for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                {
                    for (int x = range.MinX; x <= range.MaxX; x++)
                    {
                        for (int z = range.MinZ; z <= range.MaxZ; z++)
                        {
                            var offset = new GridPosition(x, z, f);

                            // 십자형 형태: x==0 또는 z==0
                            if (x == 0 || z == 0)
                            {
                                if (pattern.m_ERangeFillType == E_RangeFillType.FullRange)
                                {
                                    unique.Add(offset);
                                }
                                else if (pattern.m_ERangeFillType == E_RangeFillType.OuterRing)
                                {
                                    // 끝단만
                                    if (Mathf.Abs(x) == Mathf.Abs(range.MaxX) ||
                                        Mathf.Abs(z) == Mathf.Abs(range.MaxZ))
                                        unique.Add(offset);
                                }
                                else 
                                {
                                    // 경계선 제외 내부만 (Full - Outer)
                                    if (x > range.MinX && x < range.MaxX &&
                                        z > range.MinZ && z < range.MaxZ)
                                        unique.Add(offset);
                                }
                            }
                        }
                    }
                }
                break;
            case E_RangeShapeType.Vertical:
                for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                {
                    for (int z = range.MinZ; z <= range.MaxZ; z++)
                    {
                        var offset = new GridPosition(0, z, f);

                        switch (pattern.m_ERangeFillType)
                        {
                            case E_RangeFillType.FullRange:
                                unique.Add(offset);
                                break;

                            case E_RangeFillType.OuterRing:
                                // 위/아래 끝단만
                                if (z == range.MinZ || z == range.MaxZ)
                                    unique.Add(offset);
                                break;

                            case E_RangeFillType.Inner:
                                if (z > range.MinZ && z < range.MaxZ)
                                    unique.Add(offset);
                                break;
                        }
                    }
                }
                break;

            case E_RangeShapeType.Horizontal:
                for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                {
                    for (int x = range.MinX; x <= range.MaxX; x++)
                    {
                        var offset = new GridPosition(x, 0, f);

                        switch (pattern.m_ERangeFillType)
                        {
                            case E_RangeFillType.FullRange:
                                unique.Add(offset);
                                break;

                            case E_RangeFillType.OuterRing:
                                // 왼쪽/오른쪽 끝단만
                                if (x == range.MinX || x == range.MaxX)
                                    unique.Add(offset);
                                break;

                            case E_RangeFillType.Inner:
                                if (x > range.MinX && x < range.MaxX)
                                    unique.Add(offset);
                                break;
                        }
                    }
                }
                break;

            case E_RangeShapeType.CustomList:
                {
                    if (pattern.m_RangeOffset == null || pattern.m_RangeOffset.Count == 0)
                        break;

                    var (minX, maxX, minZ, maxZ, minF, maxF) = pattern.GetRangeMinMaxFromOffsets();

                    HashSet<GridPosition> fullRange = new();
                    HashSet<GridPosition> outerRing = new();
                    HashSet<GridPosition> innerRange = new();

                    // 1️⃣ FullRange: min~max 전부 포함
                    for (int f = minF; f <= maxF; f++)
                    {
                        for (int x = minX; x <= maxX; x++)
                        {
                            for (int z = minZ; z <= maxZ; z++)
                            {
                                fullRange.Add(new GridPosition(x, z, f));
                            }
                        }
                    }

                    // 2️⃣ OuterRing: 사용자 지정 오프셋 그대로
                    outerRing.UnionWith(pattern.m_RangeOffset);

                    // 3️⃣ InnerRange: FullRange - OuterRing
                    innerRange.UnionWith(fullRange);
                    innerRange.ExceptWith(outerRing);

                    // 4️⃣ FillType에 따라 결과 반환
                    switch (pattern.m_ERangeFillType)
                    {
                        case E_RangeFillType.FullRange:
                            unique.UnionWith(fullRange);
                            break;

                        case E_RangeFillType.OuterRing:
                            unique.UnionWith(outerRing);
                            break;

                        case E_RangeFillType.Inner:
                            unique.UnionWith(innerRange);
                            break;
                    }

                    break;
                }

        }

        return unique;
    }


    // ② 공격할 위치 가져오기
    public HashSet<GridPosition> GetCanAttackPosition
        (GameEntity owner, 
        GameEntity target, 
        AttackPattern pattern,
        bool canAccess = true )
    {
        HashSet<GridPosition> result = new();
        if (owner == null || pattern == null)
            return result;

        // 로컬 오프셋 → 월드 좌표
        var offsets = GetPatternOffsets(pattern);
        var attackerGridPosition = owner.GetGridPosition();
        var targetGridPosition = target.GetGridPosition();


        // 시작 위치(origin) 및 방향 계산
        // 8방향 모두 조사해야함.
        foreach (var dir in Enum.GetValues(typeof(E_Dir)).Cast<E_Dir>())
        {
            foreach (var offset in offsets)
            {
                GridPosition canAttackPos = LevelGrid.Instance.ToGridPosition(offset, targetGridPosition, dir);

                if(attackerGridPosition == canAttackPos)
                {
                    return new HashSet<GridPosition> { canAttackPos };
                }

                // 유효한 범위만 가져오기
                if (!LevelGrid.Instance.IsValidGridPosition(canAttackPos)) // 유효한 위치만 추가
                    continue;

                if(canAccess)
                {
                    var cellInfo = LevelGrid.Instance.GetGridPositionCellInfo(canAttackPos);
                    if (cellInfo == null)
                        continue;

                    if (cellInfo.Entity != owner && cellInfo.gridType != E_GridCheckType.Walkable)
                        continue;

                    // 갈 수 있는 없는 길이지 체크
                    if (!Pathfinding.Instance.HasPath(owner.GetGridPosition(), canAttackPos))
                    continue;

                    // TODO 너무 멀리 돌아가는가?
                }


                result.Add(canAttackPos);
            }
        }

        return result;
    }

    public void ClearPatternOffsetCache()
    {
        _patternOffsetCache.Clear();
    }


    /// <summary>
    /// 🔍 AttackPattern의 실행 조건을 검사하고,
    /// 지정한 E_AttackCondition만 필터링해서 반환.
    /// </summary>
    public List<(AttackPattern pattern, E_AttackCondition condition, HashSet<GridPosition> canAttackPosition)> EvaluateAttackPatternsByCondition(
        GameEntity owner,
        GameEntity target,
        params E_AttackCondition[] conditions)
    {
        List<(AttackPattern pattern, E_AttackCondition condition, HashSet<GridPosition>)> result = new();

        IEnumerable<AttackPattern> patterns = owner.m_AttributeSystem.m_AttackPatterns;

        if (owner == null || patterns == null)
            return default;

        foreach (var pattern in patterns)
        {
            if (pattern == null)
                continue;

            var attackCanResult = pattern.CanExecute(owner, target);

            // 지정된 조건 중 하나라도 일치하면 추가
            if (conditions.Contains(attackCanResult.condition))
            {
                // 원하는 조건을 만족했지만 그리드 타일 조건을 만족하는지 체크?
                result.Add((pattern, attackCanResult.condition, attackCanResult.CanAttackablePos));
            }
        }

        return result;
    }

    /// <summary>
    /// Game Entity 타입에 따라 하이어 라키가 배치되게 한다.
    /// 나중에..
    /// </summary>
    public void SetTransformGameEntityOfType(GameEntity entity)
    {
        if(Managers.Scene.CurrentScene is DungeonScene dungeonScene)
        {
            switch (entity.m_ObjectType)
            {
                case E_ObjectType.None:
                    break;
                case E_ObjectType.Unit:
                    if(entity.m_TeamId == E_TeamId.Player)
                        entity.transform.SetParent(dungeonScene.PlayerUnits);
                    else if (entity.m_TeamId == E_TeamId.Monster)
                        entity.transform.SetParent(dungeonScene.EnemyUnits);
                    break;
                case E_ObjectType.Building:
                    break;
                case E_ObjectType.Interact:
                    break;
                case E_ObjectType.AutoTrigger:
                    break;
                case E_ObjectType.Obstacle:
                    break;
                case E_ObjectType.Skill:
                    break;
                case E_ObjectType.PassiveObject:
                    break;
                default:
                    break;
            }
        }
    }

    #endregion
}
