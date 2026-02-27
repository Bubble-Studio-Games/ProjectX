using UnityEngine;
using System;
using System.Collections.Generic;

namespace SO.Unit
{
    /// <summary>
    /// 유닛 행동 정책 
    /// </summary>
    public enum UnitDeciderType
    {
        PlayerClickMove,
        AIChaseTargetTag,
        AIChaseAttackTargetTag,
        NPCIdle
    }

    /// <summary>
    /// 유닛 초기화 행동 
    /// </summary>
    public enum UnitInitialAction
    {
        Idle,
        None
    }

    /// <summary>
    /// 유닛 수행 가능 행위
    /// </summary>
    [Flags]
    public enum UnitCapabilities
    {
        None = 0,
        CanMove = 1 << 0,
        CanAttack = 1 << 1,
        CanBeHit = 1 << 2,
        CanDie = 1 << 3,
        // 필요하면 계속 추가
        // CanInteract = 1<<4,
    }

    /// <summary>
    /// 에디터에서 유닛 종류를 정의하는 데이터(ScriptableObject)
    /// - UnitBootstrap이 이 데이터를 바탕으로 Context/Decider/초기상태를 구성한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Unit/Unit Config", fileName = "UnitConfig_")]
    public sealed class UnitConfigSO : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "New Unit";

        [Header("Capabilities")]
        public UnitCapabilities capabilities = UnitCapabilities.CanMove;

        [Header("Decider")]
        public UnitDeciderType deciderType = UnitDeciderType.NPCIdle;
        public string chaseTargetTag = "Player";

        [Header("Initial")]
        public UnitInitialAction initialAction = UnitInitialAction.Idle;

        [Header("Modules")] // Composer 에서 순서에 따라 적용
        public List<UnitModuleSO> modules = new();
    }
}
