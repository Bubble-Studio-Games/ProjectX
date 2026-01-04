using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using static Table_Camera_Shake;
using System;

public class CameraController : MonoBehaviour
{
    public EventHandler<bool> OnChangeLookFloor;

    public static CameraController Instance { get; private set; }

    private CinemachineCamera m_CM;
    public Transform m_Follow;
    private Vector3 targetFollowOffset;

    public int m_CurrentLookFloor { get; private set; } = 0;

    public Camera m_UICamera;

    [Header("Main Cinemachine")]
    private CinemachineOrbitalFollow m_CMOrbitalFollow;
    private CinemachineRotationComposer m_CMRotationComposer;
    private CinemachineInputAxisController m_CMInputAxisController;
    public CinemachineImpulseListener m_CMImpulseListener;

#if UNITY_EDITOR
    [Header("Vertical Movement / 에디터 테스트 전용")]
    [SerializeField] private bool _enableVerticalMovement = true;
    [SerializeField] private float _verticalMoveSpeed = 10f;
    [SerializeField] private float _minHeight = 0f;
    [SerializeField] private float _maxHeight = 50f;
#endif

    private void Awake()
    {
        Instance = this;
        m_CM =  GetComponentInChildren<CinemachineCamera>();
        m_CMOrbitalFollow =  GetComponentInChildren<CinemachineOrbitalFollow>();
        m_CMRotationComposer =  GetComponentInChildren<CinemachineRotationComposer>();
        m_CMInputAxisController =  GetComponentInChildren<CinemachineInputAxisController>();
        m_CMImpulseListener =  GetComponentInChildren<CinemachineImpulseListener>();
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnActionMapChanged += OnActionMapChanged;
            // 현재 ActionMap 상태에 맞춰 초기화
            OnActionMapChanged(InputManager.Instance.CurrentActionMapGroup);
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnActionMapChanged -= OnActionMapChanged;
        }
    }

    private void Start()
    {
        targetFollowOffset = m_CM.Target.TrackingTarget.transform.position;
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
        if (InputManager.Instance == null) return;

        Vector2 inputMoveDir = InputManager.Instance.GetCameraMoveVector();

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
        bool isGamePlay = newActionMap == "GamePlay";

        // GamePlay가 아니면 모든 Cinemachine 입력 비활성화
        if (isGamePlay == false)
        {
            DisableAllCMControllers();
        }
    }

    /// <summary>
    /// 마우스 우클릭 시에만 작동하게 - GamePlay ActionMap일 때만
    /// </summary>
    private void HandleEnableCMController()
    {
        if (InputManager.Instance == null)
            return;

        // GamePlay ActionMap이 아니면 비활성화
        if (InputManager.Instance.CurrentActionMapGroup != "GamePlay")
        {
            DisableAllCMControllers();
            return;
        }

        // GamePlay ActionMap + 우클릭 홀드 시에만 활성화
        bool enableControllers = InputManager.Instance.mouse_R_Hold;

        if (enableControllers)
        {
            m_CMInputAxisController.Controllers[0].Enabled = true; // X축 회전
            m_CMInputAxisController.Controllers[1].Enabled = true; // Y축 회전
        }
        else
        {
            m_CMInputAxisController.Controllers[0].Enabled = false; // X축 회전
            m_CMInputAxisController.Controllers[1].Enabled = false; // Y축 회전
        }

        // Zoom은 항상 비활성화 (ActionMap과 독립적으로 동작하지 않도록)
        if (m_CMInputAxisController.Controllers.Count > 2)
        {
            m_CMInputAxisController.Controllers[2].Enabled = false;
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

}