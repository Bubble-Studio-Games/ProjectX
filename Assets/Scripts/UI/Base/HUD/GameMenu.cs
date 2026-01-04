using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameMenu : UI_Base
{
    public enum Buttons
    {
        Quest,
        Inventory,
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindButton(typeof(Buttons));
        GetButton((int)Buttons.Quest).onClick.AddListener(OnQuestButtonClicked);
        GetButton((int)Buttons.Inventory).onClick.AddListener(OnInventoryButtonClicked);

        return true;
    }

    private void OnQuestButtonClicked()
    {
        Managers.UI.ShowPopupUI<QuestUI>();
    }

    private void OnInventoryButtonClicked()
    {
        Managers.UI.ShowPopupUI<InventoryUI>();
    }
}
