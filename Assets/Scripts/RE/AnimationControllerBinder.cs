using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class AnimationControllerBinder : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private SteppedClipCache steppedCache;

    [Header("How to match")]
    [Tooltip("Override의 '원본 키'로 사용할 placeholder clip 이름 규칙. 보통 State/Motion 이름과 동일하게 둠.")]
    [SerializeField] private bool useClipNameAsKey = true;

    [Header("Debug")]
    [SerializeField] private bool logResult = true;

    private AnimatorOverrideController overrideController;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        Bind();
    }

    public void Bind()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            if (logResult) Debug.LogWarning("[AnimBinder] Animator/Controller missing.", this);
            return;
        }

        if (steppedCache == null)
        {
            if (logResult) Debug.LogWarning("[AnimBinder] SteppedClipCache not assigned.", this);
            return;
        }

        // base controller 기반 override 생성
        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        int replaced = 0;
        int total = overrides.Count;

        for (int i = 0; i < overrides.Count; i++)
        {
            var placeholder = overrides[i].Key;
            if (placeholder == null) continue;

            // 키는 "placeholder clip 이름" (Idle/Run/Attack...)
            var key = useClipNameAsKey ? placeholder.name : placeholder.name;

            if (steppedCache.TryGet(key, out var stepped) && stepped != null)
            {
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(placeholder, stepped);
                replaced++;
            }
        }

        overrideController.ApplyOverrides(overrides);
        animator.runtimeAnimatorController = overrideController;

        if (logResult)
            Debug.Log($"[AnimBinder] overrides applied: {replaced}/{total} ({gameObject.name})", this);
    }
}
