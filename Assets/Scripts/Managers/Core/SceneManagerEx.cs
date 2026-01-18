using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;


public class SceneManagerEx
{
    private const float DEFAULT_TIMEOUT_SECONDS = 30f;

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


    public string GetSceneName(Define.Scene type)
    {

        var sceneSettings = GameConfig.Scene;
        if (sceneSettings == null)
        {
            Debug.LogWarning("[SceneManagerEx] SceneSettings가 없어 Enum 이름을 직접 사용합니다.");
            var ret = Enum.GetName(typeof(Define.Scene), type);
            return ret;
        }

        var sceneName = type switch
        {
            Define.Scene.Start => sceneSettings.GetStartScene().ToString(),
            Define.Scene.Camp => sceneSettings.GetCampScene().ToString(),
            Define.Scene.Dungeon => sceneSettings.GetDungeonScene().ToString(),
            Define.Scene.Loading => sceneSettings.GetLoadingScene().ToString(),
            _ => Enum.GetName(typeof(Define.Scene), type)
        };

        return sceneName;
    }

    public string GetCurrentSceneName()
    {
        string curScene = SceneManager.GetActiveScene().name;
        return curScene;
    }

    public string GetNextSceneName()
    {
        var ret = GetSceneName(NextScene);
        return ret;
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
        try
        {
            Managers.Clear();
            string currentSceneName = GetCurrentSceneName();

            // 로딩씬 로드
            var loadingHandle = Addressables.LoadSceneAsync(GetSceneName(Define.Scene.Loading), LoadSceneMode.Additive, activateOnLoad: true);
            var loadingInstance = await loadingHandle.Task;

            if (loadingHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[SceneManagerEx] 로딩 씬 로드 실패: {loadingHandle.Status}");
                return;
            }
            SceneManager.SetActiveScene(loadingInstance.Scene);

            // 현재씬 언로드
            if (TryGetSceneInstance(currentSceneName, out var instance))
            {
                _sceneInstances.Remove(currentSceneName);
                var unloadHandle = Addressables.UnloadSceneAsync(instance);
                await Task.WhenAll(unloadHandle.Task, Task.Delay(3000));
            }
            else
            {
                await Task.Delay(3000);
            }

            // 다음씬 로드
            string nextSceneName = GetSceneName(nextScene);
            var nextHandle = Addressables.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive, activateOnLoad: true);
            var nextInstance = await nextHandle.Task;

            if (nextHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[SceneManagerEx] 다음 씬 로드 실패: {nextSceneName}");
                return;
            }
            SceneManager.SetActiveScene(nextInstance.Scene);
            AddSceneInstance(nextSceneName, nextInstance);
            onComplete?.Invoke();

            // 로딩씬 언로드
            await Addressables.UnloadSceneAsync(loadingInstance).Task;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SceneManagerEx] 씬 전환 중 예외 발생: {ex.Message}");
        }
    }

    public async void AddSceneAdditive(Define.Scene nextScene, Action onComplete = null)
    {
        string sceneName = GetSceneName(nextScene);
        var handle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        var instance = await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[SceneManagerEx] Additive 씬 로드 실패: {sceneName}");
            return;
        }

        SceneManager.SetActiveScene(instance.Scene);
        AddSceneInstance(sceneName, instance);
        onComplete?.Invoke();
    }

    private void AddSceneInstance(string sceneName, SceneInstance instance)
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
