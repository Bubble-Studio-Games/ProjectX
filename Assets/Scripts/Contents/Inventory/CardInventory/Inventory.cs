using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    public EventHandler<int> OnChangeDownjam;

    [Header("Card")]
    public List<GameEntity> gameEntitiesPrefab = new();

    [Header("HUD")]
    public int m_iDownJamAmount;
    public int m_iDownJamAmountMax = 99999;

    private void Awake()
    {
        Instance = this;
    }

    public List<GameEntity> GetEnableCardList()
    {
        return gameEntitiesPrefab;
    }

    public void AddDownJam(int amount)
    {
        m_iDownJamAmount = Math.Clamp(m_iDownJamAmount + amount, 0, m_iDownJamAmountMax);
        OnChangeDownjam?.Invoke(this, amount);
    }
}
