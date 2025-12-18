using Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Define;


public class DungeonScene : BaseScene
{

    DungeonScene()
    {
        SceneType = Scene.Dungeon;
    }

    protected override void Start()
    {
        base.Start();

        // Sound
        Managers.Sound.Play(m_SceneMainTemaAudioclip, 1, Sound.Bgm);
    }

    public override void Clear()
    {
        
    }

    protected override void LoadSavedGame(SaveSlotData data)
    {
        base.LoadSavedGame(data);

        // ✅ 데이터가 있다는 게 확정된 상태
        Managers.Object.Clear(); // 기존 씬 배치 제거

        Managers.Load.ObjectInfoLoad(data.dungeondata.gameEntityDatas);
        Managers.Load.ObjectRestoreSaveData(data.dungeondata.gameEntityDatas);

        BuildingTypeSelectUI.Instance.RestoreSaveDatas(data.dungeondata.buildingCardDatas);
        Inventory.Instance.m_iDownJamAmount = data.dungeondata.downJam;

        CameraController.Instance.m_Follow.transform.SetPositionAndRotation(
            data.dungeondata.cameraPos, data.dungeondata.cameraRot);
    }

    protected override void LoadNewGame()
    {
        base.LoadNewGame();

        var list = Inventory.Instance.GetEnableCardList();

        for (int i = 0; i < 5; i++)
            BuildingTypeSelectUI.Instance.AddCard(list[Random.Range(0, list.Count)], default, true);

        if (DungeonCore.instance != null)
        {
            CameraController.Instance.m_Follow.transform.SetPositionAndRotation(
                DungeonCore.instance.transform.position,
                DungeonCore.instance.transform.rotation);
        }
    }

}
