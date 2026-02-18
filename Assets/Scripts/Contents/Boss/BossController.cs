using System;
using UnityEngine;
using UnityEngine.Playables;
using Data;

// Note - BossEntity 만들기전 임시
public class BossController : MonoBehaviour
{
    [SerializeField] private int[] _healthThresholds = { 75, 50, 25 };
    [SerializeField] private PlayableDirector _introTimeline;
    [SerializeField] private Animator _bossAnimator;

    private Boss.Data _data;
    private int _currentPhase = 0;
    private bool _isInCombat = false;

    [Header("HP 시스템")]
    [SerializeField] private float _currentHealth = 100f;
    [SerializeField] private float _maxHealth = 100f;
    private bool _isDead = false;

    public Boss.Data Data => _data;
    public PlayableDirector IntroTimeline => _introTimeline;
    public event Action<int> OnPhaseChanged;
    public event Action<float> OnHealthChanged;

    public void Init(Boss.Data data)
    {
        _data = data;
        _maxHealth = data.MaxHealth;
        _currentHealth = _maxHealth;
        _isDead = false;
    }

    public void StartCombat()
    {
        _isInCombat = true;
    }

    public void StopCombat()
    {
        _isInCombat = false;
    }

    public string GetBossId()
    {
        return _data?.Id;
    }


    private void HandleBossDead()
    {
        _isInCombat = false;
        _isDead = true;

        var bossSystem = Managers.SceneServices.Get<BossSystem>();
        if (bossSystem != null)
            bossSystem.OnBossEntityDead();
    }

    public void OnTimelineIntroComplete()
    {
        StartCombat();
    }

    public void PlayRoarAnimation()
    {
        _bossAnimator.SetTrigger("RoarTrigger");
    }

    public void PlayDeathAnimation()
    {
    }

    public void Despawn()
    {
    }

    /// <summary>
    /// 보스의 HP를 0으로 설정 - 테스트용
    /// </summary>
    public void SetHealthToZero()
    {
        _currentHealth = 0f;
        HandleBossDead();
    }

    /// <summary>
    /// 현재 HP 반환
    /// </summary>
    public float GetCurrentHealth()
    {
        return _currentHealth;
    }

    /// <summary>
    /// 최대 HP 반환
    /// </summary>
    public float GetMaxHealth()
    {
        return _maxHealth;
    }
}
