using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Loader : MonoBehaviour
{
    private void Awake()
    {
        Managers.Scene.AddSceneAdditive(nextScene: Define.Scene.LMStartScene);
    }
}
