using System.Collections.Generic;
using UnityEngine;
using static Define;

[EditorShowInfo("이 스크립트를 붙이면 플레이어의 체력을 나타낸다. 이 스크립트가 붙은 GameEntity가 사망시 게임 종료")]
public class PlayerHealthComponent : MonoBehaviour, IDungeonCore
{
    public GridPosition GetGridPosition() => Managers.Grid.GetGridPosition(transform.position);

    AttributeSystem _AttributeSystem;

    [Header("Hit Effect Settings")]
    public float m_HitStunDuration = 1f;
    public Color m_HitColor = Color.red;

    private Dictionary<Material, Color> m_CoreMaterial = new();

    public void Awake()
    {
        _AttributeSystem = GetComponent<AttributeSystem>();

        // 하위 렌더러까지 색상 데이터 저장
        foreach (var render in GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in render.materials)
            {
                if (!m_CoreMaterial.ContainsKey(mat))
                    m_CoreMaterial.Add(mat, mat.color);
            }
        }

        Managers.Player.playerHealth.Register(this);
    }

    public void OnEnable()
    {
        _AttributeSystem.OnDamaged += Damaged;
        _AttributeSystem.OnDead += Dead;

    }

    public void OnDisable()
    {
        _AttributeSystem.OnDamaged -= Damaged;
        _AttributeSystem.OnDead -= Dead;
    }


    private void Damaged(OnAttackInfoEventArgs e)
    {
        float healthNormalized = _AttributeSystem.GetHealthNormalized();
        Managers.Player.playerHealth.NotifyDamaged(this, healthNormalized);
    }

    private void  Dead(OnAttackInfoEventArgs e)
    {
        Managers.Player.playerHealth.UnRegister(this);
    }
}
