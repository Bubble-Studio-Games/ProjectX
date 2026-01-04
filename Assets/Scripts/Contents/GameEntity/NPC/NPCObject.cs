using Data;
using System.Collections.Generic;
using UnityEngine;
using static Define;


public class NPCObject : MonoBehaviour, ISaveable
{
    private void OnValidate()
    {
        if (Icon == null)
            Icon = this.gameObject.GetComponentInChildren<NPCInteractionUI>(true);
    }

    [field: SerializeField] public Dialogue.Data Data {get; private set;}
    [field: SerializeField] public NPCInteractionUI Icon {get; private set;}
    [field: SerializeField] public NPCData NPCData {get; private set;} 
    [SerializeField] private string _dataID;

    private void Awake()
    {
        NPCData = NPCData.Create(_dataID);
        Icon.Setup(this, onClick: () => StartDialogue());
    }

    public void OnDestroy()
    {
        Icon?.Clear();
    }

    public void StartDialogue()
    {
        var entryPoint = GetEntryPoint();
        Managers.Dialogue.StartDialogue(NPCData.DialogueID, entryPoint,
            onStarted: () => OnDialogueStarted(),
            onEnded: () => OnDialogueEnded());
    }

    private void OnDialogueStarted()
    {
        NPCData.StartComunicationStack++;
    }

    private void OnDialogueEnded()
    {
        NPCData.EndComunicationStack++;
    }

    /// <summary>
    /// 현재 NPC 상태에 맞는 대화 진입점 반환 - Priority 순으로 조건 체크
    /// </summary>
    public string GetEntryPoint()
    {
        var dialogueId = NPCData.DialogueID;
        if (string.IsNullOrEmpty(dialogueId))
            dialogueId = Data?.Dialogue_ID;

        if (Managers.Data.EntryData.TryGetValue(dialogueId, out var entries) == false)
            return null;

        // Priority 오름차순으로 정렬
        var sortedEntries = new List<Dialogue.EntryData>(entries);
        sortedEntries.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        // 우선순위가 높은 순서대로 조건 체크
        foreach (var entry in sortedEntries)
        {
            if (CheckCondition(entry.Condition, entry.Condition_Param))
                return entry.Entry_Point;
        }

        return null;
    }

    /// <summary>
    /// 조건 충족 여부 확인
    /// </summary>
    private bool CheckCondition(E_Condition condition, string param)
    {
        bool ret;
        switch (condition)
        {
            case E_Condition.DEFAULT:
                return NPCData.EndComunicationStack > 0;

            case E_Condition.FIRST_MEET:
                return NPCData.EndComunicationStack <= 0;

            case E_Condition.QUEST_FINISHED:
                if (string.IsNullOrEmpty(param))
                    return false;
                ret = Managers.Quest.GetQuestStatus(param) == QuestData.E_QuestStatus.Finished;
                return ret;

            case E_Condition.QUEST_COMPLETE:
                if (string.IsNullOrEmpty(param))
                    return false;
                ret = Managers.Quest.GetQuestStatus(param) == QuestData.E_QuestStatus.Completed;
                return ret;

            case E_Condition.QUEST_ACTIVE:
                if (string.IsNullOrEmpty(param))
                    return false;
                ret = Managers.Quest.GetQuestStatus(param) == QuestData.E_QuestStatus.InProgress;
                return ret;

            case E_Condition.QUEST_AVAILABLE:
                if (string.IsNullOrEmpty(param))
                    return false;
                ret = Managers.Quest.CanAcceptQuest(param);
                return ret;

            case E_Condition.HAS_ITEM:
                return false;

            case E_Condition.FLAG_SET:
                return false;

            default:
                return false;
        }
    }

    public string GetDialogueID() => Data?.Dialogue_ID;

    public BaseData CaptureSaveData()
    {
        throw new System.NotImplementedException();
    }

    public void RestoreSaveData(BaseData data)
    {
        throw new System.NotImplementedException();
    }
}
