using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class DotTMPTextEx
{
    public static Tweener DOText(this TextMeshProUGUI target, string typingText,float duration)
    {
        target.text = typingText;
        target.maxVisibleCharacters = 0;
        return DOTween.To(x => target.maxVisibleCharacters = (int)x, 0, target.text.Length, duration);
    }
}


/// <summary>
/// NPC 대화 시스템 UI
/// </summary>
public class DialogueUI : UI_Popup
{
    private enum Images { NPCPortrait_Image, PlayerPortrait_Image, }

    private enum Texts { Dialogue_Text, }

    private enum GameObjs { Choice, }

    private const string PLAYER = "Player";
    
    private float _dialogueTypingSpeed => GameConfig.Dialogue.TypingText;
    private float _activeSpeakerAlpha => GameConfig.Dialogue.ActiveSpeakerAlpha;
    private float _inactiveSpeakerAlpha => GameConfig.Dialogue.InactiveSpeakerAlpha;
    private float _portraitFadeDuration => GameConfig.Dialogue.PortraitFadeDuration;
    private float _uiAnimDuration => GameConfig.Dialogue.DialogueUIAnimDuration;

    private TextMeshProUGUI _dialogueText;
    private Image _npcPortrait;
    private Image _playerPortrait;
    private GameObject _choice;

    private string _fullDialogueText;

    private Tweener _typingTweener;
    private CanvasGroup _canvasGroup;

    private List<ChoiceBoxUI> _choiceItems = new();
    private Action<Dialogue.Data> _onChoiceSelected;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindImage(typeof(Images));
        BindText(typeof(Texts));
        BindObject(typeof(GameObjs));

        _dialogueText = GetText((int)Texts.Dialogue_Text);
        _npcPortrait = GetImage((int)Images.NPCPortrait_Image);
        _playerPortrait = GetImage((int)Images.PlayerPortrait_Image);
        _choice = GetObject((int)GameObjs.Choice);

        // 기본값 설정
        _playerPortrait.color = new Color(1f, 1f, 1f, _inactiveSpeakerAlpha);
        _npcPortrait.color = new Color(1f, 1f, 1f, _inactiveSpeakerAlpha);
        _dialogueText.text = string.Empty;

        _canvasGroup = this.gameObject.GetOrAddComponent<CanvasGroup>();
        return true;
    }

    protected override void OnDestroy()
    {
        _dialogueText?.DOKill();
        _typingTweener?.Kill();
        _playerPortrait?.DOKill();
        _npcPortrait?.DOKill();
        _canvasGroup?.DOKill();
        base.OnDestroy();
    }

    public void UpdateDialogueData(Dialogue.Data dialogueData)
    {
        _npcPortrait.sprite = Managers.Resource.Load<Sprite>(Define.PORTRAIT_PATH + dialogueData.Right_Portrait);
        _playerPortrait.sprite = Managers.Resource.Load<Sprite>(Define.PORTRAIT_PATH + dialogueData.Left_Portrait);
        _fullDialogueText = dialogueData.Dialogue_Text;

        UpdatePortraitAlpha(dialogueData.Speaker_Type);
    }

    /// <summary>
    /// 화자에 따른 포트레이트 투명도 조절
    /// </summary>
    private void UpdatePortraitAlpha(string speakerType)
    {
        bool isPlayerSpeaking = speakerType.Equals(PLAYER, StringComparison.OrdinalIgnoreCase);

        float playerAlpha = isPlayerSpeaking ? _activeSpeakerAlpha : _inactiveSpeakerAlpha;
        float npcAlpha = isPlayerSpeaking ? _inactiveSpeakerAlpha : _activeSpeakerAlpha;

        _playerPortrait.DOKill();
        _npcPortrait.DOKill();

        _playerPortrait.DOFade(playerAlpha, _portraitFadeDuration).SetUpdate(true);
        _npcPortrait.DOFade(npcAlpha, _portraitFadeDuration).SetUpdate(true);
    }

    public void TypingText(Action onComplete = default)
    {
        _dialogueText.text = string.Empty;

        _typingTweener?.Kill();
        _dialogueText?.DOKill();
        _typingTweener = _dialogueText.DOText(_fullDialogueText, _dialogueTypingSpeed)
            .SetUpdate(true)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                onComplete?.Invoke();
            });
    }

    public void SkipTyping()
    {
        _typingTweener?.Complete();
    }

    public void ShowAnimation(Action onComplete = default)
    {
        _canvasGroup.DOKill();
        _canvasGroup.alpha = 0f;

        _canvasGroup.DOFade(1f, _uiAnimDuration)
            .SetUpdate(true)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void HideAnimation(Action onComplete = default)
    {
        _canvasGroup.DOKill();

        _canvasGroup.DOFade(0f, _uiAnimDuration)
            .SetUpdate(true)
            .SetEase(Ease.InQuad)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 선택지 표시
    /// </summary>
    public void ShowChoices(List<Dialogue.Data> choices, Action<Dialogue.Data> onChoiceSelected)
    {
        ClearChoices();

        _onChoiceSelected = onChoiceSelected;
        _choice.SetActive(true);

        foreach (var choiceData in choices)
        {
            var choiceItem = Managers.Resource.Instantiate(Define.CHOICE_BOX_UI, _choice.transform);
            var choiceItemComponent = choiceItem.GetOrAddComponent<ChoiceBoxUI>();
            choiceItemComponent.SetUp(choiceData, OnChoiceItemClicked);
            _choiceItems.Add(choiceItemComponent);
        }
    }

    /// <summary>
    /// 선택지 숨기기
    /// </summary>
    public void HideChoices()
    {
        ClearChoices();
        _choice.SetActive(false);
    }

    private void ClearChoices()
    {
        foreach (var item in _choiceItems)
        {
            if (item != null && item.gameObject != null)
                Managers.Resource.Destroy(item.gameObject);
        }
        _choiceItems.Clear();
    }

    private void OnChoiceItemClicked(Dialogue.Data choiceData)
    {
        HideChoices();
        _onChoiceSelected?.Invoke(choiceData);
    }

}
