using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MainQuestUI : UI_Base
{
    private enum Texts { QuestText, }

    public enum Sliders { Slider, }

    public enum EditorButtons { TestCompleteButton, }

    [SerializeField] private float _inDuration = 0.35f;
    [SerializeField] private float _outDuration = 0.20f;
    [SerializeField] private float _yOffset = 40f;

    private CanvasGroup _canvasGroup;
    private RectTransform _root;
    private Sequence _sequence;
    private Vector2 _targetAnchoredPosition;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindText(typeof(Texts));
        BindSlider(typeof(Sliders));
        BindButton(typeof(EditorButtons));

        _root = GetComponent<RectTransform>();
        _canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();

        _targetAnchoredPosition = _root.anchoredPosition;
        this.gameObject.SetActive(false);

    #if UNITY_EDITOR
        GetButton((int)EditorButtons.TestCompleteButton).onClick.AddListener(() =>
        {
            OnTestButtonClicked();
        });
    #else
        GetButton((int)EditorButtons.TestCompleteButton).gameObject.SetActive(false);
    #endif

        return true;
    }

#if UNITY_EDITOR
    private void OnTestButtonClicked()
    {
        GetText((int)Texts.QuestText).text = "퀘스트 완료 NPC에게 돌아가기";
        Managers.Quest.ForceCompleteQuest();
    }
#endif

    public void SetUp()
    {

    }

    public void Clear()
    {

    }

    public void UpdateData(Quest.Data data)
    {
        GetText((int)Texts.QuestText).text = data.Quest_Name;
        GetSlider((int)Sliders.Slider).maxValue = data.Quest_Goal_Count;
    }

    public void ShowAnimation(Action onComplete = default)
    {
        _sequence?.Kill();

        gameObject.SetActive(true);
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        var startPosition = new Vector2(_targetAnchoredPosition.x, _targetAnchoredPosition.y + _yOffset);
        _root.anchoredPosition = startPosition;
        _root.localScale = Vector3.one * 0.98f;

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(_canvasGroup.DOFade(1f, _inDuration))
            .Join(_root.DOAnchorPos(_targetAnchoredPosition, _inDuration).SetEase(Ease.OutCubic))
            .Join(_root.DOScale(1f, _inDuration).SetEase(Ease.OutBack))
            .OnComplete(() =>
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                onComplete?.Invoke();
            });
    }

    public void HideAnimation(Action onComplete = default)
    {
        _sequence?.Kill();

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        var endPosition = new Vector2(_targetAnchoredPosition.x, _targetAnchoredPosition.y + _yOffset);

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(_canvasGroup.DOFade(0f, _outDuration))
            .Join(_root.DOAnchorPos(endPosition, _outDuration).SetEase(Ease.InCubic))
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _sequence?.Kill();
    }
}
