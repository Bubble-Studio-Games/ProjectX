using UnityEngine;
using static Define;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public readonly struct LaunchContext
{
    public readonly float ColliderLength;
    public readonly float ObstacleHeight;

    public LaunchContext(float colliderLength, float obstacleHeight)
    {
        ColliderLength = colliderLength;
        ObstacleHeight = obstacleHeight;
    }
}

[CreateAssetMenu(menuName = "Attack Pattern/Range")]
public class AttackData_Range : AttackData
{
    [Header("Spawn Object")]
    public GameObject m_ProjectilePrefab;
    public List<Projectile> m_SpawnProjectiles = new();

    [Header("Launch Strategy")]
    [SerializeField] private E_Projectile _selectType;
    private IProjectileLauncher _launcher;
    public IProjectileLauncher Launcher
    {
        get
        {
            if (_launcher == null)
                _launcher = LauncherCreator.Create(this._selectType);
            return _launcher;
        }
    }
    [HideInInspector] public LaunchContext context;

    [Header("Projectile Property")]
    public int m_iSpawnProjectileCount = 1;
    public bool m_IsImmediateLaunch = false;
    [Tooltip("무기에 붙어서 발사체를 생성할지 여부")]
    public bool m_SpawnFromWeapon = true;

    [Header("최적화용도")]
    [HideInInspector] public List<(ItemObject obj, Transform spawnTransform)> keepList = new();
    [HideInInspector] public List<(ItemObject obj, Transform spawnTransform)> removeList = new();

    [HideInInspector] public E_AttackAnimationType m_EAttackAnimationType;


}
