using System;
using UnityEngine;

public class NPCInteractionUI : UI_Base
{
    private enum Images
    {
        Icon
    }

    [Header("아이콘 설정")]
    [SerializeField] private float _showDistance = 10f;
    [SerializeField] private float _upDownHeight = 0.5f;
    [SerializeField] private float _upDownSpeed = 2f;

    private Canvas _canvas;
    private Transform _playerCamera;
    private Transform _playerTransform;
    private NPCObject _owner;
    private Vector3 _originalPosition;
    private Action _onClick;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindImage(typeof(Images));
        return true;
    }

    private void OnDestroy()
    {
        Clear();
    }

    public void Clear()
    {
        _owner = null;
        _playerCamera = null;
        _playerTransform = null;
        _canvas = null;
        _onClick = null;
    }


    public void Setup(NPCObject npc, Action onClick = default)
    {
        Init();
        if (npc == null)
        {
            Debug.LogError($"{name}: NPC가 없습니다.");
            return;
        }

        _onClick = onClick;
        _owner = npc;
        _originalPosition = transform.localPosition;

        _canvas = gameObject.GetOrAddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;

        var player = FindAnyObjectByType<ControllableObject>();
        _playerTransform = player != null ? player.transform : null;

        var mainCamera = Camera.main;
        _playerCamera = mainCamera != null ? mainCamera.transform : null;

        BindEvent(GetImage((int)Images.Icon).gameObject, OnIconClicked, Define.UIEvent.Click);

        gameObject.SetActive(true);
    }

    private void OnIconClicked()
    {
        _onClick?.Invoke();
    }

    private void Update()
    {
        if (_playerTransform == null || _owner == null)
            return;

        float distance = Vector3.Distance(transform.position, _playerTransform.position);
        bool shouldShow = distance <= _showDistance;

        if (shouldShow == false)
            return;

        if (_playerCamera != null)
            transform.rotation = Quaternion.LookRotation(transform.position - _playerCamera.position);

        AnimateUpDown();
    }

    /// <summary>
    /// 위아래 움직임 애니메이션
    /// </summary>
    private void AnimateUpDown()
    {
        if (_canvas == null)
            return;

        float bobOffset = Mathf.Sin(Time.time * _upDownSpeed) * _upDownHeight;
        Vector3 newPosition = _originalPosition;
        newPosition.y += bobOffset;

        transform.localPosition = newPosition;
    }
}


