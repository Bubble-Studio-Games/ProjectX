using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Define;

/// <summary>
/// 무기 아이템 - ItemObject 상속
/// </summary>
public class WeaponItem : ItemObject
{
    public E_WeaponItemType m_EWeaponItemType;
    public Transform m_ProjectileSpawnTransform; // 발사체 소환 위치
}
