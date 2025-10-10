using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Define
{
    public enum E_BuildingType
    {
        None,
        Spawner
    }

    public enum E_SetupObjectOffsetChange
    {
        None,
        YOffset,
        XZOffset,
        All
            
    }

    public enum E_GridCheckType
    {
        Walkable,   // 그리드가 유효한 위치인지
        HasUnit,    // 유닛이 있는지
        Reserved,    // 예약된 위치인지
        Obstacle // 장애물에 막혀 있는지
    }

    public enum E_WeaponItemType
    {
        None,
        Sword,
        Bow
    }

    public enum E_DamagedValueTextDisplayType
    {
        Up,
        MiddleBounce,

    }

    public enum E_HitDecisionType
    {
        Hit, // 공격 적중
        CriticalHit, // 치명타 공격 적중
        AttackMiss, // 공격 미스
        Evasion, // 회피
        Counter, // 반격
    }

    public enum E_AttackCondition
    {
        Success,
        Fail_None,
        Fail_CoolTime,
        Fail_Distance,
        Fail_IndividualCondition,
        Fail_ManaCost,
        Fail_NotHasPrevAttack,
    }

    public enum E_AttackType
    {
        Ready,
        Melee,   // 무기 휘두름, 칸 밀착, 애니메이션 실행
        Ranged   // 투사체 발사, 일정 거리 안에서 공격
    }

    public enum E_AttackEffectType
    {
        None,
        Damage,     // 직접 피해
        Dot,        // 지속 피해 (DoT)
        Buff,       // 강화
        Debuff,     // 약화
        Heal,       // 회복
        Knockback,  // 밀치기 등 위치 이동
    }

    public enum E_UISoundType
    {

    }

    public enum E_PlayerSoundType
    {

    }

    public enum E_GameEntityClipType
    {
        // Animation State의 이름과 똑같이 해야 됨.

        // Spawn And DeSpawn
        Spawn,
        DeSpawn,
        Select,

        // Live
        Revive,
        Death,

        // Controllable
        Idle,
        Walk,
        Run,
        Attack,
        AttackMiss,
        AttackReadyFail,

        Damaged,
        PhaseChange,
        Evasion
    }

    public enum GridVisualType
    {
        White,
        Blue,
        Red,
        RedSoft,
        Yellow,
    }

    public enum E_Dir
    {
        North = 0,
        NorthEast = 4,
        NorthWest = 5,
        East = 1,
        South = 2,
        SouthEast = 6,
        SouthWest =7,
        West =  3
    }

    public enum E_MoveType
    {
        Idle,
        Walk,
        Run,
    }

    public enum E_ObjectType
    {
        None = 0, 
        Unit = 1,
        Building = 2,
        Interact = 3, 
        AutoTrigger = 4,
        Obstacle,
    }

    #region Base

    public enum E_TeamId
    {
        Player,
        Monster,
        NPC,
        None,
    }

    public enum Scene
    {
        Unknown = 0,
        Start = 1,
        Lobby = 2,
        Game = 3,
    }

    public enum Sound
    {
        Bgm = 0,
        Effect = 1,
        MaxCount,
    }

    public enum UIEvent
    {
        Click,
        Pressed,
        PointerDown,
        PointerUp,
        
    }

    public enum CursorType
    {
        None,
        Arrow,
        Hand,
        Look,
    }
    #endregion
}
