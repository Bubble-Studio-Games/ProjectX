using Data;
using UnityEngine;

/// <summary>
/// NPC 데이터 - 대화 ID, 상태, 통신 스택 등 관리
/// </summary>
[System.Serializable]
public class NPCData : BaseData
{
    public string DialogueID;
    public bool IsCompleted;
    public int EndComunicationStack;
    public int StartComunicationStack;

    public static NPCData CreateDefault()
    {
        var ret = new NPCData
        {
            DialogueID = string.Empty,
            IsCompleted = false,
            EndComunicationStack = 0,
            StartComunicationStack = 0
        };
        return ret;
    }

    public static NPCData Create(string dialogueID)
    {
        var ret = new NPCData
        {
            DialogueID = dialogueID,
            IsCompleted = false,
            EndComunicationStack = 0,
            StartComunicationStack = 0
        };
        return ret;
    }
}
