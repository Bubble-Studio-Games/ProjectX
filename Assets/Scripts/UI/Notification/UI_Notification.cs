using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 알림/토스트 UI 베이스 클래스 - Canvas 있음, Stack 관리 없음, High SortOrder
/// </summary>
public abstract class UI_Notification : UI_Base
{
    protected CanvasGroup _canvasGroup;
    protected Tweener _fadeTweener;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        Managers.UI.SetCanvas(gameObject, true);
        _canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
        return true;
    }

    /// <summary>
    /// 페이드 인 효과로 표시
    /// </summary>
    protected virtual void ShowWithFade(float duration = 0.3f)
    {
        if (_canvasGroup == null)
            return;

        _fadeTweener?.Kill();
        _canvasGroup.alpha = 0f;

        _fadeTweener = _canvasGroup.DOFade(1f, duration)
            .SetUpdate(true)
            .SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 페이드 아웃 효과로 숨김
    /// </summary>
    protected virtual void HideWithFade(float duration = 0.3f, Action onComplete = null)
    {
        if (_canvasGroup == null)
        {
            onComplete?.Invoke();
            Close();
            return;
        }

        _fadeTweener?.Kill();

        _fadeTweener = _canvasGroup.DOFade(0f, duration)
            .SetUpdate(true)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                onComplete?.Invoke();
                Close();
            });
    }

    /// <summary>
    /// 일정 시간 후 자동으로 닫힘
    /// </summary>
    protected void AutoClose(float displayDuration, float fadeOutDuration = 0.3f)
    {
        DOVirtual.DelayedCall(displayDuration, () =>
        {
            HideWithFade(fadeOutDuration);
        }).SetUpdate(true);
    }

    /// <summary>
    /// 알림 UI 닫기
    /// </summary>
    public virtual void Close()
    {
        _fadeTweener?.Kill();
        _canvasGroup?.DOKill();
        Managers.Resource.Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        _fadeTweener?.Kill();
        _canvasGroup?.DOKill();
    }
}
