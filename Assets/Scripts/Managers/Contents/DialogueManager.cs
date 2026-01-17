using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static Define;

public class DialogueManager
{
    private const string LINE_TYPE_CHOICE = "Choice";
    private const string SPEAKER_TYPE_SYSTEM = "System";

    private Dictionary<string, List<Dialogue.Data>> _dialogueData;
    public bool IsDialogueContext { get; private set; } = false;
    public string CurDialogueId { get; private set; } = string.Empty;
    private bool _isDialogueInputSubscribed = false;

    public event Action<List<Dialogue.Data>> OnDialogueStarted;
    public event Action<List<Dialogue.Data>> OnDialogueEnded;
    public event Action<string, List<Dialogue.Data>> OnLineChanged;
    public event Action<string, List<Dialogue.Data>> OnTransitionToSpeaker;

    private bool _isTyping = false;
    private bool _isWaitingChoice = false;
    private TaskCompletionSource<bool> _inputTCS;
    private DialogueUI _dialogueUI;
    private readonly Stack<IAsyncCloseable> _openAsyncUIStack = new();
    private Action _onStarted;
    private Action _onEnded;
    private CancellationTokenSource _dialogueCTS;

    IInputActionMapController maps => Managers.SceneServices.InputActionMapController;

    public void Init()
    {
        _dialogueData = Managers.Data.DialogueData;
    }

    public void Clear()
    {
        _onStarted = null;
        _onEnded = null;
        _dialogueData = null;
        _dialogueUI = null;
        _inputTCS = null;
        _openAsyncUIStack.Clear();
        OnDialogueStarted = null;
        OnDialogueEnded = null;
        OnLineChanged = null;
        OnTransitionToSpeaker = null;
        _dialogueCTS?.Dispose();
        _dialogueCTS = null;
    }

    #region Input 처리

    /// <summary>
    /// Dialogue 입력 이벤트 구독
    /// </summary>
    private void SubscribeInput()
    {
        if (_isDialogueInputSubscribed)
            return;

        _isDialogueInputSubscribed = true;

        var events = Managers.SceneServices.InputEvents;

        events.Subscribe(E_InputEvent.DialogueSubmit, OnDialogueSubmit);
        events.Subscribe(E_InputEvent.DialogueCancel, OnDialogueESC);
    }

    /// <summary>
    /// Dialogue 입력 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeInput()
    {
        if (_isDialogueInputSubscribed == false)
            return;

        var events = Managers.SceneServices.InputEvents;

        events.Unsubscribe(E_InputEvent.DialogueSubmit, OnDialogueSubmit);
        events.Unsubscribe(E_InputEvent.DialogueCancel, OnDialogueESC);

        _isDialogueInputSubscribed = false;
    }

    private void Internal_OnSubmit()
    {
        // 선택지 대기 중일 때는 입력 무시
        if (_isWaitingChoice)
            return;

        if (_isTyping)
            SkipTypingText();
        else
            _inputTCS?.TrySetResult(true);
    }

    /// <summary>
    /// Dialogue Submit 입력 처리 - Enter, Space, LeftClick
    /// </summary>
    private void OnDialogueSubmit()
    {
        Internal_OnSubmit();
    }

    /// <summary>
    /// Dialogue ESC 입력 처리 - ESC
    /// </summary>
    private void OnDialogueESC()
    {
        if (IsDialogueContext == false)
            return;

        // 열려있는 비동기 UI가 있으면 역순으로 모두 닫기
        if (_openAsyncUIStack.Count > 0)
        {
            CloseAllOpenAsyncUIs();
            return;
        }

        // 일반 대화 진행 (타이핑 스킵 또는 다음 대사)
        Internal_OnSubmit();
    }

    #endregion

    /// <summary>
    /// 대화 시작
    /// </summary>
    public void StartDialogue(string dialogueId, string entryPoint, Action onStarted = default, Action onEnded = default)
    {
        if (IsDialogueContext)
        {
            Debug.LogWarning("이미 다이얼로그가 진행중입니다.");
            Debug.LogWarning($"현재 다이얼로그 ID: {CurDialogueId}");
            return;
        }

        Init();

        if (_dialogueData.TryGetValue(dialogueId, out var dialogueData) == false)
        {
            Debug.LogError("다이얼로그 데이터를 찾을 수 없습니다. : " + dialogueId);
            return;
        }

        IsDialogueContext = true;
        CurDialogueId = dialogueId;

        // ActionMap을 Dialogue로 전환

        maps.PushActionMapGroup(Define.E_InputActionMap.Dialogue);
        SubscribeInput();

        _dialogueUI = Managers.UI.ShowPopupUI<DialogueUI>();
        OnDialogueStarted?.Invoke(dialogueData);

        // entryPoint에 해당하는 시작 인덱스 결정
        int startIndex = FindEntryPointIndex(dialogueData, entryPoint);
        _onStarted = onStarted;
        _onEnded = onEnded;

        _dialogueCTS?.Cancel();
        _dialogueCTS?.Dispose();
        _dialogueCTS = new CancellationTokenSource();

        _onStarted?.Invoke();
        RunDialogue(dialogueData, startIndex, _dialogueCTS.Token);
    }

    /// <summary>
    /// 대화 강제 취소
    /// </summary>
    public void CancelDialogue()
    {
        if (IsDialogueContext == false)
            return;

        _dialogueCTS?.Cancel();
    }

    /// <summary>
    /// Entry_Point 값으로 시작 인덱스 찾기
    /// </summary>
    private int FindEntryPointIndex(List<Dialogue.Data> dialogueData, string entryPoint)
    {
        if (string.IsNullOrEmpty(entryPoint))
            return 0;

        for (int i = 0; i < dialogueData.Count; i++)
        {
            if (dialogueData[i].Entry_Point == entryPoint)
                return i;
        }

        Debug.LogWarning($"Entry_Point '{entryPoint}'를 찾을 수 없습니다. 처음부터 시작합니다.");
        return 0;
    }

    private async void RunDialogue(List<Dialogue.Data> dialogueData, int startIndex, CancellationToken ct)
    {
        try
        {
            await ShowDialogueUIAsync(ct);

            int curIndex = startIndex;
            while (curIndex >= 0 && curIndex < dialogueData.Count)
            {
                ct.ThrowIfCancellationRequested();

                var data = dialogueData[curIndex];

                // Choice 타입인 경우 선택지 처리
                if (data.Line_Type.Equals(LINE_TYPE_CHOICE, StringComparison.OrdinalIgnoreCase))
                {
                    var selectedChoice = await ShowChoicesAsync(curIndex, dialogueData, ct);

                    // 선택한 Choice의 Next_Line으로 점프
                    curIndex = FindLineIndex(dialogueData, selectedChoice.Next_Line);
                    continue;
                }

                // 일반 대화 표시
                await DisplayLineAsync(data, ct);

                // Action 실행
                var action = await ExecuteActionAsync(data.Action, data.Action_Param, ct);
                if (action == E_Action.EXIT)
                {
                    await HideDialogueUIAsync(ct);
                    EndDialogue();
                    return;
                }

                // Next_Line으로 점프 (-1이면 종료)
                if (data.Next_Line == -1)
                    break;

                curIndex = FindLineIndex(dialogueData, data.Next_Line);
            }

            await HideDialogueUIAsync(ct);
            EndDialogue();
        }
        catch (OperationCanceledException)
        {
            // 취소 시 정리 작업
            Debug.Log("대화가 취소되었습니다.");
            EndDialogue();
        }
    }

    /// <summary>
    /// UI 등장 애니메이션 대기
    /// </summary>
    private async Task ShowDialogueUIAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<bool>();
        using (ct.Register(() => tcs.TrySetCanceled()))
        {
            _dialogueUI.ShowAnimation(() => tcs.TrySetResult(true));
            await tcs.Task;
        }
    }

    /// <summary>
    /// UI 퇴장 애니메이션 대기
    /// </summary>
    private async Task HideDialogueUIAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<bool>();
        using (ct.Register(() => tcs.TrySetCanceled()))
        {
            _dialogueUI.HideAnimation(() => tcs.TrySetResult(true));
            await tcs.Task;
        }
    }

    /// <summary>
    /// 텍스트 타이핑 대기
    /// </summary>
    private async Task TypingTextAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<bool>();
        using (ct.Register(() => tcs.TrySetCanceled()))
        {
            _isTyping = true;
            _dialogueUI.TypingText(() =>
            {
                _isTyping = false;
                tcs.TrySetResult(true);
            });
            await tcs.Task;
        }
    }

    /// <summary>
    /// 사용자 입력 대기
    /// </summary>
    private async Task WaitForInputAsync(CancellationToken ct)
    {
        _inputTCS = new TaskCompletionSource<bool>();
        using (ct.Register(() => _inputTCS.TrySetCanceled()))
        {
            await _inputTCS.Task;
        }
        _inputTCS = null;
    }

    /// <summary>
    /// 단일 대화 라인 표시 - 포트레이트 업데이트 + 타이핑 + 입력 대기
    /// </summary>
    private async Task DisplayLineAsync(Dialogue.Data data, CancellationToken ct)
    {
        _dialogueUI.UpdateDialogueData(data);
        await TypingTextAsync(ct);
        await WaitForInputAsync(ct);
    }

    /// <summary>
    /// 연속된 선택지 수집 및 선택 대기 - 선택된 데이터 반환
    /// </summary>
    private async Task<Dialogue.Data> ShowChoicesAsync(int startIndex, List<Dialogue.Data> dialogueData, CancellationToken ct)
    {
        var choices = new List<Dialogue.Data>();
        int currentIndex = startIndex;

        // 연속된 Choice 데이터를 수집후 List 할당
        while (currentIndex < dialogueData.Count &&
               dialogueData[currentIndex].Line_Type.Equals(LINE_TYPE_CHOICE, StringComparison.OrdinalIgnoreCase) &&
               dialogueData[currentIndex].Speaker_Type.Equals(SPEAKER_TYPE_SYSTEM, StringComparison.OrdinalIgnoreCase))
        {
            choices.Add(dialogueData[currentIndex]);
            currentIndex++;
        }

        _isWaitingChoice = true;
        var tcs = new TaskCompletionSource<Dialogue.Data>();
        using (ct.Register(() => tcs.TrySetCanceled()))
        {
            _dialogueUI.ShowChoices(choices, (choiceData) =>
            {
                tcs.TrySetResult(choiceData);
            });
            await tcs.Task;
        }
        _isWaitingChoice = false;

        var ret = tcs.Task.Result;
        return ret;
    }

    /// <summary>
    /// Line_Index 값으로 배열의 실제 인덱스 찾기
    /// </summary>
    private int FindLineIndex(List<Dialogue.Data> dialogueData, int lineIndex)
    {
        for (int i = 0; i < dialogueData.Count; i++)
        {
            if (dialogueData[i].Line_Index == lineIndex)
                return i;
        }
        return -1;
    }

    public void EndDialogue()
    {
        if (IsDialogueContext == false)
        {
            Debug.LogWarning("다이얼로그가 진행중이지 않습니다.");
            return;
        }

        // Input 구독 해제
        UnsubscribeInput();
        maps.PopActionMapGroup();

        OnDialogueEnded?.Invoke(null);

        // 완료 콜백 호출
        _onEnded?.Invoke();

        IsDialogueContext = false;
        CurDialogueId = string.Empty;
        _dialogueUI?.ClosePopupUI();
        _dialogueUI = null;
        Clear();
    }

    private void SkipTypingText()
    {
        if (_isTyping)
            _dialogueUI?.SkipTyping();
    }

    /// <summary>
    /// 열려있는 모든 비동기 UI를 역순으로 닫기
    /// </summary>
    private void CloseAllOpenAsyncUIs()
    {
        while (_openAsyncUIStack.Count > 0)
        {
            var asyncUI = _openAsyncUIStack.Pop();
            if (asyncUI is UI_Popup popup)
                popup.ClosePopupUI();
        }
    }

    /// <summary>
    /// 다이얼로그 액션 실행
    /// </summary>
    private async Task<E_Action> ExecuteActionAsync(E_Action action, string param, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        switch (action)
        {
            case E_Action.SHOP_OPEN:
                await HideDialogueUIAsync(ct);
                await ExecuteShopOpenAsync(param, ct);
                await ShowDialogueUIAsync(ct);
                break;

            case E_Action.QUEST_START:
                if (string.IsNullOrEmpty(param) == false)
                    Managers.Quest.AcceptQuest(param);
                break;

            case E_Action.QUEST_COMPLETE:
                if (string.IsNullOrEmpty(param) == false)
                    Managers.Quest.CompleteQuest(param);
                break;

            case E_Action.GIVE_REWARD:
                await ExecuteRewardOpenAsync(ct);
                break;




            case E_Action.REPAIR_OPEN:
                // TODO: 수리 창 열기 구현
                Debug.LogWarning("REPAIR_OPEN 액션 실행");
                break;

            case E_Action.CHECK_ITEM:
                // TODO: 아이템 체크 구현
                Debug.LogWarning($"CHECK_ITEM 액션 실행: {param}");
                break;

            case E_Action.CRAFT_OPEN:
                // TODO: 제작 창 열기 구현
                Debug.LogWarning("CRAFT_OPEN 액션 실행");
                break;

            case E_Action.CHECK_QUEST:
                // TODO: 퀘스트 체크 구현
                Debug.LogWarning($"CHECK_QUEST 액션 실행: {param}");
                break;

            case E_Action.CHECK_OBJECTIVE:
                // TODO: 목표 체크 구현
                Debug.LogWarning($"CHECK_OBJECTIVE 액션 실행: {param}");
                break;

            case E_Action.UNLOCK_QUEST:
                // TODO: 퀘스트 잠금 해제 구현
                Debug.LogWarning($"UNLOCK_QUEST 액션 실행: {param}");
                break;

            case E_Action.CHECK_GOLD:
                // TODO: 골드 체크 구현
                Debug.LogWarning($"CHECK_GOLD 액션 실행: {param}");
                break;

            case E_Action.CONSUME_GOLD:
                // TODO: 골드 소비 구현
                Debug.LogWarning($"CONSUME_GOLD 액션 실행: {param}");
                break;

            case E_Action.UNLOCK_INFO:
                // TODO: 정보 잠금 해제 구현
                Debug.LogWarning($"UNLOCK_INFO 액션 실행: {param}");
                break;

            case E_Action.UNLOCK_MAP:
                // TODO: 맵 잠금 해제 구현
                Debug.LogWarning($"UNLOCK_MAP 액션 실행: {param}");
                break;
            default:
                break;
        }

        return action;
    }

    private async Task ExecuteShopOpenAsync(string shopId, CancellationToken ct)
    {
        var (shopUI, inventoryUI) = Managers.Shop.OpenShop(shopId);
        if (shopUI == null || inventoryUI == null)
            return;

        _openAsyncUIStack.Push(shopUI);
        _openAsyncUIStack.Push(inventoryUI);

        await IAsyncCloseableEx.WaitForUICloseAsync(ct, shopUI, inventoryUI);

        _openAsyncUIStack.Clear();
        Managers.Shop.OnShopClosed();
    }

    private async Task ExecuteRewardOpenAsync(CancellationToken ct)
    {
        // 메인 퀘스트 UI 숨기기
        var tcs = new TaskCompletionSource<bool>();
        using (ct.Register(() => tcs.TrySetCanceled()))
        {
            Managers.Quest.HideMainQuestUI(() => tcs.TrySetResult(true));
            await tcs.Task;
        }

        // 보상 UI 표시 및 닫힘 대기
        var rewardUI = Managers.Quest.ShowQuestReward();
        if (rewardUI != null)
            await IAsyncCloseableEx.WaitForUICloseAsync(ct, rewardUI);
    }
}

public static class IAsyncCloseableEx
{
    /// <summary>
    /// UI 닫힘 대기 - 여러 UI가 모두 닫힐 때까지 비동기 대기
    /// </summary>
    public static async Task WaitForUICloseAsync(CancellationToken ct, params Define.IAsyncCloseable[] closeables)
    {
        var registrations = new List<CancellationTokenRegistration>();

        try
        {
            var tasks = closeables.Select(ui =>
            {
                var tcs = new TaskCompletionSource<bool>();

                if (ct.CanBeCanceled)
                {
                    var registration = ct.Register(() => tcs.TrySetCanceled());
                    registrations.Add(registration);
                }

                ui.OnClose = null;
                ui.OnClose = () => tcs.TrySetResult(true);
                return tcs.Task;
            }).ToArray();

            await Task.WhenAll(tasks);
        }
        finally
        {
            foreach (var registration in registrations)
                registration.Dispose();
        }
    }
}
