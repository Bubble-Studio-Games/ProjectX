using UnityEngine;

[CreateAssetMenu(menuName = "Game/Units/UnitStatsSO", fileName = "UnitStatsSO")]
public sealed class UnitStatsSO : ScriptableObject
{
    [Header("Core")]
    public int maxHP = 100;

    [Header("Move")]
    public float moveSpeed = 3.5f;

    [Header("Combat")]
    public int attackDamage = 10;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.0f;

    [Header("Optional")]
    public int maxMP = 0;
    public int defense = 0;
}
