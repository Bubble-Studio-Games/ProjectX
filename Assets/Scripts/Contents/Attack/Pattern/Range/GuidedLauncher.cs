using System.Collections;
using UnityEngine;
using static Define;

public class GuidedLauncher : BaseLauncher, IProjectileLauncher
{
    public E_Projectile ProjectileType => E_Projectile.Guided;

    public  void Launch(Projectile projectile, GameEntity attacker, GameEntity target, LaunchContext ctx)
    {
        if (projectile == null) return;

        // 발사 시점: collider on
        projectile.Fire();

        ignoreRoot = projectile.m_Owner != null ? projectile.m_Owner.transform : null;
        ignoreCol = projectile.m_Collider;

        if (ctx.ObstacleHeight >= 1)
            attacker.StartCoroutine(Co_ArcThenTrack(projectile, target, ctx));
        else
            attacker.StartCoroutine(Co_TrackOrStraight(projectile, target));
    }


    private IEnumerator Co_ArcThenTrack(Projectile p, GameEntity target, LaunchContext ctx)
    {
        if (p == null) yield break;

        Vector3 start = p.m_Rigidbody.position;
        Vector3 targetPos = GetTargetPosition(target);

        float speed = p.m_fStraightSpeed;
        float totalDist = Vector3.Distance(start, targetPos);
        if (totalDist <= 0.0001f) yield break;

        float arcDuration = (totalDist / speed) * ARC_PHASE;
        float elapsed = 0f;

        float arcHeight = Mathf.Max(ctx.ColliderLength + ctx.ObstacleHeight, 0.5f);
        Vector3 arcPeak = start + (targetPos - start) * 0.5f + Vector3.up * arcHeight;

        int mask = GetLayerMask();
        float radius = p.SweepRadius;

        Vector3 prev = start;

        float accum = 0f;

        while (!p.m_IsHit && elapsed < arcDuration)
        {
            int steps = Util.ProjectileUtil.ConsumeSteps(ref accum, Time.fixedDeltaTime, StepFps);
            if (steps <= 0)
            {
                yield return new WaitForFixedUpdate();
                continue;
            }

            float dt = steps * (1f / StepFps);
            elapsed += dt;

            float t = Mathf.Clamp01(elapsed / arcDuration);

            // 아크 좌표(연속) -> 위치 스냅
            Vector3 rawPos = Vector3.Lerp(start, arcPeak, t);
            rawPos.y += Mathf.Sin(t * Mathf.PI * 0.5f) * arcHeight * 0.2f;

            Vector3 pos = Util.ProjectileUtil.Snap(rawPos, SNAP_UNIT);

            // Sweep(prev -> pos)
            if (Util.ProjectileUtil.TryHitSweep(prev, pos, radius, mask, ignoreRoot, ignoreCol, out RaycastHit hit))
            {
                Vector3 hitPos = Util.ProjectileUtil.Snap(hit.point, SNAP_UNIT);

                p.NotifyMoved(hitPos);
                p.m_Rigidbody.MovePosition(hitPos);

                Vector3 hitDir = hitPos - prev;
                if (hitDir.sqrMagnitude > 0.000001f)
                {
                    var rot = Quaternion.LookRotation(hitDir.normalized);
                    rot = Util.ProjectileUtil.SnapRotation(rot, ROT_SNAP_DEG);
                    p.m_Rigidbody.MoveRotation(rot);
                }

                p.HandleHit(hit.collider, hitPos, hitDir.normalized);
                yield break;
            }

            // Move
            p.NotifyMoved(pos);
            p.m_Rigidbody.MovePosition(pos);

            // ✅ 회전도 "스텝 발생한 프레임에만" 갱신
            Vector3 lookDir = arcPeak - pos;
            if (lookDir.sqrMagnitude > 0.000001f)
            {
                var rot = Quaternion.LookRotation(lookDir.normalized);
                rot = Util.ProjectileUtil.SnapRotation(rot, ROT_SNAP_DEG);
                p.m_Rigidbody.MoveRotation(rot);
            }

            prev = pos;
            yield return new WaitForFixedUpdate();
        }

        yield return Co_TrackOrStraight(p, target);
    }

    private IEnumerator Co_TrackOrStraight(Projectile p, GameEntity target)
    {
        if (p == null) yield break;

        int mask = GetLayerMask();
        float radius = p.SweepRadius;

        float accum = 0f;

        while (!p.m_IsHit)
        {
            // 타겟 사망/없음 -> 마지막 진행 방향으로 직선 전환
            if (target == null || target.m_AttributeSystem.m_IsDead)
            {
                Vector3 dirp = p.GetLastMoveDir();
                yield return Co_StraightDirection(p, dirp);
                yield break;
            }

            int steps = Util.ProjectileUtil.ConsumeSteps(ref accum, Time.fixedDeltaTime, StepFps);
            if (steps <= 0)
            {
                yield return new WaitForFixedUpdate();
                continue;
            }

            float dt = steps * (1f / StepFps);

            Vector3 curPos = p.m_Rigidbody.position;
            Vector3 targetPos = GetTargetPosition(target);

            Vector3 toTarget = targetPos - curPos;
            if (toTarget.sqrMagnitude <= 0.000001f)
                yield break;

            Vector3 dir = toTarget.normalized;

            // 다음 위치(연속) -> 위치 스냅
            Vector3 rawNext = curPos + dir * (p.m_fStraightSpeed * dt);
            Vector3 next = Util.ProjectileUtil.Snap(rawNext, SNAP_UNIT);

            // Sweep(cur->next)
            if (Util.ProjectileUtil.TryHitSweep(curPos, next, radius, mask, ignoreRoot, ignoreCol, out RaycastHit hit))
            {
                Vector3 hitPos = Util.ProjectileUtil.Snap(hit.point, SNAP_UNIT);

                p.NotifyMoved(hitPos);
                p.m_Rigidbody.MovePosition(hitPos);

                Vector3 hitDir = hitPos - curPos;
                if (hitDir.sqrMagnitude > 0.000001f)
                {
                    var rot = Quaternion.LookRotation(hitDir.normalized);
                    rot = Util.ProjectileUtil.SnapRotation(rot, ROT_SNAP_DEG);
                    p.m_Rigidbody.MoveRotation(rot);
                }

                p.HandleHit(hit.collider, hitPos, hitDir.normalized);
                yield break;
            }

            // Move
            p.NotifyMoved(next);
            p.m_Rigidbody.MovePosition(next);

            // ✅ 회전도 스텝 프레임에만 갱신 + 각도 스냅
            {
                var rot = Quaternion.LookRotation(dir);
                rot = Util.ProjectileUtil.SnapRotation(rot, ROT_SNAP_DEG);
                p.m_Rigidbody.MoveRotation(rot);
            }

            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator Co_StraightDirection(Projectile p, Vector3 dir)
    {
        if (p == null) yield break;
        if (dir.sqrMagnitude <= 0.0001f) yield break;

        dir.Normalize();

        int mask = GetLayerMask();
        float radius = p.SweepRadius;

        float accum = 0f;

        Vector3 cur = p.m_Rigidbody.position;

        // 시작 회전도 스냅
        {
            var rot = Quaternion.LookRotation(dir);
            rot = Util.ProjectileUtil.SnapRotation(rot, ROT_SNAP_DEG);
            p.m_Rigidbody.MoveRotation(rot);
        }

        while (!p.m_IsHit)
        {
            int steps = Util.ProjectileUtil.ConsumeSteps(ref accum, Time.fixedDeltaTime, StepFps);
            if (steps <= 0)
            {
                yield return new WaitForFixedUpdate();
                continue;
            }

            float dt = steps * (1f / StepFps);

            Vector3 rawNext = cur + dir * (p.m_fStraightSpeed * dt);
            Vector3 next = Util.ProjectileUtil.Snap(rawNext, SNAP_UNIT);

            if (Util.ProjectileUtil.TryHitSweep(cur, next, radius, mask, ignoreRoot, ignoreCol, out RaycastHit hit))
            {
                Vector3 hitPos = Util.ProjectileUtil.Snap(hit.point, SNAP_UNIT);

                p.NotifyMoved(hitPos);
                p.m_Rigidbody.MovePosition(hitPos);

                Vector3 hitDir = hitPos - cur;
                if (hitDir.sqrMagnitude > 0.000001f)
                {
                    var rot = Quaternion.LookRotation(hitDir.normalized);
                    rot = Util.ProjectileUtil.SnapRotation(rot, ROT_SNAP_DEG);
                    p.m_Rigidbody.MoveRotation(rot);
                }

                p.HandleHit(hit.collider, hitPos, hitDir.normalized);
                yield break;
            }

            p.NotifyMoved(next);
            p.m_Rigidbody.MovePosition(next);

            // 직선은 방향 고정이지만, "회전도 끊기는 느낌"을 유지하려면 여기서도 한번씩 갱신해도 됨
            {
                var rot = Quaternion.LookRotation(dir);
                rot = Util.ProjectileUtil.SnapRotation(rot, ROT_SNAP_DEG);
                p.m_Rigidbody.MoveRotation(rot);
            }

            cur = next;
            yield return new WaitForFixedUpdate();
        }
    }
}
