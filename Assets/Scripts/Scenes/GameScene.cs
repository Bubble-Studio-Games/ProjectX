using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static Define;


public class GameScene : BaseScene
{

    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Game;
    }

    protected override void Start()
    {
        base.Start();

        // Temp
        Managers.Game.m_PlaySlotId = 0;
    }

    public override void Clear()
    {
        
    }
}
