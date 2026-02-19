using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using static Define;


public sealed class AttackPattern_Ready : AttackPattern<AttackData_Ready>
{
    public override void Init(AttackData_Ready data)
    {
        base.Init(data);
        data.lastAttackReadytime = -data.m_AttackReadyTime;
    }

    /// <summary>
    /// Ready가 아직 쿨/마나/콤보 OK여도 “준비시간”이 끝났는지 체크가 필요하면 여기서 조건 추가.
    /// 기존 AttackData_Ready는 m_ISAttackReadyFinished가 있었음. :contentReference[oaicite:8]{index=8}
    /// </summary>
    public override (E_AttackCondition condition, List<GridPosition> CanAttackablePos)
        CanExecute(GameEntity attacker, GameEntity target, AttackData_Ready data, AttackData prevData)
    {
        // 공통 조건 먼저
        var (cond, pos) = base.CanExecute(attacker, target, data, prevData);
        if (cond != E_AttackCondition.Success)
            return (cond, pos);

        if (!data.m_ISAttackReadyFinished)
            return (E_AttackCondition.Fail_IndividualCondition, pos);

        return (E_AttackCondition.Success, pos);
    }

    public override void StartAttack(GameEntity attacker, GameEntity target, AttackData_Ready data, AttackData prev)
    {
        // 공통 StartAttack(쿨 갱신, prev ready 제거, clip 선택)은 base가 처리 :contentReference[oaicite:6]{index=6}
        base.StartAttack(attacker, target, data, prev);

        if (data.m_ReadyGameObjectPrefab == null)
            return;

        // 1) 기존 준비 오브젝트 정보
        var existingList = attacker.m_CombatManager.m_AttackReadyItemObject;

        // 2) 비교용 리스트(지역변수)
        var keepList = new List<(ItemObject obj, Transform spawnTransform)>();
        var removeList = new List<(ItemObject obj, Transform spawnTransform)>();

        string prefabName = data.m_ReadyGameObjectPrefab.name.Replace("(Clone)", "").Trim();

        foreach (var (obj, spawnT) in existingList)
        {
            if (obj == null) continue;

            string objName = obj.name.Replace("(Clone)", "").Trim();
            if (objName == prefabName)
                keepList.Add((obj, spawnT));
            else
                removeList.Add((obj, spawnT));
        }

        // 3) 제거 대상 위치(Transform)는 재활용 후보
        List<Transform> reusableTransforms = removeList
            .Where(x => x.spawnTransform != null)
            .Select(x => x.spawnTransform)
            .ToList();

        // 제거 대상만 Destroy
        foreach (var (obj, _) in removeList)
            obj?.Destroy();

        // 리스트에서 제거 항목 제거
        attacker.m_CombatManager.m_AttackReadyItemObject
            .RemoveAll(x => removeList.Any(r => r.obj == x.obj));

        int remainingCount = keepList.Count;
        int desiredCount = data.m_iSpawnReadyCount;

        // spawn transform 후보
        List<Transform> initSpawnTransforms =
            attacker.m_ProjectileSpawnProvider.GetProjectileSpawnTransforms(data.m_SpawnFromWeapon, desiredCount);

        int reuseIndex = 0;

        // 4) 부족한 만큼 생성 (removeList의 위치 우선 재활용)
        for (int i = remainingCount; i < desiredCount; i++)
        {
            Transform spawnT;
            if (reuseIndex < reusableTransforms.Count)
                spawnT = reusableTransforms[reuseIndex++];
            else
                spawnT = initSpawnTransforms[i % initSpawnTransforms.Count];

            var newObj = Managers.Resource.Instantiate<ItemObject>(data.m_ReadyGameObjectPrefab.gameObject, spawnT);
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;

            attacker.m_CombatManager.m_AttackReadyItemObject.Add((newObj, spawnT));
        }

        // 5) 유지 오브젝트 위치 동기화
        foreach (var (obj, t) in keepList)
            obj.transform.SetPositionAndRotation(t.position, t.rotation);
    }

    public override void EndAttack(GameEntity attacker, GameEntity target, AttackData_Ready data)
    {
        base.EndAttack(attacker, target, data);

        // Ready의 “준비 시간”은 유닛별 상태로 저장해야 함
        data.lastAttackReadytime = Time.time;

        // 기존과 동일: Ready AttackPattern 등록 
        attacker.m_CombatManager.m_ReadyAttackPattern.Add(data);
    }

    public override void StartAttackFail(GameEntity attacker, GameEntity target, AttackData_Ready data)
    {
        base.StartAttackFail(attacker, target, data);

        if (data.m_FailPrefab != null)
        {
            var go = Managers.Resource.Instantiate(data.m_FailPrefab);
            attacker.StartCoroutine(ObjectDestroy(go, 3f));
        }
    }
}
