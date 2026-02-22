using UnityEngine;

[RequireComponent(typeof(Poolable))]
public class Effect : ItemObject
{
    public override void OnEnable()
    {
        base.OnEnable();
        Destroy();
    }
}
