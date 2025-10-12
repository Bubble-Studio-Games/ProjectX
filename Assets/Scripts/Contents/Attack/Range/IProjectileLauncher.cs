using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public interface IProjectileLauncher
{
    public E_Projectile ProjectileType { get; }

    /// <summary>
    /// 필요한 발사체 개수 반환 (기본 1개, Multiple은 여러 개)
    /// </summary>
    public int GetRequiredProjectileCount();

    public LaunchCheckResult CanLaunch(ControllableObject attacker, GameEntity target, LaunchContext context);

    /// <summary>
    /// 발사체 리스트를 받아서 발사 (단일 또는 다중)
    /// </summary>
    public void Launch(List<Projectile> projectiles, ControllableObject attacker, GameEntity target, LaunchCheckResult checkResult);
}

public static class LauncherCreator
{
    public static IProjectileLauncher Create(E_Projectile projectileType)
    {
        switch (projectileType)
        {
            case E_Projectile.Multiple:
                return new MultipleLauncher();
            case E_Projectile.Parabola:
                return new ParabolaLauncher();
            case E_Projectile.Straight:
                return new StraightLauncher();
            default:
                return new StraightLauncher();
        }
    }
}

