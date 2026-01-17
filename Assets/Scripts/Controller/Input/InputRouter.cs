#define USE_NEW_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;


[DefaultExecutionOrder(-3)]
[DisallowMultipleComponent]
[EditorShowInfo(
@"
역할:
- Unity InputSystem(PlayerInputActions)으로부터 ""물리 입력""을 수신한다.
- 입력을 게임 로직으로 직접 전달하지 않는다.
- 모든 입력을 E_InputEvent 기준으로 정규화하여:
    1) EventMap을 통해 ""순간 이벤트""를 발행하고
    2) 내부 상태(HashSet<E_InputEvent>)로 ""현재 입력 상태""를 관리한다.

제공 인터페이스:
- ICameraInput : 카메라 이동용 축 입력 제공
- IInputQuery  : 현재 입력 상태 조회(Read-only)
- IRawInputActions : PlayerInputActions 원본에 접근 (InputBindings 등 인프라 전용)

주의:
- InputRouter는 ""입력 발행자""일 뿐, 입력 소비자가 아니다.
- Mouse 클릭, 선택, 커서 변경 등의 실제 동작은 InputBindings 또는 다른 시스템에서 처리한다.
"
)]
public class InputRouter : 
    MonoBehaviour, 
    ICameraInput,
    IInputQuery,
    IRawInputActions
{
    // New Input System 액션 자산
    private PlayerInputActions _actions;
    public PlayerInputActions Actions => _actions;

    // 현재 활성 상태들 (Shift, RightHold 등)
    private readonly HashSet<E_InputEvent> _activeStates = new();

    // 외부에서 구독/해제만 하도록 getter만 노출
    private EventMap _events = new EventMap();

    #region Unity lifecycle

    private void Awake()
    {
        // PlayerInputActions 인스턴스 생성
        _actions = new PlayerInputActions();

        // 서비스 등록
        Managers.SceneServices.Register<Define.IInputEvents>(_events);
        Managers.SceneServices.Register<IInputQuery>(this);
        Managers.SceneServices.Register<ICameraInput>(this);
        Managers.SceneServices.Register<IRawInputActions>(this);

    }

    private void OnDestroy()
    {
        // 액션 자산 정리
        _actions?.Dispose();
        _actions = null;
    }


    private void OnEnable()
    {
        // 액션 콜백 연결
        BindGameActions();
        BindDialogueActions();
    }

    private void OnDisable()
    {
        // 상태 키 초기화 (우클릭 홀드 / 스프린트 등)
        _activeStates.Clear();
    }

    #endregion

    #region Binding

    /// <summary>
    /// Game 맵의 입력을 E_InputEvent로 매핑
    /// </summary>
    private void BindGameActions()
    {
        var game = _actions.Game;

        // Esc, R (순간 이벤트)
        game.ESC.performed += _ => _events.Invoke(E_InputEvent.EscPressed);
        game.R.performed += _ => _events.Invoke(E_InputEvent.RPressed);

        // LeftClick → Down / Up
        game.LeftClick.performed += _ => _events.Invoke(E_InputEvent.MouseLeftDown);
        game.LeftClick.canceled += _ => _events.Invoke(E_InputEvent.MouseLeftUp);

        // RightClickHold → 상태 + 전이 + 순간 이벤트 (Down/Up)
        game.RightClickHold.performed += _ =>
        {
            OnInputPerformed(E_InputEvent.RightHold, E_InputEvent.RightHoldStarted);
            _events.Invoke(E_InputEvent.MouseRightDown);
        };

        game.RightClickHold.canceled += _ =>
        {
            OnInputCanceled(E_InputEvent.RightHold, E_InputEvent.RightHoldEnded);
            _events.Invoke(E_InputEvent.MouseRightUp);
        };

        // Shift → Sprint 상태 + 전이
        game.Shift.performed += _ =>
            OnInputPerformed(E_InputEvent.Sprint, E_InputEvent.SprintStarted);

        game.Shift.canceled += _ =>
            OnInputCanceled(E_InputEvent.Sprint, E_InputEvent.SprintEnded);

        // CameraMovement / Rotate / Zoom 은
        // - Movement: ICameraInput.GetCameraMoveVector()로 제공
        // - Rotate/Zoom: 필요하면 나중에 이벤트로도 뽑을 수 있음
    }

    /// <summary>
    /// Dialogue 맵의 입력을 E_InputEvent로 매핑
    /// </summary>
    private void BindDialogueActions()
    {
        var dialogue = _actions.Dialogue;

        // Submit / Cancel
        dialogue.Submit.performed += _ => _events.Invoke(E_InputEvent.DialogueSubmit);
        dialogue.ESC.performed += _ => _events.Invoke(E_InputEvent.DialogueCancel);
    }

    #endregion

    #region 상태/이벤트 헬퍼

    private void OnInputPerformed(E_InputEvent stateKey, E_InputEvent startedEvent)
    {
        if (_activeStates.Contains(stateKey))
            return;

        _activeStates.Add(stateKey);
        _events.Invoke(startedEvent);
    }

    private void OnInputCanceled(E_InputEvent stateKey, E_InputEvent endedEvent)
    {
        if (!_activeStates.Contains(stateKey))
            return;

        _activeStates.Remove(stateKey);
        _events.Invoke(endedEvent);
    }

    #endregion

    #region ICameraInput / IInputQuery

    public bool IsActive(E_InputEvent evt)
        => _activeStates.Contains(evt);

    public Vector2 GetCameraMoveVector()
    {
        if (_actions == null)
            return Vector2.zero;

        // Game 맵의 CameraMovement 축 값을 그대로 사용
        return _actions.Game.CameraMovement.ReadValue<Vector2>();
    }

    #endregion

    #region EventMap (내부 버스)

    /// <summary>
    /// InputRouter 내부에 내장된 단순 이벤트 버스.
    /// E_InputEvent → Action 멀티캐스트.
    /// </summary>
    public sealed class EventMap : Define.IInputEvents
    {
        private readonly Dictionary<E_InputEvent, Action> _events = new();


        public void Subscribe(E_InputEvent type, Action handler)
        {
            if (handler == null) return;

            if (_events.TryGetValue(type, out var cur))
                _events[type] = cur + handler;
            else
                _events[type] = handler;
        }

        public void Unsubscribe(E_InputEvent type, Action handler)
        {
            if (handler == null) return;

            if (_events.TryGetValue(type, out var cur))
            {
                cur -= handler;
                if (cur == null) _events.Remove(type);
                else _events[type] = cur;
            }
        }

        public void Invoke(E_InputEvent type)
        {
            if (_events.TryGetValue(type, out var action))
                action?.Invoke();
        }

        public void ClearAll() => _events.Clear();
    }

    #endregion
}
