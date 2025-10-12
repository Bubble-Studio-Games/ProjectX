using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class ParabolaLauncher : IProjectileLauncher
{
    public E_Projectile ProjectileType => E_Projectile.Parabola;

    public int GetRequiredProjectileCount() => 1;

    public LaunchCheckResult CanLaunch(ControllableObject attacker, GameEntity target, LaunchContext context)
    {
        // 포물선은 장애물이 있을 때 필요
        if (context.ObstacleHeight < context.Property.MaxStraightShotHeight)
            return LaunchCheckResult.Failed;

        float parabolaHeight = context.ColliderLength + context.ObstacleHeight;

        // 천장에 닿으면 불가능
        if (parabolaHeight > context.Property.CeilingHeight)
            return LaunchCheckResult.Failed;

        return new LaunchCheckResult(
            canLaunch: true,
            needParabola: true,
            boundHeight: parabolaHeight,
            speed: context.Property.ParabolaSpeed
        );
    }

    public void Launch(List<Projectile> projectiles, ControllableObject attacker, GameEntity target, LaunchCheckResult checkResult)
    {
        if (projectiles == null || projectiles.Count <= 0)
            return;

        Projectile projectile = projectiles[0];
        Vector3 startPos = projectile.transform.position;
        Vector3 targetPos = GetTargetPosition(target);
        attacker.StartCoroutine(LaunchCoroutine(projectile, startPos, targetPos, checkResult.Speed, checkResult.BoundHeight));
    }

    private Vector3 GetTargetPosition(GameEntity target)
    {
        Vector3 baseCenter = target.m_HitCollider.bounds.center;
        float height = target.m_HitCollider.bounds.size.y;
        return baseCenter + Vector3.up * (height * (1f / 6f));
    }

    private IEnumerator LaunchCoroutine(Projectile projectile, Vector3 start, Vector3 end, float speed, float boundHeight)
    {
        float t = 0;
        float duration = Vector3.Distance(start, end) / speed;

        while (t < 1f && projectile != null)
        {
            t += Time.deltaTime / duration;

            Vector3 currentPos = Vector3.Lerp(start, end, t);
            currentPos.y += boundHeight * Mathf.Sin(t * Mathf.PI);

            projectile.m_Rigidbody.MovePosition(currentPos);
            yield return new WaitForFixedUpdate();
        }
    }
}

