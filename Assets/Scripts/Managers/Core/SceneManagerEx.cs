using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;


public class SceneManagerEx
{
    public BaseScene CurrentScene { get { return GameObject.FindFirstObjectByType<BaseScene>(); } }
    public Define.Scene NextScene { get; private set; }

	public void LoadScene(Define.Scene type)
    {
        Managers.Clear();

        // 1차로 로딩 신으로 들어간 다음에
        // 다음 씬으로 진입한다.
        SceneManager.LoadScene(GetSceneName(Define.Scene.Loading));
        NextScene = type;
    }

    string GetSceneName(Define.Scene type)
    {
        string name = System.Enum.GetName(typeof(Define.Scene), type);
        return name;
    }

    public string GetCurrentSceneName()
    {
        string curScene = SceneManager.GetActiveScene().name;
        return curScene;
    }

    public string GetNextSceneName()
    {
        string name = System.Enum.GetName(typeof(Define.Scene), NextScene);
        return name;
    }

    public void Clear()
    {
        CurrentScene.Clear();
        NextScene = Define.Scene.Unknown;
    }


    #region  어드레서블
    private Dictionary<string, SceneInstance> _sceneInstances = new();

    public async Task LoadSceneAsync(Define.Scene nextScene, Action onComplete = null)
    {
        string currentSceneName = GetCurrentSceneName();

        // 로딩씬 로드
        var loadingTCS = new TaskCompletionSource<SceneInstance>();
        Addressables.LoadSceneAsync(Define.Scene.Loading.ToString(), LoadSceneMode.Additive, activateOnLoad: true).Completed += (handle) =>
        {
            SceneManager.SetActiveScene(handle.Result.Scene);
            loadingTCS.SetResult(handle.Result);
        };
        await loadingTCS.Task;

        // 현재씬 언로드
        var unLoadTCS = new TaskCompletionSource<bool>();
        if (TryGetSceneInstance(currentSceneName, out var instance))
        {
            Addressables.UnloadSceneAsync(instance);
            _sceneInstances.Remove(currentSceneName);
            unLoadTCS.SetResult(true);
        }
        await Task.WhenAll(unLoadTCS.Task, Task.Delay(3000));

        // 다음씬 로드
        var nextTCS = new TaskCompletionSource<bool>();
        Addressables.LoadSceneAsync(GetSceneName(nextScene), LoadSceneMode.Additive, activateOnLoad: true).Completed += (handle) =>
        {
            SceneManager.SetActiveScene(handle.Result.Scene);
            AddSceneIsntance(GetSceneName(nextScene), handle.Result);
            onComplete?.Invoke();
            nextTCS.SetResult(true);
        };
        await nextTCS.Task;

        // 로딩씬 언로드
        Addressables.UnloadSceneAsync(loadingTCS.Task.Result);
    }

    public void AddSceneAdditive(Define.Scene nextScene, Action onComplete = null)
    {
        Addressables.LoadSceneAsync(GetSceneName(nextScene), LoadSceneMode.Additive).Completed += (handle) =>
        {
            SceneManager.SetActiveScene(handle.Result.Scene);
            AddSceneIsntance(GetSceneName(nextScene), handle.Result);
            onComplete?.Invoke();
        };
    }

    private void AddSceneIsntance(string sceneName, SceneInstance instance)
    {
        if (_sceneInstances.ContainsKey(sceneName) == false)
            _sceneInstances.Add(sceneName, instance);
    }

    private bool TryGetSceneInstance(string sceneName, out SceneInstance instance)
    {
        instance = default;
        if (_sceneInstances.ContainsKey(sceneName))
        {
            instance = _sceneInstances[sceneName];
            return true;
        }
        return false;
    }
    #endregion
}
