using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class MultipleLauncher : IProjectileLauncher
{
    // 개수를 조정하고 싶은경우 
    // MonoBehaviour 쪽에서 설정하는 값을가져오던가 SO쪽에서 새로운 설정값을 세팅하게해야함
    // 지금은 하드코딩
    private int _projectileCount = 3;

    public E_Projectile ProjectileType => E_Projectile.Multiple;

    public int GetRequiredProjectileCount() => _projectileCount;

    public LaunchCheckResult CanLaunch(ControllableObject attacker, GameEntity target, LaunchContext context)
    {
        var ret = LaunchCheckResult.Success(false, context.ColliderLength, context.Property.StraightSpeed);
        return ret;
    }

    public void Launch(List<Projectile> projectiles, ControllableObject attacker, GameEntity target, LaunchCheckResult checkResult)
    {
        if (projectiles == null || projectiles.Count <= 0)
            return;


        attacker.StartCoroutine(LaunchMultiple(projectiles, attacker, target, checkResult));
    }

    private IEnumerator LaunchMultiple(List<Projectile> projectiles, ControllableObject attacker, GameEntity target,
        LaunchCheckResult checkResult)
    {
        // 리스트에 있는 모든 발사체를 순차적으로 발사
        for (int i = 0; i < projectiles.Count; i++)
        {
            if (projectiles[i] == null)
                continue;

            if (i > 0)
                yield return new WaitForSeconds(0.1f);

            // 각 발사체를 목표 위치까지 직선으로 발사
            Vector3 targetPos = GetTargetPosition(target);
            yield return attacker.StartCoroutine(LaunchSingleProjectile(projectiles[i], targetPos, checkResult.Speed));
        }
    }

    private Vector3 GetTargetPosition(GameEntity target)
    {
        Vector3 baseCenter = target.m_HitCollider.bounds.center;
        float height = target.m_HitCollider.bounds.size.y;
        return baseCenter + Vector3.up * (height * (1f / 6f));
    }

    private IEnumerator LaunchSingleProjectile(Projectile projectile, Vector3 targetPos, float speed)
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

