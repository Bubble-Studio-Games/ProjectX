using static Define;
using System;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour,
    ICameraRig, ICameraInfoProvider, ICameraShakeSettings
{
    private CameraRigState _state;

    private ICameraInput _cameraInput;
    private IInputQuery _input;
    private IInputActionMapController _actionMaps;

    private CinemachineInputAxisController _axisController;
    private CinemachineImpulseListener _impulseListener;

    public Transform m_Follow;

    private void Awake()
    {
        _state = new CameraRigState();

        Managers.SceneServices.Register<ICameraRig>(this);
        Managers.SceneServices.Register<ICameraInfoProvider>(this);
        Managers.SceneServices.Register<ICameraShakeSettings>(this);

        _axisController = GetComponentInChildren<CinemachineInputAxisController>();
        _impulseListener = GetComponentInChildren<CinemachineImpulseListener>();

        _actionMaps = Managers.SceneServices.InputActionMapController;
    }

    private void Start()
    {
        _cameraInput = Managers.SceneServices.CameraInput;
        _input = Managers.SceneServices.InputQuery;

        OnActionMapChanged(_actionMaps.CurrentActionMapType);
    }

    private void OnEnable()
    {
        _actionMaps.OnActionMapChanged += _ => OnActionMapChanged(_actionMaps.CurrentActionMapType);
    }

    private void OnDisable()
    {
        _actionMaps.OnActionMapChanged -= _ => OnActionMapChanged(_actionMaps.CurrentActionMapType);
    }

    private void Update()
    {
        _state.Tick(_input.IsRightClick);

        ApplyRotationState();
        HandleMovement();
    }

    private void OnActionMapChanged(E_InputActionMap? map)
    {
        _state.OnActionMapChanged(map);
        ApplyRotationState();
    }

    private void ApplyRotationState()
    {
        bool enabled = _state.IsRotationEnabled;

        if (_axisController == null) return;

        _axisController.Controllers[0].Enabled = enabled;
        _axisController.Controllers[1].Enabled = enabled;
    }

    private void HandleMovement()
    {
        Vector2 input = _cameraInput.GetCameraMoveVector();
        if (input == Vector2.zero) return;

        // "내가 바라보는 방향" = 카메라의 수평(yaw) 기준
        Transform camTr = Camera.main != null ? Camera.main.transform : transform;

        Vector3 forward = camTr.forward;
        Vector3 right = camTr.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * input.y + right * input.x;
        m_Follow.position += move * 10f * Time.deltaTime;
    }


    // ===== 인터페이스 =====

    public Vector3 Position => m_Follow.position;
    public Quaternion Rotation => m_Follow.rotation;

    public void SetPositionAndRotation(Vector3 pos, Quaternion rot)
        => m_Follow.SetPositionAndRotation(pos, rot);

    public float GetCameraHeight() => m_Follow.position.y;
    public int CurrentLookFloor => 0;

    public void SetImpulseReactionDuration(float duration)
    {
        if (_impulseListener != null)
            _impulseListener.ReactionSettings.Duration = duration;
    }

    event Action<int> ICameraRig.OnChangeLookFloor { add { } remove { } }
}
