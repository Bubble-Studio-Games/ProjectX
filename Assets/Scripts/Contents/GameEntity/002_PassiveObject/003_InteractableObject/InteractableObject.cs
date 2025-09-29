using UnityEngine;
using static Define;

[RequireComponent(typeof(Poolable))]
public class InteractableObject : PassiveObject
{
    InteractableObject()
    {
        m_TeamId = E_TeamId.None;
        m_ObjectType = E_ObjectType.Interact;
    }
}
