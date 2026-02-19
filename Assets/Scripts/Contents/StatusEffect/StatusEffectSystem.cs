using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class StatusEffectSystem : MonoBehaviour, IStatusEffectSystem
{
    private readonly Dictionary<Type, Queue<IStatusEffect>> _effectPools = new();
    private readonly List<IStatusEffect> _activeEffects = new();

    private void Awake()
    {
        Managers.SceneServices.Register<IStatusEffectSystem>(this);
    }

    private void OnDestroy()
    {
        _effectPools.Clear();
        _activeEffects.Clear();
    }

    private void Update()
    {
        OnUpdateFrameTick();
    }

    private void OnUpdateFrameTick()
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            // Effect 업데이트 - false를 반환하면 종료됨
            if (_activeEffects[i].Tick(Time.deltaTime))
                continue;
                
            // 종료된 Effect를 Pool에 반환
            var effectType = _activeEffects[i].GetType();

            if (_effectPools.ContainsKey(effectType) == false)
                _effectPools[effectType] = new Queue<IStatusEffect>();

            _effectPools[effectType].Enqueue(_activeEffects[i]);
            _activeEffects.RemoveAt(i);
        }
    }

    public void ApplyStatusEffect<T>(GameEntity target, GameEntity source, AttackData data)
        where T : IStatusEffect, new()
    {
        if (target == null)
        {
            Debug.LogError("[StatusEffectSystem] Target이 null입니다!");
            return;
        }

        // 기존 같은 Effect 찾기 - Refresh 처리
        var existingEffect = FindExistingEffect<T>(target, source);
        if (existingEffect != null)
        {
            existingEffect.Init(target, source, data);
            return;
        }

        // Pool에서 Effect 가져오기 (없으면 새로 생성)
        var effectType = typeof(T);

        if (_effectPools.ContainsKey(effectType) == false)
            _effectPools[effectType] = new Queue<IStatusEffect>();

        var ret = _effectPools[effectType].Count > 0
            ? _effectPools[effectType].Dequeue()
            : new T();

        // Effect 초기화 및 활성화
        ret.Init(target, source, data);
        _activeEffects.Add(ret);
    }

    private T FindExistingEffect<T>(GameEntity target, GameEntity source)
        where T : IStatusEffect
    {
        foreach (var effect in _activeEffects)
        {
            if (effect is T typedEffect && effect.Target == target && effect.Source == source)
                return typedEffect;
        }

        return default;
    }
}
