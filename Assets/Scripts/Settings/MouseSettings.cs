using UnityEngine;

[CreateAssetMenu(fileName = "MouseSettings", menuName = "Game/Config/Mouse")]
public class MouseSettings : ScriptableObject
{
	[field: SerializeField] public bool IsMouseCursorEnabled { get; set; } = true;
}
