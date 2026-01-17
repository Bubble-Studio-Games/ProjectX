using Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class TestScene : BaseScene
{
    TestScene() => 
        SceneType = Define.Scene.Test;

    protected override E_InputActionMap GetRequiredActionMap() => E_InputActionMap.Lobby;
}