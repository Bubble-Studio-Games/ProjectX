using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class NPCOutlineView : MonoBehaviour
{
    [Header("Color Settings")]
    [SerializeField] private Color _hostileColor = new Color(0.5f, 0f, 0f, 1f); 
    [SerializeField] private Color _neutralColor = Color.gray;                  
    [SerializeField] private Color _friendlyColor = Color.green;            

    [Header("Animation Settings")]
    [SerializeField] private float _transitionDuration = 1.0f; 
    [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private NPC _owner;
    private Color _currentColor = Color.gray;
    private Coroutine _colorCoroutine;
    private List<Material> _valideMats = new();     // 유효한 OutLine Material 리스트
    private E_NPCState _previousState = E_NPCState.Neutral;  // 이전 상태 추적

    public void Init(NPC owner)
    {
        if (owner == null)
        {
            Debug.LogError($"[NPCOutlineView] NPC owner가 설정되지 않았습니다");
            return;
        }

        _owner = owner;

        _owner.OnStateChanged += (owner, state) => UpdateOutlineColor(owner, state);
        InitOutlineMaterials();
        InitOutlineColor();
    }

    private void OnDestroy()    
    {
        StopColorTransition();
        StopAllCoroutines();
        _valideMats.Clear();
        _valideMats = null;
        _colorCoroutine = null;

        if (_owner == null)
            return;

        _owner = null;
    }


    private void InitOutlineMaterials()
    {
        if (_owner == null)
            return;

        _valideMats.Clear();

        foreach (var (mat, obj) in _owner.GetModelsMaterial())
        {
            if (mat.HasProperty("_OutlineColor") == false)
                continue;

            // 초기 alpha 값 확인 - 0 이하면 적용하지 않음.
            float alphaColor = mat.GetColor("_OutlineColor").a;
            if (alphaColor <= 0.0f)
                continue;

            _valideMats.Add(mat);
        }

        if (_valideMats.Count <= 0)
            Debug.LogWarning($"{_owner.name}에 활성화된 아웃라인 Material이 없습니다.");
    }

    private void InitOutlineColor()
    {
        if (_owner == null)
            return;

        Color initialColor = GetColorForState(_owner.State);
        ApplyOutlineColor(initialColor);
        _currentColor = initialColor;
        _previousState = _owner.State;
    }

    private void UpdateOutlineColor(NPC owner, E_NPCState state)
    {
        Color targetColor = GetColorForState(state);

        if (_currentColor != targetColor)
        {
            PlayStateTransitionSound(_previousState, state);
            StopColorTransition();
            _colorCoroutine = StartCoroutine(AnimateColorTransition(_currentColor, targetColor));
            _previousState = state;
        }
        else
        {
            ApplyOutlineColor(targetColor);
        }
    }

    private Color GetColorForState(E_NPCState state)
    {
        return state switch
        {
            E_NPCState.Hostile => _hostileColor,
            E_NPCState.Neutral => _neutralColor,
            E_NPCState.Friendly => _friendlyColor,
            _ => _neutralColor
        };
    }

    /// <summary>
    /// 색상을 머티리얼에 즉시 적용 - 유효한 Material만 처리
    /// </summary>
    private void ApplyOutlineColor(Color color)
    {
        if (_owner == null)
            return;

        foreach (Material mat in _valideMats)
            mat.SetColor("_OutlineColor", color);

        _currentColor = color;
    }

    private IEnumerator AnimateColorTransition(Color fromColor, Color toColor)
    {
        if (fromColor == toColor)
            yield break;

        float elapsedTime = 0f;

        while (elapsedTime < _transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / _transitionDuration);

            float curveValue = _transitionCurve.Evaluate(t);
            Color currentColor = Color.Lerp(fromColor, toColor, curveValue);

            ApplyOutlineColor(currentColor);

            yield return null;
        }

        // 최종 색상 적용
        ApplyOutlineColor(toColor);
        _colorCoroutine = null;
    }

    private void StopColorTransition()
    {
        if (_colorCoroutine != null)
        {
            StopCoroutine(_colorCoroutine);
            _colorCoroutine = null;
        }
    }

    /// <summary>
    /// 성향 전환 사운드 재생
    /// </summary>
    private void PlayStateTransitionSound(E_NPCState fromState, E_NPCState toState)
    {
        // TODO: 성향 전환 사운드 재생
        return;

        string soundPath = (fromState, toState) switch
        {
            (E_NPCState.Neutral, E_NPCState.Hostile) => "Transition_Aggro",
            (E_NPCState.Hostile, E_NPCState.Neutral) => "Transition_Warning",
            (E_NPCState.Neutral, E_NPCState.Friendly) => "Transition_Calm",
            (E_NPCState.Friendly, E_NPCState.Neutral) => "Transition_Warning",
            (E_NPCState.Hostile, E_NPCState.Friendly) => "Transition_Calm",
            (E_NPCState.Friendly, E_NPCState.Hostile) => "Transition_Aggro",
            _ => null
        };

        if (soundPath != null && Managers.Sound != null)
        {
            Managers.Sound.Play(soundPath, 0, Define.Sound.Effect);
        }
    }

    /// <summary>
    /// 색상 설정 - Editor에서 확인용
    /// </summary>
    public void SetColors(Color hostile, Color neutral, Color friendly)
    {
        _hostileColor = hostile;
        _neutralColor = neutral;
        _friendlyColor = friendly;
    }

    /// <summary>
    /// 애니메이션 설정 - Editor에서 조정용
    /// </summary>
    public void SetAnimationSettings(float duration, AnimationCurve curve)
    {
        _transitionDuration = duration;
        _transitionCurve = curve;
    }
}
