using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class StraightLauncher : IProjectileLauncher
{
    public E_Projectile ProjectileType => E_Projectile.Straight;

    public int GetRequiredProjectileCount() => 1;

    public LaunchCheckResult CanLaunch(ControllableObject attacker, GameEntity target, LaunchContext context)
    {
        // 직선 발사는 장애물이 낮을 때만 가능
        if (context.ObstacleHeight >= context.Property.MaxStraightShotHeight)
            return LaunchCheckResult.Failed;

        return new LaunchCheckResult(
            canLaunch: true,
            needParabola: false,
            boundHeight: 0f,
            speed: context.Property.StraightSpeed
        );
    }

    public void Launch(List<Projectile> projectiles, ControllableObject attacker, GameEntity target, LaunchCheckResult checkResult)
    {
        if (projectiles == null || projectiles.Count <= 0)
            return;

        Projectile projectile = projectiles[0];
        Vector3 targetPos = GetTargetPosition(target);
        attacker.StartCoroutine(LaunchCoroutine(projectile, targetPos, checkResult.Speed));
    }

    private Vector3 GetTargetPosition(GameEntity target)
    {
        Vector3 baseCenter = target.m_HitCollider.bounds.center;
        float height = target.m_HitCollider.bounds.size.y;
        return baseCenter + Vector3.up * (height * (1f / 6f));
    }

    private IEnumerator LaunchCoroutine(Projectile projectile, Vector3 targetPos, float speed)
    {
        while (projectile != null && Vector3.Distance(projectile.transform.position, targetPos) > 0.1f)
        {
            Vector3 nextPos = Vector3.MoveTowards(
                projectile.transform.position,
                targetPos,
                speed * Time.deltaTime
            );
            projectile.m_Rigidbody.MovePosition(nextPos);
            yield return new WaitForFixedUpdate();
        }
    }
}

