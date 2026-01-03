using UnityEngine;

/// <summary>
/// 다이얼로그 관련 설정 - ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "DialogueSettings", menuName = "Settings/Dialogue Settings")]
public class DialogueSettings : ScriptableObject
{
	[field: SerializeField, Range(0.01f, 3.0f)] public float TypingText { get; private set; } = 0.5f;
	[field: SerializeField, Range(0f, 1f)] public float ActiveSpeakerAlpha { get; private set; } = 1f;
	[field: SerializeField, Range(0f, 1f)] public float InactiveSpeakerAlpha { get; private set; } = 0.5f;
	[field: SerializeField, Range(0.1f, 2f)] public float PortraitFadeDuration { get; private set; } = 0.3f;
	[field: SerializeField, Range(0.1f, 2f)] public float DialogueUIAnimDuration { get; private set; } = 0.4f;
}
