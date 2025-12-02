#define USE_NEW_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static Define;

// 입력만 관리함
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public PlayerInputActions playerInputActions;

    public bool mouse_R_Hold;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one InputManager! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        playerInputActions = new PlayerInputActions();
        playerInputActions.Camera.Enable();
        playerInputActions.Mouse.Enable();
        playerInputActions.KeyBoard.Enable();


        playerInputActions.KeyBoard.ESC.performed += i => Handle_ESC_Input();
        playerInputActions.KeyBoard.R.performed += i => Handle_R_Input();

        // Mouse
        playerInputActions.Mouse.RightClickHold.performed += i => Handle_Mouse_Right_Input();
        playerInputActions.Mouse.RightClickHold.canceled += i => mouse_R_Hold = false;

        playerInputActions.Mouse.LeftClick.performed += i => Handle_Mouse_Left_Input();
        playerInputActions.Mouse.LeftClick.canceled += i => MouseWorld.Instance.MouseUp();

    }

    public Vector2 GetCameraMoveVector()
    {
#if USE_NEW_INPUT_SYSTEM
        return playerInputActions.Camera.CameraMovement.ReadValue<Vector2>();
#else
        Vector2 inputMoveDir = new Vector2(0, 0);

        if (Input.GetKey(KeyCode.W))
        {
            inputMoveDir.y = +1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            inputMoveDir.y = -1f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            inputMoveDir.x = -1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            inputMoveDir.x = +1f;
        }

        return inputMoveDir;
#endif
    }

    #region Keyboard

    private void Handle_ESC_Input()
    {
        if(Managers.Scene.CurrentScene.SceneType == Scene.Start)
        {
            (Managers.Scene.CurrentScene as StartScene).SkipIntro();
           

        }

        else if (Managers.Scene.CurrentScene.SceneType == Scene.Dungeon)
        {
            Managers.GameUI.ShowAndCloseMenuUI();
        }

        // 메인 메뉴 창 말고 한 개 더 있는가?
        if (Managers.UI._popupStack.Count > 1)
        {
            Managers.UI.ClosePopupUI();
            return;
        }
    }
    
    private void Handle_R_Input()
    {
        Managers.Selection.DeselectAll();
    }

    #endregion

    #region Mouse

    private void Handle_Mouse_Left_Input()
    {
        if (Managers.Scene.CurrentScene.SceneType == Scene.Dungeon)
        {
            MouseWorld.Instance.MouseDown();
        }

        // Scene
        if (Managers.Scene.CurrentScene.SceneType == Scene.Start)
        {
            (Managers.Scene.CurrentScene as StartScene).SkipIntro();
        }
    }

    private void Handle_Mouse_Right_Input()
    {
        mouse_R_Hold = true;
        Managers.Command.ClickSelectCommand();
    }

    #endregion
}
