using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class CampScene : BaseScene
{
    public Button tempButton;

    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Camp;


        tempButton.onClick.AddListener(async () =>
        {
            tempButton.interactable = false;
            await Managers.Save.SaveAllData();
            _ = Managers.Scene.LoadSceneAsync(Define.Scene.Dungeon, () =>
            {
                Debug.Log("캠프씬에서 던전씬으로 이동 완료");
            });
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
        base.Clear();
    }

    protected override E_InputActionMap GetRequiredActionMap() => E_InputActionMap.Lobby;
}
