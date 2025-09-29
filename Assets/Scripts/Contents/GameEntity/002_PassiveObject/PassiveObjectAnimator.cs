using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassiveObjectAnimator : GameEntityAnimator
{
    private PassiveObject m_PassiveObject;

    protected override void Awake()
    {
        base.Awake();
        m_PassiveObject = GetComponentInParent<PassiveObject>();
    }
}
