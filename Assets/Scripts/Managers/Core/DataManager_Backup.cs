using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class BackupInfo
{
    public string timestamp;
    public string fileName;
    public string typeName;
    public string createdAt;
    public long fileSize;
}

[Serializable]
public class BackupMetaData
{
    public List<BackupInfo> backups = new List<BackupInfo>();
}

/// <summary>
/// 런타임 데이터를 시간 별로 데이터를 저장한다.
/// 백업, 덮어 쓰기, 삭제 기능 추가 예정.
/// </summary>
public partial class DataManager
{
    // ⚙️ 백업 관련 상수 및 유틸
    private const int MAX_BACKUPS = 10; // ✅ 유지할 최대 백업 개수
    private const string BACKUP_META_FILE = "BackupList.json";
    private BackupMetaData _backupMeta = new BackupMetaData();

    // ⚙️ 백업 관련 유틸
    private string GetBackupRoot()
    {
        string backupRoot = Path.Combine(GetFilePath(), "Backup");
        return backupRoot;
    }

    private string GetBackupFolderPath()
    {
        string folder = Path.Combine(GetBackupRoot(), DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private string GetBackupMetaPath()
    {
        return Path.Combine(GetBackupRoot(), BACKUP_META_FILE);
    }

    // 📦 백업 수행 + 메타데이터 갱신
    private void BackupFile(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        string backupFolder = GetBackupFolderPath();
        string fileName = Path.GetFileName(filePath);
        string destPath = Path.Combine(backupFolder, fileName);

        File.Copy(filePath, destPath, true);

        // 메타데이터 기록
        var info = new BackupInfo
        {
            timestamp = Path.GetFileName(backupFolder),
            fileName = fileName,
            typeName = Path.GetFileNameWithoutExtension(fileName),
            createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            fileSize = new FileInfo(destPath).Length
        };

        _backupMeta.backups.Add(info);
        SaveBackupMeta();

        CleanupOldBackups();
        Debug.Log($"📦 백업 완료 → {destPath}");
    }

    // ♻️ 오래된 백업 자동 정리
    private void CleanupOldBackups()
    {
        string backupRoot = GetBackupRoot();

        var sorted = _backupMeta.backups
            .OrderByDescending(x => x.timestamp)
            .ToList();

        if (sorted.Count <= MAX_BACKUPS)
            return;

        foreach (var old in sorted.Skip(MAX_BACKUPS))
        {
            string dir = Path.Combine(backupRoot, old.timestamp);
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
                _backupMeta.backups.Remove(old);
                Debug.Log($"🗑️ 오래된 백업 삭제: {old.timestamp}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ 백업 삭제 실패: {e.Message}");
            }
        }

        SaveBackupMeta();
    }

    // 💾 메타데이터 저장 및 로드
    private void SaveBackupMeta()
    {
        string path = GetBackupMetaPath();
        string json = JsonUtility.ToJson(_backupMeta, true);
        File.WriteAllText(path, json);
    }

    private void LoadBackupMeta()
    {
        string path = GetBackupMetaPath();
        if (!File.Exists(path))
            return;

        string json = File.ReadAllText(path);
        _backupMeta = JsonUtility.FromJson<BackupMetaData>(json) ?? new BackupMetaData();
    }

    // 🧩 백업 복원 기능
    public async Task RestoreBackupAsync<T>(string timestamp)
        where T : new()
    {
        Type t = typeof(T);
        string backupRoot = GetBackupRoot();
        string sourceDir = Path.Combine(backupRoot, timestamp);

        if (!Directory.Exists(sourceDir))
        {
            Debug.LogError($"❌ 복원 실패: {timestamp} 백업 폴더가 존재하지 않습니다.");
            return;
        }

        string sourceFile = Path.Combine(sourceDir, $"{t.Name}.json");
        string destFile = $"{GetFilePath()}/{t.Name}.json";

        if (!File.Exists(sourceFile))
        {
            Debug.LogError($"❌ 복원 실패: {t.Name}.json이 {timestamp} 백업에 없습니다.");
            return;
        }

        // 복원 전 현재 상태 백업
        BackupFile(destFile);

        // 실제 복사
        File.Copy(sourceFile, destFile, true);
        Debug.Log($"✅ {t.Name} 백업 복원 완료 → {timestamp}");

        _dataCache.Remove(t);

        // 💡 ILoader 인터페이스 상속 여부를 확인하는 일반적인 방법
        bool isLoader = t.GetInterfaces().Any(
            i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILoader<,>));
        if(isLoader)
        {
            // 1. TKey와 TValue 타입을 동적으로 추출합니다.
            // T가 구현한 ILoader<TKey, TValue> 인터페이스 정의를 찾습니다.
            Type loaderInterface = t.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILoader<,>));

            // [0]은 TKey, [1]은 TValue 입니다.
            Type tKey = loaderInterface.GetGenericArguments()[0];
            Type tValue = loaderInterface.GetGenericArguments()[1];

            // 2. LoadLoaderAsync의 MethodInfo를 Reflection으로 가져옵니다.
            // (LoadLoaderAsync가 private 또는 public 인스턴스 메서드라고 가정합니다.)
            var loadMethodInfo = GetType().GetMethod(
                nameof(LoadLoaderAsync),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            // 3. T, TKey, TValue 인수로 제네릭 메서드를 닫습니다.
            // T는 여기서는 TLoader 역할을 합니다.
            var genericLoadMethod = loadMethodInfo.MakeGenericMethod(t, tKey, tValue);

            // 4. 메서드를 Invoke하고 Task가 완료되기를 기다립니다. (fileName 인수는 null)
            // Invoke의 결과는 Task<TLoader> 타입의 Task 객체입니다.
            // (fileName은 null로 전달)
            var task = (Task)genericLoadMethod.Invoke(this, new object[] { null });

            // 비동기 작업 완료 대기
            await task;


        }
        else
        {
            await LoadSingleAsync<T>();
        }
    }

    // 📋 백업 목록 조회
    public List<BackupInfo> GetBackupList()
    {
        LoadBackupMeta();
        return _backupMeta.backups
            .OrderByDescending(x => x.timestamp)
            .ToList();
    }

}
