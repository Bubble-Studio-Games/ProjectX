#define USE_NEW_INPUT_SYSTEM
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{

    public static InputManager Instance { get; private set; }

    private PlayerInputActions playerInputActions;

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
        playerInputActions.Player.Enable();
        playerInputActions.Mouse.Enable();
        playerInputActions.KeyBoard.Enable();

        // Keyboard
        playerInputActions.KeyBoard.ESC.performed += i => ESC();
    }

    public Vector2 GetMouseScreenPosition()
    {
#if USE_NEW_INPUT_SYSTEM
        return Mouse.current.position.ReadValue();
#else
        return Input.mousePosition;
#endif
    }

    public bool IsMouseButtonDownThisFrame()
    {
#if USE_NEW_INPUT_SYSTEM
        return playerInputActions.Mouse.LeftClick.WasPressedThisFrame();
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    public Vector2 GetCameraMoveVector()
    {
#if USE_NEW_INPUT_SYSTEM
        return playerInputActions.Player.CameraMovement.ReadValue<Vector2>();
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

    public float GetCameraRotateAmount()
    {
#if USE_NEW_INPUT_SYSTEM
        return playerInputActions.Player.CameraRotate.ReadValue<float>();
#else
        float rotateAmount = 0f;

        if (Input.GetKey(KeyCode.Q))
        {
            rotateAmount = +1f;
        }
        if (Input.GetKey(KeyCode.E))
        {
            rotateAmount = -1f;
        }

        return rotateAmount;
#endif
    }

    public float GetCameraZoomAmount()
    {
#if USE_NEW_INPUT_SYSTEM
        return playerInputActions.Player.CameraZoom.ReadValue<float>();
#else
        float zoomAmount = 0f;

        if (Input.mouseScrollDelta.y > 0)
        {
            zoomAmount = -1f;
        }
        if (Input.mouseScrollDelta.y < 0)
        {
            zoomAmount = +1f;
        }

        return zoomAmount;
#endif
    }

    public bool GetShiftDown()
    {
#if USE_NEW_INPUT_SYSTEM
        return playerInputActions.KeyBoard.Shift.IsPressed();
#else
#endif
    }

    public bool MouseRightClickHold()
    {
#if USE_NEW_INPUT_SYSTEM
        return playerInputActions.Mouse.RightClickHold.IsPressed();
#else
#endif

    }

    public Vector2 GetMouseDelta()
    {
        return playerInputActions.Mouse.Delta.ReadValue<Vector2>();
    }

    public void ESC()
    {


        // 유닛의 액션 창이 떠 있다면 액션 창 닫기 
        // 상점 창, 미션 창 등의 팝업이 떠 있다면 닫기
        if (!Managers.Game.m_IsGamePauseing) 
        { 
            Managers.UI.ShowPopupUI<MenuUI>();
            Managers.Game.PauseGame();
        } 
        else 
        {
            // 메인 메뉴 창 말고 한 개 더 있는가?
            if (Managers.UI._popupStack.Count > 1)
            {
                Managers.UI.ClosePopupUI();
                return;
            }

            Managers.UI.ClosePopupUI<MenuUI>();
            Managers.Game.ResumeGame(); 

        }
    }
}
