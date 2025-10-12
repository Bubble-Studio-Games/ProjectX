using UnityEngine;

[System.Serializable]
public struct ProjectileProperty
{
    [Tooltip("즉시 발사 여부")]
    public bool UseProjectileLaunch;

    [Tooltip("직선 발사 속도")]
    public float StraightSpeed;

    [Tooltip("포물선 발사 속도")]
    public float ParabolaSpeed;

    [Tooltip("탐지 반경")]
    public float DetectionHitRadius;

    [Tooltip("천장 높이 제한")]
    public float CeilingHeight;

    [Tooltip("직선 발사 가능한 최대 장애물 높이")]
    public float MaxStraightShotHeight;

    public static ProjectileProperty Default => new ProjectileProperty
    {
        UseProjectileLaunch = false,
        StraightSpeed = 10f,
        ParabolaSpeed = 5f,
        DetectionHitRadius = 2f,
        CeilingHeight = 7f,
        MaxStraightShotHeight = 1f
    };
}

public readonly struct LaunchContext
{
    public readonly float ColliderLength;
    public readonly float ObstacleHeight;
    public readonly ProjectileProperty Property;

    public LaunchContext(float colliderLength, float obstacleHeight, ProjectileProperty property)
    {
        ColliderLength = colliderLength;
        ObstacleHeight = obstacleHeight;
        Property = property;
    }
}

public readonly struct LaunchCheckResult
{
    public readonly bool CanLaunch;
    public readonly bool NeedParabola;
    public readonly float BoundHeight;
    public readonly float Speed;

    public LaunchCheckResult(bool canLaunch, bool needParabola, float boundHeight, float speed)
    {
        CanLaunch = canLaunch;
        NeedParabola = needParabola;
        BoundHeight = boundHeight;
        Speed = speed;
    }

    public static LaunchCheckResult Success(bool needParabola, float boundHeight, float speed) 
    {
        return new LaunchCheckResult(true, needParabola, boundHeight, speed);
    }
    public static LaunchCheckResult Failed => new LaunchCheckResult(false, false, 0f, 0f);
}