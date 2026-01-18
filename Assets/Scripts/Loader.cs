using UnityEngine;

public class Loader : MonoBehaviour
{
    private void Awake()
    {
        Managers.Scene.AddSceneAdditive(nextScene: Define.Scene.Start);
    }
}
