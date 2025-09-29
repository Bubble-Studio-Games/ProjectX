using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatBarUI : MonoBehaviour
{
    private StatSystem StatSystem;
    private GameEntity m_GameEntity;

    [SerializeField] private Image healthBarImage;
    [SerializeField] private Image ManaBarImage;
    [SerializeField] private GameObject ManaBar;
    [SerializeField] private GameObject healthBar;

    private void Awake()
    {
        m_GameEntity = GetComponentInParent<GameEntity>();
        m_GameEntity.OnObjectSpawned += (s, e) => Init();
        m_GameEntity.OnSpawnObjectSelected += (s, e) => SetActiveFalseBars();

        StatSystem = GetComponentInParent<StatSystem>();

        // Event
        StatSystem.OnDamaged += (s, e) => UpdateHealthBar();
        StatSystem.OnMPUsed += (s, e) => UpdateManaBar();

        StatSystem.OnDead += (s, e) => SetActiveFalseBars();
        StatSystem.OnRevived += (s, e) => Init();
    }

    private void Start()
    {
        if (!m_GameEntity.m_IsSetuping)
            Init();
    }

    public void Init()
    {
        healthBar.SetActive(true);

        UpdateHealthBar();

        if (StatSystem.IsManaCharacter())
        {
            UpdateManaBar();
            ManaBar.SetActive(true);
        }
        else
            ManaBar.SetActive(false);
    }

    private void UpdateHealthBar()
    {
        healthBarImage.fillAmount = StatSystem.GetHealthNormalized();
    }

    private void UpdateManaBar()
    {
        ManaBarImage.fillAmount = StatSystem.GetManaNormalized();
    }

    private void SetActiveFalseBars()
    {
        ManaBar.SetActive(false); 
        healthBar.SetActive(false);
    }
}
