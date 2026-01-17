using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class QuestBoxUI : UI_Base
{
    public enum Images { }

    public enum Texts { QuestType, QuestName, }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindImage(typeof(Images));
        BindText(typeof(Texts));

        return true;
    }

    public void UpdateData(Quest.Data questData)
    {
        Init();
        UpdateQuestType(questData);
        UpdateQuestName(questData);
    }

    private void UpdateQuestType(Quest.Data questData)
    {
        if (questData == null)
        {
            GameObject.Destroy(gameObject);
        }
        else
        {
            GetText((int)Texts.QuestType).gameObject.SetActive(true);
            GetText((int)Texts.QuestType).text = questData.Quest_Type.ToString();
        }
    }

    private void UpdateQuestName(Quest.Data questData)
    {
        if (questData == null)
        {
            GameObject.Destroy(gameObject);
        }
        else
        {
            GetText((int)Texts.QuestName).gameObject.SetActive(true);
            GetText((int)Texts.QuestName).text = questData.Quest_Name;  
        }
    }
}