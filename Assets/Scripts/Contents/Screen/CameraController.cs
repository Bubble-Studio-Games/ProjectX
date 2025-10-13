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
    [SerializeField] private Transform m_Follow;
    private Vector3 targetFollowOffset;

    public int m_CurrentLookFloor { get; private set; } = 0;

    public Camera m_UICamera;

    [Header("Main Cinemachine")]
    private CinemachineOrbitalFollow m_CMOrbitalFollow;
    private CinemachineRotationComposer m_CMRotationComposer;
    private CinemachineInputAxisController m_CMInputAxisController;
    public CinemachineImpulseListener m_CMImpulseListener;

    private void Awake()
    {
        Instance = this;
        m_CM =  GetComponentInChildren<CinemachineCamera>();
        m_CMOrbitalFollow =  GetComponentInChildren<CinemachineOrbitalFollow>();
        m_CMRotationComposer =  GetComponentInChildren<CinemachineRotationComposer>();
        m_CMInputAxisController =  GetComponentInChildren<CinemachineInputAxisController>();
        m_CMImpulseListener =  GetComponentInChildren<CinemachineImpulseListener>();
    }

    private void Start()
    {
        targetFollowOffset = m_CM.Target.TrackingTarget.transform.position;
    }

    private void Update()
    {
        HandleMovement();
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

    // 마우스 우클릭 시에만 작동하게
    private void HandleEnableCMController()
    {
        if(InputManager.Instance == null) return;

        if(InputManager.Instance.MouseRightClickHold())
        {
            m_CMInputAxisController.Controllers[0].Enabled = true; // X
            m_CMInputAxisController.Controllers[1].Enabled = true; // Y
        }
        else
        {
            m_CMInputAxisController.Controllers[0].Enabled = false; // X
            m_CMInputAxisController.Controllers[1].Enabled = false; // Y
        }
    }


    public float GetCameraHeight()
    {
        return targetFollowOffset.y;
    }

}