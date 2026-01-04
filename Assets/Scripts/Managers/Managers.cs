
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Managers : MonoBehaviour
{
    static Managers s_instance; // 유일성이 보장된다
    static Managers Instance { get { Init(); return s_instance; } } // 유일한 매니저를 갖고온다

    #region Contents
    ObjectManager _object = new ObjectManager();
    GameManager _game = new GameManager();
    LayerManager _layer = new LayerManager();
    SelectionManager _selct = new SelectionManager();
    CommandManager _command = new CommandManager();
    DialogueManager _dialogue = new DialogueManager();
    QuestManager _quest = new QuestManager();
    TutorialManager _tutorial = new TutorialManager();
    EventManager _event = new EventManager();
    InventoryManager _inventory = new InventoryManager();
    ShopManager _shop = new ShopManager();

    public static ObjectManager Object { get { return Instance._object; } }
    public static GameManager Game { get { return Instance._game; } }
    public static LayerManager Layer { get { return Instance._layer; } }
    public static SelectionManager Selection { get { return Instance._selct; } }
    public static CommandManager Command { get { return Instance._command; } }
    public static DialogueManager Dialogue { get { return Instance._dialogue; } }
    public static QuestManager Quest { get { return Instance._quest; } }
    public static TutorialManager Tutorial { get { return Instance._tutorial; } }
    public static EventManager Event { get { return Instance._event; } }
    public static InventoryManager Inventory { get { return Instance._inventory; } }
    public static ShopManager Shop { get { return Instance._shop; } }
    #endregion

    #region Core
    PoolManager _pool = new PoolManager();
    ResourceManager _resource = new ResourceManager();
    SceneManagerEx _scene = new SceneManagerEx();
    SoundManager _sound = new SoundManager();
    UIManager _ui = new UIManager();
    TableManager _table = new TableManager();
    DataManager _data = new DataManager();
    SaveManager _save = new SaveManager();
    LoadManager _load = new LoadManager();

    public static PoolManager Pool { get { return Instance._pool; } }
    public static ResourceManager Resource { get { return Instance._resource; } }
    public static SceneManagerEx Scene { get { return Instance._scene; } }
    public static SoundManager Sound { get { return Instance._sound; } }
    public static UIManager UI { get { return Instance._ui; } }
    public static TableManager Table { get { return Instance._table; } }
    public static DataManager Data { get { return Instance._data; } }
    public static SaveManager Save { get { return Instance._save; } }
    public static LoadManager Load { get { return Instance._load; } }
    #endregion

    void Start()
    {
        Init();
    }

    /// <summary>
    /// Core Manager 초기화 - 모든 씬에서 필요한 범용 Manager
    /// </summary>
    static void Init()
    {
        if (s_instance == null)
        {
            GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<Managers>();
            }

            DontDestroyOnLoad(go);
            s_instance = go.GetComponent<Managers>();

            // Core Manager 초기화 (모든 씬에서 필요)
            s_instance._data.InitAsync();
            s_instance._data.Init();
            s_instance._pool.Init();
            s_instance._sound.Init();
            //s_instance._table.Init();
            s_instance._object.Init();
            s_instance._game.Init();
            s_instance._layer.Init();
            s_instance._dialogue.Init();
            s_instance._quest.Init();
            s_instance._tutorial.Init();
            s_instance._event.Init();
            s_instance._inventory.Init();
            s_instance._shop.Init();

            Application.targetFrameRate = 60;
        }
    }

    /// <summary>
    /// Manager 정리 - 씬 전환 시 호출
    /// </summary>
    public static void Clear()
    {
        Sound.Clear();
        Scene.Clear();
        UI.Clear();
        Pool.Clear();
        //Table.Clear();
    }


    public void OnApplicationQuit()
    {
        Game.OnApplicationQuit();
    }
}
