using Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static Define;

public partial class GameManager : IManager
{

    public void Clear()
    {
        _patternOffsetCache.Clear();
    }

    public Action OnDungeonExplosionStart; // 미궁 탐험 시작
    public Action OnDungeonExplosionFail; // 미궁 탐험 실패
    public Action OnDungeonExplosionFinish; // 미궁 탐험 종료

    public bool m_IsGamePauseing { get; private set; } = false;

    [Header("Data")]
    public int m_PlaySlotId;
    public float sessionStartTime;

    // 클래스 상단에 캐시 추가
    private readonly Dictionary<(E_RangeShapeType, (int, int, int, int, int, int), E_RangeFillType), HashSet<GridPosition>> _patternOffsetCache
        = new();

    #region Init

    public void Init()
    {
        sessionStartTime = Time.realtimeSinceStartup;

        m_PlaySlotId = Managers.Data.playStatistics?.lastSlotID ?? 0;
    }

    #endregion

    // 선택한 오브젝트의 가장 긴 y축(월드 상) 가져오기
    public float GetObjectColliderLongLength(GameObject obj)
    {
        Collider col = obj.GetComponentInChildren<Collider>();
        if (col == null)
            return 1f; // 기본값

        Vector3 scaledSize = Vector3.zero;

        switch (col)
        {
            case BoxCollider box:
                scaledSize = Vector3.Scale(box.size, obj.transform.lossyScale);
                return Mathf.Max(scaledSize.x, scaledSize.y, scaledSize.z); // 또는 방향 기준
            case SphereCollider sphere:
                return sphere.radius * 2f * obj.transform.lossyScale.x; // 지름
            case CapsuleCollider capsule:
                return capsule.height * obj.transform.lossyScale.y;
            case MeshCollider mesh:
                scaledSize = Vector3.Scale(mesh.sharedMesh.bounds.size, obj.transform.lossyScale);
                return Mathf.Max(scaledSize.x, scaledSize.y, scaledSize.z);
            default:
                return 1f;
        }
    }

    #region Dungeon Start & End

    // 미궁 탐사 시작
    public void DungeonExplosionStart()
    {

    }

    // 미궁 탐사 실패
    public void DungeonExplosionFail()
    {
        Debug.Log("Dungeon Core destroyed! Game Over.");
        OnDungeonExplosionFail?.Invoke();

        // 팝업 띄우기
        Managers.UI.ShowPopupUI<GameOverUI>();

        DungeonExplosionFinish();
    }

    // 미궁 탐사 종료
    public void DungeonExplosionFinish()
    {
        PauseGame();
    }

    // 일시 정지
    public void PauseGame()
    {
        // 게임 진행 멈춤
        Time.timeScale = 0f;
        m_IsGamePauseing = true;


        //Debug.Log("게임 일시 정지");
    }

    // 일시 정지 해제
    public void ResumeGame()
    {
        Time.timeScale = 1f;
        m_IsGamePauseing = false;

        //Debug.Log("게임 일시 정지 해제");
    }

    // 게임 데이터 저장
    public async Task GameSave(Action action = null)
    {
        Debug.Log("Data Save...");

        // 게임 일시 정지
        PauseGame();

        // 저장 팝업 표시하기

        await SaveAllPlayRuntimeData();

        action?.Invoke();
    }

    // 게임 종료
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(); // 어플리케이션 종료
#endif
    }

    #endregion

    #region Screen Shot

    public void CaptureAndSave()
    {
        Managers.SceneServices.CoroutineRunner.Run(ICaptureAndSave());
    }

    // 2. 파일로 저장하기 (PNG)
    private IEnumerator ICaptureAndSave()
    {
        yield return new WaitForEndOfFrame(); // 화면 렌더 끝난 후 캡처

        Texture2D tex = Util.CaptureScreenshot();
        byte[] bytes = tex.EncodeToPNG();

#if UNITY_EDITOR
        // 에디터 환경에서만 직접 Assets 접근
        string slotPath = $"{Managers.Data.GetFilePath()}/slot_{m_PlaySlotId}.png";
#else
    // 빌드 시에는 StreamingAssets에서 불러오기
    return Application.streamingAssetsPath + "/";
#endif

        File.WriteAllBytes(slotPath, bytes);

        //Debug.Log($"스크린샷 저장 완료: {slotPath}");

        GameObject.Destroy(tex); // 메모리 해제

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }


    public Sprite LoadScreenShot(int slotID)
    {
#if UNITY_EDITOR
        // 에디터 환경에서만 직접 Assets 접근
        string slotPath = $"{Managers.Data.GetFilePath()}/slot_{slotID}.png";
#else
    // 빌드 시에는 StreamingAssets에서 불러오기
    return Application.streamingAssetsPath + "/";
#endif

        if (!File.Exists(slotPath))
            return null;

        byte[] bytes = File.ReadAllBytes(slotPath);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    #endregion

    public void FilCopyAndRename(string directory, string originalName, string newName)
    {
        string srcPath = Path.Combine(directory, originalName).Replace("\\", "/");
        string dstPath = Path.Combine(directory, newName).Replace("\\", "/");

        if (!File.Exists(srcPath))
        {
            Debug.LogError($"파일을 찾을 수 없습니다: {srcPath}");
            return;
        }

        File.Copy(srcPath, dstPath, overwrite: true);
        Debug.Log($"파일 복사 완료: {originalName} → {newName}");
    }

    #region GameEntity

    public void GameEntityModelsSetLayer(GameEntity gameEntity, int layerID)
    {
        if (gameEntity == null)
            return;

        foreach (var (mat, obj) in gameEntity.GetModelsMaterial())
        {
            if (obj != null)
                obj.layer = layerID;
        }
    }

    public void GameEntityModelsSetColor(GameEntity gameEntity, Color color)
    {
        foreach (var (mat, obj) in gameEntity.GetModelsMaterial())
        {
            if (mat != null)
                mat.color = color;
        }
    }

    #endregion

    #region Runtime Data Save & Load

    public async Task SaveAllPlayRuntimeData()
    {
        await AutoSaveSlotAsync();
        //await SaveAsync<SettingData>();
        //await SaveAsync<AchievementData>();
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

                    buildingCardDatas = Managers.SceneServices.BuildingCardUI.CaptureSaveData(),
                    downJam = Managers.Player.Inventory.DownJamAmount,
                    cameraPos = Managers.SceneServices.CameraInfo.Position,
                    cameraRot = Managers.SceneServices.CameraInfo.Rotation,
                };
                break;

            case Define.Scene.Camp:
                saveDic[slotId].campdata = new CampSaveData
                {
                    // 캠프 전용 데이터 추가 시 여기에 작성
                };
                break;
            case Define.Scene.Start:
                if (isNewGame)
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
        await AutoSaveSlotAsync(m_PlaySlotId);
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
        float sessionTime = Time.realtimeSinceStartup - sessionStartTime;
        slot.totalPlaySeconds += sessionTime;
        sessionStartTime = Time.realtimeSinceStartup;

        // 4️ 스크린샷 저장
        CaptureAndSave();

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
        FilCopyAndRename(
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

    public async Task SavePlayStatistics()
    {
        // 딕셔너리에 슬롯이 없으면 신규 생성
        var data = Managers.Data.playStatistics;
        data.lastSlotID = m_PlaySlotId;

        Managers.Data.Set<PlayStatistics>(data);

        await Managers.Data.SaveAsync<PlayStatistics>();
    }

    #endregion

    #region Runtime (실시간 유동 데이터)

    public SaveSlotData GetContinueSaveData()
    {
        if (Managers.Data.SaveDic.TryGetValue(Managers.Data.playStatistics.lastSlotID, out var slot))
            return slot;

        return null;
    }

    public void ObjectInfoLoad(List<BaseData> objs)
    {
        foreach (var obj in objs)
            ObjectInfoLoad(obj);
    }

    public void ObjectInfoLoad(BaseData data)
    {
        // 2. ObjectManager에서 프리팹 원본을 가져옵니다.
        GameObject go = Managers.Object.GetPrefabByName(data.prefabName);

        if (go == null)
        {
            Debug.LogError($"ObjectLoad Failed: Prefab '{data.prefabName}' not found in ObjectManager.");
            return;
        }

        GameObject newGO = Managers.Resource.Instantiate(go);

        newGO.GetComponent<IGuidObject>().SetGUID(data.guid);
        Managers.Object.Add(newGO);
    }

    public void ObjectRestoreSaveData(List<BaseData> datas)
    {
        foreach (var obj in datas)
            ObjectRestoreSaveData(obj);
    }

    public void ObjectRestoreSaveData(BaseData data)
    {
        GameObject newGO = Managers.Object.FindByGuidObject(data.guid);

        // 4. 소환된 오브젝트에서 ISaveable 컴포넌트를 얻어 데이터를 복원합니다.
        ISaveable saveableComponent = newGO.GetComponent<ISaveable>();

        if (saveableComponent != null)
        {
            // 5. RestoreSaveData를 호출하여 GUID, 스탯 등의 런타임 상태를 덮어씁니다.
            saveableComponent.RestoreSaveData(data);
        }
        else
        {
            Debug.LogError($"ObjectLoad Failed: Instantiated object '{data.prefabName}' is missing ISaveable component.");
        }
    }

    #endregion
}
