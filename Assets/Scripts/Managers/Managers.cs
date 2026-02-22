using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * Managers
 * ------------------------------------------------------------------
 * 게임 전반에서 사용되는 모든 시스템 매니저에 대한
 * 중앙 접근 지점(Central Access Point) 역할을 한다.
 *
 * ❖ 설계 목적
 * - Managers.Object, Managers.Sound 와 같은 정적 접근 방식 유지
 * - 불필요한 초기 로딩 비용을 줄이기 위해 Lazy Initialization 적용
 * - 매니저는 실제로 접근되는 시점에만 생성 및 초기화된다.
 *
 * ❖ 생성 방식
 * - 각 매니저는 처음 접근 시 new 로 생성된다.
 * - new 직후 Init()이 1회 호출됨을 보장한다.
 * - 이후 동일 인스턴스를 계속 재사용한다.
 *
 * ❖ 생명주기
 * - Managers 오브젝트는 DontDestroyOnLoad 로 유지된다.
 * - 개별 매니저는 씬 전환 시 Clear()를 통해 정리된다.
 *
 * ❖ 주의사항
 * - 매니저 생성자에서는 Unity API를 사용하지 않는다.
 * - 모든 초기화 로직은 Init() 내부에서만 수행한다.
 * - 매니저 간 의존성은 Init 시점에만 해결하도록 한다.
 */
[DisallowMultipleComponent]
public class Managers : MonoBehaviour
{
    static Managers s_instance; // 유일성이 보장된다
    static Managers Instance { get { return s_instance; } } // 유일한 매니저를 갖고온다

    #region Contents
    ObjectManager _object          ;
    GameManager _game              ;
    GameUIManager _gameUI          ;
    DialogueManager _dialogue      ;
    QuestManager _quest            ;
    TutorialManager _tutorial      ;
    EventManager _event            ;
    InventoryManager _inventory    ;
    ShopManager _shop              ;
    SelectionManager _selct        ;
    CommandManager _command        ;
    ServicesManager _sceneServices   ;
    SettingManager _setting;

    // 추가
    GridManager _grid;
    PathfindManager _path;
    PlayerManager _player;
    GridBuildingManager _building;
    TickManager _tick;

    public static ObjectManager Object =>
        Instance.GetOrCreate(ref Instance._object);

    public static DialogueManager Dialogue =>
        Instance.GetOrCreate(ref Instance._dialogue);

    public static QuestManager Quest =>
        Instance.GetOrCreate(ref Instance._quest);

    public static TutorialManager Tutorial =>
        Instance.GetOrCreate(ref Instance._tutorial);

    public static EventManager Event =>
        Instance.GetOrCreate(ref Instance._event);

    public static InventoryManager Inventory =>
        Instance.GetOrCreate(ref Instance._inventory);

    public static ShopManager Shop =>
        Instance.GetOrCreate(ref Instance._shop);

    public static ServicesManager SceneServices =>
        Instance.GetOrCreate(ref Instance._sceneServices);

    public static SettingManager Setting =>
        Instance.GetOrCreate(ref Instance._setting);

    public static GameManager Game =>
        Instance.GetOrCreate(ref Instance._game);

    public static GameUIManager GameUI =>
        Instance.GetOrCreate(ref Instance._gameUI);

    public static SelectionManager Selection =>
        Instance.GetOrCreate(ref Instance._selct);

    public static CommandManager Command =>
        Instance.GetOrCreate(ref Instance._command);


    // 추가

    public static GridManager Grid =>
        Instance.GetOrCreate(ref Instance._grid);

    public static PathfindManager Path =>
        Instance.GetOrCreate(ref Instance._path);

    public static PlayerManager Player =>
        Instance.GetOrCreate(ref Instance._player);

    public static GridBuildingManager Building =>
        Instance.GetOrCreate(ref Instance._building);

    public static TickManager Tick =>
        Instance.GetOrCreate(ref Instance._tick);

    #endregion

    #region Core
    PoolManager _pool         ;
    ResourceManager _resource ;
    SceneManagerEx _scene     ;
    SoundManager _sound       ;
    UIManager _ui             ;
    DataManager _data;

    public static PoolManager Pool =>
        Instance.GetOrCreate(ref Instance._pool);

    public static ResourceManager Resource =>
        Instance.GetOrCreate(ref Instance._resource);

    public static SceneManagerEx Scene =>
        Instance.GetOrCreate(ref Instance._scene);

    public static SoundManager Sound =>
        Instance.GetOrCreate(ref Instance._sound);

    public static UIManager UI =>
        Instance.GetOrCreate(ref Instance._ui);
        

    public static DataManager Data =>
        Instance.GetOrCreate(ref Instance._data);

    #endregion

    void Awake()
    {
        if (s_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Manager 정리 - 씬 전환 시 호출
    /// </summary>
    public static void Clear()
    {
        for (int i = 0; i < _createdManagers.Count; i++)
            _createdManagers[i].Clear();

        _createdManagers.Clear();
    }

    static readonly List<IManager> _createdManagers = new(16);

    // 매니저 공통 생성 헬퍼
    //
    // - manager가 null일 경우에만 new 한다.
    // - new 직후 Init()을 호출하여 초기화를 강제한다.
    // - 생성된 매니저는 Clear 대상 목록에 등록된다.
    //
    // 이 메서드를 통해 생성된 매니저는
    // 1) Lazy Initialization
    // 2) Init 1회 보장
    // 3) Clear 일괄 관리
    // 를 모두 만족한다.
    T GetOrCreate<T>(ref T manager)
        where T : class, IManager
    {
        if (manager == null)
        {
            manager = Activator.CreateInstance<T>();
            manager.Init();          // ⭐ 생성 직후 Init 보장
            _createdManagers.Add(manager);
        }
        return manager;
    }
}

public interface IManager
{
    void Init();
    void Clear();
}