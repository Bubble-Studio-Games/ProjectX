using System;
using System.Collections.Generic;
using UnityEngine;
using Unit.Dependencies;
using Unit.ActionDecider;
using SO.Unit;
using System.Linq;

namespace Unit.Composer 
{
    /// <summary>
    /// 의존성묶음에 따라 유닛의 종류를 구분하여고 정의, 유닛의 구성요소 객체들을 생성 하여 UnitBootstrap에 전달 
    /// </summary>
    public static class UnitComposerFactory
    {
        private static readonly Dictionary<EUnitType, Func<IUnitComposer>> table = new()
        {
            { EUnitType.Player, () => new PlayerComposer() },
            { EUnitType.Monster, () => new MonsterComposer() },
            { EUnitType.NPC, () => new NPCComposer() },
        };

        public static IUnitComposer Create(EUnitType type)
        {
            if (table.TryGetValue(type, out var ctor))
                return ctor();
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown UnitKind");
        }
    }
    /// <summary>
    /// 유닛 분류 enum
    /// </summary>
    public enum EUnitType {Player,Monster,NPC }


    /// <summary>
    /// TODO: 파일 분리 
    /// </summary>
    public interface IUnitComposer
    {
        UnitContext CreateContext(UnitDependencies deps, GameObject unitGo, IGameEntity entity);
        IActionDecider CreateDecider(UnitDependencies deps, GameObject unitGo);
    }
    public sealed class PlayerComposer : IUnitComposer
    {
        // 플레이어의 "개념적 가능 행동"을 여기서 고정
        // (스탯/전투 붙기 전이라도, 최소 Move는 넣어두는 게 자연스러움)
        private const UnitCapabilities DefaultCaps =
            UnitCapabilities.CanMove
            // | UnitCapabilities.CanAttack
            // | UnitCapabilities.CanBeHit
            // | UnitCapabilities.CanDie
            ;

        public UnitContext CreateContext(UnitDependencies deps, GameObject unitGo, IGameEntity entity)
            => new UnitContext(entity, DefaultCaps);

        public IActionDecider CreateDecider(UnitDependencies deps, GameObject unitGo)
            => new PlayerClickMoveDecider(deps);
    }
    public sealed class MonsterComposer : IUnitComposer
    {
        private readonly string targetTag;

        // 몬스터 기본 가능 행동(필요시 확장)
        private const UnitCapabilities DefaultCaps =
            UnitCapabilities.CanMove
            // | UnitCapabilities.CanAttack
            // | UnitCapabilities.CanBeHit
            // | UnitCapabilities.CanDie
            ;

        public MonsterComposer(string targetTag = "Player")
        {
            this.targetTag = targetTag;
        }

        public UnitContext CreateContext(UnitDependencies deps, GameObject unitGo, IGameEntity entity)
            => new UnitContext(entity, DefaultCaps);

        public IActionDecider CreateDecider(UnitDependencies deps, GameObject unitGo)
        {
            var tag = string.IsNullOrWhiteSpace(targetTag) ? "Player" : targetTag;
            var targetGo = GameObject.FindWithTag(tag); // G는 나중에 서비스로 교체
            return new AIChaseDecider(deps, targetGo != null ? targetGo.transform : null);
        }
    }
    public sealed class NPCComposer : IUnitComposer
    {
        private const UnitCapabilities DefaultCaps =
            UnitCapabilities.None
            // 필요해지면:
            // UnitCapabilities.CanMove
            // | UnitCapabilities.CanBeHit
            ;

        public UnitContext CreateContext(UnitDependencies deps, GameObject unitGo, IGameEntity entity)
            => new UnitContext(entity, DefaultCaps);

        public IActionDecider CreateDecider(UnitDependencies deps, GameObject unitGo)
            => new NPCIdleDecider();
    }

    public sealed class ConfigUnitComposer : IUnitComposer
    {
        //if (ctx.Features.TryGet<UnitCombat>(out var combat))  combat.Stats / combat.Runtime / combat.Health / combat.State
        private readonly UnitConfigSO config;

        public ConfigUnitComposer(UnitConfigSO config)
        {
            this.config = config;
        }

        public UnitContext CreateContext(UnitDependencies deps, GameObject unitGo, IGameEntity entity)
        {
            var caps = config != null ? config.capabilities : UnitCapabilities.None;
            var ctx = new UnitContext(entity, caps);

            // Transform 기본 바인딩(모듈이 또 처리해도 상관없음)
            if (ctx.Transform == null && unitGo != null)
                ctx.Transform = unitGo.transform;

            // 모듈 레시피 적용
            if (config != null && config.modules != null && config.modules.Count > 0)
            {
                foreach (var module in config.modules
                             .Where(m => m != null)
                             .OrderBy(m => m.order))
                {
                    module.Apply(ctx, deps, unitGo, entity);
                }
            }

            return ctx;
        }
        public IActionDecider CreateDecider(UnitDependencies deps, GameObject unitGo)
        {
            if (config == null)
                return new NPCIdleDecider();

            switch (config.deciderType)
            {
                case UnitDeciderType.PlayerClickMove:
                    return new PlayerClickMoveDecider(deps);

                case UnitDeciderType.AIChaseTargetTag:
                    {
                        var tag = string.IsNullOrWhiteSpace(config.chaseTargetTag) ? "Player" : config.chaseTargetTag;
                        var targetGo = GameObject.FindWithTag(tag);
                        return new AIChaseDecider(deps, targetGo != null ? targetGo.transform : null);
                    }
                case UnitDeciderType.AIChaseAttackTargetTag:
                    {
                        var tag = string.IsNullOrWhiteSpace(config.chaseTargetTag) ? "Player" : config.chaseTargetTag;
                        var targetGo = GameObject.FindWithTag(tag);
                        return new AIChaseAttackDecider(deps, targetGo != null ? targetGo.transform : null);
                    }
                case UnitDeciderType.NPCIdle:
                default:
                    return new NPCIdleDecider();
            }
        }
    }
}


