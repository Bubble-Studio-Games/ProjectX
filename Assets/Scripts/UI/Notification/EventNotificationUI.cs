using TMPro;
using UnityEngine;

/// <summary>
/// 게임 이벤트 알림 토스트 UI - 화면 상단에 간단한 메시지 표시 후 자동 소멸
/// </summary>
public class EventNotificationUI : UI_Notification
{
    private enum Texts { MessageText }
    private enum GameObjects { NotificationPanel }

    private const float DISPLAY_DURATION = 3f;
    private const float FADE_IN_DURATION = 0.3f;
    private const float FADE_OUT_DURATION = 0.3f;

    private TextMeshProUGUI _messageText;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindText(typeof(Texts));
        BindObject(typeof(GameObjects));

        _messageText = GetText((int)Texts.MessageText);

        ShowWithFade(FADE_IN_DURATION);
        AutoClose(DISPLAY_DURATION, FADE_OUT_DURATION);

        return true;
    }

    /// <summary>
    /// 알림 메시지 설정
    /// </summary>
    public void SetMessage(string message)
    {
        if (_messageText != null)
            _messageText.text = message;
    }
}
