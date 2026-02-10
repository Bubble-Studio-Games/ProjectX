using UnityEngine;

[CreateAssetMenu(menuName = "ProjectX/Animation/AnimationProfile", fileName = "AnimProfile_")]
public sealed class AnimationProfile : ScriptableObject
{
    [Header("Controller")]
    [Tooltip("이미 에디터에서 만들어 둔 AnimatorOverrideController(또는 RuntimeAnimatorController)")]
    public RuntimeAnimatorController controller;

    [Header("Optional")]
    [Tooltip("기본 재생 상태(필요하면). 비워두면 건드리지 않음.")]
    public string defaultStateName = "Idle";

    [Tooltip("애니메이터 초기 속도")]
    public float animatorSpeed = 1f;
}
