using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static Define;


public class GameScene : BaseScene
{
    public bool m_isStateInit = false;

    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Game;
    }

    public void Start()
    {
        // ?? 뭐지 왜 0으로 시작하냐
        Time.timeScale = 1f;

        // Temp
        Managers.Game.m_PlaySlotId = 0;
    }

    public override void Clear()
    {
        
    }
}
