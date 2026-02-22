using Data;
using System.Linq;
using UnityEngine;
using static Define;

public class DungeonScene : BaseScene, IGridTerrainScanner
{
    IBuildingCardUI _buildingCardUI;
    DungeonScene() => SceneType = Scene.Dungeon;

    protected override void Start()
    {
        _buildingCardUI = Managers.SceneServices.BuildingCardUI;

        base.Start();

        // Sound
        Managers.Sound.Play(m_SceneMainTemaAudioclip, 1, Sound.Bgm);

        Managers.Grid.SetupTerrain(this);
    }

    protected override E_InputActionMap GetRequiredActionMap() => E_InputActionMap.Game;

    protected override void LoadSavedGame(SaveSlotData data)
    {
        base.LoadSavedGame(data);

        // ✅ 데이터가 있다는 게 확정된 상태
        Managers.Object.Clear(); // 기존 씬 배치 제거

        Managers.Game.ObjectInfoLoad(data.dungeondata.gameEntityDatas);
        Managers.Game.ObjectRestoreSaveData(data.dungeondata.gameEntityDatas);

        _buildingCardUI.RestoreSaveDatas(data.dungeondata.buildingCardDatas);

        Managers.Player.Inventory.AddDownJam(data.dungeondata.downJam);

        Managers.SceneServices.CameraInfo.SetPositionAndRotation(
            data.dungeondata.cameraPos, data.dungeondata.cameraRot);
    }

    protected override void LoadNewGame()
    {
        base.LoadNewGame();

        var list = Managers.Player.Inventory.EnabledCards;

        for (int i = 0; i < 5; i++)
            _buildingCardUI?.AddCard(list[Random.Range(0, list.Count)], default, true);
    }

    [SerializeField] private float rayOffset = 1f;

    public E_TerrainCellType Scan(GridPosition pos, Vector3 worldPos)
    {
        E_TerrainCellType type = E_TerrainCellType.Void;

        if (Physics.Raycast(worldPos + Vector3.up * rayOffset,
                            Vector3.down,
                            rayOffset * 2,
                            GameConfig.Layer.mousePlaneLayerMask))
        {
            type = E_TerrainCellType.Walkable;
        }

        if (Physics.Raycast(worldPos + Vector3.down * rayOffset,
                            Vector3.up,
                            out var r,
                            rayOffset * 2,
                            GameConfig.Layer.ObstaclesLayerMask))
        {
            type = E_TerrainCellType.Obstacle;
        }

        return type;
    }

    private void LateUpdate()
    {
        // TODO unit action tick system으로 이동 바람
        Managers.Grid?.LateTick();
    }
}