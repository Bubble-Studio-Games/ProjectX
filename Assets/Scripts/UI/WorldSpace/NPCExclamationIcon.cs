using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// NPC 상호작용 아이콘 - EventTrigger 클릭 감지
/// </summary>
public class NPCExclamationIcon : UI_Base
{
    [Header("Icon Settings")]
    [SerializeField] private float _showDistance = 10f; 
    [SerializeField] private float _upDownHeight = 0.5f; 
    [SerializeField] private float _upDownSpeed = 2f; 

    private Canvas _canvas;
    private Image _iconImage;
    private Transform _playerCamera;
    private Transform _playerTransform;
    private NPC _owner;
    private Vector3 _originalPosition;

    private void Awake()
    {
        _iconImage = GetComponentInChildren<Image>();
        _playerCamera = Camera.main?.transform;
        
        var eventTrigger = _iconImage.gameObject.GetOrAddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener((data) => OnIconClicked());
        eventTrigger.triggers.Add(entry);
    }

    public void Init(NPC npc)
    {
        if (npc == null)
        {
            Debug.LogError($"{name}: NPC가 없습니다.");
            return;
        }

        _canvas = this.gameObject.GetOrAddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _playerTransform = FindAnyObjectByType<ControllableObject>()?.transform;
        _owner = npc;
    }

    private void Start()
    {
        _originalPosition = transform.localPosition;
        SetVisible(false);
    }

    private void Update()
    {
        if (_playerTransform == null || _owner == null)
            return;

        // 플레이어와의 거리 계산, 거리에 따른 표시
        float distance = Vector3.Distance(transform.position, _playerTransform.position);
        bool shouldShow = distance <= _showDistance;
        SetVisible(shouldShow);

        if (shouldShow)
        {
            if (_playerCamera != null)
                transform.rotation = Quaternion.LookRotation(transform.position - _playerCamera.position);

            AnimateUpDown();
        }
    }

    /// <summary>
    /// 아이콘 표시/숨김 설정 - 상태 변경
    /// </summary>
    public void SetVisible(bool visible)
    {
        this.gameObject.SetActive(visible);
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

    public void OnIconClicked()
    {
        Debug.Log($"NPCExclamationIcon: {_owner.name} 클릭됨");
        var dialogueUI = Managers.UI.ShowPopupUI<DialogueUI>();
        dialogueUI.StartDialogue(_owner);
    }
}
