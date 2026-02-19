using Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class CampScene : BaseScene
{
    public Button tempButton;

    CampScene() => SceneType = Define.Scene.Camp;

    protected override void Awake()
    {
        base.Awake();

        tempButton.onClick.AddListener(async () =>
        {
            tempButton.interactable = false;
            await Managers.Save.SaveAllData();
            _ = Managers.Scene.LoadSceneAsync(Define.Scene.Dungeon, () =>
            {
                Debug.Log("??");
            });
        });
    }

    protected override E_InputActionMap GetRequiredActionMap() => E_InputActionMap.Lobby;
}
