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
            _ = Managers.Scene.LoadSceneAsync
            (
                Define.Scene.Camp,
                Define.Scene.LMGameScene,
                () => Debug.Log($"씬 전환 완료 {Define.Scene.LMGameScene}")
            );
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
