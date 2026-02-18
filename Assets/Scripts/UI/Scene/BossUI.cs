using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Data;

public class BossUI : UI_Scene
{
    private enum Texts { BossNameText, }
    private enum Sliders { BossSlider, }

    private CanvasGroup _canvasGroup;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindText(typeof(Texts));
        BindSlider(typeof(Sliders));

        var healthBar = GetSlider((int)Sliders.BossSlider);
        if (healthBar != null)
        {
            healthBar.value = 1f;
            _canvasGroup = healthBar.gameObject.GetOrAddComponent<CanvasGroup>();
        }

        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;

        return true;
    }

    public void SetBossData(Boss.Data bossData)
    {
        var bossNameText = GetText((int)Texts.BossNameText);
        if (bossNameText != null)
            bossNameText.text = bossData.Name;

        var healthBar = GetSlider((int)Sliders.BossSlider);
        if (healthBar != null)
            healthBar.value = 1f;
    }

    public void UpdateBossHealth(float currentHealth, float maxHealth)
    {
        var healthBar = GetSlider((int)Sliders.BossSlider);
        if (healthBar == null)
            return;

        var healthPercent = Mathf.Clamp01(currentHealth / maxHealth);
        healthBar.value = healthPercent;
    }

    public void FadeIn(float duration)
    {
        if (_canvasGroup == null)
            return;

        StartCoroutine(FadeInRoutine(duration));
    }

    public void FadeOut(float duration)
    {
        if (_canvasGroup == null)
            return;

        StartCoroutine(FadeOutRoutine(duration));
    }

    public void StopFade()
    {
        StopAllCoroutines();
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
    }

    public IEnumerator FadeOutAsync(float duration)
    {
        if (_canvasGroup == null)
            yield break;

        yield return StartCoroutine(FadeOutRoutine(duration));
    }

    private IEnumerator FadeInRoutine(float duration)
    {
        if (_canvasGroup == null)
            yield break;

        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        if (_canvasGroup == null)
            yield break;

        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        _canvasGroup.alpha = 0f;
    }
}

