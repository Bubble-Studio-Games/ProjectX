using System.Collections;
using UnityEngine;
using static Define;

public class StraightLauncher : BaseLauncher, IProjectileLauncher
{
    public E_Projectile ProjectileType => E_Projectile.Straight;

    public void Launch(Projectile projectile, GameEntity attacker, GameEntity target, LaunchContext launchContext)
    {
        if (projectile == null) return;

        projectile.Fire(); // 발사 시점: collider on

        Vector3 targetPos = GetTargetPosition(target);

        ignoreRoot = projectile.m_Owner != null ? projectile.m_Owner.transform : null;
        ignoreCol = projectile.m_Collider;

        if (launchContext.ObstacleHeight >= 1)
            attacker.StartCoroutine(Co_Parabola(projectile, targetPos, launchContext.ObstacleHeight));
        else
            attacker.StartCoroutine(Co_Straight(projectile, targetPos));
    }

    private IEnumerator Co_Straight(Projectile p, Vector3 targetPos)
    {
        Vector3 dir = (targetPos - p.m_Rigidbody.position).normalized;
        if (dir.sqrMagnitude < 0.000001f) yield break;


        p.transform.rotation = Quaternion.LookRotation(dir);

        Vector3 cur = p.m_Rigidbody.position;
        int mask = GetLayerMask();
        float radius = p.SweepRadius;

        // ✅ Step Update 누적 타이머
        float accum = 0f;

        while (!p.m_IsHit)
        {
            // 이번 Fixed 틱에서 "스텝 몇 번" 처리할지 계산
            int steps = Util.ProjectileUtil.ConsumeSteps(ref accum, Time.fixedDeltaTime, StepFps);

            // 아직 스텝 시간이 안 됐으면 이번 프레임은 움직이지 않음(끊김 연출)
            if (steps <= 0)
            {
                yield return new WaitForFixedUpdate();
                continue;
            }

            // steps 만큼 시간을 한 번에 반영해서 "툭" 이동(프레임 드랍에도 속도 보존)
            float dt = steps * (1f / StepFps);

            // 기존 속도(초당) * dt 만큼 이동
            Vector3 rawNext = cur + dir * (p.m_fStraightSpeed * dt);

            // ✅ 스냅(픽셀 워블/텔레포트 감)
            Vector3 next = Util.ProjectileUtil.Snap(rawNext, SNAP_UNIT);

            // Sweep(from->to)도 스냅된 to 기준으로 검사해야 "텔레포트 구간"과 일치
            if (Util.ProjectileUtil.TryHitSweep(cur, next, radius, mask, ignoreRoot, ignoreCol, out RaycastHit hit))
            {
                Vector3 hitPos = Util.ProjectileUtil.Snap(hit.point, SNAP_UNIT);

                p.NotifyMoved(hitPos);
                p.m_Rigidbody.MovePosition(hitPos);

                Vector3 hitDir = (hitPos - cur);
                if (hitDir.sqrMagnitude > 0.000001f)
                    p.m_Rigidbody.MoveRotation(Quaternion.LookRotation(hitDir.normalized));

                p.HandleHit(hit.collider, hitPos, hitDir.normalized);
                yield break;
            }

            // Move (스텝 시점에만 실제 이동)
            p.NotifyMoved(next);
            p.m_Rigidbody.MovePosition(next);

            cur = next;
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator Co_Parabola(Projectile p, Vector3 targetPos, float obstacleHeight)
    {
        Vector3 start = p.m_Rigidbody.position;
        float dist = Vector3.Distance(start, targetPos);
        if (dist <= 0.0001f) yield break;

        float duration = dist / p.m_fStraightSpeed;

        Vector3 prev = start;
        int mask = GetLayerMask();
        float radius = p.SweepRadius;

        // ✅ 포물선 진행 시간(0~duration)
        float elapsed = 0f;

        // ✅ Step Update 누적 타이머
        float accum = 0f;

        while (!p.m_IsHit && elapsed < duration)
        {
            int steps = Util.ProjectileUtil.ConsumeSteps(ref accum, Time.fixedDeltaTime, StepFps);
            if (steps <= 0)
            {
                yield return new WaitForFixedUpdate();
                continue;
            }

            float dt = steps * (1f / StepFps);
            elapsed += dt;

            float t = Mathf.Clamp01(elapsed / duration);

            // 포물선 계산(연속 좌표)
            Vector3 rawPos = Vector3.Lerp(start, targetPos, t);
            rawPos.y += obstacleHeight * Mathf.Sin(t * Mathf.PI);

            // ✅ 스냅된 위치로 "툭" 이동
            Vector3 pos = Util.ProjectileUtil.Snap(rawPos, SNAP_UNIT);

            // Sweep(prev -> pos)                                                                                                                                               
            if (Util.ProjectileUtil.TryHitSweep(prev, pos, radius, mask, ignoreRoot, ignoreCol, out RaycastHit hit))
            {
                Vector3 hitPos = Util.ProjectileUtil.Snap(hit.point, SNAP_UNIT);

                p.NotifyMoved(hitPos);
                p.m_Rigidbody.MovePosition(hitPos);

                Vector3 hitDir = (hitPos - prev);
                if (hitDir.sqrMagnitude > 0.000001f)
                    p.m_Rigidbody.MoveRotation(Quaternion.LookRotation(hitDir.normalized));

                p.HandleHit(hit.collider, hitPos, hitDir.normalized);
                yield break;
            }

            p.NotifyMoved(pos);
            p.m_Rigidbody.MovePosition(pos);

            Vector3 moveDir = pos - prev;
            if (moveDir.sqrMagnitude > 0.000001f)
                p.m_Rigidbody.MoveRotation(Quaternion.LookRotation(moveDir.normalized));

            prev = pos;
            yield return new WaitForFixedUpdate();
        }
    }
}