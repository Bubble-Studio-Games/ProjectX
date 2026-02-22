using static Define;

public class CameraRigState
{
    public bool IsInputEnabled { get; private set; }
    public bool IsRotationEnabled { get; private set; }

    public void OnActionMapChanged(E_InputActionMap? map)
    {
        IsInputEnabled = map.HasValue && map.Value == E_InputActionMap.Game;
        IsRotationEnabled = false;
    }

    public void Tick(bool isRightClick)
    {
        if (!IsInputEnabled)
        {
            IsRotationEnabled = false;
            return;
        }

        IsRotationEnabled = isRightClick;
    }
}
