using GLTF.Schema;
using RootMotion.FinalIK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using static Define;
using static UnityEngine.UI.Image;
using Material = UnityEngine.Material;
using Random = UnityEngine.Random;

public interface IAccessories<TAnimator, TSounder> 
    where TAnimator : GameEntityAnimator 
    where TSounder : GameEntitySounder
{
    public  List<TAnimator> GetAnimationsManager();
    public  TSounder GetSounderManager();
}

[RequireComponent(typeof(AttributeSystem))]
public class GameEntity : MonoBehaviour, IAccessories<GameEntityAnimator,  GameEntitySounder>
{
    // Event
    public event EventHandler OnSpawnObjectSelected; // 오브젝트를 배치하려고 할 때
    public event EventHandler OnObjectSpawned; // 씬에 생성되거나 활성화될 때
    public event EventHandler OnObjectDespawned; // 파괴되거나 비활성화될 때

    // Ref
    [Header("Ref")]
    protected List<GameEntityAnimator> m_AnimatorManagers;
    protected GameEntitySounder m_SounderManager;
    [SerializeField] public AttributeSystem m_AttributeSystem;
    public Collider m_HitCollider { get; protected set; }
    public SetupAnimation m_SetupAnimation { get; private set; }

    [SerializeField] public Transform[] m_ModelTransforms;
    protected HashSet<(Material mat, GameObject obj)> m_ModelMaterials = new();

    [Header("Info")]
    public GridPosition[] m_GridPositionOffsets;
    public GridPosition m_GridPosition { get; protected set; }
    public E_ObjectType m_ObjectType { get; protected set; }
    public E_Dir m_CurrentEDir = E_Dir.South;
    public E_TeamId m_TeamId;

    [SerializeField] float m_fDelayDestroyTime = 3f;

    [Header("Flag")]
    public bool m_IsDirectDesawnAtDeath = true; // 사망 후 바로 디스폰 처리 하는가?
    public bool m_IsSetuping { get; protected set; } = false; // 카드에서 뽑아 소환 중인가?

    // 전역 캐시 (Prefab 단위)
    private static Dictionary<string, bool> s_RotateSymmetryCache = new();
    public bool m_IsRotateSymmetry { get; private set; }

    public bool IsDead => m_AttributeSystem.m_IsDead;
    public float CurHP 
    {
        get => m_AttributeSystem.m_Stat.m_iCurrentHp;
        set => m_AttributeSystem.m_Stat.m_iCurrentHp = value;
    }
    public float MaxHP => m_AttributeSystem.m_Stat.m_iMaxHP;
    
    protected virtual void Awake()
    {
        m_AttributeSystem = GetComponent<AttributeSystem>();

        if(m_AnimatorManagers == null)
            m_AnimatorManagers = GetComponentsInChildren<GameEntityAnimator>().ToList();
        if(m_SounderManager == null)
            m_SounderManager = GetComponent<GameEntitySounder>();

        m_SetupAnimation = GetComponent<SetupAnimation>();
    }

    protected virtual void Start()
    {
        CheckRotateSymmetry();


        if (m_IsSetuping)
            return;

        // 맵에 그냥 배치되어 있을 경우
        InitSpawn();
        SpawnComplete();
    }

    protected void OnEnable()
    {
        // 이미 월드 내에 배치되어 있는 경우
        // 오브젝트를 배치하지 않은 상태에서 삭제한 경우를 대비하여
        m_IsSetuping = false;
    }


    protected virtual void Update()
    {

    }

    public IEnumerable<(Material mat, GameObject obj)> GetModelsMaterial()
    {
        if (m_ModelMaterials == null || m_ModelMaterials.Count == 0)
        {
            // 렌더러 리스트를 먼저 구해서 즉시 평가
            var renderers = m_ModelTransforms
                .Where(t => t != null)
                .SelectMany(t => t.GetComponentsInChildren<Renderer>(true))
                .ToArray(); // ✅ ToArray()로 즉시 평가 (지연 실행 방지)

            // 이제 renderer.materials로 개별 인스턴스 생성
            m_ModelMaterials = renderers
                .SelectMany(r => r.materials   // ✅ 인스턴스화 발생
                    .Where(m => m != null)
                    .Select(m => (mat: m, obj: r.gameObject)))
                .ToHashSet();
        }

        return m_ModelMaterials;
    }



    public List<Collider> GetChildColliders()
    {
        List<Collider> colliders = new List<Collider>();

        // root 포함 모든 자식 탐색
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (((1 << child.gameObject.layer) & LayerManager.Instance.HitColLayerMask) != 0)
            {
                Collider col = child.GetComponent<Collider>();
                if (col != null)
                {
                    colliders.Add(col);
                }
            }
        }

        return colliders;
    }

    protected void UpdateGridPosition()
    {
        GridPosition newGridPosition = LevelGrid.Instance.GetGridPosition(transform.position);

        if (newGridPosition != m_GridPosition)
        {
            // Unit changed Grid Position
            List<GridPosition> oldGridPositions = GetGridPositionListAtCurrentPosition();
            m_GridPosition = newGridPosition;
            List<GridPosition> newGridPositions = GetGridPositionListAtCurrentPosition();

            LevelGrid.Instance.UnitMovedGridPosition(this, oldGridPositions, newGridPositions);
        }
    }

    public virtual void OnDeselected()
    {
        //Debug.Log($"{name} DeSelectMe");
    }

    public virtual void OnSelected()
    {
        //Debug.Log($"{name} SelectMe");
    }

    public GridPosition GetGridPosition()
    {
        return m_GridPosition;
    }

    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }

    public List<GameEntityAnimator> GetAnimationsManager()
    {
        return m_AnimatorManagers;
    }

    public GameEntitySounder GetSounderManager()
    {
        return m_SounderManager;
    }

    #region Dir

    public E_Dir GetNextDir()
    {
        switch (m_CurrentEDir)
        {
            default:
            case E_Dir.South: return E_Dir.West;
            case E_Dir.West: return E_Dir.North;
            case E_Dir.North: return E_Dir.East;
            case E_Dir.East: return E_Dir.South;
        }
    }


    // GameEntity의 GridOffset과 원점인 GridPosition을 반환한다.
    public List<GridPosition> GetGridPositionListAtCurrentPosition()
    {
        return GetGridPositionListAtSelectPosition(m_GridPosition);
    }

    // 지정된 위치인 pivot을 기준으로 GameEntity의 GridOffset을 반영한다.
    public List<GridPosition> GetGridPositionListAtSelectPosition(GridPosition pivot)
    {
        // 피벗 셀 포함해서 점유 셀 리스트 만들기
        var result = new List<GridPosition> { pivot };

        // m_GridPositionOffsets의 오프셋들을 pivot 기준으로 회전 적용
        result.AddRange(LevelGrid.Instance.ToGridPosition(this, pivot));

        return result;
    }

    // GetRotationAngle 함수: 각 방향에 따른 회전 각도를 반환합니다.
    public int GetRotationAngle()
    {
        switch (m_CurrentEDir)
        {
            default:
            case E_Dir.South: return 0;   // 기존 Dir.Down
            case E_Dir.West: return 90;  // 기존 Dir.Left
            case E_Dir.North: return 180; // 기존 Dir.Up
            case E_Dir.East: return 270; // 기존 Dir.Right
        }
    }

    #endregion

    #region Setup & Spawn

    // 몬스터의 경우 몬스터 스포너에서 소환
    // 플레이어의 경우 카드 선택 -> 드로우 소환
    public virtual void SpawnStart()
    {
        InitSpawn();

        m_IsSetuping = true;

        OnObjectSpawned?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void InitSpawn()
    {
        Managers.Object.Add(gameObject);

        //Level grid 
        m_GridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.SetReserveGridPosition(GetGridPositionListAtCurrentPosition(), false, this);
        LevelGrid.Instance.AddUnitAtGridPosition(GetGridPositionListAtCurrentPosition(), this);

        // 타격 콜라이더 켜기

        // Find Hit Collider
        if (m_HitCollider == null)
        {
            m_HitCollider = GetChildColliders().FirstOrDefault();
        }

        m_HitCollider.enabled = true;
    }

    // 조작 가능해짐
    public virtual void SpawnComplete()
    {
        m_IsSetuping = false;
        m_AnimatorManagers.ToList().ForEach(animator => animator.AnimationPlay());
    }

    // 보통은 디스폰을 사망 애니메이션이 끝나면 바로 호출.
    public void DeSpawnStart()
    {
        OnObjectDespawned?.Invoke(this, EventArgs.Empty);
    }

    //
    public virtual void DeSpawnComplete()
    {
        LevelGrid.Instance.RemoveUnitAtGridPosition(GetGridPositionListAtCurrentPosition(), this);

        Managers.Object.Remove(gameObject);

        Managers.Resource.Destroy(gameObject);
    }

    // 플레이어가 카드에서 오브젝트를 드래그 해서 선택 중일 때
    public void SelectSpawnObject()
    {
        // 타격 콜라이더 끄기
        // Find Hit Collider
        if (m_HitCollider == null)
        {
            m_HitCollider = GetChildColliders().FirstOrDefault();
        }

        m_HitCollider.enabled = false;

        m_IsSetuping = true;

        // 고스트 메테리얼은 buildingGhost에서

        OnSpawnObjectSelected?.Invoke(this, EventArgs.Empty);

        if (m_AnimatorManagers == null)
            m_AnimatorManagers = GetComponentsInChildren<GameEntityAnimator>().ToList();
        if (m_SounderManager == null)
            m_SounderManager = GetComponent<GameEntitySounder>();

        m_AnimatorManagers.ToList().ForEach(a => a.AnimationStop());
    }

    public (int Min, int Max) GetGridPositionYOffset()
    {
        int min = 0;
        int max = 0;

        if(m_GridPositionOffsets.Length > 0)
        {
            min = m_GridPositionOffsets.Min(offset => offset.floor);
            max = m_GridPositionOffsets.Max(offset => offset.floor);
        }

        return (min, max);
    }

    private void CheckRotateSymmetry()
    {
        string prefabKey = gameObject.name; // Prefab 이름 기준 (필요하면 GUID 기반으로 교체)

        if (!s_RotateSymmetryCache.ContainsKey(prefabKey))
        {
            // 실제 체크 로직 실행
            var dirs = new[] { E_Dir.West, E_Dir.South, E_Dir.North, E_Dir.East };
            var results = dirs.Select(d => LevelGrid.Instance.ToGridPosition(this, d)).ToList();

            bool isSymmetry = results.All(r => r == results[0]);

            s_RotateSymmetryCache[prefabKey] = isSymmetry;
        }

        m_IsRotateSymmetry = s_RotateSymmetryCache[prefabKey];
    }


    #endregion



    public bool IsEnemy(GameEntity target)
    {
        return GetEnemyTeamIDs().Contains(target.m_TeamId);
    }

    public List<E_TeamId> GetEnemyTeamIDs()
    {
        List<E_TeamId> enemyTeams = new();

        switch (m_TeamId)
        {
            case E_TeamId.Player:
            case E_TeamId.NPC:
                enemyTeams.Add(E_TeamId.Monster);
                break;

            case E_TeamId.Monster:
                enemyTeams.Add(E_TeamId.Player);
                enemyTeams.Add(E_TeamId.NPC);
                break;
        }

        return enemyTeams;
    }
}
