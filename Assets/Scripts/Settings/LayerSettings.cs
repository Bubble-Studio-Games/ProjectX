// LayerData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "LayerSettings", menuName = "Game/Config/Layer")]
public class LayerSettings : ScriptableObject
{
    public LayerMask mousePlaneLayerMask;
    public LayerMask HitColLayerMask;
    public LayerMask ObstaclesLayerMask;
    public LayerMask StructLayer;
}