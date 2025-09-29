using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : PassiveObject
{
    public Obstacle()
    {
        m_ObjectType = Define.E_ObjectType.Obstacle;
    }
}
