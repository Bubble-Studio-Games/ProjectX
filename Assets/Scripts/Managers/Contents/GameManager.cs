using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Define;
using static Unity.VisualScripting.Member;
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


}
