using UnityEngine;
using UnityEngine.UI;
using static Define;

/// <summary>
/// 설정 패널 - 오디오, 비디오, 게임, 커스텀, 접근성 설정 관리
/// </summary>
public class SettingsPanel : UI_Base
{
    public enum Buttons
    {
        Video_Btn,
        Game_Btn,
        Custom_Btn,
        Accessibility_Btn
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindButton(typeof(Buttons));

        InitButtons();
        return true;
    }


    private void InitButtons()
    {
        // Video - 화면 설정
        // Game - FPS 조절 등
        // Custom - 커스텀 설정
        // Accessibility - 접근성 설정
    }

}
