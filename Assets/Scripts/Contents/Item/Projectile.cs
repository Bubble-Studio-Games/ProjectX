using Data;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Poolable), typeof(Rigidbody))]
public class Projectile : Item
{
    // --- 이번 변경 핵심: kinematic + MovePosition 방식 ---
    // * Rigidbody는 항상 kinematic
    // * 런처 코루틴이 FixedUpdate마다 MovePosition으로 이동
    // * 관통 방지를 위해 런처가 Sweep(SphereCast)로 먼저 맞는지 검사
    // * Sweep로 맞았을 땐 OnCollisionEnter가 호출되지 않을 수 있으므로 HandleHit를 직접 호출

    private AudioSource m_AudioSource;
    public Rigidbody m_Rigidbody { get; private set; }
    public Collider m_Collider { get; private set; }

    [Header("Info")]
    public float m_fStraightSpeed = 10f;
    public float ParabolaSpeed = 5f;

    [Header("Destroy")]
    private AttackData m_AttackPattern;
    public GameEntity m_Target { get; private set; }

    [Header("Fly")]
    [SerializeField] private AudioClip m_ProjectileFlyingAudioClip;

    [Header("Hit")]
    [SerializeField] private AudioClip m_ProjectileHitAudioClip;
    [SerializeField] private GameObject m_AfterProjectileHitPrefab;
    public bool m_IsHit { get; private set; } = false;

    private Vector3 _lastMoveDir = Vector3.forward;  // 마지막 프레임 이동 방향(박히는 방향 연출용)
    private Vector3 _lastPos;
    public float SweepRadius => 0.1f; // Sweep 반지름(투사체 크기에 맞춰 조절)

    public override void Awake()
    {
        base.Awake();

        m_AudioSource = GetComponent<AudioSource>();
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Collider = GetComponent<Collider>();
    }

    private void Start()
    {
        m_AudioSource.spatialBlend = 1f;
        m_AudioSource.maxDistance = 40f;

        m_AudioSource.clip = null;
        m_AudioSource.playOnAwake = false;

        m_Rigidbody.isKinematic = true;     // 항상 true 고정
    }

    public override void OnEnable()
    {
        base.OnEnable();

        m_Rigidbody.Sleep(); // 선택: 깔끔하게 물리 상태 정리

        // ✅ 준비 상태: 손/시전 중에는 충돌 OFF
        m_Collider.enabled = false;         
        m_IsHit = false;

        foreach (Transform child in transform)
            child.gameObject.SetActive(true);


        _lastPos = m_Rigidbody.position;
        _lastMoveDir = transform.forward;
    }

    public override void OnDisable()
    {
        base.OnDisable();

        foreach (Transform child in transform)
            child.gameObject.SetActive(true);

        m_Collider.enabled = false;
        m_IsHit = false;
    }

    public void Prepare()
    {
        // 준비(장전) 단계: 충돌 OFF
        m_IsHit = false;
        m_Collider.enabled = false;
    }

    public void Fire()
    {
        // 발사 단계: 충돌 ON
        m_IsHit = false;
        m_Collider.enabled = true;
    }

    public void StopOnHit()
    {
        // 히트 후: 중복 히트 방지 위해 충돌 OFF
        m_IsHit = true;
        m_Collider.enabled = false;
    }


    /// <summary>
    /// 런처가 MovePosition을 호출하기 직전(또는 직후)마다 호출해서
    /// '이 프레임에 실제로 이동한 방향'을 저장한다.
    /// - kinematic 방식이라 rigidbody.velocity가 없으므로, 박히는 방향/회전용으로 필요
    /// </summary>
    public void NotifyMoved(Vector3 newPos)
    {
        Vector3 delta = newPos - _lastPos;
        if (delta.sqrMagnitude > 0.000001f)
            _lastMoveDir = delta.normalized;

        _lastPos = newPos;
    }

    public Vector3 GetLastMoveDir() => _lastMoveDir;

    public void AttackReady(GameEntity owner, AttackData attack, GameEntity target)
    {
        Prepare(); // ✅ 여기서 collider OFF 보장

        foreach (Transform child in transform)
            child.gameObject.SetActive(true);

        // Audio
        if (m_ProjectileFlyingAudioClip != null)
            m_AudioSource.PlayOneShot(m_ProjectileFlyingAudioClip);

        m_Owner = owner;
        m_AttackPattern = attack;
        m_Target = target;
    }

    private void HitEffect(Vector3 hitPos)
    {
        if (m_ProjectileHitAudioClip != null)
            m_AudioSource.PlayOneShot(m_ProjectileHitAudioClip);

        if (m_AfterProjectileHitPrefab != null)
        {
            GameObject go = Managers.Resource.Instantiate(m_AfterProjectileHitPrefab);
            go.transform.position = hitPos;
            go.transform.rotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Sweep(구간 캐스팅) 또는 실제 Collision 이벤트에서 공통으로 호출되는 "히트 처리".
    /// - Sweep로 맞으면 OnCollisionEnter가 호출되지 않을 수 있으므로, 런처가 직접 호출한다.
    /// </summary>
    public void HandleHit(Collider hitCol, Vector3 hitPoint, Vector3 hitDir)
    {
        if (m_IsHit) return;

        // 레이어 필터(투사체가 반응할 대상만)
        int layerBit = 1 << hitCol.gameObject.layer;
        bool isValidLayer =
            ((layerBit & GameConfig.Layer.HitColLayerMask) != 0) ||
            ((layerBit & GameConfig.Layer.m_StructLayer) != 0);

        if (!isValidLayer) return;

        // 데미지 대상 찾기
        GameEntity target = hitCol.GetComponentInParent<GameEntity>();
        if (target != null && m_Owner != null && m_Owner.IsEnemy(target))
        {
            target.m_AttributeSystem.Hit(m_AttackPattern, m_Owner);
        }

        // 히트 연출(이펙트/사운드)
        HitEffect(hitPoint);

        // 박히기(시각적): 충돌 지점으로 고정 + 진행 방향으로 회전 + 부모 붙이기
        transform.position = hitPoint;
        if (hitDir.sqrMagnitude > 0.000001f)
            transform.rotation = Quaternion.LookRotation(hitDir.normalized);

        transform.SetParent(hitCol.transform, true);

        StopOnHit();
        Managers.Resource.Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision col)
    {
        var go = col.gameObject.GetComponent<GameEntity>();
        if (go != null || go.IsAlly(m_Owner) || go.m_IsSetuping) // 설치중이거나 아군이면 넘기기
            return;

        // ✅ 혹시 Sweep가 아닌 실제 충돌이 들어왔을 때도 HandleHit로 통일
        Vector3 hitPoint = col.contacts[0].point;
        Vector3 dir = GetLastMoveDir();
        HandleHit(col.collider, hitPoint, dir);
    }


    #region Data Save & Load

    public override BaseData CaptureSaveData()
    {
        var iData = base.CaptureSaveData() as ItemData;

        ProjectileData pdata = new ProjectileData
        {
            prefabName = name,
            spawnPosition = spawnTransform.position,
            spawnRotation = spawnTransform.rotation,
            position = transform.position,
            rotation = transform.rotation,
            guid = _guid,
            targetGuid = m_Target != null ? m_Target._guid : string.Empty,
            onwerGuid = iData.onwerGuid,

        };

        return pdata;
    }

    public override void RestoreSaveData(BaseData baseData)
    {
        base.RestoreSaveData(baseData);

        ProjectileData data = baseData as ProjectileData;

        if (!string.IsNullOrEmpty(data.targetGuid))
        {
            // 로드 후 Managers.Object에서 해당 guid를 가진 GameEntity를 찾아 연결
            m_Target = Managers.Object.FindByGuidObject<GameEntity>(data.targetGuid);
        }
    }

    #endregion
}
