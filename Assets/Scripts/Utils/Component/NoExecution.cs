using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000000)]
public class NoExecution : MonoBehaviour
{
    private void Awake()
    {
        var childCount = transform.childCount;
        for (var i = 0; i < childCount; i++)
        {
            var child = transform.GetChild(i);
            child.gameObject.SetActive(false);
            Recursive(child);
        }
    }

    private void Recursive(Transform parent)
    {
        var childCount = parent.childCount;
        for (var i = 0; i < childCount; i++)
        {
            var child = parent.GetChild(i);
            child.gameObject.SetActive(false);
            Recursive(child);
        }
    }
}
