using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class TestScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Test;
    }

    public override void Clear()
    {
        base.Clear();
    }

    protected override InputActionMap GetRequiredActionMap() => InputActionMap.Lobby;
}