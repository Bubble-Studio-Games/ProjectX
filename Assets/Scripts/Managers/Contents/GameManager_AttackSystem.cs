using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public partial class GameManager
{
    #region Attack Grid Caculate
    public IEnumerable<GridPosition> GetAllDirAndAllAttackpatternDistance(GameEntity attacker, GridPosition targetGridPosition, bool checkHasPath = false)
    {
        HashSet<GridPosition> result = new();

        var attackerGridPosition = attacker.GetGridPosition();

        // 공격자가 가지고 있는 모든 공격 패턴의 오프셋을 이용해서 destgridpostion에 모든 방향을 더한다.
        var offsets = GetAllPatternOffsets(attacker.m_AttributeSystem.m_AttackPatterns);
        // 시작 위치(origin) 및 방향(8방향) 계산
        foreach (var dir in Enum.GetValues(typeof(E_Dir)).Cast<E_Dir>())
        {
            foreach (var offset in offsets)
            {
                GridPosition canAttackPos = Util.ToGridPosition(offset, targetGridPosition, dir);

                // 유효한 범위만 가져오기
                if (!Managers.SceneServices.Grid.IsValidGridPosition(canAttackPos)) // 유효한 위치만 추가
                    continue;

                if (checkHasPath)
                {
                    if (!Managers.SceneServices.Pathfinder.HasPath(attackerGridPosition, canAttackPos))
                        continue;
                }


                result.Add(canAttackPos);
            }
        }

        return result;
    }

    // 공격 오프셋 가져오기
    public HashSet<GridPosition> GetAllPatternOffsets(IEnumerable<AttackData> attackPatterns)
    {
        HashSet<GridPosition> unique = new();

        foreach (var pattern in attackPatterns)
        {
            unique.AddRange(GetPatternOffsets(pattern));
        }

        return unique;
    }

    public HashSet<GridPosition> GetPatternOffsets(AttackData pattern)
    {
        if (pattern == null)
            return new();

        // 키 생성
        var key = (
            pattern.m_ERangeShapeType,
            GetRangeMinMax(pattern),
            pattern.m_ERangeFillType
        );

        // 이미 계산된 캐시가 있으면 그대로 반환
        if (_patternOffsetCache.TryGetValue(key, out var cached))
            return cached;

        // 없으면 새로 계산
        var computed = CalculatePatternOffsets(pattern);

        // 캐싱
        _patternOffsetCache[key] = computed;

        return computed;
    }

    private HashSet<GridPosition> CalculatePatternOffsets(AttackData pattern)
    {
        var unique = new HashSet<GridPosition>();
        if (pattern == null)
            return unique;

        // 1) bounding box 결정: custom offsets가 있으면 그것의 박스, 없으면 radius 기반 박스

        var range = GetRangeMinMax(pattern);

        switch (pattern.m_ERangeShapeType)
        {
            // 최소 값, 최대 값을 구해서 중심을 반경으로 사각형 형태의 범위를 구한다.
            case E_RangeShapeType.Square:

                for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                {
                    for (int x = range.MinX; x <= range.MaxX; x++)
                    {
                        for (int z = range.MinZ; z <= range.MaxZ; z++)
                        {
                            var offset = new GridPosition(x, z, f);

                            if (pattern.m_ERangeFillType == E_RangeFillType.FullRange)
                                unique.Add(offset);

                            // 경계선 위치한 셀만 true
                            else if (pattern.m_ERangeFillType == E_RangeFillType.OuterRing)
                            {
                                if (x == range.MinX || x == range.MaxX ||
                                    z == range.MinZ || z == range.MaxZ)
                                    unique.Add(offset);
                            }
                            // 경계선 안쪽 위치한 셀만 true
                            else
                            {
                                if (x != range.MinX && x != range.MaxX && z != range.MinZ && z != range.MaxZ)
                                    unique.Add(offset);
                            }

                        }
                    }
                }

                break;
            case E_RangeShapeType.Checker: 
                for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                {
                    for (int x = range.MinX; x <= range.MaxX; x++)
                    {
                        for (int z = range.MinZ; z <= range.MaxZ; z++)
                        {
                            var offset = new GridPosition(x, z, f);

                            if (pattern.m_ERangeFillType == E_RangeFillType.FullRange)
                            {
                                if ((x + z) % 2 == 0)
                                    unique.Add(offset);
                            }

                            // 경계선 위치한 셀만 true
                            else if (pattern.m_ERangeFillType == E_RangeFillType.OuterRing)
                            {
                                if (x == range.MinX || x == range.MaxX ||
                                    z == range.MinZ || z == range.MaxZ)
                                    if ((x + z) % 2 == 0)
                                        unique.Add(offset);
                            }
                            // 경계선 안쪽 위치한 셀만 true
                            else
                            {
                                if (x != range.MinX && x != range.MaxX && z != range.MinZ && z != range.MaxZ)
                                    if ((x + z) % 2 == 0)
                                        unique.Add(offset);
                            }

                        }
                    }
                }

                break;
            case E_RangeShapeType.Diamond:
                {
                    int maxX = range.MaxX;
                    int maxZ = range.MaxZ;

                    for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                    {
                        for (int x = -maxX; x <= maxX; x++)
                        {
                            for (int z = -maxZ; z <= maxZ; z++)
                            {
                                // 다이아몬드 형태 기본 조건 (비율 계산 없이)
                                // |x| + |z| <= radius
                                int radius = Mathf.Max(maxX, maxZ);
                                if (Mathf.Abs(x) + Mathf.Abs(z) > radius)
                                    continue;

                                // 🔹 경계선 판정
                                // 이 셀에서 한 칸이라도 나가면 범위를 벗어나는가? → 경계
                                bool isEdge = false;
                                int[][] dirs = new int[][] {
                                                new int[] { 1, 0 },
                                                new int[] { -1, 0 },
                                                new int[] { 0, 1 },
                                                new int[] { 0, -1 }
                                            };


                                foreach (var dir in dirs)
                                {
                                    int nx = x + dir[0];
                                    int nz = z + dir[1];
                                    if (Mathf.Abs(nx) + Mathf.Abs(nz) > radius)
                                    {
                                        isEdge = true;
                                        break;
                                    }
                                }

                                var offset = new GridPosition(x, z, f);

                                switch (pattern.m_ERangeFillType)
                                {
                                    case E_RangeFillType.FullRange:
                                        unique.Add(offset);
                                        break;

                                    case E_RangeFillType.OuterRing:
                                        if (isEdge)
                                            unique.Add(offset);
                                        break;

                                    case E_RangeFillType.Inner:
                                        if (!isEdge)
                                            unique.Add(offset);
                                        break;
                                }
                            }
                        }
                    }
                    break;
                }

            case E_RangeShapeType.Arc: // 수정 필요
                {
                    float halfAngle = pattern.m_ArcAngle * 0.5f;

                    // 반경 계산
                    int radius = Mathf.Max(Mathf.Abs(range.MaxX), Mathf.Abs(range.MaxZ));

                    for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                    {
                        for (int x = range.MinX; x <= range.MaxX; x++)
                        {
                            for (int z = range.MinZ; z <= range.MaxZ; z++)
                            {
                                var offset = new GridPosition(x, z, f);

                                // 거리 계산
                                float dist = Mathf.Sqrt(x * x + z * z);
                                if (dist == 0f || dist > radius)
                                    continue;

                                // 각도 계산 (Z축이 forward)
                                float angle = Mathf.Atan2(z, x) * Mathf.Rad2Deg;
                                float diff = Mathf.Abs(Mathf.DeltaAngle(0f, angle)); // 0° 기준 전방

                                if (diff > halfAngle)
                                    continue; // 부채꼴 영역 밖

                                // FillType 처리
                                switch (pattern.m_ERangeFillType)
                                {
                                    case E_RangeFillType.FullRange:
                                        unique.Add(offset);
                                        break;

                                    case E_RangeFillType.OuterRing:
                                        // 외곽(거리 거의 radius인 셀)
                                        if (Mathf.RoundToInt(dist) == radius)
                                            unique.Add(offset);
                                        break;

                                    case E_RangeFillType.Inner:
                                        // 내부 (OuterRing 제외)
                                        if (dist < radius)
                                            unique.Add(offset);
                                        break;
                                }
                            }
                        }
                    }
                    break;
                }

            case E_RangeShapeType.Triangle:
                {
                    int zMax = range.MaxZ;

                    for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                    {
                        for (int z = 0; z < zMax; z++)
                        {
                            int halfWidth = (zMax - 1) - z; // 위로 갈수록 폭이 줄어듦

                            for (int x = -halfWidth; x <= halfWidth; x++)
                            {
                                bool isEdge = (z == 0) || (x == -halfWidth) || (x == halfWidth) || (z == zMax - 1);
                                var offset = new GridPosition(x, z, f);

                                switch (pattern.m_ERangeFillType)
                                {
                                    case E_RangeFillType.FullRange:
                                        unique.Add(offset);
                                        break;

                                    case E_RangeFillType.OuterRing:
                                        if (isEdge)
                                            unique.Add(offset);
                                        break;

                                    case E_RangeFillType.Inner:
                                        if (!isEdge)
                                            unique.Add(offset);
                                        break;
                                }
                            }
                        }
                    }
                    break;
                }

            case E_RangeShapeType.ReverseTriangle:
                {
                    int zMax = range.MaxZ;

                    for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                    {
                        for (int z = 0; z < zMax; z++)
                        {
                            int halfWidth = z; // 아래로 갈수록 폭이 넓어짐

                            for (int x = -halfWidth; x <= halfWidth; x++)
                            {
                                bool isEdge = (z == 0) || (x == -halfWidth) || (x == halfWidth) || (z == zMax - 1);

                                var offset = new GridPosition(x, z, f);

                                switch (pattern.m_ERangeFillType)
                                {
                                    case E_RangeFillType.FullRange:
                                        unique.Add(offset);
                                        break;

                                    case E_RangeFillType.OuterRing:
                                        if (isEdge)
                                            unique.Add(offset);
                                        break;

                                    case E_RangeFillType.Inner:
                                        if (!isEdge)
                                            unique.Add(offset);
                                        break;
                                }
                            }
                        }
                    }
                    break;
                }

            case E_RangeShapeType.Plus:
                for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                {
                    for (int x = range.MinX; x <= range.MaxX; x++)
                    {
                        for (int z = range.MinZ; z <= range.MaxZ; z++)
                        {
                            var offset = new GridPosition(x, z, f);

                            // 십자형 형태: x==0 또는 z==0
                            if (x == 0 || z == 0)
                            {
                                if (pattern.m_ERangeFillType == E_RangeFillType.FullRange)
                                {
                                    unique.Add(offset);
                                }
                                else if (pattern.m_ERangeFillType == E_RangeFillType.OuterRing)
                                {
                                    // 끝단만
                                    if (Mathf.Abs(x) == Mathf.Abs(range.MaxX) ||
                                        Mathf.Abs(z) == Mathf.Abs(range.MaxZ))
                                        unique.Add(offset);
                                }
                                else
                                {
                                    // 경계선 제외 내부만 (Full - Outer)
                                    if (x > range.MinX && x < range.MaxX &&
                                        z > range.MinZ && z < range.MaxZ)
                                        unique.Add(offset);
                                }
                            }
                        }
                    }
                }
                break;
            case E_RangeShapeType.Vertical:
                for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                {
                    for (int z = range.MinZ; z <= range.MaxZ; z++)
                    {
                        var offset = new GridPosition(0, z, f);

                        switch (pattern.m_ERangeFillType)
                        {
                            case E_RangeFillType.FullRange:
                                unique.Add(offset);
                                break;

                            case E_RangeFillType.OuterRing:
                                // 위/아래 끝단만
                                if (z == range.MinZ || z == range.MaxZ)
                                    unique.Add(offset);
                                break;

                            case E_RangeFillType.Inner:
                                if (z > range.MinZ && z < range.MaxZ)
                                    unique.Add(offset);
                                break;
                        }
                    }
                }
                break;

            case E_RangeShapeType.Horizontal:
                for (int f = range.MinFloor; f <= range.MaxFloor; f++)
                {
                    for (int x = range.MinX; x <= range.MaxX; x++)
                    {
                        var offset = new GridPosition(x, 0, f);

                        switch (pattern.m_ERangeFillType)
                        {
                            case E_RangeFillType.FullRange:
                                unique.Add(offset);
                                break;

                            case E_RangeFillType.OuterRing:
                                // 왼쪽/오른쪽 끝단만
                                if (x == range.MinX || x == range.MaxX)
                                    unique.Add(offset);
                                break;

                            case E_RangeFillType.Inner:
                                if (x > range.MinX && x < range.MaxX)
                                    unique.Add(offset);
                                break;
                        }
                    }
                }
                break;

            case E_RangeShapeType.CustomList:
                {
                    if (pattern.m_RangeOffset == null || pattern.m_RangeOffset.Count == 0)
                        break;

                    var (minX, maxX, minZ, maxZ, minF, maxF) = GetRangeMinMax(pattern);

                    HashSet<GridPosition> fullRange = new();
                    HashSet<GridPosition> outerRing = new();
                    HashSet<GridPosition> innerRange = new();

                    // 1️⃣ FullRange: min~max 전부 포함
                    for (int f = minF; f <= maxF; f++)
                    {
                        for (int x = minX; x <= maxX; x++)
                        {
                            for (int z = minZ; z <= maxZ; z++)
                            {
                                fullRange.Add(new GridPosition(x, z, f));
                            }
                        }
                    }

                    // 2️⃣ OuterRing: 사용자 지정 오프셋 그대로
                    outerRing.UnionWith(pattern.m_RangeOffset);

                    // 3️⃣ InnerRange: FullRange - OuterRing
                    innerRange.UnionWith(fullRange);
                    innerRange.ExceptWith(outerRing);

                    // 4️⃣ FillType에 따라 결과 반환
                    switch (pattern.m_ERangeFillType)
                    {
                        case E_RangeFillType.FullRange:
                            unique.UnionWith(fullRange);
                            break;

                        case E_RangeFillType.OuterRing:
                            unique.UnionWith(outerRing);
                            break;

                        case E_RangeFillType.Inner:
                            unique.UnionWith(innerRange);
                            break;
                    }

                    break;
                }

        }

        return unique;
    }

    #endregion

    /// <summary>
    /// 🔍 AttackPattern의 실행 조건을 검사하고,
    /// 지정한 E_AttackCondition만 필터링해서 반환.
    /// </summary>
    public IEnumerable
        <(AttackData pattern, E_AttackCondition condition, IEnumerable<GridPosition> canAttackPosition)>
        EvaluateAttackPatternsByCondition(
        GameEntity owner,
        GameEntity target,
        params E_AttackCondition[] conditions)
    {
        List<(AttackData pattern, E_AttackCondition condition, IEnumerable<GridPosition>)> result = new();

        IEnumerable<AttackData> datas = owner.m_AttributeSystem.m_AttackPatterns;
        var thisTimeAttack = owner.GetAction<CombatAction>().m_ThisTimeAttack;

        if (owner == null || datas == null)
            return default;

        foreach (var data in datas)
        {
            if (data == null)
                continue;

            //var attackCanResult = pattern.CanExecute(owner, target);
            var attackCanResult = Managers.Game.AttackPattern(data).CanExecute(owner, target, data, thisTimeAttack);


            // 지정된 조건 중 하나라도 일치하면 추가
            if (conditions.Contains(attackCanResult.condition))
            {
                // 원하는 조건을 만족했지만 그리드 타일 조건을 만족하는지 체크?
                result.Add((data, attackCanResult.condition, attackCanResult.CanAttackablePos));
            }
        }

        return result;
    }

    private readonly Dictionary<Type, AttackPattern> _map= new Dictionary<Type, AttackPattern>()
    {
        { typeof(AttackData_Melee),  new AttackPattern_Melee()  },
        { typeof(AttackData_Range),  new AttackPattern_Range()  },
        { typeof(AttackData_Heal),   new AttackPattern_Heal()   },
        { typeof(AttackData_Ready),  new AttackPattern_Ready()  },
        { typeof(AttackData_Summon), new AttackPattern_Summon() },
    };


    public AttackPattern AttackPattern(AttackData data) => _map[data.GetType()];

    // key: AttackData.Id / value: 다음 콤보 Id 집합
    private static Dictionary<int, HashSet<int>> _nextIdSetCache = new();
    public IReadOnlyDictionary<int, HashSet<int>> NextIdSetCache => _nextIdSetCache;

    /// <summary>
    /// 게임 시작 시 1회 호출
    /// Resources/Data/Attack Data 폴더의 모든 AttackData를 로드해서 캐시를 만든다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BuildNextComboCache()
    {
        if (_nextIdSetCache.Count > 0)
        {
            Debug.Log("Attack Combo Data Dic에 이미 데이터가 있습니다. ");
            return;
        }

        _nextIdSetCache.Clear();

        var allAttacks = Resources.LoadAll<AttackData>("Data/Attack Data");
        if (allAttacks == null || allAttacks.Length == 0)
        {
            Debug.LogWarning("[AttackPattern] No AttackData found in Resources/Data/Attack Data");
            return;
        }

        foreach (var data in allAttacks)
        {
            if (data == null)
                continue;

            if (_nextIdSetCache.ContainsKey(data.Id))
            {
                Debug.LogError($"[AttackPattern] Duplicate AttackData Id: {data.Id}");
                continue;
            }

            var set = new HashSet<int>();
            var nexts = data.NextAttacks;
            if (nexts != null)
            {
                for (int i = 0; i < nexts.Length; i++)
                {
                    var next = nexts[i];
                    if (next != null)
                        set.Add(next.Id);
                }
            }

            _nextIdSetCache.Add(data.Id, set);
        }
    }

    private (int MinX, int MaxX, int MinZ, int MaxZ, int MinFloor, int MaxFloor) GetRangeMinMax(AttackData data)
    {
        if(data.m_RangeOffsetMinMax == default)
            data.m_RangeOffsetMinMax = Util.GetRangeMinMaxFromOffsets(data.m_RangeOffset);
        
        return data.m_RangeOffsetMinMax;
    }
}

