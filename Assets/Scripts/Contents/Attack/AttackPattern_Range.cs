using System.Collections;
using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Attack Pattern/Range")]
public class AttackPattern_Range : AttackPattern<AttackPatternInfoClip>
{
    [Header("Spawn Object")]
    public GameObject m_ProjectilePrefab;
    private Projectile m_SpawnProjectile;

    [Header("Projectile Property")]
    [SerializeField] private bool m_bUseProjectileLaunch = false; // Projectile.Launch 사용 여부
    [SerializeField] private float StraighSpeed = 10f;
    [SerializeField] private float PalabolaSpeed = 5f;
    [SerializeField] private float detectionHitRadius = 2f;
    [SerializeField] private float CeilingHeight = 7f;
    [SerializeField] private float MaxStraightShotHeight = 1f;
    [SerializeField] private float boundHeight;
    public bool m_iIsLaunchProjectileParabola { get; private set; }

    public override E_AttackCondition CanExecute(ControllableObject attacker, GameEntity target)
    {
        if (base.CanExecute(attacker, target) >= E_AttackCondition.Fail_None)
            return base.CanExecute(attacker, target);

        // 프로젝타일 콜라이더 사이즈 (가장 긴 축)
        float colliderLength;
        
        if (attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject != null)
            colliderLength = Managers.Game.GetObjectLength(attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject.gameObject);
        else
            colliderLength = Managers.Game.GetObjectLength(m_ProjectilePrefab.gameObject);

        float obstacleHeight = LevelGrid.Instance.GetObstacleMaxHeight(attacker.GetGridPosition(), target.GetGridPosition()); ;

        float parabolaHeight = colliderLength + obstacleHeight;

        // 구조물이 거의 천장에 닿았다면 실패.
        if (parabolaHeight > CeilingHeight)
            return E_AttackCondition.Fail_IndividualCondition;


        // 구조물이 포물선을 그릴 수 있는 위치라면 장애물이라면
        if (obstacleHeight >= MaxStraightShotHeight)
        {
            m_iIsLaunchProjectileParabola = true;
            boundHeight = parabolaHeight;
        }
        // 일직선으로
        else
        {
            m_iIsLaunchProjectileParabola = false;
        }

        return E_AttackCondition.Success;
    }

    public override void StartAttack(ControllableObject attacker, GameEntity target, AttackPattern prevAttackpatern)
    {
        base.StartAttack(attacker, target, prevAttackpatern);

        Transform spawnTransform = null;

        if (attacker is Unit u &&
                u.m_UnitWeaponSlotManager.m_RightHandSlot == null &&
                u.m_UnitWeaponSlotManager.m_LeftHandSlot == null)
        {
            spawnTransform = u.transform;
        }

        if(attacker is Unit unit && spawnTransform == null)
        {
            var slot = unit.m_UnitWeaponSlotManager;

            // 무기를 든 손의 프로젝타일 소환 위치로
            if(slot.m_RightHandSlot.currentWeaponModel != null)
            {
                spawnTransform = slot.m_RightHandSlot.currentWeapon.m_ProjectileSpawnTransform;
            }

            else if(slot.m_LeftHandSlot.currentWeaponModel != null)
            {
                spawnTransform = slot.m_LeftHandSlot.currentWeapon.m_ProjectileSpawnTransform;
            }

            // 무기가 없다면 공격 손의 빈 위로
            else
            {
                spawnTransform = slot.m_RightHandSlot.transform;
            }
        }

        else if (attacker is Building building)
        {

        }

        // 교체할 오브젝트가 있는가?
        if (m_ProjectilePrefab != null)
        {
            if (attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject != null)
            {
                attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject.Destroy();
            }

            // 새로운 발사 오브젝트를 생성
            m_SpawnProjectile = Managers.Resource.Instantiate<Projectile>(m_ProjectilePrefab, spawnTransform);
            m_SpawnProjectile.transform.localPosition = Vector3.zero;
            m_SpawnProjectile.transform.localRotation = Quaternion.identity;

            // Projectile.Launch 사용하는 경우 바로 발사
            if (m_bUseProjectileLaunch)
            {
                m_SpawnProjectile.AttackReady(attacker, this);
                m_SpawnProjectile.transform.SetParent(null);
                
                Vector3 baseCenter = target.m_HitCollider.bounds.center;
                float height = target.m_HitCollider.bounds.size.y;
                Vector3 targetPos = baseCenter + Vector3.up * (height * (1f / 6f));
                
                attacker.StartCoroutine(LaunchStraight(m_SpawnProjectile, targetPos));
            }
        }
        else
        {
            // 손에 생성된 준비 오브젝트를 제거
            if (attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject != null)
            {
                m_SpawnProjectile = attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject as Projectile;
            }
        }

        // 준비 오브젝트 리셋
        if (attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject != null)
            attacker.m_ControllableObjectCombatManager.m_AttackReadyItemObject = null;
    }

    public override void Attack(ControllableObject attacker, GameEntity target)
    {
        if (m_bUseProjectileLaunch)
            return;

        Projectile projectile = m_SpawnProjectile;
        
        if (projectile == null)
            return;

        projectile.AttackReady(attacker, this);

        // 부모 분리 (손에서 떨어뜨림)
        projectile.transform.SetParent(null);

        Vector3 ProjectileStartPosition = projectile.transform.position;

        // 콜라이더의 y축 기준 2/3 지점으로
        Vector3 baseCenter = target.m_HitCollider.bounds.center;
        float height = target.m_HitCollider.bounds.size.y;
        Vector3 ProjectileEndPosition = baseCenter + Vector3.up * (height * (1f / 6f));

        if (!m_iIsLaunchProjectileParabola)
            attacker.StartCoroutine(LaunchStraight(projectile, ProjectileEndPosition));
        else
            attacker.StartCoroutine(LaunchParabola(projectile, ProjectileStartPosition, ProjectileEndPosition));
    }

    private IEnumerator LaunchStraight(Projectile projectile, Vector3 targetPos)
    {
        while (projectile != null && Vector3.Distance(projectile.transform.position, targetPos) > 0.1f)
        {
            Vector3 nextPos = Vector3.MoveTowards(
                projectile.transform.position, targetPos, StraighSpeed * Time.deltaTime);
            projectile.m_Rigidbody.MovePosition(nextPos);
            yield return new WaitForFixedUpdate(); // 반드시 FixedUpdate 주기로
        }
    }

    private IEnumerator LaunchParabola(Projectile projectile, Vector3 start, Vector3 end)
    {
        float t = 0;
        float duration = Vector3.Distance(start, end) / PalabolaSpeed;

        while (t < 1f && projectile != null)
        {
            t += Time.deltaTime / duration;

            Vector3 currentPos = Vector3.Lerp(start, end, t);
            currentPos.y += boundHeight * Mathf.Sin(t * Mathf.PI);

            projectile.m_Rigidbody.MovePosition(currentPos);
            yield return new WaitForFixedUpdate(); // FixedUpdate 주기
        }
    }
}
