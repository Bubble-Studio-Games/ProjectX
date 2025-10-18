using UnityEngine;
using static Define;
using System.Linq;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Attack Pattern/Range")]
public class AttackPattern_Range : AttackPattern<AttackPatternInfoClip>
{
    [Header("Spawn Object")]
    public GameObject m_ProjectilePrefab;
    private readonly List<Projectile> m_SpawnProjectiles = new();

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


    [Header("Projectile Property")]
    [SerializeField] private ProjectileProperty _projectileProperty = ProjectileProperty.Default;

    private LaunchCheckResult _cachedCheckResult;

    public void ClearProjectiles() => m_SpawnProjectiles.Clear();

    public override E_AttackCondition CanExecute(ControllableObject attacker, GameEntity target)
    {
        if (base.CanExecute(attacker, target) >= E_AttackCondition.Fail_None)
            return base.CanExecute(attacker, target);

        // LaunchContext 생성
        float colliderLength;
        if (attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject != null)
            colliderLength = Managers.Game.GetObjectColliderLongLength(attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject.gameObject);
        else
            colliderLength = Managers.Game.GetObjectColliderLongLength(m_ProjectilePrefab.gameObject);

        float obstacleHeight = LevelGrid.Instance.GetObstacleMaxHeight(attacker.GetGridPosition(), target.GetGridPosition());

        LaunchContext context = new LaunchContext(
            colliderLength: colliderLength,
            obstacleHeight: obstacleHeight,
            property: _projectileProperty
        );

        // 발사 가능 여부 체크 및 결과 캐싱
        _cachedCheckResult = Launcher.CanLaunch(attacker, target, context);

        if (_cachedCheckResult.CanLaunch == false)
            return E_AttackCondition.Fail_IndividualCondition;

        return E_AttackCondition.Success;
    }

    public override void StartAttack(ControllableObject attacker, GameEntity target, AttackPattern prevAttackpatern)
    {
        base.StartAttack(attacker, target, prevAttackpatern);

        ClearProjectiles();

        List<Transform> spawnTransforms = GetProjectileSpawnTransform(attacker);

        // 교체할 오브젝트가 있는가?
        if (m_ProjectilePrefab != null)
        {
            if (attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject != null)
            {
                attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject.Destroy();
            }

            // Launcher가 필요로 하는 개수만큼 발사체 생성
            int requiredCount = Launcher.GetRequiredProjectileCount();
            for (int i = 0; i < requiredCount; i++)
            {
                if (spawnTransforms.Count < i)
                    continue;
                var projectile = Managers.Resource.Instantiate<Projectile>(m_ProjectilePrefab, spawnTransforms[i]);
                projectile.transform.position = spawnTransforms[i].position;
                projectile.transform.rotation = spawnTransforms[i].rotation;
                m_SpawnProjectiles.Add(projectile);
            }

            // 즉시 발사
            if (_projectileProperty.UseProjectileLaunch)
            {
                foreach (var projectile in m_SpawnProjectiles)
                {
                    projectile.AttackReady(attacker, this);
                    projectile.transform.SetParent(null);
                }
                Launcher.Launch(m_SpawnProjectiles, attacker, target, _cachedCheckResult);
            }
        }
        else
        {
            // 손에 생성된 준비 오브젝트 사용
            if (attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject != null)
            {
                var projectile = attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject as Projectile;
                if (projectile != null)
                    m_SpawnProjectiles.Add(projectile);
            }
        }

        // 준비 오브젝트 리셋
        if (attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject != null)
            attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject = null;
    }

    private List<Transform> GetProjectileSpawnTransform(ControllableObject attacker)
    {
        if (this.Launcher.ProjectileType == E_Projectile.Multiple)
        {
            var ret = attacker.GetComponentInChildren<UnitWeaponSlotManager>();
            return ret.m_ProjectileSpawnTransforms;
        }

        var spawnTransforms = new List<Transform>();

        if (attacker is Unit unit)
        {
            var slot = unit.m_UnitWeaponSlotManager;

            // 슬롯이 없으면 유닛 자체 위치
            if (slot.m_RightHandSlot == null && slot.m_LeftHandSlot == null)
            {
                spawnTransforms.Add(unit.transform);
                return spawnTransforms; 
            }

            // 무기를 든 손의 프로젝타일 소환 위치로
            if (slot.m_RightHandSlot?.currentWeaponModel != null)
            {
                spawnTransforms.Add(slot.m_RightHandSlot.currentWeapon.m_ProjectileSpawnTransform);
                return spawnTransforms;
            }

            if (slot.m_LeftHandSlot?.currentWeaponModel != null)
            {
                spawnTransforms.Add(slot.m_LeftHandSlot.currentWeapon.m_ProjectileSpawnTransform);
                return spawnTransforms;
            }

            // 무기가 없다면 오른손 슬롯
            spawnTransforms.Add(slot.m_RightHandSlot.transform);
            return spawnTransforms;
        }

        if (attacker is Building building)
        {
            spawnTransforms.Add(building.transform);
            return spawnTransforms;
        }

        spawnTransforms.Add(attacker.transform);
        return spawnTransforms;
    }

    public override void Attack(ControllableObject attacker, GameEntity target)
    {
        if (_projectileProperty.UseProjectileLaunch)
            return;

        if (m_SpawnProjectiles == null || m_SpawnProjectiles.Count <= 0)
            return;

        // 모든 발사체 준비
        foreach (var projectile in m_SpawnProjectiles)
        {
            projectile.AttackReady(attacker, this);
            projectile.transform.SetParent(null);
        }

        // 발사
        Launcher.Launch(m_SpawnProjectiles, attacker, target, _cachedCheckResult);
    }

    public AttackPatternInfoClip GetAttackPatternInfoClip(CombatAction.OnAttackBaseEventArgs args)
    {
        AttackPatternInfoClip info = default;
        if (this.Launcher.ProjectileType == E_Projectile.Parabola)
        {
            info = args.attackPattern.GetBaseClip().FirstOrDefault(clip => clip.AttackAnimationClip.name.Contains("Parabola"));
        }
        // 위로 던지는 애니메이션이 없다면 그냥 일반 공격으로 대체
        else if (info == null)
        {
            var clips = args.attackPattern.GetBaseClip().Where(clip => !clip.AttackAnimationClip.name.Contains("Parabola"));
            info = clips.RandomPick();
        }

        return info;
    }
}
