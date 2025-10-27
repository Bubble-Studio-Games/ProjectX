using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NPC 대화 시스템 UI
/// </summary>
public class DialogueUI : UI_Popup
{
    private enum Images
    {
        NPCPortrait_Image,
        PlayerPortrait_Image,
    }

    private enum Texts
    {
        Dialogue_Text,
    }

    private enum Buttons
    {
        ContinueButton,
        SkipButton,
        CloseButton
    }

    [Header("테스트용")]
    [SerializeField] private List<string> _testDummyDialogueTexts;

    [Header("Dialogue Settings")]
    [SerializeField] private float _typingSpeed = 0.05f;        // 타이핑 속도 (초당 글자 수)
    [SerializeField] private float _autoContinueDelay = 2f;     // 자동 진행 대기 시간

    private TextMeshProUGUI _dialogueText;
    private Image _npcPortrait;
    private Image _playerPortrait;
    private Button _continueButton;
    private Button _skipButton;

    private string _fullDialogueText;
    private int _currentCharIndex = 0;
    private bool _isTyping = false;
    private Coroutine _typingCoroutine;
    private Coroutine _autoContinueCoroutine;

    private NPC _currentNPC;
    private int _currentDialogueIndex = 0;  // 테스트용 더미 데이터 인덱스

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindImage(typeof(Images));
        BindText(typeof(Texts));
        BindButton(typeof(Buttons));

        _dialogueText = GetText((int)Texts.Dialogue_Text);
        _npcPortrait = GetImage((int)Images.NPCPortrait_Image);
        _playerPortrait = GetImage((int)Images.PlayerPortrait_Image);
        _continueButton = GetButton((int)Buttons.ContinueButton);
        _skipButton = GetButton((int)Buttons.SkipButton);

        // GetButton((int)Buttons.ContinueButton).onClick.AddListener(OnContinueButtonClicked);
        // GetButton((int)Buttons.SkipButton).onClick.AddListener(OnSkipButtonClicked);
        // GetButton((int)Buttons.CloseButton).onClick.AddListener(CloseDialogue);

        // 초기 상태 설정
        SetDialogueState(false);
        return true;
    }

    public void StartDialogue(NPC npc)
    {
        Init();

        _currentNPC = npc;
        _fullDialogueText = _testDummyDialogueTexts[_currentDialogueIndex];

        SetDialogueState(true);
        StartTypingEffect();
    }

    private void SetDialogueState(bool isActive)
    {
        if (isActive)
            Managers.Game.PauseGame();
        else
            Managers.Game.ResumeGame();
    }

    /// <summary>
    /// 타이핑 효과 시작
    /// </summary>
    private void StartTypingEffect()
    {
        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        _isTyping = true;
        _currentCharIndex = 0;
        _dialogueText.text = "";

        _typingCoroutine = StartCoroutine(TypingCoroutine());

        // 버튼 상태 업데이트
        UpdateButtonStates();
    }

    /// <summary>
    /// 타이핑 코루틴
    /// </summary>
    private IEnumerator TypingCoroutine()
    {
        while (_currentCharIndex < _fullDialogueText.Length)
        {
            _currentCharIndex++;
            _dialogueText.text = _fullDialogueText.Substring(0, _currentCharIndex);

            yield return new WaitForSecondsRealtime(_typingSpeed);
        }

        _isTyping = false;
        UpdateButtonStates();

        // 타이핑 완료 후 자동 진행 대기
        if (_autoContinueCoroutine != null)
            StopCoroutine(_autoContinueCoroutine);

        _autoContinueCoroutine = StartCoroutine(AutoContinueCoroutine());
    }

    /// <summary>
    /// 자동 진행 코루틴
    /// </summary>
    private IEnumerator AutoContinueCoroutine()
    {
        yield return new WaitForSecondsRealtime(_autoContinueDelay);

        if (_isTyping == false)
            OnContinueButtonClicked();
    }

    /// <summary>
    /// 계속 버튼 클릭 처리
    /// </summary>
    private void OnContinueButtonClicked()
    {
        // 타이핑 중이면 즉시 완료
        if (_isTyping)
            CompleteTyping();
        else
            EndDialogue();
    }

    /// <summary>
    /// 건너뛰기 버튼 클릭 처리
    /// </summary>
    private void OnSkipButtonClicked()
    {
        if (_isTyping)
        {
            CompleteTyping();
        }
        else
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// 타이핑 완료 처리
    /// </summary>
    private void CompleteTyping()
    {
        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        _isTyping = false;
        _dialogueText.text = _fullDialogueText;
        _currentCharIndex = _fullDialogueText.Length;

        UpdateButtonStates();

        // 자동 진행 코루틴 시작
        if (_autoContinueCoroutine != null)
            StopCoroutine(_autoContinueCoroutine);

        _autoContinueCoroutine = StartCoroutine(AutoContinueCoroutine());
    }

    /// <summary>
    /// 버튼 상태 업데이트
    /// </summary>
    private void UpdateButtonStates()
    {
        if (_continueButton != null)
            _continueButton.interactable = !_isTyping;

        if (_skipButton != null)
            _skipButton.interactable = true;  // 항상 사용 가능
    }

    /// <summary>
    /// 대화 종료
    /// </summary>
    private void EndDialogue()
    {
        SetDialogueState(false);

        // 다음 대사가 있으면 계속, 없으면 종료
        if (HasNextDialogue())
        {
            _currentDialogueIndex++;

            if (_currentNPC != null)
                StartDialogue(_currentNPC);
            else
                StartDialogue(null);
        }
        else
        {
            CloseDialogue();
        }
    }

    /// <summary>
    /// 다음 대사가 있는지 확인
    /// </summary>
    private bool HasNextDialogue()
    {
        if (_testDummyDialogueTexts != null && _testDummyDialogueTexts.Count > 0)
        {
            return _currentDialogueIndex < _testDummyDialogueTexts.Count;
        }

        return false;
    }

    /// <summary>
    /// 대화창 닫기
    /// </summary>
    private void CloseDialogue()
    {
        SetDialogueState(false);
        _currentDialogueIndex = 0;
        ClosePopupUI();
    }
}
