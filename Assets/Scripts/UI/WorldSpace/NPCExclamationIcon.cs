using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NPC 상호작용 아이콘
/// </summary>
public class NPCExclamationIcon : UI_Base
{
    [Header("Icon Settings")]
    [SerializeField] private float _showDistance = 10f; 
    [SerializeField] private float _hideDistance = 15f;
    [SerializeField] private float _bobHeight = 0.5f; 
    [SerializeField] private float _bobSpeed = 2f; 

    private Canvas _canvas;
    private Image _iconImage;
    private Transform _playerCamera;
    private Transform _playerTransform;
    private NPC _ownerNPC;
    private Vector3 _originalPosition;

    private void Awake()
    {
        _canvas = this.gameObject.GetOrAddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _iconImage = GetComponentInChildren<Image>();
        _playerCamera = Camera.main?.transform;
        _playerTransform = FindObjectOfType<ControllableObject>()?.transform;
    }

    private void Start()
    {
        _originalPosition = transform.localPosition;
        SetVisible(false);
    }

    private void Update()
    {
        if (_playerTransform == null || _ownerNPC == null)
            return;

        // 플레이어와의 거리 계산, 거리에 따른 표시
        float distance = Vector3.Distance(transform.position, _playerTransform.position);
        bool shouldShow = distance <= _showDistance && _ownerNPC.IsInteractable;
        SetVisible(shouldShow);

        if (shouldShow)
        {
            if (_playerCamera != null)
                transform.rotation = Quaternion.LookRotation(transform.position - _playerCamera.position);

            AnimateBob();
        }
    }

    /// <summary>
    /// 아이콘 표시/숨김 설정
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (_canvas != null)
            _canvas.enabled = visible;

        if (_iconImage != null)
            _iconImage.enabled = visible;
    }

    public void SetOwnerNPC(NPC npc)
    {
        _ownerNPC = npc;
    }

    /// <summary>
    /// 위아래 움직임 애니메이션
    /// </summary>
    private void AnimateBob()
    {
        if (_canvas == null)
            return;

        float bobOffset = Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
        Vector3 newPosition = _originalPosition;
        newPosition.y += bobOffset;

        transform.localPosition = newPosition;
    }

    public void OnIconClicked()
    {
        if (_ownerNPC != null && _ownerNPC.IsInteractable)
            _ownerNPC.Interact();
    }
}
