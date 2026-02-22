using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 세이브 슬롯 관리 패널
/// </summary>
public class SaveSlotPanel : UI_Base
{
    enum Buttons
    {
        CopyBtn,
        DeleteBtn,
        SaveSlotPlayBtn
    }

    enum Texts
    {
        CopyBtnText
    }

    public SaveSlotItem[] Slots;

    public bool IsCopying { get; private set; } = false;
    public bool IsSelectingSlot => _selectedSlotID != -1;
    public int SelectedSlotID => _selectedSlotID;
    public SaveSlotItem ActiveSlot => Slots[_selectedSlotID];

    private int _selectedSlotID = -1;
    private Action _onRefreshUI;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindButton(typeof(Buttons));
        BindText(typeof(Texts));
        InitSlots();
        InitButtons();

        return true;
    }

    /// <summary>
    /// 패널 설정 - MenuUI에서 호출
    /// </summary>
    public void Setup(Action onRefreshUI)
    {
        _onRefreshUI = onRefreshUI;
    }

    private void InitSlots()
    {
        Slots = GetComponentsInChildren<SaveSlotItem>(true);

        for (int i = 0; i < Slots.Length; i++)
            Slots[i].slotID = i;

        _selectedSlotID = -1;
    }

    private void InitButtons()
    {
        PressButtonSetAction(GetButton((int)Buttons.CopyBtn), OnCopyButtonClicked);
        PressButtonSetAction(GetButton((int)Buttons.DeleteBtn), OnDeleteButtonClicked);
        PressButtonSetAction(GetButton((int)Buttons.SaveSlotPlayBtn), OnPlayButtonClicked);
    }

    /// <summary>
    /// 복사 버튼 클릭 - 슬롯 복사 시작
    /// </summary>
    private void OnCopyButtonClicked()
    {
        CopyStart();
    }

    /// <summary>
    /// 삭제 버튼 클릭 - 삭제 확인 팝업
    /// </summary>
    private void OnDeleteButtonClicked()
    {
        Managers.UI.ShowPopupUI<CheckUI>().SetDataCheck(
            OnDeleteConfirmed,
            OnDeleteCancelled,
            "Are you sure you want to delete your save file?"
        );
    }

    /// <summary>
    /// 삭제 확인 - OK 버튼
    /// </summary>
    private async void OnDeleteConfirmed()
    {
        await Managers.Game.DeleteSlotAsync(_selectedSlotID);
        Slots[_selectedSlotID].RefreshUI();
        RefreshUI();
        Managers.UI.ClosePopupUI<CheckUI>();
    }

    /// <summary>
    /// 삭제 취소 - Cancel 버튼
    /// </summary>
    private void OnDeleteCancelled()
    {
        Managers.UI.ClosePopupUI<CheckUI>();
    }

    /// <summary>
    /// 플레이 버튼 클릭 - 통계 저장 후 패널 닫기
    /// </summary>
    private async void OnPlayButtonClicked()
    {
        await Managers.Game.SavePlayStatistics();
        gameObject.SetActive(false);
    }

    public override void RefreshUI()
    {
        if (_init == false)
            return;

        for (int i = 0; i < 3; i++)
            Slots[i].RefreshUI();

        if (IsSelectingSlot)
            UpdateSlotButtons(Slots[_selectedSlotID].m_havingData);
    }

    /// <summary>
    /// 슬롯 선택 상태 업데이트
    /// </summary>
    public void UpdateSlotButtons(bool havingData)
    {
        GetButton((int)Buttons.CopyBtn).gameObject.SetActive(havingData);
        GetButton((int)Buttons.DeleteBtn).gameObject.SetActive(havingData);
    }

    /// <summary>
    /// 슬롯 복사 시작
    /// </summary>
    public void CopyStart()
    {
        GetText((int)Texts.CopyBtnText).text = "Copying...";
        GetButton((int)Buttons.CopyBtn).interactable = false;
        IsCopying = true;
    }

    /// <summary>
    /// 슬롯 복사 완료
    /// </summary>
    public void CopyComplete()
    {
        GetText((int)Texts.CopyBtnText).text = "Copy";
        GetButton((int)Buttons.CopyBtn).interactable = true;
        IsCopying = false;
    }

    /// <summary>
    /// 모든 슬롯 선택 취소
    /// </summary>
    public void CancelAllSlotSelection()
    {
        foreach (var slot in Slots)
        {
            slot.ClickCancle();
        }
    }

    /// <summary>
    /// 슬롯 선택 설정
    /// </summary>
    public void SetSelectedSlot(int slotID)
    {
        _selectedSlotID = slotID;
    }
}
