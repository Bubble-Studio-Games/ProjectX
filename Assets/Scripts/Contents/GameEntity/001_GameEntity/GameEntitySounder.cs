using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;
using static Unity.Cinemachine.CinemachineInputAxisController;
using Random = UnityEngine.Random;

[Serializable]
public  class GameEntitySounder : MonoBehaviour
{
    [SerializeField] private Transform m_AudiosTransform;
    public Dictionary<string, AudioSource> m_DicAudioSources = new Dictionary<string, AudioSource> ();

    [Header("Ref")]
    protected AttributeSystem m_StatSystem;
    private GameEntity m_GameEntity;

    [Header("Spawn And DeSpawn")]
    public AudioClip[] SpawnClipList; // 소환
    public AudioClip[] DeSpawnClipList; // 소멸
    public AudioClip[] SpawnObjectSelectedClipList; // 스폰 오브젝트 선택

    [Header("Live")]
    public AudioClip[] ReviveClipList; // 사망
    public AudioClip[] DestroyClipList; // 사망

    [Header("Battle")]
    public AudioClip[] DamagedClipList; // 피격
    public AudioClip[] CriticalDamagedClipList; // 피격

    protected virtual void Awake()
    {
        Managers.Sound.InitAudioSourceWith3dObject<E_GameEntityClipType>(m_AudiosTransform, ref m_DicAudioSources);

        // Event Set 
        m_GameEntity = GetComponent<GameEntity>();

        m_GameEntity.OnObjectSpawned += (s, e) => SoundPlay(SpawnClipList, E_GameEntityClipType.Spawn.ToString());
        m_GameEntity.OnSpawnObjectSelected += (s, e) => SoundPlay(SpawnObjectSelectedClipList, E_GameEntityClipType.Select.ToString());
        m_GameEntity.OnObjectDespawned += (s, e) => SoundPlay(DeSpawnClipList, E_GameEntityClipType.DeSpawn.ToString());

        m_StatSystem = GetComponentInParent<AttributeSystem>();
        m_StatSystem.OnRevived += (s, e) => SoundPlay(ReviveClipList, E_GameEntityClipType.Revive.ToString());
        m_StatSystem.OnDead += (s, e) => SoundPlay(DestroyClipList, E_GameEntityClipType.Death.ToString());

        m_StatSystem.OnDamaged += DamagedSoundPlay;
    }

    public virtual void StepSoundPlay() { }


    public virtual void DamagedSoundPlay(object sender, AttributeSystem.OnAttackInfoEventArgs e) 
    {
        if (e.AttackPattern != null)
            return;

        if (m_GameEntity.m_AttributeSystem.m_IsDead)
            return;

        switch (e.EHitDeCisionType)
        {
            case E_HitDecisionType.Hit:
                SoundPlay(DamagedClipList, E_GameEntityClipType.Damaged.ToString());
                break;
            case E_HitDecisionType.CriticalHit:
                SoundPlay(CriticalDamagedClipList, E_GameEntityClipType.Damaged.ToString());
                break;
        }
    }



    public void AttackSoundPlay(AttackPattern attack)
    {
        SoundPlay(attack.AttackAudioClip, E_GameEntityClipType.Attack.ToString());
    }

    public void AttackMissPlay(object sender, AttackPattern attack)
    {
        SoundPlay(attack.AttackMissClipList, E_GameEntityClipType.Attack.ToString());
    }

    public void AttackReadyFailPlay()
    {
        var attack = m_GameEntity.GetAction<CombatAction>().m_ThisTimeAttack;

        if (attack == null)
            return;

        // AttackPattern_Ready로 캐스팅하고 Ready 타입인지 확인
        if (attack is not AttackPattern_Ready readyPattern)
            return;

        SoundPlay(readyPattern.ReadyFailAudioClip, E_GameEntityClipType.Attack.ToString());
    }

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
            Debug.Log($"{m_GameEntity.name} 의 {audioClipName}의 Audio Clip이 없습니다.");
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
