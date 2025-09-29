using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class GameEntityData : MonoBehaviour
{
    public int m_iID;
    public string m_sName;
    public GameEntity prefab;
    public Transform visual;
}
