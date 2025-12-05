using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Define;


public class DungeonScene : BaseScene
{
    [SerializeField] private GameObject m_InitObject; // 초기 데이터

    protected override void Init()
    {
        base.Init();

        SceneType = Scene.Dungeon;

        var data = Managers.Load.GetContinueSaveData();

        // 기존 플레이 했던 데이터가 있는 경우
        if (data != null && data.dungeondata.gameEntityDatas.Count > 0 && useLoadSaveFile)
        {
            m_InitObject.SetActive(false);

            Managers.Object.Clear();
        }
        else
        {
            m_InitObject.SetActive(true);
        }
    }

    protected override void Start()
    {
        base.Start();

        // Sound
        Managers.Sound.Play(m_SceneMainTemaAudioclip, 1, Sound.Bgm);

        // Data
        var data = Managers.Load.GetContinueSaveData();

        // 기존 플레이 했던 데이터가 있는 경우
        if (data != null && data.dungeondata.gameEntityDatas.Count > 0 && useLoadSaveFile)
        {
            // 배치 오브젝트
            Managers.Object.Clear();

            // 오브젝트 로드
            Managers.Load.ObjectInfoLoad(data.dungeondata.gameEntityDatas); // 오브젝트 등록
            Managers.Load.ObjectRestoreSaveData(data.dungeondata.gameEntityDatas); // 오브젝트 정보 로드

            // 카드
            BuildingTypeSelectUI.Instance.RestoreSaveDatas(data.dungeondata.buildingCardDatas);

            // 다운 잼 (재화)
            Inventory.Instance.m_iDownJamAmount = data.dungeondata.downJam;

            // 카메라
            CameraController.Instance.m_Follow.transform.SetPositionAndRotation(data.dungeondata.cameraPos, data.dungeondata.cameraRot);
        }
        else
        {
            // 초기 데이터라면 무작위로 5개 생성
            // Card Data Load
            var list = Inventory.Instance.GetEnableCardList();
            for (int i = 0; i < 5; i++)
                BuildingTypeSelectUI.Instance.AddCard(list[Random.Range(0, list.Count)], default, true);

            // 카메라
            if (DungeonCore.instance != null)
            {
                CameraController.Instance.m_Follow.transform.SetPositionAndRotation(
                    DungeonCore.instance.transform.position, DungeonCore.instance.transform.rotation);
            }
        }

        // Input Binding
        InputManager.Instance.playerInputActions.KeyBoard.ESC.performed += i =>
        {

        };
    }

    public override void Clear()
    {
        
    }

}
