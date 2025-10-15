using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Define;
using static Unity.VisualScripting.Member;
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
    private static readonly Dictionary<AttackPattern, List<ClipBackup>> _attackPatternOriginals = new();

    private sealed class ClipBackup
    {
        public object Owner;          // 필드의 실제 소유자 (패턴이 아닐 수 있음)
        public FieldInfo Field;       // AnimationClip 필드 자체
        public AnimationClip Original; // 원본 클립
    }


    #region Init

    public void Init()
    {
        sessionStartTime = Time.realtimeSinceStartup;
    }

    // 보상품 리스트의 뽑힐 확률의 총합을 1.0으로 맞춤.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitAllRewards()
    {
        if (CheckRunMethodThisScene() == false)
            return;

        RewardData[] rewards = Resources.LoadAll<RewardData>("Data/Reward");
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

        var patterns = Resources.LoadAll<AttackPattern>("Data/Unit");
        int convertedCount = 0;

        foreach (var pattern in patterns)
        {
            if (pattern == null) continue;

            // 백업용 사전 초기화
            if (!_attackPatternOriginals.ContainsKey(pattern))
                _attackPatternOriginals[pattern] = new();

            // 모든 AnimationClip 필드 검색 (배열/리스트/Serializable 내부 포함)
            var clipFields = Util.FindAllFieldsOfType<AnimationClip>(pattern);

            foreach (var (field, owner, clip) in clipFields)
            {
                if (clip == null) continue;
                if (clip.name.Contains("_stepped_")) continue;

                // 🔸 (owner, field) 단위로 한 번만 백업
                if (!_attackPatternOriginals[pattern].Exists(b => ReferenceEquals(b.Owner, owner) && b.Field == field))
                {
                    _attackPatternOriginals[pattern].Add(new ClipBackup
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

        Debug.Log($"✅ AttackPattern 스텝 애니메이션 변환 완료: {convertedCount}개 변환됨 ({patterns.Length}개 패턴)");
    }

    // 🔹 게임 종료 시 원본 복원
    public static void RestoreOriginalClips()
    {
        int restoreCount = 0;

        foreach (var kvp in _attackPatternOriginals)
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

        Debug.Log($"♻️ AttackPattern 원본 복원 완료: {restoreCount}개 필드 복원 (패턴 {_attackPatternOriginals.Count}개)");
    }




    static bool CheckRunMethodThisScene()
    {
        if (Managers.Scene.CurrentScene == null)
            return false;

        if (Managers.Scene.CurrentScene.SceneType == Scene.Game)
            return true;

        if (Managers.Scene.CurrentScene.SceneType == Scene.Test)
            return true;

        return false;
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

    public float GetObjectLength(GameObject obj)
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

        PauseGame();
        // 게임 일시 정지
        // 팝업 표시하기

        //sessionStartTime = Time.realtimeSinceStartup; // 실행된 시간 기록
        await Managers.Data.asyncSave();
        // 세이브가 전부 되면 action 실행

        //ResumeGame();
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

    #region Grid Range System

    // 오브젝트(유닛, 스킬, 건물 등)를 중심으로 범위 내 영향 영역(Attack / Heal / Buff / Detect 등) 을 계산하고 
    // 시각적으로 표현한다.

    /// <summary>
    /// 📏 반경 기반 정사각형 범위 계산
    /// - 중심 기준 N×N 범위를 계산
    /// - 방향 회전 없음 (기본 북쪽 기준)
    /// - 범위 포함 규칙 (Full, OuterRing 등) 및 유효성 체크 지원
    /// </summary>
    /// <param name="origin">기준 GridPosition</param>
    /// <param name="radius">반경 (예: 2 → 5x5)</param>
    /// <param name="inclusionType">범위 포함 규칙</param>
    /// <param name="checkType">그리드 유효성 검사 타입 (Walkable, HasUnit 등)</param>
    /// <returns>범위 내 GridPosition 목록</returns>

    public List<GridPosition> GetGridRange(
        GridPosition origin,
        int radius,
        E_RangeInclusionType inclusionType,
        E_GridCheckType? checkType = null)
    {
        List<GridPosition> offsets = new();

        for (int x = -radius; x <= radius; x++)
            for (int z = -radius; z <= radius; z++)
                offsets.Add(new GridPosition(x, z, 0));

        return ProcessGridRangeCore(
            origin,
            offsets,
            E_Dir.North, // 회전 없음
            inclusionType,
            includeIntermediate: true,
            checkType);
    }

    /// <summary>
    /// 🎯 단일 방향 기반 라인형 범위 계산
    /// - 하나의 방향 오프셋(예: 전방 3칸)을 기준으로 범위 계산
    /// - LevelGrid.ToGridPosition()을 이용해 방향 회전 처리
    /// - includeIntermediate 옵션으로 중간 칸 포함 여부 설정 가능
    /// </summary>
    /// <param name="entity">방향 기준이 되는 GameEntity</param>
    /// <param name="origin">시작 위치 (기준 GridPosition)</param>
    /// <param name="directionOffset">공격 방향 및 거리 오프셋 (예: 0,3)</param>
    /// <param name="inclusionType">범위 포함 규칙</param>
    /// <param name="includeIntermediate">중간 칸 포함 여부</param>
    /// <param name="checkType">그리드 유효성 검사 타입</param>
    /// <returns>방향을 고려한 GridPosition 목록</returns>

    public List<GridPosition> GetDirectionalRange(
        GameEntity entity,
        GridPosition origin,
        GridPosition directionOffset,
        E_RangeInclusionType inclusionType,
        bool includeIntermediate = true,
        E_GridCheckType? checkType = null)
    {
        var offsets = new List<GridPosition> { directionOffset };

        return ProcessGridRangeCore(
            origin,
            offsets,
            entity.m_CurrentEDir,
            inclusionType,
            includeIntermediate,
            checkType);
    }


    /// <summary>
    /// 다양한 방향 오프셋(List<GridPosition>) 기반의 범위 계산
    /// </summary>
    public List<GridPosition> GetDirectionalRangeList(
        GameEntity entity,
        GridPosition origin,
        List<GridPosition> offsets,
        E_RangeInclusionType inclusionType,
        bool includeIntermediate = true,
        E_GridCheckType? checkType = null)
    {
        return ProcessGridRangeCore(
            origin,
            offsets,
            entity.m_CurrentEDir,
            inclusionType,
            includeIntermediate,
            checkType);
    }

    /// <summary>
    /// ⚙️ 범위 계산 공통 처리 로직
    /// - 방향 회전, 거리 계산, 포함 규칙, 유효성 체크를 일괄 수행
    /// - GetGridRange, GetDirectionalRange, GetDirectionalRangeList에서 공통 호출
    /// - LevelGrid.ToGridPosition()으로 방향 보정, GetGridPositionsBetween()으로 라인 중간칸 계산
    /// </summary>
    /// <param name="origin">중심 좌표</param>
    /// <param name="offsets">범위 오프셋 리스트</param>
    /// <param name="dir">적용 방향</param>
    /// <param name="inclusionType">범위 포함 규칙</param>
    /// <param name="includeIntermediate">중간 칸 포함 여부</param>
    /// <param name="checkType">그리드 유효성 검사 타입</param>
    /// <returns>최종 유효한 GridPosition 목록</returns>
    private List<GridPosition> ProcessGridRangeCore(
    GridPosition origin,
    List<GridPosition> offsets,
    E_Dir dir,
    E_RangeInclusionType inclusionType,
    bool includeIntermediate,
    E_GridCheckType? checkType)
    {
        List<GridPosition> result = new();

        foreach (var offset in offsets)
        {
            // 방향 회전 적용 (LevelGrid의 ToGridPosition 활용)
            GridPosition rotatedTarget = LevelGrid.Instance.ToGridPosition(offset, origin, dir);
            rotatedTarget.floor = origin.floor + offset.floor; 


            // 직선형이라면 중간칸도 계산 (LevelGrid 함수로)
            List<GridPosition> linePath = LevelGrid.Instance.GetGridPositionsBetween(origin, rotatedTarget);

            foreach (var step in linePath)
            {
                if (!LevelGrid.Instance.IsValidGridPosition(step))
                    break;

                if (!includeIntermediate && step != linePath[^1])
                    continue;

                if (IsInclude(step, origin, inclusionType, Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.z))) &&
                    IsValidGrid(step, checkType))
                {
                    result.Add(step);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 🔍 범위 포함 판정
    /// - 중심(origin)과 타겟(target) 간 거리 비교로 포함 여부 결정
    /// - 범위 포함 규칙(Enum)에 따라 특정 모양(Full, Outer, DiagonalOnly 등)을 필터링
    /// </summary>
    /// <param name="target">대상 GridPosition</param>
    /// <param name="origin">기준 GridPosition</param>
    /// <param name="type">범위 포함 규칙</param>
    /// <param name="radius">현재 반경</param>
    /// <returns>해당 셀이 범위 내인지 여부</returns>

    private bool IsInclude(GridPosition target, GridPosition origin, E_RangeInclusionType type, int radius, E_Dir? dir = null)
    {
        int dx = target.x - origin.x;
        int dz = target.z - origin.z;
        int df = Mathf.Abs(target.floor - origin.floor);
        float distance = Mathf.Sqrt(dx * dx + dz * dz + df * df);

        switch (type)
        {
            case E_RangeInclusionType.FullRange:
                return true;

            case E_RangeInclusionType.OuterRing:
                return Mathf.RoundToInt(distance) == radius;

            case E_RangeInclusionType.InnerRing:
                return distance < radius;

            case E_RangeInclusionType.Checker:
                return ((Mathf.Abs(dx) + Mathf.Abs(dz) + df) % 2 == 0);

            case E_RangeInclusionType.DiagonalOnly:
                return (Mathf.Abs(dx) == Mathf.Abs(dz) && Mathf.Abs(dx) <= radius);

            // 호 형태 (예: 전방 90도 부채꼴)
            case E_RangeInclusionType.Arc:
                if (dir == null) return false;
                float angle = Mathf.Atan2(dz, dx) * Mathf.Rad2Deg;
                float centerAngle = GetDirAngle(dir.Value);
                float diff = Mathf.DeltaAngle(centerAngle, angle);
                return distance <= radius && Mathf.Abs(diff) <= 45f; // 전방 90도

            // 삼각형 형태 (전방으로 갈수록 폭이 넓어짐)
            case E_RangeInclusionType.Triangle:
                if (dir == null) return false;
                float coneAngle = Mathf.Atan2(Mathf.Abs(dx), Mathf.Abs(dz)) * Mathf.Rad2Deg;
                return dz > 0 && distance <= radius && coneAngle <= (radius * 10f); // 예시값
        }

        return false;
    }

    private float GetDirAngle(E_Dir dir)
    {
        return dir switch
        {
            E_Dir.North => 0f,
            E_Dir.East => 90f,
            E_Dir.South => 180f,
            E_Dir.West => -90f,
            E_Dir.NorthEast => 45f,
            E_Dir.SouthEast => 135f,
            E_Dir.SouthWest => -135f,
            E_Dir.NorthWest => -45f,
            _ => 0f
        };
    }


    /// <summary>
    /// ✅ 그리드 유효성 검사
    /// - [System.Flags] 기반으로 복합 조건 검사 가능 (ex: Walkable | Empty)
    /// - LevelGrid API를 통해 Walkable / HasUnit / Reserved / Obstacle 상태 확인
    /// - Empty는 유닛, 예약, 장애물 모두 없는 셀로 판정
    /// </summary>
    /// <param name="pos">검사할 GridPosition</param>
    /// <param name="checkType">유효성 검사 플래그 (조합 가능)</param>
    /// <returns>유효한 셀이면 true, 아니면 false</returns>

    private bool IsValidGrid(GridPosition pos, E_GridCheckType? checkType = null)
    {
        if (checkType == null || checkType == E_GridCheckType.None)
            return true;

        bool valid = true;

        if (checkType.Value.HasFlag(E_GridCheckType.Walkable))
            valid &= LevelGrid.Instance.IsValidGridPosition(pos);

        if (checkType.Value.HasFlag(E_GridCheckType.HasUnit))
            valid &= LevelGrid.Instance.HasAnyUnitOnGridPosition(pos);

        if (checkType.Value.HasFlag(E_GridCheckType.Reserved))
            valid &= LevelGrid.Instance.IsReservedGridPosition(pos);

        if (checkType.Value.HasFlag(E_GridCheckType.Obstacle))
        {
            var obj = LevelGrid.Instance.GetObjectAtGridPosition(pos);
            valid &= (obj != null && obj.m_ObjectType == E_ObjectType.Obstacle);
        }

        if (checkType.Value.HasFlag(E_GridCheckType.Empty))
        {
            bool hasUnit = LevelGrid.Instance.HasAnyUnitOnGridPosition(pos);
            bool reserved = LevelGrid.Instance.IsReservedGridPosition(pos);
            bool obstacle = false;
            var obj = LevelGrid.Instance.GetObjectAtGridPosition(pos);
            if (obj != null && obj.m_ObjectType == E_ObjectType.Obstacle)
                obstacle = true;

            valid &= !(hasUnit || reserved || obstacle);
        }

        return valid;
    }



    #endregion

    #region Attack Pattern

    // 🔹 AttackPattern 리스트에서 중복 없는 모든 GridPosition(3D) 오프셋을 수집
    public HashSet<GridPosition> GetAllUniqueAttackOffsets(IEnumerable<AttackPattern> attackPatterns)
    {
        HashSet<GridPosition> unique = new();

        foreach (var pattern in attackPatterns)
        {
            // 1️. CustomList or 직접 오프셋 정의된 경우 그대로 사용
            if (pattern.m_ERangeInclusionType == E_RangeInclusionType.CustomList ||
                (pattern.m_RangeOffset != null && pattern.m_RangeOffset.Count > 0))
            {
                foreach (var off in pattern.m_RangeOffset)
                    unique.Add(off);
                continue;
            }

            // 2️. 그 외 타입은 반경 + 규칙으로 자동 생성
            int r = Mathf.Max(1, pattern.m_RangeRadius);

            for (int f = -r; f <= r; f++)
            {
                for (int x = -r; x <= r; x++)
                {
                    for (int z = -r; z <= r; z++)
                    {
                        GridPosition offset = new GridPosition(x, z, f);
                        if (IsOffsetIncludedByPattern3D(offset, pattern))
                            unique.Add(offset);
                    }
                }
            }
        }

        return unique;
    }


    // 🔹 패턴 타입에 따른 3D 포함 판정
    private bool IsOffsetIncludedByPattern3D(GridPosition offset, AttackPattern pattern)
    {
        int dx = Mathf.Abs(offset.x);
        int dz = Mathf.Abs(offset.z);
        int df = Mathf.Abs(offset.floor);

        float dist2D = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z);
        float dist3D = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z + offset.floor * offset.floor);

        switch (pattern.m_ERangeInclusionType)
        {
            case E_RangeInclusionType.FullRange:
                return dist3D <= pattern.m_RangeRadius;

            case E_RangeInclusionType.OuterRing:
                return Mathf.RoundToInt(dist3D) == pattern.m_RangeRadius;

            case E_RangeInclusionType.InnerRing:
                return dist3D < pattern.m_RangeRadius;

            case E_RangeInclusionType.Checker:
                return ((dx + dz + df) % 2) == 0 && dist3D <= pattern.m_RangeRadius;

            case E_RangeInclusionType.DiagonalOnly:
                // 3D 완전 대각 (x=z=floor)
                return (dx == dz && dz == df && dx <= pattern.m_RangeRadius) ||
                       (dx == dz && df == 0 && dx <= pattern.m_RangeRadius);

            case E_RangeInclusionType.Arc:
                if (dist2D < 0.5f || dist3D > pattern.m_RangeRadius) return false;
                {
                    float angle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
                    float centerAngle = 90f;
                    float half = pattern.m_ArcAngle * 0.5f;
                    float diff = Mathf.DeltaAngle(centerAngle, angle);
                    return Mathf.Abs(diff) <= half;
                }

            case E_RangeInclusionType.Triangle:
                if (offset.z <= 0 || dist3D > pattern.m_RangeRadius) return false;
                {
                    float maxHalfWidthAtFar = pattern.m_RangeRadius;
                    float halfWidth = maxHalfWidthAtFar * (offset.z / (float)pattern.m_RangeRadius);
                    return Mathf.Abs(offset.x) <= Mathf.CeilToInt(Mathf.Clamp(halfWidth, 0f, maxHalfWidthAtFar));
                }

            case E_RangeInclusionType.Cone:
                if (dist2D < 0.5f || dist3D > pattern.m_RangeRadius) return false;
                {
                    float angle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
                    float centerAngle = 90f;
                    float half = pattern.m_ArcAngle * 0.5f;
                    float diff = Mathf.DeltaAngle(centerAngle, angle);
                    return Mathf.Abs(diff) <= half;
                }

            case E_RangeInclusionType.CustomList:
            default:
                return false;
        }
    }


    #endregion
}
