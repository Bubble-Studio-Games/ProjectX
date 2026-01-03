using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Define;

public static partial class Util
{
    #region Attack

    public static class ProjectileUtil
    {
        // 프로젝트마다 값 다를 수 있으니 Projectile에 있는 반지름을 기본으로 쓰는 걸 추천
        public static bool TryHitSweep(
            Vector3 from,
            Vector3 to,
            float radius,
            int layerMask,
            Transform ignoreRoot,
            Collider ignoreCollider,
            out RaycastHit hit)
        {
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            if (dist <= 0.0001f)
            {
                hit = default;
                return false;
            }

            dir /= dist;

            // 단발 cast는 "무시"가 어렵기 때문에, 여러 개를 받아서 거르는 방식이 안전함
            // (성능: 투사체 10~50개 수준이면 충분히 감당됨)
            var hits = Physics.SphereCastAll(from, radius, dir, dist, layerMask, QueryTriggerInteraction.Ignore);

            if (hits == null || hits.Length == 0)
            {
                hit = default;
                return false;
            }

            // 거리순 정렬(가장 가까운 유효 히트를 선택)
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];

                // 1) 자기 자신의 콜라이더 무시
                if (ignoreCollider != null && h.collider == ignoreCollider)
                    continue;

                // 2) 발사자 루트(및 자식) 무시
                if (ignoreRoot != null && h.collider != null && h.collider.transform.IsChildOf(ignoreRoot))
                    continue;

                hit = h;
                return true;
            }

            hit = default;
            return false;
        }


        /// <summary>
        /// 누적 시간을 stepInterval(=1/stepFps) 단위로 잘라서,
        /// 이번 틱에서 실제로 "몇 step"을 처리할지 반환.
        /// </summary>
        public static int ConsumeSteps(ref float accum, float fixedDeltaTime, float stepFps)
        {
            if (stepFps <= 0f) stepFps = 1f;

            float interval = 1f / stepFps;
            accum += fixedDeltaTime;

            if (accum < interval) return 0;

            int steps = Mathf.FloorToInt(accum / interval);
            accum -= steps * interval;
            return steps;
        }

        /// <summary>
        /// Vector3를 snapUnit 간격으로 스냅(반올림).
        /// </summary>
        public static Vector3 Snap(Vector3 v, float snapUnit)
        {
            if (snapUnit <= 0f) return v;

            v.x = Mathf.Round(v.x / snapUnit) * snapUnit;
            v.y = Mathf.Round(v.y / snapUnit) * snapUnit;
            v.z = Mathf.Round(v.z / snapUnit) * snapUnit;
            return v;
        }

        // 각도 스냅(회전 툭툭 느낌 강화)
        public static Quaternion SnapRotation(Quaternion q, float snapDeg)
        {
            if (snapDeg <= 0f) return q;

            Vector3 e = q.eulerAngles;
            e.x = Mathf.Round(e.x / snapDeg) * snapDeg;
            e.y = Mathf.Round(e.y / snapDeg) * snapDeg;
            e.z = Mathf.Round(e.z / snapDeg) * snapDeg;
            return Quaternion.Euler(e);
        }
    }

    #endregion


    /// <summary>
    /// IEnumerble<GridPosition>을 받아서 디버그용 표시를 해준다.
    /// SceneView와 GameView에서 모두 확인 가능.
    /// </summary>
    public static void DrawDebugPositions(IEnumerable<GridPosition> positions,
                                          float duration = 5f,
                                          float size = 0.3f)
    {
        foreach (var pos in positions)
        {
            Vector3 wp = Managers.SceneServices.Grid.GetWorldPosition(pos);

            // Sphere-like marker (actually cube for simplicity)
            DebugDrawSphere(wp, size, duration);

            // Optional: vertical indicator line
            Debug.DrawLine(wp, wp + Vector3.up * 1.5f, Color.red, duration);
        }
    }

    private static void DebugDrawSphere(Vector3 position, float radius, float duration)
    {
        // 6 lines to fake a sphere
        Debug.DrawLine(position + Vector3.up * radius, position - Vector3.up * radius, Color.red, duration);
        Debug.DrawLine(position + Vector3.right * radius, position - Vector3.right * radius, Color.red, duration);
        Debug.DrawLine(position + Vector3.forward * radius, position - Vector3.forward * radius, Color.red, duration);
    }
}