using UnityEngine;
using Unit.Dependencies;

namespace SO.Unit
{
    public abstract class UnitModuleSO : ScriptableObject
    {
        [Tooltip("모듈 적용 순서. 낮을수록 우선 적용됨.")]
        public int order = 0;

        public abstract void Apply(UnitContext ctx, UnitDependencies deps, GameObject unitGo, IGameEntity entity);
    }
}