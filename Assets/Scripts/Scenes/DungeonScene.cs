using Data;
using System.Linq;
using UnityEngine;
using static Define;

public class DungeonScene : BaseScene
{
    IBuildingCardUI _buildingCardUI;
    DungeonScene() => SceneType = Scene.Dungeon;

    protected override void Start()
    {
        _buildingCardUI = Managers.SceneServices.BuildingCardUI;

        base.Start();

        // Sound
        Managers.Sound.Play(m_SceneMainTemaAudioclip, 1, Sound.Bgm);
    }

    protected override E_InputActionMap GetRequiredActionMap() => E_InputActionMap.Game;

    protected override void LoadSavedGame(SaveSlotData data)
    {
        base.LoadSavedGame(data);

        // ✅ 데이터가 있다는 게 확정된 상태
        Managers.Object.Clear(); // 기존 씬 배치 제거

        Managers.Load.ObjectInfoLoad(data.dungeondata.gameEntityDatas);
        Managers.Load.ObjectRestoreSaveData(data.dungeondata.gameEntityDatas);

        _buildingCardUI.RestoreSaveDatas(data.dungeondata.buildingCardDatas);

        Managers.SceneServices.InventoryWrite.AddDownJam(data.dungeondata.downJam);

        Managers.SceneServices.CameraInfo.SetPositionAndRotation(
            data.dungeondata.cameraPos, data.dungeondata.cameraRot);
    }

    protected override void LoadNewGame()
    {
        base.LoadNewGame();

        var list = Managers.SceneServices.InventoryRead.EnabledCards;

        for (int i = 0; i < 5; i++)
            _buildingCardUI?.AddCard(list[Random.Range(0, list.Count)], default, true);

        if (Managers.SceneServices.DungeonCores.Cores.Count > 0)
        {
            Managers.SceneServices.CameraInfo.SetPositionAndRotation
                (Managers.SceneServices.DungeonCores.Cores.First().Position,
                 Managers.SceneServices.DungeonCores.Cores.First().Rotation);
        }
    }
}