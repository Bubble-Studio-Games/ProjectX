using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HitVolume : MonoBehaviour
{
    private Volume m_Volume;
    private Vignette _Vignette;

    [SerializeField] private float m_fMaxIntensity = 0.45f;
    [SerializeField] private float m_fFadeDuration = 0.5f; // 서서히 줄어드는 시간

    private StatSystem _StatSystem;
    private Coroutine _fadeCoroutine;

    private void Start()
    {
        if (DungeonCore.instance == null)
            return;

        _StatSystem = DungeonCore.instance.GetComponent<StatSystem>();
        _StatSystem.OnDamaged += OnDamaged;

        m_Volume = GetComponent<Volume>();
        m_Volume.profile.TryGet(out _Vignette);

        if (!_Vignette)
        {
            Debug.LogError("Error: HitVolume could not find Vignette in Volume Profile!");
        }
        else
        {
            _Vignette.intensity.value = 0f;
        }
    }

    private void OnDamaged(object s, EventArgs e)
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(HitEffectCoroutine());
    }

    private IEnumerator HitEffectCoroutine()
    {
        if (_Vignette == null) yield break;

        // 1. 순간적으로 강도 최대로 올림
        _Vignette.intensity.value = m_fMaxIntensity;

        // 1-2. Volume의 Weight 조정
        m_Volume.weight = 0.5f;

        // 2. 목표 강도 = 현재 체력 비율 기반
        float healthNormalized = _StatSystem.GetHealthNormalized();
        float targetIntensity = Mathf.Lerp(m_fMaxIntensity, 0f, healthNormalized);

        // 3. 서서히 내려감
        float startIntensity = _Vignette.intensity.value;
        float time = 0f;

        while (time < m_fFadeDuration)
        {
            time += Time.deltaTime;
            float t = time / m_fFadeDuration;
            _Vignette.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, t);
            m_Volume.weight = Mathf.Lerp(m_Volume.weight, 0, t);

            yield return null;
        }

        _Vignette.intensity.value = targetIntensity;
    }
}
