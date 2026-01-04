using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class RewardUI : UI_Popup, IAsyncCloseable
{
    public enum Texts {  QuestCompleteText, }
    public enum Buttons { RewardButton, }
    public enum GameObjs { Content, }

    private QuestData _currentQuestData;
    private const string ITEM_BOX_PATH = "UI/ItemBoxUI";

    [SerializeField] private float _completeTextDuration = 0.6f;      // 텍스트 페이드인 시간
    [SerializeField] private float _completeTextHoldTime = 1.0f;      // 텍스트 유지 시간
    [SerializeField] private float _completeTextFadeOutDuration = 0.4f; // 텍스트 페이드아웃 시간
    [SerializeField] private float _rewardShowDuration = 0.5f;        // 보상 페이드인 시간
    [SerializeField] private float _rewardShowDelay = 0.1f;           // 보상 아이템 간 딜레이

    private TextMeshProUGUI _completeText;
    private Button _rewardButton;
    private GameObject _contentObj;
    private CanvasGroup _completeTextCanvasGroup;
    private CanvasGroup _rewardButtonCanvasGroup;
    private CanvasGroup _contentCanvasGroup;
    private Sequence _sequence;
    private Action _onRewarded;
    public Action OnClose { get; set; }
    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindButton(typeof(Buttons));
        BindObject(typeof(GameObjs));
        BindText(typeof(Texts));

        _completeText = GetText((int)Texts.QuestCompleteText);
        _rewardButton = GetButton((int)Buttons.RewardButton);
        _contentObj = GetObject((int)GameObjs.Content);

        _completeTextCanvasGroup = _completeText.gameObject.GetOrAddComponent<CanvasGroup>();
        _rewardButtonCanvasGroup = _rewardButton.gameObject.GetOrAddComponent<CanvasGroup>();
        _contentCanvasGroup = _contentObj.GetOrAddComponent<CanvasGroup>();

        _rewardButton.onClick.AddListener(OnRewardButtonClicked);

        return true;
    }

    private void OnDisable()
    {
        _sequence?.Kill();
        _onRewarded = null;
        _currentQuestData = null;
    }

    private void OnDestroy()
    {
        _sequence?.Kill();
        _onRewarded = null;
        _currentQuestData = null;
    }

    public override void ClosePopupUI()
    {
        OnClose?.Invoke();
        OnClose = null;
        base.ClosePopupUI();
    }


    public void SetUp(QuestData questData, Action onRewarded = default)
    {
        _onRewarded = null;
        _currentQuestData = questData;
        _onRewarded = onRewarded;
        ShowAnimation();
    }

    public void ShowAnimation(Action onComplete = default)
    {
        _sequence?.Kill();

        // 초기 상태 설정
        _completeTextCanvasGroup.alpha = 0f;
        _rewardButtonCanvasGroup.alpha = 0f;
        _contentCanvasGroup.alpha = 0f;

        _completeText.transform.localScale = Vector3.zero;
        _rewardButton.gameObject.SetActive(false);
        _contentObj.SetActive(false);

        // 애니메이션 시퀀스 구성
        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            // Step 1: "퀘스트 완료!" 텍스트 페이드인 + 스케일
            .Append(_completeTextCanvasGroup.DOFade(1f, _completeTextDuration))
            .Join(_completeText.transform.DOScale(1f, _completeTextDuration).SetEase(Ease.OutBack))

            // Step 2: 텍스트 유지
            .AppendInterval(_completeTextHoldTime)

            // Step 3: 텍스트 페이드아웃
            .Append(_completeTextCanvasGroup.DOFade(0f, _completeTextFadeOutDuration))
            .Join(_completeText.transform.DOScale(0.8f, _completeTextFadeOutDuration).SetEase(Ease.InBack))

            // Step 4: 보상 버튼 활성화 및 페이드인
            .AppendCallback(() =>
            {
                _rewardButton.gameObject.SetActive(true);
                _contentObj.SetActive(true);
            })
            .Append(_rewardButtonCanvasGroup.DOFade(1f, _rewardShowDuration).SetEase(Ease.OutCubic))
            .Join(_contentCanvasGroup.DOFade(1f, _rewardShowDuration).SetEase(Ease.OutCubic))
            .OnComplete(() =>
            {
                onComplete?.Invoke();
            });
    }

    private void OnRewardButtonClicked()
    {
        if (_currentQuestData == null)
        {
            Debug.LogWarning("[RewardUI] 보상 데이터가 없습니다.");
            return;
        }

        _onRewarded?.Invoke();
        Managers.UI.ClosePopupUI<RewardUI>();
    }

}

