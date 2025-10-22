using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NPC 등장 알림
/// - 화면 상단에 간단한 메시지를 표시
/// - 3초 후 자동으로 사라짐
/// </summary>
public class UI_NPCNotification : UI_Popup
{
    public enum Texts { MessageText }
    public enum GameObjects { NotificationPanel }

    private float _displayDuration = 3f;

    private void Awake()
    {
        this.gameObject.SetActive(false);
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindText(typeof(Texts));
        BindObject(typeof(GameObjects));

        RunFade();
        return true;
    }

    private void RunFade()
    {
        var panel = GetObject((int)GameObjects.NotificationPanel);
        var canvasGroup = panel.GetOrAddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        
        IEnumerator fadeOut = FadeOut(canvasGroup, onComplete: () => ClosePopupUI());
        StartCoroutine(FadeIn(canvasGroup, onComplete: () => StartCoroutine(fadeOut)));
    }

    public void SetMessage(string message)
    {
        //GetText((int)Texts.MessageText).text = message;
    }

    private IEnumerator FadeIn(CanvasGroup canvasGroup, Action onComplete = default)
    {
        float elapsed = 0f;
        float fadeTime = 0.3f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeTime);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        onComplete?.Invoke();
    }

    private IEnumerator FadeOut(CanvasGroup canvasGroup, Action onComplete = default)
    {
        yield return new WaitForSeconds(_displayDuration);

        float elapsed = 0f;
        float fadeTime = 0.3f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        onComplete?.Invoke();
    }

    public override void ClosePopupUI()
    {
        StopAllCoroutines();
        base.ClosePopupUI();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
