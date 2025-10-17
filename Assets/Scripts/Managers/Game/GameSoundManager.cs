using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSoundManager : MonoBehaviour
{
    public static GameSoundManager Instance;

    public void Awake()
    {
        Instance = this;
    }

    [Header("UI")]
    public AudioClip m_UIButtonClickAudioClip;

}
