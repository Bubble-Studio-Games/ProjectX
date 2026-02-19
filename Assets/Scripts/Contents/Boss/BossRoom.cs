using UnityEngine;
using Data;
using static Define;
using Unity.Cinemachine;

[RequireComponent(typeof(BoxCollider))]
public class BossRoom : MonoBehaviour
{
    [Header("컷신")]
    [SerializeField] private BossCutSceneController _cutScene;
    [SerializeField] private BoxCollider _collider;

    [Header("보스")]
    [SerializeField] private E_BossType _spawnBossType = E_BossType.Ork;

    private const string BOSS_PATH = "Prefabs/GameEntity/Enemy/{0}";

    private bool _isTriggered = false;
    private BossController _currentBoss;

    private void OnValidate()
    {
        if (_cutScene == null)
            _cutScene = GetComponentInChildren<BossCutSceneController>();

        if (_collider == null)
            _collider = GetComponent<BoxCollider>();
    }

    public void TriggerBossRoom()
    {
        if (_isTriggered)
        {
            Debug.LogWarning("[BossRoom] 이미 트리거됨");
            return;
        }

        var bossSystem = Managers.SceneServices.Get<BossSystem>();
        if (bossSystem == null)
            return;

        bossSystem.BossUI.StopFade();

        _cutScene.OnSpawn += () => SpawnBoss();
        _cutScene.OnFocus += () => FocusBoss();
        _cutScene.OnUnfocus += () => UnFocusBoss();
        _cutScene.OnStart += () =>
        {
            var cameraRig = Managers.SceneServices.CameraRig as CameraController;
            cameraRig?.EnterCutsceneMode();
        };
        _cutScene.OnComplete += () =>
        {
            var cameraRig = Managers.SceneServices.CameraRig as CameraController;
            cameraRig?.ExitCutsceneMode();

            bossSystem.BossUI.FadeIn(1.0f);
        };

        _isTriggered = true;

        bossSystem.SetUpAndRun(this, () =>
        {
            _cutScene.StartCutscene();
        });
    }

    private void SpawnBoss()
    {
        var bossData = Boss.GetBossData(_spawnBossType);
        string path = string.Format(BOSS_PATH, _spawnBossType);
        var direction = Camera.main.transform.position - transform.position;
        var spawnedBoss = Managers.Resource.Instantiate(path, transform.position, Quaternion.LookRotation(direction));

        if (spawnedBoss == null)
        {
            Debug.LogError($"[BossRoom] 보스 프리팹을 찾을 수 없음: {path}");
            return;
        }

        _currentBoss = spawnedBoss.GetComponent<BossController>();
        _currentBoss.Init(bossData);
        _cutScene.AttachAnchorToBoss(_currentBoss.transform, this.transform);

        var bossSystem = Managers.SceneServices.Get<BossSystem>();
        if (bossSystem != null)
            bossSystem.SetActiveBoss(_currentBoss);
    }

    private void FocusBoss()
    {
        if (_currentBoss != null)
        {
            _currentBoss.PlayRoarAnimation();
        }
    }

    private void UnFocusBoss()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isTriggered)
            return;

        if (other.CompareTag("Player"))
            TriggerBossRoom();
    }

    public Boss.Data GetBossData()
    {
        if (_currentBoss != null)
        {
            var ret = _currentBoss.Data;
            return ret;
        }

        var fallbackRet = Boss.GetBossData(_spawnBossType);
        return fallbackRet;
    }

    public BossController GetCurrentBoss()
    {
        return _currentBoss;
    }

    public void DestroyBoss()
    {
        if (_currentBoss != null)
        {
            _currentBoss.PlayDeathAnimation();
            Destroy(_currentBoss.gameObject);
            _currentBoss = null;
        }
    }

    public void ResetBossRoom()
    {
        _isTriggered = false;
        DestroyBoss();
    }
}