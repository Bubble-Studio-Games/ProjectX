using System;
using static Define;

public class TreasureChest : PassiveObject, IInteractable
{
    public event Action OnInteracted;
    bool m_isInteracted; // 상호작용 완료 여부 확인.

    public TreasureChest()
    {
        m_TeamId = E_TeamId.None;
        m_EObjectType = E_ObjectType.Interact;
    }

    public bool CanInteract(GameEntity interactor)
    {
        return !m_isInteracted;
    }

    public int GetInteractRange()
    {
        return 1;
    }

    public void Interact(GameEntity interactor)
    {
        OnInteracted?.Invoke();

        m_AttributeSystem.Reward();

        m_isInteracted = true;
    }
}
