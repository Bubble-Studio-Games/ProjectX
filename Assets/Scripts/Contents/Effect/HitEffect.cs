using static Define;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[EditorShowInfo("이 스크립트를 붙이면 데미지를 받았을 때 오브젝트의 모든 메쉬의 색이 빨갛게 변했다가 되돌아옴.")]
public class HitEffect : MonoBehaviour
{
    GameEntity _GameEntity;

    [Header("Hit Effect")]
    public float hitDuration = 1f;
    public Color hitColor = Color.red;
    public ParticleSystem hitParticles;

    Dictionary<Material, Color> originalColors = new();

    void Awake()
    {
        _GameEntity = GetComponent<GameEntity>();

        foreach (var r in GetComponentsInChildren<Renderer>())
            foreach (var m in r.materials)
                originalColors[m] = m.color;
    }

    void OnEnable()
    {
        _GameEntity.OnDamaged += PlayHitEffect;
    }

    void OnDisable()
    {
        _GameEntity.OnDead -= PlayHitEffect;
    }

    void PlayHitEffect(OnAttackInfoEventArgs e)
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            foreach (var m in r.materials)
                originalColors[m] = m.color;

        foreach (var kvp in originalColors)
        {
            kvp.Key.DOKill();
            kvp.Key.color = hitColor;
            kvp.Key.DOColor(kvp.Value, "_BaseColor", hitDuration);
        }

        if(hitParticles != null)
            hitParticles.Play();
    }
}
