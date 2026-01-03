using System;
using System.Collections.Generic;
using System.Linq;
using static Define;

public class TreasureChest : PassiveObject, IInteractable
{
    public event Action OnInteracted;

    public TreasureChest()
    {
        m_TeamId = E_TeamId.None;
        m_EObjectType = E_ObjectType.Interact;
    }

    public bool CanInteract(GameEntity interactor)
    {
        return true;
    }

    public int GetInteractRange()
    {
        return 1;
    }

    public void Interact(GameEntity interactor)
    {
        OnInteracted?.Invoke();

        m_AttributeSystem.Reward(null);
    }
}
