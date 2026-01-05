using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class MenuUI : UI_Popup
{
    /// <summary>
    /// MenuUI 사용 컨텍스트 - 어떤 상황에서 사용되는지 정의
    /// </summary>
    public enum MenuContext
    {
        StartScreen,    // 타이틀 화면
        InGamePaused,   // 게임 중 일시정지
    }

    public enum Buttons
    {
        Continue_Btn,
        NewGame_Btn,
        Save_Btn,
        SaveSlot_Btn,
        Setting_Btn,
        GameChallenges_Btn,
        Quit_Btn,
        GoToLobby_Btn,
    }

    public enum GameObjects
    {
        SaveSlotPanel,
        SettingsPanel,
        GameChallengesPanel
    }

    public enum Sliders
    {
        MasterAudioSlider,
        BGMAudioSlider,
        EffectAudioSlider
    }

    [Header("Panels")]
    private SaveSlotPanel _saveSlotPanel;
    private SettingsPanel _settingsPanel;
    private GameChallengesPanel _gameChallengesPanel;

    [Header("Animator")]
    public Animator m_Animator;

    private MenuContext _context;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindButton(typeof(Buttons));
        BindObject(typeof(GameObjects));
        BindSlider(typeof(Sliders));

        InitPanels();
        InitButtons();
        InitPopups();
        InitSliders();
        m_Animator = GetComponent<Animator>();

        return true;
    }

    public void OnEnable()
    {
        Active(_saveSlotPanel.gameObject, false);
        Active(_settingsPanel.gameObject, false);
        Active(_gameChallengesPanel.gameObject, false);
    }

    /// <summary>
    /// 각 패널 컴포넌트 초기화
    /// </summary>
    private void InitPanels()
    {
        _saveSlotPanel = GetObject((int)GameObjects.SaveSlotPanel).GetComponent<SaveSlotPanel>();
        _settingsPanel = GetObject((int)GameObjects.SettingsPanel).GetComponent<SettingsPanel>();
        _gameChallengesPanel = GetObject((int)GameObjects.GameChallengesPanel).GetComponent<GameChallengesPanel>();

        _saveSlotPanel?.Init();
        _saveSlotPanel?.Setup(() => RefreshUI());
        _settingsPanel?.Init();
        _gameChallengesPanel?.Init();



        PressButtonSetAction(GetButton((int)Buttons.SaveSlot_Btn), () =>
        {
            if (_saveSlotPanel == null)
                return;
            Active(_saveSlotPanel?.gameObject, _saveSlotPanel.gameObject.activeSelf == false);
        });

        PressButtonSetAction(GetButton((int)Buttons.Setting_Btn), () =>
        {
            if (_settingsPanel == null)
                return;
            Active(_settingsPanel.gameObject, _settingsPanel.gameObject.activeSelf == false);
        });

        PressButtonSetAction(GetButton((int)Buttons.GameChallenges_Btn), () =>
        {
            if (_gameChallengesPanel == null)
                return;
            Active(_gameChallengesPanel.gameObject, _gameChallengesPanel.gameObject.activeSelf == false);
        });

        Active(_saveSlotPanel.gameObject, false);
        Active(_settingsPanel.gameObject, false);
        Active(_gameChallengesPanel.gameObject, false);
    }

    public void SetUp(MenuContext context)
    {
        _context = context;
        ApplyContextSettings();
    }

    /// <summary>
    /// 컨텍스트에 따른 버튼 상태 설정
    /// </summary>
    private void ApplyContextSettings()
    {
        switch (_context)
        {
            case MenuContext.StartScreen:
                ConfigureForStartScreen();
                break;

            case MenuContext.InGamePaused:
                ConfigureForInGamePaused();
                break;
        }
    }

    /// <summary>
    /// 타이틀 화면 설정 - 계속하기, 새게임, 종료
    /// </summary>
    private void ConfigureForStartScreen()
    {
        GetButton((int)Buttons.Continue_Btn).gameObject.SetActive(true);
        GetButton((int)Buttons.NewGame_Btn).gameObject.SetActive(true);
        GetButton((int)Buttons.Save_Btn).gameObject.SetActive(false);
        GetButton((int)Buttons.SaveSlot_Btn).gameObject.SetActive(true);
        GetButton((int)Buttons.Quit_Btn).gameObject.SetActive(true);
    }

    /// <summary>
    /// 게임 중 일시정지 설정 - 계속하기(Resume), 저장, 종료
    /// </summary>
    private void ConfigureForInGamePaused()
    {
        GetButton((int)Buttons.Continue_Btn).gameObject.SetActive(true);
        GetButton((int)Buttons.NewGame_Btn).gameObject.SetActive(false);
        GetButton((int)Buttons.Save_Btn).gameObject.SetActive(true);
        GetButton((int)Buttons.SaveSlot_Btn).gameObject.SetActive(true);
        GetButton((int)Buttons.Quit_Btn).gameObject.SetActive(true);
    }

    private void InitButtons()
    {
        PressButtonSetAction(GetButton((int)Buttons.Continue_Btn), OnContinueButtonClicked);
        PressButtonSetAction(GetButton((int)Buttons.NewGame_Btn), OnNewGameButtonClicked);
        PressButtonSetAction(GetButton((int)Buttons.Save_Btn), OnSaveButtonClicked);
        PressButtonSetAction(GetButton((int)Buttons.Quit_Btn), OnQuitButtonClicked);
        PressButtonSetAction(GetComponentsInChildren<Button>(), OnAnyButtonClicked);
        PressButtonSetAction(GetButton((int)Buttons.GoToLobby_Btn), OnGoToLobbyButtonClicked);
    }

    private async void OnGoToLobbyButtonClicked()
    {
        DisableAllButtons();
        await Managers.Save.SaveAllData();
        _ = Managers.Scene.LoadSceneAsync(Define.Scene.Start);
    }

    /// <summary>
    /// 계속하기 버튼 클릭 - 컨텍스트에 따라 다른 동작
    /// </summary>
    private void OnContinueButtonClicked()
    {
        switch (_context)
        {
            case MenuContext.StartScreen:
                OnContinueFromStart();
                break;

            case MenuContext.InGamePaused:
                OnResumeGame();
                break;
        }
    }

    private void OnContinueFromStart()
    {
        DisableAllButtons();
        var data = Managers.Load.GetContinueSaveData();
        Managers.UI.ClosePopupUI<MenuUI>();
        Managers.Scene.LoadScene(data.LastScene);
    }

    private void OnResumeGame()
    {
        Managers.Game.ResumeGame();
        Managers.UI.ClosePopupUI<MenuUI>();
    }

    private async void OnNewGameButtonClicked()
    {
        DisableAllButtons();
        await Managers.Save.SaveAllData();
        Managers.Game.ResumeGame();
        Managers.UI.ClosePopupUI<MenuUI>();
        await Managers.Scene.LoadSceneAsync(Define.Scene.Camp,
         () => Debug.Log($"씬 전환 완료 {Define.Scene.Camp}"));
    }

    /// <summary>
    /// 저장 버튼 클릭 - 게임 저장 확인 팝업
    /// </summary>
    private void OnSaveButtonClicked()
    {
        Managers.UI.ShowPopupUI<CheckUI>().SetDataCheck(
            OnSaveConfirmed,
            OnSaveCancelled,
            "Did you Save?");
    }

    /// <summary>
    /// 저장 확인 - OK 버튼
    /// </summary>
    private async void OnSaveConfirmed()
    {
        await Managers.Save.AutoSaveSlotAsync();
        RefreshUI();
        Managers.UI.ClosePopupUI<CheckUI>();
    }

    /// <summary>
    /// 저장 취소 - Cancel 버튼
    /// </summary>
    private void OnSaveCancelled()
    {
        Managers.UI.ClosePopupUI<CheckUI>();
    }

    /// <summary>
    /// 종료 버튼 클릭 - 게임 종료 확인 팝업
    /// </summary>
    private void OnQuitButtonClicked()
    {
        Managers.UI.ShowPopupUI<CheckUI>().SetDataCheck(
            OnQuitConfirmed,
            OnQuitCancelled,
            "Did you Save And Quit?");
    }

    /// <summary>
    /// 종료 확인 - OK 버튼
    /// </summary>
    private async void OnQuitConfirmed()
    {
        await Managers.Game.GameSave();
        Managers.Game.ExitGame();
        Managers.UI.ClosePopupUI<CheckUI>();
    }

    /// <summary>
    /// 종료 취소 - Cancel 버튼
    /// </summary>
    private void OnQuitCancelled()
    {
        Managers.UI.ClosePopupUI<CheckUI>();
        Managers.Game.ExitGame();
    }

    /// <summary>
    /// 모든 버튼 클릭 - UI 공통 사운드 재생
    /// </summary>
    private void OnAnyButtonClicked()
    {
        Managers.Sound.Play(SettingManager.Instance.m_UIButtonClickAudioClip);
    }

    private void InitPopups()
    {
        if (_saveSlotPanel != null)
            _saveSlotPanel.UpdateSlotButtons(false);
    }

    private void Active(GameObject gameObject, bool active)
    {
        gameObject.SetActive(active);
    }

    private void DisableAllButtons()
    {
        var buttons = GetComponentsInChildren<Button>();
        foreach (var button in buttons)
            button.interactable = false;
    }

    public override void RefreshUI()
    {
        if (_init == false)
            return;

        if (_saveSlotPanel != null)
            _saveSlotPanel.RefreshUI();
    }

    /// <summary>
    /// 오디오 슬라이더 초기화
    /// </summary>
    private void InitSliders()
    {
        var bgmSlider = GetSlider((int)Sliders.BGMAudioSlider);
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.AddListener(value => OnAudioVolumeChanged(Sound.Bgm, value));
            bgmSlider.value = Managers.Sound.GetAudioSource(Sound.Bgm).volume;
        }

        var effectSlider = GetSlider((int)Sliders.EffectAudioSlider);
        if (effectSlider != null)
        {
            effectSlider.onValueChanged.AddListener(value => OnAudioVolumeChanged(Sound.Effect, value));
            effectSlider.value = Managers.Sound.GetAudioSource(Sound.Effect).volume;
        }
    }

    /// <summary>
    /// 오디오 볼륨 변경 처리
    /// </summary>
    private void OnAudioVolumeChanged(Sound type, float value)
    {
        if (Managers.Sound.GetAudioSource(type) != null)
            Managers.Sound.GetAudioSource(type).volume = value;
    }

    /// <summary>
    /// 지정된 인덱스의 Slider를 반환 - SettingsPanel에서 사용
    /// </summary>
    public Slider GetAudioSlider(int idx)
    {
        return GetSlider(idx);
    }
}
