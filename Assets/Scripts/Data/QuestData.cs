using UnityEngine;

/// <summary>
/// 퀘스트 데이터 - 퀘스트 상태, 진행도, 타입 등 관리
/// </summary>
[System.Serializable]
public class QuestData
{
    /// <summary>
    /// 퀘스트 타입 - Main/Sub 구분
    /// </summary>
    public enum E_QuestType
    {
        Main,
        Sub,
    }

    /// <summary>
    /// 퀘스트 상태
    /// </summary>
    public enum E_QuestStatus
    {
        NotStarted,
        InProgress,
        Completed,   // 목표 달성 (보상 대기)
        Finished,    // 보상 지급 완료
        Failed
    }

    public Quest.Data _QuestData;
    public E_QuestStatus Status = E_QuestStatus.NotStarted;
    public E_QuestType Type;
    public float AcceptTime;
    public float StartTime;
    public float CompleteTime;
    public int CurrentProgress;

    public NPC ProviderNPC;

    public bool IsCompleted
    {
        get
        {
            bool ret = CurrentProgress >= _QuestData.Quest_Goal_Count || Status == E_QuestStatus.Completed;
            return ret;
        }
    }

    public float ProgressPercent
    {
        get
        {
            if (_QuestData.Quest_Goal_Count <= 0)
                return 0f;

            var ret = Mathf.Clamp01((float)CurrentProgress / _QuestData.Quest_Goal_Count);
            return ret;
        }
    }

    /// <summary>
    /// QuestData 생성 - 타입 지정
    /// </summary>
    public static QuestData Create(Quest.Data questData, E_QuestType type)
    {
        var ret = new QuestData()
        {
            _QuestData = questData,
            Type = type,
            Status = E_QuestStatus.NotStarted,
            CurrentProgress = 0
        };
        return ret;
    }

    public void Accept(NPC providerNPC)
    {
        Status = E_QuestStatus.InProgress;
        AcceptTime = Time.time;
        StartTime = Time.time;
        ProviderNPC = providerNPC;
        CurrentProgress = 0;
    }

    public void Complete()
    {
        Status = E_QuestStatus.Completed;
        CompleteTime = Time.time;
    }

    public void Fail()
    {
        Status = E_QuestStatus.Failed;
    }

    /// <summary>
    /// 보상 지급 완료
    /// </summary>
    public void Finish()
    {
        Status = E_QuestStatus.Finished;
    }

    public void UpdateProgress(int amount)
    {
        CurrentProgress = Mathf.Min(CurrentProgress + amount, _QuestData.Quest_Goal_Count);
    }
}
