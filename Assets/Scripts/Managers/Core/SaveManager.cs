using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Data;
using System.Collections.Generic;
using static Define;

public class SaveManager
{
    public async Task SaveAllData()
    {
        await AutoSaveSlotAsync();
        //await Managers.Data.SaveAsync<SettingData>();
        //await Managers.Data.SaveAsync<AchievementData>();
        await SavePlayStatistics();
    }


    #region Slot
    /// <summary>
    /// 🔹 현재 슬롯의 인게임 데이터를 DataManager 캐시에 반영
    /// </summary>
    private void CacheSlotData(int slotId)
    {
        var sceneType = Managers.Scene.CurrentScene.SceneType;
        var saveDic = Managers.Data.SaveDic;

        bool isNewGame = false;
        // 슬롯 없으면 생성
        if (!saveDic.ContainsKey(slotId))
        {
            saveDic[slotId] = new SaveSlotData { slotId = slotId };
            isNewGame = true;
        }

        // 씬 타입별로 분기
        switch (sceneType)
        {
            case Define.Scene.Dungeon:

                saveDic[slotId].dungeondata = new DungeonSaveData
                {
                    gameEntityDatas = Managers.Object?._objects?
                            .Where(obj => obj != null)
                            .Select(obj => obj?.GetComponent<ISaveable>())
                            .Where(isave => isave != null)
                            .Select(isave => isave.CaptureSaveData())
                            .ToList(),

                    buildingCardDatas = BuildingTypeSelectUI.Instance.CaptureSaveData(),
                    downJam = Inventory.Instance.m_iDownJamAmount,
                    cameraPos = CameraController.Instance.m_Follow.transform.position,
                    cameraRot = CameraController.Instance.m_Follow.transform.rotation,
                };
                break;

            case Define.Scene.Camp:
                saveDic[slotId].campdata = new CampSaveData
                {
                    // 캠프 전용 데이터 추가 시 여기에 작성
                };
                break;
            case Define.Scene.Start:
                if(isNewGame)
                {
                    saveDic[slotId].dungeondata = new DungeonSaveData();
                    saveDic[slotId].campdata = new CampSaveData();
                }
                break;
            default:
                Debug.LogWarning($"⚠️ CacheSlotData: 정의되지 않은 SceneType ({sceneType})");
                break;
        }

        // 공통 필드 갱신
        saveDic[slotId].LastScene = sceneType;

        // DataManager에 캐시 반영
        Managers.Data.SetDic<SaveSlotLoader, int, SaveSlotData>(saveDic);

        Debug.Log($"💾 슬롯 {slotId} 데이터 캐싱 완료 ({sceneType})");
    }

    /// <summary>
    /// 🔹 현재 진행 중인 슬롯 자동 저장
    /// </summary>
    public async Task AutoSaveSlotAsync()
    {
        await AutoSaveSlotAsync(Managers.Game.m_PlaySlotId);
    }

    public async Task AutoSaveSlotAsync(int slotId)
    {
        if (slotId < 0 || slotId > 2)
        {
            Debug.LogError("❌ 잘못된 슬롯 ID");
            return;
        }

        // 1️ 현재 게임 상태 캐싱
        CacheSlotData(slotId);

        // 2️ 메타데이터 갱신
        var slot = Managers.Data.SaveDic[slotId];
        if (string.IsNullOrEmpty(slot.createTime))
            slot.createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        slot.lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 3️ 플레이 타임 누적
        float sessionTime = Time.realtimeSinceStartup - Managers.Game.sessionStartTime;
        slot.totalPlaySeconds += sessionTime;
        Managers.Game.sessionStartTime = Time.realtimeSinceStartup;

        // 4️ 스크린샷 저장
        Managers.Game.CaptureAndSave();

        // 5️⃣ DataManager를 통한 비동기 저장 (자동 백업/파일 관리)
        await Managers.Data.SaveAsync<SaveSlotLoader>();

        Debug.Log($"💾 슬롯 {slotId} 저장 완료! 총 {slot.totalPlaySeconds:0.0}s");
    }

    /// <summary>
    /// 🔹 슬롯 복사 (예: 0번 슬롯 → 2번 슬롯)
    /// </summary>
    public async Task CopySlotAsync(int fromSlotId, int toSlotId)
    {
        await Managers.Data.CopyDicValueAsync<SaveSlotLoader, int, SaveSlotData>(fromSlotId, toSlotId);

        // 미리보기 이미지 복사
        Managers.Game.FilCopyAndRename(
            Managers.Data.GetFilePath(),
            $"slot_{fromSlotId}.png",
            $"slot_{toSlotId}.png");

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif

        Debug.Log($"✅ 슬롯 {fromSlotId} → {toSlotId} 복사 완료");
    }

    /// <summary>
    /// 🔹 슬롯 데이터 삭제
    /// </summary>
    public async Task DeleteSlotAsync(int slotId)
    {
        await Managers.Data.DeleteDicKeyAsync<SaveSlotLoader, int, SaveSlotData>(slotId);

        string slotImage = $"{Managers.Data.GetFilePath()}/slot_{slotId}.png";
        if (System.IO.File.Exists(slotImage))
            System.IO.File.Delete(slotImage);

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif

        Debug.Log($"🗑️ 슬롯 {slotId} 삭제 완료 (백업됨)");
    }

    /// <summary>
    /// 🔹 슬롯 복원 (백업 시점 기준)
    /// </summary>
    public async Task RestoreSlotAsync(string timestamp)
    {
        await Managers.Data.RestoreBackupAsync<SaveSlotLoader>(timestamp);
        Debug.Log($"♻️ 슬롯 데이터 복원 완료 → {timestamp}");
    }

    #endregion

    #region PlayStatistics

    public async Task SavePlayStatistics()
    {
        // 딕셔너리에 슬롯이 없으면 신규 생성
        var data = Managers.Data.playStatistics;
        data.lastSlotID = Managers.Game.m_PlaySlotId;

        Managers.Data.Set<PlayStatistics>(data);

        await Managers.Data.SaveAsync<PlayStatistics>();
    }


    #endregion
}
