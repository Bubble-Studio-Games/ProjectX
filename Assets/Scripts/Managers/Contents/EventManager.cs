
using System;

public class EventManager 
{
    public event Action<NPCObject> NPCAppeared;
    public event Action<NPCObject> NPCDisappeared;

    public void Init()
    {
    }

    public void Clear()
    {
    }

    public void OnNPCAppeared(NPCObject npc)
    {
        NPCAppeared?.Invoke(npc);
    }

    public void OnNPCDisappeared(NPCObject npc)
    {
        NPCDisappeared?.Invoke(npc);
    }
}