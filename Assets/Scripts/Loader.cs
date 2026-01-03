using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Loader : MonoBehaviour
{
    private void Awake()
    {
        Managers.Scene.LoadSceneAsync(nextScene: Define.Scene.LMStartScene);
    }
}
