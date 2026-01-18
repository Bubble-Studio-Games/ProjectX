using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대화 선택지 아이템 UI
/// </summary>
public class ChoiceBoxUI : UI_Base
{
    private enum Texts { ChoiceText, }

    private TextMeshProUGUI _choiceText;
    private Button _choiceButton;
    private Dialogue.Data _choiceData;
    private Action<Dialogue.Data> _onChoiceSelected;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindText(typeof(Texts));

        _choiceButton = this.gameObject.GetOrAddComponent<Button>();
        _choiceText = GetText((int)Texts.ChoiceText);
        return true;
    }

    protected override void OnDestroy()
    {
        _onChoiceSelected = null;
        _choiceData = null;
        _choiceButton.onClick.RemoveAllListeners();
        _choiceButton = null;
        _choiceText = null;
    }

    public void SetUp(Dialogue.Data choiceData, Action<Dialogue.Data> onChoiceSelected)
    {
        _onChoiceSelected = onChoiceSelected;
        _choiceData = choiceData;
        _choiceText.text = choiceData.Dialogue_Text;
        _choiceButton.onClick.AddListener(() => _onChoiceSelected?.Invoke(_choiceData));
    }
}
