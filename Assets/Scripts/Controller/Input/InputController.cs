using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public enum E_MouseClickType
{
    Left,
    Right,
    Wheel,
}

public partial class InputController : 
    MonoBehaviour, 
    IInputActionMapController,
    ICameraInput,
    IInputQuery
{
    #region Field

    private PlayerInputActions _actions;

    [Header("Mouse")]
    private Coroutine _delayLeftDown;
    private Coroutine _delayLeftUp;

    [Header("Action Map")]
    private Stack<string> _actionMapStack = new();
    public string CurrentActionMapGroup => _actionMapStack.Count > 0 ? _actionMapStack.Peek() : "None";
    public Define.E_InputActionMap? CurrentActionMapType
    {
        get
        {
            if (_actionMapStack.Count == 0)
                return null;

            var currentName = _actionMapStack.Peek();
            return currentName switch
            {
                "Lobby" => Define.E_InputActionMap.Lobby,
                "Game" => Define.E_InputActionMap.Game,
                "Dialogue" => Define.E_InputActionMap.Dialogue,
                "Tutorial" => Define.E_InputActionMap.Tutorial,
                _ => null
            };
        }
    }

    /// <summary>
    /// ActionMap 변경 시 발생하는 이벤트 - 새로운 ActionMap 이름 전달
    /// </summary>
    public event Action<string> OnActionMapChanged;

    [SerializeField] private bool _isMouseRightHold;
    public bool IsRightClick => _isMouseRightHold;

    [SerializeField] private bool _isLeftShiftHold;
    #endregion

    private void Awake()
    {
        _actions = new PlayerInputActions();

        // 서비스 등록
        Managers.SceneServices.Register<IInputQuery>(this);
        Managers.SceneServices.Register<ICameraInput>(this);
        Managers.SceneServices.Register<IInputActionMapController>(this);
    }

    private void Start()
    {
        // 액션 콜백 연결
        BindGameActions();
        BindDialogueActions();

        // Mouse
        SelectionBox.gameObject.SetActive(false);

        // Cursor - GlobalSettings에서 활성화 여부 확인
        if (IsCursorEnabled())
            Cursor.SetCursor(DefaultCursor, hotspot, CursorMode.Auto);
    }

    private void LateUpdate()
    {
        // 건물 배치 중이며, 마우스의 위치가 바뀌었다면
        if (Managers.Building.IsSetuping && UpdateGridPosition())
        {
            Managers.Grid.RequestVisualRefresh();
        }

        if (_isMouseRightHold)
        {
            GetCameraMoveVector();
        }
    }

    private void OnDestroy()
    {
        _actions?.Dispose();
        _actions = null;
    }

    private void BindGameActions()
    {
        var game = _actions.Game;

        // KeyBoard
        game.ESC.performed += i => ESC();
        game.R.performed += _ => OnR();

        // Mouse
        game.LeftClick.performed += _ => OnMouseLeftDown();
        game.LeftClick.canceled += _ => OnMouseLeftUp();

        game.RightClickHold.performed += _ => OnRightHoldStarted();
        game.RightClickHold.performed += _ => _isMouseRightHold = true;
        game.RightClickHold.canceled += _ => _isMouseRightHold = false;
    }

    #region Game

    private void ESC()
    {
        // 기존 InputManager.Handle_ESC_Input 내용 여기로 이동
        if (Managers.Scene.CurrentScene.SceneType == Scene.Start)
            (Managers.Scene.CurrentScene as StartScene).SkipIntro();
        else if (Managers.Scene.CurrentScene.SceneType == Scene.Dungeon)
            Managers.GameUI.ShowAndCloseMenuUI();

        if (Managers.UI._popupStack.Count > 1)
            Managers.UI.ClosePopupUI();
    }

    private void OnR()
    {
        if (Managers.Building.IsSetuping)
        {
            Managers.Building.Reqeust(new BuildContext(BuildRequestType.Rotate, Managers.Building.PlacingObject));
        }
        else
        {
            Managers.Selection.DeselectAll();
        }

    }

    private void OnRightHoldStarted()
    {
        Managers.Command.ClickSelectCommand();
    }

    private void OnMouseLeftDown()
    {
        if (_delayLeftDown != null) StopCoroutine(_delayLeftDown);
        _delayLeftDown = StartCoroutine(DelayOneFrame(() => MouseDown(E_MouseClickType.Left)));
    }

    private void OnMouseLeftUp()
    {
        if (_delayLeftUp != null) StopCoroutine(_delayLeftUp);
        _delayLeftUp = StartCoroutine(DelayOneFrame(() => MouseUp(E_MouseClickType.Left)));
    }

    private IEnumerator DelayOneFrame(System.Action action)
    {
        yield return null;          // ✅ 딱 1프레임 뒤
        action?.Invoke();
    }

    #endregion

    /// <summary>
    /// Dialogue 맵의 입력을 E_InputEvent로 매핑
    /// </summary>
    private void BindDialogueActions()
    {
        var dialogue = _actions.Dialogue;

        // Submit / Cancel
        dialogue.Submit.performed += _ => Managers.Dialogue.OnDialogueSubmit();
        dialogue.ESC.canceled += _ => Managers.Dialogue.OnDialogueESC();
    }

    #region ICameraInput / IInputQuery

    public Vector2 GetCameraMoveVector() => 
        _actions.Game.CameraMovement.ReadValue<Vector2>();

    #endregion


    #region ActionMap Stack 시스템

    /// <summary>
    /// 열거형 → 문자열 변환
    /// </summary>
    private string ActionMapTypeToString(Define.E_InputActionMap mapType)
    {
        var ret = mapType switch
        {
            Define.E_InputActionMap.Lobby => "Lobby",
            Define.E_InputActionMap.Game => "Game",
            Define.E_InputActionMap.Dialogue => "Dialogue",
            Define.E_InputActionMap.Tutorial => "Tutorial",
            _ => throw new ArgumentException($"지원하지 않는 ActionMapType: {mapType}")
        };
        return ret;
    }

    /// <summary>
    /// ActionMap 그룹 추가 - 열거형 기반 타입 안전 버전
    /// </summary>
    public void PushActionMapGroup(Define.E_InputActionMap mapType)
    {
        var ret = ActionMapTypeToString(mapType);
        PushActionMapGroup(ret);
    }

    /// <summary>
    /// ActionMap 그룹 추가 - 이전 상태는 스택에 저장
    /// </summary>
    public void PushActionMapGroup(string groupName)
    {
        // 이미 최상단에 같은 그룹이 있으면 무시
        if (_actionMapStack.Count > 0 && _actionMapStack.Peek() == groupName)
        {
            Debug.LogWarning($"[입력] {groupName}이 이미 활성화되어 있습니다.");
            return;
        }

        // 현재 활성 그룹 비활성화
        if (_actionMapStack.Count > 0)
        {
            DisableActionMapGroup(_actionMapStack.Peek());
        }

        _actionMapStack.Push(groupName);
        EnableActionMapGroup(groupName);

        Debug.Log($"[입력] 추가됨: {groupName} | 스택: [{string.Join(" → ", _actionMapStack)}]");

        // ActionMap 변경 이벤트 발생
        OnActionMapChanged?.Invoke(groupName);
    }

    /// <summary>
    /// ActionMap 그룹 제거 - 이전 상태 복구
    /// </summary>
    public void PopActionMapGroup()
    {
        if (_actionMapStack.Count == 0)
        {
            Debug.LogWarning("[입력] 스택이 비어있습니다! Pop할 수 없습니다.");
            return;
        }

        var current = _actionMapStack.Pop();
        DisableActionMapGroup(current);

        // 이전 상태 복구
        if (_actionMapStack.Count > 0)
        {
            var previous = _actionMapStack.Peek();
            EnableActionMapGroup(previous);
            Debug.Log($"[입력] 제거됨: {current} | 복구됨: {previous}");

            // ActionMap 변경 이벤트 발생
            OnActionMapChanged?.Invoke(previous);
        }
        else
        {
            Debug.LogWarning("[입력] Pop 후 스택이 비어있습니다!");

            // ActionMap이 없는 상태로 변경
            OnActionMapChanged?.Invoke("None");
        }
    }

    private void EnableActionMapGroup(string groupName)
    {
        var actions = _actions;

        switch (groupName)
        {
            case "Game":
                // 게임 플레이 시: Game ON, Dialogue OFF
                actions.Game.Enable();
                actions.Dialogue.Disable();
                break;

            case "Dialogue":
                // 대화 중: Dialogue ON, Game OFF
                actions.Game.Disable();
                actions.Dialogue.Enable();
                break;

            case "Lobby":
                // 로비: 일단 둘 다 OFF (필요하면 나중에 Lobby 맵 추가)
                actions.Game.Disable();
                actions.Dialogue.Disable();
                break;

            case "Tutorial":
                // 튜토리얼: 우선 Game만 ON (튜토리얼 맵 따로 만들면 여기서 처리)
                actions.Game.Enable();
                actions.Dialogue.Disable();
                break;

            default:
                // 알 수 없는 그룹: 모두 OFF
                actions.Game.Disable();
                actions.Dialogue.Disable();
                break;
        }
    }

    private void DisableActionMapGroup(string groupName)
    {
        var actions = _actions;

        switch (groupName)
        {
            case "Game":
                actions.Game.Disable();
                break;

            case "Dialogue":
                actions.Dialogue.Disable();
                break;

            case "Lobby":
            case "Tutorial":
                // 지금은 별도 맵이 없으니, 굳이 할 건 없음.
                break;

            default:
                break;
        }
    }


    #endregion
}
