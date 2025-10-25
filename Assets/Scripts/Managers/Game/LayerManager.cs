using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerManager : MonoBehaviour
{
    public static LayerManager Instance { get; private set; }

    public LayerMask mousePlaneLayerMask;
    public LayerMask PlayerInteractableLayerMask;
    public LayerMask HitColLayerMask;
    public LayerMask ObstaclesLayerMask;
    public LayerMask ControllableObjectLayerMask;
    public LayerMask m_StructLayer;
    public LayerMask m_IgnoreLayerMask;

    private void Awake()
    {
        Instance = this;

        // Layer 설정
        mousePlaneLayerMask = 1 << LayerMask.NameToLayer("MousePlane");
        PlayerInteractableLayerMask = 
            1 << LayerMask.NameToLayer("Controllable") |
            1 << LayerMask.NameToLayer("Interactable");
        ObstaclesLayerMask = 1 << LayerMask.NameToLayer("Obstacles");
        ControllableObjectLayerMask = 1 << LayerMask.NameToLayer("Controllable");
        HitColLayerMask = 1 << LayerMask.NameToLayer("HitCol");
        m_StructLayer = //1 << LayerMask.NameToLayer("Default") |
            1 << LayerMask.NameToLayer("MousePlane") |
            1 << LayerMask.NameToLayer("Interactable") |
            1 << LayerMask.NameToLayer("Obstacles") |
            1 << LayerMask.NameToLayer("Trap")
            ;
        m_IgnoreLayerMask = 1 << LayerMask.NameToLayer("Ignore Raycast");
    }
}
