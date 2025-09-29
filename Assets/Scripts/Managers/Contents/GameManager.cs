using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Define;
using static Unity.VisualScripting.Member;

public class GameManager
{
    public EventHandler OnDungeonExplosionStart; // 미궁 탐험 시작
    public EventHandler OnDungeonExplosionFail; // 미궁 탐험 실패
    public EventHandler OnDungeonExplosionFinish; // 미궁 탐험 종료

    public bool m_IsGamePauseing { get; private set; } = false;

    [Header("Data")]
    public int m_PlaySlotId;
    public float sessionStartTime;

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitAttackAnimationStepAnimation()
    {
        if (CheckRunMethodThisScene() == false)
            return;

        AttackPattern[] attackPatterns = Resources.LoadAll<AttackPattern>("Data/Unit");
        foreach (var ap in attackPatterns)
        {
            ap.Init();
            //Debug.Log($"[RewardInitializer] {reward.name} initialized.");
        }
    }

    static bool CheckRunMethodThisScene()
    {
        if (Managers.Scene.CurrentScene == null)
            return false;

        if (Managers.Scene.CurrentScene.SceneType != Define.Scene.Game)
            return false;

        return true;
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

        Debug.Log("게임 일시 정지 해제");
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
}
