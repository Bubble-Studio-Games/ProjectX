using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

// 플레이어의 정보를 총괄 관리한다.
// 플레이어의 체력, 인벤토리 아이템 정보 등
public class PlayerManager : IManager
{
    public  Re_Inventory Inventory { get; private set; } = new();
    public PlayerHealth playerHealth { get; private set; } = new();

    public void Init()
    {
    }

    public void Clear()
    {
    }
}

public class Re_Inventory
{
    public event Action<int> DownJamChanged;

    private int m_iDownJamAmount;
    private int m_iDownJamAmountMax = int.MaxValue;

    public int DownJamAmount => m_iDownJamAmount;
    public IReadOnlyList<GameEntity> EnabledCards => GameConfig.Inventory.GameEntityPrefab;

    public void AddDownJam(int amount)
    {
        m_iDownJamAmount = Math.Clamp(m_iDownJamAmount + amount, 0, m_iDownJamAmountMax);

        DownJamChanged?.Invoke(m_iDownJamAmount);
    }
}

public class PlayerHealth 
{
    readonly HashSet<IDungeonCore> _cores = new();

    public event Action<float> OnHealthChanged;
    public event Action OnPlayerDead;
    public event Action<IDungeonCore, float> OnAnyCoreDamaged;

    public IReadOnlyCollection<IDungeonCore> Cores => _cores;

    public void Register(IDungeonCore core)
    {
        if (_cores.Add(core))
        {
        }
    }

    public void UnRegister(IDungeonCore core)
    {
        if (_cores.Remove(core))
        {
        }
    }

    public void NotifyDamaged(IDungeonCore core, float healthNormalized)
    {
        OnAnyCoreDamaged?.Invoke(core, healthNormalized);
    }
}

