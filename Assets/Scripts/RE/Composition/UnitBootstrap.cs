using Unit.Composer;
using Unit.Dependencies;
using Unit.ActionDecider;
using UnityEngine;
using SO.Unit;

/// <summary>
/// 유닛을 구성하는 역할의 의존성 및 설정을 추가해주는 설정 스크립트 
/// </summary>

[RequireComponent(typeof(GameEntityBase))]
[RequireComponent(typeof(ActionController))]
public sealed class UnitBootstrap : MonoBehaviour
{
    public enum BootstrapMode { ConfigSO, Factory }

    [Header("Mode")]
    [SerializeField] private BootstrapMode mode = BootstrapMode.ConfigSO;
    [Header("Config (Mode=ConfigSO)")]
    [SerializeField] private UnitConfigSO config;
    [Header("Factory (Mode=Factory)")]
    [SerializeField] private EUnitType type = EUnitType.Player;

    [Header("Common Refs")]
    [SerializeField] private Camera cameraRef;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private MonoBehaviour gridServiceBehaviour;

    private ActionController actionController;
    private GameEntityBase entity;
    private UnitContext ctx;

    private void Awake()
    {
        entity = GetComponent<GameEntityBase>();
        actionController = GetComponent<ActionController>();
        if (cameraRef == null) cameraRef = Camera.main;

        var deps = new UnitDependencies
        {
            Camera = cameraRef,
            GroundMask = groundMask,
            GridService = gridServiceBehaviour as IGridService
        };

        IUnitComposer composer = CreateComposer(mode, config, type);

        ctx = composer.CreateContext(deps, gameObject, entity);

        // Register/Unregister는 지금은 Bootstrap에서 담당
        EntityManager.Register(ctx);

        var decider = composer.CreateDecider(deps, gameObject);
        actionController.Init(ctx, decider);

        ApplyInitialAction(actionController, config);
    }

    private void OnDestroy()
    {
        if (ctx != null)
            EntityManager.Unregister(ctx);
    }

    private static IUnitComposer CreateComposer(BootstrapMode mode, UnitConfigSO config, EUnitType type)
    {
        return mode switch
        {
            BootstrapMode.ConfigSO => new ConfigUnitComposer(config),
            BootstrapMode.Factory => UnitComposerFactory.Create(type),
            _ => new ConfigUnitComposer(config)
        };
    }

    private static void ApplyInitialAction(ActionController controller, UnitConfigSO config)
    {
        // config가 없으면 기본 Idle
        var initial = config != null ? config.initialAction : UnitInitialAction.Idle;

        switch (initial)
        {
            case UnitInitialAction.None:
                break;
            case UnitInitialAction.Idle:
            default:
                controller.RequestIdle();
                break;
        }
    }
}