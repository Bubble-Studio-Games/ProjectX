using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[Serializable]
public class SaveData
{
    // 미궁
    // 맵에 대한 정보
    // 맵에 배치된 오브젝트들의 정보
    public List<GameObject> objects;

    // 인벤토리
    // 카드
    public List<BuildingCard> buildingCards;
    // 다운 잼 (재화)
    public int downJam;

    // 카메라 위치
    public Transform CameraTransform;
}

[Serializable]
public class SaveSlot
{
    public int slotId;
    public SaveData data;
    public string createTime;
    public string lastSaveTime;
    public double totalPlaySeconds; // 총 플레이 시간 (초)
}

[Serializable]
public class SaveWrapper
{
    // 각 슬롯에 저장된 정보들
    public SaveSlot[] slots = new SaveSlot[3];
}


[Serializable]
public class GameSetting
{
    // 각 슬롯에 저장된 정보들
    public SaveSlot[] slots = new SaveSlot[3];

    // 기타 세팅 정보들 (언어, 접근성, 비디오 등)
}

public class DataManager
{
    private SaveWrapper wrapper = new SaveWrapper();

    // 환경에 따른 파일 경로 접근
    public string GetFilePath()
    {
#if UNITY_EDITOR
        // 에디터 환경에서만 직접 Assets 접근
        return Application.dataPath + "/Resources/Data/Save";
#else
    // 빌드 시에는 StreamingAssets에서 불러오기
    return Application.streamingAssetsPath + "/";
#endif
    }

    public void Init()
    {
        for (int i = 0; i < 3; i++)
        {
            wrapper.slots[i] = new SaveSlot()
            {
                slotId = i,
                data = new SaveData(),
                createTime = "",
                lastSaveTime = "",
                totalPlaySeconds = 0,
            };
        }

        Load();
    }

    public void Load()
    {
        // 저장 폴더 경로 확인
        if (!Directory.Exists(GetFilePath()))
        {
            Directory.CreateDirectory(GetFilePath());
            Debug.Log($"Save 폴더 생성: {GetFilePath()}");
        }

        // 각 슬롯 파일 로드 시도
        for (int slotId = 0; slotId < 3; slotId++)
        {
            string slotPath = $"{GetFilePath()}/{slotId}.json";

            if (File.Exists(slotPath))
            {
                string json = File.ReadAllText(slotPath);
                SaveSlot slot = JsonUtility.FromJson<SaveSlot>(json);
                wrapper.slots[slotId] = slot;

                //Debug.Log($"{slotId}번 슬롯 데이터를 불러왔습니다.");
            }
        }
    }

    /// <summary>
    /// 현재 진행 중인 슬롯에 데이터 자동 저장
    /// </summary>
    
    public void AutoSave()
    {

        // 저장 폴더 경로 확인
        if (!Directory.Exists(GetFilePath()))
        {
            Directory.CreateDirectory(GetFilePath());
            Debug.Log($"Save 폴더 생성: {GetFilePath()}");
        }

        int slotId = Managers.Game.m_PlaySlotId; // 1~3 이라고 가정
        if (slotId < 0 || slotId > 2)
        {
            Debug.LogError("잘못된 슬롯 ID: " + slotId);
            return;
        }

        // 데이터 저장
        SetSaveData(slotId);
        //wrapper.slots[slotId].playTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(GetFilePath(), json);

        Debug.Log($"슬롯 {slotId} 저장 완료!");
    }

    // 게임 종료시 저장을 확립할 때 쓰는 것.
    public async Task asyncSave()
    {
        await asyncSave(Managers.Game.m_PlaySlotId);
    }

    public async Task asyncSave(int slotId, bool isCopy = false)
    {
        // 저장 폴더 경로 확인
        if (!Directory.Exists(GetFilePath()))
        {
            Directory.CreateDirectory(GetFilePath());
            Debug.Log($"Save 폴더 생성: {GetFilePath()}");
        }

        if (slotId < 0 || slotId > 2)
        {
            Debug.LogError("잘못된 슬롯 ID: " + slotId);
            return;
        }

        // 데이터들 집어 넣기
        SetSaveData(slotId);

        if(isCopy == false)
        {
            // 최초
            if (string.IsNullOrEmpty(wrapper.slots[slotId].createTime))
            {
                wrapper.slots[slotId].createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }

            // 이번 세션 플레이 시간 계산
            float sessionPlayTime = Time.realtimeSinceStartup - Managers.Game.sessionStartTime;
            wrapper.slots[slotId].totalPlaySeconds += sessionPlayTime;

            wrapper.slots[slotId].lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // ✅ 저장 직후 세션 시작 시간 갱신
            Managers.Game.sessionStartTime = Time.realtimeSinceStartup;
        }

        // 스크린샷 찍기
        Managers.Game.CaptureAndSave();

        string json = JsonUtility.ToJson(wrapper.slots[slotId], true);
        string slotPath = $"{GetFilePath()}/{slotId}.json";

        try
        {
            await File.WriteAllTextAsync(slotPath, json);


        }
        catch (Exception e)
        {
            Debug.LogError($"세이브 실패: {e.Message}");
        }

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif

        Debug.Log($"슬롯 {slotId} 저장 완료! 총 플레이 시간: {wrapper.slots[slotId].totalPlaySeconds}초");
    }

    // 본격 데이터 넣기
    public void SetSaveData(int index)
    {

        SaveData data = new SaveData()
        {
            objects = Managers.Object.GetObjectList().ToList(),

            buildingCards = BuildingTypeSelectUI.Instance.ShowGameEntityCard,

            downJam = Inventory.Instance.m_iDownJamAmount,

            CameraTransform = Camera.main.transform,

        };

        wrapper.slots[index].data = data;

        // 새로 만들기 시 
        // CreateTime

        // 플레이 타임
        // PlayTime
    }


    // 데이터 초기화
    public async Task DeleteAsync(int slotId)
    {
        wrapper.slots[slotId] = new SaveSlot
        {
            slotId = slotId,
            data = new SaveData(),
            createTime = "",
            lastSaveTime = "",
            totalPlaySeconds = 0,
        };

        // 파일 삭제
        string slotData = $"{GetFilePath()}/{slotId}.json";
        if (File.Exists(slotData))
        {
            // json파일 삭제
            File.Delete(slotData);
        }

        // 이미지 파일 삭제
        string slotImage = $"{Managers.Data.GetFilePath()}/slot_{slotId}.png";
        if (File.Exists(slotImage))
        {
            // json파일 삭제
            File.Delete(slotImage);
        }

        AssetDatabase.Refresh(); // Refresh the Unity Editor to reflect changes

        // UX적으로 삭제 처리중 "잠깐의 대기" (예: 0.2초)
        await Task.Delay(200);
    }


    /// <summary>
    /// 현재 모든 슬롯 저장 (강제)
    /// 쓸 데가 있을까?
    /// </summary>
    public void SaveAll()
    {
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(GetFilePath(), json);
    }

    /// <summary>
    /// 특정 슬롯 불러오기
    /// </summary>
    public SaveData GetSlotData(int slotId)
    {
        if (slotId < 0 || slotId > 2) return null;
        return wrapper.slots[slotId]?.data;
    }

    public SaveData GetSlotData()
    {
        return wrapper.slots[Managers.Game.m_PlaySlotId]?.data;
    }

    /// <summary>
    /// 슬롯 정보 (메타데이터 포함)
    /// </summary>
    public SaveSlot GetSlot(int slotId)
    {
        if (slotId < 0 || slotId > 2) return null;
        return wrapper.slots[slotId];
    }

    public SaveSlot GetSlot()
    {
        return wrapper.slots[Managers.Game.m_PlaySlotId];
    }

    public async Task Copy(int fromSlotId, int toSlotId)
    {
        // 원본 가져오기
        var data = wrapper.slots[fromSlotId];

        // JSON 직렬화 후 역직렬화 → 깊은 복사
        string json = JsonUtility.ToJson(data);
        var clone = JsonUtility.FromJson<SaveSlot>(json);

        // 복사한 인스턴스를 toSlot에 할당
        wrapper.slots[toSlotId] = clone;

        await asyncSave(toSlotId, true);

        // Copy And Past Image
        Managers.Game.FilCopyAndRename(
            Managers.Data.GetFilePath(),
            $"slot_{fromSlotId}.png",
            $"slot_{toSlotId}.png");
    }
}
