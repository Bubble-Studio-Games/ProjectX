using System;
using System.Collections.Generic;
using UnityEngine;

// 발사체 발사 전 소환
// 무기에 버프를 둘러서 강화하기
[CreateAssetMenu(menuName = "Attack Pattern/Ready")]
[Serializable]
public class AttackData_Ready : AttackData
{
    [Header("Spawn Object")]
    public ItemObject m_ReadyGameObjectPrefab;
    public GameObject m_FailPrefab;
    public int m_iSpawnReadyCount = 1;
    [Tooltip("무기에 붙어서 생성할지 여부")]
    public bool m_SpawnFromWeapon = true;

    [Header("Ready")]
    public float m_AttackReadyTime = 2f;
    [HideInInspector] public float lastAttackReadytime;
    public bool m_ISAttackReadyFinished => Time.time - lastAttackReadytime >= m_AttackReadyTime;

    [Header("Ready Object 최적화용도")]
    [HideInInspector] public List<(ItemObject obj, Transform spawnTransform)> keepList = new();
    [HideInInspector] public List<(ItemObject obj, Transform spawnTransform)> removeList = new();

}
