using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CampScene : BaseScene
{
    public Button tempButton;

    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Camp;


        tempButton.onClick.AddListener(async () =>
        {
            await Managers.Save.SaveAllData();
            Managers.Scene.LoadScene(Define.Scene.Dungeon);
        });
    }

    protected override void Start()
    {
        base.Start();

        // 데이거 긁어오기
        var data = Managers.Load.GetContinueSaveData();
    }

    public override void Clear()
    {
        
    }
}
