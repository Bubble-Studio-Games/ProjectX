using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using static Define;

public abstract class BaseScene : MonoBehaviour
{
    public Define.Scene SceneType { get; protected set; } = Define.Scene.Unknown;

    [Tooltip("세이브 파일을 로드할 것인가?")]
    public bool useLoadSaveFile = true;

    [Tooltip("데이터를 저장할 것인가?")]
    public bool isSaveFile = true;

    [Header("Scene")]
    public AudioClip m_SceneMainTemaAudioclip;

    void Awake()	{
		Init();
	}

	protected virtual void Init()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.PushActionMapGroup(GetRequiredActionMap());

        GameObject obj = FindAnyObjectByType<EventSystem>().gameObject;
        if (obj == null && GlobalSettings.Instance?.EventSystem == null)
            Managers.Resource.Instantiate("UI/EventSystem").name = "@EventSystem";

        if (obj == GlobalSettings.Instance?.EventSystem?.gameObject)
            return;

        if (GlobalSettings.Instance != null && GlobalSettings.Instance.EventSystem != null && obj != null)
            GameObject.Destroy(obj);
    }

    protected virtual void Start()
    {
        Managers.Game.ResumeGame();
    }

    public virtual void Clear()
    {
        // 씬 종료 시 액션맵 복원
        if (InputManager.Instance != null)
        {
            InputManager.Instance.PopActionMapGroup();
        }
    }

	protected virtual E_InputActionMap GetRequiredActionMap() => E_InputActionMap.Lobby;
}
