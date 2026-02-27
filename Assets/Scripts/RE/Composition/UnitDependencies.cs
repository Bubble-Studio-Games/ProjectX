using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unit.Dependencies 
{
    /// <summary>
    /// 월드-> 그리드 좌표 변환
    /// </summary>
    public interface IGridService
    {
        bool TryWorldToGrid(Vector3 worldPos, out GridPosition grid);
    }
    public sealed class UnitDependencies
    {
        public Camera Camera;
        public LayerMask GroundMask;
        public IGridService GridService;
        // 나중에 필요하면 추가:
        // public IPathfinder Pathfinder;
        // public ITargetService TargetService;
    }
}
