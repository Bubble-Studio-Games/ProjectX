using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassiveObjectSounder : GameEntitySounder
{
    private PassiveObject m_PassiveObject;

    protected override void Awake()
    {
        base.Awake();
        m_PassiveObject = GetComponent<PassiveObject>();
    }
}
