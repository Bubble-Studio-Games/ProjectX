using Data;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using static Define;

public abstract class BaseScene : MonoBehaviour
{
    public Define.Scene SceneType { get; protected set; } = Define.Scene.Unknown;

    protected IInputActionMapController m_InputActionMapController;

    // 초기 세팅 데이터
    [SerializeField] private GameObject m_InitObject;

    [Tooltip("세이브 파일을 로드할 것인가?")]
    public bool useLoadSaveFile = true;

    [Tooltip("데이터를 저장할 것인가?")]
    public bool isSaveFile = true;

    [Header("Scene")]
    public AudioClip m_SceneMainTemaAudioclip;

    public Action OnSceneStarted;

    #region Unity LifeCycle

    protected virtual void Awake()	
    {
        // 현재 씬에 활성 EventSystem이 있는지 확인
        var sceneEs = EventSystem.current;

        // 없으면 프리팹에서 하나 생성
        if (sceneEs == null)
        {
            var esGo = Managers.Resource.Instantiate("UI/EventSystem");
            esGo.name = "@EventSystem";
            return;
        }
    }

    protected virtual void Start()
    {
        Managers.Game.ResumeGame();

        m_InputActionMapController = Managers.SceneServices.InputActionMapController;
        m_InputActionMapController.PushActionMapGroup(GetRequiredActionMap());

        if (Managers.Data.IsRuntimeReady)
        {
            SafeDataLoad();
        }
        else
        {
            Managers.Data.OnRuntimeDataReady += SafeDataLoad;
        }
    }

    #endregion

    public virtual void Clear()
    {
        // 씬 종료 시 액션맵 복원
        Managers.SceneServices.InputActionMapController.PopActionMapGroup();
    }

    private void SafeDataLoad()
    {
        Managers.Data.OnRuntimeDataReady -= SafeDataLoad;
        DataLoad();
    }

    private void DataLoad()
    {
        // 제일 먼저 체크 → 세이브 데이터 건드리지 않음
        if (!useLoadSaveFile)
        {
            LoadNewGame();
            return;
        }

        // 여기서만 실제 load 호출
        var data = Managers.Game.GetContinueSaveData();

        if (data != null && data.dungeondata.gameEntityDatas.Count > 0)
        {
            LoadSavedGame(data);
        }
        else
        {
            LoadNewGame();
        }
    }

    protected virtual void LoadSavedGame(SaveSlotData data)
    {
        m_InitObject.SetActive(false);
    }
    protected virtual void LoadNewGame()
    {
        m_InitObject.SetActive(true);
    }

    protected virtual E_InputActionMap GetRequiredActionMap() => E_InputActionMap.Lobby;
}
