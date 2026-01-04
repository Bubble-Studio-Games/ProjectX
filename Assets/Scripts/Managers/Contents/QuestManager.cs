using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

/// <summary>
/// 퀘스트 매니저 - 퀘스트 수령, 진행도 추적, 완료/실패 처리
/// </summary>
public class QuestManager
{
    private Dictionary<string, Quest.Data> _questData;
    private Dictionary<string, QuestData> _quests = new();

    public event Action<QuestData> OnQuestAccepted;
    public event Action<QuestData> OnQuestProgressUpdated;
    public event Action<QuestData> OnQuestCompleted;
    public event Action<QuestData> OnQuestFailed;

    private MainQuestUI _mainQuestUI;
    public MainQuestUI MainQuestUI
    {
        get
        {
            if (_mainQuestUI == null)
                _mainQuestUI = GameObject.FindAnyObjectByType<MainQuestUI>(FindObjectsInactive.Include);
            return _mainQuestUI;
        }
    }

    public void Init()
    {
        _questData = Managers.Data.QuestData;
    }

    public void Clear()
    {
        _quests.Clear();
    }

    /// <summary>
    /// 퀘스트 수락
    /// </summary>
    public bool AcceptQuest(string questId)
    {
        Init();
        if (_questData.TryGetValue(questId, out var questData) == false)
        {
            Debug.LogError($"[QuestManager] 퀘스트 데이터를 찾을 수 없음: {questId}");
            return false;
        }

        // 이미 수락한 퀘스트인지 확인
        if (_quests.ContainsKey(questId))
        {
            Debug.LogWarning($"[QuestManager] 이미 수락한 퀘스트: {questId}");
            return false;
        }

        // Quest.Data.Quest_Type에서 타입 파싱
        var type = ParseQuestType(questData.Quest_Type);

        // 메인 퀘스트 1개 제한 체크
        if (type == QuestData.E_QuestType.Main && GetActiveMainQuest() != null)
        {
            Debug.LogWarning("[QuestManager] 이미 진행 중인 메인 퀘스트가 있습니다.");
            return false;
        }

        var quest = QuestData.Create(questData, type);
        quest.Accept(null);
        _quests.Add(questId, quest);

        // 메인 퀘스트인 경우 UI 업데이트
        if (type == QuestData.E_QuestType.Main)
        {
            MainQuestUI.UpdateData(questData);
            MainQuestUI.ShowAnimation();
        }

        Debug.Log($"[QuestManager] 퀘스트 수락: {questId} (타입: {type}, 상태: {quest.Status})");
        OnQuestAccepted?.Invoke(quest);

        return true;
    }

    /// <summary>
    /// Quest.Data.Quest_Type 문자열 파싱
    /// </summary>
    private QuestData.E_QuestType ParseQuestType(string questType)
    {
        if (string.IsNullOrEmpty(questType))
            return QuestData.E_QuestType.Main;

        if (Enum.TryParse<QuestData.E_QuestType>(questType, true, out var ret))
            return ret;

        return QuestData.E_QuestType.Main;
    }

    /// <summary>
    /// 퀘스트 진행도 업데이트
    /// </summary>
    public void UpdateQuestProgress(string questId, int progress)
    {
        if (_quests.TryGetValue(questId, out var quest) == false)
            return;

        quest.UpdateProgress(progress);
        OnQuestProgressUpdated?.Invoke(quest);
    }

    /// <summary>
    /// 메인 퀘스트 진행도 업데이트
    /// </summary>
    public void UpdateQuestProgress(int progress)
    {
        var mainQuest = GetActiveMainQuest();
        if (mainQuest == null)
            return;

        mainQuest.UpdateProgress(progress);
        OnQuestProgressUpdated?.Invoke(mainQuest);
    }

    /// <summary>
    /// 메인 퀘스트 강제 완료 - 테스트용
    /// </summary>
    public void ForceCompleteQuest()
    {
        var mainQuest = GetActiveMainQuest();
        if (mainQuest == null)
        {
            Debug.LogWarning("[QuestManager] 진행 중인 메인 퀘스트가 없습니다.");
            return;
        }

        mainQuest.Complete();
        OnQuestCompleted?.Invoke(mainQuest);
    }

    /// <summary>
    /// 퀘스트 완료 처리 - questId 지정
    /// </summary>
    public void CompleteQuest(string questId)
    {
        if (_quests.TryGetValue(questId, out var quest) == false)
        {
            Debug.LogWarning($"[QuestManager] 퀘스트를 찾을 수 없음: {questId}");
            return;
        }

        if (quest.IsCompleted == false)
        {
            Debug.LogWarning("[QuestManager] 퀘스트 목표가 아직 달성되지 않았습니다.");
            return;
        }

        quest.Complete();
        OnQuestCompleted?.Invoke(quest);
    }

    /// <summary>
    /// 퀘스트 보상 UI 표시 - questId 지정
    /// </summary>
    public RewardUI ShowQuestReward(string questId)
    {
        if (_quests.TryGetValue(questId, out var quest) == false)
        {
            Debug.LogWarning($"[QuestManager] 퀘스트를 찾을 수 없음: {questId}");
            return null;
        }

        var rewardUI = Managers.UI.ShowPopupUI<RewardUI>();
        rewardUI.SetUp(quest, () => GiveQuestRewards(questId));
        return rewardUI;
    }

    /// <summary>
    /// 퀘스트 보상 UI 표시 - 메인 퀘스트 자동 조회
    /// </summary>
    public RewardUI ShowQuestReward()
    {
        // 완료된 메인 퀘스트 먼저 확인 (QUEST_COMPLETE 후 GIVE_REWARD 호출 시)
        var mainQuest = GetCompletedMainQuest();

        // 완료된 퀘스트가 없으면 진행 중인 퀘스트 확인
        if (mainQuest == null)
            mainQuest = GetActiveMainQuest();

        if (mainQuest == null)
        {
            Debug.LogWarning("[QuestManager] 보상 지급 가능한 메인 퀘스트가 없습니다.");
            return null;
        }

        var questId = mainQuest._QuestData.Quest_ID;
        return ShowQuestReward(questId);
    }

    public void HideMainQuestUI(Action onComplete = null)
    {
        if (MainQuestUI == null)
        {
            onComplete?.Invoke();
            return;
        }

        MainQuestUI.HideAnimation(onComplete);
    }

    /// <summary>
    /// 퀘스트 보상 지급 - Finished 상태로 변경
    /// </summary>
    private void GiveQuestRewards(string questId)
    {
        if (_quests.TryGetValue(questId, out var quest) == false)
        {
            Debug.LogError("[QuestManager] 보상 지급 실패: 퀘스트를 찾을 수 없습니다.");
            return;
        }

        Debug.Log($"[QuestManager] 퀘스트 보상 지급: {questId}");
        quest.Finish();
    }

    /// <summary>
    /// 현재 활성 메인 퀘스트 데이터 조회
    /// </summary>
    public Quest.Data MainQuestData => GetActiveMainQuest()?._QuestData;

    /// <summary>
    /// 활성 메인 퀘스트 조회 - InProgress 상태
    /// </summary>
    public QuestData GetActiveMainQuest()
    {
        var ret = _quests.Values.FirstOrDefault(q =>
            q.Type == QuestData.E_QuestType.Main &&
            q.Status == QuestData.E_QuestStatus.InProgress);
        return ret;
    }

    /// <summary>
    /// 보상 대기 중인 메인 퀘스트 조회 - Completed 상태
    /// </summary>
    public QuestData GetCompletedMainQuest()
    {
        var ret = _quests.Values.FirstOrDefault(q =>
            q.Type == QuestData.E_QuestType.Main &&
            q.Status == QuestData.E_QuestStatus.Completed);
        return ret;
    }


    /// <summary>
    /// 현재 메인 퀘스트 상태 조회
    /// </summary>
    public QuestData.E_QuestStatus GetMainQuestStatus()
    {
        var mainQuest = GetActiveMainQuest();
        if (mainQuest == null)
            return QuestData.E_QuestStatus.NotStarted;

        return mainQuest.Status;
    }

    /// <summary>
    /// 특정 퀘스트 ID의 상태 조회
    /// </summary>
    public QuestData.E_QuestStatus GetQuestStatus(string questId)
    {
        if (_quests.TryGetValue(questId, out var quest))
            return quest.Status;

        return QuestData.E_QuestStatus.NotStarted;
    }

    /// <summary>
    /// 퀘스트를 받을 수 있는지 확인
    /// </summary>
    public bool CanAcceptQuest(string questId)
    {
        var status = GetQuestStatus(questId);
        return status == QuestData.E_QuestStatus.NotStarted;
    }
}
