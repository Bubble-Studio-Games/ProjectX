using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using Unity.Cinemachine;
using Data;
using UnityEngine.Timeline;

[RequireComponent(typeof(PlayableDirector), typeof(SignalReceiver))]
public class BossCutSceneController : MonoBehaviour
{
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private SignalReceiver _signalReceiver;

    [Header("카메라 앵커")]
    [SerializeField] private Transform _cameraOrbitAnchor;

    [Header("컷신 카메라")]
    [SerializeField] private CinemachineCamera _cutSceneCamera;
    [SerializeField] private CinemachineCamera _focusCamera;

    private bool _isCutscenePlaying = false;
    public event Action OnSpawn;
    public event Action OnFocus;
    public event Action OnUnfocus;
    public event Action OnStart;
    public event Action OnComplete;

    public event Action<Vector3, Quaternion> OnBossSpawnRequested;

    private void OnValidate()
    {
        if (_director == null)
            _director = GetComponent<PlayableDirector>();

        if (_signalReceiver == null)
            _signalReceiver = GetComponent<SignalReceiver>();

        if (_cameraOrbitAnchor == null)
            _cameraOrbitAnchor = this.transform.EnsureChild("OrbitRoot");
    }

    private void Clear()
    {
        OnSpawn = null;
        OnFocus = null;
        OnUnfocus = null;
        OnStart = null;
        OnComplete = null;
    }

    private void OnEnable()
    {
        if (_director != null)
            _director.stopped += OnTimelineStopped;
    }

    private void OnDisable()
    {
        if (_director != null)
        {
            _director.stopped -= OnTimelineStopped;
            Clear();
        }
    }

    public void StartCutscene()
    {
        if (_isCutscenePlaying)
        {
            Debug.LogWarning("[BossCutSceneController] 이미 컷신이 재생 중");
            return;
        }

        _isCutscenePlaying = true;

        if (_director != null)
        {
            _director.Play();
            OnStart?.Invoke();
        }
    }

    public void OnUnFocusSignal()
    {
        OnUnfocus?.Invoke();
    }

    public void OnFocusSignal()
    {
        OnFocus?.Invoke();
    }

    public void OnSpawnSignal()
    {
        OnSpawn?.Invoke();
    }

    public void AttachAnchorToBoss(Transform bossTransform, Transform parentTransform)
    {
        if (_cameraOrbitAnchor == null || bossTransform == null)
            return;

        _cameraOrbitAnchor.SetParent(parentTransform);
        _cameraOrbitAnchor.position = bossTransform.position;
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        if (director != _director)
            return;

        OnComplete?.Invoke();
        CompleteCutscene();
    }

    private void CompleteCutscene()
    {
        _isCutscenePlaying = false;
        DetachCameraAnchor();
        Clear();
    }

    private void DetachCameraAnchor()
    {
        if (_cameraOrbitAnchor == null)
            return;

        _cameraOrbitAnchor.SetParent(transform);
    }

    public void StopCutscene()
    {
        if (_director != null && _director.state == PlayState.Playing)
        {
            _director.Stop();
        }

        CompleteCutscene();
    }
}
