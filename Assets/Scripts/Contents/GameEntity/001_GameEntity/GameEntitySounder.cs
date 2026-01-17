using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

[Serializable]
public  class GameEntitySounder : MonoBehaviour
{
    public Dictionary<string, AudioSource> m_DicAudioSources = new Dictionary<string, AudioSource> ();

    private GameEntity m_GameEntity;
    private IInteractable m_Interactable;

    [Header("Spawn And DeSpawn")]
    public AudioClip[] SpawnClipList; // 소환
    public AudioClip[] DeSpawnClipList; // 소멸

    [Header("Live")]
    public AudioClip[] ReviveClipList; // 사망
    public AudioClip[] DestroyClipList; // 사망

    [Header("Battle")]
    public AudioClip[] DamagedClipList; // 피격
    public AudioClip[] CriticalDamagedClipList; // 피격
    public AudioClip[] PhaseChangeClipList;
    public AudioClip[] DodgeClipList;

    [Header("Move")]
    public AudioClip[] WalkClipList;
    public AudioClip[] RunClipList;

    [Header("Order")]
    public AudioClip[] InteractClipList;
    public AudioClip[] SelectedClipList; // 스폰 오브젝트 선택

    protected virtual void Awake()
    {
        Managers.Sound.InitAudioSourceWith3dObject<E_GameEntityClipType>(transform, ref m_DicAudioSources);

        // Event Set 
        m_GameEntity = GetComponentInParent<GameEntity>();
        m_Interactable = GetComponentInParent<IInteractable>();
    }

    private void OnEnable()
    {
        m_GameEntity.OnObjectSpawnStart += Spawnd;
        m_GameEntity.OnSpawnObjectSelected += SpawnObjectSelected;
        m_GameEntity.OnObjectDespawned += OnObjectDespawned;
        if(m_Interactable != null)
            m_Interactable.OnInteracted += Interact;

        m_GameEntity.OnRevived += Revived;
        m_GameEntity.OnDead += Dead;

        m_GameEntity.OnDamaged += DamagedSoundPlay;

        m_GameEntity.OnPhaseChange += PhaseChange;

        m_GameEntity.OnAttackReadyFailed += AttackReadyFail;
        m_GameEntity.OnAttackPoint += AttackSoundPlay;

        m_GameEntity.OnStep += StepSoundPlay;
    }

    private void OnDisable()
    {
        m_GameEntity.OnObjectSpawnStart -= Spawnd;
        m_GameEntity.OnSpawnObjectSelected -= SpawnObjectSelected;
        m_GameEntity.OnObjectDespawned -= OnObjectDespawned;
        if (m_Interactable != null)
            m_Interactable.OnInteracted -= Interact;

        m_GameEntity.OnRevived -= Revived;
        m_GameEntity.OnDead -= Dead;

        m_GameEntity.OnDamaged -= DamagedSoundPlay;

        m_GameEntity.OnPhaseChange -= PhaseChange;

        m_GameEntity.OnAttackReadyFailed -= AttackReadyFail;
        m_GameEntity.OnAttackPoint -= AttackSoundPlay;

        m_GameEntity.OnStep -= StepSoundPlay;
    }

    private void Spawnd() => SoundPlay(SpawnClipList, E_GameEntityClipType.Spawn.ToString());
    private void OnObjectDespawned() =>SoundPlay(DeSpawnClipList, E_GameEntityClipType.DeSpawn.ToString());
    private void SpawnObjectSelected() =>SoundPlay(SelectedClipList, E_GameEntityClipType.Select.ToString());
    private void Revived() =>  SoundPlay(ReviveClipList, E_GameEntityClipType.Revive.ToString());
    private void Dead(OnAttackInfoEventArgs e) => SoundPlay(DestroyClipList, E_GameEntityClipType.Death.ToString());
    private void PhaseChange() => SoundPlay(PhaseChangeClipList, E_GameEntityClipType.PhaseChange.ToString());
    private void Interact() => SoundPlay(InteractClipList, E_GameEntityClipType.Interact.ToString());
    

    // 이동 상태에 따른 발자국 사운드 플레이
    public virtual void StepSoundPlay()
    {
        switch (m_GameEntity.CurrentMoveType)
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

    public virtual void DamagedSoundPlay(OnAttackInfoEventArgs e) 
    {
        switch (e.EHitDeCisionType)
        {
            case E_HitDecisionType.Hit:
                SoundPlay(DamagedClipList, E_GameEntityClipType.Damaged.ToString());
                break;
            case E_HitDecisionType.CriticalHit:
                SoundPlay(CriticalDamagedClipList, E_GameEntityClipType.Damaged.ToString());
                break;
            case E_HitDecisionType.Evasion:
                SoundPlay(DodgeClipList, E_GameEntityClipType.Evasion.ToString());
                break;
            case E_HitDecisionType.Counter:
                break;
            case E_HitDecisionType.AttackMiss: // 공격자 입장에서 쓰고 싶은데, 무기 종류에 따라
                break;
        }
    }

    public void AttackSoundPlay(AttackData attack) => SoundPlay(attack.selectInfoClip.AttackSuccessAudioClip, E_GameEntityClipType.Attack.ToString());

    public void AttackMissSoundPlay(AttackData attack) =>SoundPlay(attack.selectInfoClip.AttackMissAudioClip, E_GameEntityClipType.Attack.ToString());

    public void AttackReadyFail(AttackData readyPattern) =>SoundPlay(readyPattern.selectInfoClip.AttackFailAudioClip, E_GameEntityClipType.Attack.ToString());





    public void SoundPlay(AudioClip audioClip, string audioClipName, int loop = 0, float pitch = 1.0f)
    {
        // Check Audio Source
        if (!m_DicAudioSources.TryGetValue(audioClipName, out var source))
        {
            Debug.LogWarning($"{audioClipName} Audio Source 가 없습니다.");
            return;
        }

        if (audioClip == null)
        {
            //Debug.Log($"{m_GameEntity.name} 캐릭터의 {audioClipName}의 Audio Clip이 없습니다.");
            return;
        }

        source.pitch = pitch;

        if (loop == 0)
            source.PlayOneShot(audioClip);
        else
        {
            source.clip = audioClip;
            source.Play();
        }
    }

    public void SoundPlay(AudioClip[] audioClip, string audioClipName, int loop = 0, float pitch = 1.0f)
    {
        // Check Clip
        if (audioClip.Length == 0)
        {
            //Debug.Log($"{m_GameEntity.name} 의 {audioClipName} Audio Clip이 없습니다.");
            return;
        }

        SoundPlay(audioClip.RandomPick(), audioClipName, loop, pitch);
    }
}
