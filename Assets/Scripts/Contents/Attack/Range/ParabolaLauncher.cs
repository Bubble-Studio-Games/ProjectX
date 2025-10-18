using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// 포물선형 발사체 런
/// - 실시간 타겟 추적, 타겟 사망 시 직선형으로 전환
/// </summary>
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
        {
            Debug.LogError("프로젝티일이 존재하지 않습니다.");
            return;
        }

        Projectile projectile = projectiles[0];

        attacker.StartCoroutine(LaunchTrackingOrStraight(projectile, attacker, target, checkResult.Speed, checkResult.BoundHeight));
    }

    private Vector3 GetTargetPosition(GameEntity target)
    {
        Vector3 baseCenter = target.m_HitCollider.bounds.center;
        float height = target.m_HitCollider.bounds.size.y;
        return baseCenter + Vector3.up * (height * (1f / 6f));
    }

    private IEnumerator LaunchTrackingOrStraight(Projectile projectile, ControllableObject attacker, GameEntity target, float speed, float boundHeight)
    {
        Vector3 startPos = projectile.transform.position;
        float elapsedTime = 0f;

        while (projectile != null)
        {
            // 타겟이 사망시 직선형 전환
            if (target == null || target.IsDead)
            {
                Vector3 dir = projectile.m_Rigidbody.velocity.normalized;
                if (dir == Vector3.zero)
                    dir = (target.transform.position - projectile.transform.position).normalized;

                yield return LaunchStraight(projectile, dir, speed);
                yield break;
            }

            // 타겟의 현재 위치를 실시간으로 가져옴
            Vector3 targetPos = GetTargetPosition(target);
            float distance = Vector3.Distance(startPos, targetPos);
            float duration = distance / speed;

            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            // 포물선 궤적 계산
            Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
            pos.y += boundHeight * Mathf.Sin(t * Mathf.PI);

            projectile.m_Rigidbody.MovePosition(pos);

            // 타겟에 도달했는지 확인
            if (Vector3.Distance(projectile.transform.position, targetPos) < 0.5f)
                yield break;

            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator LaunchStraight(Projectile projectile, Vector3 direction, float speed)
    {
        float maxDistance = 50f; // 최대 이동 거리
        float distance = 0f;

        while (projectile != null && distance < maxDistance)
        {
            Vector3 nextPos = projectile.transform.position + direction * speed * Time.deltaTime;
            projectile.m_Rigidbody.MovePosition(nextPos);

            distance += speed * Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        // 최대 거리 도달 시 발사체 파괴
        if (projectile != null)
            projectile.Destroy(0.0f);
    }
}

