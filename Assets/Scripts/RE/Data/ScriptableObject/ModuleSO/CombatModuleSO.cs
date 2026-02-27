using UnityEngine;
using Unit.Dependencies;

namespace SO.Unit
{
    [CreateAssetMenu(menuName = "Game/Unit/Modules/Combat Module", fileName = "UnitModule_Combat_")]
    public sealed class CombatModuleSO : UnitModuleSO
    {
        [Header("Stats Source")]
        public UnitStatsSO statsSO;

        [Header("Create Subsystems")]
        public bool createRuntimeStats = true;
        public bool createHealth = true;
        public bool createCombatState = true;

        public override void Apply(UnitContext ctx, UnitDependencies deps, GameObject unitGo, IGameEntity entity)
        {
            if (ctx == null) return;

            if (statsSO == null)
            {
                Debug.LogWarning($"[CombatModuleSO] statsSO is null. Skip. ({name})");
                return;
            }

            // Transform 기본 바인딩 (모듈 독립성을 위해 여기서도 처리)
            if (ctx.Transform == null && unitGo != null)
                ctx.Transform = unitGo.transform;

            // 이미 등록되어 있으면 중복 등록 방지
            if (ctx.Modules.Has<CombatModule>())
            {
                Debug.LogWarning($"[CombatModuleSO] UnitCombat already exists. Skip. ({name})");
                return;
            }

            var stats = new UnitStats(statsSO);

            RuntimeStats runtime = null;
            if (createRuntimeStats)
                runtime = new RuntimeStats(stats.MaxHP, stats.MaxMP);

            Health health = null;
            if (createHealth)
            {
                if (runtime == null)
                {
                    Debug.LogWarning($"[CombatModuleSO] runtime required for Health. Skip Health. ({name})");
                }
                else
                {
                    health = new Health(runtime);
                }
            }

            CombatState state = null;
            if (createCombatState)
                state = new CombatState();

            ctx.Modules.Add(new CombatModule(stats, runtime, health, state));
        }
    }
}
