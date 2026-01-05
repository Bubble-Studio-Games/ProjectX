using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static Define;
using E_InputActionMap = Define.E_InputActionMap;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public PlayerInputActions InputActions { get; private set; }

    public bool mouse_R_Hold;

    private Stack<E_InputActionMap> _actionMapStack = new();
    private bool _isGameInputSubscribed = false;
    public E_InputActionMap? CurrentActionMap => _actionMapStack.Count > 0 ? _actionMapStack.Peek() : null;
    public event Action<E_InputActionMap?> OnActionMapChanged;

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
    }

    private void OnDestroy()
    {
        if (InputActions == null)
            return;

        InputActions.Dispose();
    }

    /// <summary>
    /// ActionMap 그룹 추가 - 이전 상태는 스택에 저장
    /// </summary>
    public void PushActionMapGroup(Define.E_InputActionMap actionMap)
    {
        // 이미 최상단에 같은 그룹이 있으면 무시
        if (_actionMapStack.Count > 0 && _actionMapStack.Peek() == actionMap)
            return;

        // 현재 활성 그룹 비활성화
        if (_actionMapStack.Count > 0)
            DisableActionMapGroup(_actionMapStack.Peek());

        _actionMapStack.Push(actionMap);
        EnableActionMapGroup(actionMap);

        OnActionMapChanged?.Invoke(actionMap);
        
#if UNITY_EDITOR
        GlobalSettings.Instance?.Scene?.SyncInputStack(_actionMapStack);
#endif
    }

    /// <summary>
    /// ActionMap 그룹 제거 - 이전 상태 복구
    /// </summary>
    public void PopActionMapGroup()
    {
        if (_actionMapStack.Count <= 0)
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
            OnActionMapChanged?.Invoke(previous);
        }
        else
        {
            OnActionMapChanged?.Invoke(null);
        }
        
#if UNITY_EDITOR
        GlobalSettings.Instance?.Scene?.SyncInputStack(_actionMapStack);
#endif
    }

    private void EnableActionMapGroup(Define.E_InputActionMap actionMap)
    {
        switch (actionMap)
        {
            case Define.E_InputActionMap.Lobby:
                InputActions.Lobby.Enable();
                break;

            case Define.E_InputActionMap.Game:
                SubGameInput();
                InputActions.Game.Enable();
                break;

            case Define.E_InputActionMap.Dialogue:
                InputActions.Dialogue.Enable();
                break;

            case Define.E_InputActionMap.Tutorial:
                InputActions.Tutorial.Enable();
                break;
        }
    }

    private void DisableActionMapGroup(Define.E_InputActionMap actionMap)
    {
        switch (actionMap)
        {
            case Define.E_InputActionMap.Lobby:
                InputActions.Lobby.Disable();
                break;

            case Define.E_InputActionMap.Game:
                UnsubGameInput();
                InputActions.Game.Disable();
                break;

            case Define.E_InputActionMap.Dialogue:
                InputActions.Dialogue.Disable();
                break;

            case Define.E_InputActionMap.Tutorial:
                InputActions.Tutorial.Disable();
                break;
        }
    }

    #region Game ActionMap 구독 관리

    private void SubGameInput()
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

    private void UnsubGameInput()
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
