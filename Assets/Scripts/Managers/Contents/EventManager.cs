
using System;

public class EventManager : IManager
{
    public void Init()
    {
    }

    public void Clear()
    {
    }

    public event Action<NPCObject> NPCAppeared;
    public event Action<NPCObject> NPCDisappeared;


    public void OnNPCAppeared(NPCObject npc)
    {
        NPCAppeared?.Invoke(npc);
    }

    public void OnNPCDisappeared(NPCObject npc)
    {
        NPCDisappeared?.Invoke(npc);
    }
}