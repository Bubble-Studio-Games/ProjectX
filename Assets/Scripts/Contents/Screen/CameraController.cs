using UnityEngine;
using Unity.Cinemachine;
using System;
using static Define;
using System.Collections;

public class CameraController : MonoBehaviour, ICameraRig, ICameraInfoProvider, ICameraShakeSettings
{
    private ICameraInput _cameraInput;
    private IInputQuery _input;
    private IInputActionMapController _inputActionMaps;

    public Action<int> OnChangeLookFloor;

    private CinemachineCamera m_CM;
    public Transform m_Follow;
    private Vector3 targetFollowOffset;

    public Camera m_UICamera;

    [Header("Main Cinemachine")]
    private CinemachineRotationComposer m_CMRotationComposer;
    private CinemachineInputAxisController m_CMInputAxisController;
    private CinemachineImpulseListener m_CMImpulseListener;
    private CinemachineOrbitalFollow m_CMOrbitalFollow;
    private event Action<int> _onChangeLookFloor;

    [Header("Cutscene")]
    [SerializeField] private CinemachineCamera m_AnchorCamera;
    private bool _isCutsceneMode = false;
    private bool[] _savedControllerStates;
    private int _savedMainCameraPriority;
    private bool _savedOrbitalFollowState;


#if UNITY_EDITOR
    [Header("Vertical Movement / 에디터 테스트 전용")]
    [SerializeField] private bool _enableVerticalMovement = true;
    [SerializeField] private float _verticalMoveSpeed = 10f;
    [SerializeField] private float _minHeight = 0f;
    [SerializeField] private float _maxHeight = 50f;
#endif

    event Action<int> ICameraRig.OnChangeLookFloor
    {
        add { _onChangeLookFloor += value; }
        remove { _onChangeLookFloor -= value; }
    }

    private void ChangeFloor(int floor)
    {
        _onChangeLookFloor?.Invoke(floor);
    }

    private void Awake()
    {
        Managers.SceneServices.Register<ICameraRig>(this);
        Managers.SceneServices.Register<ICameraInfoProvider>(this);
        Managers.SceneServices.Register<ICameraShakeSettings>(this);

        m_CM =  GetComponentInChildren<CinemachineCamera>();
        m_CMRotationComposer =  GetComponentInChildren<CinemachineRotationComposer>();
        m_CMInputAxisController =  GetComponentInChildren<CinemachineInputAxisController>();
        m_CMImpulseListener =  GetComponentInChildren<CinemachineImpulseListener>();
        m_CMOrbitalFollow = GetComponentInChildren<CinemachineOrbitalFollow>();
        _inputActionMaps = Managers.SceneServices.InputActionMapController;

    }


    private void Start()
    {
        targetFollowOffset = m_CM.Target.TrackingTarget.transform.position;

        _cameraInput = Managers.SceneServices.CameraInput;
        _input = Managers.SceneServices.InputQuery;
        OnActionMapChanged(_inputActionMaps.CurrentActionMapGroup);
    }

    private void OnEnable()
    {
        _inputActionMaps.OnActionMapChanged += OnActionMapChanged;
    }

    private void OnDisable()
    {
        _inputActionMaps.OnActionMapChanged -= OnActionMapChanged;
    }


    private void Update()
    {
        HandleMovement();
#if UNITY_EDITOR
        if (_enableVerticalMovement)
            HandleVerticalMovement();
#endif
        HandleEnableCMController();
    }

    private void HandleMovement()
    {
        Vector2 inputMoveDir = _cameraInput.GetCameraMoveVector();

        float moveSpeed = 10f;

        // Cinemachine 카메라의 방향 기준
        Vector3 forward = m_CM.transform.forward;
        Vector3 right = m_CM.transform.right;

        // 수직 방향은 제거 (y축 이동 방지)
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // 입력 벡터 기반으로 이동
        Vector3 moveVector = forward * inputMoveDir.y + right * inputMoveDir.x;
        m_Follow.position += moveVector * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// ActionMap 변경 시 호출되는 콜백
    /// </summary>
    private void OnActionMapChanged(string newActionMap)
    {
        var type = _inputActionMaps.CurrentActionMapType;
        bool isGame = type.HasValue && type.Value == E_InputActionMap.Game;

        if (!isGame)
        {
            // Game이 아니면 모든 Cinemachine 입력 비활성화
            DisableAllCMControllers();
        }
    }

    /// <summary>
    /// 마우스 우클릭 시에만 작동하게 - Game ActionMap일 때만
    /// </summary>
    private void HandleEnableCMController()
    {
        var type = _inputActionMaps.CurrentActionMapType;
        bool isGame = type.HasValue && type.Value == E_InputActionMap.Game;

        if (!isGame)
        {
            DisableAllCMControllers();
            return;
        }

        // Game + RightHold 상태일 때만 카메라 회전 허용
        bool enableControllers = _input.IsActive(E_InputEvent.RightHold);

        if (enableControllers)
        {
            m_CMInputAxisController.Controllers[0].Enabled = true; // X축 회전
            m_CMInputAxisController.Controllers[1].Enabled = true; // Y축 회전
        }
        else
        {
            m_CMInputAxisController.Controllers[0].Enabled = false;
            m_CMInputAxisController.Controllers[1].Enabled = false;
        }
    }

    /// <summary>
    /// 모든 Cinemachine 입력 컨트롤러 비활성화
    /// </summary>
    private void DisableAllCMControllers()
    {
        if (m_CMInputAxisController == null)
            return;

        for (int i = 0; i < m_CMInputAxisController.Controllers.Count; i++)
        {
            m_CMInputAxisController.Controllers[i].Enabled = false;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 수직 이동 처리 - 에디터 테스트 전용
    /// - "[" 아래로 이동
    /// - "]" 위로 이동
    /// </summary>
    private void HandleVerticalMovement()
    {
        Vector3 newPos = m_Follow.position;
        bool moved = false;

        if (Input.GetKey(KeyCode.LeftBracket))
        {
            newPos.y -= _verticalMoveSpeed * Time.deltaTime;
            moved = true;
        }

        if (Input.GetKey(KeyCode.RightBracket))
        {
            newPos.y += _verticalMoveSpeed * Time.deltaTime;
            moved = true;
        }

        if (moved)
        {
            newPos.y = Mathf.Clamp(newPos.y, _minHeight, _maxHeight);
            m_Follow.position = newPos;
        }
    }
#endif


    public float GetCameraHeight()
    {
        return targetFollowOffset.y;
    }

    public void SetPositionAndRotation(Vector3 position, Quaternion rotation) => m_Follow.transform.SetPositionAndRotation(position, rotation);

    public Vector3 Position => m_Follow.transform.position;
    public Quaternion Rotation => m_Follow.transform.rotation;

    public int CurrentLookFloor => 0;

    public void SetImpulseReactionDuration(float duration)
    {
        if (m_CMImpulseListener != null)
            m_CMImpulseListener.ReactionSettings.Duration = duration;
    }

    /// <summary>
    /// 컷씬 모드 진입 - Anchor Camera로 전환 후 카메라 입력 차단
    /// </summary>
    public void EnterCutsceneMode()
    {
        if (_isCutsceneMode == true)
            return;

        if (m_CM == null || m_AnchorCamera == null)
            return;

        _savedMainCameraPriority = m_CM.Priority;

        if (m_AnchorCamera != null)
        {
            var ret = m_CM.transform.position;
            var retRot = m_CM.transform.rotation;
            m_AnchorCamera.transform.SetPositionAndRotation(ret, retRot);
            m_AnchorCamera.Priority = 15;
        }

        if (m_CMOrbitalFollow != null)
        {
            _savedOrbitalFollowState = m_CMOrbitalFollow.enabled;
            m_CMOrbitalFollow.enabled = false;
        }

        if (m_CMInputAxisController != null)
        {
            var ret = m_CMInputAxisController.Controllers.Count;
            _savedControllerStates = new bool[ret];

            for (int i = 0; i < ret; i++)
            {
                _savedControllerStates[i] = m_CMInputAxisController.Controllers[i].Enabled;
                m_CMInputAxisController.Controllers[i].Enabled = false;
            }
        }

        m_CM.Priority = 0;

        _isCutsceneMode = true;
    }

    /// <summary>
    /// 컷씬 모드 종료 - 메인 카메라로 복귀 및 입력 복원
    /// </summary>
    public void ExitCutsceneMode()
    {
        if (_isCutsceneMode == false)
            return;

        if (m_CM == null)
            return;

        if (m_AnchorCamera != null)
            m_AnchorCamera.Priority = 15;

        StartCoroutine(DelayedMainCameraActivation());
    }

    private IEnumerator DelayedMainCameraActivation()
    {
        yield return new WaitForSeconds(0.5f);

        if (m_CM != null)
            m_CM.Priority = _savedMainCameraPriority;

        if (m_AnchorCamera != null)
            m_AnchorCamera.Priority = -1;

        if (m_CMOrbitalFollow != null)
            m_CMOrbitalFollow.enabled = _savedOrbitalFollowState;

        if (m_CMInputAxisController != null && _savedControllerStates != null)
        {
            var ret = Mathf.Min(_savedControllerStates.Length, m_CMInputAxisController.Controllers.Count);
            for (int i = 0; i < ret; i++)
            {
                m_CMInputAxisController.Controllers[i].Enabled = _savedControllerStates[i];
            }
        }

        _isCutsceneMode = false;
    }
}