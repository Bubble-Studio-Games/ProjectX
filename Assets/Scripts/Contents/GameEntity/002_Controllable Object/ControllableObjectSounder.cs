using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Define;

public class ControllableObjectSounder : GameEntitySounder
{
    private ControllableObject m_ControllableObject;

    [Header("Controllable Object")]
    public AudioClip[] WalkClipList;
    public AudioClip[] RunClipList;
    public AudioClip[] AttackMissClipList;
    public AudioClip[] PhaseChangeClipList;
    public AudioClip[] DodgeClipList;

    protected override void Awake()
    {
        base.Awake();

        m_ControllableObject = GetComponent<ControllableObject>();
        m_ControllableObject.OnSpawnObjectSelected += (s, e) => SoundPlay(SpawnObjectSelectedClipList, E_GameEntityClipType.Select.ToString());

        m_StatSystem.OnDamaged += (s, e) => AttackReadyFailPlay();

        if (m_ControllableObject.TryGetComponent<CombatAction>(out CombatAction combat))
        {
            combat.OnPhaseChange += (s, e) => SoundPlay(PhaseChangeClipList, E_GameEntityClipType.PhaseChange.ToString());
        }
    }

    // 이동 상태에 따른 발자국 사운드 플레이
    public override void StepSoundPlay()
    {
        switch (m_ControllableObject.m_EMoveType)
        {
            case E_MoveType.Walk:
                SoundPlay(WalkClipList, E_GameEntityClipType.Walk.ToString());
                break;
            case E_MoveType.Run:
                SoundPlay(RunClipList, E_GameEntityClipType.Run.ToString());
                break;
            default:
                break;
        }
    }

    public void AttackSoundPlay(AttackPattern attack)
    {
        m_ControllableObject.GetSounderManager().SoundPlay(attack.AttackAudioClip, E_GameEntityClipType.Attack.ToString());
    }

    public void AttackMissPlay(object sender, EventArgs e)
    {
        SoundPlay(AttackMissClipList, E_GameEntityClipType.Attack.ToString());
    }

    public void AttackReadyFailPlay()
    {
        var attack = m_ControllableObject.GetAction<CombatAction>().m_ThisTimeAttack;

        if (attack == null)
            return;

        // AttackPattern_Ready로 캐스팅하고 Ready 타입인지 확인
        if (attack is not AttackPattern_Ready readyPattern)
            return;

        SoundPlay(readyPattern.ReadyFailAudioClip, E_GameEntityClipType.Attack.ToString());
    }

    public override void DamagedSoundPlay(object sender, StatSystem.OnAttackInfoEventArgs e)
    {
        if (e.AttackPattern != null)
            return;

        base.DamagedSoundPlay(sender, e);

        switch (e.EHitDeCisionType)
        {
            case E_HitDecisionType.Evasion:
                SoundPlay(DodgeClipList, E_GameEntityClipType.Evasion.ToString());
                break;
            case E_HitDecisionType.Counter:
                break;
            case E_HitDecisionType.AttackMiss: // 공격자 입장에서 쓰고 싶은데, 무기 종류에 따라
                break;
        }
    }

}
