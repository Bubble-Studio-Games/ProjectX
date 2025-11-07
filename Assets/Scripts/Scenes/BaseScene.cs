using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using static Define;

public abstract class BaseScene : MonoBehaviour
{
    public Define.Scene SceneType { get; protected set; } = Define.Scene.Unknown;

<<<<<<< HEAD
    [Tooltip("세이브 파일을 로드할 것인가?")]
    public bool useLoadSaveFile = true;

    [Tooltip("데이터를 저장할 것인가?")]
    public bool isSaveFile = true;

=======
>>>>>>> develop
    [Header("Scene")]
    public AudioClip m_SceneMainTemaAudioclip;

    void Awake()	{
		Init();
	}

	protected virtual void Init()
    {
        Object obj = GameObject.FindFirstObjectByType(typeof(EventSystem));
        if (obj == null)
            Managers.Resource.Instantiate("UI/EventSystem").name = "@EventSystem";


    }

    protected virtual void Start()
    {
        Managers.Game.ResumeGame();
<<<<<<< HEAD
=======

        // Sound
        Managers.Sound.Play(m_SceneMainTemaAudioclip, 1, Sound.Bgm);
>>>>>>> develop
    }

    public abstract void Clear();
}
