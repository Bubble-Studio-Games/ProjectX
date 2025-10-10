using System.Collections;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System;
using Unity.Cinemachine;

public class DungeonCore : PassiveObject
{
    public static DungeonCore instance;

    public EventHandler OnHit;

    [Header("Hit Effect Settings")]
    public float m_HitStunDuration = 1f;
    public Color m_HitColor = Color.red;
    public ParticleSystem m_HitParticles;
    public AudioClip m_HitSound;

    private AudioSource m_AudioSource;
    private Dictionary<Material, Color> m_CoreMaterial = new();

    [Header("Hit Effect - CameraShake")]
    [SerializeField] float m_fMinForce = 1f;
    [SerializeField] float m_fMaxForce = 5f;
    [SerializeField] float m_fMinTime = 0.5f;
    [SerializeField] float m_fMaxTime = 3f;

    private CinemachineImpulseSource m_CMImpulseSource;


    protected override void Awake()
    {
        base.Awake();

        instance = this;

        // Hit 효과에 관하여 (석상 색상 변화 + Volume + Camera Shake)
        m_AttributeSystem.OnDamaged += (s, e) => Hit();
        m_AttributeSystem.OnDead += (s, e) => HeartZero();

        m_AudioSource = GetComponent<AudioSource>();

        // 하위 렌더러까지 색상 데이터 저장
        foreach (var render in GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in render.materials)
            {
                if (!m_CoreMaterial.ContainsKey(mat))
                    m_CoreMaterial.Add(mat, mat.color);
            }
        }

        m_CMImpulseSource = GetComponent<CinemachineImpulseSource>();

    }

    private void Hit()
    {
        foreach (var kvp in m_CoreMaterial)
        {
            // 기존 트윈이 있으면 삭제 (겹침 방지)
            kvp.Key.DOKill();

            // 빨간색 → 원래 색상으로 트윈 실행
            kvp.Key.color = m_HitColor;
            kvp.Key.DOColor(kvp.Value, "_BaseColor", m_HitStunDuration).SetEase(Ease.OutCubic);
        }

        // 파티클 & 사운드
        if (m_HitParticles != null)
            m_HitParticles.Play();
        if (m_AudioSource != null && m_HitSound != null)
            m_AudioSource.PlayOneShot(m_HitSound);

        // Camera Shake
        // ShakeCamera 호출 부분
        float healthFactor = 1f - m_AttributeSystem.GetHealthNormalized();
        // 0 ~ 1 사이 (체력이 적을수록 1에 가까움)

        // healthFactor 비율로 1 ~ maxForce 사이를 보간
        float forceintensity = Mathf.Lerp(m_fMinForce, m_fMaxForce, healthFactor);
        float timeintensity = Mathf.Lerp(m_fMinTime, m_fMaxTime, healthFactor);
        ShakeCamera(forceintensity, timeintensity);
    }


    private void HeartZero()
    {
        // 게임 종료 처리
        Managers.Game.DungeonExplosionFail();



        // TODO : 게임 종료 처리
        // UIManager.Instance.ShowPopup("GameOverPopup");
        // GameManager.Instance.GameOver();
    }

    public override void DeSpawnComplete()
    {
    }

    public void ShakeCamera(float force = 1, float duration = 0.5f)
    {
        CameraController.Instance.m_CMImpulseListener.ReactionSettings.Duration = duration;
        m_CMImpulseSource.GenerateImpulse(force);
    }
}
