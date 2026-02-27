using UnityEngine;
using Unit.Dependencies;

namespace Unit.ActionDecider 
{
    /// <summary>
    /// 액션에 대한 정책에 따른 로직을 담당하는 중간계층 구조 
    /// 예시: 클릭에 따른 이동-> ClickMoveDecider
    /// ClickMoveDecider객체는 클릭을 통한 이동에 해당하는 MoveAction객체를 Controller에 요청하는 구조로 동작  
    /// </summary>
    public interface IActionDecider
    {
        void Init(UnitContext context, ActionController controller);
        void TickDecision(float dt);
    }

    /// <summary>
    /// TODO: 파일분리 
    /// </summary>
    public sealed class PlayerClickMoveDecider : IActionDecider
    {
        private UnitContext ctx;
        private ActionController controller;

        private readonly UnitDependencies deps;

        public PlayerClickMoveDecider(UnitDependencies deps)
        {
            this.deps = deps;
        }

        public void Init(UnitContext context, ActionController controller)
        {
            ctx = context;
            this.controller = controller;
        }

        public void TickDecision(float dt)
        {
            if (ctx == null || controller == null || deps == null) return;

            // 1) Cancel
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                controller.RequestIdle();
                return;
            }
            // 2) Click Move (우클릭)
            if (Input.GetMouseButtonDown(1))
            {
                if (TryGetClickedGrid(out var target))
                    controller.RequestMove(target);
                return;
            }
        }

        private bool TryGetClickedGrid(out GridPosition target)
        {
            target = default;

            if (deps.Camera == null || deps.GridService == null) return false;

            var ray = deps.Camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 500f, deps.GroundMask))
                return false;

            return deps.GridService.TryWorldToGrid(hit.point, out target);
        }
    }
    public sealed class AIChaseDecider : IActionDecider
    {
        private UnitContext ctx;
        private ActionController controller;

        private readonly UnitDependencies deps;
        private readonly Transform targetTransform;

        public AIChaseDecider(UnitDependencies deps, Transform targetTransform)
        {
            this.deps = deps;
            this.targetTransform = targetTransform;
        }

        public void Init(UnitContext context, ActionController controller)
        {
            ctx = context;
            this.controller = controller;
        }

        public void TickDecision(float dt)
        {
            if (ctx == null || controller == null || deps?.GridService == null) return;
            if (targetTransform == null) return;

            // 한 프레임 1개: 그냥 이동만 예시
            if (deps.GridService.TryWorldToGrid(targetTransform.position, out var targetGrid))
            {
                controller.RequestMove(targetGrid);
            }
        }
    }
    public sealed class NPCIdleDecider : IActionDecider
    {
        public void Init(UnitContext context, ActionController controller) { }
        public void TickDecision(float dt) { }
    }
    public sealed class AIChaseAttackDecider : IActionDecider
    {
        private UnitContext ctx;
        private ActionController controller;

        private readonly UnitDependencies deps;
        private readonly Transform targetTransform;

        public AIChaseAttackDecider(UnitDependencies deps, Transform targetTransform)
        {
            this.deps = deps;
            this.targetTransform = targetTransform;
        }

        public void Init(UnitContext context, ActionController controller)
        {
            ctx = context;
            this.controller = controller;
        }

        public void TickDecision(float dt)
        {
            if (ctx == null || controller == null || deps?.GridService == null) return;
            if (targetTransform == null) return;
            if (ctx.Transform == null) return;

            // 1) 내 CombatModule 없으면 "공격 AI"가 아니므로 추적만 하거나 Idle
            if (!ctx.Modules.TryGet<CombatModule>(out var selfCombat))
            {
                // 추적만 원하면 아래 유지, 아니면 controller.RequestIdle()로 바꿔도 됨
                TryChase();
                return;
            }

            // 2) 내 사망 체크
            if (selfCombat.Health != null && selfCombat.Health.IsDead) return;

            // 3) 거리 기반: 사거리면 공격, 아니면 추적
            float dist = Vector3.Distance(ctx.Transform.position, targetTransform.position);
            if (dist <= selfCombat.Stats.AttackRange)
            {
                // 공격 요청: targetTransform -> UnitContext로 바꾸는 게 가장 깔끔
                // 당장은 Transform만 알고 있으니, 엔티티 시스템에서 Transform->Context 찾는 함수를 쓰는 게 좋다.
                // 예: EntityManager.TryGetByTransform(targetTransform, out var targetCtx)

                if (EntityManager.TryGetByTransform<UnitContext>(targetTransform, out var targetCtx))
                {
                    // 공격 요청
                    controller.RequestAttack(targetCtx);
                }
                return;
            }

            // 사거리 밖이면 추적 이동
            TryChase();

            void TryChase()
            {
                if (deps.GridService.TryWorldToGrid(targetTransform.position, out var targetGrid))
                    controller.RequestMove(targetGrid);
            }
        }
    }
}
