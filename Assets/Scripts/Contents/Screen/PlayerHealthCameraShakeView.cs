using UnityEngine;
using Unity.Cinemachine;
using static Define;

[EditorShowInfo("플레이어 체력 기반 카메라 쉐이크 전용 View")]
[RequireComponent(typeof(CinemachineImpulseSource))]
public class PlayerHealthCameraShakeView : MonoBehaviour
{
    [SerializeField] float minForce = 1f;
    [SerializeField] float maxForce = 5f;
    [SerializeField] float minTime = 0.5f;
    [SerializeField] float maxTime = 3f;

    CinemachineImpulseSource impulse;

    void Awake()
    {
        impulse = GetComponent<CinemachineImpulseSource>();
    }

    void OnEnable()
    {
        Managers.Player.playerHealth.OnAnyCoreDamaged += OnDamaged;
    }

    void OnDisable()
    {
        Managers.Player.playerHealth.OnAnyCoreDamaged -= OnDamaged;
    }

    void OnDamaged(IDungeonCore core, float healthNormalized)
    {
        float healthFactor = 1f - healthNormalized;

        float force = Mathf.Lerp(minForce, maxForce, healthFactor);
        float duration = Mathf.Lerp(minTime, maxTime, healthFactor);

        Managers.SceneServices.CameraShakeSettings
            .SetImpulseReactionDuration(duration);

        impulse?.GenerateImpulse(force);
    }
}
