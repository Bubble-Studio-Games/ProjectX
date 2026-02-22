using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Define;

public class HitVolume : MonoBehaviour
{
    private Volume m_Volume;
    private Vignette _Vignette;

    [SerializeField] private float m_fMaxIntensity = 0.45f;
    [SerializeField] private float m_fFadeDuration = 0.5f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        m_Volume = GetComponent<Volume>();

        if (m_Volume.profile.TryGet(out _Vignette) == false)
        {
            Debug.LogError("Vignette not found in Volume Profile!");
        }
    }

    void OnEnable()
    {
        Managers.Player.playerHealth.OnAnyCoreDamaged += OnCoreDamaged;
    }

    void OnDisable()
    {
        Managers.Player.playerHealth.OnAnyCoreDamaged -= OnCoreDamaged;
    }

    void OnCoreDamaged(IDungeonCore core, float healthNormalized)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(healthNormalized));
    }

    IEnumerator FadeRoutine(float healthNormalized)
    {
        _Vignette.intensity.value = m_fMaxIntensity;
        m_Volume.weight = 0.5f;

        float targetIntensity = Mathf.Lerp(m_fMaxIntensity, 0f, healthNormalized);

        float startIntensity = _Vignette.intensity.value;
        float time = 0f;

        while (time < m_fFadeDuration)
        {
            time += Time.deltaTime;
            float t = time / m_fFadeDuration;

            _Vignette.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, t);
            m_Volume.weight = Mathf.Lerp(m_Volume.weight, 0f, t);

            yield return null;
        }

        _Vignette.intensity.value = targetIntensity;
        m_Volume.weight = 0f;
    }
}
