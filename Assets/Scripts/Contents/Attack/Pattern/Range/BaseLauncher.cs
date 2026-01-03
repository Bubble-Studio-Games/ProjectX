using UnityEngine;

public class BaseLauncher
{
    // ✅ 스텝 이동 FPS (애니메이션 스텝이랑 통일하고 싶으면 이걸 사용)
    protected float StepFps => GameConfig.AnimationStepFps;

    // ✅ 위치 스냅 단위 (픽셀/도트 느낌 강도 조절)
    protected const float SNAP_UNIT = 0.05f;

    // 회전도 끊기게: 각도 스냅 단위(값 클수록 더 끊김)
    // 0이면 각도 스냅 없이 "스텝 회전만" 적용(갱신 빈도만 끊김)
    protected const float ROT_SNAP_DEG = 15f;

    protected const float ARC_PHASE = 0.4f;

    protected Transform ignoreRoot;
    protected Collider ignoreCol;

    protected int GetLayerMask()
        => GameConfig.Layer.HitColLayerMask | GameConfig.Layer.m_StructLayer;

    protected Vector3 GetTargetPosition(GameEntity target)
    {
        Vector3 baseCenter = target.m_HitCollider.bounds.center;
        float height = target.m_HitCollider.bounds.size.y;
        return baseCenter + Vector3.up * (height * (1f / 6f));
    }
}
