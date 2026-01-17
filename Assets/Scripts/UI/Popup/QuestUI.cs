using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestUI : UI_Popup
{
    private enum Buttons
    {
        RewardButton,
        CloseButton,
    }

    private enum GameObj
    {
        Content,
        Description,
    }

    private RectTransform _parent;
    private RectTransform _descriptionPanel;
    private Button _rewardButton;
    private Button _closeButton;

    private List<QuestBoxUI> _missionItems = new();
    private QuestBoxUI _selectedQuestBox;

    private void OnEnable()
    {
        UpdateMainQuestData();
    }

    private void OnDisable()
    {
        HideDescription();
        ClearQuestItems();
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindObject(typeof(GameObj));
        BindButton(typeof(Buttons));
        _parent = GetObject((int)GameObj.Content).GetComponent<RectTransform>();
        _descriptionPanel = GetObject((int)GameObj.Description).GetComponent<RectTransform>();
        _rewardButton = GetButton((int)Buttons.RewardButton);
        _closeButton = GetButton((int)Buttons.CloseButton);

        _rewardButton.onClick.AddListener(OnRewardButtonClicked);
        _closeButton.onClick.AddListener(OnCloseButtonClicked);

        _descriptionPanel.gameObject.SetActive(false);
        return true;
    }

    private void OnDestroy()
    {
        _rewardButton.onClick.RemoveAllListeners();
        _closeButton.onClick.RemoveAllListeners();
        _rewardButton = null;
        _closeButton = null;
        _parent = null;
        _descriptionPanel = null;
        _missionItems?.Clear();
        _missionItems = null;
    }

    private void OnRewardButtonClicked()
    {
    }

    private void OnCloseButtonClicked()
    {
        ClosePopupUI();
    }

    private void UpdateMainQuestData()
    {
        ClearQuestItems();

        var mainQuestData = Managers.Quest.MainQuestData;
        if (mainQuestData == null)
            return;

        var questBoxUI = Managers.Resource.Instantiate(Define.QUEST_BOX_UI, _parent);
        var questBoxComponent = questBoxUI.GetOrAddComponent<QuestBoxUI>();
        questBoxComponent.UpdateData(mainQuestData);

        // QuestBoxUI 클릭 이벤트 등록
        BindEvent(questBoxUI, () => OnQuestBoxClicked(questBoxComponent));

        _missionItems.Add(questBoxComponent);
    }

    private void OnQuestBoxClicked(QuestBoxUI clickedQuestBox)
    {
        // 같은 퀘스트를 다시 클릭한 경우
        if (_selectedQuestBox == clickedQuestBox)
        {
            HideDescription();
            return;
        }

        // 다른 퀘스트를 클릭한 경우
        _selectedQuestBox = clickedQuestBox;
        ShowDescription();
    }

    private void ShowDescription()
    {
        _descriptionPanel.gameObject.SetActive(true);
    }

    private void HideDescription()
    {
        _descriptionPanel.gameObject.SetActive(false);
        _selectedQuestBox = null;
    }

    /// <summary>
    /// 기존 퀘스트 아이템 제거
    /// </summary>
    private void ClearQuestItems()
    {
        foreach (var item in _missionItems)
        {
            if (item != null && item.gameObject != null)
                Managers.Resource.Destroy(item.gameObject);
        }
        _missionItems.Clear();
    }

}
