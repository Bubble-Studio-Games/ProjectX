using UnityEngine;

[CreateAssetMenu(fileName = "MouseSettings", menuName = "Settings/Mouse Settings")]
public class MouseSettings : ScriptableObject
{
	[field: SerializeField] public bool IsMouseCursorEnabled { get; private set; } = true;
}
