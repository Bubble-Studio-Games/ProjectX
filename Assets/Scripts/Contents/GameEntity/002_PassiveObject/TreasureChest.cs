using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Define;

public class TreasureChest : PassiveObject, IInteractable
{
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
        //Debug.Log("보물상자 열기! 다운잼 100개 추가");
        //Inventory.Instance.AddDownJam(100);
    }
}
