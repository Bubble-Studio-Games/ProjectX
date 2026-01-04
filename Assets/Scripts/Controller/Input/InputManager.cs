using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static Define;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public PlayerInputActions InputActions { get; private set; }

    public bool mouse_R_Hold;

    private Stack<string> _actionMapStack = new();
    private bool _isGameInputSubscribed = false;
    public string CurrentActionMapGroup => _actionMapStack.Count > 0 ? _actionMapStack.Peek() : "None";
    public Define.InputActionMap? CurrentActionMapType
    {
        get
        {
            if (_actionMapStack.Count == 0)
                return null;

            var currentName = _actionMapStack.Peek();
            return currentName switch
            {
                "Lobby" => Define.InputActionMap.Lobby,
                "Game" => Define.InputActionMap.Game,
                "Dialogue" => Define.InputActionMap.Dialogue,
                "Tutorial" => Define.InputActionMap.Tutorial,
                _ => null
            };
        }
    }

    /// <summary>
    /// ActionMap 변경 시 발생하는 이벤트 - 새로운 ActionMap 이름 전달
    /// </summary>
    public event Action<string> OnActionMapChanged;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one InputManager! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InputActions = new PlayerInputActions();

        // 기본 액션맵 제거 - 각 씬에서 필요한 액션맵을 직접 활성화
    }

    private void OnDestroy()
    {
        if (InputActions == null)
            return;

        InputActions.Dispose();
    }

    #region ActionMap Stack 시스템

    /// <summary>
    /// 열거형 → 문자열 변환
    /// </summary>
    private string ActionMapTypeToString(Define.InputActionMap mapType)
    {
        var ret = mapType switch
        {
            Define.InputActionMap.Lobby => "Lobby",
            Define.InputActionMap.Game => "Game",
            Define.InputActionMap.Dialogue => "Dialogue",
            Define.InputActionMap.Tutorial => "Tutorial",
            _ => throw new ArgumentException($"지원하지 않는 ActionMapType: {mapType}")
        };
        return ret;
    }

    /// <summary>
    /// ActionMap 그룹 추가 - 열거형 기반 타입 안전 버전
    /// </summary>
    public void PushActionMapGroup(Define.InputActionMap mapType)
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
        switch (groupName)
        {
            case "Lobby":
                InputActions.Lobby.Enable();
                break;

            case "Game":
                SubscribeGameInput();
                InputActions.Game.Enable();
                break;

            case "Dialogue":
                InputActions.Dialogue.Enable();
                break;

            case "Tutorial":
                InputActions.Tutorial.Enable();
                break;
        }
    }

    private void DisableActionMapGroup(string groupName)
    {
        switch (groupName)
        {
            case "Lobby":
                InputActions.Lobby.Disable();
                break;

            case "Game":
                UnsubscribeGameInput();
                InputActions.Game.Disable();
                break;

            case "Dialogue":
                InputActions.Dialogue.Disable();
                break;

            case "Tutorial":
                InputActions.Tutorial.Disable();
                break;
        }
    }

    #region Game ActionMap 구독 관리

    private void SubscribeGameInput()
    {
        if (_isGameInputSubscribed == true)
        {
            return;
        }
        _isGameInputSubscribed = true;

        InputActions.Game.ESC.performed += Handle_ESC_Input;
        InputActions.Game.R.performed += Handle_R_Input;
        InputActions.Game.RightClickHold.performed += Handle_Mouse_Right_Input;
        InputActions.Game.RightClickHold.canceled += Handle_Mouse_Right_Canceled;
        InputActions.Game.LeftClick.performed += Handle_Mouse_Left_Input;
        InputActions.Game.LeftClick.canceled += Handle_Mouse_Left_Canceled;
    }

    private void UnsubscribeGameInput()
    {
        if (_isGameInputSubscribed == false)
        {
            return;
        }
        _isGameInputSubscribed = false;

        InputActions.Game.ESC.performed -= Handle_ESC_Input;
        InputActions.Game.R.performed -= Handle_R_Input;
        InputActions.Game.RightClickHold.performed -= Handle_Mouse_Right_Input;
        InputActions.Game.RightClickHold.canceled -= Handle_Mouse_Right_Canceled;
        InputActions.Game.LeftClick.performed -= Handle_Mouse_Left_Input;
        InputActions.Game.LeftClick.canceled -= Handle_Mouse_Left_Canceled;
    }

    #endregion

    #endregion

    #region Camera

    public Vector2 GetCameraMoveVector()
    {
        var ret = InputActions.Game.CameraMovement.ReadValue<Vector2>();
        return ret;
    }

    #endregion

    #region Keyboard Handlers

    private void Handle_ESC_Input(InputAction.CallbackContext context)
    {
        if (Managers.Scene.CurrentScene == null)
        {
            return;
        }

        if (Managers.Scene.CurrentScene.SceneType == Scene.Start)
        {
            (Managers.Scene.CurrentScene as StartScene)?.SkipIntro();
        }
        else if (Managers.Scene.CurrentScene.SceneType == Scene.Dungeon)
        {
            if (Managers.UI._popupStack.Count <= 0)
            {
                var menuUI = Managers.UI.ShowPopupUI<MenuUI>();
                menuUI.SetUp(MenuUI.MenuContext.InGamePaused);
                return;
            }
            else if (Managers.UI._popupStack.Count > 0 && Managers.UI.TryGetUIBase<MenuUI>(out var menuUI) == false)
            {
                Managers.UI.ClosePopupUI();
                return;
            }
            else if (Managers.UI._popupStack.Count > 0 && Managers.UI.TryGetUIBase<MenuUI>(out var menuUI2))
            {
                Managers.UI.ClosePopupUI();
                return;
            }
        }
    }

    private void Handle_R_Input(InputAction.CallbackContext context)
    {
        Managers.Selection.DeselectAll();
    }

    #endregion

    #region Mouse Handlers

    private void Handle_Mouse_Left_Input(InputAction.CallbackContext context)
    {
        if (Managers.Scene.CurrentScene == null)
        {
            return;
        }

        if (Managers.Scene.CurrentScene.SceneType == Scene.Dungeon)
        {
            MouseWorld.Instance?.MouseDown();
        }

        if (Managers.Scene.CurrentScene.SceneType == Scene.Start)
        {
            (Managers.Scene.CurrentScene as StartScene)?.SkipIntro();
        }
    }

    private void Handle_Mouse_Left_Canceled(InputAction.CallbackContext context)
    {
        MouseWorld.Instance?.MouseUp();
    }

    private void Handle_Mouse_Right_Input(InputAction.CallbackContext context)
    {
        mouse_R_Hold = true;
        Managers.Command.ClickSelectCommand();
    }

    private void Handle_Mouse_Right_Canceled(InputAction.CallbackContext context)
    {
        mouse_R_Hold = false;
    }

    #endregion
}
