using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

public sealed class AttackPattern_Range : AttackPattern<AttackData_Range>
{
    public override (E_AttackCondition condition, List<GridPosition> CanAttackablePos)
    CanExecute(GameEntity attacker, GameEntity target, AttackData_Range data, AttackData prevData)
    {
        var baseAttackPattern = base.CanExecute(attacker, target, data, prevData);

        if (baseAttackPattern.condition >= E_AttackCondition.Fail_None)
            return baseAttackPattern;

        float colliderLength = Managers.Game.GetObjectColliderLongLength(data.m_ProjectilePrefab.gameObject);
        float obstacleHeight =
            Util.GetObstacleMaxHeight(
                Managers.SceneServices.Pathfinder,
                Managers.SceneServices.Grid,
                attacker.GetGridPosition(), target.GetGridPosition());

        data.context = new LaunchContext(colliderLength, obstacleHeight);

        // 장애물이 너무 높으면 실패
        if (colliderLength + obstacleHeight >= FLOOR_HEIGHT)
            return (E_AttackCondition.Fail_IndividualCondition, default);


        return baseAttackPattern;
    }


    public override void StartAttack(GameEntity attacker, GameEntity target, AttackData_Range data, AttackData prevAttackdata)
    {
        base.StartAttack(attacker, target, data, prevAttackdata);

        var combatManager = attacker.m_CombatManager;
        if (combatManager == null)
        {
            Debug.Log($"{attacker}에서 controllableObjectCombatManager가 발견되지 않았습니다.");
            return;
        }

        if (data.m_ProjectilePrefab == null)
        {
            Debug.Log($"{attacker}에서 data.m_ProjectilePrefab가 발견되지 않았습니다.");
            return;
        }

        // 기존 오브젝트 정보 가져오기 (삭제 X)
        var existingList = attacker.m_CombatManager.m_AttackReadyItemObject;

        // 비교용 리스트 초기화
        data.keepList.Clear();
        data.removeList.Clear();
        data.m_SpawnProjectiles.Clear();

        foreach (var (obj, spawnT) in existingList)
        {
            if (obj == null) continue;

            // 프리팹 이름으로 비교 (Clone 제거 후 비교)
            string objName = obj.name.Replace("(Clone)", "").Trim();
            string prefabName = data.m_ProjectilePrefab.name.Replace("(Clone)", "").Trim();

            // 동일 프리팹이라면 유지
            if (objName == prefabName)
                data.keepList.Add((obj, spawnT));
            else
                data.removeList.Add((obj, spawnT));
        }

        // 제거 대상 오브젝트만 삭제 (spawnTransform은 보존)
        List<Transform> reusableTransforms = data.removeList
            .Where(x => x.spawnTransform != null)
            .Select(x => x.spawnTransform)
            .ToList();

        //  제거 대상 오브젝트만 삭제
        foreach (var (obj, _) in data.removeList)
        {
            obj?.Destroy();
        }

        // 남은 개수
        int remainingCount = data.keepList.Count;
        int desiredCount = data.m_iSpawnProjectileCount;

        // 필요한 만큼 새로 생성
        List<Transform> initSpawnTransforms
            = attacker.m_ProjectileSpawnProvider.GetProjectileSpawnTransforms(data.m_SpawnFromWeapon, desiredCount);


        // 새로 생성해야 할 개수만큼 생성
        //     → 제거된 위치(reusableTransforms)를 우선 재사용
        int reuseIndex = 0;

        for (int i = remainingCount; i < desiredCount; i++)
        {
            Transform spawnT = null;

            // 기존 제거된 위치부터 사용
            if (reuseIndex < reusableTransforms.Count)
            {
                spawnT = reusableTransforms[reuseIndex];
                reuseIndex++;
            }
            else
            {
                // 부족하면 새 위치를 사용
                spawnT = initSpawnTransforms[i % initSpawnTransforms.Count];
            }

            //Debug.Log("Range에서 새로운 준비 오브젝트를 생성");

            var newObj = Managers.Resource.Instantiate<Projectile>(data.m_ProjectilePrefab, spawnT);
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;
            data.m_SpawnProjectiles.Add(newObj);
        }

        // Projectile 생성 및 타겟 할당
        data.m_SpawnProjectiles.AddRange(
            attacker.m_CombatManager.m_AttackReadyItemObject
            .Where(x => x.obj is Projectile)
            .Select(x => x.obj as Projectile)
            .ToList());

        List<GameEntity> m_tempTargets = GetTargets(attacker, target, data);

        if (m_tempTargets != null && m_tempTargets.Count > 0)
        {
            for (int i = 0; i < data.m_SpawnProjectiles.Count; i++)
                data.m_SpawnProjectiles[i].AttackReady(attacker, data, m_tempTargets[i]);

            // 즉시 발사
            if (data.m_IsImmediateLaunch)
                Managers.SceneServices.CoroutineRunner.Run(LaunchProjectileCoroutine(attacker, data));
        }

        //  리스트에서 제거한 오브젝트 항목 삭제
        attacker.m_CombatManager.m_AttackReadyItemObject.Clear();

        // Animation
        if (data.context.ObstacleHeight >= 1)
        {
            data.m_EAttackAnimationType = E_AttackAnimationType.Parabola;
        }
        else
        {
            data.m_EAttackAnimationType = E_AttackAnimationType.None;

        }
    }

    private List<GameEntity> GetTargets(GameEntity attacker, GameEntity target, AttackData_Range data)
    {
        var result = GetAttackGridPositions(attacker, target, data);

        List<GameEntity> targets = result.targetGridList.Select(p => Managers.SceneServices.Grid.GetCellEntity(p)).ToList();

        if (result.targetGridList.Count() == 0)
            return default;

        // 실제로 사용할 타겟 리스트
        List<GameEntity> assignedTargets = new();

        // ① 적 1명인 경우 -> 모든 발사체가 같은 타겟
        if (targets.Count == 1)
        {
            for (int i = 0; i < data.m_iSpawnProjectileCount; i++)
                assignedTargets.Add(targets[0]);
        }
        // ② 적의 수와 발사체 수가 같은 경우 -> 1:1 대응
        else if (targets.Count == data.m_iSpawnProjectileCount)
        {
            assignedTargets.AddRange(targets);
        }
        // ③ 적이 발사체보다 많으면 -> 랜덤으로 뽑기
        else if (targets.Count > data.m_iSpawnProjectileCount)
        {
            // 중복 없는 랜덤 샘플링
            assignedTargets = targets.OrderBy(_ => UnityEngine.Random.value)
                                     .Take(data.m_iSpawnProjectileCount)
                                     .ToList();
        }
        // ④ 적이 더 적은 경우(예: 2명밖에 없는데 3발 쏴야 함)
        else if (targets.Count < data.m_iSpawnProjectileCount)
        {
            // 적들을 순환하면서 배분
            for (int i = 0; i < data.m_iSpawnProjectileCount; i++)
            {
                int index = i % targets.Count;
                assignedTargets.Add(targets[index]);
            }
        }

        return assignedTargets;
    }

    public override void Attack(GameEntity attacker, GameEntity target, AttackData_Range data)
    {
        base.Attack(attacker, target, data);

        if (data.m_IsImmediateLaunch)
            return;

        if (data.m_SpawnProjectiles == null || data.m_SpawnProjectiles.Count <= 0)
            return;

        Managers.SceneServices.CoroutineRunner.Run(LaunchProjectileCoroutine(attacker, data));
    }

    // 애니메이션에서 event를 호출하기 때문에 분리 위치가 안 맞음. 반드시 한 프레임 늦춰야 됨
    private IEnumerator LaunchProjectileCoroutine(GameEntity attacker, AttackData_Range data)
    {
        yield return new WaitForEndOfFrame();

        // 모든 발사체 준비
        for (int i = 0; i < data.m_SpawnProjectiles.Count; i++)
        {
            var projectile = data.m_SpawnProjectiles[i];
            projectile.transform.SetParent(null, true); // true는 월드 위치 유지
            data.Launcher.Launch(projectile, attacker, projectile.m_Target, data.context);
        }
    }


    protected override AttackPatternInfoClip SelectClip(AttackData_Range data)
    {
        if (data.m_EAttackAnimationType == E_AttackAnimationType.Parabola)
        {
            if (data.m_AttackPatternInfoClips.Any(clip => clip.AttackAnimationClip.name.Contains("Parabola")))
                return data.m_AttackPatternInfoClips.Where(clip => clip.AttackAnimationClip.name.Contains("Parabola")).RandomPick();
        }

        return data.m_AttackPatternInfoClips.Where(clip => !clip.AttackAnimationClip.name.Contains("Parabola")).RandomPick();
    }

}
